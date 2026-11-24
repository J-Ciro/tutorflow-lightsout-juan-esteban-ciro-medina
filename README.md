# TutorFlow

Asistente académico de la Corporación Educativa Horizonte (institución ficticia), construido como la prueba técnica de Q10 "Senior Full Stack con IA" en modalidad *lights-out*: el humano escribió solo Markdown y la configuración de los agentes; el código, las pruebas, los evals, Docker y el CI los escribieron agentes de IA.

El enunciado original de la prueba se conserva sin cambios en [`docs/ENUNCIADO.md`](docs/ENUNCIADO.md).

## Qué es

TutorFlow responde preguntas de estudiantes y coordinadores sobre el periodo 2026-2:

- **Reglamento:** responde con el reglamento vigente (`reglamento_academico_2025.md`, que deroga el de 2023, `evaluacion_y_calificaciones.md` y `bienestar_y_becas.md`) y cita documento y sección.
- **Datos académicos:** consulta notas y asistencia en la API académica simulada (`mock-api/`) y calcula en el backend, con aritmética `decimal`, las definitivas, el promedio ponderado, la pérdida por inasistencia, la habilitación, la prueba académica y la beca.
- **Acceso:** un estudiante solo ve sus propios datos; un coordinador, los de los estudiantes de su programa.
- **Sin información:** si la respuesta no está en las fuentes, responde exactamente "No encuentro esa información en los documentos disponibles.".

Objetivo, actores e índice de features: [`SPEC.md`](SPEC.md). Spec de la feature: [`specs/001-asistente-academico/spec.md`](specs/001-asistente-academico/spec.md).

## Arranque rápido

Requisito: Docker con Docker Compose. Nada más en el host.

```bash
export LLM_API_KEY=<tu-llave>      # o en un archivo .env junto a docker-compose.yml
docker compose up                  # API en http://localhost:8080
```

Compose levanta dos servicios: `api` (puerto 8080) y `mock-api` (puerto 4000, solo en la red interna de Compose; no se publica en el host).

### Elección del proveedor de LLM

El backend es independiente del proveedor (plan 001 §4, decisión D-84). Variables que lee el servicio `api`:

| Variable | Uso |
| --- | --- |
| `LLM_API_KEY` | Llave del proveedor. Solo se lee de la variable de entorno; nunca se registra, se devuelve ni entra en la imagen |
| `LLM_PROVEEDOR` | Proveedor explícito: `anthropic`, `openai`, `groq`, `openrouter`, `gemini`, `ollama` u `openai-compatible` (este exige `LLM_URL_BASE`) |
| `LLM_MODELO` | Modelo; sobreescribe el modelo pequeño por defecto de cada proveedor (`appsettings.json`) |
| `LLM_URL_BASE` | URL del proveedor compatible con OpenAI o de Ollama |

Orden de resolución (D-84): `LLM_PROVEEDOR` explícito; sin llave, Ollama; prefijo inequívoco de `LLM_MODELO` (`claude-`, `gemini-`, `gpt-`, o un modelo con `/` para OpenRouter); el prefijo de la llave solo como pista; `LLM_URL_BASE` sola, compatible con OpenAI. **La llave nunca se valida ni se rechaza por su formato.** Si nada permite decidir, el arranque lo registra como no configurado.

**Sin llave:** se usa Ollama local con `qwen2.5:7b` en `http://host.docker.internal:11434`:

```bash
ollama pull qwen2.5:7b
docker compose up
```

### Ejemplos

```bash
curl http://localhost:8080/api/salud
# {"estado":"ok"}

# Estudiante: sus propios datos
curl -X POST http://localhost:8080/api/asistente/preguntar \
  -H "Content-Type: application/json" \
  -d '{"pregunta":"¿Cuál es mi promedio ponderado del periodo?","usuario":{"rol":"estudiante","id":"EST-001"}}'

# Estudiante: datos ajenos -> 403 con la negativa y la cita del Artículo 22
curl -X POST http://localhost:8080/api/asistente/preguntar \
  -H "Content-Type: application/json" \
  -d '{"pregunta":"¿Cuáles son las notas de EST-002?","usuario":{"rol":"estudiante","id":"EST-001"}}'

# Coordinador: su programa
curl -X POST http://localhost:8080/api/asistente/preguntar \
  -H "Content-Type: application/json" \
  -d '{"pregunta":"¿Qué estudiantes de mi programa tienen alguna asignatura perdida?","usuario":{"rol":"coordinador","id":"COORD-02"}}'
```

Usuarios de prueba: `EST-001` a `EST-003` (ING-SIS), `EST-004` a `EST-006` (ADM), `COORD-01` (ING-SIS) y `COORD-02` (ADM).

## Contrato

Contrato fijo, definido en [`constitution.md`](constitution.md) §2:

```
POST /api/asistente/preguntar
  Entrada: { "pregunta": string, "usuario": { "rol": "estudiante" | "coordinador", "id": string } }
  Salida:  { "respuesta": string, "citas": [ { "documento": string, "seccion": string } ],
             "usoDatosEstudiante": boolean, "tokens": number, "costoUsd": number }

GET /api/salud  ->  200 { "estado": "ok" }
```

