# REPORTE.md · Corridas, costo y eficiencia

Se actualiza al cerrar cada corrida con la skill `cerrar-corrida`. Los tokens de la fábrica salen de `/cost` de Claude Code; los del producto, del runner de evals. Si un dato no se midió, dice "pendiente": no se estima a ojo.

## 0. Bloqueos del entorno

| Fecha | Bloqueo | Impacto | Estado |
| --- | --- | --- | --- |
| 2026-11-22 | La máquina no tiene el SDK de .NET, Docker ni GitHub CLI (`gh`). Sí tiene `git` 2.56 y `node` 26 | No se puede ejecutar `implementar-spec base`: sin el SDK no hay build ni pruebas, y la fábrica no declara éxito sin evidencia | Resuelto: SDK de .NET 11, Docker Desktop y Ollama 0.40.17 con `qwen2.5:7b` instalados y verificados. `gh` sigue sin instalar (protección de ramas pendiente del humano) |
| 2026-11-22 | Postman CLI | — | Resuelto: 1.93.0 instalado y verificado en local, sin login |

## 1. Corridas de la fábrica

| # | Fecha | Spec / objetivo | Inicio–fin | Iteraciones | Intervenciones | Tokens | USD | Resultado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 2026-11-22 | Arranque: AGENTS, FABRICA, subagentes, skills, SPEC y escenarios; revisión adversarial de la spec (2 revisores: 30 hallazgos incorporados); CA en Gherkin; SPEC reestructurada (v1.1); CLAUDE.md de contexto | 2026-11-22 20:35 UTC | 1 | INT-001…INT-019 | en el total (§2) | en el total (§2) | Fábrica configurada; spec endurecida |
| 1 | 2026-11-22 a 2026-11-24 | `implementar-spec 001`: base, rondas 1–4 y ronda de cierre (spec v2.0 → v2.2, D-41…D-104); verificación con Ollama y con Gemini; auditorías de seguridad y calidad; CI endurecido; README de la solución | 2026-11-22 → 2026-11-24 05:18 UTC | 4 rondas + cierre | INT-020…INT-034 | en el total (§2) | en el total (§2) | 001 cerrada; frontend (002) especificado y fuera de alcance por tiempo |

## 2. Totales

Los tokens salen de las transcripciones de `~/.claude/projects/` (sesión principal y todos los subagentes, sin duplicar mensajes). El USD de la sesión actual sale de `/cost`; el de la parte anterior a la compactación, que `/cost` no cubre, se estima en proporción a la caché leída de cada modelo y se indica como estimado.

| Métrica | Valor |
| --- | --- |
| Horas humanas | ≈ 1 h en intervenciones registradas (55 min en las 31 INT con costo); tiempo de pared de la sesión: 2026-11-22 20:35 → 2026-11-24 05:18 UTC |
| Corridas | 2 |
| Intervenciones | 34 (INT-001…INT-034) |
| Tokens Opus 5.5 (orquestador, `arquitecto-spec`, `seguridad`) | 4,20 M de salida · 6,5 k de entrada · 13,0 M de caché escrita · 828,1 M de caché leída (2.929 respuestas) |
| Tokens Sonnet 5.5 (`backend-dotnet`, `qa-calidad`, `devops-cicd`) | 5,09 M de salida · 10,2 k de entrada · 16,1 M de caché escrita · 828,9 M de caché leída (5.064 respuestas) |
| Tokens Haiku 5.5 (tareas internas de Claude Code) | 3,8 M de entrada · 0,34 M de salida (según `/cost`) |
| Costo de la sesión actual (`/cost`) | USD 398,94 (Opus 262,12 · Sonnet 136,26 · Haiku 0,56) |
| Costo de la parte anterior a la compactación (estimado) | USD ≈ 99,92 (Opus ≈ 65,62 · Sonnet ≈ 34,30) |
| Costo total de la fábrica (USD) | ≈ 498,86 |

## 3. Costo del producto por pregunta

