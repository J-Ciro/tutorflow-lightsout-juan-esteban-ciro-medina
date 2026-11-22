# 08 · Contrato HTTP y operación (CA-07 a CA-11)

### Contrato

### ESC-100 · Salud `@contrato`
- **Cuando** `GET /api/salud` · **Entonces** 200 con el cuerpo exacto `{"estado":"ok"}`, aunque el LLM no esté configurado.

### ESC-101 · Forma de la salida `@contrato`
- **Dado** cualquier respuesta 200 o 403 de `/api/asistente/preguntar` · **Entonces** el JSON tiene exactamente `respuesta` (string), `citas` (array de `{documento, seccion}`, nunca `null`), `usoDatosEstudiante` (boolean), `tokens` (entero ≥ 0) y `costoUsd` (número ≥ 0), en camelCase.

### ESC-102 · JSON malformado `@contrato`
- **Cuando** el cuerpo es `{"pregunta": ` · **Entonces** 400 `ProblemDetails` sin stack trace.

### ESC-103 · Campos faltantes o nulos `@contrato`
- **Cuando** falta `pregunta`, falta `usuario`, `usuario.id` es `null` o `pregunta` es `""` o solo espacios · **Entonces** 400.

### ESC-104 · Tipos incorrectos `@contrato`
- **Cuando** `pregunta` es un número o `usuario` es un string · **Entonces** 400.

### ESC-105 · Método incorrecto `@contrato`
- **Cuando** `GET /api/asistente/preguntar` · **Entonces** 405.

### ESC-106 · Tokens y costo coherentes `@contrato`
- **Dado** una respuesta 200 que llamó al LLM · **Entonces** `tokens` > 0 y `costoUsd` > 0 y `costoUsd` = tokens de entrada × precio de entrada + tokens de salida × precio de salida (según la configuración, con tolerancia de 1e-6). En 403 y 400: `tokens` 0 y `costoUsd` 0. (CA-11)

### ESC-107 · Campos extra en la entrada `@contrato`
- **Cuando** la entrada incluye `"usuario": {"rol":"estudiante","id":"EST-001","programas":["ADM"],"esAdmin":true}` · **Entonces** los campos extra se ignoran y no otorgan permisos.

### Resiliencia

### ESC-108 · API académica caída `@operacion`
- **Dado** que la API simulada no responde · **Cuando** `EST-001` pregunta por su promedio · **Entonces** 503 `ProblemDetails` genérico en menos de 7 s; no inventa datos; la auditoría registra `resultado: error`.

### ESC-109 · LLM caído o con timeout `@operacion`
- **Dado** que el LLM no responde en 20 s · **Entonces** 503; sin URL, modelo ni llave en el cuerpo.

### ESC-110 · Sin `LLM_API_KEY` `@operacion`
- **Dado** que la variable no está definida · **Entonces** la aplicación arranca, `/api/salud` responde 200, una pregunta que requiere el LLM devuelve 503 y una pregunta denegada sigue devolviendo 403 (la autorización no depende del LLM).

### ESC-111 · Encabezados de seguridad `@contrato`
- **Dado** cualquier respuesta · **Entonces** incluye `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` y `Referrer-Policy: no-referrer`; ninguna respuesta incluye `Access-Control-Allow-Origin: *`. (SEG-14)

### ESC-112 · Errores internos `@contrato`
- **Dado** una excepción no controlada · **Entonces** 500 `ProblemDetails` con un `traceId` y sin stack trace, tipos .NET ni rutas.

### Auditoría

### ESC-113 · Un evento por pregunta `@operacion`
- **Dado** las preguntas de ESC-002 (respondida), ESC-009 (sin información), ESC-010 (denegada) y ESC-103 (inválida) · **Entonces** cada una produce exactamente un evento con los campos de SPEC §11.3 y el `resultado` correspondiente.

### ESC-114 · Datos consultados `@operacion`
- **Dado** ESC-002 · **Entonces** el evento tiene `estudiantesConsultados: ["EST-001"]` y `recursosConsultados` con `notas`; **no** contiene valores de notas ("4.39", "4.5"…).

### ESC-118 · Terceros enmascarados en la auditoría `@operacion`
- **Dado** ESC-010 · **Entonces** el campo `pregunta` del evento contiene `[TERCERO]` y no contiene "Andrés Martínez"; `ipOrigen` está presente. (SPEC §11.3)

### ESC-115 · Coordinador consulta su programa `@operacion`
- **Dado** ESC-006 · **Entonces** `estudiantesConsultados` contiene solo IDs de ADM (EST-004, EST-005, EST-006) y `recursosConsultados` incluye `programa`.

### Despliegue y CI

### ESC-116 · Docker Compose `@operacion`
- **Dado** un `.env` con `LLM_API_KEY` · **Cuando** `docker compose up` · **Entonces** `GET http://localhost:8080/api/salud` responde 200; el puerto 4000 de la API simulada no está publicado en el host; el contenedor de la API no corre como root; `docker history` de la imagen no contiene la llave.

### ESC-117 · El CI bloquea `@operacion`
- **Dado** un PR que (a) rompe una prueba, (b) cambia el umbral de 15% a 20%, (c) elimina la verificación de SEG-01 o (d) tiene un commit con asunto de 61 caracteres o (e) agrega un paquete NuGet con una vulnerabilidad conocida · **Entonces** el CI falla en el trabajo correspondiente y el PR no se puede integrar.
