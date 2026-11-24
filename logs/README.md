# /logs · Sesiones de los agentes

Transcripciones de Claude Code de la fábrica (regla lights-out 5), exportadas por `.claude/hooks/exportar-log.js`.

```
logs/sesiones/<session-id>/sesion.jsonl                 sesión del orquestador
logs/sesiones/<session-id>/subagentes/agent-<id>.jsonl  cada subagente lanzado en esa sesión
```

## Cuándo se exportan

| Momento | Mecanismo |
| --- | --- |
| Al terminar cada subagente | Hook `SubagentStop` (`.claude/settings.json`) |
| Al cerrar la sesión | Hook `SessionEnd` |
| Al cerrar una corrida y para recuperar lo pendiente | `node .claude/hooks/exportar-log.js --todo` (skill `cerrar-corrida`) |

La exportación es idempotente: cada vez se sobrescribe con la versión más reciente de la transcripción. Los hooks se cargan al iniciar Claude Code, así que una sesión iniciada antes de configurarlos solo se exporta con `--todo`.

## Privacidad

Antes de copiar, el script redacta correos, llaves de API y los términos privados listados en `.claude/redacciones.local.txt`, un archivo local ignorado por git para que esos términos no queden escritos en el repositorio. Antes de cada commit de logs, se comprueba con `grep` que no queden correos, llaves ni términos privados.