Cálculo con precios, sin llamadas nuevas a la API: tokens medidos con Gemini `gemini-3.5-flash-lite` por HTTP (fase V, 5 corridas por pregunta) × precio de lista USD 0,30 / 1 M de entrada y 2,50 / 1 M de salida ([AI Studio](https://aistudio.google.com/docs/models/gemini-3.5-flash-lite)). La medición por HTTP solo da el total de tokens; la salida se estima con la longitud de la respuesta entregada (≈ 3,5 caracteres por token) y el resto se cuenta como entrada. Mezcla: las 10 preguntas de ejemplo de Q10 con el mismo peso.

| Tipo de pregunta | % (mezcla Q10) | Tokens prom. | Salida estimada | USD prom. |
| --- | --- | --- | --- | --- |
| Reglamento (RAG): P1, P4, P7, P8 | 40 % | 1.198 | 38–106 | 0,000517 |
| Datos propios (herramientas): P2, P3, P5 | 30 % | 2.136 | 25–56 | 0,000738 |
| Coordinador (programa): P6 y ESC-040…046 | 10 % | 2.145 | 17–56 | 0,000736 |
| Sin información o denegada en el filtro previo: P9, P10 | 20 % | 0 | 0 | 0 |

**Costo estimado por 1.000 preguntas:** ≈ USD 0,50 (= 0,4 × 0,000517 + 0,3 × 0,000738 + 0,1 × 0,000736) × 1.000. Cotas con los mismos tokens: USD 0,40 si todo fuera entrada y USD 3,34 si todo fuera salida (por ejemplo, con tokens de razonamiento facturados como salida). El precio de lista puede diferir de la factura (riesgo aceptado por el humano, D-105). Las cifras se midieron antes de D-98; el modo bloque quita las herramientas en las preguntas de datos, así que se espera igual o menos.

## 4. Incidentes de la fábrica
Agentes que reportaron éxito falso, bucles que no convergieron o fallos de CI.

| Fecha | Incidente | Detección | Corrección |
| --- | --- | --- | --- |
| 2026-11-22 | La tabla de datos de referencia de la spec tenía 3 valores intermedios mal transcritos (p. ej. 3.51 en lugar de 3.45); las definitivas finales eran correctas | Verificación cruzada del orquestador contra un cálculo con aritmética entera | Corregido antes del commit de la spec |
| 2026-11-22 | Un script de edición de la fábrica pegó 5 filas de decisiones al inicio de `SPEC.md` (commit `2c176c2`). En este entorno el shell reduce la barra invertida doble a una sola dentro de los scripts. Por eso la regex escrita para coincidir con una barra vertical literal antes de "D-01" quedó como una alternancia vacía y coincidió con el inicio del archivo | El humano lo vio en la revisión visual (INT-003); la verificación de la fábrica no lo detectó | Reconstrucción sin regex; verificación obligatoria tras cada edición por script (`head`, filas pegadas `||`, IDs duplicados) |
| 2026-11-22 | Los commits `a0613a4`, `13892b7`, `8cc2f2b` y `c93b84f` (INT-003/INT-004) se hicieron directamente en `main`: tras el merge anterior no se creó una rama nueva y el merge posterior no tuvo efecto | Revisión del log por el orquestador antes de reportar | Sin reescribir el historial. Se añadió `.githooks/pre-commit`, que bloquea los commits directos en `main` |
| 2026-11-23 | El commit `794aadf` (test(core) raíces de contenido) incluye además `ApoyoYSinRespaldoTests`, `CitasYNumerosTests` y `FugaConfiguracionTests`: el hook rechazó un intento previo con asunto de 62 caracteres y los archivos quedaron preparados | Reporte del propio agente `qa-calidad` | Sin reescribir historial; el brief de los agentes pide revisar `git status` tras un rechazo del hook |
| 2026-11-22 | El commit `cb91e98` (asunto "add academic API client with route checks") incluye además la auditoría JSON y el resolutor del proveedor del LLM: un error al encadenar comandos de git del agente `backend-dotnet`; es casi idéntico en asunto a `f0d2045` | Revisión de calidad (fase B, H-01/N-10) | Sin reescribir historial; los briefs exigen `git show --stat` antes de cada commit |
| 2026-11-23 | Commits no atómicos `9be980f`, `41f9bf4`, `a1a3132` y `aa36e2d` (ronda 4) | Revisión del log por el orquestador | Sin reescribir historial; los briefs piden un commit por regla y `git show --stat` antes de confirmar |
| 2026-11-23 | Agentes que entregaron el informe con procesos en segundo plano vivos (3 en espera de un shell) | El humano lo vio en el panel (INT-026) | Detenidos con `TaskStop`; AGENTS.md §7 exige cerrar con "Procesos en segundo plano: ninguno" |
| 2026-11-24 | Commits no atómicos `8f00584` (D-78 y D-79 juntos), `092612e` (6 reglas de D-76) y `0102f11` (asunto de B-08 con el contenido de B-02/03/05/06, por commits lanzados en paralelo) | Revisión de calidad (fase B, M-03) y reporte del agente | Sin reescribir historial; los briefs exigen commits secuenciales y uno por regla |
| 2026-11-24 | Gemini respondía HTTP 400 en todas las llamadas: el adaptador enviaba `seed`, que Gemini no admite, y luego perdía la `thought_signature` | Medición de Q10 con Gemini; diagnóstico con el mensaje del proveedor | Semilla eliminada (INT-032, D-85) y dato opaco del proveedor reenviado (D-86); errores del proveedor diagnosticables en el log sin secretos |
