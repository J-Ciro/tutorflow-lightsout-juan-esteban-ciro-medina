# 09 · Falsos positivos y suplantación

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · CA-12, CA-13

Un filtro que deniega preguntas legítimas también es un defecto; un bloqueo que alcanza a identidades legítimas, también. Notación en [`README.md`](../README.md#notación).

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

Salvo que se indique otra cosa: reloj controlado; conexión 10.0.0.5 sin `Proxy:IpsConfiables`; cada petición es "¿Cuál es mi promedio?" con un LLM doble; ventana 10 y máximo 3 (defecto); "min N" = minuto N desde la primera petición; "evento" = `multiples_identidades` (plan §12). La IP de origen tras un proxy está en `08-contrato-y-operacion.md` (ESC-047…049, 067…069, 095).

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-140 `@abuso` | Bloqueo desactivado (defecto) | EST-001, EST-002, EST-003 y EST-004 en los min 0, 1, 2 y 3 | Las 4: 200; en el min 3, exactamente 1 evento con `ipOrigen` "10.0.0.5", `usuarioIds` EST-001…EST-004 y `bloqueada: false` | SEG-16, D-21 |
| ESC-141 a `@abuso` | `Limites:BloquearMultiplesIdentidades` = `true` | EST-001…EST-003 en los min 0, 1 y 2; EST-004 en el min 3; EST-001 en el min 4 | Min 0–2: 200 y 0 eventos. Min 3: 429 con `Retry-After: 420` (600 − 180), 1 evento con `usuarioIds` EST-001…EST-004 y `bloqueada: true`, 0 llamadas al LLM y 0 lecturas de notas o asistencia. Min 4: 200 (ya registrada) | SEG-16, D-69 |
| ESC-141 b `@permisos` | Igual que (a) | EST-004 desde la conexión 10.0.0.6 en el min 3 | 200 y 0 eventos: el bloqueo es por IP de origen | SEG-16, SEG-19 |
| ESC-141 c `@permisos` `@abuso` | Igual que (a) | EST-999 desde 10.0.0.5 en el min 3 | 403 NEG `identidad_invalida`, no 429; 0 eventos: sin identidad validada no cuenta | SEG-03, SEG-16, D-08 |
| ESC-097 `@abuso` | Bloqueo desactivado (defecto) | EST-001…EST-005 en los min 0–4; EST-004 otra vez en el min 5 | Las 6: 200; exactamente 2 eventos: min 3 (`usuarioIds` EST-001…EST-004) y min 4 (EST-001…EST-005), ambos `bloqueada: false`; 0 en el min 5 | SEG-16, D-69 |
| ESC-098 a `@abuso` | Bloqueo `true` | EST-001…EST-003 en los min 0, 1 y 2; EST-004 en los min 3, 9, 9 min 59.5 s y 10 | 429 con `Retry-After` 420, 60 y 1 (⌈0.5⌉); en el min 10 vence EST-001 y EST-004 recibe 200: los intentos bloqueados no renovaron la ventana | SEG-16, D-69 |
| ESC-098 b | Igual que (a) | Eventos hasta el min 9 min 59.5 s | Exactamente 1 (el del min 3): el reintento de una identidad ya reportada no emite otro | SEG-16, D-69 |
| ESC-098 c `@abuso` | Igual que (a), tras el min 10 | EST-001 en el min 10 | 429 con `Retry-After: 60` (vence EST-002, registrada en el min 1) y 1 evento nuevo con `usuarioIds` EST-002, EST-003, EST-004, EST-001 | SEG-16, D-69 |
| ESC-099 a `@alterno` | Bloqueo `true` | EST-001…EST-003 en los min 0, 1 y 2; EST-001 en el min 8; EST-004 en el min 9 | Min 8: 200 y EST-001 se renueva. Min 9: 429 con `Retry-After: 120` (la más antigua es EST-002: vence en el min 11) | SEG-16, D-69 |
| ESC-099 b `@alterno` | Bloqueo `true`; `Limites:MaximoIdentidadesPorIp` = 1 | EST-001 en el min 0; EST-002 en el min 1; EST-001 en el min 1 | 200; 429 con `Retry-After: 540`; 200 | SEG-16, D-69 |
| ESC-142 `@abuso` | IP A con 61 preguntas en un minuto como EST-001; IP B como EST-001 | Una pregunta desde B | A recibe 429 y B 200: el límite es por IP | SEG-13 |
