# 08 · Contrato HTTP, auditoría, límites y operación

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · Plan: [`plan.md`](../../specs/001-asistente-academico/plan.md) §12, §13.1 · Contrato: [`constitution.md`](../../constitution.md) §2 · CA-07…CA-11, CA-13

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
| ESC-151 `@contrato` | — | Una petición 200 (ESC-002), 400 (ESC-102), 403 (ESC-050), 415 (ESC-150) y 500 (ESC-112) | `X-Correlation-Id` UUID v7 en minúsculas (variante `8`, `9`, `a` o `b`) igual al `correlationId` del evento y al `traceId`; con un `TimeProvider` falso, su marca (ms) es la del reloj inyectado e igual al `timestamp`; los 5 valores son distintos | D-26, F-06 |
| ESC-152 `@abuso` | EST-001 | "¿Cuál es mi promedio?" con `X-Correlation-Id: 00000000-0000-7000-8000-000000000001` | La respuesta trae otro UUID v7; ningún log ni evento contiene el valor del cliente | D-26 |

## Resiliencia, límites y proveedor

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-108 `@operacion` | EST-001; API académica caída | Su promedio | 503 `ProblemDetails` genérico en < 7 s; no inventa; `error` | RNF-02 |
| ESC-109 `@operacion` | LLM sin respuesta en 20 s | Cualquiera | 503; sin URL, modelo ni llave en el cuerpo (varias llamadas lentas: ESC-158) | SEG-13 |
| ESC-110 a `@operacion` | Sin `LLM_API_KEY` y Ollama caído, o llave de prefijo desconocido sin `LLM_URL_BASE` | — | Arranca; salud 200; una pregunta que necesita el LLM da 503; una denegada sigue dando 403 | plan §4.1 |
| ESC-110 b `@operacion` | EST-001; proveedor doble que responde 429 con `Retry-After: 30` | "¿Cuál es mi promedio?" | 503 con el `ProblemDetails` genérico y `Retry-After: 30`; 1 llamada (sin reintento); `error` | plan §4.1, D-70 |
| ESC-110 c `@operacion` | Igual; reloj 2026-11-23T10:00:00Z y `Retry-After: Mon, 23 Nov 2026 10:02:00 GMT` | Igual | 503 con `Retry-After: 120` | plan §4.1, D-70 |
| ESC-110 d `@alterno` `@abuso` | Igual, una corrida por valor de `Retry-After`: `0`, `3601`, `abc`, `-5`, fecha ya pasada, ausente | Igual | 503 con el `ProblemDetails` genérico y sin encabezado `Retry-After` | plan §4.1, D-70 |
| ESC-110 e `@alterno` | Igual con `Retry-After: 3600` y luego `1` | Igual | 503 con `Retry-After: 3600` y `1` (límites del rango) | plan §4.1, D-70 |
| ESC-110 f `@operacion` `@abuso` | EST-001; llave canaria; proveedor doble que responde el 400 de la nota ESC-110 f | "¿Cuál es mi promedio?" | 503 genérico; 1 línea `warn` con `400` e `INVALID_ARGUMENT` y nada más del cuerpo (0 `Unknown name`); 0 apariciones de la llave, de encabezados y del cuerpo de la petición | plan §4.1, D-85 |
| ESC-110 g `@abuso` | Igual; una corrida por respuesta del doble: 400 con `status` `INVALID_ARGUMENT` y `message` "nota de prueba 4.39\r\nERROR falso"; 401 con `message` que contiene `sk-ant-canaria` | Igual | 503 genérico en ambas; el log tiene `400`/`INVALID_ARGUMENT` y `401`, y 0 apariciones de "nota de prueba", `sk-ant-canaria` o una línea que empiece por "ERROR falso" | D-85 |
| ESC-119 a `@operacion` | `LLM_API_KEY` con `sk-ant-`, `sk-or-`, `gsk_`, `AIza`, `sk-proj-` y sin llave; sin `LLM_PROVEEDOR` ni `LLM_MODELO` | — | Proveedor, en orden: Anthropic, OpenRouter, Groq, Gemini, OpenAI y Ollama (pista de la llave); el log muestra proveedor y modelo, nunca la llave | D-16, D-84 |
| ESC-119 b | `LLM_PROVEEDOR` = `groq` con una llave `sk-ant-` | — | Groq: gana la variable | D-84 |
| ESC-119 c | Llave sin prefijo conocido y `LLM_MODELO` = `gemini-2.5-flash`, `claude-haiku-5-5`, `gpt-5-mini` o `meta/llama-4` (una corrida por valor) | — | Gemini, Anthropic, OpenAI y OpenRouter; la llave nunca se rechaza por su formato | D-84 |
| ESC-119 d | Llave `AIza…` y `LLM_MODELO` = `claude-haiku-5-5` | — | `NoConfigurado`; log que nombra Gemini y Anthropic, sin la llave; salud 200; una pregunta con LLM da 503 y el proveedor recibe 0 peticiones | D-84 |
| ESC-119 e | Llave `sk-` genérica y `LLM_MODELO` = `gemini-2.5-flash` | — | Gemini (`sk-` genérica no es conflicto) | D-84 |
| ESC-119 f | Llave sin prefijo conocido y `LLM_MODELO` = `qwen2.5:7b`, sin `LLM_URL_BASE` | — | `NoConfigurado` con el log `No se pudo determinar el proveedor: define LLM_PROVEEDOR, LLM_MODELO o LLM_URL_BASE` (no se infiere por `qwen`) | D-84 |
| ESC-119 g | Llave sin prefijo conocido y solo `LLM_URL_BASE` | — | Compatible con OpenAI en esa URL | D-84 |
| ESC-119 h | Cualquiera de (a)–(g) | — | 0 apariciones de la llave en el log de arranque y en los errores | SEG-11, D-84 |
| ESC-158 a `@operacion` | EST-001; doble con 5 llamadas de 6 s y las 4 primeras invocan herramienta | "¿Cuál es mi promedio?" | 503 genérico en < 21 s desde el inicio de la fase del LLM (la 4.ª empieza en 18 s y se cancela en 20 s); como máximo 4 llamadas; `error` | SEG-13, RNF-02 |
| ESC-158 b `@operacion` `@alterno` | EST-001; la API académica tarda 5 s en resolver la identidad (las demás lecturas, al instante) y el LLM doble nunca responde | "¿Cuál es mi promedio?" | 503 genérico con petición total ≥ 25 s y < 27 s (5 s de API + 20 s de LLM, sin acumular más); `error` | RNF-02, D-71 |
| ESC-159 `@abuso` | Presupuesto 0.010 USD, `Llm:ReservaPorPeticionUsd` 0.004; doble que retiene y cuesta 0.003 | 5 peticiones simultáneas de EST-001; se libera tras los 503 | 2 responden 200 y 3 responden 503 sin LLM (2 × 0.004 = 0.008 ≤ 0.010; 3 × 0.004 = 0.012 > 0.010); 2 llamadas; gasto 0.006 USD | SEG-13 |
| ESC-160 `@abuso` | Proveedor doble que responde 302 hacia otro servidor doble | EST-001: "¿Cuál es mi promedio?" | 503 genérico; el servidor de `Location` recibe 0 peticiones; la llave no sale a otro host | SEG-07 |
| ESC-180 a `@alterno` `@abuso` `@operacion` | Presupuesto 0.03; doble de §4.4 (salida 200); sin precio; reserva sin precio 0.01; reloj 23:00:00 UTC | EST-001, 4 veces "¿Cuál es mi promedio?" | 1–3: 200, tokens = lo reportado, costo 0, `precioDesconocido: true`, gasto 0.01 cada una (3 × 0.01 = 0.03); 4.ª: 503 sin LLM con `Retry-After: 3600` (0.03 + 0.01 > 0.03); 3 llamadas | SEG-13, D-39, D-70 |
| ESC-180 b | Igual con `Llm:ReservaSinPrecioUsd` 0 (se usa 0.01 con advertencia al arrancar) | Igual | Igual que (a) | SEG-13, D-39, D-40 |
| ESC-180 c `@operacion` | Igual con `ollama` a precio 0 / 0 | Igual | Las 4 responden 200 con costo 0; ningún `precioDesconocido`; 4 llamadas | D-39 |

