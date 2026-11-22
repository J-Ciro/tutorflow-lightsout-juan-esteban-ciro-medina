# 04 · Coordinador (CA-02, CA-04)

> Spec: [`specs/001-asistente-academico/spec.md`](../../specs/001-asistente-academico/spec.md) · Sistema: [`SPEC.md`](../../SPEC.md)

### ESC-040 · Pérdidas en ING-SIS `@feliz`
- **Dado** el coordinador `COORD-01` (programa ING-SIS)
- **Cuando** pregunta "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?"
- **Entonces** la respuesta contiene "Andrés Martínez", "Cálculo Integral", "2.6", "Bases de Datos" y la pérdida por inasistencia; no contiene estudiantes de ADM. (RN-07, SEG-02)

### ESC-041 · Notas de un estudiante de su programa `@feliz`
- **Dado** el coordinador `COORD-02`
- **Cuando** pregunta "¿Cuál es el promedio de Valentina Castro?"
- **Entonces** responde 200 con "2.65"; `usoDatosEstudiante` es `true`; cita `Datos del programa · ADM`.

### ESC-042 · Estudiante de su programa por ID `@feliz`
- **Dado** el coordinador `COORD-01`
- **Cuando** pregunta "¿Cuál es el promedio de EST-003?"
- **Entonces** responde 200 con "4.11".

### ESC-043 · Estudiantes en prueba académica `@feliz`
- **Dado** el coordinador `COORD-02`
- **Cuando** pregunta "¿Quiénes de mi programa están en prueba académica?"
- **Entonces** la respuesta contiene "Valentina Castro" (2.65) y no contiene "Camilo Rojas" ni "Mateo Vargas"; cita `Artículo 20`.

### ESC-044 · Asignaturas pendientes de definitiva en su programa `@alterno`
- **Dado** el coordinador `COORD-02`
- **Cuando** pregunta "¿Hay estudiantes sin nota definitiva en alguna materia?"
- **Entonces** menciona a "Mateo Vargas" en "Estadística" (corte 3 sin calificar).

### ESC-045 · Coordinador pregunta por "mis notas" `@alterno`
- **Dado** el coordinador `COORD-01`
- **Cuando** pregunta "¿Cuál es mi promedio?"
- **Entonces** responde 200 indicando que como coordinador no tiene notas propias en el sistema; no devuelve datos de ningún estudiante; `usoDatosEstudiante` es `false`.

### ESC-046 · Asistencia sin registro vista por el coordinador `@alterno`
- **Dado** el coordinador `COORD-01`
- **Cuando** pregunta "¿Qué estudiantes tienen inasistencias sin registrar?"
- **Entonces** menciona a "Sofía Herrera" en "Inglés III". (RN-09)
