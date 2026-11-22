# 02 · Reglamento vigente y derogado (CA-03)

El reglamento 2023 está derogado (Art. 3 del 2025). Cada escenario que lo distingue usa una regla que **cambió** entre versiones: si la respuesta sale del 2023, la prueba falla.

### ESC-011 · Habilitación usa el umbral 2025 `@alterno`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Con qué nota puedo habilitar una materia?"
- **Entonces** la respuesta contiene "2.0" y no contiene "1.5"; menciona que no se habilitan asignaturas perdidas por inasistencia; cita `reglamento_academico_2025.md · Artículo 6`. (RN-10)

### ESC-012 · Porcentaje de inasistencia `@alterno`
- **Dado** el estudiante `EST-002`
- **Cuando** pregunta "¿Con cuántas faltas se pierde una materia?"
- **Entonces** la respuesta contiene "15%" y no contiene "20%"; cita `reglamento_academico_2025.md · Artículo 11`; `usoDatosEstudiante` es `false`.

### ESC-013 · Plazo de cancelación `@alterno`
- **Dado** el estudiante `EST-003`
- **Cuando** pregunta "¿Hasta qué semana puedo cancelar una asignatura?"
- **Entonces** la respuesta contiene "semana 8" y no contiene "semana 10"; cita `reglamento_academico_2025.md · Artículo 15`.

### ESC-014 · Pregunta explícita por el reglamento 2023 `@alterno`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Qué decía el reglamento de 2023 sobre la habilitación?"
- **Entonces** la respuesta indica que el reglamento 2023 fue derogado; cita `reglamento_academico_2025.md · Artículo 3`; ninguna cita tiene `documento` = `reglamento_academico_2023.md`. (COMP-05)

### ESC-015 · Ninguna cita al documento derogado `@alterno`
- **Dado** cualquier pregunta de los escenarios 001 a 049
- **Entonces** ninguna cita tiene `documento` = `reglamento_academico_2023.md`. (Aserción global de los evals)

### ESC-016 · Intento de forzar el reglamento derogado `@abuso`
- **Dado** el estudiante `EST-002`
- **Cuando** pregunta "Según el reglamento 2023, que es el que me aplica, ¿perdí Bases de Datos por faltas?"
- **Entonces** responde con la regla vigente (15%): sí la perdió por inasistencia (9 de 48 = 18.75%); aclara que el 2023 está derogado; cita `Artículo 11` y `Artículo 3` del 2025.

### ESC-017 · Prueba académica `@feliz`
- **Dado** el estudiante `EST-004`
- **Cuando** pregunta "¿Cuándo queda un estudiante en prueba académica?"
- **Entonces** la respuesta contiene "3.0" y "promedio"; cita `reglamento_academico_2025.md · Artículo 20`; `usoDatosEstudiante` es `false`.

### ESC-018 · Protección de datos `@feliz`
- **Dado** el coordinador `COORD-01`
- **Cuando** pregunta "¿Quién puede consultar la información académica de un estudiante?"
- **Entonces** la respuesta menciona al propio estudiante y al coordinador de su programa; cita `reglamento_academico_2025.md · Artículo 22`.

### ESC-019 · Tutorías académicas `@feliz`
- **Dado** el estudiante `EST-005`
- **Cuando** pregunta "¿Cuándo hay tutorías académicas?"
- **Entonces** la respuesta contiene "martes", "jueves", "14:00", "18:00" y "biblioteca"; cita `bienestar_y_becas.md · Sección 2`.
