# Plan técnico · 002 Interfaz web

> Cada regla técnica conserva el ID de `spec.md` y cita la D de [`decisiones.md`](decisiones.md) que la motiva. El contrato es el de [`constitution.md`](../../constitution.md) §2; las reglas generales de código y de Git, las de [`AGENTS.md`](../../AGENTS.md). TDD obligatorio también en el frontend (skill `tdd`): ninguna línea de `frontend/src/` sin una prueba en rojo antes.

## 1. Arquitectura y componentes

Aplicación de una sola página, sin estado global ni enrutador: estado local de React y `fetch` (D-002-01). El backend no cambia: su tasa por IP (D-72 de la 001) se especifica y prueba allí; esta spec verifica por el perfil `web` que el 429 llega a la interfaz con su `Retry-After` (CA-002-15).

```
frontend/
  package.json · package-lock.json · .nvmrc (versión exacta) · .npmrc · tsconfig.json · vite.config.ts · eslint.config.js
  playwright.config.ts · components.json (shadcn) · index.html · Dockerfile
  nginx/default.conf · nginx/seguridad.conf (generado en el build)
  scripts/
    generar-cabeceras.mjs         Escribe nginx/seguridad.conf desde seguridad/cabeceras.mjs
    verificar-build.mjs           SEG-002-03, SEG-002-10, SEG-002-11, RNF-002-04 sobre dist/
    verificar-sin-comentarios.mjs Sin comentarios ni `eslint-disable` en src/ y e2e/ (D-002-24)
    verificar-versiones.mjs       Versiones exactas y lista cerrada de §1 (D-002-25, D-002-30)
  seguridad/cabeceras.mjs         Única fuente de las cabeceras de producción: la usan nginx y `vite preview`
  seguridad/cabeceras-dev.mjs     Política de contenido del modo de desarrollo (§4.9)
  src/
    main.tsx · App.tsx
    api/contrato.ts               Tipos idénticos a constitution.md §2 (único archivo de tipos del contrato)
    api/validarContrato.ts        COMP-002-08
    api/preguntar.ts              Petición, tiempo de espera, cancelación y clasificación (§4.1, §4.2)
    api/salud.ts                  COMP-002-09
    dominio/identidades.ts        Lista cerrada de 8 usuarios (D-002-02)
    dominio/resultado.ts          Unión discriminada de resultados de un turno
    dominio/formato.ts            Tokens, Retry-After, espera, UUID v7 (D-002-08, D-002-10, D-002-20)
    dominio/textos.ts             Textos fijos de spec §5 y textos de la 001 que se comparan
    dominio/preguntasEjemplo.ts   D-002-22
    hooks/useConversacion.ts      Historial, turno pendiente, cancelación, salida de la página (D-002-04, D-002-05, D-002-27)
    hooks/useEspera.ts            Espera de 429 por fecha de fin (D-002-10)
    hooks/useSalud.ts             Sondeo y visibilidad (D-002-12)
    componentes/                  Cabecera, AvisoDemo, SelectorIdentidad, IndicadorSalud, FormularioPregunta,
                                  Historial, Turno, TarjetaRespuesta, Metadatos, MensajeEstado,
                                  CodigoSoporte, Bienvenida, Anuncios, AvisoCambio, CuentaRegresiva
    components/ui/                Copias de shadcn/ui (lista cerrada en §5.4)
    estilos/tokens.css            Tokens de §5 con `@theme` de Tailwind 4
  src/**/*.test.ts(x)             Vitest + Testing Library
  e2e/                            Playwright Test: dobles del backend (`@dobles`) y sistema real (`@real`)
  evidencias/                     Capturas de la validación con Playwright MCP (§9.4)
```

- Nombres del dominio en español (AGENTS.md §3); infraestructura con su nombre convencional (`main.tsx`, `vite.config.ts`).
- Dependencias de ejecución permitidas: `react`, `react-dom`, `@fontsource-variable/plus-jakarta-sans`, `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`, `@radix-ui/react-slot`, `@radix-ui/react-label`.
- Dependencias de desarrollo (build y pruebas): `vite`, `@vitejs/plugin-react`, `typescript`, `tailwindcss`, `@tailwindcss/vite`, `vitest`, `@vitest/coverage-v8`, `jsdom`, `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom`, `@playwright/test`, `@axe-core/playwright`.
- Dependencias de desarrollo (calidad): `eslint`, `typescript-eslint`, `eslint-plugin-react`, `eslint-plugin-react-hooks`, `eslint-plugin-jsx-a11y`, `lockfile-lint`.
- Las dos listas son cerradas: `verificar-versiones.mjs` falla si `package.json` tiene otra dependencia directa o una versión con `^`, `~`, `*`, rango, URL o `file:` (D-002-30). Añadir una exige actualizar esta lista en el mismo commit.
- `.nvmrc` con la versión exacta `X.Y.Z` de Node de la imagen `node:24-alpine` fijada en §10; `verificar-versiones.mjs` compara ambas.

## 2. Flujo de procesamiento

1. **Carga:** `useSalud` comprueba `/api/salud`; mientras no hay respuesta, el indicador dice "Comprobando servicio…" y "Preguntar" está deshabilitado.
2. **Identidad:** el `<select>` nativo controlado entrega un ID; `identidades.ts` lo busca en la lista cerrada. Sin coincidencia → identidad nula (SEG-002-06). Cambio a un ID distinto del actual → `useConversacion.reiniciar()` (§4.4).
3. **Envío:** con identidad, servicio en línea, sin petición en curso, sin espera de 429 y pregunta válida (§4.3), se crea un turno pendiente, se vacía el campo y se llama a `preguntar()`.
4. **Clasificación:** `preguntar()` devuelve un `Resultado` (§4.2). Si la generación de la conversación cambió mientras esperaba, el resultado se descarta (D-002-05).
5. **Presentación:** el turno pendiente se sustituye por su resultado; `Anuncios` publica el texto correspondiente (§4.6); 400, 413 y 429 devuelven la pregunta al campo; red y 503 disparan una comprobación de salud.
6. **Salida:** `pagehide` vacía todo el estado; `pageshow` con `persisted` lo reinicia (§4.4, D-002-27).

