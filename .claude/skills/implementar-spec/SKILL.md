---
name: implementar-spec
description: Ejecuta el ciclo completo de la fábrica sobre una spec aprobada con el patrón de orquestación - el orquestador planifica y delega; subagentes en paralelo, cada uno en su worktree, escriben pruebas, implementan, verifican y auditan; bucle de corrección con límite. Úsala con el feature-id (p. ej. 001) cuando la spec ya fue aprobada por el humano.
---

# Implementar una spec aprobada

Entrada: `$ARGUMENTS` = `NNN`, el feature-id de `specs/NNN-<slug>/spec.md` (la feature base es `001`).

## Patrón de orquestación (obligatorio)

- **Tú eres el orquestador:** planificas, divides en tareas, delegas, evalúas reportes, integras ramas y decides. **No** escribes código, pruebas ni spec, y **no** verificas tú mismo: todo eso lo hacen subagentes. Solo editas la configuración de la fábrica y la bitácora (`INTERVENCIONES.md`, `REPORTE.md`) cuando el humano lo pide.
- **Paralelo por defecto:** toda tarea independiente se lanza en el mismo mensaje. Máximo **4 subagentes simultáneos**.
- **Aislamiento:** cada subagente que escribe trabaja con `isolation: "worktree"` en su propia rama `feat/NNN-<slug>--<tarea>`, creada desde la rama de la feature. Nunca dos agentes escriben en el mismo directorio.
- **Archivos sin solapamiento:** antes de lanzar, asigna a cada tarea los archivos o carpetas que le pertenecen. Si dos tareas necesitan el mismo archivo, van en serie.
- **Integración:** cuando un subagente termina, integras su rama a `feat/NNN-<slug>` con `git merge --no-ff`. Un conflicto lo resuelve el agente dueño de esos archivos, no tú.
- **Brief completo:** cada subagente recibe la ruta de la spec, sus IDs (CA, ESC, hallazgos), sus archivos, las reglas de commit (AGENTS.md §1 regla 2) y qué evidencia debe devolver. No recibe tu opinión sobre el resultado esperado de una revisión.

Anota la hora de inicio y fin de cada paso para `REPORTE.md`.

## 0. Preparación
- Confirma que la spec está aprobada. Si no, detente.
- Rama `feat/NNN-<slug>` desde `develop` actualizado. Verifica `git config core.hooksPath` = `.githooks`.
- Escribe el **plan de tareas**: tarea → agente → archivos → IDs → dependencias. Lo que no depende de nada va en la primera ola.

## 1. Pruebas primero (`qa-calidad` fase A, en paralelo) · skill `tdd`
Una tarea por área: `backend/tests/TutorFlow.Core.Tests`, `backend/tests/TutorFlow.Api.Tests`, `backend/evals/`, `backend/postman/`. Cada una devuelve la evidencia de rojo y su parte de la matriz de trazabilidad. Si las pruebas necesitan superficie pública que no existe, primero `backend-dotnet` crea un esqueleto sin lógica.

## 2. Implementación (en paralelo)
`backend-dotnet` × N, uno por funcionalidad vertical con archivos disjuntos (p. ej. cálculos de Core, autorización, flujo del asistente, adaptadores de Infrastructure, host de Api), y `devops-cicd` para Docker y CI. Cada uno devuelve el resumen real de `dotnet test` de su parte. Integras las ramas en el orden de dependencias.

## 3. Verificación (`qa-calidad` fase V)
Un subagente que **no** implementó ejecuta, sobre la rama integrada: `dotnet build -warnaserror`, `dotnet test`, evals offline, `docker compose up` con la prueba de humo, y la colección de Postman contra la API levantada (`postman collection run postman/collections/tutorflow -e "postman/environments/local.environment.yaml" --delay-request 1100`, desde `backend/`). Devuelve la **salida cruda** de cada comando. Tú comparas esa salida con lo que reportaron los implementadores; cualquier diferencia es un incidente en `REPORTE.md`.

## 4. Auditoría (en paralelo)
Lanza en el mismo mensaje `seguridad` y `qa-calidad` (fase B) sobre `git diff develop...HEAD`. Solo reciben la spec y el diff.

## 5. Bucle de corrección (máximo 3 iteraciones)
- Agrupa los hallazgos críticos, altos y medios por archivos dueños. Si un hallazgo exige una regla nueva, primero `arquitecto-spec` actualiza la spec (con aprobación humana si toca contrato o permisos).
- Por grupo, en paralelo y en worktrees: `qa-calidad` escribe la prueba de regresión en rojo y luego el agente dueño corrige (skill `tdd`).
- Repite el paso 3 y, del paso 4, solo los auditores que reportaron hallazgos.

## 6. Condiciones de parada
- **Éxito:** verificación en verde y `seguridad` y `qa-calidad` con APROBADO. Entonces la skill `cerrar-corrida` y después `abrir-pr` hacia `develop`; el humano hace el merge en GitHub.
- **Escalar al humano** (detente y explica): 3 iteraciones sin converger; dos agentes en desacuerdo sobre la spec; un cambio exige tocar el contrato, la política de acceso o una prueba existente; un agente propone saltarse una verificación; falta una herramienta del entorno.
- Nunca declares éxito sin la salida real de los comandos que devolvió el verificador.
