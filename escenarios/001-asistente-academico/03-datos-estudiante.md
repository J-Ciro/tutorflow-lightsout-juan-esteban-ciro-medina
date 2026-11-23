# 03 · Datos del propio estudiante (CA-02)

> Spec: [`specs/001-asistente-academico/spec.md`](../../specs/001-asistente-academico/spec.md) · Sistema: [`SPEC.md`](../../SPEC.md)

Valores de la spec §11.4. En todos: `usoDatosEstudiante` es `true` y no hay datos de terceros.

### ESC-020 · Redondeo en el punto medio de la definitiva `@alterno`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuál es mi definitiva en Cálculo Integral?"
- **Entonces** la respuesta contiene "4.5" y no contiene "4.4" como definitiva (4.45 se redondea hacia arriba; el redondeo bancario daría 4.4). (RN-04)

### ESC-021 · Pérdida por inasistencia `@alterno`
- **Dado** el estudiante `EST-002`
- **Cuando** pregunta "¿Cuál es mi nota definitiva en Bases de Datos?"
- **Entonces** la respuesta indica que la perdió por inasistencia (9 de 48 faltas, más del 15%) con definitiva "0.0", aunque sus cortes darían 3.8; cita `Artículo 11` del 2025 y `Datos del estudiante`. (RN-07)

### ESC-022 · Promedio con pérdida por inasistencia y redondeo `@alterno`
- **Dado** el estudiante `EST-002`
- **Cuando** pregunta "¿Cuál es mi promedio del periodo?"
- **Entonces** la respuesta contiene "2.28" (27.3 / 12 = 2.275 → 2.28; Bases de Datos cuenta con 0.0). (RN-06, RN-08)

### ESC-023 · ¿Estoy en prueba académica? `@feliz`
- **Dado** el estudiante `EST-005`
- **Cuando** pregunta "¿Estoy en prueba académica?"
- **Entonces** la respuesta afirma que sí, con promedio "2.65" < 3.0 ((3.2·3 + 1.8·3 + 2.3·4 + 3.8·2) / 12 = 31.8 / 12 = 2.65); `citas` incluye `reglamento_academico_2025.md · Artículo 20`, garantizada aunque el modelo no la escriba por las citas de las reglas aplicadas ordenadas por relevancia (D-31, v1.9): la pregunta activa COMP-07 por (i) (`estoy` + `prueba academica`); bloque de `EST-005` [Sección 3, Sección 4, Artículo 11, `bienestar_y_becas.md · Sección 3.1`, Artículo 5, Artículo 6, Artículo 20] (Microeconomía 1.8 y Estadística 2.3 perdidas, ninguna por inasistencia, promedio < 3.0); raíces de la pregunta {`prue`, `acad`}; relevancia: `Artículo 20` 2, `Sección 3.1` 1 (`acad`, "académico" en el título), las demás 0 → si el modelo no escribe citas y responde "Sí, estás en prueba académica: tu promedio es 2.65.", citas de documento exactamente [`reglamento_academico_2025.md · Artículo 20`, `evaluacion_y_calificaciones.md · Sección 4`] (**ajuste de la v1.11, D-53:** la respuesta usa los campos prueba académica y promedio; ordenadas por relevancia, 2 y 0; sin ningún campo usado, la regla de D-49 daría [`Artículo 20`, `bienestar_y_becas.md · Sección 3.1`]; en la v1.9 y la v1.10 se agregaba además `evaluacion_y_calificaciones.md · Sección 3`), y citas de datos `Datos del estudiante · Notas` y `· Asistencia`. Con el orden fijo de la v1.8 eran [Sección 3, Sección 4, Artículo 11], sin el `Artículo 20` (detalle en ESC-177 h y ESC-194 b). (RN-11, COMP-02, D-31, D-49)

### ESC-024 · Asignatura sin definitiva `@alterno`
- **Dado** el estudiante `EST-006`
- **Cuando** pregunta "¿Cuál es mi definitiva en Estadística?"
- **Entonces** indica que aún no tiene definitiva porque falta calificar el corte 3; informa la nota acumulada "2.4" sobre el 60% evaluado (equivale a "4.0"); no inventa un valor para el corte 3; cita `evaluacion_y_calificaciones.md · Sección 3`. (RN-05, D-05)

### ESC-025 · Promedio excluye asignaturas sin definitiva `@alterno`
- **Dado** el estudiante `EST-006`
- **Cuando** pregunta "¿Cuál es mi promedio ponderado?"
- **Entonces** la respuesta contiene "4.05" y aclara que Estadística no entra porque no tiene definitiva. (RN-06)

### ESC-026 · Asistencia sin registro `@alterno`
- **Dado** el estudiante `EST-003`
- **Cuando** pregunta "¿Cuántas faltas tengo en Inglés III?"
- **Entonces** indica que no hay registro de inasistencias para esa asignatura y que debe solicitar la revisión a su coordinador; no responde "0" ni inventa un número; cita `Artículo 12` del 2025. (RN-09, D-04)

