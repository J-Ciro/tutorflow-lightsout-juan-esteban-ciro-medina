---
name: cerrar-corrida
description: Cierra una corrida de la fábrica - actualiza REPORTE.md con horas, intervenciones, tokens y costo, recalcula el costo por 1.000 preguntas y verifica que los logs de la sesión estén en /logs. Úsala al terminar implementar-spec o al final de una sesión de trabajo.
---

# Cerrar una corrida

1. **Datos de la fábrica:** pide al usuario la salida de `/cost` (o `/usage`) de la sesión. No inventes tokens: si no hay dato, escribe "pendiente" y dilo.
2. **Datos del producto:** ejecuta el runner de evals con LLM real (si hay `LLM_API_KEY`) y toma del resultado los tokens y el `costoUsd` promedio por pregunta. Costo por 1.000 preguntas = promedio × 1.000. Separa las preguntas que no llaman al LLM (403 y validación), que cuestan 0.
3. **REPORTE.md:** agrega una fila a la tabla de corridas (fecha, spec, inicio-fin, iteraciones del bucle, intervenciones INT-xxx, tokens, USD, resultado) y actualiza los totales y la sección de costo del producto.
4. **Logs:** ejecuta `node .claude/hooks/exportar-log.js --todo`, que exporta la sesión del orquestador y todos sus subagentes a `logs/sesiones/<session-id>/`. Verifica que `.claude/redacciones.local.txt` existe y revisa con `grep` que no queden correos, llaves ni términos privados.
5. Commits: los logs van en su propio commit (`chore(logs): export run <spec> transcripts`, con `Generado-por: Claude Code`, porque no son `.md`) y el reporte en otro (`docs(report): close run <spec>`, sin trailer).
