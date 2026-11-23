# 01 · Preguntas de ejemplo de Q10

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · Plan: [`plan.md`](../../specs/001-asistente-academico/plan.md) · CA-01

Las 10 preguntas de `preguntas_ejemplo.json`: el mínimo innegociable. Notación en [`README.md`](../README.md#notación). Datos de referencia en plan.md §14.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-001 `@feliz` | EST-001 | "¿Cuál es la nota mínima para aprobar una asignatura?" | 200; contiene "3.0", "0.0" y "5.0"; citas ⊇ [Art. 5]; uso false; el texto real del modelo no se bloquea como fuga (ESC-173 a) | RN-02 |
| ESC-002 `@feliz` | EST-001 | "¿Cuál es mi promedio ponderado del periodo?" | 200; contiene "4.39" y una línea por asignatura: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0; citas de documento exactamente [Sec. 4, Sec. 3] (nota ESC-002); datos [Notas, Asistencia]; uso true; sin terceros | RN-03, RN-04, RN-06, UX-02, COMP-07, D-53 |
| ESC-003 `@feliz` | EST-001 | "¿Cuántas faltas tengo en Bases de Datos?" | 200; contiene "2" y "48"; citas ⊇ [Datos del estudiante · Asistencia] y de documento exactamente [Art. 11] (raíces {falt, base, dato}: solo el Art. 11 comparte `falt`); uso true; un límite mencionado es 15% (máx. 7), nunca 20%; el resumen trae la línea exacta de ESC-189 | RN-07, RN-13, D-02, D-49 |
| ESC-004 `@feliz` | EST-001 | "¿Cómo se calcula la nota definitiva de una asignatura?" | 200; contiene "30%", "30%", "40%" y el redondeo a una cifra decimal; citas ⊇ [Sec. 2, Sec. 3] aunque el modelo escriba solo la Sec. 3 o ninguna (nota ESC-004); uso false; no activa COMP-07 | RN-03, RN-04, COMP-02, D-30, D-55 |
| ESC-005 `@feliz` | EST-004 | "¿Qué nota definitiva tengo en Contabilidad?" | 200; contiene "3.9" (3.6·0.3 + 3.9·0.3 + 4.1·0.4 = 3.89 → 3.9); citas ⊇ [Notas, Sec. 3]: con "Contabilidad: 3.9." o "Tu definitiva en Contabilidad es 3.9." documento exactamente [Sec. 3]; con "3.9." [Sec. 3, Sec. 4, Art. 11] y se entrega; uso true | RN-03, RN-04, D-56, D-63 F4 |
| ESC-006 `@feliz` | COORD-02 | "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?" | 200; contiene "Valentina Castro", "Microeconomía", "1.8", "Estadística" y "2.3"; no presenta con pérdidas a Camilo Rojas ni a Mateo Vargas ni a nadie de ING-SIS; citas ⊇ [Datos del programa · ADM, Art. 5] (nota ESC-006); uso true; llega por la prelectura (ESC-190) | SEG-02, RN-02, COMP-08, D-31 |
| ESC-007 `@feliz` | EST-001 | "¿Cuál es el horario de atención de Bienestar?" | 200; contiene "8:00", "17:00", "12:00", "sábado" y "edificio B, piso 2"; citas ⊇ [bienestar · Sec. 1]; uso false | COMP-02 |
| ESC-008 `@feliz` | EST-004 | "¿Qué requisitos tiene la beca por rendimiento académico?" | 200; contiene "4.3", "12 créditos", "50%" y "ninguna asignatura perdida"; citas ⊇ [Sec. 3.1], la única entregada (una cita escrita al Art. 20 se descarta); uso false; sin nombres, IDs ni notas (el Anexo no tiene efecto); el "3.1" de "Sección 3.1" no se verifica como cifra | RN-12, D-17, D-33, D-54 |
| ESC-009 `@alterno` | EST-001 | "¿Qué dice el reglamento sobre el uso de parqueaderos?" | 200 COMP-03; citas []; uso false; sin LLM; tokens 0; costo 0; `sin_informacion` (raíces {parq}, r = 1, mínimo 1, ninguna sección la contiene) | COMP-03 (1), D-29 |
| ESC-010 `@permisos` | EST-001 | "¿Cuál es la nota de Bases de Datos de Andrés Martínez?" | 403 NEG; citas ⊇ [Art. 22]; no contiene "3.8", "3.6", "3.9" ni "0.0"; tokens 0; sin LLM ni lecturas de EST-002; `denegada` | SEG-01, SEG-04 |
| ESC-197 a `@feliz` | Fase V: las 10 preguntas, 5 veces cada una con su usuario; LLM del entorno de evals (por defecto Ollama `qwen2.5:7b` con `OLLAMA_CONTEXT_LENGTH=8192`), `Llm:Temperatura` 0, semilla 1, prompt final | 5 de 5 con el código, la cifra y la cita de CA-01 (P9 COMP-03 con []; P10 403 con [Art. 22]) | CA-01, D-52, D-59 |
| ESC-197 b | Igual | Dato completo 5 de 5: P2 "4.39" y las 4 definitivas en la línea de su asignatura; P3 "2" y "48 clases"; P7 "8:00", "17:00", "12:00" y "edificio B, piso 2"; P8 "4.3", "12" y "50%" o "50 %" | D-44, D-46, D-58 |
| ESC-197 c | Igual | Citas de documento 5 de 5: P2 exactamente {Sec. 3, Sec. 4}; P4 ⊇ {Sec. 2, Sec. 3}; P7 exactamente {bienestar · Sec. 1}; P8 exactamente {Sec. 3.1} | D-55, D-56 |
| ESC-197 d | P6 | 5 de 5: `respondida` (por HTTP: 200 con una respuesta distinta de los 3 textos fijos); contiene "Valentina Castro", "Microeconomía", "1.8", "Estadística" y "2.3"; citas ⊇ [Art. 5, Datos del programa · ADM] | D-45, D-63 F8 |
| ESC-197 e | Las 50 respuestas | 0 oraciones terminadas en preposición o cláusula de atribución (plan §10 (7)); 0 referencias o atribuciones a un documento sin cita; 0 `documento_ejemplo.md`; 0 `contextoTruncado` | D-50, D-57, D-60 |
| ESC-197 f | Registro | `REPORTE.md`: por corrida, tabla con fecha UTC, commit, proveedor, modelo, `Llm:Temperatura`, `Llm:Semilla`, `OLLAMA_CONTEXT_LENGTH` (o "n/a"), pregunta P1…P10, aciertos `x/5` por criterio (código, cifra, dato completo, citas), tokens y `costoUsd` totales; debajo, el texto real de cada corrida fallida con su criterio (nota ESC-197 f) | D-52, D-63 F9 |

- **ESC-002:** las citas de documento salen de los campos promedio y definitiva, nunca Sec. 3.1 (ESC-177 a, ESC-194 f).
- **ESC-004:** secciones entregadas [Sec. 3, Sec. 2, Art. 5]; tema `pesos`/`30%`/`40%` y `redondea` (ESC-171, ESC-198 b, c).
- **ESC-006:** citas de documento [Art. 6, Sec. 3, Art. 5], con relevancia 3, 2 y 1.
- **ESC-197 f:** el runner imprime la tabla en Markdown; quien cierra la corrida la copia. Los umbrales no se bajan.