## 3. Integraciones y datos

| Uso | Endpoint o fuente | Cuándo |
| --- | --- | --- |
| Preguntar | `POST /api/asistente/preguntar` (relativa, mismo origen) | Cada envío, "Reintentar" y "Volver a preguntar" |
| Salud | `GET /api/salud` (relativa, mismo origen) | Carga, cada 30 s con la pestaña visible, tras red o 503, "Comprobar ahora" |
| Preguntas de ejemplo | Copiadas en `preguntasEjemplo.ts` desde `preguntas_ejemplo.json` | Bienvenida con identidad |

- Las rutas son siempre relativas (D-002-13): en Compose las reenvía nginx a `api:8080` (§4.7); en desarrollo, el proxy de Vite (§4.9).
- Preguntas de ejemplo: estudiante "¿Cuál es la nota mínima para aprobar una asignatura?", "¿Cuál es mi promedio ponderado del periodo?", "¿Qué requisitos tiene la beca por rendimiento académico?"; coordinador "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?", "¿Cómo se calcula la nota definitiva de una asignatura?".

## 4. Reglas técnicas por componente

### 4.1 Petición (`api/preguntar.ts`)
- **SEG-002-09:** `fetch` con `method: "POST"`, `headers: {"Content-Type": "application/json"}`, `body: JSON.stringify({pregunta, usuario: {rol, id}})`, `credentials: "omit"`, `cache: "no-store"`, `redirect: "error"`, `referrerPolicy: "no-referrer"`. Ningún otro encabezado.
- **COMP-002-11 (D-002-11):** un `AbortController` propio por petición, abortado por `setTimeout(35000)` o por la señal de la conversación; el temporizador se limpia al terminar. No se usa `AbortSignal.timeout` (el reloj falso no lo controla). Aborto por tiempo → `red`; por cambio de identidad → `descartado` (no se presenta).
- `X-Correlation-Id` se lee de `response.headers` y se guarda solo si cumple `^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$` (D-002-08).
- El cuerpo de un 4xx o 5xx distinto de 403 **no se lee** (SEG-002-07).

### 4.2 Clasificación (`dominio/resultado.ts`)
Unión discriminada `Resultado`, evaluada en este orden:

| Condición | `tipo` | Presentación |
| --- | --- | --- |
| `fetch` rechaza o tiempo agotado | `red` | Texto "Red o tiempo agotado", "Reintentar", sin código |
| 200 con cuerpo que no cumple el contrato | `ilegible` | Texto "Respuesta ilegible", "Reintentar", código |
| 200 y `respuesta` === texto de COMP-03 de la 001 | `sinInformacion` | Mensaje informativo, sugerencia, tokens |
| 200 y `respuesta` === `TEXTO_NO_VERIFICABLE` | `noVerificable` | Aviso, "Volver a preguntar", tokens |
| 200 | `respuesta` | COMP-002-02 |
| 403 con cuerpo del contrato | `negativa` | COMP-002-04: título y texto, sin metadatos |
| 403 con cuerpo que no cumple el contrato | `ilegible` | Igual que arriba |
| 400 · 413 · 415 · 429 · 503 | `error` con su código | COMP-002-06, COMP-002-12 |
| Cualquier otro código | `error` `inesperado` | "500 y otros códigos", "Reintentar", código |

- **COMP-002-08 (`validarContrato`):** `respuesta` string con algún carácter distinto de espacio (fallar cerrado: un texto vacío es `ilegible`); `citas` arreglo de objetos con `documento` y `seccion` string; `usoDatosEstudiante` boolean; `tokens` con `Number.isInteger` y ≥ 0; `costoUsd` número finito ≥ 0. Campos extra se ignoran. Un JSON inválido es `ilegible`.
- **SEG-002-18 (D-002-33, D-002-38):** `Resultado` solo lleva `texto`, `usoDatosEstudiante` y `tokens` (salvo `negativa`, sin `tokens`); `citas` y `costoUsd` se validan y se descartan en `validarContrato`, así ningún componente puede pintarlos.
- Las comparaciones con COMP-03 y `TEXTO_NO_VERIFICABLE` son exactas, byte a byte, con los textos de SPEC-001 §5 copiados en `textos.ts`.
- El 429 solo viene de la tasa por IP del backend (30, 60 o 120 s; D-72 de la 001); la interfaz no depende de su origen (D-002-10).

### 4.3 Pregunta (`FormularioPregunta`)
- **D-002-21:** longitud = `pregunta.length` (unidades UTF-16, igual que `string.Length` del backend). Válida si `pregunta.trim() !== ""` y `length <= 1000`. Se envía sin recortar.
- `<textarea>` controlado con `maxLength` sin fijar (para poder mostrar el exceso); `aria-invalid="true"` y mensaje "La pregunta supera los 1000 caracteres." asociado por `aria-describedby` cuando `length > 1000`.
- `autocomplete="off"` en el `<form>`, el selector y el campo (SEG-002-04, SEG-002-12); el `<form>` usa `onSubmit` con `preventDefault` y nunca navega.
- Enter sin modificadores envía (`preventDefault`); Mayús+Enter inserta salto de línea; durante la composición IME (`isComposing`) Enter no envía.

