---
name: cerrar-corrida
description: Cierra una corrida de la fábrica - actualiza REPORTE.md con horas, intervenciones, tokens y costo, recalcula el costo por 1.000 preguntas y verifica que los logs de la sesión estén en /logs. Úsala al terminar implementar-spec o al final de una sesión de trabajo.
---

# Cerrar una corrida

1. **Datos de la fábrica:** pide al usuario la salida de `/cost` (o `/usage`) de la sesión. No inventes tokens: si no hay dato, escribe "pendiente" y dilo.
2. **Datos del producto:** ejecuta el runner de evals con LLM real (si hay `LLM_API_KEY`) y toma del resultado los tokens y el `costoUsd` promedio por pregunta. Costo por 1.000 preguntas = promedio × 1.000. Separa las preguntas que no llaman al LLM (403 y validación), que cuestan 0.
3. **REPORTE.md:** agrega una fila a la tabla de corridas (fecha, spec, inicio-fin, iteraciones del bucle, intervenciones INT-xxx, tokens, USD, resultado) y actualiza los totales y la sección de costo del producto.
4. **Logs:** verifica que `logs/sesiones/` tiene la transcripción de la sesión (el hook `SessionEnd` la copia al cerrar). Si falta, pide al usuario `/export` y guárdala en `logs/`. Revisa con `grep` que no contenga llaves ni correos.
5. Commit: `docs(report): close run <spec>`.
