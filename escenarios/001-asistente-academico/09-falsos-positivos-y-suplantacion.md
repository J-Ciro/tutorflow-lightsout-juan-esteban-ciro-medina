# 09 · Falsos positivos y suplantación

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · CA-12, CA-08 (suplantación)

Un filtro que deniega preguntas legítimas también es un defecto. La suplantación solo se audita: nunca bloquea (D-72). Notación en [`README.md`](../README.md#notación).

## Preguntas legítimas que no reciben 403 (CA-12)

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-120 `@alterno` | EST-006 | "¿Cuál es mi nota en EST-103?" | 200 sobre su Estadística (sin definitiva, acumulada 2.4 sobre el 60%); uso true (`EST-103` es código de asignatura) | SEG-04a |
| ESC-121 `@alterno` | EST-004 | "¿Cuántas notas rojas tengo?" | 200; no tiene asignaturas perdidas ("rojas" no es el nombre completo de Camilo Rojas) | SEG-04a, D-09 |
| ESC-122 `@alterno` | EST-003 | "¿Cómo le pido a Marcela Ríos la revisión de mis faltas de Inglés III?" | 200; citas ⊇ [Art. 12] | SEG-04, D-09 |
| ESC-123 `@feliz` | EST-001 | "¿Quién es mi coordinador?" | 200; contiene "Marcela Ríos"; citas ⊇ [Datos del estudiante · Datos básicos] | COMP-02 |
| ESC-124 `@feliz` | COORD-02 | "¿Cuántos estudiantes de Administración de Empresas están en prueba académica?" | 200; bloque con `Programa ADM (Administración de Empresas): 3 estudiantes.`, `Cantidad de estudiantes en prueba académica: 1` y "Valentina Castro" (promedio 2.65 < 3.0) | SEG-04b, D-106 |
| ESC-125 `@alterno` | COORD-01 | "¿Qué trámite administrativo se hace para cancelar una asignatura?" | 200; contiene "semana 8" ("administrativo" no es el programa ADM) | SEG-04b |
| ESC-126 `@alterno` | EST-005 | "¿Puedo ir a tutorías con mis compañeros?" | 200; menciona las tutorías de martes y jueves | SEG-05 |
| ESC-127 `@alterno` | EST-001 | "¿Qué promedio mínimo pide la beca?" | 200; contiene "4.3"; uso false | COMP-04 |
| ESC-128 `@alterno` | EST-004 | "Mi hermano se llama Mateo, ¿cuál es mi promedio?" | 200; contiene "3.88"; sin datos de Mateo Vargas | D-09 |
| ESC-129 `@feliz` | COORD-01 | "¿Cómo va Andrés Martínez?" | 200; contiene "2.28" o menciona sus asignaturas perdidas; uso true | SEG-02 |

## Suplantación: solo auditoría (CA-08)

Salvo que se indique otra cosa: reloj controlado; conexión 10.0.0.5; cada petición es "¿Cuál es mi promedio?" con un LLM doble; ventana 10 y máximo 3 (defecto); "min N" = minuto N desde la primera petición; "evento" = `multiples_identidades` (plan §12). La tasa por IP está en `08-contrato-y-operacion.md` (ESC-047, 068, 142).

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-140 `@abuso` | Configuración por defecto | EST-001, EST-002, EST-003 y EST-004 en los min 0, 1, 2 y 3 | Las 4: 200; en el min 3, exactamente 1 evento con `ipOrigen` "10.0.0.5" y `usuarioIds` EST-001…EST-004, sin campo `bloqueada` | SEG-16, D-21, D-72 |
| ESC-141 a `@permisos` | Igual que ESC-140 en los min 0–2 | EST-004 desde la conexión 10.0.0.6 en el min 3 | 200 y 0 eventos: el registro es por IP de la conexión | SEG-16, D-72 |
| ESC-141 b `@permisos` `@abuso` | Igual que ESC-140 en los min 0–2 | EST-999 desde 10.0.0.5 en el min 3 | 403 NEG `identidad_invalida`, nunca 429; 0 eventos: sin identidad validada no cuenta | SEG-03, SEG-16, D-08 |
| ESC-097 `@abuso` | Configuración por defecto | EST-001…EST-005 en los min 0–4; EST-004 otra vez en el min 5 | Las 6: 200; exactamente 2 eventos: min 3 (`usuarioIds` EST-001…EST-004) y min 4 (EST-001…EST-005); 0 en el min 5 (ya reportada) | SEG-16, D-72 |

- ESC-098 · retirado (D-72)
- ESC-099 · retirado (D-72)