### 4.4 Identidad, historial y espera (`identidades.ts`, `useConversacion`, `useEspera`)
- Lista cerrada, en este orden y en dos `<optgroup>` ("Estudiantes", "Coordinadores"): EST-001…EST-006 con rol `estudiante`; COORD-01 y COORD-02 con rol `coordinador`. Etiqueta `"<ID> · <rol>"` (D-002-03).
- La app no lee `location.search`, `location.hash` ni `document.referrer` (SEG-002-06).
- `reiniciar()`: aborta el `AbortController` de la conversación, incrementa la generación, vacía los turnos y la región de anuncios, conserva la espera de 429 y, si había una identidad anterior, muestra `AvisoCambio` (`role="status"`, sin botones) con el texto de spec §5 y, con espera pendiente, la frase de la espera (COMP-002-13, D-002-10). Elegir el mismo ID no reinicia.
- Turnos en un arreglo inmutable; al agregar el 51.º se elimina el más antiguo (D-002-04). Ningún estado se escribe en almacenamiento del navegador.
- "Reintentar" y "Volver a preguntar" reenvían la pregunta del turno con la identidad actual, que es la misma porque un cambio de identidad borra el turno.
- **SEG-002-12 (D-002-27):** `window.addEventListener("pagehide", …)` deja identidad nula, 0 turnos, campo vacío, anuncios vacíos y aborta la petición; `pageshow` con `event.persisted === true` repite el reinicio sin aviso. Sin `unload` ni `beforeunload`. La espera de 429 se conserva (vive en memoria y caduca por fecha).
- **COMP-002-12 (`useEspera`, D-002-10):** `Retry-After` se acepta si cumple `^\d+$` y su valor está entre 1 y 3600; si no, 60. Guarda `fin = Date.now() + n × 1000`; cada 1 s (`setTimeout`) recalcula `ceil((fin − ahora) / 1000)`, así una pestaña oculta muestra el valor correcto al volver.
- `CuentaRegresiva`: visible con `aria-live="off"`; formato `<n> s` si n ≤ 60, `mm:ss` si n > 60. Al llegar a 0 habilita "Preguntar" (si se cumplen las demás condiciones de §2.3), publica "Ya puedes preguntar de nuevo." en `Anuncios` y no envía nada. El selector nunca se deshabilita por la espera.

### 4.5 Salud (`useSalud`)
- **COMP-002-09 (D-002-12):** `GET` con `AbortController` y `setTimeout(5000)`, `cache: "no-store"`. En línea si y solo si 200 y el JSON tiene `estado === "ok"`.
- Sondeo cada 30 000 ms mientras `document.visibilityState === "visible"`; al volver a visible (`visibilitychange`), comprobación inmediata. Una comprobación en curso no se duplica.
- Disparadores extra: resultado `red`, error 503 y "Comprobar ahora".
- Servicio no disponible: aviso con "Comprobar ahora" bajo la cabecera, indicador "Servicio no disponible" y "Preguntar" deshabilitado.

### 4.6 Accesibilidad
- `<html lang="es">`; `<title>` "TutorFlow · Asistente académico"; enlace "Saltar a la pregunta" como primer elemento enfocable.
- Puntos de referencia: `header` (marca "TutorFlow" en texto, indicador de salud, selector, aviso de demostración), `main` con el historial (`<ol aria-label="Conversación">`) y el formulario.
- `Anuncios`: un nodo visualmente oculto con `role="status"` y `aria-live="polite"` que recibe, en orden, "Consultando al asistente…", el texto mostrado del resultado, el aviso de cambio de identidad y "Ya puedes preguntar de nuevo.". Los errores se renderizan con `role="alert"`; el de 429 se anuncia una vez al inicio.
- Orden de tabulación (con identidad, sin turnos y en línea): "Saltar a la pregunta", "Usuario de prueba", "Nueva conversación" (en la cabecera), las preguntas de ejemplo, "Tu pregunta", "Preguntar". Con turnos, sus botones ("Reintentar", "Volver a preguntar", "Copiar código") van entre la cabecera y "Tu pregunta"; "Comprobar ahora", tras el selector.
- Foco: tras enviar y tras cada resultado queda en "Tu pregunta"; tras cambiar de identidad, en el selector. No hay trampas de foco ni `tabindex` positivo.
- `:focus-visible`: `outline: 2px solid #2b1d19; outline-offset: 2px`; sobre superficies #2b1d19, `outline-color: #ff6a13`. Los campos suman el halo decorativo `0 0 0 1.5px #ff6a13` (D-002-18).
- La barra del formulario fija en móvil define `scroll-padding-bottom` igual a su altura + 16 px (WCAG 2.4.11).
- Sin lista de fuentes (D-002-38). Iconos `aria-hidden="true"` y siempre acompañados de texto.

### 4.7 Seguridad del cliente y servidor web
- **SEG-002-01 (D-002-07):** el texto se renderiza solo como hijo de texto de React; contenedor con `white-space: pre-line` y `overflow-wrap: anywhere`. No se usan bibliotecas de Markdown ni de autoenlace.
- **SEG-002-13 (D-002-31):** la respuesta, la pregunta del turno y la región de anuncios van en `<bdi dir="auto">` con `unicode-bidi: isolate`; el texto se conserva sin filtrar.
- **SEG-002-02 (D-002-16):** cabeceras de `seguridad/cabeceras.mjs`, idénticas en nginx y en `vite preview`:
  - `Content-Security-Policy: default-src 'none'; script-src 'self'; style-src 'self'; font-src 'self'; img-src 'self'; connect-src 'self'; manifest-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'; object-src 'none'; require-trusted-types-for 'script'; trusted-types 'none'`
  - `X-Content-Type-Options: nosniff` · `X-Frame-Options: DENY` · `Referrer-Policy: no-referrer` · `Permissions-Policy: camera=(), microphone=(), geolocation=()` · `Cross-Origin-Opener-Policy: same-origin` · `Cross-Origin-Resource-Policy: same-origin`. Sin `report-to` ni `report-uri`.
- **SEG-002-03 (D-002-13):** `vite.config.ts` falla con "Variable VITE_ no permitida: <nombre>" si existe cualquier `VITE_*` en el entorno; ninguna variable entra al build.
- **SEG-002-10 (`verificar-build.mjs`):** `vite build` con `build.sourcemap: false`; `dist/` con 0 archivos `.map` y 0 apariciones de la lista prohibida:
  - cada valor `nombre` de `mock-api/datos.json` (personas y programas, leídos por el script), `ING-SIS`, `ADM` como palabra, `consultar_`, `api:8080`, `mock-api`, `ApiAcademica`;
  - `LLM_API_KEY`, `sk-ant-`, `sk-or-`, `gsk_`, `AIza`, `sk-proj-` y `q10` sin distinguir mayúsculas.
