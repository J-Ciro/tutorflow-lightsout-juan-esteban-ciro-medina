# Decisiones · 002 Interfaz web

> Regla: cada decisión ocupa como máximo 3 líneas: decisión · por qué · estado. Una decisión que cambie permisos o el contrato lleva "requiere aprobación humana" y detiene el trabajo. Todo riesgo tiene una decisión.

Origen: D-002-01…D-002-26, propuestas de `arquitecto-spec` (2026-11-23); D-002-27…D-002-37, revisión de seguridad F-01…F-20 e INT-025; D-002-38, INT-028; D-002-14, D-002-35 y D-002-36 reescritas por INT-030. Ninguna cambia el contrato ni la matriz de acceso de [`constitution.md`](../../constitution.md).

Decisiones del humano: D-002-01, D-002-14 (INT-030), D-002-29 (INT-030), D-002-33 y D-002-38. D-002-35 y D-002-36 quedan sustituidas por D-72 de la 001 (INT-030).

## Decisiones

- **D-002-01** · Stack: Vite + React 19 + TypeScript `strict` + shadcn/ui (componentes copiados en el repo, sobre Radix UI y Tailwind CSS 4); regla ya aplicada en `.claude/agents/frontend-react.md`.
  Por qué: lo pide el requisito; shadcn no es una dependencia opaca sino código propio revisable. · Estado: aprobada por el humano (INT-025).
- **D-002-02** · Identidad por selector cerrado de los 8 usuarios de prueba, sin texto libre, sin preselección y con el rol tomado de la lista; aviso de demostración siempre visible.
  Por qué: el contrato no autentica (D-01 de la 001); evita IDs mal formados y que la app parezca un inicio de sesión; el backend decide. · Estado: vigente.
- **D-002-03** · Las opciones muestran solo "ID · rol", sin nombres ni programas. · Por qué: minimización (Ley 1581) y no duplicar en el cliente datos de la API académica. · Estado: vigente.
- **D-002-04** · Historial solo en memoria, últimos 50 turnos; se borra al cambiar de identidad, con "Nueva conversación", al recargar o al salir (D-002-27); 0 escrituras en almacenamiento del navegador.
  Por qué: minimización; evita mostrar en pantalla respuestas de una identidad bajo otra; 50 acota la memoria. · Estado: vigente.
- **D-002-05** · Cambiar de identidad cancela la petición en curso y descarta su resultado; vacía la región de anuncios y los códigos de soporte. · Por qué: nada de la identidad anterior debe quedar bajo la nueva (SEG-002-08). · Estado: vigente.
- **D-002-06** · Cada pregunta se envía sola, sin historial, y la ayuda del campo lo dice. · Por qué: el contrato no admite historial y la 001 lo deja fuera del alcance. · Estado: vigente.
- **D-002-07** · Todo texto del servidor se muestra como texto plano: sin HTML, sin Markdown y sin autoenlaces; se conservan los saltos de línea. · Por qué: XSS y enlaces engañosos en la salida del LLM; UX-02 de la 001 usa una línea por asignatura. · Estado: vigente.
- **D-002-08** · Errores con textos fijos de la interfaz; nunca se muestra el `ProblemDetails`; el código de soporte solo si `X-Correlation-Id` es un UUID v7 canónico. · Por qué: SEG-12 de la 001 y trazabilidad de D-26 sin mostrar datos no validados. · Estado: vigente.
- **D-002-09** · Sin información y negativa 403 son mensajes informativos sin reintento ni código; `TEXTO_NO_VERIFICABLE` ofrece "Volver a preguntar". · Por qué: SPEC-001 §2; reintentar una negativa no cambia el resultado y el no verificable admite reintento. · Estado: vigente.
- **D-002-10** · 429: `Retry-After` entero de 1 a 3600 s se respeta, otro valor → 60 s; texto neutro único para todo 429; cuenta por fecha de fin; selector habilitado; espera conservada al cambiar de identidad; la pregunta vuelve al campo; 0 reenvíos automáticos.
  Por qué: el 429 viene de la tasa por IP del backend (30, 60 o 120 s, D-72 de la 001), compartida por todos los usuarios de la interfaz, por eso la espera se conserva; no culpa ni revela actividad ajena (F-02). · Estado: vigente.
