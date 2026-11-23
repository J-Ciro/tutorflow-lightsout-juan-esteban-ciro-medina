# 01 · Preguntas de ejemplo de Q10 (CA-01)

> Spec: [`specs/001-asistente-academico/spec.md`](../../specs/001-asistente-academico/spec.md) · Sistema: [`SPEC.md`](../../SPEC.md)

Las 10 preguntas de `preguntas_ejemplo.json`. Son el mínimo innegociable: las pruebas ocultas parten de aquí.

### ESC-001 · Nota mínima para aprobar `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuál es la nota mínima para aprobar una asignatura?"
- **Entonces** responde 200; la respuesta contiene "3.0" y "0.0" y "5.0"; `citas` incluye `reglamento_academico_2025.md · Artículo 5`; `usoDatosEstudiante` es `false`. El texto real del modelo no se bloquea como fuga (ESC-173 a). (RN-02)

### ESC-002 · Mi promedio ponderado `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuál es mi promedio ponderado del periodo?"
- **Entonces** responde 200; la respuesta contiene "4.39" y lista las definitivas con una línea por asignatura: "Cálculo Integral" con "4.5", "Programación Orientada a Objetos" con "4.8", "Bases de Datos" con "4.1" e "Inglés III" con "4.0" (como `preguntas_ejemplo.json`); `citas` incluye `evaluacion_y_calificaciones.md · Sección 3` y `· Sección 4`; `usoDatosEstudiante` es `true`; no contiene datos de terceros. Los valores salen del resumen propio entregado en la primera llamada al LLM (COMP-07, D-27). (RN-03, RN-04, RN-06, UX-02; CA-01, CA-14)

### ESC-003 · Mis faltas en Bases de Datos `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuántas faltas tengo en Bases de Datos?"
- **Entonces** responde 200; la respuesta contiene "2" y "48"; `citas` incluye `Datos del estudiante · Asistencia` y, desde la v1.8, `reglamento_academico_2025.md · Artículo 11` (citas de las reglas aplicadas, D-31, ESC-177); `usoDatosEstudiante` es `true`; si menciona un límite de faltas usa el 15% (máximo 7) y nunca el 20% del reglamento 2023. (RN-07, RN-13, D-02)

### ESC-004 · Cómo se calcula la definitiva `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cómo se calcula la nota definitiva de una asignatura?"
- **Entonces** responde 200; la respuesta contiene "30%", "30%" y "40%" y menciona el redondeo a una cifra decimal; `citas` incluye `evaluacion_y_calificaciones.md · Sección 2` y `· Sección 3`; `usoDatosEstudiante` es `false` (pregunta normativa). Apoyo de las citas (spec §11.1, D-30): para "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal." las raíces son {`defi`, `pond`, `cort`, `redo`, `cifr`, `deci`} y las cifras {`30%`, `40%`}; la `Sección 2` (tabla de pesos) comparte `cort`, `30%` y `40%` (3 ≥ 2; con solo palabras sería 1 y se descartaría) y la `Sección 3` comparte `defi`, `cort`, `redo`, `cifr` y `deci` (5 ≥ 2), así que ambas citas se conservan. La pregunta no activa COMP-07 (sin marcador, sin `puedo` y sin asignatura nombrada). (RN-03, RN-04, COMP-02, COMP-04, D-30; detalle en ESC-171)

### ESC-005 · Mi definitiva en Contabilidad `@feliz`
- **Dado** el estudiante `EST-004`
- **Cuando** pregunta "¿Qué nota definitiva tengo en Contabilidad?"
- **Entonces** responde 200; la respuesta contiene "3.9"; `citas` incluye `Datos del estudiante · Notas` y `evaluacion_y_calificaciones.md · Sección 3`; `usoDatosEstudiante` es `true`. (3.6·0.3 + 3.9·0.3 + 4.1·0.4 = 3.89 → 3.9)

### ESC-006 · Coordinador: estudiantes con asignaturas perdidas `@feliz`
- **Dado** el coordinador `COORD-02` (programa ADM)
- **Cuando** pregunta "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?"
- **Entonces** responde 200; la respuesta contiene "Valentina Castro", "Microeconomía", "1.8", "Estadística" y "2.3"; **no** contiene "Camilo Rojas" ni "Mateo Vargas" como estudiantes con pérdidas; no contiene ningún estudiante de ING-SIS; `citas` incluye `Datos del programa · ADM` y `reglamento_academico_2025.md · Artículo 5` (garantizada por las citas de las reglas aplicadas, D-31); `usoDatosEstudiante` es `true`. Una afirmación de pérdida de Mateo Vargas o de pérdida por inasistencia de Valentina Castro se reemplaza por `TEXTO_NO_VERIFICABLE` (SEG-10f, ESC-175). (SEG-02, RN-02)

### ESC-007 · Horario de Bienestar `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuál es el horario de atención de Bienestar?"
- **Entonces** responde 200; la respuesta contiene "8:00", "17:00", "12:00", "sábado" y "edificio B, piso 2"; `citas` incluye `bienestar_y_becas.md · Sección 1`; `usoDatosEstudiante` es `false`.

### ESC-008 · Requisitos de la beca por rendimiento `@feliz`
- **Dado** el estudiante `EST-004`
- **Cuando** pregunta "¿Qué requisitos tiene la beca por rendimiento académico?"
- **Entonces** responde 200; la respuesta contiene "4.3", "12 créditos" y "50%" y menciona "ninguna asignatura perdida"; `citas` incluye `bienestar_y_becas.md · Sección 3.1`; `usoDatosEstudiante` es `false`; **no** contiene nombres, IDs ni notas de ningún estudiante (el anexo con inyección no tiene efecto, D-17). El "3.1" de "Sección 3.1" escrito en la respuesta no se verifica como cifra (ESC-174). (RN-12)

### ESC-009 · Parqueaderos (no existe en los documentos) `@alterno`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Qué dice el reglamento sobre el uso de parqueaderos?"
- **Entonces** responde 200; la `respuesta` es exactamente el texto de COMP-03; `citas` es `[]`; `usoDatosEstudiante` es `false`; **sin llamar al LLM**, con `tokens: 0`, `costoUsd: 0` y auditoría `sin_informacion`: raíces de contenido {`parq`} (`reglamento` es palabra genérica y `uso` tiene 3 letras), `r = 1`, mínimo `min(2, 1) = 1`, ninguna sección contiene `parq`. (COMP-03 (1), D-29)

### ESC-010 · Nota de otro estudiante por nombre `@permisos`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuál es la nota de Bases de Datos de Andrés Martínez?"
- **Entonces** responde **403** con el cuerpo de D-07; `citas` incluye `reglamento_academico_2025.md · Artículo 22`; no contiene "3.8", "3.6", "3.9" ni "0.0"; `tokens` es 0; no se llamó al LLM ni se leyeron datos de `EST-002`; la auditoría registra `resultado: denegada`. (SEG-01, SEG-04)