- **nginx (`nginx/default.conf`, D-002-28, D-002-29, D-002-14):**
  - `resolver 127.0.0.11 valid=10s ipv6=off;` y `set $api api;` en el `server`: el nombre del servicio se resuelve en cada petición (RNF-002-07).
  - **SEG-002-16 (D-002-29):** sin límite en el proxy: `default.conf` no tiene `limit_req`, `limit_req_zone`, `include` de límites, `location @limite` ni `error_page 429`. El único límite es la tasa del backend (D-72 de la 001), cuyo 429 pasa con su `Retry-After`.
  - `location = /api/asistente/preguntar { limit_except POST { deny all; } proxy_pass http://$api:8080/api/asistente/preguntar; }` y `location = /api/salud { limit_except GET { deny all; } proxy_pass http://$api:8080/api/salud; }`: URI fija, sin `$args`, `$is_args` ni `$request_uri`.
  - En ambas (F-04, D-002-14): `proxy_set_header X-Forwarded-For "";`, `proxy_set_header Forwarded "";`, `proxy_set_header X-Real-IP "";`: nginx no reenvía cabeceras de IP del cliente (nunca `$proxy_add_x_forwarded_for` ni `$remote_addr`) y el backend, que las ignora, ve la IP de nginx.
  - En ambas: `proxy_connect_timeout 5s`, `proxy_read_timeout 40s`, `client_max_body_size 64k` (un cuerpo de 17 KB llega al backend y su 413 se conserva), sin `proxy_cache`, `add_header Cache-Control "no-store" always`.
  - `proxy_hide_header` de `Cache-Control`, `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Server` y `X-Powered-By`: cada cabecera sale 1 vez, la de nginx. `X-Correlation-Id`, `Retry-After` y `Content-Type` pasan.
  - `location ~* ^/api(/|$) { return 404; }` (rutas de `/api` en cualquier mayúscula; las `location =` ganan). `location /assets/` → `Cache-Control: public, max-age=31536000, immutable`. `location /` → `try_files $uri /index.html` con `Cache-Control: no-store`.
  - Cada `location` incluye `seguridad.conf` (las cabeceras de `add_header` no se heredan). `server_tokens off`. TRACE lo rechaza nginx con 405.
  - `access_log` con el formato `$time_iso8601 $status $request_method $uri $upstream_status $body_bytes_sent $request_time`: sin IP, consulta, cuerpo ni encabezados; `$upstream_status` "-" prueba que la petición no llegó al backend.

### 4.8 Reglas de código (`eslint.config.js`, D-002-24)
- `typescript-eslint` `strict-type-checked`; `tsconfig` con `strict`, `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`.
- `no-console`, `react/no-danger`, `no-eval`, `no-implied-eval`, `no-new-func`: error.
- `no-restricted-properties` y `no-restricted-globals`: `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `document.cookie`, `localStorage`, `sessionStorage`, `indexedDB`, `caches`, `navigator.serviceWorker`, `BroadcastChannel`, `AbortSignal.timeout`.
- `no-restricted-syntax` (D-002-32): en el nivel de módulo de `src/`, sin `let` ni `var` y sin `new Map`, `new Set`, `new WeakMap` ni arreglos u objetos que luego se muten; las constantes de módulo son literales congelados con `as const`.
- `complexity: 5`, `max-depth: 2`, `max-lines-per-function: 20` (sin líneas en blanco), `jsx-a11y/recommended`: error.
- Sin comentarios: `verificar-sin-comentarios.mjs` recorre `src/` y `e2e/` con el escáner de TypeScript y falla ante cualquier comentario.
- Sin `eslint-disable` en ningún archivo (el mismo script lo verifica).

### 4.9 Modo de desarrollo (D-002-34)
- `npm run dev`: Vite en `server.host: "127.0.0.1"`, `server.port: 5173`, `strictPort: true` (si el perfil `web` ocupa 5173, Vite termina con error; no conviven).
- `server.proxy` solo con `/api/asistente/preguntar` y `/api/salud` → `http://localhost:8080`, sin `xfwd`; otra ruta de `/api` no se reenvía. El backend ve la IP local; `Proxy:IpsConfiables` no se configura.
- Cabeceras de `seguridad/cabeceras-dev.mjs`: la política de producción con `script-src 'self' 'unsafe-inline'` (preámbulo de React Refresh), `style-src 'self' 'unsafe-inline'` (estilos de HMR), `connect-src 'self' ws://127.0.0.1:5173` y sin Trusted Types; el resto igual.
- La política estricta se valida solo con el build (`vite preview` y nginx, ESC-282 y ESC-285); ninguna prueba ni trabajo del CI usa `npm run dev`.

## 5. Diseño

### 5.1 Tokens (`estilos/tokens.css`)
| Token | Valor | Uso |
| --- | --- | --- |
| `fondo` · `superficie` | #f7f7f7 · #ffffff | Página · tarjetas y campos |
| `primario` · `primario-hover` | #ff6a13 · #e85d0c | Botón principal, acentos, marcador de carga |
| `tinta` · `tinta-900` | #2b1d19 · #120c0b | Texto y texto sobre naranja · burbuja de la pregunta |
| `tinta-200` | #9d9795 | Solo decorativo (separadores), nunca texto ni borde de campo |
| `crema` · `crema-600` | #fff3ec · #e8ddd7 | Insignia, aviso de demostración, aviso de cambio · borde de la insignia |
| `borde` · `borde-2` | #ece7e5 · #e3dcd9 | Bordes de tarjetas (decorativos) |
| `borde-campo` | #7a7270 | Borde del selector y del campo (≥ 3:1) |
| `texto-secundario` | #6b6260 | Ayuda, contador, metadatos, marcador de posición, cuenta regresiva |
| `naranja-texto` | #c2410c | Único naranja permitido como texto (título de bienvenida, enlace "Saltar a la pregunta") |
| `exito` · `error` | #1f9d5c · #b42318 | Punto de "Servicio en línea" (no texto) · icono, borde y título de errores |

