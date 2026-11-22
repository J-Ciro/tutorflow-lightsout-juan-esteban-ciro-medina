# /logs · Sesiones de los agentes

- `sesiones/AAAA-MM-DD_<session-id>.jsonl`: transcripción completa de cada sesión de Claude Code. La copia el hook `SessionEnd` (`.claude/hooks/exportar-log.js`) al cerrar la sesión, con los correos y las llaves de API redactados.
- Si una sesión termina de forma abrupta y no se copia, se exporta a mano con `/export` y se guarda aquí.
- Las transcripciones de los subagentes forman parte de la transcripción de la sesión principal.
