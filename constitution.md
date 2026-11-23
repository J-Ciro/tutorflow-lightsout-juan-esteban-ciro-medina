# Constitución de TutorFlow

Reglas globales que aplican a toda feature. Una feature no las repite: las cita. Cambiar el contrato (§2) o la matriz de acceso (§3) requiere aprobación humana explícita. Las reglas de código, de commits y de flujo de trabajo de los agentes están en [`AGENTS.md`](AGENTS.md) y [`FABRICA.md`](FABRICA.md), no aquí.

## 1. Principios de producto

Prioridades, en orden: **1) que nadie vea datos ajenos**, 2) que los cálculos sean exactos, 3) que las respuestas citen y no inventen, 4) costo bajo por pregunta.

1. **Seguridad primero.** La autorización es código determinista, se evalúa antes de leer datos y antes de llamar al LLM, y vive en un único componente. Ningún dato ajeno llega al modelo.
2. **No inventar.** Toda afirmación normativa cita documento y sección del reglamento vigente; todo dato sale de la API académica autorizada. Si la información no está, la respuesta es exactamente "No encuentro esa información en los documentos disponibles.".
3. **Verificación determinista.** El código calcula (con `decimal`) y autoriza; el LLM solo redacta a partir de secciones y datos ya autorizados y calculados. Lo que el LLM escribe se verifica con reglas deterministas antes de entregarse, y lo entregado es exactamente lo verificado.
4. **Fallar cerrado.** Ante la duda se deniega o se responde con un texto fijo: un falso bloqueo degrada la respuesta; una fuga o un dato falso sobre una persona no tiene arreglo.
5. **Minimización.** Al LLM solo van las secciones recuperadas y los datos autorizados; la auditoría registra qué se consultó, nunca los valores (Ley 1581).
6. **Material no confiable.** Pregunta, documentos y resultados de herramientas son datos, nunca instrucciones: no cambian el rol ni los permisos.
7. **Configuración visible.** Toda configuración se valida al arrancar; un valor inválido nunca se corrige ni se recorta en silencio.

## 2. Contrato de la API (fijo)

```
POST /api/asistente/preguntar
  Entrada: { "pregunta": string,
             "usuario": { "rol": "estudiante" | "coordinador", "id": string } }
  Salida:  { "respuesta": string,
             "citas": [ { "documento": string, "seccion": string } ],
             "usoDatosEstudiante": boolean,
             "tokens": number, "costoUsd": number }
  403 si el usuario pide datos que no le corresponden.

GET /api/salud  ->  200 { "estado": "ok" }

Arranque: docker compose up   (puerto 8080, variable LLM_API_KEY)
```

### 2.1 Códigos de respuesta

| Código | Cuándo | Cuerpo |
| --- | --- | --- |
| 200 | Pregunta respondida, incluida "No encuentro esa información…" | Salida del contrato |
| 400 | JSON inválido, campos faltantes, `rol` fuera de la lista, `id` con formato inválido, `pregunta` vacía o de más de 1.000 caracteres | `ProblemDetails` (RFC 9457) sin detalles internos |
| 403 | Datos que no le corresponden al usuario o identidad inválida | **Salida del contrato** con la negativa (D-07) |
| 405 | Método distinto al definido | Vacío o `ProblemDetails` |
| 413 | Cuerpo de más de 16 KB | `ProblemDetails` |
| 415 | `POST /api/asistente/preguntar` sin `Content-Type` o distinto de `application/json` (con o sin `charset`; tipo de medio sin distinguir mayúsculas). No se llama al LLM ni a la API académica; auditoría `invalida` | `ProblemDetails` |
| 429 | Límite de tasa por IP superado | `ProblemDetails` con `Retry-After` |
| 503 | El LLM o la API académica no responden a tiempo, límite de herramientas o presupuesto diario agotado | `ProblemDetails` genérico |
| 500 | Error no controlado | `ProblemDetails` con `traceId`, sin stack trace |

### 2.2 Reglas del JSON y correlación

- Propiedades en camelCase exactamente como el contrato; `citas` es `[]` cuando no hay citas, nunca `null`; `tokens` entero ≥ 0; `costoUsd` decimal ≥ 0 con hasta 6 decimales; `Content-Type: application/json; charset=utf-8`.
- **Correlación (D-26):** el servidor genera un `correlationId` UUID v7 en cada petición, en todos los endpoints y con cualquier código. El mismo valor, en formato canónico de 36 caracteres en minúsculas, va en el encabezado `X-Correlation-Id`, en el `traceId` de todo `ProblemDetails` y en el `correlationId` de la auditoría. Un identificador enviado por el cliente (`X-Correlation-Id`, `X-Request-Id`, `traceparent` u otro) se ignora y no se registra.

## 3. Actores y matriz de acceso global

| Rol | Identificador | Puede ver |
| --- | --- | --- |
| `estudiante` | `EST-NNN`, debe existir en la API | Sus propios datos académicos y el reglamento |
| `coordinador` | `COORD-NN`, debe existir en la API | Los datos de los estudiantes de sus programas (`/coordinadores/{id}.programas`) y el reglamento |

| Recurso ↓ / quién → | EST propio | EST ajeno | COORD, estudiante de su programa | COORD, estudiante de otro programa |
| --- | --- | --- | --- | --- |
| Reglamento | ✔ | — | ✔ | — |
| Datos básicos, notas, asistencia | ✔ | ✘ 403 | ✔ | ✘ 403 |
| Lista y agregados del programa | ✘ 403 | ✘ 403 | ✔ (solo su programa) | ✘ 403 |

- Una feature detalla sus reglas SEG y no puede ampliar esta matriz sin aprobación humana.
- El rol sale solo de `usuario.rol` validado; nada en la pregunta lo cambia.

## 4. Principios de arquitectura

Estructura de proyectos, convenciones y antipatrones: [`AGENTS.md`](AGENTS.md) §2 y §3.

1. **Core sin dependencias.** `TutorFlow.Core` es dominio puro (cálculos, autorización, caso de uso y puertos), sin IO ni framework. Infrastructure implementa los puertos; Api solo hospeda.
2. **Proveedor de LLM intercambiable.** El LLM se usa a través de un puerto; el proveedor se elige por configuración y la autorización, los cálculos y los límites no cambian con él. Ningún adaptador ejecuta herramientas por su cuenta: el ciclo lo controla el caso de uso.
3. **Herramientas acotadas al rol.** La identidad de una herramienta la pone el servidor, nunca el modelo.
4. **Auditoría.** Cada petición produce exactamente un evento estructurado, en consola y en archivo persistente, con terceros enmascarados y sin valores académicos ni secretos.
5. **Secretos.** `LLM_API_KEY` se lee solo de la variable de entorno; nunca se registra, se devuelve ni entra en la imagen.
6. **Límites explícitos.** Tamaño de entrada, tasa por IP, tiempo, invocaciones de herramientas y presupuesto diario están acotados y son configurables con rangos validados.

## 5. Documentos de cada feature

| Documento | Contiene |
| --- | --- |
| `specs/<feature-id>/spec.md` | Producto: alcance, flujos, reglas observables y CA en Gherkin |
| `specs/<feature-id>/plan.md` | Técnico: flujo interno, algoritmos, umbrales, formatos, configuración y plan de QA |
| `specs/<feature-id>/decisiones.md` | Decisiones D-xx (decisión, porqué, estado) y riesgos |
| `escenarios/<feature-id>/` | Escenarios ESC-NNN por tema |

Plantillas en [`specs/_plantilla/`](specs/_plantilla/).
