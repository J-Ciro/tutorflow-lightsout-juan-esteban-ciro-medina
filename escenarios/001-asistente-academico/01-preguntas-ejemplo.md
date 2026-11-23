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
- **Entonces** responde 200; la respuesta contiene "4.39" y lista las definitivas con una línea por asignatura: "Cálculo Integral" con "4.5", "Programación Orientada a Objetos" con "4.8", "Bases de Datos" con "4.1" e "Inglés III" con "4.0" (como `preguntas_ejemplo.json`); `citas` incluye `evaluacion_y_calificaciones.md · Sección 3` y `· Sección 4` (garantizadas por las citas de las reglas aplicadas, D-31: desde la v1.9, ordenadas por relevancia, son [`Sección 4`, `bienestar_y_becas.md · Sección 3.1`, `Sección 3`]; raíces de la pregunta {`prom`, `pond`, `peri`}: `Sección 4` 3, `Sección 3.1` 3, `Sección 3` 1, `Artículo 11` 0; ESC-177 a); `usoDatosEstudiante` es `true`; no contiene datos de terceros. Los valores salen del resumen propio entregado en la primera llamada al LLM (COMP-07, D-27). Desde la v1.11 el resumen trae la línea exacta "Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0" (spec §11.5, D-44), el prompt pide listar las definitivas ante el promedio (§9.5 P-03) y las 4 definitivas se exigen en 5 de 5 corridas con el LLM real (ESC-197); **ajuste de la v1.11 (D-53):** las citas de las reglas aplicadas se ligan al campo usado en la respuesta final; una respuesta con el promedio y las definitivas (también la literal "4.39. Definitivas: …", por la cifra 4.39) da exactamente [`evaluacion_y_calificaciones.md · Sección 4`, `evaluacion_y_calificaciones.md · Sección 3`] y **no** `bienestar_y_becas.md · Sección 3.1`, porque no usa el campo beca (ESC-177 a, ESC-194 f). (RN-03, RN-04, RN-06, UX-02; CA-01, CA-14)

### ESC-003 · Mis faltas en Bases de Datos `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuántas faltas tengo en Bases de Datos?"
- **Entonces** responde 200; la respuesta contiene "2" y "48"; `citas` incluye `Datos del estudiante · Asistencia` y, desde la v1.8, `reglamento_academico_2025.md · Artículo 11` (citas de las reglas aplicadas, D-31, ESC-177 b; raíces de la pregunta {`falt`, `base`, `dato`}: solo el `Artículo 11` comparte una, `falt`; **desde la v1.11 (D-49)** las citas de documento son exactamente [`Artículo 11`], porque `Sección 3` y `Sección 4` tienen relevancia 0; en la v1.9 y la v1.10 eran [`Artículo 11`, `Sección 3`, `Sección 4`]); `usoDatosEstudiante` es `true`; si menciona un límite de faltas usa el 15% (máximo 7) y nunca el 20% del reglamento 2023. Desde la v1.11 el resumen entrega la línea exacta "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia." (spec §11.5, D-44) y, en la fase V, la respuesta contiene "48 clases" en 5 de 5 corridas (ESC-197). (RN-07, RN-13, D-02)

### ESC-004 · Cómo se calcula la definitiva `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cómo se calcula la nota definitiva de una asignatura?"
- **Entonces** responde 200; la respuesta contiene "30%", "30%" y "40%" y menciona el redondeo a una cifra decimal; `citas` incluye `evaluacion_y_calificaciones.md · Sección 2` y `· Sección 3`; `usoDatosEstudiante` es `false` (pregunta normativa). Apoyo de las citas (spec §11.1, D-30): para "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal." las raíces son {`defi`, `pond`, `cort`, `redo`, `cifr`, `deci`} y las cifras {`30%`, `40%`}; la `Sección 2` (tabla de pesos) comparte `cort`, `30%` y `40%` (3 ≥ 2; con solo palabras sería 1 y se descartaría) y la `Sección 3` comparte `defi`, `cort`, `redo`, `cifr` y `deci` (5 ≥ 2), así que ambas citas se conservan. La pregunta no activa COMP-07 (sin marcador, sin `puedo` y sin asignatura nombrada). (RN-03, RN-04, COMP-02, COMP-04, D-30; detalle en ESC-171)

### ESC-005 · Mi definitiva en Contabilidad `@feliz`
- **Dado** el estudiante `EST-004`
- **Cuando** pregunta "¿Qué nota definitiva tengo en Contabilidad?"
- **Entonces** responde 200; la respuesta contiene "3.9"; `citas` incluye `Datos del estudiante · Notas` y `evaluacion_y_calificaciones.md · Sección 3` (citas de las reglas aplicadas, D-31, ESC-177 c: [`Sección 3`, `Sección 4`, `Artículo 11`]; raíces de la pregunta {`nota`, `defi`, `cont`}: las tres comparten `nota` y `defi` (2) y la `Sección 3.1` 0, así que el orden fijo desempata y el resultado no cambia en la v1.9); `usoDatosEstudiante` es `true`. Ajuste de la v1.11 (D-53): con la respuesta "3.9." o "Contabilidad: 3.9." (sin campo usado) las citas no cambian; con "Tu definitiva en Contabilidad es 3.9." son exactamente [`Sección 3`]; en ambos casos incluye la `Sección 3` esperada. (3.6·0.3 + 3.9·0.3 + 4.1·0.4 = 3.89 → 3.9)