- **D-002-11** · Tiempo de espera del cliente: 35 s, luego error de red; con `setTimeout` y `AbortController`, no `AbortSignal.timeout`. · Por qué: el backend responde 503 en < 21 s (ESC-158) más 6 s de la API académica; el reloj falso controla `setTimeout` (F-09). · Estado: vigente.
- **D-002-12** · Salud al cargar, cada 30 s con pestaña visible, tras red caída o 503 y a demanda; no disponible → "Preguntar" deshabilitado; tiempo de 5 s con `setTimeout`. · Por qué: fallar cerrado sin sondear con la pestaña oculta. · Estado: vigente.
- **D-002-13** · Solo mismo origen: rutas relativas, sin URL base configurable ni CORS; `connect-src 'self'` fijo. · Por qué: una URL absoluta exigiría CORS en el backend (fuera de alcance) y ampliaría la política de contenido (F-17). · Estado: vigente.
- **D-002-14** · IP compartida tras el proxy: nginx reenvía sin `X-Forwarded-For` de confianza (borra esa cabecera, `Forwarded` y `X-Real-IP`) y el backend ve la IP de nginx; todos los usuarios de la interfaz comparten `ipOrigen` y el umbral de 600 por minuto (D-72 de la 001).
  Por qué: la solución más simple, sin redes ni IP fijas; con un umbral holgado y la demo solo en 127.0.0.1 basta. · Estado: aceptado por el humano (INT-030), umbral holgado.
- **D-002-15** · El servicio web va en el perfil `web` de Compose, publicado solo en `127.0.0.1:5173`; `docker compose up` sin perfil no cambia su comportamiento. · Por qué: el contrato y las pruebas ocultas usan 8080; solo el equipo local usa el selector (F-07). · Estado: vigente.
- **D-002-16** · Política de contenido estricta (todo `'self'`, `img-src 'self'` sin `data:`, Trusted Types, sin marcos, `Cross-Origin-Resource-Policy: same-origin`, sin `report-to`) y solo componentes shadcn que no inyectan `<style>`: select nativo, sin `Select`, `Dialog` ni `Sheet` de Radix.
  Por qué: esos componentes usan `react-remove-scroll`, que inyecta estilos bloqueados por `style-src 'self'`; un `report-to` sería un tercero (F-18). · Estado: vigente.
- **D-002-17** · Tipografía Plus Jakarta Sans, autoalojada, pesos 400 y 500; respaldo `system-ui, sans-serif`.
  Por qué: licencia OFL 1.1, formas geométricas amables que acompañan los radios en píldora, tildes, ñ y signos ¿¡; no es la fuente de Q10; autoalojada para 0 peticiones a terceros. · Estado: vigente.
- **D-002-18** · Paleta con contraste medido (plan §8.1): texto #2b1d19 sobre #ff6a13 (5.67:1); hover #e85d0c (4.64:1); presionado sin oscurecer; foco con contorno #2b1d19 de 2 px; bordes de campo #7a7270; texto secundario #6b6260.
  Por qué: blanco sobre #ff6a13 da 2.87:1, #ff6a13 como texto o foco sobre #f7f7f7 da 2.68:1 y #9d9795 da 2.69:1. · Estado: vigente.
