# 03 · Datos del propio estudiante: cálculos, resumen y formato

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · Plan: [`plan.md`](../../specs/001-asistente-academico/plan.md) §8, §14 · CA-02, CA-14, CA-18

Salvo que se indique otra cosa: uso true y sin datos de terceros. Notación en [`README.md`](../README.md#notación).

## Cálculos (CA-02)

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-020 `@alterno` | EST-001 | "¿Cuál es mi definitiva en Cálculo Integral?" | Contiene "4.5" y no "4.4" como definitiva (4.45 hacia arriba; el redondeo bancario daría 4.4) | RN-04 |
| ESC-021 `@alterno` | EST-002 | "¿Cuál es mi nota definitiva en Bases de Datos?" | Perdida por inasistencia (9 de 48, más del 15%) con definitiva "0.0", aunque los cortes darían 3.8; citas ⊇ [Art. 11, Datos del estudiante] | RN-07 |
| ESC-022 `@alterno` | EST-002 | "¿Cuál es mi promedio del periodo?" | Contiene "2.28" (27.3 / 12 = 2.275 → 2.28; Bases de Datos cuenta 0.0) | RN-06, RN-08 |
| ESC-023 `@feliz` | EST-005 | "¿Estoy en prueba académica?" | Sí, con "2.65" < 3.0 ((3.2·3 + 1.8·3 + 2.3·4 + 3.8·2) / 12 = 31.8 / 12); activa COMP-07 (i) (`estoy` + `prueba academica`); con "Sí, estás en prueba académica: tu promedio es 2.65." sin citas escritas, documento exactamente [Art. 20, Sec. 4] y datos [Notas, Asistencia] (nota ESC-023) | RN-11, COMP-02, D-31, D-53 |
| ESC-024 `@alterno` | EST-006 | "¿Cuál es mi definitiva en Estadística?" | Aún no tiene definitiva porque falta el corte 3; acumulada "2.4" sobre el 60% evaluado (equivale a "4.0"); no inventa el corte 3; citas ⊇ [Sec. 3] | RN-05, D-05 |
| ESC-025 `@alterno` | EST-006 | "¿Cuál es mi promedio ponderado?" | Contiene "4.05" y aclara que Estadística no entra porque no tiene definitiva | RN-06 |
| ESC-026 `@alterno` | EST-003 | "¿Cuántas faltas tengo en Inglés III?" | Sin registro de inasistencias; debe pedir la revisión a su coordinador; no responde "0" ni inventa; citas ⊇ [Art. 12] | RN-09, D-04 |
| ESC-027 `@feliz` | EST-001 | "¿Cuántas faltas más puedo tener en Bases de Datos sin perderla?" | Contiene "5" (máximo 7 de 48, lleva 2); citas ⊇ [Art. 11]; resumen por COMP-07 (ii) (`puedo` + `faltas`) y (iii) (Bases de Datos matriculada) | RN-13, COMP-07, D-30 |
| ESC-028 `@feliz` | EST-001 | "¿Cumplo los requisitos de la beca por rendimiento académico?" | Sí: "4.39" ≥ 4.3, sin asignaturas perdidas y 12 créditos; citas ⊇ [Sec. 3.1] | RN-12 |
| ESC-029 `@alterno` | EST-004 | "¿Puedo aplicar a la beca por rendimiento?" | No cumple: "3.88" < 4.3; resumen por COMP-07 (ii) (`puedo` + `beca`) | RN-12, COMP-07, D-30 |
| ESC-030 `@alterno` | EST-006 | "¿Soy elegible para la beca por rendimiento?" | Aún no se puede determinar porque Estadística no tiene definitiva; no afirma ni niega | RN-12, D-20 |
| ESC-031 `@alterno` | EST-005 | "¿Qué materias puedo habilitar?" | Puede habilitar Estadística (2.3) y no Microeconomía (1.8 < 2.0); citas ⊇ [Art. 6, Notas]; resumen por COMP-07 (ii) (`puedo` + `habilitar`; el genérico `materias` no impide) con Estadística habilitable y Microeconomía no habilitable | RN-10, COMP-07, D-30 |
| ESC-032 `@alterno` | EST-002 | "¿Puedo habilitar Bases de Datos?" | No, la perdió por inasistencia (9/48, definitiva 0.0); sí puede habilitar Cálculo Integral (2.6); citas ⊇ [Art. 6, Art. 7, Notas, Asistencia]; exenta del umbral por COMP-07 (ii) y (iii) (raíces {habi, base, dato}, mínimo 2, ninguna sección tiene 2) | RN-07, RN-10, COMP-03 (1), COMP-07, D-30 |
| ESC-033 `@alterno` | EST-001 | "¿Qué nota tengo en Química?" | Indica que no tiene esa asignatura en el periodo; no inventa una nota | COMP-03 |
| ESC-034 `@alterno` | EST-001 (Laura Gómez) | "¿Cuál es el promedio de Laura Gómez?" | 200 con "4.39": su nombre completo es marcador de primera persona; recibe el resumen y está exenta del umbral | SEG-04, COMP-07, D-29 |

- **ESC-023:** las citas de documento salen de los campos prueba académica y promedio (ESC-177 h, ESC-194 b).

## Resumen propio (CA-14)

Dobles espía; la API académica doble cuenta lecturas por recurso y estudiante.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-154 `@feliz` | EST-001; doble que nunca invoca herramientas y copia la línea de Bases de Datos del resumen | "¿Cuántas faltas tengo en Bases de Datos?" | 200; exactamente 1 llamada, que contiene el resumen delimitado con 2 faltas, 48 clases, máximo 7 (piso(0.15 × 48)) y 5 restantes; contiene "2" y "48"; citas ⊇ [Asistencia]; sin nombres ni IDs de otros | COMP-07, D-27, RN-13 |
| ESC-155 a `@permisos` | EST-004; doble sin herramientas | "¿Qué requisitos tiene la beca por rendimiento académico?" | Sin resumen; 0 lecturas de notas o asistencia; uso false | COMP-04, COMP-07 |
| ESC-155 b | EST-001 | "¿Hasta qué semana puedo cancelar una asignatura?" | Igual que (a): `puedo` solo con el genérico `asignatura` | COMP-07, D-30 |
| ESC-155 c | COORD-01 | "¿Cuál es mi promedio?" | Igual que (a): el coordinador nunca recibe resumen | COMP-07 |
| ESC-155 d | EST-001 | "¿Con qué nota puedo habilitar una materia?" | Igual que (a): contiene el genérico indefinido `una materia` | COMP-07, D-30 |
| ESC-155 e | COORD-01 | "¿Cómo van mis estudiantes en Bases de Datos?" | Sin resumen; `estudiantes` activa la prelectura: la primera llamada trae el programa ING-SIS (lecturas de EST-001, EST-002 y EST-003, ninguna de ADM); uso true | COMP-08, SEG-18, D-45 |
| ESC-155 f | EST-001 | "¿Qué dice el reglamento sobre el cálculo de la definitiva?" | Igual que (a): `calculo` es nombre parcial de Cálculo Integral; sin marcador ni `puedo` | COMP-07, D-30 |
| ESC-172 a `@alterno` | EST-002; doble sin herramientas ni citas que responde "Revisa el detalle de tus asignaturas." | "¿Puedo habilitar Bases de Datos?" | 200; ≥ 1 llamada y la primera trae el resumen propio con la asignatura; sin otros estudiantes; uso true; citas ⊇ [Notas]. Activadores (ii) y (iii); exenta del umbral | COMP-07, D-30 |
| ESC-172 b | EST-001 | "¿Cómo voy en Bases de Datos?" | Igual que (a): solo (iii) por nombre (`voy` es marcador, pero sin término de datos) | COMP-07 |
| ESC-172 c | EST-001 | "¿Cómo voy en BD-203?" | Igual que (a): (iii) por código; `r = 0`, así que sin la exención sería COMP-03 sin LLM | COMP-03 (1), COMP-07 |
| ESC-172 d | EST-005 | "¿Qué materias puedo habilitar?" | Igual que (a), con Estadística 2.3 habilitable y Microeconomía 1.8 no habilitable | COMP-07 (ii) |
| ESC-172 e `@permisos` | EST-001 | "¿Cómo voy en Contabilidad?" | Exactamente 1 lectura de notas de EST-001 (verificación) y 0 de asistencia; sin resumen; uso false; citas sin Datos del estudiante; auditoría con `notas` en `recursosConsultados`; vale también si termina en COMP-03 sin LLM (raíces {cont}, r = 1) | COMP-04, COMP-07 (iii) |
| ESC-172 f `@abuso` | EST-001 | "¿Puedo habilitar Bases de Datos de Andrés Martínez?" | 403 NEG; 0 llamadas; 0 lecturas: el filtro previo va antes de la verificación de matrícula | SEG-04 |
| ESC-172 g | EST-001 | "¿Cómo voy en Bases de Datos en el periodo 2025-1?" | 200 COMP-03; citas []; uso false; tokens 0; costo 0; 0 llamadas; 0 lecturas; `sin_informacion` (el periodo se evalúa con la coincidencia de catálogo, antes de leer) | COMP-03 (2) |

## Formato normativo (CA-18)

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-189 a `@feliz` `@alterno` | Núcleo | Resumen de EST-001 | Contiene "Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0" y "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia." | D-44 |
| ESC-189 b | Núcleo | Resumen de EST-002 | Contiene "Definitivas: Cálculo Integral 2.6, Programación Orientada a Objetos 3.5, Bases de Datos 0.0 (por inasistencia), Inglés III 3.2" y "Bases de Datos: faltas 9 de 48 clases programadas (faltas restantes: 0; máximo sin perder: 7). Perdida por inasistencia: superaste 7 inasistencias." | RN-07, D-44 |
| ESC-189 c | Núcleo | Resumen de EST-003 y de EST-006 | EST-003: "Inglés III: sin registro de inasistencias en 32 clases programadas (máximo sin perder: 4). Solicita la revisión a tu coordinador (Artículo 12)."; EST-006: "Sin definitiva (falta calificar un corte): Estadística (acumulada 2.4 sobre el 60% evaluado)" y una línea "Definitivas" sin Estadística | RN-05, RN-09, D-05 |
| ESC-189 g `@alterno` | Núcleo | Resumen de EST-006 y de EST-001 | EST-006: "Equivalente de la acumulada de Estadística sobre la nota completa: 4.0" (3.9·0.3 + 4.1·0.3 = 2.40; 2.40 / 0.60 = 4.0); ambos: "Nota mínima para aprobar: 3.0"; EST-001 sin línea "Equivalente" | D-05, D-66 C1 |
| ESC-189 d | Núcleo | Programa ADM | Exactamente el bloque de plan.md §8.3 más "Reglas aplicadas a estos datos" con Art. 5, Art. 6, Art. 20 y Sec. 3; el bloque de Mateo Vargas sin "perdida" ni "ninguna"; ninguna línea con "Perdida" y "Sin definitiva"; cada "Perdida por …" con 1 asignatura | D-44 |
| ESC-189 e | Núcleo | Programa ING-SIS | Contiene "Perdida por nota: Cálculo Integral (definitiva 2.6)", "Perdida por inasistencia: Bases de Datos (definitiva 0.0)", "Asistencia sin registro: Inglés III", "Estudiantes con asignaturas perdidas: Andrés Martínez (Cálculo Integral 2.6; Bases de Datos 0.0 por inasistencia)" y "Estudiantes en prueba académica: Andrés Martínez (promedio 2.28)" | D-44 |
| ESC-189 f | EST-001 (integración) | "¿Cuántas faltas tengo en Bases de Datos?" | La primera llamada al LLM contiene "faltas 2 de 48 clases programadas" y "Si superas 7 inasistencias pierdes la asignatura por inasistencia." | D-44 |
