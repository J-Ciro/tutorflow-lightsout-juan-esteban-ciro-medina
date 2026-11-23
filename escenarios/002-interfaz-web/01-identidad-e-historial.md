# 01 · Identidad simulada e historial

> Spec: [`spec.md`](../../specs/002-interfaz-web/spec.md) §5, §7 · Plan: [`plan.md`](../../specs/002-interfaz-web/plan.md) §4.4 · CA-002-01, CA-002-05, CA-002-07, CA-002-13

Notación en [`README.md`](../README.md#notación-de-la-feature-002). Salvo que la fila diga otra cosa: servicio en línea, ancho 1280 px y doble del backend que responde 200 con el cuerpo de ESC-223. "Aviso de cambio de X" = el texto exacto de spec §5 con `<ID>` = X.

## Identidad

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-200 `@feliz` | Persona sin identidad | Abre la app | "Elige un usuario de prueba" elegido; 8 opciones en "Estudiantes" (EST-001…EST-006 · estudiante) y "Coordinadores" (COORD-01, COORD-02 · coordinador); "Preguntar" deshabilitado con "Elige un usuario de prueba para preguntar."; 0 preguntas de ejemplo; 0 peticiones | RF-002-01, UX-002-03, D-002-22 |
| ESC-201 `@feliz` `@contrato` | Elige "EST-004 · estudiante" | "¿Cuál es mi promedio ponderado del periodo?" | 1 petición `POST /api/asistente/preguntar` con `Content-Type: application/json` y cuerpo exactamente `{"pregunta":"¿Cuál es mi promedio ponderado del periodo?","usuario":{"rol":"estudiante","id":"EST-004"}}`; sin `X-Correlation-Id`, `X-Request-Id` ni `traceparent` | SEG-002-09 |
| ESC-202 `@feliz` | Elige "COORD-02 · coordinador" | "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?" | `usuario` exactamente `{"rol":"coordinador","id":"COORD-02"}` | RF-002-01 |
| ESC-203 `@alterno` | Cualquiera | Recorre todos los controles de la página | 0 `input` de texto y 0 `textarea` además de "Tu pregunta"; ninguna opción del selector tiene nombres de personas ni programas | D-002-02, D-002-03 |
| ESC-204 `@alterno` | EST-001 con 10 turnos | Desplaza el historial hasta el final a 360 y 1280 px | El aviso de demostración, con el texto exacto de spec §5, sigue visible en la cabecera | COMP-002-01 |
| ESC-205 `@permisos` `@abuso` | Sin identidad | Abre `/?id=COORD-01&rol=coordinador#COORD-01` | Ninguna identidad elegida; "Preguntar" deshabilitado; 0 peticiones de pregunta | SEG-002-06 |
| ESC-206 `@permisos` `@abuso` | EST-001 elegido | Cambia en el DOM el `value` de la opción elegida a `ADMIN-01` y dispara `change` | Identidad nula; "Preguntar" deshabilitado; 0 peticiones de pregunta | SEG-002-06 |

## Historial y cambio de identidad

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-207 `@feliz` | EST-001 | Envía 2 preguntas respondidas | 2 turnos en orden de envío; cada uno con la pregunta, la etiqueta "EST-001" y su respuesta; el foco queda en "Tu pregunta", vacío | COMP-002-10, UX-002-04 |
| ESC-208 `@permisos` | EST-001 con 2 turnos | Elige "COORD-01 · coordinador" | 0 turnos; el DOM contiene 0 veces "Una asignatura se aprueba con una nota definitiva de 3.0."; aviso de cambio de COORD-01 visible con `role="status"`, sin botones; la región de anuncios recibe ese texto; el foco queda en el selector | RF-002-07, COMP-002-13, D-002-04 |
| ESC-208 b `@permisos` | EST-001 con 1 turno respondido (anuncio con su texto) | Elige "EST-002 · estudiante" | La región de anuncios contiene solo el aviso de cambio de EST-002: 0 caracteres del texto anterior | SEG-002-08, D-002-05 |
| ESC-208 c `@permisos` | EST-001 con 1 turno de error 503 y "Código de soporte: 019a5f3e-7c1d-7b2a-9e4f-0123456789ab" | Elige "EST-002 · estudiante" | 0 "Código de soporte" y 0 apariciones de "019a5f3e" en el DOM | SEG-002-08, D-002-05 |
| ESC-208 d `@abuso` | Chromium; EST-001 con 2 turnos cuya respuesta contiene la marca "MARCA-EST001-7f3a" | Elige "EST-002 · estudiante", envía 1 pregunta, y la prueba fuerza la recolección (`HeapProfiler.collectGarbage`) y toma una instantánea de memoria por CDP | La instantánea contiene 0 cadenas con "MARCA-EST001-7f3a" | D-002-32 |
| ESC-209 `@permisos` `@abuso` | EST-001; doble que responde en 3 s | Envía "¿Cuál es mi promedio?" y elige "EST-002 · estudiante" a los 500 ms | La señal de la petición queda abortada; 0 turnos; a los 5 s el DOM contiene 0 veces el texto del doble; "Preguntar" se habilita para EST-002 | SEG-002-08, D-002-05 |
| ESC-210 `@alterno` | EST-001 con 3 turnos | Pulsa "Nueva conversación" | 0 turnos; la identidad sigue siendo EST-001; 0 avisos de cambio; el foco queda en "Tu pregunta" | RF-002-07 |
| ESC-211 `@alterno` | EST-001 con 2 turnos | Recarga la página | 0 turnos y ninguna identidad elegida | D-002-04 |
| ESC-211 b `@permisos` | Chromium, Firefox y WebKit; EST-002 con 2 turnos y "borrador" escrito en el campo | Navega a `about:blank` y vuelve con Atrás | 0 turnos; ninguna identidad elegida; campo vacío; región de anuncios vacía; el DOM contiene 0 veces "EST-002" fuera del selector y 0 veces el texto de las respuestas | SEG-002-12, D-002-27 |
| ESC-211 c `@permisos` | Componente; EST-002 con 2 turnos | Dispara `pagehide` y luego `pageshow` con `persisted: true` (camino de la caché de ida y vuelta y de reabrir la pestaña con Ctrl+Mayús+T) | Tras `pagehide`: identidad nula, 0 turnos, campo vacío, petición abortada; tras `pageshow`: el mismo estado y 0 avisos de cambio | SEG-002-12, D-002-27 |
| ESC-211 d `@permisos` | Firefox; EST-002 elegido y "borrador" escrito en el campo, sin enviar | Recarga la página | Selector en "Elige un usuario de prueba" y campo vacío (sin restauración de formulario); `autocomplete="off"` en el `form` | SEG-002-12, D-002-27 |
| ESC-211 e `@permisos` | Dos pestañas del mismo contexto: A con EST-001 y 2 turnos, B recién abierta | En B elige "COORD-01 · coordinador" y pregunta | A conserva EST-001 y sus 2 turnos; B no muestra nada de A; 0 mensajes entre pestañas | D-002-04 |
| ESC-212 `@alterno` | EST-001 con 50 turnos | Envía la pregunta 51 | 50 turnos; el primero ya no está y el último es el 51 | COMP-002-10 |
| ESC-213 `@alterno` | EST-001 con 2 turnos | Elige otra vez "EST-001 · estudiante" | 2 turnos; 0 avisos de cambio | D-002-04 |
| ESC-213 b `@alterno` | Dobles 200 para toda pregunta | Recorre 8 cambios de identidad (EST-001…EST-006, COORD-01, COORD-02) con 1 pregunta en cada una | 8 respuestas mostradas; 7 avisos de cambio; 0 `role="alert"`; 0 textos de 429 | COMP-002-13, D-002-10 |
| ESC-214 `@abuso` | EST-001 con 2 turnos y una espera de 429 de 40 s; reloj falso | Elige "EST-003 · estudiante" a los 10 s | 0 turnos; el aviso de cambio de EST-003 termina con "La espera de 30 s se mantiene: el límite se aplica a todos los usuarios."; selector habilitado; "Preguntar" deshabilitado hasta los 40 s | COMP-002-12, D-002-10 |
| ESC-214 b `@abuso` | Doble: 200 a EST-001…003, 429 con `Retry-After: 120` a EST-004; reloj falso | Pregunta como EST-001…EST-004; a los 30 s elige EST-001 | "…en 02:00."; la pregunta vuelve al campo; 0 "Acceso restringido"; tras el cambio, cuenta "01:30" y "Preguntar" deshabilitado; a 120 s, "Ya puedes preguntar de nuevo." y 0 peticiones nuevas (nota ESC-214 b) | COMP-002-12, D-002-10 |

- **ESC-214 b:** 120 s es la duración del 3.er bloqueo de la tasa del backend (D-72 de la 001).