| Código | Cuándo |
| --- | --- |
| 200 | Pregunta respondida, incluida "No encuentro esa información…" |
| 400 | JSON inválido, campos faltantes, `rol` fuera de la lista, `id` con formato inválido, `pregunta` vacía o de más de 1.000 caracteres |
| 403 | Datos que no le corresponden al usuario o identidad inválida (cuerpo del contrato con la negativa) |
| 405 | Método distinto al definido |
| 413 | Cuerpo de más de 16 KB |
| 415 | `Content-Type` ausente o distinto de `application/json` |
| 429 | Límite de tasa por IP superado, con `Retry-After` |
| 503 | El LLM o la API académica no responden a tiempo, límite de herramientas o presupuesto diario agotado |
| 500 | Error no controlado, con `traceId` y sin stack trace |

Los errores son `ProblemDetails` (RFC 9457). Cada respuesta lleva `X-Correlation-Id` (UUID v7), el mismo valor del `traceId` y de la auditoría.

## Arquitectura

| Proyecto | Responsabilidad |
| --- | --- |
| `backend/src/TutorFlow.Core` | Dominio puro: cálculos, autorización, caso de uso y puertos; sin IO ni framework |
| `backend/src/TutorFlow.Infrastructure` | Adaptadores de los puertos: proveedores de LLM, API académica, reglamento y auditoría |
| `backend/src/TutorFlow.Api` | ASP.NET Core Minimal API: hospeda, valida la entrada y la configuración, y aplica límites y encabezados |

Flujo de una pregunta (detalle en [`specs/001-asistente-academico/plan.md`](specs/001-asistente-academico/plan.md)):

1. **Validación** del contrato y de la identidad contra la API académica (el `id` debe existir y coincidir con el rol).
2. **Filtro previo determinista, sin LLM:** deniega con 403 la mención de otra persona o de un programa ajeno, los agregados y rankings del estudiante y las preguntas por las capacidades de otro rol. Reconoce variantes de escritura (mayúsculas, tildes, homoglifos, caracteres invisibles).
3. **Recuperación** de las secciones vigentes del reglamento; si ninguna es relevante, responde el texto fijo sin llamar al LLM (`tokens: 0`).
4. **Herramientas acotadas al rol:** la identidad de cada herramienta la pone el servidor, nunca el modelo; un ID ajeno pedido por el modelo termina en 403. El ciclo de herramientas lo controla el caso de uso, con un máximo de 4 invocaciones.
5. **Datos de personas escritos por el backend (D-98):** cuando la pregunta es sobre datos de una persona, el modelo solo escribe una introducción breve y la explicación normativa; el bloque "Datos de <Nombre> (<ID>)" o los listados del programa los escribe el backend con los valores calculados.
6. **Verificación determinista de la salida:** citas solo de secciones entregadas y que respaldan la respuesta; bloqueo de nombres o IDs no autorizados, de cifras que no están en los datos ni en las secciones, de estados que no cuadran con los datos y de detalles internos del asistente. Lo entregado es exactamente lo verificado.

**Auditoría:** cada petición produce exactamente un evento JSON en una línea, en consola y en `auditoria-AAAA-MM-DD.jsonl` (volumen `auditoria` en `/app/auditoria`). Registra quién preguntó, el resultado, el motivo de denegación y qué datos se consultaron, sin valores académicos ni secretos; los terceros mencionados se enmascaran como `[TERCERO]` (plan 001 §12).

## Seguridad

Matriz de acceso ([`constitution.md`](constitution.md) §3):

| Recurso | Estudiante, propio | Estudiante, ajeno | Coordinador, su programa | Coordinador, otro programa |
| --- | --- | --- | --- | --- |
| Reglamento | Sí | — | Sí | — |
| Datos básicos, notas y asistencia | Sí | 403 | Sí | 403 |
| Lista y agregados del programa | 403 | 403 | Sí | 403 |

Defensas principales (reglas SEG de la [spec 001](specs/001-asistente-academico/spec.md) §7):

- La autorización es código determinista, se evalúa antes de leer datos y antes de llamar al LLM; ningún dato ajeno llega al modelo.
- La pregunta, los documentos y los resultados de herramientas son datos no confiables: llegan delimitados y no cambian el rol ni los permisos.
- La salida se verifica antes de entregarse; ante la duda, falla cerrado con un texto fijo.
- Límites: pregunta de 1.000 caracteres, cuerpo de 16 KB, 4 invocaciones de herramientas, 20 s para la fase del LLM, 6 s para la API académica y 600 preguntas por minuto por IP con bloqueo escalonado.
- Encabezados `nosniff`, `X-Frame-Options: DENY` y `Referrer-Policy: no-referrer`; CORS sin comodín; contenedores sin root, de solo lectura y sin capacidades; la API académica no se publica fuera de la red de Compose.

## Cumplimiento de la prueba técnica

