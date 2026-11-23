# 02 · Pregunta y respuesta

> Spec: [`spec.md`](../../specs/002-interfaz-web/spec.md) §5 · Plan: [`plan.md`](../../specs/002-interfaz-web/plan.md) §4.2, §4.3, §8.2, §8.3 · CA-002-02, CA-002-03, CA-002-04, CA-002-13

Notación en [`README.md`](../README.md#notación-de-la-feature-002). Salvo que la fila diga otra cosa: identidad EST-001, servicio en línea y ancho 1280 px.

## Pregunta

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-215 `@alterno` | EST-001 | (a) campo vacío; (b) 3 espacios; en ambos pulsa Enter | Contador "0/1000" y "3/1000"; "Preguntar" deshabilitado; 0 peticiones | RF-002-02, D-002-21 |
| ESC-216 `@feliz` | Solo Chromium; EST-001; doble que responde en 2 s | "¿Cuál es mi promedio?" + Enter | "Preguntar" muestra "Consultando…" y está deshabilitado en ≤ 200 ms (p95 de 20 envíos en una sola corrida, sin reintentos); el campo queda vacío; el turno muestra la pregunta y un marcador de carga | COMP-002-11, RNF-002-03, UX-002-04 |
| ESC-217 `@feliz` | (a) EST-001; (b) COORD-02; sin turnos | Pulsa la primera pregunta de ejemplo | (a) 3 ejemplos del estudiante y (b) 2 del coordinador, los de plan §3; el campo contiene exactamente el texto pulsado y tiene el foco; 0 peticiones | UX-002-03, D-002-22 |
| ESC-218 `@alterno` | EST-001 | 1000 letras "a" + Enter | Contador "1000/1000"; 1 petición cuya `pregunta` tiene longitud 1000 | D-002-21 |
| ESC-219 `@alterno` | EST-001 | 1001 letras "a" | Contador "1001/1000"; "La pregunta supera los 1000 caracteres."; campo con `aria-invalid="true"`; "Preguntar" deshabilitado; 0 peticiones tras Enter | D-002-21 |
| ESC-220 `@alterno` | EST-001 | 999 letras "a" y el emoji "😀" | Contador "1001/1000" (el emoji cuenta 2, como en el backend); "Preguntar" deshabilitado; 0 peticiones | D-002-21 |
| ESC-221 `@abuso` | EST-001; doble que responde en 2 s | "¿Cuál es mi promedio?" y Enter 2 veces en 100 ms; luego clic en "Preguntar" | Exactamente 1 petición | COMP-002-11 |
| ESC-222 `@alterno` | EST-001 | Escribe "  línea 1", Mayús+Enter, "línea 2  " y Enter | 1 petición con `pregunta` exactamente "  línea 1\nlínea 2  " (sin recortar) | UX-002-04, D-002-21 |

## Respuesta

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-223 `@feliz` | EST-001; doble con el cuerpo ESC-223 de plan §8.2 | Una pregunta | Texto exacto "Una asignatura se aprueba con una nota definitiva de 3.0."; "812 tokens"; 0 insignias "Usó datos académicos"; el DOM contiene 0 veces "US$", "0.000412", "reglamento_academico_2025.md", "Artículo 5" y "Fuentes" | COMP-002-02, D-002-33, D-002-38 |
| ESC-224 `@feliz` | EST-001; doble con el cuerpo ESC-224 | Una pregunta | El texto ocupa 5 líneas, en orden, empezando por "Tu promedio es 4.39."; 1 insignia; "1530 tokens"; el DOM contiene 0 veces "US$", "evaluacion_y_calificaciones.md" y "Sección 4" | COMP-002-02, D-002-20 |
| ESC-225 `@feliz` | COORD-02; doble con el cuerpo ESC-225 | Una pregunta | 1 insignia; "2048 tokens"; el DOM contiene 0 veces "US$", "1.234567", "Datos del programa" y "Artículo 5" | COMP-002-02 |
| ESC-226 `@alterno` | EST-001; doble 200 con `respuesta` "Texto de prueba.", `citas` [], tokens 0 y costo 0 | Una pregunta | Texto exacto; "0 tokens"; 0 "US$" y 0 "Fuentes" en el DOM | COMP-002-02 |
| ESC-227 `@alterno` | EST-001 a 360 px; doble 200 con `respuesta` de 20 000 letras "a" sin espacios | Una pregunta | El texto completo está en el DOM; `scrollWidth - clientWidth` del documento = 0 | SEG-002-01, RNF-002-02 |
| ESC-228 `@alterno` | EST-001; doble 200 con el cuerpo ESC-223 más `"extra":"CAMPO_EXTRA"` | Una pregunta | Se muestra como ESC-223; el DOM contiene 0 veces "CAMPO_EXTRA" | COMP-002-08 |
| ESC-229 `@alterno` | EST-001; doble 200 con `respuesta` "" (y, en b, "   ") y el resto válido | Una pregunta | "Recibimos una respuesta que no pudimos leer. Intenta de nuevo." con "Reintentar" | COMP-002-08 |

## Mensajes informativos

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-230 `@feliz` | EST-001; doble 200 con `respuesta` = COMP-03, `citas` [], tokens 0, costo 0 | "¿Qué dice el reglamento sobre el uso de parqueaderos?" | Texto exacto de COMP-03 y "Prueba con otra pregunta sobre el reglamento, tus notas o tu asistencia."; 0 "Reintentar"; 0 "Código de soporte"; 0 `role="alert"`; "0 tokens"; 0 "US$" | COMP-002-03, D-002-09 |
| ESC-231 `@permisos` | EST-001; doble 403 con NEG del estudiante, [Art. 22], tokens 0, costo 0 | "¿Cuáles son las notas de EST-002?" | "Acceso restringido"; el texto exacto de NEG; el turno contiene 0 veces "tokens" y "US$"; el DOM contiene 0 veces "Artículo 22" y "reglamento_academico_2025.md"; 0 "Reintentar"; 0 "Código de soporte"; 0 `role="alert"` | COMP-002-04, D-002-09, D-002-33 |
| ESC-232 `@permisos` | COORD-01; doble 403 con NEG del coordinador | "¿Qué estudiantes de ADM perdieron materias?" | Igual que ESC-231 con el texto de NEG del coordinador | COMP-002-04 |
| ESC-233 `@alterno` | EST-001; doble 200 con `respuesta` = TNV, `citas` [], tokens 0 | "¿Cuál es mi promedio?" y luego "Volver a preguntar" | Aviso con el texto exacto de TNV, "0 tokens" y "Volver a preguntar"; al pulsarlo, una 2.ª petición con cuerpo idéntico a la 1.ª; 0 "Código de soporte" | COMP-002-05, D-002-09 |
| ESC-234 `@abuso` | EST-001; doble 403 con `application/problem+json` `{"title":"Forbidden","detail":"Acceso denegado a EST-002"}` | Una pregunta | "Recibimos una respuesta que no pudimos leer. Intenta de nuevo."; 0 "Acceso restringido"; la tarjeta del turno contiene 0 veces "EST-002" y "Forbidden" | COMP-002-08, SEG-002-07 |
