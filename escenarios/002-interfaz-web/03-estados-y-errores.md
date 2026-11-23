# 03 · Errores y estado del servicio

> Spec: [`spec.md`](../../specs/002-interfaz-web/spec.md) §5 · Plan: [`plan.md`](../../specs/002-interfaz-web/plan.md) §4.1, §4.2, §4.5, §8.2, §8.3 · CA-002-05, CA-002-06

Notación en [`README.md`](../README.md#notación-de-la-feature-002). Salvo que la fila diga otra cosa: identidad EST-001, servicio en línea, pregunta "¿Cuál es mi promedio?" y, en los errores, el cuerpo `ProblemDetails` y el `X-Correlation-Id` "019a5f3e-7c1d-7b2a-9e4f-0123456789ab" de plan §8.2 ("u" en la tabla). Todo error se renderiza con `role="alert"`.

## Errores de la pregunta

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-235 `@alterno` | Doble 400 | Envía | "No pudimos procesar la pregunta. Revísala e intenta de nuevo."; el campo contiene otra vez "¿Cuál es mi promedio?" y tiene el foco; "Código de soporte: u"; el DOM contiene 0 veces "System.", "Bad Request" y "TutorFlow.Api" | COMP-002-06, SEG-002-07 |
| ESC-236 `@alterno` | Doble 413 | Envía | "La pregunta es demasiado grande para enviarla. Acórtala e intenta de nuevo."; la pregunta vuelve al campo; "Código de soporte: u" | COMP-002-06 |
| ESC-237 `@alterno` | Doble 415 | Envía y pulsa "Recargar" | "La aplicación no pudo enviar la pregunta. Recarga la página."; "Código de soporte: u"; "Recargar" recarga la página (1 navegación) | COMP-002-06 |
| ESC-238 `@alterno` | Doble 429 con `Retry-After: 12`; reloj falso | Envía | "Hay muchas preguntas en este momento. Podrás preguntar de nuevo en 12 s."; "Código de soporte: u"; la pregunta vuelve al campo; selector habilitado; "Preguntar" deshabilitado a 11.9 s y habilitado a 12 s; se anuncia "Ya puedes preguntar de nuevo."; 0 peticiones nuevas a 15 s | COMP-002-12 |
| ESC-238 b `@alterno` | Doble 429 con `Retry-After: 120`; reloj falso | Envía y espera 59 s | Texto "…en 02:00."; la cuenta visible dice "01:01" a los 59 s; el texto 429 no contiene "Hiciste", "tú" ni "otros" | COMP-002-12, UX-002-08 |
| ESC-238 c `@alterno` | Componente; doble 429 con `Retry-After: 60`; reloj falso | Envía, oculta la pestaña (`visibilityState` "hidden") 50 s y la muestra | La cuenta muestra "10 s" al volver (calculada por fecha de fin) | COMP-002-12, D-002-10 |
| ESC-239 `@abuso` | Doble 429 con `Retry-After` (a) ausente, (b) "0", (c) "3601", (d) "-5", (e) "1e9", (f) "abc", (g) "Wed, 21 Oct 2026 07:28:00 GMT"; (h) "1"; (i) "3600" | Envía | (a)–(g): "…en 60 s." y 60 s de espera; (h): "…en 1 s." y 1 s; (i): "…en 60:00." y 3600 s | D-002-10 |
| ESC-240 `@alterno` | Doble 503 y luego 200 con el cuerpo ESC-223 | Envía y pulsa "Reintentar" | "El asistente no está disponible en este momento. Intenta de nuevo en unos minutos."; "Código de soporte: u"; 1 comprobación de salud inmediata; "Reintentar" hace una 2.ª petición con cuerpo idéntico y el turno muestra la respuesta de ESC-223 | COMP-002-06, D-002-12 |
| ESC-241 `@alterno` | Doble 500 | Envía | "Ocurrió un error inesperado. Intenta de nuevo."; "Reintentar"; "Código de soporte: u" | COMP-002-06 |
| ESC-242 `@alterno` | Doble (a) 404, (b) 405, (c) 502 | Envía | Igual que ESC-241 | COMP-002-06 |
| ESC-243 `@alterno` | Doble 200 con (a) `text/html` "<html>Error</html>", (b) `citas` null, (c) `tokens` "12", (d) sin `costoUsd`, (e) `tokens` -1, (f) `tokens` 1.5, (g) `usoDatosEstudiante` "true" | Envía | "Recibimos una respuesta que no pudimos leer. Intenta de nuevo."; "Reintentar"; "Código de soporte: u"; la tarjeta no contiene nada del cuerpo | COMP-002-08 |
| ESC-244 `@alterno` | Doble que aborta la conexión (`failed`) | Envía | "No pudimos conectar con el asistente. Revisa tu conexión e intenta de nuevo."; "Reintentar"; 0 "Código de soporte"; 1 comprobación de salud inmediata | COMP-002-06, D-002-12 |
| ESC-245 `@alterno` | Componente; `fetch` simulado que nunca resuelve; `vi.useFakeTimers()` | Envía | A los 34 999 ms sigue "Consultando…"; a los 35 000 ms la señal está abortada y se muestra el texto de ESC-244 | COMP-002-11, D-002-11 |
| ESC-246 `@abuso` | Doble 503 con `X-Correlation-Id` (a) en mayúsculas, (b) "019a5f3e-7c1d-4b2a-9e4f-0123456789ab" (versión 4), (c) ausente, (d) `<b>x</b>` | Envía | El texto de 503 con 0 "Código de soporte" y 0 elementos `b` en la tarjeta | COMP-002-07, D-002-08 |
| ESC-247 `@feliz` | Solo Chromium con permisos `clipboard-read` y `clipboard-write`; doble 503 | Pulsa "Copiar código" | El portapapeles contiene exactamente "019a5f3e-7c1d-7b2a-9e4f-0123456789ab" | COMP-002-07 |
| ESC-248 `@abuso` | Doble 503 cuyo `detail` es "http://mock-api:4000/estudiantes/EST-002 sk-ant-prueba at TutorFlow.Api.Program" | Envía | El DOM contiene 0 veces "mock-api", "sk-ant-", "EST-002/" y "at TutorFlow" | SEG-002-07 |
| ESC-249 `@abuso` | Doble 302 con `Location: https://otro.invalid/robar` | Envía | El texto de ESC-244; 0 peticiones a `otro.invalid` | COMP-002-06, SEG-002-05 |

## Estado del servicio

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-250 `@feliz` | Doble de salud 200 `{"estado":"ok"}` | Abre la app y elige EST-001 | "Servicio en línea" con el punto #1f9d5c; "Preguntar" habilitado | COMP-002-09 |
| ESC-251 `@alterno` | Doble de salud que responde en 2 s | Abre la app y elige EST-001 | Durante los 2 s: "Comprobando servicio…" y "Preguntar" deshabilitado; después, "Servicio en línea" | COMP-002-09 |
| ESC-252 `@alterno` | Doble de salud (a) 503, (b) fallo de red, (c) 200 `{"estado":"degradado"}`, (d) 200 `text/html` | Abre la app y elige EST-001 | "Servicio no disponible"; aviso "El asistente no está disponible. Lo comprobamos de nuevo cada 30 s." con "Comprobar ahora"; "Preguntar" deshabilitado; Enter hace 0 peticiones | COMP-002-09, D-002-12 |
| ESC-253 `@alterno` | Salud 503 y, desde los 30 s, 200 ok; reloj falso | Espera | Peticiones de salud a los 0 s y 30 s (± 1 s); a los 30 s, "Servicio en línea" sin recargar | COMP-002-09 |
| ESC-254 `@feliz` | Salud 503 | Pulsa "Comprobar ahora" 3 veces mientras la 1.ª comprobación tarda 2 s | Exactamente 1 petición de salud adicional | D-002-12 |
| ESC-255 `@alterno` | Componente; salud 200 ok; `vi.useFakeTimers()`; `document.visibilityState` simulado | Pasa a "hidden" 120 s, luego a "visible" y dispara `visibilitychange` | 0 peticiones de salud mientras está oculta; 1 inmediata al volver | D-002-12 |
| ESC-256 `@alterno` | Componente; `fetch` de salud que nunca resuelve; `vi.useFakeTimers()` | Monta la app | A los 4 999 ms, "Comprobando servicio…"; a los 5 000 ms, señal abortada y "Servicio no disponible" | COMP-002-09, D-002-12 |
