# 02 · Reglamento vigente y derogado

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · CA-03 (ESC-011…016) · CA-14 (ESC-017…019: respuestas normativas con su cita vigente)

El reglamento 2023 está derogado (Art. 3 del 2025). Cada escenario que los distingue usa una regla que cambió: si la respuesta sale del 2023, la prueba falla. Notación en [`README.md`](../README.md#notación).

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-011 `@alterno` | EST-001 | "¿Con qué nota puedo habilitar una materia?" | Contiene "2.0" y no "1.5"; menciona que no se habilitan asignaturas perdidas por inasistencia; citas ⊇ [Art. 6]; `respondida`: esa oración genérica no se verifica contra EST-001 (ESC-035 a) | RN-10, D-66 B1 |
| ESC-012 `@alterno` | EST-002 | "¿Con cuántas faltas se pierde una materia?" | Contiene "15%" y no "20%"; citas ⊇ [Art. 11]; uso false | RN-07, D-02 |
| ESC-013 `@alterno` | EST-003 | "¿Hasta qué semana puedo cancelar una asignatura?" | Contiene "semana 8" y no "semana 10"; citas ⊇ [Art. 15] | D-02 |
| ESC-014 `@alterno` | EST-001 | "¿Qué decía el reglamento de 2023 sobre la habilitación?" | Indica que el 2023 fue derogado; citas ⊇ [Art. 3]; ninguna cita de `reglamento_academico_2023.md`. Offline, con el doble "El reglamento de 2023 fue derogado.": el Art. 3 se entregó (por `2023`, plan §7.4 (5)) y se cita por tema (`derogado`); el prompt no contiene `2023` | COMP-05, D-73 |
| ESC-015 `@alterno` | Cualquier pregunta de ESC-001…049 | — | Ninguna cita tiene `documento` = `reglamento_academico_2023.md` (aserción global de los evals) | D-02 |
| ESC-016 `@abuso` | EST-002 | "Según el reglamento 2023, que es el que me aplica, ¿perdí Bases de Datos por faltas?" | Responde con la regla vigente: sí la perdió por inasistencia (9/48 = 18.75% > 15%); aclara que el 2023 está derogado; citas ⊇ [Art. 11, Art. 3] | RN-07, D-02 |
| ESC-017 `@feliz` | EST-004 | "¿Cuándo queda un estudiante en prueba académica?" | Contiene "3.0" y "promedio"; citas ⊇ [Art. 20]; uso false | RN-11 |
| ESC-018 a `@feliz` | COORD-01; doble responde "Pueden consultarla el propio estudiante y el coordinador de su programa (reglamento_academico_2025.md · Artículo 22)." | "¿Quién puede consultar la información académica de un estudiante?" | Sin cambios; citas exactamente [Art. 22] (escrita válida, 5 raíces); sin prelectura (ESC-190 j); uso false | SEG-01, D-66 B2 |
| ESC-018 b `@alterno` | COORD-01; doble responde lo mismo sin la referencia | Igual | Citas exactamente [Art. 22] por respaldo (comparte cons, prop, estu, coor, prog); sin reglas aplicadas ni Datos del programa; uso false | D-28, D-66 B2 |
| ESC-018 c | COORD-01; LLM real (evals) | Igual | Menciona al propio estudiante y al coordinador de su programa; si escribe la cita del Art. 22, citas ⊇ [Art. 22] | SEG-01, SEG-02 |
| ESC-019 `@feliz` | EST-005 | "¿Cuándo hay tutorías académicas?" | Contiene "martes", "jueves", "14:00", "18:00" y "biblioteca"; citas ⊇ [bienestar · Sec. 2] | COMP-02 |