- **ESC-110 f:** cuerpo `{"error":{"code":400,"status":"INVALID_ARGUMENT","message":"Unknown name \"seed\": Cannot find field."}}` (medido con Gemini antes de D-85). Un código con CR/LF o de más de 64 caracteres se registra saneado.

## Tasa por IP y bloqueo escalonado (CA-13)

Salvo que se indique otra cosa: configuración por defecto (600 por minuto); reloj controlado; la prueba fija la IP de la conexión en 10.0.0.5 (plan §15.4); cada pregunta es de EST-001, "¿Cuál es la nota mínima para aprobar una asignatura?", con un LLM doble; "tanda en t" = 601 preguntas con el reloj fijo en el segundo t; "XFF" = `X-Forwarded-For`.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-047 `@abuso` | Conexión 10.0.0.5 | Una pregunta con XFF `203.0.113.7`; otra con `Forwarded: for=203.0.113.9` y `X-Real-IP: 203.0.113.9`; otra desde la conexión `::ffff:10.0.0.5` | Las 3: 200 y `ipOrigen` "10.0.0.5"; ningún evento contiene "203.0.113.7" ni "203.0.113.9" | SEG-13, D-72 |
| ESC-068 `@permisos` `@abuso` | Tanda en 0 desde 10.0.0.5 | En el segundo 1: una pregunta desde 10.0.0.6 y otra desde 10.0.0.5 con XFF `10.0.0.6` | 10.0.0.6: 200; 10.0.0.5: 429 con `Retry-After: 29`: límite y bloqueo son por IP de la conexión | SEG-13, D-72 |
| ESC-142 a `@abuso` | Tanda en 0 | — | Las 600 primeras: 200; la 601.ª: 429 `ProblemDetails` con `Retry-After: 30`, 0 llamadas al LLM y 0 a la API académica | SEG-13, D-72 |
| ESC-142 b `@abuso` `@alterno` | Tanda en 0 | Preguntas en los segundos 10, 29.5 y 30; `GET /api/salud` en el 10 | 429 con `Retry-After: 20` y `1`; en el 30, 200: las bloqueadas no alargan el bloqueo y la ventana empieza vacía; salud 200 | SEG-13, D-72 |
| ESC-142 c `@abuso` `@permisos` | Tanda en 0 cuya 601.ª lleva, una por corrida, el cuerpo `{}`, `usuario.id` EST-999 o `Content-Type: text/plain` | — | 429 con `Retry-After: 30` en las 3, no 400, 403 ni 415; 0 lecturas de la API académica: el 429 no revela si un ID existe | SEG-13, D-72 |
| ESC-142 d `@abuso` | Tandas en 0, 30, 90 y 210 | — | En cada tanda, 600 × 200; la 601.ª: 429 con `Retry-After` 30, 60, 120 y 120 (bloqueos hasta los segundos 30, 90, 210 y 330) | SEG-13, D-72 |
| ESC-142 e `@alterno` | Igual que (d) | Una tanda más en el segundo 929 (corrida 1) o 930 (corrida 2) | 929: 429 con `Retry-After: 120` (599 s desde el fin del bloqueo, en 330); 930: `Retry-After: 30` (600 s: la escalada se reinicia) | SEG-13, D-72 |
| ESC-142 f `@alterno` | 600 preguntas en el segundo 0 | Una pregunta en el 59.9 (corrida 1) o en el 60 (corrida 2) | 59.9: 429 con `Retry-After: 30` (las 600 siguen en la ventana); 60: 200 (una marca de hace 60 s exactos ya no cuenta) | SEG-13, D-72 |
| ESC-142 g `@abuso` `@operacion` | Tandas en 0 y 30, más 50 preguntas en el segundo 10 | — | Exactamente 2 eventos 429: la 601.ª de cada tanda, con `bloqueoSegundos` 30 y 60; 0 eventos de las 50 del segundo 10 (siguen con 429 `Retry-After: 20`) | RF-08, D-81 |

