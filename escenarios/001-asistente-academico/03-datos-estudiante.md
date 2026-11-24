# 03 · Datos del propio estudiante: cálculos, resumen y formato

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · Plan: [`plan.md`](../../specs/001-asistente-academico/plan.md) §8, §14 · CA-02, CA-14, CA-17, CA-18, CA-27

Salvo que se indique otra cosa: uso true y sin datos de terceros. Notación en [`README.md`](../README.md#notación). Con un activador, la respuesta termina con el bloque de COMP-09 (D-98): los valores de esta sección se comprueban en él.

## Cálculos (CA-02)

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-020 `@alterno` | EST-001 | "¿Cuál es mi definitiva en Cálculo Integral?" | Contiene "4.5" y no "4.4" como definitiva (4.45 hacia arriba; el redondeo bancario daría 4.4) | RN-04 |
| ESC-021 `@alterno` | EST-002 | "¿Cuál es mi nota definitiva en Bases de Datos?" | Perdida por inasistencia (9 de 48, más del 15%) con definitiva "0.0", aunque los cortes darían 3.8; citas ⊇ [Art. 11, Datos del estudiante] | RN-07 |
| ESC-022 `@alterno` | EST-002 | "¿Cuál es mi promedio del periodo?" | Contiene "2.28" (27.3 / 12 = 2.275 → 2.28; Bases de Datos cuenta 0.0) | RN-06, RN-08 |
| ESC-023 `@feliz` | EST-005 | "¿Estoy en prueba académica?" | Sí, con "2.65" < 3.0 ((3.2·3 + 1.8·3 + 2.3·4 + 3.8·2) / 12 = 31.8 / 12); activa COMP-07 (i) (`estoy` + `prueba academica`); con "Sí, estás en prueba académica: tu promedio es 2.65." sin citas escritas, documento exactamente [Art. 20, Sec. 4] y datos [Notas, Asistencia] (nota ESC-023) | RN-11, COMP-02, D-31, D-53 |
| ESC-024 `@alterno` | EST-006 | "¿Cuál es mi definitiva en Estadística?" | Aún no tiene definitiva porque falta el corte 3; acumulada "2.4" sobre el 60% evaluado (equivale a "4.0"); no inventa el corte 3; citas ⊇ [Sec. 3] | RN-05, D-05 |
| ESC-025 `@alterno` | EST-006 | "¿Cuál es mi promedio ponderado?" | Contiene "4.05" y aclara que Estadística no entra porque no tiene definitiva | RN-06 |
| ESC-026 `@alterno` | EST-003 | "¿Cuántas faltas tengo en Inglés III?" | Sin registro de inasistencias; debe pedir la revisión a su coordinador; no responde "0" ni inventa; desglose con la línea "sin registro" y `Total: sin registro`; citas ⊇ [Art. 12] | RN-09, D-04, D-92 |
| ESC-027 `@feliz` | EST-001 | "¿Cuántas faltas más puedo tener en Bases de Datos sin perderla?" | Contiene "5" (máximo 7 de 48, lleva 2); citas ⊇ [Art. 11]; resumen por COMP-07 (ii) (`puedo` + `faltas`) y (iii) (Bases de Datos matriculada) | RN-13, COMP-07, D-30 |
| ESC-028 `@feliz` | EST-001 | "¿Cumplo los requisitos de la beca por rendimiento académico?" | Sí: "4.39" ≥ 4.3, sin asignaturas perdidas y 12 créditos; citas ⊇ [Sec. 3.1] | RN-12 |
| ESC-029 `@alterno` | EST-004 | "¿Puedo aplicar a la beca por rendimiento?" | No cumple: "3.88" < 4.3; resumen por COMP-07 (ii) (`puedo` + `beca`) | RN-12, COMP-07, D-30 |
| ESC-030 `@alterno` | EST-006 | "¿Soy elegible para la beca por rendimiento?" | Aún no se puede determinar porque Estadística no tiene definitiva; no afirma ni niega | RN-12, D-20 |
| ESC-031 `@alterno` | EST-005 | "¿Qué materias puedo habilitar?" | Puede habilitar Estadística (2.3) y no Microeconomía (1.8 < 2.0); citas ⊇ [Art. 6, Notas]; resumen por COMP-07 (ii) (`puedo` + `habilitar`; el genérico `materias` no impide) con Estadística habilitable y Microeconomía no habilitable | RN-10, COMP-07, D-30 |
| ESC-032 `@alterno` | EST-002 | "¿Puedo habilitar Bases de Datos?" | No, la perdió por inasistencia (9/48, definitiva 0.0); sí puede habilitar Cálculo Integral (2.6); citas ⊇ [Art. 6, Art. 7, Notas, Asistencia]; exenta del umbral por COMP-07 (ii) y (iii) (raíces {habi, base, dato}, mínimo 2, ninguna sección tiene 2) | RN-07, RN-10, COMP-03 (1), COMP-07, D-30 |
| ESC-033 `@alterno` | EST-001 | "¿Qué nota tengo en Química?" | Química no está en el catálogo: bloque con la línea `Definitivas` de sus 4 asignaturas; 0 líneas ni cifras de Química; no inventa una nota | COMP-03, D-98 |
| ESC-034 `@alterno` | EST-001 (Laura Gómez) | "¿Cuál es el promedio de Laura Gómez?" | 200 con "4.39": su nombre completo es marcador de primera persona; recibe el resumen y está exenta del umbral | SEG-04, COMP-07, D-29 |

- **ESC-023:** las citas de documento salen de los campos prueba académica y promedio (ESC-177 h, ESC-194 b).

## Resumen propio (CA-14)

Dobles espía; la API académica doble cuenta lecturas por recurso y estudiante.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-154 `@feliz` | EST-001; doble que nunca invoca herramientas y copia la línea de Bases de Datos del resumen | "¿Cuántas faltas tengo en Bases de Datos?" | 200; 1 llamada, con el resumen: 2 faltas, 48 clases, máximo 7 (⌊0.15 × 48⌋) y 5 restantes; la copia se elimina entera (nota ESC-154) y la respuesta es el desglose de ESC-189 h; citas ⊇ [Asistencia] | COMP-07, D-27, RN-13, D-92, D-94 |
| ESC-155 a `@permisos` | EST-004; doble sin herramientas | "¿Qué requisitos tiene la beca por rendimiento académico?" | Sin resumen ni bloque (`beca` sola no activa); 0 lecturas de notas o asistencia; uso false; citas sin `Datos del estudiante` | COMP-04, COMP-07, D-98 |
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

- **ESC-154:** la oración copiada no se corta en `;` (D-94): la respuesta no contiene "máximo sin perder: 7)." suelto.

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

## Faltas con desglose y total (CA-27, D-92)

LLM doble sin herramientas; sin reintento por cifras de faltas (D-92): siempre 1 llamada. "Línea" = la línea de asistencia exacta de plan §8.3 (nota ESC-189 h–p). Datos de plan §14.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-189 h `@feliz` | EST-001; doble "Aquí está el detalle de tus faltas." | Q10 #3 | 1 llamada; esa oración, línea vacía, la línea de Bases de Datos y "Total: 2 faltas"; contiene "2 de 48 clases programadas" (Q10 #3); citas ⊇ [Datos del estudiante · Asistencia, Art. 11] | RF-02, D-92 |
| ESC-189 i `@feliz` | EST-002; doble "Tienes 30 faltas en total." | "¿Cuántas faltas tengo?" | Las 4 líneas, en el orden de los datos, y "Total: 15 faltas" (3 + 2 + 9 + 1); 0 "30" (la oración se elimina); 1 llamada; sin `reintento` | CA-27, D-92 |
| ESC-189 j `@alterno` | EST-001; doble "Este es tu detalle." | "¿Cuántas faltas tengo en Cálculo Integral y en Inglés III?" | Solo las líneas de Cálculo Integral (1 de 64; restantes 8; máximo 9) e Inglés III (1 de 32; restantes 3; máximo 4) y "Total: 2 faltas" | D-92 |
| ESC-189 k `@alterno` | EST-002; doble "Este es tu detalle." | "¿Cuántas faltas me quedan en Bases de Datos?" | La línea de Bases de Datos con "faltas restantes: 0" y "Perdida por inasistencia: superaste 7 inasistencias." (9 × 100 = 900 > 720) y "Total: 9 faltas" | RN-07, D-92 |
| ESC-189 l `@abuso` | EST-001; doble "Tienes 9 faltas en Bases de Datos." | Q10 #3 | 1 llamada; respuesta = solo el desglose de (h) (la oración se elimina, sin reintento); 0 "9 faltas"; `respondida` sin `reintento` | D-92 |
| ESC-189 m `@alterno` | EST-001 | "¿Con cuántas faltas se pierde una materia?" | Sin desglose ni "Total:"; 0 lecturas de asistencia; uso false; citas ⊇ [Art. 11] (ESC-012) | D-92 |
| ESC-189 n1 `@permisos` | COORD-01; doble "Este es el detalle de Andrés Martínez." | "¿Cuántas faltas tiene Andrés Martínez?" | Esa oración (lista cerrada), "Datos de Andrés Martínez (EST-002)", las 4 líneas en tercera persona y "Total: 15 faltas"; lecturas solo de EST-002 (notas y asistencia), sin prelectura; citas sin `Datos del programa`; 0 `tools` | RF-03, D-92, D-98 |
| ESC-189 n2 `@permisos` | COORD-02 | La pregunta de (n1) | 403 `tercero_mencionado` | SEG-02, D-92 |
| ESC-189 o1 `@alterno` | EST-001; doble "Las inasistencias se rigen por reglamento_academico_2025.md · Artículo 11." | Q10 #3 | Se conserva (cita válida, `se`, `2025` del archivo no es cifra) seguida del bloque; citas ⊇ [Art. 11] | D-94, D-98 |
| ESC-189 o2 `@alterno` | EST-001; doble "Tus inasistencias se rigen por reglamento_academico_2025.md · Artículo 11." | Q10 #3 | Se poda (`tus`): solo el bloque; citas ⊇ [Art. 11] | D-98 |
| ESC-189 p `@permisos` `@alterno` | COORD-01; doble "Ambos tienen faltas registradas." | "¿Cuántas faltas tienen Andrés Martínez y Sofía Herrera?" | La oración se poda; 2 bloques, "Datos de Andrés Martínez (EST-002)" con "Total: 15 faltas" y "Datos de Sofía Herrera (EST-003)" con "Total: 3 faltas (sin registro: Inglés III)"; lecturas solo de EST-002 y EST-003; sin prelectura | D-92, D-98 |

- **ESC-189 h–p:** la línea de Bases de Datos de EST-001 es "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia." (⌊0.15 × 48⌋ = 7; 7 − 2 = 5).

## Datos de personas escritos por el backend (CA-17, D-98)

LLM doble que repite su texto en un posible reintento; API académica doble espía. **Bloque** = el de plan §8.4, tras una línea vacía. **Podada** = la oración no aparece en la respuesta. Salvo otra cosa: 1 llamada, 0 `tools` en la petición, sin `reintento`, `respondida`.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-189 q1 `@abuso` | EST-002, una corrida por texto | "¿Cómo voy en mis materias?" (`mis` + `materias`; Estado por `voy`) → las 7 frases de S-01 sin Laura Gómez (nota ESC-189 q) | Las 7 podadas; respuesta = solo el bloque "Datos de Andrés Martínez (EST-002)" con `Estado de …` de las 4 asignaturas | COMP-09, D-98 |
| ESC-189 q2 `@abuso` `@permisos` | EST-002 | La pregunta de (q1) → "Laura Gómez reprobó Cálculo." | NEG del estudiante con `fuga_bloqueada` (SEG-10b, antes de la poda) | SEG-10b, D-98 |
| ESC-189 r `@abuso` | EST-001 (promedio 4.39, sin prueba académica), una corrida por texto | "¿Estoy en prueba académica?" → las 3 frases de S-02 (nota ESC-189 r) | Las 3 podadas (nombre propio); respuesta exacta "Datos de Laura Gómez (EST-001)\nPromedio ponderado del periodo: 4.39\nPrueba académica: no"; 0 "2.28" | COMP-09, D-98 |
| ESC-189 s1 `@abuso` `@permisos` | COORD-01 | "¿Cuántas faltas tiene Laura Gómez?" → "Estas son las faltas de Andrés Martínez:" | Podada (Andrés es autorizado: no es SEG-10b); respuesta = "Datos de Laura Gómez (EST-001)", sus 4 líneas en tercera persona y "Total: 4 faltas" (1 + 0 + 2 + 1); 0 "Andrés"; lecturas solo de EST-001 | COMP-09, D-98 |
| ESC-189 s2 `@abuso` | COORD-01 | Igual → "Andrés Martínez nunca ha faltado a clase." | Igual que (s1) | COMP-09, D-98 |
| ESC-189 t `@feliz` | COORD-02; doble "Este es el listado." | Q10 #6 | "Este es el listado." y el bloque que empieza exactamente por "Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)" (1.9·0.3 + 1.6·0.3 + 1.9·0.4 = 1.81 → 1.8; 2.4·0.3 + 2.2·0.3 + 2.3·0.4 = 2.30); citas exactamente [Art. 5, Datos del programa · ADM]; 0 "Sec. 3" | COMP-08, COMP-09, D-98 |
| ESC-189 u `@alterno` | EST-004 y EST-001, una corrida por pregunta | Q10 #8 · Q10 #1 · Q10 #4 | Sin bloque ("Datos de" ausente), 0 lecturas de notas o asistencia, uso false, citas sin `Datos del estudiante`; texto del modelo con (c), (f), (g) y (e) como antes | COMP-09, D-98 |
| ESC-189 v `@feliz` | EST-004; doble "Tu definitiva en Contabilidad es 3.9 (reglamento_academico_2025.md · Artículo 5)." | Q10 #5 | Oración podada; respuesta exacta "Datos de Camilo Rojas (EST-004)\nDefinitivas: Contabilidad 3.9" (3.6·0.3 + 3.9·0.3 + 4.1·0.4 = 3.89 → 3.9); citas de documento exactamente [Sec. 3]; 0 Art. 5 | COMP-09, D-98, D-101 |
| ESC-189 w1 `@alterno` | EST-002; doble "Una asignatura se pierde con definitiva menor a 3.0 (reglamento_academico_2025.md · Artículo 5). Perdiste por poco." | "¿Por qué perdí Cálculo Integral?" | Queda la 1.ª oración (citada, `se`, sin asignatura) y se poda la 2.ª | COMP-09, D-98 |
| ESC-189 w2 `@alterno` | Igual que (w1) | Igual | Bloque con "Estado de Cálculo Integral: perdida con definitiva 2.6, habilitable" (2.63 → 2.6 ≥ 2.0); citas ⊇ [Art. 5, Art. 6] | RN-10, D-98 |
| ESC-189 x `@permisos` | EST-001 y COORD-02; doble que intenta invocar `consultar_programa` | "¿Cuál es mi promedio?" · Q10 #6 | La petición al proveedor no lleva `tools`; 0 lecturas de otros estudiantes (S-08); respuesta con el bloque | SEG-06, D-98 |
| ESC-189 y `@alterno` | EST-001; doble "Estos son tus datos:" · "Estos son los datos de Laura Gómez." · "Estos son los datos de Andrés Martínez." | "¿Cuál es mi promedio?" | La 1.ª y la 2.ª se conservan (lista cerrada, persona del bloque); la 3.ª da NEG con `fuga_bloqueada` (SEG-10b antes de la poda) | D-98, SEG-10b |
| ESC-189 z `@abuso` | EST-001; doble "Se registran nueve faltas (reglamento_academico_2025.md · Artículo 11)." · "Bases de Datos se aprueba con 3.0 (reglamento_academico_2025.md · Artículo 5)." | Q10 #3 | Ambas podadas (número en letras; asignatura del catálogo); solo el bloque de ESC-189 h | D-98 |
| ESC-189 aa `@alterno` | EST-001; doble "Se pierde con más del 15% de inasistencias (reglamento_academico_2025.md · Artículo 11)." · igual con "20%" | Q10 #3 | La 1.ª se conserva y pasa SEG-10c (15% en el Art. 11); la 2.ª se conserva y da TNV `numero_no_verificado` con `reintento: true` (20% no está en lo entregado) | SEG-10c, D-98 |
| ESC-189 ab `@alterno` | EST-004 | "¿Cuál es mi nota en Cálculo Integral?" | Bloque "Datos de Camilo Rojas (EST-004)\nSin matrícula en el periodo 2026-2: Cálculo Integral"; 0 cifras | COMP-09, D-98 |

- **ESC-189 q:** "Andrés Martínez aprobó todas sus asignaturas y no está en prueba académica." · "Andrés no perdió Cálculo Integral." · "no ha perdido ninguna materia" · "lleva nueve faltas en Cálculo Integral" · "promedio de cuatro punto tres nueve" · "Laura Gómez reprobó Cálculo." (SEG-10b: NEG) · "Pasaste Cálculo Integral." · "Tienes beca por rendimiento.".
  - Las 7 sin Laura Gómez se podan; la de Laura Gómez (no autorizada para EST-002) da NEG del estudiante con `fuga_bloqueada` (SEG-10b, antes de la poda).
  - Estados del bloque: Cálculo Integral `perdida con definitiva 2.6, habilitable`; Programación Orientada a Objetos `aprobada con definitiva 3.5`; Bases de Datos `perdida por inasistencia, no habilitable`; Inglés III `aprobada con definitiva 3.2`.
- **ESC-189 r:** "Laura Gómez si está en prueba académica." · "Para Laura Gómez se requiere 2.28 en Cálculo Integral; tiene 2.28." · "La definitiva mínima es 2.28 para Laura Gómez.".

## Datos de referencia (plan §14)

RN-03…RN-13 sobre `mock-api/datos.json`. Coordinadores: `COORD-01` Marcela Ríos (ING-SIS) y `COORD-02` Julián Pardo (ADM).

| Est. | Asignatura | Cr | Cortes | Definitiva | Faltas / clases (máx.) | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| EST-001 Laura Gómez (ING-SIS) | Cálculo Integral | 4 | 4.2 / 4.5 / 4.6 | 4.45 → **4.5** | 1/64 (9) | Aprobada |
| | Programación Orientada a Objetos | 3 | 4.8 / 4.6 / 4.9 | 4.78 → **4.8** | 0/48 (7) | Aprobada |
| | Bases de Datos | 3 | 4.0 / 3.8 / 4.4 | **4.1** | 2/48 (7) | Aprobada |
| | Inglés III | 2 | 3.9 / 4.1 / 4.0 | **4.0** | 1/32 (4) | Aprobada |
| | **Promedio** 52.7 / 12 = 4.3916 → **4.39** · 12 créditos · beca: **elegible** | | | | | |
| EST-002 Andrés Martínez (ING-SIS) | Cálculo Integral | 4 | 2.5 / 2.8 / 2.6 | 2.63 → **2.6** | 3/64 | Perdida (habilitable) |
| | Programación Orientada a Objetos | 3 | 3.5 / 3.2 / 3.6 | 3.45 → **3.5** | 2/48 | Aprobada |
| | Bases de Datos | 3 | 3.8 / 3.6 / 3.9 | 3.78 → **0.0** | **9/48 = 18.75%** | **Perdida por inasistencia** (no habilitable) |
| | Inglés III | 2 | 3.0 / 3.4 / 3.1 | 3.16 → **3.2** | 1/32 | Aprobada |
| | **Promedio** 27.3 / 12 = 2.275 → **2.28** · **prueba académica** | | | | | |
| EST-003 Sofía Herrera (ING-SIS) | Cálculo Integral | 4 | 4.0 / 3.9 / 4.2 | 4.05 → **4.1** | 2/64 | Aprobada |
| | Programación Orientada a Objetos | 3 | 3.7 / 4.0 / 3.8 | 3.83 → **3.8** | 1/48 | Aprobada |
| | Bases de Datos | 3 | 4.1 / 4.3 / 4.0 | 4.12 → **4.1** | 0/48 | Aprobada |
| | Inglés III | 2 | 4.5 / 4.4 / 4.7 | 4.55 → **4.6** | **null**/32 | Aprobada · sin registro de asistencia |
| | **Promedio** 49.3 / 12 = 4.1083 → **4.11** | | | | | |
| EST-004 Camilo Rojas (ADM) | Contabilidad | 3 | 3.6 / 3.9 / 4.1 | 3.89 → **3.9** | 0/48 | Aprobada |
| | Microeconomía | 3 | 3.4 / 3.0 / 3.5 | 3.32 → **3.3** | 2/48 | Aprobada |
| | Estadística | 4 | 4.0 / 4.2 / 3.9 | 4.02 → **4.0** | 3/64 | Aprobada |
| | Comunicación Oral y Escrita | 2 | 4.5 / 4.6 / 4.4 | 4.49 → **4.5** | 1/32 | Aprobada |
| | **Promedio** 46.6 / 12 = 3.8833 → **3.88** · beca: no elegible (< 4.3) | | | | | |
| EST-005 Valentina Castro (ADM) | Contabilidad | 3 | 3.2 / 3.0 / 3.3 | 3.18 → **3.2** | 4/48 | Aprobada |
| | Microeconomía | 3 | 1.9 / 1.6 / 1.9 | 1.81 → **1.8** | 5/48 | Perdida (no habilitable, < 2.0) |
| | Estadística | 4 | 2.4 / 2.2 / 2.3 | **2.3** | 6/64 | Perdida (habilitable) |
| | Comunicación Oral y Escrita | 2 | 3.8 / 3.5 / 3.9 | 3.75 → **3.8** | 2/32 | Aprobada |
| | **Promedio** 31.8 / 12 = **2.65** · **prueba académica** | | | | | |
| EST-006 Mateo Vargas (ADM) | Contabilidad | 3 | 4.1 / 4.0 / 4.3 | 4.15 → **4.2** | 1/48 | Aprobada |
| | Microeconomía | 3 | 3.6 / 3.8 / 3.9 | 3.78 → **3.8** | 0/48 | Aprobada |
| | Estadística | 4 | 3.9 / 4.1 / **null** | **Sin definitiva** · acumulada 2.4 sobre el 60% (≈ 4.0) | 2/64 | Pendiente |
| | Comunicación Oral y Escrita | 2 | 4.2 / 4.0 / 4.4 | 4.22 → **4.2** | 0/32 | Aprobada |
| | **Promedio** (8 créditos con definitiva) 32.4 / 8 = **4.05** · beca: no determinable aún | | | | | |