- Radios: `tarjeta` 20 px, `campo` 12 px, `pildora` 999 px (botones, insignias, preguntas de ejemplo).
- Sombras: `normal` `0 2px 8px #2b1d190f, 0 8px 24px #2b1d1914`; `destacada` `0 24px 48px -28px #2b1d1973`; halo de campo `0 0 0 1.5px #ff6a13`.
- Botón principal: fondo `primario`, texto `tinta`, peso 500, relleno 15 px / 26 px, alto mínimo 44 px; hover `primario-hover`; presionado `primario-hover` con `scale(0.98)` (D-002-18). Deshabilitado: fondo `borde`, texto `texto-secundario` (4.84:1).
- La pregunta del usuario: burbuja `tinta-900` con texto #f7f7f7. La respuesta: tarjeta `superficie`, radio 20, sombra `normal`.

### 5.2 Tipografía (D-002-17)
- `"Plus Jakarta Sans Variable", system-ui, sans-serif`, de `@fontsource-variable/plus-jakarta-sans` (subconjuntos `latin` y `latin-ext`), `font-display: swap`. Solo pesos 400 y 500.
- Escala (tamaño/interlínea en px): S 14/20, cuerpo 16/24, L 18/28; títulos 22/30, 28/36, 48/56 y 64/72. Campo de texto ≥ 16 px (evita el zoom de iOS).
- Uso: marca "TutorFlow" 22; título de bienvenida 28 (< 768 px), 48 (≥ 768 px) y 64 (≥ 1280 px); respuesta L en ≥ 768 px y cuerpo en móvil; metadatos e insignia S.

### 5.3 Movimiento y diseño responsivo (D-002-26)
- `transition-property: transform, background-color, box-shadow; transition-duration: 200ms`. Hover de botones y preguntas de ejemplo: `translateY(-2px)` y sombra `destacada`.
- `@media (prefers-reduced-motion: reduce)`: `transition-duration: 0ms`, sin `transform` en hover y `scrollIntoView({behavior: "auto"})`. El marcador de carga pasa de animado a estático con el texto "Consultando…".
- < 768 px: una columna; cabecera con la marca y el indicador en una fila y el selector debajo; formulario fijo abajo con `env(safe-area-inset-bottom)`.
- ≥ 768 px: columna centrada de 760 px; ≥ 1024 px: 880 px, cabecera en una fila.

### 5.4 Componentes shadcn/ui permitidos (D-002-16)
- `button`, `card`, `badge`, `textarea`, `label`, `alert`, `separator`, `skeleton`, copiados en `src/components/ui/` y ajustados a las reglas de §4.8.
- Prohibidos (inyectan `<style>` o portales con bloqueo de desplazamiento): `select`, `dialog`, `sheet`, `drawer`, `popover`, `dropdown-menu`, `toast`/`sonner`. El selector de identidad es un `<select>` nativo con los tokens.

## 6. Auditoría y observabilidad
- El cliente no registra nada: sin consola, sin analítica, sin telemetría (SEG-002-04, SEG-002-05).
- El contenedor web registra solo el formato de §4.7. La auditoría de cada pregunta es la del backend (CA-08 de la 001); por el perfil `web`, `ipOrigen` es la IP de nginx para todos los clientes (D-002-14).

## 7. Configuración
| Clave | Por defecto | Regla | Si es inválida |
| --- | --- | --- | --- |
| Variables `VITE_*` | Ninguna | Prohibidas | El build falla (SEG-002-03) |
| Puerto del servicio `web` | `127.0.0.1:5173` → 8080 del contenedor | Fijo en `docker-compose.yml` | — |
| Puerto del modo de desarrollo | `127.0.0.1:5173`, `strictPort` | Fijo en `vite.config.ts` | Vite termina con error si está ocupado |
| Límite de nginx | No existe (D-002-29) | — | — |
| Tasa del backend | 600 por minuto por IP, compartida por todos los clientes de la interfaz; bloqueo de 30, 60 y 120 s (D-72 de la 001) | `Limites__PreguntasPorMinutoPorIp` del servicio `api` (vacía = 600; 20 solo en ESC-299 e con `TASA_POR_IP_PRUEBA`) | El backend no arranca (001) |
| Tiempos | 35 s pregunta, 5 s salud, 30 s sondeo, 60 s `Retry-After` por defecto | Constantes con nombre en `dominio/` | — |

## 8. Datos de referencia

### 8.1 Contraste (WCAG 2.x, luminancia relativa L)
L calculada con `c ≤ 0.04045 ? c/12.92 : ((c+0.055)/1.055)^2.4` y `L = 0.2126 R + 0.7152 G + 0.0722 B`; contraste `(L1 + 0.05) / (L2 + 0.05)`.

| Color | L |
| --- | --- |
| #ffffff · #f7f7f7 · #fff3ec · #ece7e5 | 1.0000 · 0.9301 · 0.9142 · 0.8064 |
| #ff6a13 · #e85d0c · #c2410c | 0.3162 · 0.2501 · 0.1528 |
| #2b1d19 · #6b6260 · #7a7270 · #9d9795 | 0.0146 · 0.1271 · 0.1734 · 0.3147 |
| #b42318 · #1f9d5c | 0.1097 · 0.2518 |

