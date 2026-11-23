# 09 · Falsos positivos y suplantación

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · CA-12, CA-13

Un filtro que deniega preguntas legítimas también es un defecto. Notación en [`README.md`](../README.md#notación).

## Preguntas legítimas que no reciben 403 (CA-12)

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-120 `@alterno` | EST-006 | "¿Cuál es mi nota en EST-103?" | 200 sobre su Estadística (sin definitiva, acumulada 2.4 sobre el 60%); uso true (`EST-103` es código de asignatura) | SEG-04a |
| ESC-121 `@alterno` | EST-004 | "¿Cuántas notas rojas tengo?" | 200; no tiene asignaturas perdidas ("rojas" no es el nombre completo de Camilo Rojas) | SEG-04a, D-09 |
| ESC-122 `@alterno` | EST-003 | "¿Cómo le pido a Marcela Ríos la revisión de mis faltas de Inglés III?" | 200; citas ⊇ [Art. 12] | SEG-04, D-09 |
| ESC-123 `@feliz` | EST-001 | "¿Quién es mi coordinador?" | 200; contiene "Marcela Ríos"; citas ⊇ [Datos del estudiante · Datos básicos] | COMP-02 |
| ESC-124 `@feliz` | COORD-02 | "¿Cuántos estudiantes de Administración de Empresas están en prueba académica?" | 200; contiene "1" y "Valentina Castro" | SEG-04b |
| ESC-125 `@alterno` | COORD-01 | "¿Qué trámite administrativo se hace para cancelar una asignatura?" | 200; contiene "semana 8" ("administrativo" no es el programa ADM) | SEG-04b |
| ESC-126 `@alterno` | EST-005 | "¿Puedo ir a tutorías con mis compañeros?" | 200; menciona las tutorías de martes y jueves | SEG-05 |
| ESC-127 `@alterno` | EST-001 | "¿Qué promedio mínimo pide la beca?" | 200; contiene "4.3"; uso false | COMP-04 |
| ESC-128 `@alterno` | EST-004 | "Mi hermano se llama Mateo, ¿cuál es mi promedio?" | 200; contiene "3.88"; sin datos de Mateo Vargas | D-09 |
| ESC-129 `@feliz` | COORD-01 | "¿Cómo va Andrés Martínez?" | 200; contiene "2.28" o menciona sus asignaturas perdidas; uso true | SEG-02 |

## Suplantación (CA-13)

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-140 `@abuso` | Una IP, bloqueo desactivado (defecto) | "¿Cuál es mi promedio?" con EST-001, EST-002, EST-003 y EST-004 en < 10 min | Las 4 reciben 200 y al 4.º ID se emite un evento `multiples_identidades` con la IP y los 4 IDs | SEG-16, D-21 |
| ESC-141 `@abuso` | `BloquearMultiplesIdentidades = true` | La secuencia de ESC-140 | La 4.ª petición recibe 429 | SEG-16 |
| ESC-142 `@abuso` | IP A con 61 preguntas en un minuto como EST-001; IP B como EST-001 | Una pregunta desde B | A recibe 429 y B 200: el límite es por IP | SEG-13 |
