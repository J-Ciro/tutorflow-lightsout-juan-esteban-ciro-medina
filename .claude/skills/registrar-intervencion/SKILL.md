---
name: registrar-intervencion
description: Registra en INTERVENCIONES.md una corrección o redirección manual del humano sobre el trabajo de los agentes (qué pasó, qué hizo el humano y por qué). Úsala SIEMPRE que el usuario corrija, rechace, reoriente o edite a mano algo producido por la fábrica, aunque sea menor.
---

# Registrar una intervención humana

**Solo se registra** cuando el humano corrige, rechaza, reorienta o edita a mano algo que produjo la fábrica. **No se registra:** el requisito inicial, una aprobación sin cambios, problemas del entorno (herramientas faltantes, credenciales) ni decisiones que la fábrica tomó sola. Esos van en `REPORTE.md` (bloqueos e incidentes) o en las decisiones D-xx de la spec.

1. Lee `INTERVENCIONES.md` y toma el siguiente número `INT-NNN`.
2. Agrega una entrada al final con este formato exacto:

```markdown
### INT-NNN · AAAA-MM-DD HH:MM · <título corto>
- **Fase:** spec | pruebas | implementación | auditoría | CI | entorno | otro
- **Qué pasó:** comportamiento observado del agente o de la fábrica (hechos, con archivo o commit si aplica).
- **Qué hizo el humano:** la corrección o instrucción exacta.
- **Por qué:** el motivo.
- **Costo:** minutos humanos aproximados.
- **Prevención:** el cambio en AGENTS.md, un agente, una skill o una spec que evita que se repita (o "ninguno" con justificación).
```

3. Si la prevención implica cambiar una instrucción de agente, propón el diff al usuario y no lo apliques sin su visto bueno.
4. Commit: `docs(interv): log INT-NNN <resumen>` (60 caracteres como máximo).

Las palabras del humano se citan literalmente, sin exagerar ni suavizar. Nunca omitas una intervención: esconderla descalifica; registrarla no.