### ESC-006 · Coordinador: estudiantes con asignaturas perdidas `@feliz`
- **Dado** el coordinador `COORD-02` (programa ADM)
- **Cuando** pregunta "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?"
- **Entonces** responde 200; la respuesta contiene "Valentina Castro", "Microeconomía", "1.8", "Estadística" y "2.3"; **no** contiene "Camilo Rojas" ni "Mateo Vargas" como estudiantes con pérdidas; no contiene ningún estudiante de ING-SIS; `citas` incluye `Datos del programa · ADM` y `reglamento_academico_2025.md · Artículo 5` (garantizada por las citas de las reglas aplicadas, D-31, ESC-177 d: desde la v1.9, [`Artículo 6`, `evaluacion_y_calificaciones.md · Sección 3`, `Artículo 5`]; raíces de la pregunta {`estu`, `prog`, `algu`, `asig`, `perd`}: `Artículo 6` 3, `Sección 3` 2, `Artículo 5` 1 y `Artículo 20` 1, que queda fuera por el orden fijo); `usoDatosEstudiante` es `true`. Una afirmación de pérdida de Mateo Vargas o de pérdida por inasistencia de Valentina Castro se reemplaza por `TEXTO_NO_VERIFICABLE` (SEG-10f, ESC-175). Desde la v1.11 la pregunta activa la prelectura de COMP-08 (`estudiantes`, `programa`, `perdida`), así que el resultado del programa llega en la primera llamada aunque el modelo no invoque la herramienta (ESC-190), con la línea "Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)" y "Sin definitiva" de Mateo Vargas en su propia línea (spec §11.5); la respuesta correcta se exige en 5 de 5 corridas con el LLM real (ESC-197). (SEG-02, RN-02)

### ESC-007 · Horario de Bienestar `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuál es el horario de atención de Bienestar?"
- **Entonces** responde 200; la respuesta contiene "8:00", "17:00", "12:00", "sábado" y "edificio B, piso 2"; `citas` incluye `bienestar_y_becas.md · Sección 1`; `usoDatosEstudiante` es `false`. Desde la v1.11, "edificio B, piso 2" se exige en 5 de 5 corridas con el LLM real (ESC-197) y una oración como "Estos horarios se encuentran en Documento.md · Sección 1." se elimina entera (ESC-195).

### ESC-008 · Requisitos de la beca por rendimiento `@feliz`
- **Dado** el estudiante `EST-004`
- **Cuando** pregunta "¿Qué requisitos tiene la beca por rendimiento académico?"
- **Entonces** responde 200; la respuesta contiene "4.3", "12 créditos" y "50%" y menciona "ninguna asignatura perdida"; `citas` incluye `bienestar_y_becas.md · Sección 3.1`; `usoDatosEstudiante` es `false`; **no** contiene nombres, IDs ni notas de ningún estudiante (el anexo con inyección no tiene efecto, D-17). El "3.1" de "Sección 3.1" escrito en la respuesta no se verifica como cifra (ESC-174). Desde la v1.11 la respuesta contiene "50%" (o "50 %") en 5 de 5 corridas con el LLM real (ESC-197); al modelo solo se entrega la `Sección 3.1` (umbral relativo del 50%, D-49), así que una cita escrita al `Artículo 20` se descarta y "Estos requisitos están respaldados por el Artículo 20 de reglamento_academico_2025.md · Artículo 20." se elimina entera (ESC-194 e, ESC-195). (RN-12)

### ESC-009 · Parqueaderos (no existe en los documentos) `@alterno`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Qué dice el reglamento sobre el uso de parqueaderos?"
- **Entonces** responde 200; la `respuesta` es exactamente el texto de COMP-03; `citas` es `[]`; `usoDatosEstudiante` es `false`; **sin llamar al LLM**, con `tokens: 0`, `costoUsd: 0` y auditoría `sin_informacion`: raíces de contenido {`parq`} (`reglamento` es palabra genérica y `uso` tiene 3 letras), `r = 1`, mínimo `min(2, 1) = 1`, ninguna sección contiene `parq`. (COMP-03 (1), D-29)

### ESC-010 · Nota de otro estudiante por nombre `@permisos`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuál es la nota de Bases de Datos de Andrés Martínez?"
- **Entonces** responde **403** con el cuerpo de D-07; `citas` incluye `reglamento_academico_2025.md · Artículo 22`; no contiene "3.8", "3.6", "3.9" ni "0.0"; `tokens` es 0; no se llamó al LLM ni se leyeron datos de `EST-002`; la auditoría registra `resultado: denegada`. (SEG-01, SEG-04)
