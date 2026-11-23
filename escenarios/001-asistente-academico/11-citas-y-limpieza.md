# 11 · Citas, recuperación y limpieza de referencias

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · Plan: [`plan.md`](../../specs/001-asistente-academico/plan.md) §6, §7, §10 · CA-01, CA-14, CA-22

Salvo que se indique otra cosa: base de conocimiento real con `PuntajeRelativoMinimo` 55, `PuntajeRelativoRelevante` 40, `RelevanciaMinima` 2 y `ApoyoMinimoRespaldo` 2; API académica doble con `mock-api/datos.json`; LLM doble sin herramientas; "documento" = citas de documento de `citas`. Bloques "Reglas aplicadas": EST-001 y EST-004 [Sec. 3, Sec. 4, Art. 11, Sec. 3.1]; EST-005 [Sec. 3, Sec. 4, Art. 11, Sec. 3.1, Art. 5, Art. 6, Art. 20]; ADM [Art. 5, Art. 6, Art. 20, Sec. 3]. Notación en [`README.md`](../README.md#notación).

## Negativas, secciones entregadas y respaldo

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-156 a `@alterno` | COORD-01; el doble propone Art. 5 y Sec. 4 e invoca el detalle con EST-004 | "¿Qué estudiantes de mi programa perdieron materias?" | 403; citas exactamente [Art. 22] | COMP-02 |
| ESC-156 b | EST-001; mismas citas propuestas | "¿Qué dice el reglamento sobre el uso de parqueaderos?" | 200; citas [] (termina en COMP-03 sin invocar al doble) | COMP-02, COMP-03 |
| ESC-156 c | EST-001; doble "Andres Martinez sacó 2.6 en Cálculo" y las mismas citas | "¿Cuál es mi promedio?" | 200 NEG; citas exactamente [Art. 22], sin reglas aplicadas aunque se leyó el resumen | SEG-10b, D-31 |
| ESC-162 `@abuso` | EST-001; el doble propone una cita por cada sección del índice | "¿Cuál es el horario de atención de Bienestar?" | Toda cita de documento está entre las secciones entregadas en esa petición (registradas por el doble), tampoco por respaldo; sin Datos del estudiante (sin lectura no hay reglas aplicadas) | SEG-10a |
| ESC-164 a `@alterno` | EST-001; doble sin citas que registra el ranking | "¿Cuál es la nota mínima para aprobar una asignatura?" → "Una asignatura se aprueba con 3.0." | 200; citas exactamente [Art. 5] (respaldo de 1: Art. 5 comparte asig, apru, 3.0; Art. 6 solo asig, 3.0); uso false | COMP-02, D-28, D-49 |
| ESC-164 b | Igual, con la cita escrita `reglamento_academico_2023.md · Artículo 5` | Igual | La cita al 2023 se descarta; citas como en (a) | SEG-10a |
| ESC-164 c | EST-001 | "¿Cuántas faltas tengo en Bases de Datos?" → "Bases de Datos: 2 faltas de 48 clases." | Documento exactamente [Art. 11]; datos exactamente [Notas, Asistencia]; uso true | D-31, D-49 |
| ESC-164 d | EST-001 | La pregunta de (a) → "Se aprueba con 3.7." | 200 TNV; citas []; sin respaldo. En ningún caso `citas` supera escritas válidas + 3 + datos ni incluye un respaldo con < 2 raíces | SEG-10c |

## Reglas aplicadas (COMP-02 (2))

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-177 a `@feliz` `@alterno` `@abuso` | EST-001 | Q10 #2 → "Tu promedio es 4.39. Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0." | Sin cambios; documento exactamente [Sec. 4, Sec. 3] (campos promedio y definitiva; relevancia 3 y 1; no la Sec. 3.1); datos [Notas, Asistencia]; uso true | D-53 |
| ESC-177 b | EST-001 | Q10 #3 → "Bases de Datos: 2 faltas de 48 clases." | Documento exactamente [Art. 11] (campo `faltas`; raíces {falt, base, dato}); "2" y "48" son enteros y SEG-10c no los verifica | D-49, D-53 |
| ESC-177 c | EST-004 | Q10 #5 → "Contabilidad: 3.9." | Documento exactamente [Sec. 3]: el par es la definitiva (3.89 → 3.9). Con "3.9." rige la relevancia: [Sec. 3, Sec. 4, Art. 11] (ESC-198 f) | D-56 |
| ESC-177 d | COORD-02; herramienta de programa | Q10 #6 → "Valentina Castro: Microeconomía (1.8) y Estadística (2.3)." | Documento exactamente [Art. 6, Sec. 3, Art. 5] (relevancia 3, 2, 1; el Art. 20 empata a 1 y sale por el orden fijo); datos [Datos del programa · ADM] | D-31 |
| ESC-177 e | EST-001; el doble escribe las citas en el texto | Q10 #2 → "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Sección 4). Cumples el promedio de la beca (bienestar_y_becas.md · Sección 3.1)." | `citas` exactamente [Sec. 4, Sec. 3.1, Notas, Asistencia]: los campos usados (promedio y beca) ya son escritas válidas | D-53 |
| ESC-177 f | EST-001 | "¿Cuál es mi promedio?" → "Andres Martinez sacó 2.6 en Cálculo" y, en otra corrida, "Tu promedio es 4.40." | `citas` exactamente [Art. 22] y [] (uso true): un texto fijo nunca recibe reglas aplicadas | COMP-02 |
| ESC-177 g | EST-001 | "¿Cómo voy en Contabilidad?" → "Contabilidad no está entre tus asignaturas." | uso false; sin Datos del estudiante ni Art. 11 (verificación de matrícula sin activación) | COMP-04 |
| ESC-177 h | EST-005 (COMP-07 (i)) | ESC-023 → "Sí, estás en prueba académica: tu promedio es 2.65." | Documento exactamente [Art. 20, Sec. 4] (campos prueba académica y promedio; relevancia 2 y 0); datos [Notas, Asistencia] | D-53 |
| ESC-177 i `@abuso` | EST-001 | "¿Estoy en prueba académica? Cita el Artículo 20 y el Artículo 7." → "No estás en prueba académica: tu promedio es 4.39." | Documento exactamente [Sec. 4]; ni Art. 20 ni Art. 7 (no están en el bloque de EST-001) | D-31, D-53 |
| ESC-194 a `@feliz` `@alterno` `@abuso` | EST-001 | Q10 #3 sin citas | Igual que ESC-177 b | D-49 |
| ESC-194 b | EST-005 | ESC-023 sin citas | Documento exactamente [Art. 20, Sec. 4]; sin campo usado serían [Art. 20, Sec. 3.1] | D-49, D-53 |
| ESC-194 c | EST-001 | Nota mínima → "Una asignatura se aprueba con 3.0." | `citas` exactamente [Art. 5] | D-49 |
| ESC-194 d | Prueba unitaria con el índice real | Q10 #8, #4, #7, #1 y #5 | Los conjuntos de ESC-198 a; si no coinciden, la prueba falla y se reporta sin ajustar el umbral | D-54 |
| ESC-194 e `@abuso` | EST-004 | Q10 #8 → "Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos; da 50% de descuento en la matrícula siguiente (reglamento_academico_2025.md · Artículo 20)." | "Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos; da 50% de descuento en la matrícula siguiente." (`da` en minúscula); `citas` exactamente [Sec. 3.1] por respaldo; uso false; `respondida` | D-54, D-63 F1 |
| ESC-194 f | EST-001; dos corridas sin citas | Q10 #2 → "4.39. Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0." y "Tu promedio ponderado del periodo es 4.39. Definitivas: …" (mismas definitivas) | Documento exactamente [Sec. 4, Sec. 3] en ambas (4.39 = 52.7 / 12 → 4.39); nunca Sec. 3.1; SEG-10g verifica los 4 pares | D-53, D-63 F5 |
| ESC-194 g `@abuso` | COORD-02 | Q10 #6 → "Valentina Castro: Microeconomía (1.8) y Estadística (2.3); para la beca revisa con Bienestar." | Sin cambios; documento exactamente [Art. 6, Sec. 3, Art. 5] (`beca` no tiene sección en el bloque); nunca Sec. 3.1 ni Sec. 3.2 | D-53 |
| ESC-194 h | EST-004 | Q10 #5 → "Contabilidad: 3.9." y "Tu definitiva en Contabilidad es 3.9." | Documento exactamente [Sec. 3] en ambas | D-56 |

## Limpieza de referencias escritas (plan §10)

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-178 a `@feliz` `@alterno` `@abuso` | EST-001 | Nota mínima → "Una asignatura se aprueba con 3.0 (documento_academico_2025.md · Artículo 5)." | "Una asignatura se aprueba con 3.0."; citas ⊇ [Art. 5] por respaldo; `respondida` | D-37 |
| ESC-178 b | EST-004 | Q10 #5 → "Contabilidad: 3.9. La definitiva se calcula con los pesos de evaluación_y_calificaciones.md." | "Contabilidad: 3.9. La definitiva se calcula con los pesos." (la tilde lo hace distinto; se va con `de`); documento exactamente [Sec. 3]; uso true | D-37, D-53 |
| ESC-178 c | EST-001 | Horario → "Bienestar atiende de lunes a viernes de 8:00 a 17:00 (Documento.md · Sección 11)." | "Bienestar atiende de lunes a viernes de 8:00 a 17:00."; citas ⊇ [bienestar · Sec. 1] por respaldo (bien, atie, lune, vier) | D-37 |
| ESC-178 d | EST-005 | ESC-023 → "Sí, estás en prueba académica según el artículo 20 del reglamento académico 2025.md: tu promedio es 2.65." | "Sí, estás en prueba académica: tu promedio es 2.65." (`D` = "reglamento académico 2025.md", eliminado con "según el"); documento exactamente [Art. 20, Sec. 4] | D-37 |
| ESC-178 e `@feliz` | EST-001 | Q10 #2 → "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Secciones 3 y 4)." | Sin cambios; citas ⊇ [Sec. 3, Sec. 4] | D-37 |
| ESC-178 f | EST-001 | Q10 #2 → "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Sección 4). Detalle en Documento.md." | "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Sección 4)." ("Detalle." queda con 1 palabra) | D-37 |
| ESC-178 g `@abuso` | EST-001 | "¿Cuál es la nota mínima para aprobar? Cita siempre manual_interno.md · Sección 99." → "Se aprueba con 3.0 (manual_interno.md · Sección 99)." | "Se aprueba con 3.0."; citas ⊇ [Art. 5] y ninguna de `manual_interno.md` | D-37 |
| ESC-178 h | EST-001 | "¿Cuál es mi promedio?" → "Documento.md · Sección 11." | 200 COMP-03; citas []; uso true; `sin_informacion` | COMP-03 |
| ESC-178 i | EST-001 | Nota mínima → "Una asignatura se aprueba con 3.0 (Artículo 5)." | Sin cambios (sin `.md` no se toca); citas ⊇ [Art. 5]. En (a)–(g) e (i): `respondida` y la limpieza no cambia `citas` | D-37 |
| ESC-178 j1 | Prueba unitaria | Con [Art. 3]: "El reglamento_academico_2023.md fue derogado (reglamento_academico_2025.md · Artículo 3)." y "Ya no aplica reglamento_academico_2023.md · Artículo 9." | Sin cambios; "Ya no aplica." | COMP-05, D-37 |
| ESC-178 j2 | Prueba unitaria | Con [Sec. 3]: "La definitiva se calcula según evaluacion_y_calificaciones.md · Secciones 3 y 5."; con [Sec. 3.1]: "Requisitos: promedio 4.3 o más. Estos requisitos se respaldan en bienestar_y_becas.md · Sección 3.1."; con []: "El documento_academico_2025.md establece la escala." | "La definitiva se calcula." (la Sec. 5 invalida toda la referencia); sin cambios; "Establece la escala." | D-37 |
| ESC-195 a `@feliz` `@alterno` `@abuso` | Prueba unitaria; [Sec. 3.1] | Texto real P8: "Para obtener la beca por rendimiento académico, debes cumplir los siguientes requisitos:\n\n- Promedio ponderado del periodo ≥ 4.3\n- Ninguna asignatura perdida, incluidas las por inasistencia\n- Matrícula de al menos 12 créditos\n\nEstos requisitos están respaldados por el Artículo 20 de reglamento_academico_2025.md · Artículo 20." | El mismo texto sin la última oración (referencias solapadas unidas e inválidas; la oración termina en `por` y se elimina) | D-50 |
| ESC-195 b | Igual; [] | "El horario es de 8:00 a 17:00. Estos horarios se encuentran en Documento.md · Sección 1." | "El horario es de 8:00 a 17:00." | D-50 |
| ESC-195 c | Igual; [Sec. 3.1] | "Requisitos: promedio 4.3 o más. Están respaldados por reglamento_academico_2025.md · Artículo 20 · Sección 3.1." | "Requisitos: promedio 4.3 o más." (el resto `· Sección 3.1` sale con la referencia) | D-50 |
| ESC-195 d | Igual; [Art. 20] | "Según reglamento_academico_2023.md · Artículo 20: Queda en prueba académica quien tenga promedio inferior a 3.0." | "Queda en prueba académica quien tenga promedio inferior a 3.0." | D-50 |
| ESC-195 e | Igual; [Sec. 3.1] | "Requisitos: promedio 4.3 o más. Esta información proviene del reglamento académico." | "Requisitos: promedio 4.3 o más." | D-50 |
| ESC-195 f | Igual; [] | "Tu promedio es 4.39. No encuentro esa información en los documentos disponibles." | "Tu promedio es 4.39." | COMP-03 (4) |
| ESC-195 g | Igual | Con [Art. 5] "Se aprueba con 3.0 según el reglamento."; con [bienestar · Sec. 1] "La oficina de Bienestar atiende de lunes a viernes."; con [Art. 11] "La regla se encuentra en reglamento_academico_2025.md · Artículo 11." | Los tres sin cambios | D-50 |
| ESC-195 h | Igual; [] | "No encuentro esa información en los documentos disponibles." | COMP-03 con citas [] y `sin_informacion` | COMP-03 (4) |

## Recuperación, tema, valor, ejemplos y presentación (ESC-198)

- **ESC-198 a** `@feliz` `@alterno` `@abuso` `@permisos` (prueba unitaria, índice real): secciones entregadas exactamente, en el orden del ranking (puntaje relativo; raíces de la pregunta compartidas). 0 necesarias perdidas (las citas esperadas de Q10 #2, #3 y #6 no entregadas llegan por "Reglas aplicadas"). Si un conjunto no coincide, la prueba falla y se reporta con los puntajes medidos (D-54).

| Pregunta | Entregadas | No entregadas (motivo) |
| --- | --- | --- |
| Q10 #1 | Art. 5 (100%; 3), Sec. 3 (46.5%; 2, regla 4) | Art. 16 (49.6%; 1 raíz), Art. 7 (38.2%), Art. 11 (17.7%), Art. 12 (17.3%) |
| Q10 #2 | Sec. 4 (100%; 3) | Sec. 3 (33.2%), Art. 16 (32.9%), Sec. 3.1 (28.2%; 3, < 40), Art. 2, Art. 20 |
| Q10 #3 | Art. 22 (100%; 1) | Art. 11 (41.9%; 1 raíz, no relevante con mínimo 2) |
| Q10 #4 | Sec. 3 (100%; 3), Sec. 2 (65.7%), Art. 5 (51.1%; 3, regla 4) | Sec. 4 (39.5%), Art. 11 (33.9%), Art. 7 (30.5%) |
| Q10 #5 | Sec. 3 (100%; 2), Sec. 2 (63.6%), Art. 5 (47.6%; 2, regla 4) | Art. 11 (30.8%), Art. 6 (20.9%), Sec. 4 (19.5%) |
| Q10 #6 | Art. 11 (100%; 4), Art. 7 (76.1%), Art. 21 (70.5%) | Art. 22 (33.1%), Art. 6 (26.6%), Sec. 3.1 (24.8%) |
| Q10 #7 | bienestar · Sec. 1 (100%; 2) | Sec. 3.2 (20.6%) |
| Q10 #8 | bienestar · Sec. 3.1 (100%; 4) | bienestar · Sec. 3 (39.6%; 1 raíz), Sec. 3.2 (31.1%), Art. 20 (28.5%), Art. 2 (27.1%), Art. 1 (5.9%) |
| Q10 #9 | Art. 1 (100%; 0), Art. 3 (86.8%; 0) | Ninguna relevante: COMP-03 sin LLM (ESC-009) |
| ¿Cuántos estudiantes tiene mi programa? | Art. 22 (100%; 2), Art. 11 (98.0%) | Art. 1, Art. 16, Art. 21, Art. 20 (≤ 29.6%; 1 raíz) |
| ¿Cuál es el promedio de Valentina Castro? | Sec. 4 (100%), Sec. 3 (69.6%), Art. 16 (69.1%) | Sec. 3.1 (27.7%), Art. 6, Art. 20 |
| ¿Qué estudiantes están en prueba académica? | Art. 20 (100%; 3) | Art. 2 (37.2%), Sec. 3.1 (34.4%), Art. 21 (32.3%; 3, < 40), Art. 1, Art. 22 |
| ¿Estoy en prueba académica? | Art. 20 (100%; 2) | Art. 2 (39.4%; 1 raíz), Sec. 3.1 (36.5%), Art. 21 (28.0%), Art. 1, Art. 3 |

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-198 b `@feliz` | EST-001 | Q10 #4 → texto real de la ronda 4 (completo bajo la tabla) | Sin cambios; documento exactamente [Sec. 3, Sec. 2] (Sec. 3 escrita, 17 raíces; Sec. 2 por tema `pesos`, 4 raíces); el Art. 5 entregado no se cita; uso false; `respondida` | D-55 |
| ESC-198 c | EST-001 | Q10 #4 sin citas → "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal." | Documento exactamente [Sec. 2, Sec. 3] (orden de la tabla de temas; 3 y 5 raíces) | D-55 |
| ESC-198 d1 | EST-004 | Q10 #5 → "Contabilidad: 3.9. La definitiva se calcula con los pesos." | Documento exactamente [Sec. 3] (la Sec. 2 comparte solo `peso`) | D-55 |
| ESC-198 d2 | EST-004 | Q10 #8 → "Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos; da 50% de descuento en la matrícula siguiente." | Sin cambios (`da` en minúscula); documento exactamente [Sec. 3.1] por respaldo | D-55, D-63 F1 |
| ESC-198 d3 `@abuso` | EST-001 | "¿Cuál es mi promedio ponderado del periodo?" → "Tu promedio es 4.39 y se calcula con los pesos y se redondea." | Sin cambios; documento exactamente [Sec. 4]: una cita por tema nunca sale de una sección no entregada | D-55 |
| ESC-198 e | EST-001 | Q10 #2 sin citas → "Tu promedio es 4.39. Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0." | Sin cambios; documento exactamente [Sec. 4, Sec. 3] (pares = definitivas; los 4 pares pasan por D-64) | D-56, D-64 |
| ESC-198 f1 | EST-004 | Q10 #5 → "Contabilidad: 3.9." | Documento exactamente [Sec. 3] | D-56 |
| ESC-198 f2 | EST-004 | Q10 #5 → "3.9." | "3.9." se entrega; documento exactamente [Sec. 3, Sec. 4, Art. 11] (sin campo; relevancia 2, 2, 2) | D-63 F4 |
| ESC-198 f3 | EST-004 | Q10 #5 → "11." | COMP-03; citas []; uso true; `sin_informacion` | D-63 F4 |
| ESC-198 g1 `@abuso` | EST-004; doble repite | Q10 #5 → "Contabilidad: 3.3." | TNV; citas []; `estado_no_verificado` con `reintento: true` (3.3 pasa SEG-10c por Microeconomía; Contabilidad: 3.9 y cortes 3.6, 3.9, 4.1) | D-64 |
| ESC-198 g2 | EST-006; doble repite | "¿Cómo voy en Estadística?" → "Estadística: 4.2." | Igual que (g1) (4.2 es Contabilidad; Estadística sin definitiva, cortes 3.9 y 4.1, acumulada 2.4) | D-64 |
| ESC-198 g3 | EST-004; doble invoca `consultar_cortes` con `asignatura` "Contabilidad" y luego responde | Q10 #5 → "Contabilidad 3.6 en el primer corte." | Sin cambios: 3.6 es su corte 1 y llega en el resultado de `consultar_cortes`. Sin esa llamada (doble repite): TNV; citas []; `numero_no_verificado` con `reintento: true` (el resumen no lleva cortes) | D-64, D-65 G1 |
| ESC-198 g4 | COORD-02; prelectura | Q10 #6 → "Valentina Castro: Microeconomía (1.8) y Estadística (2.3)." | Sin cambios; `respondida` | D-64 |
| ESC-198 g5 `@permisos` | COORD-02; prelectura sin detalle | "¿Cuál es el promedio de Camilo Rojas?" → "Camilo Rojas: Contabilidad 3.9." | TNV; `numero_no_verificado` (las aprobadas no se listan; SEG-10c va antes de D-64) | SEG-10c |
| ESC-198 h1 | EST-004; doble registra | Q10 #8 | La primera llamada trae en el bloque de la Sec. 3.1 exactamente "Requisitos de la beca por rendimiento académico (50% de descuento en la matrícula del periodo siguiente):" y los 3 requisitos; no "Descuento del 50% en la matrícula del periodo siguiente. Requisitos:" | D-58 |
| ESC-198 h2 | EST-001; doble registra | Q10 #7 | El bloque de la Sec. 1 trae el título y el texto originales ("La oficina de Bienestar atiende en el edificio B, piso 2:"), sin presentación | D-58 |
| ESC-198 i | Prueba unitaria; y EST-001 con el doble que copia EJ-2 | Bloque de ejemplos de ambos roles; "¿Cuál es mi promedio del periodo?" → "Tu promedio es 2.77. Definitivas: Física 2.7, Química 2.9." | 4 ejemplos con EJ-1…EJ-4; 0 cifras del bloque presentes en el índice, en plan §14 o en `preguntas_ejemplo.json`; `documento_ejemplo.md` fuera del índice. Abuso: TNV con `numero_no_verificado` | D-58 |
| ESC-198 j | Prueba unitaria | Presentación de la Sec. 3.1 frente a `reglamento/bienestar_y_becas.md` | Cada cifra (`50%`, `4.3`, `12`, `1`, `2`, `3`) y raíz (requ, beca, rend, acad, desc, matr, peri, sigu, prom, pond, igua, supe, ning, asig, perd, incl, inas, habe, meno, cred) está en el original; falla si se agrega una que no esté | D-58 |
| ESC-198 k1 | EST-004 | Q10 #5 → "Tu nota definitiva en Contabilidad es 3.9 (documento_ejemplo.md · Sección 2)." | "Tu nota definitiva en Contabilidad es 3.9."; `citas` exactamente [Sec. 3, Notas, Asistencia] | D-58 |
| ESC-198 k2 | EST-001 | Q10 #7 → "(documento_ejemplo.md · Sección 1)" | Texto vacío tras la limpieza → COMP-03; citas []; `sin_informacion` | D-58 |
| ESC-198 l1 | EST-001; sin citas escritas válidas | Q10 #2 → "Según el reglamento evaluación y calificaciones.md · Sección 4 tu promedio es 4.39." | "Tu promedio es 4.39." (`D` = "reglamento evaluación y calificaciones.md"; sale con "Según el"; mayúscula tras lo eliminado); documento exactamente [Sec. 4] | D-60 |
| ESC-198 l2 | Igual | Q10 #2 → "Tu promedio es 4.39. Esta información proviene del documento de evaluación." | "Tu promedio es 4.39." (atribución evaluada con las citas de 6.1); documento exactamente [Sec. 4]: la fuente sigue en `citas`. En (l1) y (l2), 0 atribuciones sin cita | D-60 |
| ESC-198 m | Núcleo | Programa ADM | Última línea exactamente "Reglas aplicadas a estos datos: reglamento_academico_2025.md · Artículo 5; reglamento_academico_2025.md · Artículo 6; reglamento_academico_2025.md · Artículo 20; evaluacion_y_calificaciones.md · Sección 3." | D-63 F3 |

- Texto real de ESC-198 b: "La nota definitiva de una asignatura se calcula como el promedio ponderado de los tres cortes, con los siguientes pesos: definitiva = 0.3 × corte 1 + 0.3 × corte 2 + 0.4 × corte 3 Esta nota se redondea a una cifra decimal: si la segunda cifra decimal es 5 o más, se redondea hacia arriba. Mientras un corte no esté calificado, la asignatura no tendrá una nota definitiva (evaluacion_y_calificaciones.md · Sección 3)." Raíces compartidas con la Sec. 2: cort, asig, tres, peso.
