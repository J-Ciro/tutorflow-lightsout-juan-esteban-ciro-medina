# REPORTE.md · Corridas, costo y eficiencia

Se actualiza al cerrar cada corrida con la skill `cerrar-corrida`. Los tokens de la fábrica salen de `/cost` de Claude Code; los del producto, del runner de evals. Si un dato no se midió, dice "pendiente": no se estima a ojo.

## 0. Bloqueos del entorno

| Fecha | Bloqueo | Impacto | Estado |
| --- | --- | --- | --- |
| 2026-11-22 | La máquina no tiene .NET 8 SDK, Docker ni GitHub CLI (`gh`). Sí tiene `git` 2.56 y `node` 26 | No se puede ejecutar `implementar-spec base`: sin el SDK no hay build ni pruebas, y la fábrica no declara éxito sin evidencia | Abierto: instalar el .NET 8 SDK y Docker Desktop |

## 1. Corridas de la fábrica

| # | Fecha | Spec / objetivo | Inicio–fin | Iteraciones | Intervenciones | Tokens | USD | Resultado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 2026-11-22 | Arranque: AGENTS, FABRICA, subagentes, skills, SPEC y escenarios; revisión adversarial de la spec (2 revisores: 30 hallazgos incorporados); CA en Gherkin; SPEC reestructurada (v1.1); CLAUDE.md de contexto | pendiente | 1 | INT-001…INT-004 | pendiente | pendiente | Fábrica configurada; spec endurecida; implementación bloqueada (§0) |

## 2. Totales

| Métrica | Valor |
| --- | --- |
| Horas humanas | pendiente |
| Corridas | 1 |
| Intervenciones | 4 |
| Tokens totales de la fábrica | pendiente |
| Costo total de la fábrica (USD) | pendiente |

## 3. Costo del producto por pregunta

| Tipo de pregunta | % esperado | Tokens prom. | USD prom. |
| --- | --- | --- | --- |
| Reglamento (RAG) | pendiente | pendiente | pendiente |
| Datos propios (herramientas) | pendiente | pendiente | pendiente |
| Denegada en el filtro previo / validación | pendiente | 0 | 0 |

**Costo estimado por 1.000 preguntas:** pendiente (= Σ % × USD prom. × 1.000), con el modelo y los precios configurados en `appsettings.json`.

## 4. Incidentes de la fábrica
Agentes que reportaron éxito falso, bucles que no convergieron o fallos de CI.

| Fecha | Incidente | Detección | Corrección |
| --- | --- | --- | --- |
| 2026-11-22 | La tabla de datos de referencia de la spec tenía 3 valores intermedios mal transcritos (p. ej. 3.51 en lugar de 3.45); las definitivas finales eran correctas | Verificación cruzada del orquestador contra un cálculo con aritmética entera | Corregido antes del commit de la spec |
| 2026-11-22 | Un script de edición de la fábrica pegó 5 filas de decisiones al inicio de `SPEC.md` (commit `53fd241`). En este entorno el shell reduce la barra invertida doble a una sola dentro de los scripts. Por eso la regex escrita para coincidir con una barra vertical literal antes de "D-01" quedó como una alternancia vacía y coincidió con el inicio del archivo | El humano lo vio en la revisión visual (INT-003); la verificación de la fábrica no lo detectó | Reconstrucción sin regex; verificación obligatoria tras cada edición por script (`head`, filas pegadas `||`, IDs duplicados) |
