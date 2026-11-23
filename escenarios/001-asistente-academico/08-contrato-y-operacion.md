# 08 · Contrato HTTP, auditoría, límites y operación

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · Plan: [`plan.md`](../../specs/001-asistente-academico/plan.md) §12, §13.1 · Contrato: [`constitution.md`](../../constitution.md) §2 · CA-07…CA-11

Notación en [`README.md`](../README.md#notación).

## Contrato

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-100 `@contrato` | — | `GET /api/salud` | 200 con exactamente `{"estado":"ok"}`, aunque el LLM no esté configurado | RF-09 |
| ESC-101 `@contrato` | — | Cualquier respuesta 200 o 403 del asistente | Exactamente `respuesta` (string), `citas` (array de `{documento, seccion}`, nunca `null`), `usoDatosEstudiante` (boolean), `tokens` (entero ≥ 0) y `costoUsd` (número ≥ 0), en camelCase | Contrato |
| ESC-102 `@contrato` | — | Cuerpo `{"pregunta": ` | 400 `ProblemDetails` sin stack trace | Contrato |
| ESC-103 `@contrato` | — | Falta `pregunta` o `usuario`, `usuario.id` es `null`, o `pregunta` es `""` o solo espacios | 400 | Contrato |
| ESC-104 `@contrato` | — | `pregunta` es un número o `usuario` un string | 400 | Contrato |
| ESC-105 `@contrato` | — | `GET /api/asistente/preguntar` | 405 | Contrato |
| ESC-106 `@contrato` | — | Respuesta 200 que llamó al LLM; respuestas 403 y 400 | 200: tokens > 0, costo > 0 y `costoUsd` = entrada × precio de entrada + salida × precio de salida (tolerancia 1e-6). 403 y 400: tokens 0 y costo 0 | RF-07 |
| ESC-107 `@contrato` | — | `"usuario": {"rol":"estudiante","id":"EST-001","programas":["ADM"],"esAdmin":true}` | Los campos extra se ignoran y no otorgan permisos | SEG-08 |
| ESC-111 `@contrato` | — | Cualquier respuesta | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`; nunca `Access-Control-Allow-Origin: *` | SEG-14 |
| ESC-112 `@contrato` | — | Excepción no controlada | 500 `ProblemDetails` con `traceId` (UUID v7 = `X-Correlation-Id`, ESC-151), sin stack trace, tipos .NET ni rutas | SEG-12 |
| ESC-150 `@contrato` | EST-001 | Cuerpo válido con `Content-Type: text/plain`, con `application/x-www-form-urlencoded` o sin encabezado | 415 sin stack trace; 0 llamadas al LLM y a la API; auditoría `invalida` con `pregunta: [NO_EVALUADA]`. Con `application/json` o `application/json; charset=utf-8`, 200 | Contrato |
| ESC-151 `@contrato` | — | Una petición 200 (ESC-002), 400 (ESC-102), 403 (ESC-050), 415 (ESC-150) y 500 (ESC-112) | `X-Correlation-Id` UUID en minúsculas con versión `7` y variante `8`, `9`, `a` o `b`; igual al `correlationId` del evento y al `traceId` de los `ProblemDetails`; su marca de tiempo difiere < 1 s del `timestamp`; los 5 valores son distintos | D-26 |
| ESC-152 `@abuso` | EST-001 | "¿Cuál es mi promedio?" con `X-Correlation-Id: 00000000-0000-7000-8000-000000000001` | La respuesta trae otro UUID v7; ningún log ni evento contiene el valor del cliente | D-26 |

## Resiliencia, límites y proveedor

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-108 `@operacion` | EST-001; API académica caída | Su promedio | 503 `ProblemDetails` genérico en < 7 s; no inventa; `error` | RNF-02 |
| ESC-109 `@operacion` | LLM sin respuesta en 20 s | Cualquiera | 503; sin URL, modelo ni llave en el cuerpo (varias llamadas lentas: ESC-158) | SEG-13 |
| ESC-110 `@operacion` | Sin `LLM_API_KEY` y Ollama caído, o llave de prefijo desconocido sin `LLM_URL_BASE` | — | Arranca; salud 200; una pregunta que necesita el LLM da 503; una denegada sigue dando 403 | plan §4.1 |
| ESC-119 `@operacion` | `LLM_API_KEY` con `sk-ant-`, `sk-or-`, `gsk_`, `AIza`, `sk-proj-` y sin llave | — | Proveedor, en orden: Anthropic, OpenRouter, Groq, Gemini, OpenAI y Ollama; con `LLM_PROVEEDOR`, gana la variable; el log muestra proveedor y modelo, nunca la llave | D-16 |
| ESC-158 `@operacion` | EST-001; doble con 5 llamadas de 6 s y las 4 primeras invocan herramienta | "¿Cuál es mi promedio?" | 503 genérico en < 21 s (la 4.ª empieza en 18 s y se cancela en 20 s); como máximo 4 llamadas; `error` | SEG-13, RNF-02 |
| ESC-159 `@abuso` | Presupuesto 0.010 USD, `Llm:ReservaPorPeticionUsd` 0.004; doble que retiene y cuesta 0.003 | 5 peticiones simultáneas de EST-001; se libera tras los 503 | 2 responden 200 y 3 responden 503 sin LLM (2 × 0.004 = 0.008 ≤ 0.010; 3 × 0.004 = 0.012 > 0.010); 2 llamadas; gasto 0.006 USD | SEG-13 |
| ESC-160 `@abuso` | Proveedor doble que responde 302 hacia otro servidor doble | EST-001: "¿Cuál es mi promedio?" | 503 genérico; el servidor de `Location` recibe 0 peticiones; la llave no sale a otro host | SEG-07 |
| ESC-180 a `@alterno` `@abuso` `@operacion` | Presupuesto 0.03; doble de plan §4.4 (salida 200); sin precio; `Llm:ReservaSinPrecioUsd` 0.01 | EST-001, 4 veces "¿Cuál es mi promedio?" | Peticiones 1–3: 200, tokens = lo reportado, costo 0, `precioDesconocido: true`, 0.01 de gasto cada una (3 × 0.01 = 0.03); la 4.ª: 503 sin LLM (0.03 + 0.01 > 0.03); 3 llamadas | SEG-13, D-39 |
| ESC-180 b | Igual con `Llm:ReservaSinPrecioUsd` 0 (se usa 0.01 con advertencia al arrancar) | Igual | Igual que (a) | SEG-13, D-39, D-40 |
| ESC-180 c `@operacion` | Igual con `ollama` a precio 0 / 0 | Igual | Las 4 responden 200 con costo 0; ningún `precioDesconocido`; 4 llamadas | D-39 |

## Auditoría

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-113 `@operacion` | — | Las peticiones de ESC-002, ESC-009, ESC-010 y ESC-103 | Exactamente un evento cada una con los campos de plan §12 y `resultado` `respondida`, `sin_informacion`, `denegada` e `invalida` | RF-08 |
| ESC-114 `@operacion` | — | ESC-002 | `estudiantesConsultados: ["EST-001"]`, `recursosConsultados` con `notas`; sin valores ("4.39", "4.5"…) | D-13 |
| ESC-115 `@operacion` | — | ESC-006 | `estudiantesConsultados` solo con EST-004, EST-005 y EST-006; `recursosConsultados` con `programa` | RF-08 |
| ESC-118 `@operacion` | — | ESC-010 | `pregunta` contiene `[TERCERO]` y no "Andrés Martínez"; `ipOrigen` presente | RF-08 |
| ESC-143 `@operacion` | — | ESC-003 | `datosConsultados` con `{estudianteId: EST-001, programaId: ING-SIS, recurso: asistencia, asignatura: BD-203}`; sin "48" ni el número de faltas | D-13 |
| ESC-144 `@operacion` | `Auditoria:Ruta` configurada | 3 preguntas (200, 403 y 400) | `auditoria-<fecha UTC>.jsonl` con exactamente 3 líneas nuevas idénticas a la consola; con Compose, el archivo sobrevive a `down` y `up` | D-25 |
| ESC-148 `@abuso` | Registro de personas en caché | "Notas de Andrés Martínez y EST-002" con las variantes (a)–(d) de la nota ESC-148 | (a)–(c): `pregunta` exactamente "Notas de [TERCERO] y [TERCERO]"; (d): exactamente `[NO_EVALUADA]`; ningún evento contiene "Andrés Martínez" ni "EST-002" en `pregunta` | RF-08 |
| ESC-153 `@abuso` | COORD-01; doble con la herramienta de detalle y `"EST-004"` | "¿Qué estudiantes de mi programa perdieron materias?" | 403 `herramienta_id_ajeno` con `idSolicitado: "EST-004"`; EST-004 fuera de `estudiantesConsultados` y `datosConsultados`; variantes en la nota ESC-153 | SEG-06, D-22 |

- **ESC-148:** variantes (a) `rol: "admin"` → 400; (b) la 61.ª petición del minuto → 429; (c) `EST-999` → 403 `identidad_invalida`; (d) cuerpo de 17 KB → 413.
- **ESC-153:** con `"../EST-004\nx"`, `idSolicitado` = `[FORMATO_INVALIDO]` y una sola línea JSON; con EST-001 y `"EST-002"` ("¿Cuál es mi promedio?"), `idSolicitado: "EST-002"`.

## Despliegue y CI

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-116 `@operacion` | `.env` con `LLM_API_KEY` | `docker compose up` | Salud 200 en `localhost:8080`; puerto 4000 no publicado; el contenedor no corre como root; `docker history` sin la llave | RF-10, SEG-15 |
| ESC-117 `@operacion` | PR que (a) rompe una prueba, (b) cambia 15% por 20%, (c) elimina la verificación de SEG-01, (d) tiene un asunto de 61 caracteres o (e) agrega un paquete NuGet vulnerable | CI | Falla el trabajo correspondiente y el PR no se puede integrar | RF-11, SEG-17 |
