# 06 · Despliegue, proxy y CI

> Spec: [`spec.md`](../../specs/002-interfaz-web/spec.md) RF-002-10, RF-002-11 (tasa del backend), SEG-002-14…17 · Plan: [`plan.md`](../../specs/002-interfaz-web/plan.md) §4.7, §9.3, §10, §11 · CA-002-14, CA-002-15

Notación en [`README.md`](../README.md#notación-de-la-feature-002). "Perfil web" = `docker compose --profile web up -d --build` desde la raíz, sin `LLM_API_KEY`. Nunca se aserta una IP concreta: todos los clientes de la interfaz comparten la de nginx (D-002-14).

"No llega al backend" = `$upstream_status` "-" en la línea del registro de `web`. Las preguntas por 5173 siguen el orden y los intervalos de plan §9.3.

## Docker Compose

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-293 `@operacion` | — | `docker compose up -d --build` sin perfil | `docker compose ps -q web` vacío; `docker network ls` sin redes nuevas de la 002; `GET localhost:8080/api/salud` 200 `{"estado":"ok"}`; `127.0.0.1:5173` no responde | D-002-15, D-002-14 |
| ESC-294 `@operacion` `@contrato` | — | Perfil web; `GET 127.0.0.1:5173/`, `GET 127.0.0.1:5173/api/salud` y el `POST` de ESC-290 por 5173 | `/`: 200 `text/html` con las cabeceras de ESC-282; salud: 200 `{"estado":"ok"}`; `POST`: 403 con el mismo cuerpo que por 8080 y `X-Correlation-Id` UUID v7 en minúsculas | D-002-13 |
| ESC-294 b `@abuso` | — | Perfil web; por 5173 obtiene (a) 200 de salud, (b) 403 de ESC-290, (c) 400 con cuerpo `{}`, (d) 413 con 17 KB, (e) 429 de ESC-299 e, (f) 503 con `docker compose stop mock-api` y "¿Cuál es mi promedio?" | Cada respuesta tiene exactamente 1 `Cache-Control: no-store`; `docker compose exec web nginx -T` contiene 0 veces `proxy_cache` | SEG-002-16, D-002-28 |
| ESC-295 `@abuso` | — | Perfil web; por 5173 las variantes (a)–(m) de la nota ESC-295 | Cada variante obtiene exactamente el resultado de la nota; todas las rechazadas por el servidor web no llegan al backend | SEG-002-14, SEG-002-16, D-002-28, D-002-29 |
| ESC-296 `@operacion` | — | Perfil web; inspecciona el contenedor `web` y su registro tras `GET /?id=EST-001` | Usuario 101 (no root); sistema de archivos de solo lectura; 0 capacidades; el puerto 4000 sigue sin publicarse; la línea de registro no contiene "EST-001", "?id" ni la IP del cliente | D-002-15, SEG-002-04 |
| ESC-296 b `@permisos` `@abuso` | Otro equipo de la red | Perfil web; `docker compose port web 8080` y conexión a `<IP de red del anfitrión>:5173` | El puerto publicado es `127.0.0.1:5173`; la conexión por la IP de red es rechazada; el selector no se puede usar desde otra máquina | SEG-002-17, D-002-15 |
| ESC-296 c `@operacion` | — | Perfil web; `docker compose restart api` | `GET 127.0.0.1:5173/api/salud` vuelve a 200 en ≤ 30 s; `StartedAt` del contenedor `web` no cambia | RNF-002-07, D-002-28 |
| ESC-296 d `@operacion` | — | Perfil web; `docker compose up -d --force-recreate --no-deps api` | Igual que ESC-296 c, aunque `api` resuelva a otra IP | RNF-002-07, D-002-28 |

- **ESC-295:** (a) `GET /api/otra` → 404; (b) `POST /api/asistente/preguntar` con 17 KB → 413 `application/problem+json` del backend; (c) `GET /api/asistente/preguntar` → 403; (d) `GET /api/s%61lud` → 200 de salud; (e) `GET /api/x/..%2Fsalud` → 200 de salud o 404.
- **ESC-295 (cont.):** (f) `GET //api//salud` → 200 de salud; (g) `GET /API/SALUD` → 404; (h) `GET /api/salud?x=<script>` → 200 y el registro muestra `$uri` "/api/salud".
- **ESC-295 (cont.):** (i) `OPTIONS /api/salud` → 403 o 405; (j) `TRACE /api/salud` → 405; (k) `PUT /api/asistente/preguntar` → 403; (l) `GET /api/salud/` → 404.
- **ESC-295 (sin límite en el proxy, D-002-29):** (m) configuración por defecto: 30 `POST` de COMP-03 como EST-001 en < 5 s → 30 respuestas 200 del backend y 0 × 429. (n) retirado (D-002-29).
- **ESC-295 (configuración):** `nginx -T` contiene 0 veces `$args`, `$is_args`, `$request_uri`, `$proxy_add_x_forwarded_for`, `limit_req` y `@limite`, y los 2 `proxy_pass` terminan en la ruta fija del contrato.

## IP compartida tras el proxy y tasa del backend

Antes de (a) y (e): `docker compose restart api` y espera de la salud (estado de la tasa vacío, D-002-37). Cada pregunta es "¿Qué dice el reglamento sobre el uso de parqueaderos?" (COMP-03 sin LLM). La tasa y su escalada se prueban en el backend: ESC-142 de la 001.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-299 `@operacion` | Perfil web | Por 5173, EST-001…EST-004 preguntan | 4 × 200 con COMP-03, 0 × 429; los 4 eventos tienen la misma `ipOrigen` (la de nginx, compartida); los eventos `multiples_identidades` no cambian ninguna respuesta | SEG-002-15, D-002-14 |
| ESC-299 b `@abuso` | Perfil web | Por 5173, 1 pregunta con `X-Forwarded-For: 203.0.113.7`, `Forwarded: for=203.0.113.8` y `X-Real-IP: 203.0.113.9` | La `ipOrigen` del evento es igual a la de ESC-299 y distinta de 203.0.113.7, 203.0.113.8 y 203.0.113.9; `nginx -T` reenvía esas 3 cabeceras vacías | SEG-002-15, D-002-14 |
| ESC-299 c `@abuso` | Perfil web | Por `localhost:8080`, 1 pregunta con `X-Forwarded-For: 203.0.113.7` | La `ipOrigen` del evento es distinta de 203.0.113.7: el backend usa siempre la IP de la conexión | SEG-002-15, D-72 de la 001 |
| ESC-299 e `@abuso` | Perfil web; última prueba del trabajo `web-e2e`; umbral de prueba 20 (plan §9.3) | La secuencia de la nota ESC-299 e | La 21.ª muestra "…Podrás preguntar de nuevo en 30 s." (429 del backend); selector habilitado; 0 reenvíos; a los 30 s, "Ya puedes preguntar de nuevo." y la nueva pregunta responde COMP-03 | SEG-002-16, COMP-002-12, D-72 de la 001 |

- **ESC-299 e:** `TASA_POR_IP_PRUEBA=20` y `api` recreado; 20 `POST` de COMP-03 por 5173 con `curl` en < 10 s y enseguida EST-001 pregunta COMP-03 en la interfaz; su `$upstream_status` es 429, no "-"; a los 30 s vuelve a preguntar. La prueba de 600 y 601 está en el backend (ESC-142 de la 001); el doble de 429 con 30 s, en ESC-238 b.
- ESC-299 d · retirado (D-72 de la 001)
- ESC-299 f · retirado (D-72 de la 001)

## CI y cadena de suministro

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-297 `@operacion` | Pull request que agrega en `frontend/src/` (a) `dangerouslySetInnerHTML`, (b) `console.log`, (c) `localStorage`, (d) un comentario, (e) `eslint-disable`, (f) una prueba que falla, (g) cobertura < 80%, (h) `let` o `new Map()` de módulo, (i) `AbortSignal.timeout` | CI | El trabajo `frontend` falla en cada caso y el pull request no se puede integrar | D-002-24, D-002-32 |
| ESC-298 `@operacion` | Pull request que agrega (a) un paquete npm con una vulnerabilidad alta conocida, (b) una dependencia con versión `^1.0.0` | CI | El trabajo `frontend` falla en ambos casos | D-002-25 |
| ESC-298 c `@abuso` | Pull request cuyo `package-lock.json` tiene una entrada con `resolved` en un registro distinto de registry.npmjs.org, o sin `integrity` | CI | `lockfile-lint` falla y el trabajo `frontend` falla | D-002-30 |
| ESC-298 d `@abuso` | Pull request que agrega una dependencia exacta que no está en la lista de plan §1 | CI | `verificar-versiones.mjs` falla nombrando el paquete | D-002-30 |
| ESC-298 e `@abuso` | Pull request que agrega un paquete cuyo `postinstall` crea el archivo `/tmp/postinstall-002` | CI | Tras `npm ci --ignore-scripts`, `/tmp/postinstall-002` no existe; el trabajo falla por ESC-298 d | D-002-30 |
| ESC-298 f `@operacion` | Pull request que cambia `.nvmrc` a una versión distinta de la de la imagen `node:24-alpine` fijada | CI | `verificar-versiones.mjs` falla | D-002-30 |