- **D-002-19** · Sin marca Q10: sin logos, textos, fuentes ni peticiones a sus dominios; el build no contiene "q10"; la marca es la palabra "TutorFlow" en texto. · Por qué: lo exige el requisito. · Estado: vigente.
- **D-002-20** · Metadatos: solo `tokens`, entero tal cual el contrato ("812 tokens"); `costoUsd` se valida (número finito ≥ 0) y no se formatea ni se muestra (D-002-33); la app no recalcula ni estima. · Por qué: el contrato fija los campos y la interfaz no altera los valores. · Estado: vigente.
- **D-002-21** · El límite de 1000 se cuenta en unidades UTF-16, como el backend; vacío o solo espacios no se envía; la pregunta se envía tal como se escribió. · Por qué: evita 400 previsibles sin divergir del backend, que sigue validando. · Estado: vigente.
- **D-002-22** · Preguntas de ejemplo por rol, tomadas de `preguntas_ejemplo.json`, que solo llenan el campo y no se muestran sin identidad. · Por qué: ayudan a descubrir el asistente sin enviar peticiones no intencionadas (F-14). · Estado: vigente.
- **D-002-23** · Pruebas: Vitest + Testing Library, Playwright Test con dobles del backend y axe en 3 motores, e2e contra Compose real sin LLM y validación exploratoria con Playwright MCP; cobertura ≥ 80% de líneas.
  Por qué: los estados se fijan con dobles; el 403 y COMP-03 sin LLM son deterministas con el backend real. · Estado: vigente.
- **D-002-24** · Reglas de código del frontend verificadas por ESLint: sin consola, sin `dangerouslySetInnerHTML` ni `innerHTML`, sin almacenamiento del navegador, sin estado mutable de módulo, complejidad ≤ 5, funciones ≤ 20 líneas, sin comentarios. · Por qué: traslada AGENTS.md §3 al frontend con controles automáticos. · Estado: vigente.
- **D-002-25** · Dependencias fijadas con `package-lock.json` y `npm ci`; 0 vulnerabilidades altas o críticas en `npm audit`; imágenes Docker por digest. · Por qué: cadena de suministro (SEG-17 de la 001 aplicado al frontend); se amplía en D-002-30. · Estado: vigente.
- **D-002-26** · Movimiento: solo `transform`, `background-color` y `box-shadow` en 200 ms; con movimiento reducido, 0 ms, sin elevación ni desplazamiento suave. · Por qué: transiciones cortas del requisito y WCAG 2.3.3. · Estado: vigente.
- **D-002-27** · En `pagehide` se borran historial, identidad, campo y anuncios; en `pageshow` con `persisted` se reinicia; selector y campo controlados por React con `autocomplete="off"`. · Por qué: el navegador restaura la sesión de otra identidad desde la caché de ida y vuelta o al restaurar formularios (F-01). · Estado: vigente.
- **D-002-28** · nginx: `location =` por ruta con URI fija sin consulta, `limit_except`, el resto de `/api` (sin distinguir mayúsculas) → 404, `no-store` sin `proxy_cache`, `proxy_hide_header` de las cabeceras que fija nginx, `resolver 127.0.0.11 valid=10s`.
  Por qué: superficie mínima, sin cabeceras duplicadas ni caché de respuestas con datos, y sin perder el backend al recrearlo (F-05, F-06, F-18). · Estado: vigente.
- **D-002-29** · Sin límite en el proxy; el único límite es el del backend (D-72 de la 001): sin `limit_req`, `@limite`, `error_page 429` ni el override `frontend/compose.limite-web.yml`; se verifica con `nginx -T`.
  Por qué: INT-030, sin infraestructura ni overrides; un segundo límite bajaría el umbral compartido. · Estado: aprobado por el humano (INT-030), tras la evaluación de backend-dotnet y frontend-react.
- **D-002-30** · Cadena de suministro: `npm ci --ignore-scripts` y `npm rebuild esbuild` como única excepción; `lockfile-lint` (solo registry.npmjs.org por https y con `integrity`); `npm audit signatures`; `verificar-versiones.mjs` contra la lista de plan §1; `.nvmrc` con versión exacta.
  Por qué: impide scripts de instalación, registros ajenos y paquetes no revisados (F-08). · Estado: vigente.