| Par (primer plano sobre fondo) | Contraste | Mínimo | Uso |
| --- | --- | --- | --- |
| #2b1d19 sobre #ff6a13 | 0.36615 / 0.06463 = **5.67:1** | 4.5 | Texto del botón principal |
| #2b1d19 sobre #e85d0c | 0.30011 / 0.06463 = **4.64:1** | 4.5 | Botón en hover y presionado |
| #ffffff sobre #ff6a13 | 1.05 / 0.36615 = **2.87:1** ✘ | 4.5 | Prohibido |
| #ff6a13 sobre #f7f7f7 | 0.98011 / 0.36615 = **2.68:1** ✘ | 4.5 texto · 3 foco | Prohibido como texto y como foco |
| #2b1d19 sobre #f7f7f7 · #ffffff · #fff3ec | **15.17:1** · **16.25:1** · **14.92:1** | 4.5 | Texto principal, insignia, avisos y foco |
| #f7f7f7 sobre #120c0b | 0.98011 / 0.05416 = **18.10:1** | 4.5 | Burbuja de la pregunta |
| #c2410c sobre #f7f7f7 · #ffffff · #fff3ec | **4.83:1** · **5.18:1** · **4.76:1** | 4.5 | Naranja de texto |
| #6b6260 sobre #f7f7f7 · #ffffff · #fff3ec | **5.54:1** · **5.93:1** · **5.45:1** | 4.5 | Texto secundario y cuenta regresiva |
| #6b6260 sobre #ece7e5 | 0.85642 / 0.17706 = **4.84:1** | 4.5 | Botón deshabilitado |
| #7a7270 sobre #ffffff · #f7f7f7 | **4.70:1** · **4.39:1** | 3 | Borde de campos |
| #9d9795 sobre #f7f7f7 | 0.98011 / 0.36471 = **2.69:1** ✘ | 3 | Solo decorativo |
| #b42318 sobre #ffffff · #f7f7f7 | **6.57:1** · **6.14:1** | 4.5 | Título e icono de error |
| #1f9d5c sobre #ffffff · #f7f7f7 | **3.48:1** · **3.25:1** | 3 (no texto) | Punto de "Servicio en línea" |
| #ff6a13 sobre #2b1d19 | **5.67:1** | 3 | Foco sobre superficies oscuras |

- L de #120c0b: 0.2126 × 0.00605 + 0.7152 × 0.00368 + 0.0722 × 0.00335 = 0.00416.

### 8.2 Cuerpos de los dobles del backend
- **ESC-223 (EST-001):** `{"respuesta":"Una asignatura se aprueba con una nota definitiva de 3.0.","citas":[{"documento":"reglamento_academico_2025.md","seccion":"Artículo 5"}],"usoDatosEstudiante":false,"tokens":812,"costoUsd":0.000412}`.
- **ESC-224 (EST-001):** `respuesta` = "Tu promedio es 4.39.\nCálculo Integral: 4.5\nProgramación Orientada a Objetos: 4.8\nBases de Datos: 4.1\nInglés III: 4.0"; `citas` Sec. 4 y Sec. 3 en ese orden; `usoDatosEstudiante` true; `tokens` 1530; `costoUsd` 0.0015. Los valores son los de CA-14 de la 001.
- **ESC-225 (COORD-02):** `respuesta` = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3)."; `citas` Art. 5 y `{"documento":"Datos del programa","seccion":"ADM"}`; `usoDatosEstudiante` true; `tokens` 2048; `costoUsd` 1.234567.
- **Negativas (ESC-231, ESC-232):** `TEXTO_NEGATIVA` del rol, `citas` [Art. 22], `usoDatosEstudiante` false, `tokens` 0, `costoUsd` 0 (D-07 de la 001).
- **Errores (ESC-235…243):** `Content-Type: application/problem+json`, cuerpo `{"title":"Bad Request","status":<código>,"detail":"System.Text.Json falló en TutorFlow.Api","traceId":"019a5f3e-7c1d-7b2a-9e4f-0123456789ab"}` y `X-Correlation-Id: 019a5f3e-7c1d-7b2a-9e4f-0123456789ab`.
- **Bidi (ESC-276 b):** `respuesta` = "Nota: ‮0.5 es la nota‬ final​." (con U+202E, U+202C y U+200B); `citas[0].seccion` = "Artículo ‮5"; tokens 812, costo 0.000412.
- **50 citas (ESC-273 b):** el cuerpo ESC-223 con `citas` = 47 objetos `{"documento":"DOC-MARCA-NN.md","seccion":"SEC-MARCA-NN"}` (NN 01…47) más reglamento_academico_2025.md · Artículo 22, evaluacion_y_calificaciones.md · Sección 4 y Datos del estudiante · Notas.

### 8.3 Formatos esperados
| Entrada | Salida |
| --- | --- |
| `tokens` 812, `costoUsd` 0.000412 | "812 tokens"; "US$" y "0.000412" 0 veces en el DOM |
| `tokens` 1530 · 2048 · 0 (200) | "1530 tokens" · "2048 tokens" · "0 tokens" |
| 403 con `tokens` 0 y `costoUsd` 0 | Sin metadatos: 0 "tokens" y 0 "US$" en el turno |
| Cita `{documento: "reglamento_academico_2025.md", seccion: "Artículo 22"}` | Nada: 0 apariciones de ambos textos en el DOM |
| `Retry-After` "3" · "12" · "1" · "60" | 3 s · 12 s · 1 s · 60 s; texto "3 s", "12 s", "1 s", "60 s" |
| `Retry-After` "61" · "120" · "600" · "3600" | 61 s, 120 s, 600 s, 3600 s; texto "01:01", "02:00", "10:00", "60:00" |
| `Retry-After` de la tasa del backend "30" · "60" · "120" | "30 s" · "60 s" · "02:00" (D-72 de la 001) |
| `Retry-After` ausente · "0" · "3601" · "-5" · "1e9" · "abc" · fecha HTTP | 60 s; texto "60 s" |
| `X-Correlation-Id` "019a5f3e-7c1d-7b2a-9e4f-0123456789ab" | Se muestra |
| `X-Correlation-Id` en mayúsculas · versión 4 · ausente · `<b>x</b>` | No se muestra |

## 9. Plan de QA

