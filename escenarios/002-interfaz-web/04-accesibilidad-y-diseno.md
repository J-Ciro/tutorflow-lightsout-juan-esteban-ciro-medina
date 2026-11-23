# 04 · Accesibilidad y diseño

> Spec: [`spec.md`](../../specs/002-interfaz-web/spec.md) §5, §8 · Plan: [`plan.md`](../../specs/002-interfaz-web/plan.md) §4.6, §5, §8.1 · CA-002-09, CA-002-10, CA-002-11

Notación en [`README.md`](../README.md#notación-de-la-feature-002). Revisión automática = `@axe-core/playwright` con las etiquetas `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa` y `wcag22aa`.

## Revisión automática y contraste

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-257 `@feliz` | Estados (a) bienvenida sin identidad, (b) cargando, (c) respuesta ESC-224, (d) sin información, (e) negativa, (f) 503 con servicio no disponible, (g) 429, (h) ilegible, (i) aviso de cambio de identidad con espera de 429 | Revisión automática a 360 y 1280 px | 0 violaciones en los 18 casos | RNF-002-01 |
| ESC-258 `@feliz` | Estado (c) | Lee los estilos calculados del botón principal (normal y hover), texto secundario, borde del campo, error e insignia | Cada par coincide con un par de plan §8.1 y su contraste calculado es ≥ el mínimo de esa fila | D-002-18 |
| ESC-259 `@abuso` | Todos los estados de ESC-257 | Recorre todos los elementos visibles con texto | 0 elementos con fondo #ff6a13 o #e85d0c y texto distinto de #2b1d19; 0 elementos con texto #ff6a13 sobre #f7f7f7 o #ffffff | UX-002-02, D-002-18 |
| ESC-260 `@alterno` | Estado (c) | (a) A 1280 px aplica interlineado 1.5, espaciado de letras 0.12em, de palabras 0.16em y de párrafos 2em; (b) zoom al 200% de 1280 px, reproducido con una ventana de 640 × 800 px | Desplazamiento horizontal 0 px; cada botón e insignia con `scrollHeight` ≤ `clientHeight` (texto sin recortar) | RNF-002-01 |

## Teclado, foco y anuncios

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-261 `@feliz` | EST-001 sin turnos, servicio en línea | Tab desde el inicio de la página | Orden exacto de plan §4.6: "Saltar a la pregunta", "Usuario de prueba", "Nueva conversación", las 3 preguntas de ejemplo, "Tu pregunta", "Preguntar"; 0 elementos con `tabindex` > 0 | RF-002-08 |
| ESC-262 `@feliz` | Cada control del orden de ESC-261 y "Reintentar", "Copiar código", "Comprobar ahora" | Recibe el foco con el teclado | Contorno sólido de 2 px #2b1d19 con separación de 2 px (o #ff6a13 sobre #2b1d19), contraste ≥ 3:1 con el fondo adyacente | D-002-18 |
| ESC-263 `@feliz` | EST-001; doble con ESC-223 que responde en 1 s | Escribe con el teclado y pulsa Enter; luego elige COORD-01 con el teclado | La región `role="status"` recibe en orden "Consultando al asistente…", "Una asignatura se aprueba con una nota definitiva de 3.0." y "Conversación borrada: ahora preguntas como COORD-01."; el foco queda en "Tu pregunta" tras la respuesta | RF-002-08 |
| ESC-264 `@feliz` | Bienvenida y estado (c) | Lee el árbol de accesibilidad | `lang="es"`; título "TutorFlow · Asistente académico"; combobox "Usuario de prueba"; textbox "Tu pregunta" con descripción que contiene "Cada pregunta se responde por separado. Máximo 1000 caracteres."; lista "Conversación"; 0 listas "Fuentes"; cada botón con nombre visible | RF-002-08 |
| ESC-265 `@alterno` | EST-001 a 360 px con un error 503 en el último turno | Activa "Saltar a la pregunta"; luego Mayús+Tab hasta "Reintentar" | El foco pasa a "Tu pregunta"; con "Reintentar" enfocado, su rectángulo se solapa 0 px con la barra fija del formulario | RF-002-08 |

## Diseño responsivo, movimiento y tokens

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-266 `@feliz` | Estado (c) en 320, 360, 768 y 1280 px | Mide el documento | `scrollWidth - clientWidth` = 0 en los 4; a 360 × 640, "Tu pregunta" y "Preguntar" visibles sin desplazarse; "Preguntar", "Reintentar" y las preguntas de ejemplo miden ≥ 44 × 44 px | RNF-002-02, UX-002-07 |
| ESC-267 `@feliz` | Movimiento sin preferencia | Pasa el cursor sobre "Preguntar" | `transition-property` "transform, background-color, box-shadow"; `transition-duration` 0.2s; `transform` translateY(-2px) | UX-002-06, D-002-26 |
| ESC-268 `@alterno` | `prefers-reduced-motion: reduce` | Pasa el cursor sobre "Preguntar"; envía una pregunta con 10 turnos previos | `transition-duration` 0s; `transform` none; el desplazamiento al último turno es instantáneo; el marcador de carga es estático y dice "Consultando…" | UX-002-05, UX-002-06 |
| ESC-269 `@feliz` | Estado (c) a 1280 px | Lee los estilos calculados | Radio de "Preguntar" y de la insignia 999px; de la tarjeta de respuesta 20px; del campo 12px; relleno de "Preguntar" 15px 26px; `font-family` empieza por "Plus Jakarta Sans"; cuerpo 16px; todo `font-weight` visible es 400 o 500; la tarjeta usa la sombra `normal` de plan §5.1 | UX-002-01, D-002-17 |