- **D-002-31** · Respuesta, pregunta y anuncios en `<bdi>` con `dir="auto"` y `unicode-bidi: isolate`; los caracteres se conservan, no se eliminan. · Por qué: U+202E o U+200B no deben reordenar metadatos ni otros turnos y la interfaz no altera el texto recibido (F-16). · Estado: vigente.
- **D-002-32** · Memoria tras cambiar de identidad: sin estado mutable de módulo (ESLint) y prueba de instantánea de memoria en Chromium sin el texto de la identidad anterior. · Por qué: el historial borrado no debe seguir alcanzable en el montón (F-19). · Estado: vigente; riesgo mitigado.
- **D-002-33** · La interfaz nunca muestra `costoUsd` ("US$" 0 veces en el DOM); muestra los tokens salvo en la negativa 403, que no lleva metadatos. La API los sigue devolviendo (contrato fijo) y la interfaz los valida.
  Por qué: decisión del humano, que prevalece sobre el requisito original de mostrar tokens y costo; ocultar los tokens del 403 evita revelar qué capa negó (F-20, D-22 de la 001). · Estado: decisión del humano.
- **D-002-34** · Modo de desarrollo: Vite en `127.0.0.1:5173` (`strictPort`) con proxy solo de las 2 rutas a `http://localhost:8080`, sin cabeceras de IP y con una política de contenido de desarrollo; la estricta se valida solo con el build. · Por qué: HMR necesita `ws:` y estilos en línea; no se usa en el CI. · Estado: vigente.
- **D-002-35** · Topología de D-68 de la 001 (red `web-api`, IP .2/.3 y alias `api-web`) → sustituida por D-72 de la 001: `web` en la red existente y nginx a `api:8080`.
- **D-002-36** · Perfil `web` sin el bloqueo de identidades de D-69 de la 001 → sustituida por D-72 de la 001: ya no existe ese bloqueo.
- **D-002-37** · Determinismo: ESC-245, 255 y 256 en componente con reloj falso y `visibilityState` simulado; ESC-216 y ESC-247 solo en Chromium; ESC-285 b con `script-src-elem` solo en Chromium; ESC-260 con ventana de 640 px; ESC-299 tras reiniciar `api` y ESC-299 e como última prueba (umbral 20 solo en su entorno). · Por qué: F-09…F-13. · Estado: vigente.
- **D-002-38** · La interfaz no muestra las citas: ni chips "documento · sección" ni "Fuentes"; `citas` se valida (COMP-002-08) y no llega a ningún componente. · Por qué: son un detalle interno que no aporta a quien pregunta (INT-028). · Estado: decisión del humano.

## Riesgos y su decisión

