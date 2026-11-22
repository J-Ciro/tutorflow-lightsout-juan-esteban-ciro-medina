---
name: implementar-spec
description: Ejecuta el ciclo completo de la fábrica sobre una spec aprobada - pruebas primero, implementación, CI local, auditorías paralelas y bucle de corrección con límite. Úsala con el feature-id (p. ej. 001) cuando la spec ya fue aprobada por el humano.
---

# Implementar una spec aprobada

Entrada: `$ARGUMENTS` = `NNN`, el feature-id de `specs/NNN-<slug>/spec.md` (la feature base es `001`).

Anota la hora de inicio y fin de cada paso: la necesitas para `REPORTE.md`.

## 0. Preparación
- Confirma que la spec está aprobada (puerta humana de `nueva-spec`). Si no, detente.
- Rama `feat/NNN-<slug>` desde `main`. Verifica `git config core.hooksPath` = `.githooks`.

## 1. Pruebas primero (`qa-calidad`, fase A)
Delega la escritura de pruebas y evals para todos los CA y escenarios de la spec. Exige la evidencia de que fallan (rojo) y la matriz de trazabilidad. Commit `test(...)`.

## 2. Implementación (`backend-dotnet`; `devops-cicd` si toca Docker o CI)
Delega con la ruta de la spec y la lista de pruebas en rojo. Si las tareas son independientes (p. ej. CI y backend), lánzalas en paralelo. Exige el resumen real de `dotnet test`.

## 3. Verificación local (tú, el orquestador)
Ejecuta tú mismo `dotnet build -warnaserror`, `dotnet test`, los evals offline y, con la API levantada, la colección de Postman (`postman collection run tests/postman/tutorflow.postman_collection.json -e tests/postman/local.postman_environment.json`). No confíes en el reporte del agente: compara.

## 4. Auditoría paralela
Lanza **en el mismo mensaje** a `seguridad` y a `qa-calidad` (fase B, revisión) sobre `git diff main...HEAD`. Ninguno recibe tu opinión ni la del implementador: solo la spec y el diff.

## 5. Bucle de corrección (máximo 3 iteraciones)
- Junta los hallazgos críticos y altos y envíalos a `backend-dotnet` (o al agente dueño del archivo).
- Si un hallazgo revela un caso no cubierto, primero `qa-calidad` agrega la prueba que falla y luego se corrige.
- Repite los pasos 3 y 4 solo con el agente que reportó hallazgos.

## 6. Condiciones de parada
- **Éxito:** build sin advertencias, pruebas y evals en verde y `seguridad` y `qa-calidad` con APROBADO. Entonces `git switch main && git merge --no-ff feat/...` y luego la skill `cerrar-corrida`.
- **Escalar al humano** (detente y explica): 3 iteraciones sin converger; dos agentes en desacuerdo sobre la interpretación de la spec; un cambio exige tocar el contrato, la política de acceso o una prueba existente; un agente propone saltarse una verificación; o falta una herramienta del entorno (SDK, Docker).
- Nunca declares éxito sin la salida real de los comandos.