### ESC-027 · Faltas restantes `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cuántas faltas más puedo tener en Bases de Datos sin perderla?"
- **Entonces** la respuesta contiene "5" (máximo 7 de 48, lleva 2); cita `Artículo 11`. Desde la v1.7 recibe el resumen de COMP-07 por el activador (ii) (`puedo` + `faltas`) y por el (iii) (nombra Bases de Datos, matriculada); `usoDatosEstudiante: true`. (RN-13, COMP-07, D-30)

### ESC-028 · Elegible para la beca `@feliz`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Cumplo los requisitos de la beca por rendimiento académico?"
- **Entonces** responde que sí: promedio "4.39" ≥ 4.3, sin asignaturas perdidas y 12 créditos; cita `bienestar_y_becas.md · Sección 3.1`; `usoDatosEstudiante` es `true`. (RN-12)

### ESC-029 · No elegible para la beca `@alterno`
- **Dado** el estudiante `EST-004`
- **Cuando** pregunta "¿Puedo aplicar a la beca por rendimiento?"
- **Entonces** responde que no cumple: promedio "3.88" < 4.3. Desde la v1.7 recibe el resumen de COMP-07 por el activador (ii) (`puedo` + `beca`, sin genérico indefinido); `usoDatosEstudiante: true`. (RN-12, COMP-07, D-30)

### ESC-030 · Elegibilidad no determinable `@alterno`
- **Dado** el estudiante `EST-006`
- **Cuando** pregunta "¿Soy elegible para la beca por rendimiento?"
- **Entonces** responde que aún no se puede determinar porque Estadística no tiene definitiva; no afirma ni niega la elegibilidad. (D-20)

### ESC-031 · Habilitación propia `@alterno`
- **Dado** el estudiante `EST-005`
- **Cuando** pregunta "¿Qué materias puedo habilitar?"
- **Entonces** indica que puede habilitar Estadística (2.3) y no Microeconomía (1.8 < 2.0); cita `Artículo 6`. Activa el resumen de COMP-07 por el activador (ii): `puedo` + `habilitar` (término específico), sin `una materia` ni otro genérico indefinido (el genérico `materias` no impide la activación; solo no activa por sí mismo). El LLM recibe en la primera llamada el resumen de `EST-005` con Estadística 2.3 (perdida, habilitable) y Microeconomía 1.8 (perdida, no habilitable); `usoDatosEstudiante: true`; `citas` incluye `Datos del estudiante · Notas`. (RN-10, COMP-07, D-30)

### ESC-032 · Habilitación con pérdida por inasistencia `@alterno`
- **Dado** el estudiante `EST-002`
- **Cuando** pregunta "¿Puedo habilitar Bases de Datos?"
- **Entonces** responde que no, porque la perdió por inasistencia; cita `Artículo 6` y `Artículo 7`. Y sí puede habilitar Cálculo Integral (2.6) si lo pregunta. Activa el resumen de COMP-07 por el activador (ii) (`puedo` + `habilitar`) y por el (iii) (nombra Bases de Datos, `BD-203`, en la que está matriculado), así que está exenta del umbral: raíces {`habi`, `base`, `dato`}, `r = 3`, mínimo 2, y ninguna sección contiene 2 (el `Artículo 6` solo `habi`, el `Artículo 22` solo `dato`); hasta la v1.6 terminaba en COMP-03 sin LLM. El LLM recibe en la primera llamada el resumen de `EST-002` con Bases de Datos perdida por inasistencia (9/48, definitiva 0.0, no habilitable) y Cálculo Integral 2.6 (habilitable); `usoDatosEstudiante: true`; `citas` incluye `Datos del estudiante · Notas` y `Datos del estudiante · Asistencia`. (RN-07, RN-10, COMP-03 (1), COMP-07, D-30; ESC-172 a)

### ESC-033 · Pregunta en primera persona sin datos `@alterno`
- **Dado** el estudiante `EST-001`
- **Cuando** pregunta "¿Qué nota tengo en Química?"
- **Entonces** indica que no tiene esa asignatura matriculada en el periodo; no inventa una nota.

### ESC-034 · Mención de sí mismo por nombre `@alterno`
- **Dado** el estudiante `EST-001` (Laura Gómez)
- **Cuando** pregunta "¿Cuál es el promedio de Laura Gómez?"
- **Entonces** responde 200 con "4.39": mencionarse a sí mismo no es acceso a un tercero. El nombre completo propio es marcador de primera persona, así que se entrega el resumen de COMP-07 y la pregunta está exenta del umbral de relevancia (COMP-03 (1)); `usoDatosEstudiante: true`. (SEG-04, COMP-07, D-29)