| Riesgo | Decisión | Control verificable | D | CA / ESC |
| --- | --- | --- | --- | --- |
| XSS por la salida del LLM, las citas o la pregunta | Mitigado | Texto plano, política de contenido con Trusted Types y ESLint `react/no-danger`; 0 `img`/`script`/`a`/`b` y `window.__xss` indefinido; las citas no se pintan | D-002-07, D-002-16, D-002-24, D-002-38 | CA-002-08 · ESC-270…278 |
| Texto bidireccional o invisible que reordena metadatos u otros turnos | Mitigado | `<bdi>` con `unicode-bidi: isolate` en el 100% de la respuesta, la pregunta y los anuncios | D-002-31 | CA-002-08 · ESC-276 b |
| Detalles internos (documentos, secciones o costo) mostrados a la persona | Eliminado | `documento`, `seccion`, "US$" y el valor de `costoUsd` aparecen 0 veces en el DOM, también con 50 citas y en el 403 | D-002-33, D-002-38 | CA-002-03 · ESC-273 b |
| Confundir la demo con autenticación real | Mitigado | Aviso de demostración visible en 320–1280 px; 0 campos de texto para el ID | D-002-02 | CA-002-01 · ESC-200, ESC-203, ESC-204 |
| El selector es una herramienta de suplantación deliberada | Mitigado | Escucha solo en 127.0.0.1; desde otra interfaz no conecta; no permite nada que 8080 no permita ya (D-01 de la 001, aprobada) | D-002-02, D-002-15 | CA-002-14 · ESC-296 b |
| Respuesta de una identidad mostrada bajo otra | Eliminado | Petición cancelada y descartada al cambiar de identidad; 0 textos de la respuesta en el DOM | D-002-05 | CA-002-07 · ESC-209 |
| Anuncios o códigos de soporte de la identidad anterior | Eliminado | Región de anuncios con solo el aviso nuevo; 0 "Código de soporte" previos | D-002-05 | CA-002-07 · ESC-208 b, c |
| Sesión de otra identidad restaurada por el navegador | Eliminado | Atrás tras `about:blank` en 3 motores, `pageshow` persistido y recarga en Firefox: 0 turnos, sin identidad, campo vacío | D-002-27 | CA-002-07 · ESC-211 b, c, d |
| Una pestaña afecta a otra | Eliminado | Dos pestañas con identidades distintas no comparten turnos ni identidad | D-002-04 | CA-002-07 · ESC-211 e |
| Historial borrado alcanzable en memoria | Mitigado | 0 apariciones del texto anterior en la instantánea de memoria tras recolectar | D-002-32 | CA-002-07 · ESC-208 d |
| Inspección con DevTools del propio navegador | Eliminado por diseño | Nada persiste (ESC-279) y la persona solo ve lo que su selector ya le permite; fuera del alcance de la spec | D-002-02, D-002-04 | CA-002-12 · ESC-279 |
| Datos académicos persistidos en el navegador | Eliminado | 0 entradas en almacenamiento, IndexedDB, cookies y caché; ESLint prohíbe las API | D-002-04, D-002-24 | CA-002-12 · ESC-279 |
| Datos en la consola | Mitigado | ESLint `no-console`; 0 mensajes con la pregunta, la respuesta o IDs | D-002-24 | CA-002-12 · ESC-280 |
| Secretos o datos internos en el bundle | Eliminado | 0 `VITE_*` admitidas; `dist/` sin llaves, nombres de `datos.json`, programas, herramientas, `api:8080` ni `.map` | D-002-13, D-002-24 | CA-002-12 · ESC-283, ESC-284 |
| Peticiones a terceros o a otro backend | Eliminado | Fuente autoalojada; `connect-src 'self'`; 100% de peticiones al mismo origen | D-002-13, D-002-17 | CA-002-12 · ESC-281, ESC-288 |
| IP compartida tras el proxy: todos los usuarios de la interfaz comparten `ipOrigen` y el umbral de 600 por minuto | Aceptado por el humano (INT-030): demo solo en 127.0.0.1 y umbral holgado | ESC-299 a (misma `ipOrigen`), ESC-295 m (30 en < 5 s → 0 × 429) | D-002-14 | CA-002-15 · ESC-299 a |
| Un usuario de la interfaz agota el umbral compartido y los demás esperan 30 a 120 s | Aceptado por el humano (INT-030): 600/min no se alcanza con uso manual; el 429 se muestra neutro con su cuenta y sin reenvíos | ESC-238 b (doble con 30 s), ESC-299 e (umbral de prueba 20: 21.ª → "…en 30 s.") | D-002-10, D-002-14 | CA-002-05, CA-002-15 · ESC-299 e |
| Suplantación de IP con `X-Forwarded-For`, `Forwarded` o `X-Real-IP` | Eliminado: nginx las borra y el backend las ignora | Cabeceras falsas por 5173 y por 8080 no cambian `ipOrigen` | D-002-14 | CA-002-15 · ESC-299 b, c |
| Una persona que prueba rápido recibe 429 con los valores por defecto | Eliminado | Sin `limit_req` en `nginx -T`; 30 preguntas en < 5 s por 5173 → 30 × 200 y 0 × 429 | D-002-29 | CA-002-15 · ESC-295 m |
| Sin límite en el proxy, un cliente agota el servicio para los demás | Mitigado: la tasa del backend (600/min, escalonada) limita a todos los clientes juntos; aprobado por el humano (INT-030) | 0 `limit_req` en `nginx -T`; con el umbral de prueba en 20, la 21.ª muestra la cuenta de 30 s en la interfaz | D-002-29, D-72 de la 001 | CA-002-15 · ESC-295 m, ESC-299 e |
| Rotar identidades bloquea la demo | Eliminado: no hay bloqueo por identidades (D-72 de la 001); rotar solo emite eventos de auditoría | ESC-213 b (8 cambios, 0 textos de 429), ESC-299 a (4 × 200) | D-002-10 | CA-002-05, CA-002-15 · ESC-213 b, ESC-299 a |
| Rutas, métodos o codificaciones que alcanzan otra cosa del backend | Eliminado | Variantes de ESC-295: 404/403/405 del servidor web o la ruta del contrato; `$upstream_status` "-" en las rechazadas | D-002-28 | CA-002-15 · ESC-295 |
| Respuestas con datos guardadas en caché | Eliminado | `no-store` en cada código de `/api/*` por 5173; `nginx -T` sin `proxy_cache` | D-002-28 | CA-002-15 · ESC-294 b |
| Cabeceras de seguridad duplicadas o contradictorias | Eliminado | Cada cabecera aparece 1 vez en `/` y en `/api/*` | D-002-28 | CA-002-12 · ESC-282 |
| El proxy pierde el backend al recrearlo | Mitigado | `resolver` con 10 s; salud por 5173 en ≤ 30 s tras `restart` y `--force-recreate` | D-002-28 | CA-002-14 · ESC-296 c, d |
| Oráculo de capa por los tokens de un 403 | Eliminado en la interfaz (decisión del humano); por 8080 el contrato fijo lo expone igual (D-22 de la 001) | El 403 se muestra sin metadatos: 0 "tokens" y 0 "US$" en el turno | D-002-33 | CA-002-04, CA-002-13 · ESC-231, ESC-290 |
| El frontend rompe `docker compose up` evaluado | Eliminado | Perfil `web`; sin perfil no hay contenedor web y 8080 responde igual | D-002-15 | CA-002-14 · ESC-293 |
| Contraste AA insuficiente con la paleta | Mitigado | Pares con cifras en plan §8.1 y 0 violaciones de axe | D-002-18 | CA-002-09 · ESC-257…260 |
| La política de contenido rompe componentes | Mitigado | Lista cerrada de componentes; 0 violaciones de la política en todos los estados | D-002-16 | CA-002-12 · ESC-285 |
| Uso de la marca Q10 | Eliminado | Build sin "q10"; 0 peticiones a dominios de Q10; fuente distinta | D-002-19 | CA-002-12 · ESC-284 |
| Paquetes npm vulnerables, alterados o con scripts de instalación | Mitigado | `npm audit`, `npm audit signatures`, `lockfile-lint`, `--ignore-scripts` y lista cerrada en el CI | D-002-25, D-002-30 | CA-002-14 · ESC-298 |
| Clickjacking | Eliminado | `frame-ancestors 'none'` y `X-Frame-Options: DENY`; un iframe de otro origen no muestra la app | D-002-16 | CA-002-12 · ESC-286 |
| Detalles internos en mensajes de error | Eliminado | Textos fijos; "System." y el `detail` aparecen 0 veces | D-002-08 | CA-002-05 · ESC-235 |
| `Retry-After` abusivo bloquea la app | Mitigado | Valores fuera de 1–3600 → 60 s | D-002-10 | CA-002-05 · ESC-239 |
| Texto de 429 que culpa o revela actividad ajena | Eliminado | Texto único neutro en todo 429 | D-002-10 | CA-002-05 · ESC-238 |
| Pruebas no deterministas | Mitigado | Reloj falso, motor fijo y ventana limpia según D-002-37 | D-002-37 | ESC-216, 245, 247, 255, 256, 260, 285, 299 |
