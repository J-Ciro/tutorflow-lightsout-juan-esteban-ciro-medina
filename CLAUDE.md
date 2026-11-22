@AGENTS.md

# Notas específicas de Claude Code

- Subagentes de la fábrica: `.claude/agents/`. Skills del ciclo: `.claude/skills/`. Ver `FABRICA.md`.
- La sesión principal actúa como **orquestador**: delega en subagentes y no escribe código de producción directamente cuando existe un subagente para ello.
- Nuevo requisito → skill `nueva-spec`. Implementar una spec aprobada → skill `implementar-spec`. Corrección manual del humano → skill `registrar-intervencion`. Cierre de corrida → skill `cerrar-corrida`.
- Comandos habituales: `dotnet build -warnaserror`, `dotnet test`, `node mock-api/server.js`, `docker compose up`.
- Idioma: responde y documenta en español; los mensajes de commit van en inglés.