### 9.1 Niveles de prueba
| Nivel | Qué valida | Herramienta | Corre en |
| --- | --- | --- | --- |
| Unidad | `validarContrato`, clasificación, formatos, `Retry-After`, espera, UUID v7, identidades | Vitest | CI `frontend` |
| Componente | Estados de cada turno, formulario, selector, salud, anuncios, foco, espera, salida de la página, tiempos | Vitest + Testing Library + `user-event`, `fetch` simulado con `vi.stubGlobal`, `vi.useFakeTimers()` y `document.visibilityState` simulado | CI `frontend` |
| Navegador con dobles (`@dobles`) | Flujos, estados, XSS, bidi, privacidad, CSP, axe, responsivo, movimiento, caché de ida y vuelta | Playwright Test sobre `vite preview` con `page.route`, en Chromium, Firefox y WebKit salvo D-002-37 | CI `frontend` |
| Sistema real (`@real`) | Proxy, IP del cliente, cabeceras, límite, rutas, 403 y COMP-03 sin LLM, salud, resolver, perfil | Playwright Test y `curl` contra `docker compose --profile web up` en `127.0.0.1:5173` y `localhost:8080` | CI `web-e2e` |
| Build | Secretos, datos internos, "q10", tamaño, `VITE_*`, comentarios, dependencias | Scripts de `frontend/scripts/`, `lockfile-lint`, `npm audit` | CI `frontend` |
| Exploratoria | Recorrido visual de los estados a 360 y 1280 px | Playwright MCP (`.mcp.json`) | Local, antes de la revisión (§9.4) |

- Cada prueba cita su ESC y su CA en el título (`test("ESC-235 CA-002-05 …")`). `qa-calidad` verifica que cada ESC-2NN y cada letra tengan al menos una prueba.
- Cobertura de Vitest ≥ 80% de líneas en `src/` (sin `main.tsx`).

### 9.2 Matriz de verificación
| CA | Nivel | Escenarios | Umbral |
| --- | --- | --- | --- |
| CA-002-01 | Componente, navegador con dobles | ESC-200…206 | 100% en cada corrida; cuerpo enviado idéntico byte a byte; 0 campos de texto extra |
| CA-002-02 | Componente, navegador con dobles | ESC-215…222 | 100%; número exacto de peticiones por caso |
| CA-002-03 | Componente, navegador con dobles | ESC-223…229, ESC-273 b | 100%; textos y tokens idénticos; 0 apariciones de "US$", del costo, de cada `documento` y de cada `seccion` en el DOM, también con 50 citas; 0 px de desplazamiento horizontal con 20 000 caracteres |
| CA-002-04 | Componente, navegador con dobles | ESC-230…234 | 100%; 0 "Código de soporte" y 0 "Reintentar" en informativos; 0 "tokens" y 0 "US$" en el 403 |
| CA-002-05 | Unidad, componente con reloj falso, navegador con dobles | ESC-235…249, ESC-214 | 100%; 0 apariciones de `detail`, `title` o "System." del cuerpo; habilitación exacta al segundo; 0 reenvíos tras la espera |
| CA-002-06 | Componente con reloj falso, navegador con dobles | ESC-250…256 | 100%; sondeo a 30 s ± 1 s; 0 peticiones con la pestaña oculta |
| CA-002-07 | Componente, navegador con dobles (3 motores en ESC-211 b) | ESC-207…214 | 100%; 0 textos de respuestas borradas en el DOM 5 s después y 0 en la instantánea de memoria |
| CA-002-08 | Navegador con dobles (3 motores) | ESC-270…278 | 100%; 0 `img`/`script`/`a` en la tarjeta; `window.__xss` indefinido; 100% de textos del servidor aislados |
| CA-002-09 | Navegador con dobles + axe | ESC-257…260 | 0 violaciones de `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa` en cada estado y ancho |
| CA-002-10 | Navegador con dobles | ESC-261…265 | 100%; contorno de 2 px en el 100% de los controles enfocables; orden de Tab igual al de §4.6 |
| CA-002-11 | Navegador con dobles | ESC-266…269 | `scrollWidth - clientWidth` = 0 en 4 anchos; duración 200 ms o 0 ms exactos |
| CA-002-12 | Navegador con dobles, build, sistema real | ESC-279…289 | 0 entradas de almacenamiento; 0 mensajes de consola de la app; 100% mismo origen; 0 violaciones de CSP salvo ESC-285 b; 0 apariciones prohibidas en `dist/`; cada cabecera 1 vez |
| CA-002-13 | Sistema real | ESC-290…292 (y ESC-201, 205, 206, 231 con dobles) | 100% en cada corrida; textos idénticos a SPEC-001 §5; 0 metadatos en las negativas y 0 "US$" |
| CA-002-14 | Sistema real, CI | ESC-293, ESC-296…298 | 100%; cada cambio de ESC-297 y ESC-298 hace fallar el trabajo indicado; salud en ≤ 30 s tras recrear `api` |
| CA-002-15 | Sistema real | ESC-294, ESC-295, ESC-299 | 100% en cada corrida; `ipOrigen` igual en todos los clientes y ≠ las IP falsas; 0 × 429 en 30 preguntas con el defecto y 0 `limit_req` en `nginx -T`; con el umbral de prueba en 20, la 21.ª → "…en 30 s." y cuenta exacta; 0 reenvíos |
| RNF-002-03 | Navegador con dobles (solo Chromium) | ESC-216 | "Consultando…" visible ≤ 200 ms en p95 de 20 envíos de una corrida |
| RNF-002-04 | Build | ESC-284 | JS + CSS de la carga inicial ≤ 250 KB en gzip |
| RNF-002-07 | Sistema real | ESC-296 c, d | 200 en `/api/salud` por 5173 en ≤ 30 s |

