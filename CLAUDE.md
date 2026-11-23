# TutorFlow Lights-Out

## Qué es
Asistente académico para estudiantes y coordinadores de una institución ficticia (Corporación Educativa Horizonte). Responde preguntas con base en el reglamento vigente, citando documento y sección, y consulta notas y asistencia mediante herramientas. Un estudiante solo ve sus propios datos; un coordinador, los de su programa.

## Para qué
Es la prueba técnica de Q10 "Senior Full Stack con IA", modalidad *lights-out*. El humano solo escribe Markdown y la configuración de agentes; el código, las pruebas, los evals, Docker y el CI los escriben agentes de IA. Se evalúa el producto con pruebas ocultas, la calidad de la spec, el arnés (CI y evals), la transparencia y el costo de la fábrica.

## Contexto clave
- Contrato de la API fijo: `POST /api/asistente/preguntar` y `GET /api/salud`, en el puerto 8080 (`SPEC.md` §3).
- Vigentes: `reglamento_academico_2025.md` (deroga el 2023), `evaluacion_y_calificaciones.md` y `bienestar_y_becas.md`.
- La API simulada entrega notas por corte; las definitivas, los promedios y la pérdida por inasistencia los calcula el backend.
- Periodo 2026-2. Usuarios de prueba: `EST-001`…`EST-006`, `COORD-01` (ING-SIS) y `COORD-02` (ADM).

## Stack tecnológico
| Capa | Tecnología |
| --- | --- |
| Backend | .NET 11 (`net11.0`), ASP.NET Core Minimal API |
| Pruebas | xUnit, `WebApplicationFactory`; API de punta a punta, rendimiento y simulación con Postman CLI (colecciones v3) |
| Evals | Runner propio en .NET (`evals/TutorFlow.Evals`) |
| LLM | Independiente del proveedor: se detecta por el prefijo de `LLM_API_KEY` (Anthropic, OpenAI, Groq, OpenRouter, Gemini…); sin llave, Ollama local con `qwen2.5:7b` (spec 001 §9.4) |
| API académica | Node 18+ (`mock-api/server.js`, puerto 4000) |
| Despliegue | Docker y Docker Compose |
| CI | GitHub Actions |
| Frontend (opcional) | React + TypeScript + Vite |

## Mapa del repositorio
| Ruta | Contenido |
| --- | --- |
| `SPEC.md` | Spec del sistema: objetivo, actores, contrato, matriz de acceso, decisiones e índice de features |
| `specs/<feature-id>/spec.md` | Spec de cada feature (RF, reglas, seguridad, datos, CA en Gherkin y plan de QA); plantilla en `specs/_plantilla/spec.md` |
| `escenarios/<feature-id>/` | Escenarios de aceptación y de abuso de cada feature (`ESC-NNN`) |
| `AGENTS.md` | Reglas de trabajo para los agentes |
| `FABRICA.md` | Cómo funciona la fábrica: agentes, ciclo y condiciones de parada |
| `INTERVENCIONES.md` | Correcciones del humano sobre la fábrica |
| `REPORTE.md` | Corridas, tokens, costo y bloqueos del entorno |
| `reglamento/` | Base de conocimiento (material de Q10, solo lectura) |
| `mock-api/` | API simulada y `datos.json` (material de Q10, solo lectura) |
| `preguntas_ejemplo.*` | 10 preguntas con la respuesta y la cita esperadas |
| `backend/` | Todo el .NET (lo crean los agentes): solución `TutorFlow.slnx`, `global.json` y propiedades compartidas |
| `backend/src/` | `TutorFlow.Core`, `TutorFlow.Infrastructure` y `TutorFlow.Api` |
| `backend/tests/` | Pruebas unitarias y de integración |
| `backend/evals/` | Casos y runner de evals |
| `backend/postman/` | Colecciones v3, entornos, mocks y simulaciones de Postman |
| `frontend/` | React + TypeScript (opcional, spec futura) |
| `logs/` | Transcripciones de las sesiones de agentes |
| `.githooks/` | Hook `commit-msg` |

## Agentes y skills
- **Subagentes:** `.claude/agents/`: `arquitecto-spec`, `backend-dotnet`, `frontend-react`, `devops-cicd`, `qa-calidad` y `seguridad`.
- **Skills:** `.claude/skills/`:
  - `nueva-spec`: requisito nuevo → spec + escenarios + aprobación humana.
  - `implementar-spec`: ciclo de pruebas, implementación y revisión.
  - `tdd`: prueba en rojo antes de cualquier línea de código de producción.
  - `criterios-gherkin-smart`: cómo redactar los CA.
  - `abrir-pr`: publicar una rama y preparar su PR hacia `develop` con título y descripción.
  - `registrar-intervencion`: anotar una corrección del humano.
  - `cerrar-corrida`: actualizar `REPORTE.md` y los logs.
- **Plugin `postman`** (marketplace oficial, habilitado en `.claude/settings.json`): skills `postman:*` para validación local, entre ellas `api-testing`, `api-simulation`, `performance-testing` y `collection-schema-v3`.
- **MCP Playwright** (`.mcp.json`, `@playwright/mcp@0.0.86` headless): validación de la interfaz web por `frontend-react` y `qa-calidad`.
- **Hooks y permisos:** `.claude/settings.json`; scripts de hooks en `.claude/hooks/`.
- **Patrón de orquestación:** la sesión principal planifica y delega; los subagentes, en paralelo y cada uno en su worktree, escriben, verifican y auditan (`FABRICA.md`, skill `implementar-spec`).
- **Reglas de trabajo:** `AGENTS.md`. **Funcionamiento de la fábrica:** `FABRICA.md`.

## Comandos esenciales
```bash
git config core.hooksPath .githooks          # una vez por clon
node mock-api/server.js                      # API simulada en :4000
ollama pull qwen2.5:7b                       # modelo local (sin LLM_API_KEY)
docker compose up                            # sistema completo en :8080
curl localhost:8080/api/salud                # salud

cd backend
dotnet build -warnaserror                    # compilar
dotnet test                                  # pruebas unitarias e integración
dotnet run --project evals/TutorFlow.Evals -- --offline   # evals (sin --offline: LLM real)
npm install -g postman-cli                   # una vez (validación local; no se usa en el CI)
postman collection run postman/collections/tutorflow \
  -e "postman/environments/local.environment.yaml"   # pruebas de API locales
```

En Claude Code: `/nueva-spec <requisito>`, `/implementar-spec <NNN>`, `/cerrar-corrida`.
