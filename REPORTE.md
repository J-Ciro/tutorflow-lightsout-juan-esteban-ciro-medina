# REPORTE.md · Corridas, costo y eficiencia

Se actualiza al cerrar cada corrida con la skill `cerrar-corrida`. Los datos de tokens de la fábrica salen de `/cost` de Claude Code; los del producto, del runner de evals. Si un dato no se midió, dice "pendiente": no se estima a ojo.

## 1. Corridas de la fábrica

| # | Fecha | Spec / objetivo | Inicio–fin | Iteraciones | Intervenciones | Tokens | USD | Resultado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 2026-11-22 | Arranque: AGENTS, FABRICA, subagentes, skills, SPEC y escenarios | pendiente | — | INT-001, INT-002 | pendiente | pendiente | Fábrica configurada; implementación bloqueada por falta de SDK |

## 2. Totales

| Métrica | Valor |
| --- | --- |
| Horas humanas | pendiente |
| Corridas | 1 |
| Intervenciones | 2 |
| Tokens totales de la fábrica | pendiente |
| Costo total de la fábrica (USD) | pendiente |

## 3. Costo del producto por pregunta

| Tipo de pregunta | % esperado | Tokens prom. | USD prom. |
| --- | --- | --- | --- |
| Reglamento (RAG) | pendiente | pendiente | pendiente |
| Datos propios (herramientas) | pendiente | pendiente | pendiente |
| Denegada 403 / validación | pendiente | 0 | 0 |

**Costo estimado por 1.000 preguntas:** pendiente (= Σ % × USD prom. × 1.000), con el modelo y precios configurados en `appsettings.json`.

## 4. Incidentes de la fábrica
Agentes que reportaron éxito falso, bucles que no convergieron o fallos de CI. Ninguno por ahora.
