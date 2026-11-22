# INTERVENCIONES.md · Registro de intervenciones humanas

Cada corrección o redirección manual del humano sobre el trabajo de los agentes queda aquí (regla lights-out 4). Formato y procedimiento: skill `registrar-intervencion`.

Intervenir no descalifica; esconderlo sí.

### INT-001 · 2026-11-22 · Arranque de la fábrica dirigido por el humano
- **Fase:** spec
- **Qué pasó:** el repositorio estaba vacío. El humano entregó a Claude Code el PDF de la prueba, el material de Q10 y sus criterios: foco en el backend .NET 8, seguridad y permisos como prioridad, clean code sin complejidad ciclomática ni sobreingeniería, auditoría por subagentes, Conventional Commits y ramas convencionales con asunto de 60 caracteres como máximo.
- **Qué hizo el humano:** dirigió la creación de `AGENTS.md`, `FABRICA.md`, `SPEC.md`, `escenarios/`, subagentes y skills. Claude Code redactó los borradores y el humano los revisa y aprueba (puerta humana de la spec).
- **Por qué:** la regla 1 permite al humano escribir Markdown; se usó asistencia de IA para redactarlo y se declara aquí por transparencia.
- **Costo:** pendiente de medir (ver `REPORTE.md`, corrida 0).
- **Prevención:** no aplica; es el arranque planificado.

### INT-002 · 2026-11-22 · Entorno sin .NET SDK, Docker ni GitHub CLI
- **Fase:** entorno
- **Qué pasó:** al preparar el repositorio, la fábrica detectó que la máquina no tiene `dotnet`, `docker` ni `gh`. Hay `git` 2.56 y `node` 26.
- **Qué hizo el humano:** pendiente: instalar el .NET 8 SDK y Docker Desktop antes de ejecutar `implementar-spec base`.
- **Por qué:** sin el SDK los agentes no pueden compilar ni ejecutar pruebas, y la regla de la fábrica prohíbe declarar éxito sin evidencia.
- **Costo:** pendiente.
- **Prevención:** el paso 0 de `implementar-spec` verifica las herramientas antes de empezar.