| Requisito | Cómo se cumple | Referencia |
| --- | --- | --- |
| Responder con el reglamento y citar | Recuperación de secciones vigentes, cita `documento · sección`, el reglamento 2023 se trata como derogado | RF-01, COMP-02, COMP-05 |
| Herramientas para notas y asistencia | Herramientas acotadas al rol sobre la API académica; cálculos en el backend | RF-02, RF-03, RN-01 a RN-14 |
| Estudiante y coordinador | Matriz de acceso, filtro previo y 403 con la negativa del rol | `constitution.md` §3, SEG-01 a SEG-06 |
| "No lo sé" | Texto fijo "No encuentro esa información en los documentos disponibles." con `citas: []` | RF-05, COMP-03 |
| Auditoría | Un evento por petición, en consola y en archivo diario | RF-08, plan 001 §12 |
| .NET 11 | `net11.0` en `backend/Directory.Build.props`, SDK fijado en `backend/global.json` | `CLAUDE.md` |
| Arranque con `docker compose up` en 8080 | `docker-compose.yml` y prueba de humo en el CI | RF-10, CA-09 |
| Frontend (opcional) | No incluido por tiempo; su spec 002 está escrita en la rama `docs/002-interfaz-web` | `SPEC.md` |

## Pruebas y calidad

```bash
cd backend
dotnet build -warnaserror                                   # compilar
dotnet test                                                 # pruebas unitarias y de integración (xUnit)
dotnet run --project evals/TutorFlow.Evals -- --offline     # evals con LLM doble
dotnet run --project evals/TutorFlow.Evals -- --url http://localhost:8080 --corridas 3   # evals con el LLM real (sistema levantado)
postman collection run postman/collections/tutorflow \
  -e "postman/environments/local.environment.yaml"         # pruebas de API locales (Postman CLI)
```

Las colecciones de Postman (v3) están en `backend/postman/collections/`: `tutorflow` (contrato, permisos, inyección y falsos positivos), `tutorflow-rendimiento` y `tutorflow-simulacion`. Los casos de evals están en `backend/evals/casos/`.

El CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) corre en cada pull request y push a `develop` y `main`, y bloquea si falla alguno de estos trabajos:

| Trabajo | Qué verifica |
| --- | --- |
| `build` | Restauración bloqueada, build con warnings como error y `dotnet format --verify-no-changes` |
| `test` | Pruebas con cobertura y umbrales mínimos (Core 90%, total 80%) |
| `evals-offline` | Evals con LLM doble |
| `security` | Cero paquetes vulnerables (incluidos transitivos) y búsqueda de secretos con gitleaks |
| `docker` | `docker compose up`, salud 200, prueba de humo de un 403 y el puerto 4000 no publicado |
| `commits` | Mensajes conformes al hook `.githooks/commit-msg` |
| `limites-specs` | Límites de tamaño de specs, planes, decisiones y `SPEC.md` |
| `evals-llm` | Evals con el LLM real (3 corridas); solo si el repositorio tiene el secreto `LLM_API_KEY` |

Los resultados de las corridas, los tokens y el costo están en [`REPORTE.md`](REPORTE.md).

## La fábrica de agentes

El repositorio se construyó en modalidad *lights-out* con Claude Code. La sesión principal orquesta y delega en subagentes por disciplina (`arquitecto-spec`, `backend-dotnet`, `devops-cicd`, `qa-calidad`, `seguridad` y `frontend-react`), en paralelo y cada uno en su worktree. Quien escribe las pruebas no implementa y los revisores no editan. La única intervención humana planificada es aprobar la spec.

- [`FABRICA.md`](FABRICA.md): agentes, ciclo de trabajo, condiciones de parada y control de costo.
- [`AGENTS.md`](AGENTS.md): reglas de trabajo de los agentes.
- [`INTERVENCIONES.md`](INTERVENCIONES.md): correcciones del humano sobre la fábrica.
- [`REPORTE.md`](REPORTE.md): corridas, tokens, costo y bloqueos del entorno.
- [`logs/`](logs/): transcripciones de las sesiones de los agentes.
- `.claude/`: subagentes, skills, hooks y permisos.

## Estructura del repositorio

| Ruta | Contenido |
| --- | --- |
| `constitution.md` | Principios de producto, contrato de la API, matriz de acceso y arquitectura |
| `SPEC.md` | Objetivo, actores e índice de features |
| `specs/001-asistente-academico/` | Spec, plan técnico y decisiones de la feature |
| `escenarios/001-asistente-academico/` | Escenarios de aceptación y de abuso (`ESC-NNN`) |
| `backend/` | Solución .NET: `src/`, `tests/`, `evals/` y `postman/` |
| `docker/`, `docker-compose.yml` | Imagen de la API académica y orquestación |
| `.github/` | Workflow del CI y sus scripts |
| `reglamento/` | Base de conocimiento (material de Q10) |
| `mock-api/` | API académica simulada y `datos.json` (material de Q10) |
| `preguntas_ejemplo.md`, `preguntas_ejemplo.json` | 10 preguntas con la respuesta y la cita esperadas |
| `docs/ENUNCIADO.md` | Enunciado original de la prueba |