### 9.3 Casos límite, dobles y criterios de salida
- Dobles: `page.route("**/api/asistente/preguntar")` y `page.route("**/api/salud")` con código, cuerpo y encabezados de §8.2; demora configurable; `route.abort("failed")` para la red. En Vitest, `fetch` simulado y `vi.useFakeTimers()` para 35 s, 5 s, 30 s y `Retry-After`; `page.clock` en Playwright para las esperas de 429.
- Sistema real sin `LLM_API_KEY`: el 403 del filtro previo y COMP-03 sin sección relevante no llaman al LLM (CA-04 y CA-06 de la 001), por eso son deterministas.
- **Orden del trabajo `web-e2e` (D-002-29, D-002-37):** las pruebas `@real` que preguntan por 5173 corren en serie, sin pausas; ESC-299 a empieza con `docker compose restart api` (estado de la tasa vacío) y espera la salud; ESC-295 m corre con la configuración por defecto.
- Última prueba: ESC-299 e, con `Limites__PreguntasPorMinutoPorIp=20` solo en el entorno de esa prueba: `TASA_POR_IP_PRUEBA=20 docker compose up -d --no-deps --force-recreate api` (reinicia `api` con la variable) y preguntas COMP-03 sin LLM. La prueba de 600 y 601 vive en el backend (ESC-142 de la 001).
- `api` recibe `Limites__PreguntasPorMinutoPorIp: "${TASA_POR_IP_PRUEBA:-}"`: sin la variable queda vacía y vale 600 (plan §13.2 de la 001); no es un override ni un archivo nuevo.
- Las pruebas afirman "la misma `ipOrigen` en todos los clientes" y "distinta de la IP falsa", nunca una IP concreta: pasan en Linux y en Docker Desktop (D-002-14).
- Límites: 1000 y 1001 caracteres, emoji (2 unidades), 50 y 51 turnos, `Retry-After` 1, 60, 61, 3600 y 3601, respuesta de 20 000 caracteres sin espacios, cuerpo de 17 KB, 30 preguntas en ráfaga sin 429, 20 y 21 preguntas con el umbral de prueba del backend, 50 citas.
- Criterios de salida: todos los trabajos del CI en verde; `seguridad` y `qa-calidad` sin hallazgos altos ni críticos; evidencias de §9.4 completas; `REPORTE.md` actualizado.

### 9.4 Validación con Playwright MCP
- Quién: `frontend-react` al terminar y `qa-calidad` en la revisión, con el servidor MCP de `.mcp.json` en modo headless.
- Sobre `docker compose --profile web up` en `http://127.0.0.1:5173` (y, para los estados con LLM, con `LLM_API_KEY` u Ollama), recorre: bienvenida sin identidad, cargando, respuesta, sin información, negativa 403, cambio de identidad, espera de 429, servicio no disponible (`docker compose stop api`) y error de red, a 360 y 1280 px.
- Por estado: captura en `frontend/evidencias/<estado>-<ancho>.png` (≤ 300 KB), revisión de foco con Tab y de contraste. Los hallazgos se convierten en pruebas de Playwright Test antes de corregirse (TDD).
- `REPORTE.md` lista las capturas y el resultado de cada estado.

## 10. Docker y Compose (D-002-14, D-002-15)
- `frontend/Dockerfile`, contexto la raíz del repositorio:
  - Etapa `construccion`: `node:24-alpine` fijada por digest; `npm ci --ignore-scripts` y `npm rebuild esbuild`; `npm run build` (`vite build`, `generar-cabeceras.mjs` y `verificar-build.mjs`, que lee `mock-api/datos.json`). Sin `ARG` ni `ENV` `VITE_*`.
  - Etapa final: `nginxinc/nginx-unprivileged` (alpine) fijada por digest; copia `dist/`, `default.conf` y `seguridad.conf`; `USER 101`; `EXPOSE 8080`.
- Sin overrides de Compose: no hay `frontend/compose.limite-web.yml` ni `frontend/nginx/limite/` (D-002-29).
- Red: `web` se une a la red puente existente de la 001 (`publica`), la misma de `api`; sin redes nuevas, subredes, IP fijas, alias ni overrides de Compose para la IP (D-72 de la 001, INT-030).
- Servicio `web`: `profiles: [web]`; `build` con `dockerfile: frontend/Dockerfile`; `ports: ["127.0.0.1:5173:8080"]`; `networks: [publica]`; sin `depends_on` (el `resolver` tolera que `api` arranque después).
  - `read_only: true`, `tmpfs: [/tmp]`, `cap_drop: [ALL]`, `security_opt: [no-new-privileges:true]`, `restart: unless-stopped`; `healthcheck` con `wget -q -O /dev/null http://127.0.0.1:8080/`.
- Servicio `api`: sin cambios por la 002; `mock-api` y `mock-api-dev` tampoco cambian. El backend ve la IP de nginx en toda pregunta por 5173 (D-002-14).
- `.dockerignore` excluye `frontend/node_modules`, `frontend/dist`, `frontend/evidencias` y `frontend/test-results`.

## 11. CI (`.github/workflows/ci.yml`)
- **`frontend`** (`working-directory: frontend`), acciones fijadas por SHA, Node desde `.nvmrc`, caché de npm por `package-lock.json`:
  1. `npm ci --ignore-scripts` → `npm rebuild esbuild` → `npx lockfile-lint --path package-lock.json --type npm --allowed-hosts npm --validate-https --validate-integrity` → `npm audit signatures`.
  2. `npm run lint` → `npm run typecheck` → `node scripts/verificar-sin-comentarios.mjs` → `node scripts/verificar-versiones.mjs`.
  3. `npm run test:cobertura` (Vitest, umbral 80% de líneas).
  4. `npm audit --audit-level=high` (D-002-25).
  5. `npm run build` (incluye `verificar-build.mjs`).
  6. `npx playwright install --with-deps chromium firefox webkit` → `npm run e2e:dobles`.
- **`web-e2e`:** `docker compose --profile web up -d --build`; espera 8080 y 5173 (60 s); `npm ci --ignore-scripts`; `npx playwright install --with-deps chromium`; `npm run e2e:real` en el orden de §9.3; siempre `docker compose --profile web down -v`.
  - `curl -sI 127.0.0.1:5173/` compara las cabeceras con `seguridad/cabeceras.mjs`; `docker compose exec web nginx -T` sin `proxy_cache`, `$args`, `$request_uri`, `$proxy_add_x_forwarded_for`, `$remote_addr` en `proxy_set_header`, `limit_req` ni `@limite`; ESC-299 lee los eventos de `docker compose logs api`.
- **`docker`** (existente): agrega el paso "ESC-293 sin perfil no hay contenedor web": `test -z "$(docker compose ps -q web)"`.
- `limites-specs` (existente) cubre esta spec sin cambios.
