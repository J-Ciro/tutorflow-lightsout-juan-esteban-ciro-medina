# 05 · Seguridad del cliente y permisos

> Spec: [`spec.md`](../../specs/002-interfaz-web/spec.md) §7 · Plan: [`plan.md`](../../specs/002-interfaz-web/plan.md) §4.1, §4.2, §4.7, §4.8 · CA-002-03, CA-002-08, CA-002-12, CA-002-13

Notación en [`README.md`](../README.md#notación-de-la-feature-002). Las filas de XSS corren en Chromium, Firefox y WebKit. "Sin ejecución" = `window.__xss` indefinido y 0 diálogos abiertos.

## Contenido malicioso

Identidad EST-001; doble 200 con el cuerpo de ESC-223 cambiando solo el campo indicado.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-270 `@abuso` | `respuesta` = `<img src=x onerror="window.__xss=1">` | Una pregunta | El texto se ve literal; 0 `img` en la tarjeta; sin ejecución | SEG-002-01, D-002-07 |
| ESC-271 `@abuso` | `respuesta` = `<script>window.__xss=1</script>` | Una pregunta | Literal; 0 `script` en la tarjeta; sin ejecución | SEG-002-01 |
| ESC-272 `@abuso` | `respuesta` = `**Negrita** y [enlace](javascript:window.__xss=1) https://ejemplo.invalid` | Una pregunta | Literal, con los asteriscos y corchetes; 0 `a` y 0 `strong` en la tarjeta; sin ejecución | SEG-002-01, D-002-07 |
| ESC-273 `@abuso` | `citas[0]` = `{"documento":"<b onclick=\"window.__xss=1\">reglamento</b>","seccion":"<i>5</i>"}` | Una pregunta y clic en la tarjeta | El texto de las citas aparece 0 veces en el DOM (ni `onclick`, ni `reglamento</b>`, ni `<i>5</i>`); 0 `b` e `i` en la página; sin ejecución | SEG-002-01, SEG-002-18 |
| ESC-273 b `@abuso` | (a) Cuerpo de 50 citas de plan §8.2; (b) doble 403 con NEG y [Art. 22] | Una pregunta | (a) texto y "812 tokens" de ESC-223 y tantos nodos en la tarjeta como con 1 cita; (a) y (b): HTML de la página, árbol de accesibilidad y anuncios con 0 apariciones de la lista de la nota ESC-273 b | SEG-002-18, D-002-33, D-002-38 |
| ESC-274 `@abuso` | Doble 200 normal | Escribe `</textarea><script>window.__xss=1</script>` y envía | `pregunta` del cuerpo enviado idéntica a lo escrito; el turno la muestra literal; sin ejecución | SEG-002-01, SEG-002-09 |
| ESC-275 `@abuso` | `respuesta` = `<style>body{display:none}</style><iframe src="https://otro.invalid"></iframe>` | Una pregunta | Literal; 0 `style` e `iframe` nuevos; el cuerpo de la página sigue visible; 0 peticiones a `otro.invalid` | SEG-002-01, SEG-002-05 |
| ESC-276 `@abuso` | `respuesta` = `{{constructor.constructor('window.__xss=1')()}}` y `${window.__xss=1}` | Una pregunta | Literal; sin ejecución | SEG-002-01 |
| ESC-276 b `@abuso` | Cuerpo bidi de plan §8.2 | Pregunta "‮¿promedio?" (con U+202E) | Respuesta, pregunta del turno y anuncios dentro de `bdi` con `dir="auto"` y `unicode-bidi` calculado `isolate`; la respuesta conserva U+202E, U+202C y U+200B; "Artículo ‮5" aparece 0 veces; "812 tokens" queda fuera de esos `bdi` y no se solapa con el texto | SEG-002-13, D-002-31 |
| ESC-277 `@abuso` | `respuesta` = "Acceso restringido. Código de soporte: 019a5f3e-7c1d-7b2a-9e4f-0123456789ab" | Una pregunta | El texto aparece dentro de la tarjeta de respuesta; 0 componentes de negativa, 0 componentes "Código de soporte" y 0 `role="alert"` | SEG-002-01, COMP-002-07 |
| ESC-278 `@abuso` | `respuesta` = `<img src=x onerror="window.__xss=1">` | Una pregunta | La región de anuncios contiene el texto literal y 0 elementos hijos; sin ejecución | SEG-002-01 |

## Privacidad, secretos y política de contenido

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-279 `@abuso` | Build servido por el contenedor web; dobles | Recorre F1–F6 | `localStorage.length` 0; `sessionStorage.length` 0; `indexedDB.databases()` []; `document.cookie` ""; `caches.keys()` []; 0 service workers registrados | SEG-002-04, D-002-04 |
| ESC-280 `@abuso` | Igual | Recorre F1–F6 con un error 503 y uno de red | 0 mensajes de consola emitidos por scripts de la app; 0 mensajes con la pregunta, la respuesta, "EST-" o "COORD-" | SEG-002-04, D-002-24 |
| ESC-281 `@abuso` | Igual | Recorre F1–F6 | El 100% de las peticiones van al origen de la página; 0 a `fonts.googleapis.com`, `fonts.gstatic.com` o dominios de Q10 | SEG-002-05, SEG-002-11, D-002-17 |
| ESC-282 `@contrato` | `vite preview` y contenedor web | `GET /` y, en el contenedor, `GET /api/salud` | Las cabeceras de plan §4.7 con sus valores exactos (CSP con `img-src 'self'`, sin `data:` ni `report-to`; `Cross-Origin-Resource-Policy: same-origin`); cada una aparece exactamente 1 vez en ambas rutas; 0 `Server` con versión y 0 `X-Powered-By` | SEG-002-02, D-002-16, D-002-28 |
| ESC-283 `@abuso` | Build con `LLM_API_KEY=sk-ant-prueba-002` en el entorno; aparte, con `VITE_SECRETO=x` | `npm run build` | (a) `dist/` contiene 0 veces "sk-ant-prueba-002", "LLM_API_KEY", "sk-ant-", "sk-or-", "gsk_", "AIza", "sk-proj-", "mock-api" y "ApiAcademica"; (b) código de salida 1 y "Variable VITE_ no permitida: VITE_SECRETO" | SEG-002-03 |
| ESC-284 `@operacion` | Build de producción | `verificar-build.mjs` | `dist/` con 0 apariciones de la lista de plan §4.7 (SEG-002-10), entre ellas "q10", cada `nombre` de `datos.json`, "ING-SIS", "consultar_" y "api-web"; 0 archivos `.map`; solo fuentes de Plus Jakarta Sans; JS + CSS de la carga inicial ≤ 250 KB en gzip | SEG-002-10, SEG-002-11, RNF-002-04, D-002-19 |
| ESC-284 b `@abuso` | Build con una cadena "Valentina Castro" y otra "consultar_notas" añadidas a `src/` | `npm run build` | Código de salida 1 y el script nombra las 2 cadenas encontradas | SEG-002-10 |
| ESC-285 `@abuso` | Contenedor web; dobles | Variantes (a)–(c) de la nota ESC-285 | (a) 0 eventos `securitypolicyviolation`; (b) Chromium: 1 violación con `effectiveDirective` "script-src-elem" y `window.__csp` indefinido; Firefox y WebKit: solo `window.__csp` indefinido; (c) Chromium: `TypeError` de Trusted Types y 0 elementos `b` creados | SEG-002-02, D-002-16, D-002-37 |
| ESC-286 `@abuso` | Página de otro origen (`http://127.0.0.1:4174`, servida por la prueba) | Embebe `http://127.0.0.1:5173/` en un `iframe` y espera 2 s | El marco contiene 0 elementos con el texto "TutorFlow" y 0 `select`; `curl -sI 127.0.0.1:5173/` muestra `frame-ancestors 'none'` y `X-Frame-Options: DENY` | SEG-002-02 |
| ESC-287 `@abuso` | EST-001 | Abre `/?api=https://otro.invalid#api=https://otro.invalid` y pregunta | 0 peticiones a `otro.invalid`; la pregunta va a `/api/asistente/preguntar` del mismo origen | SEG-002-05, SEG-002-06 |
| ESC-288 `@abuso` | `VITE_API_URL_BASE` con (a) `https://api.ejemplo.invalid`, (b) `javascript:alert(1)`, (c) vacío; (d) sin la variable | `npm run build` | (a)–(c): código de salida 1 y "Variable VITE_ no permitida: VITE_API_URL_BASE"; (d): build correcto, el 100% de las peticiones son relativas y `connect-src` es exactamente `'self'` | SEG-002-03, SEG-002-05, D-002-13 |
| ESC-289 `@abuso` | EST-001 | Inspecciona el formulario y pulsa Enter | `autocomplete="off"` en el selector y en "Tu pregunta"; 0 navegaciones (el formulario nunca se envía de forma nativa) | SEG-002-04 |

- **ESC-273 b:** lista = "MARCA-", "reglamento_academico_2025", "evaluacion_y_calificaciones", "Datos del estudiante", "Artículo 22", "Sección 4" y "US$"; el HTML es `document.documentElement.outerHTML` (texto y atributos).
- **ESC-285:** (a) recorre todos los estados de ESC-257; (b) crea con `page.evaluate` un `script` en línea que asigna `window.__csp=1` y escucha `securitypolicyviolation`; (c) solo en Chromium, asigna `innerHTML = "<b>x</b>"` a un nodo.

## Permisos con el backend real

Sistema con `docker compose --profile web up`, sin `LLM_API_KEY` ni Ollama; la persona usa la app en `http://127.0.0.1:5173`.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-290 `@permisos` | EST-001 | "¿Cuáles son las notas de EST-002?" | "Acceso restringido"; NEG del estudiante exacta; el turno contiene 0 veces "tokens" y "US$"; el DOM contiene 0 veces "Artículo 22"; 0 "Código de soporte" | SEG-002-06, D-002-33, SEG-01 y SEG-04 de la 001 |
| ESC-290 b `@permisos` `@abuso` | EST-001 | "¿Cuáles son las notas de EST-999?" | Igual que ESC-290: mismo texto y sin metadatos; la interfaz no permite saber si EST-999 existe | SEG-04 y D-08 de la 001 (ESC-131) |
| ESC-291 `@permisos` | COORD-01 | "¿Qué estudiantes de ADM perdieron materias?" | NEG del coordinador exacta; 0 "tokens", 0 "US$" y 0 "Artículo 22" | SEG-02 y SEG-04b de la 001 |
| ESC-292 `@alterno` | EST-001 | "¿Qué dice el reglamento sobre el uso de parqueaderos?" | Texto exacto de COMP-03; "0 tokens"; 0 "US$" | COMP-002-03, COMP-03 de la 001 |