- ESC-048 · retirado (D-72)
- ESC-049 · retirado (D-72)
- ESC-067 · retirado (D-72)
- ESC-069 · retirado (D-72)
- ESC-095 · retirado (D-72)

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

- **ESC-148:** variantes (a) `rol: "admin"` → 400; (b) con la tasa configurada en 60, la 61.ª petición del minuto → 429; (c) `EST-999` → 403 `identidad_invalida`; (d) cuerpo de 17 KB → 413.
- **ESC-153:** con `"../EST-004\nx"`, `idSolicitado` = `[FORMATO_INVALIDO]` y una sola línea JSON; con EST-001 y `"EST-002"` ("¿Cuál es mi promedio?"), `idSolicitado: "EST-002"`.

## Despliegue y CI

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-116 `@operacion` | `.env` con `LLM_API_KEY` | `docker compose up` | Salud 200 en `localhost:8080`; puerto 4000 no publicado; el contenedor no corre como root; `docker history` sin la llave | RF-10, SEG-15 |
| ESC-117 `@operacion` | PR que (a) rompe una prueba, (b) cambia 15% por 20%, (c) elimina la verificación de SEG-01, (d) tiene un asunto de 61 caracteres o (e) agrega un paquete NuGet vulnerable | CI | Falla el trabajo correspondiente y el PR no se puede integrar | RF-11, SEG-17 |
