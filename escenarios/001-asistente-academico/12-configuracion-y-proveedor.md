# 12 · Configuración, prompt y proveedor del LLM

> Spec: [`spec.md`](../../specs/001-asistente-academico/spec.md) · Plan: [`plan.md`](../../specs/001-asistente-academico/plan.md) §4, §5, §13 · CA-15 (también ESC-096), CA-20, CA-23

Salvo que se indique otra cosa:

- configuración de `appsettings.json` del repositorio y `LLM_API_KEY` con una llave falsa de prueba, que nunca aparece en el error de arranque ni en la consola;
- "Vacío" es la cadena vacía (p. ej. `Limites__PresupuestoDiarioUsd=`);
- "No arranca" = el arranque falla antes de escuchar en 8080 (código de salida ≠ 0; en integración, la creación del host lanza una excepción) y el error contiene la línea o líneas indicadas, completas y en una sola línea.

Notación en [`README.md`](../README.md#notación).

## Validación al arrancar (ESC-181 `@alterno` `@abuso` `@operacion`)

| ESC-181 | Clave | Valor | Efecto | Línea esperada |
| --- | --- | --- | --- | --- |
| a | `Conocimiento:RelevanciaMinima` | `0` | No arranca | `Configuración inválida: Conocimiento:RelevanciaMinima = "0" (entero de 1 a 10)` |
| b | `Conocimiento:RelevanciaMinima` | `11` | No arranca | `Configuración inválida: Conocimiento:RelevanciaMinima = "11" (entero de 1 a 10)` |
| c | `Conocimiento:ApoyoMinimoRespaldo` | `dos` | No arranca | `Configuración inválida: Conocimiento:ApoyoMinimoRespaldo = "dos" (entero de 1 a 10)` |
| d | `Conocimiento:ApoyoMinimoRespaldo` | `2.0` | No arranca | `Configuración inválida: Conocimiento:ApoyoMinimoRespaldo = "2.0" (entero de 1 a 10)` |
| e | `Limites:PreguntasPorMinutoPorIp` | `0` | No arranca | `Configuración inválida: Limites:PreguntasPorMinutoPorIp = "0" (entero de 1 a 10000)` |
| f | `Limites:TimeoutLlmSegundos` | `-5` | No arranca | `Configuración inválida: Limites:TimeoutLlmSegundos = "-5" (entero de 1 a 60)` |
| g | `Limites:MaximoSeccionesRecuperadas` | `7` | No arranca | `Configuración inválida: Limites:MaximoSeccionesRecuperadas = "7" (entero de 1 a 6)` |
| h | `Limites:PresupuestoDiarioUsd` | `0` | No arranca | `Configuración inválida: Limites:PresupuestoDiarioUsd = "0" (decimal mayor que 0 y hasta 1000)` |
| i | `Limites:PresupuestoDiarioUsd` | `0,5` | No arranca | `Configuración inválida: Limites:PresupuestoDiarioUsd = "0,5" (decimal mayor que 0 y hasta 1000)` |
| j | `LLM_PRECIO_ENTRADA` | `-1` | No arranca | `Configuración inválida: LLM_PRECIO_ENTRADA = "-1" (decimal de 0 a 1000)` |
| k | `Autorizacion:Asignaturas:2:Codigo` | `bd203` | No arranca | `Configuración inválida: Autorizacion:Asignaturas:2:Codigo = "bd203" (formato ^[A-Z]{2,4}-[0-9]{3}\z)` |
| l | `Autorizacion:Asignaturas:7:Codigo` | `BD-203` | No arranca | `Configuración inválida: Autorizacion:Asignaturas:7:Codigo = "BD-203" (código duplicado de Autorizacion:Asignaturas:2:Codigo)` |
| m | `Autorizacion:Asignaturas:6:Nombre` | vacío | No arranca | `Configuración inválida: Autorizacion:Asignaturas:6:Nombre = "" (nombre vacío)` |
| n | (a) y (h) a la vez | `0` y `0` | No arranca | Las 2 líneas de (a) y (h): todos los inválidos, no solo el primero |
| o | `Llm:ReservaSinPrecioUsd`; modelo sin precio; presupuesto 0.02 | `-0.5` | Arranca con advertencia | `Configuración ignorada: Llm:ReservaSinPrecioUsd = "-0.5" (decimal mayor que 0); se usa 0.01` |
| p | Igual que (o) | `abc` | Arranca con advertencia | `Configuración ignorada: Llm:ReservaSinPrecioUsd = "abc" (decimal mayor que 0); se usa 0.01` |
| q | `Llm:ReservaPorPeticionUsd`; `claude-haiku-5-5` (0.10 / 0.50 USD por millón); presupuesto 0.0036 | `0` | Arranca con advertencia | `Configuración ignorada: Llm:ReservaPorPeticionUsd = "0" (decimal mayor que 0); se usa el estimado de 8000 tokens de entrada y 2000 de salida` |
| r | `Conocimiento:RelevanciaMinima` | `1` | Arranca; efectivo 1 | — |
| s | `Conocimiento:RelevanciaMinima` | `10` | Arranca; efectivo 10 | — |
| t | `Limites:MaximoSeccionesRecuperadas` | `6` | Arranca; efectivo 6 | — |
| u | `Limites:PresupuestoDiarioUsd` | `0.000001` | Arranca; efectivo 0.000001 | — |
| v | `Limites:PresupuestoDiarioUsd` | vacío | Arranca; efectivo 5 | — |
| w | `Llm:ReservaSinPrecioUsd` | vacío | Arranca; efectivo 0.01 | — |
| x | `Autorizacion:Asignaturas:2:Codigo` | `BD-203`, un salto de línea real y `Arranque correcto` | No arranca | `Configuración inválida: Autorizacion:Asignaturas:2:Codigo = "BD-203\nArranque correcto" (formato ^[A-Z]{2,4}-[0-9]{3}\z)`, con `\n` como 2 caracteres; ninguna línea de la consola empieza por `Arranque correcto` |
| y | `Autorizacion:Asignaturas:2:Codigo` | `BD-203` + salto de línea final | No arranca | `Configuración inválida: Autorizacion:Asignaturas:2:Codigo = "BD-203\n" (formato ^[A-Z]{2,4}-[0-9]{3}\z)` (`$` lo aceptaría) |
| z | `Autorizacion:Asignaturas:2:Codigo` | `BD-２０３` (U+FF12, U+FF10, U+FF13) | No arranca | `Configuración inválida: Autorizacion:Asignaturas:2:Codigo = "BD-２０３" (formato ^[A-Z]{2,4}-[0-9]{3}\z)` |
| aa | `Autorizacion:CodigosAsignatura:0` | `bd203` | No arranca | Exactamente 1 línea para esa clave: `Configuración inválida: Autorizacion:CodigosAsignatura:0 = "bd203" (formato ^[A-Z]{2,4}-[0-9]{3}\z)`; ninguna con `no está en Autorizacion:Asignaturas` |
| ab | `Autorizacion:Asignaturas:2:Codigo` y `Autorizacion:CodigosAsignatura:0` | `bd203` y `BD-203` | No arranca | La línea de formato de (k) y `Configuración inválida: Autorizacion:CodigosAsignatura:0 = "BD-203" (no está en Autorizacion:Asignaturas)` |
| ac | `Conocimiento:RelevanciaMinima`, `Limites:PresupuestoDiarioUsd`, `Limites:BloquearMultiplesIdentidades` (una por corrida) | ` 5 `, `5 `, ` true` | No arranca | `… RelevanciaMinima = " 5 " (entero de 1 a 10)`, `… PresupuestoDiarioUsd = "5 " (decimal mayor que 0 y hasta 1000)`, `… BloquearMultiplesIdentidades = " true" (true o false)` |
| ad | `Autorizacion:Asignaturas:6:Nombre` | ausente (`null`; la entrada solo tiene `Codigo` `EST-103`) | No arranca | `Configuración inválida: Autorizacion:Asignaturas:6:Nombre = "" (nombre vacío)` |
| ae | `Autorizacion:Asignaturas:2:Codigo` | 63 veces `A`, una comilla doble y `B` | No arranca | `Configuración inválida: Autorizacion:Asignaturas:2:Codigo = "` + 63 `A` + `\…" (formato ^[A-Z]{2,4}-[0-9]{3}\z)`: se escapa y después se trunca a 64 caracteres (63 `A` y la barra), sin la `B` |
| af | `Autorizacion:Asignaturas` (prueba unitaria del validador) | Lista vacía | Inválido | Exactamente `Configuración inválida: Autorizacion:Asignaturas = "" (catálogo vacío)` |

- **(o)–(q):** salud 200 y la advertencia indicada; con un doble que retiene cada llamada, de 3 peticiones simultáneas de EST-001 ("¿Cuál es mi promedio?") exactamente 2 responden 200 y 1 responde 503 sin LLM, con 2 llamadas.
  - Cálculo (o) y (p): 2 × 0.01 = 0.02 ≤ 0.02 y 0.03 > 0.02.
  - Cálculo (q): 8000 × 0.10 / 10⁶ + 2000 × 0.50 / 10⁶ = 0.0018, 2 × 0.0018 = 0.0036 ≤ 0.0036 y 0.0054 > 0.0036.
  - Con la reserva en 0, las 3 darían 200 (control desactivado).
- **(r)–(w):** salud 200, sin `Configuración inválida` ni `Configuración ignorada`, valor efectivo indicado.
- Reglas: RNF-05, SEG-11, SEG-13, D-39, D-40. CA-15.

## Catálogo, avisos y claves nuevas

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-182 a `@permisos` `@abuso` | Catálogo más la entrada 8 con `Codigo` `EST-002` y `Nombre` "Electiva de prueba" (arranca); doble sin herramientas | EST-001: "¿Cuáles son las notas de EST-002?" | Advertencia de la nota ESC-182 a; 403 NEG; 0 llamadas; 0 lecturas de EST-002; `denegada` `tercero_mencionado` | SEG-04a, D-40 |
| ESC-182 b | Igual | EST-006: "¿Cuál es mi nota en EST-103?" | 200; 1 llamada; `respondida` (`EST-103` sigue exceptuado) | SEG-04a |
| ESC-188 a `@abuso` `@operacion` `@alterno` | Igual que ESC-182 | EST-006 pregunta 3 veces "¿Cuál es mi nota en EST-103?" | 3 respuestas 200 y exactamente 1 línea `Configuración ignorada: Autorizacion:Asignaturas:8:Codigo = "EST-002" …` | D-43 |
| ESC-188 b `@abuso` | Valor de la nota ESC-188 b, una corrida por carácter | Arranque | No arranca con `Configuración inválida: Autorizacion:Asignaturas:2:Codigo = "BD-203 Arranque correcto" (formato ^[A-Z]{2,4}-[0-9]{3}\z)` y sus equivalentes ` `, `‮`, `​` (6 caracteres ASCII); sin el carácter crudo; ninguna línea empieza por `Arranque correcto` | D-43 |
| ESC-188 c | `Conocimiento:PuntajeRelativoMinimo` = `0` o `101`; `Llm:Semilla` = `-1` (uno por corrida) | Arranque | No arranca con `… PuntajeRelativoMinimo = "0" (entero de 1 a 100)`, `… = "101" (entero de 1 a 100)` y `Configuración inválida: Llm:Semilla = "-1" (entero de 0 a 2147483647)` | RNF-05 |
| ESC-188 d | Prueba unitaria: la reserva llega al núcleo sin pasar por la validación | `Llm:ReservaSinPrecioUsd` 0 o -0.5 (sin precio); `Llm:ReservaPorPeticionUsd` 0 (`claude-haiku-5-5`) | Reserva 0.01, 0.01 y 0.0018 USD; nunca 0 | SEG-13, D-43 |
| ESC-199 c1 `@abuso` `@permisos` `@alterno` `@operacion` | Una clave por corrida | `Llm:Temperatura` = `0.31`, `-0.1`, `0,2`, `1e-1`, ` 0.1`; `Conocimiento:PuntajeRelativoRelevante` = `0`, `101`; `Conocimiento:PuntajeRelativoMinimo` = ` 55`; `Llm:Semilla` = `1 ` | No arrancan, con las líneas de la nota ESC-199 c1 | D-59, D-63 F6 |
| ESC-199 c2 | Igual | `Llm:Temperatura` = `0`, `0.3`, `0.30`; `PuntajeRelativoRelevante` = `40` | Arrancan sin advertencia | D-59 |

- **ESC-182 a:** advertencia exacta `Configuración ignorada: Autorizacion:Asignaturas:8:Codigo = "EST-002" (coincide con el ID de una persona del registro); se excluye del catálogo`.
- **ESC-188 b:** `Autorizacion:Asignaturas:2:Codigo` = "BD-203" + un carácter + "Arranque correcto", una corrida por U+2028, U+2029, U+202E y U+200B.
- **ESC-199 c1:** `Configuración inválida: Llm:Temperatura = "<valor>" (decimal de 0 a 0.3)`, `… PuntajeRelativoRelevante = "<valor>" (entero de 1 a 100)`, `… PuntajeRelativoMinimo = " 55" (entero de 1 a 100)` y `… Llm:Semilla = "1 " (entero de 0 a 2147483647)`.

## Proxy de confianza (ESC-096)

Una corrida por valor. `<regla>` = `IP individual IPv4 o IPv6, sin red, rango, puerto ni zona`; la línea esperada es `Configuración inválida: <clave> = "<valor>" (<regla>)`.

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-096 a `@abuso` | `Proxy:IpsConfiables:0` | `10.0.0.0/8`, `172.30.80.0/29`, `2001:db8::/32` | No arranca; línea con la clave `Proxy:IpsConfiables:0` y el valor | RNF-05, D-68 |
| ESC-096 b `@abuso` | `Proxy:IpsConfiables:0` | `10.0.0.5-10.0.0.9` | No arranca; su línea | RNF-05, D-68 |
| ESC-096 c | `Proxy:IpsConfiables:0` | `abc`, `10.0.0.256`, `10.1`, `010.0.0.5`, ` 10.0.0.5` | No arranca; su línea (`10.1` es inválido aunque `IPAddress.TryParse` lo acepte) | RNF-05, D-68 |
| ESC-096 d `@abuso` | `Proxy:IpsConfiables:0` | `10.0.0.5:8080`, `fe80::1%eth0` | No arranca; su línea | RNF-05, D-68 |
| ESC-096 e `@abuso` | `Proxy:IpsConfiables:0` | `0.0.0.0`, `::` | No arranca; su línea | RNF-05, D-68 |
| ESC-096 f | `Proxy:IpsConfiables:0` = `10.0.0.5` y `Proxy:IpsConfiables:1` = `abc` | — | No arranca con exactamente 1 línea, la de `Proxy:IpsConfiables:1` | RNF-05, D-68 |
| ESC-096 g | Variable `Proxy__IpsConfiables` (escalar, no lista) | `172.30.80.2` | No arranca; línea con la clave `Proxy:IpsConfiables` | RNF-05, D-68 |
| ESC-096 h | `Proxy:IpsConfiables:0` | `10.0.0.5`; con `Proxy:IpsConfiables:1` = `10.0.0.5` (duplicado) | Arranca sin advertencia; el log de arranque registra `10.0.0.5` como IP de confianza | D-68 |
| ESC-096 i | `Proxy:IpsConfiables:0` | `2001:db8::5` | Arranca sin advertencia | D-68 |
| ESC-096 j `@alterno` | Variable `Proxy__IpsConfiables__0` vacía | — | Arranca sin advertencia con la lista vacía: se comporta como ESC-047 | RNF-05, D-68 |

## Prompt y proveedor

| ID | Actor | Entrada | Resultado esperado | Regla |
| --- | --- | --- | --- | --- |
| ESC-191 `@feliz` `@alterno` | Prompt de `estudiante` y de `coordinador` | Instrucciones fuera de `<ejemplos>` | ≤ 12 pasos numerados de ≤ 40 palabras; P-01…P-07 como en la nota ESC-191; los 7 pasos distintos; 0 `documento.md`; el bloque cumple ESC-173 e y ESC-198 i | D-46, D-58, D-63 F7 |
| ESC-192 a–d `@operacion` | Servidor doble del proveedor que registra el cuerpo | EST-001: "¿Cuál es la nota mínima para aprobar una asignatura?" con (a) `ollama`, (b) `openai`, (c) `anthropic`, (d) `ollama` y `Llm:Semilla` 42 | `temperature` 0 (defecto) en todos; `seed` 1 en (a) y (b), ausente en (c), 42 en (d) | D-47, D-59 |
| ESC-192 e–g | Igual | (e) `ollama` con `Llm:Temperatura` 0.3; (f) `anthropic` con 0.1; (g) `openai` con 0.2 | `temperature` 0.3, 0.1 y 0.2; `seed` 1 en (e) y (g), ausente en (f) | D-59 |
| ESC-199 d1 `@operacion` | Prompt y formatos reales | Cada llamada de Q10 #1 a #8 con su usuario (Q10 #6 con prelectura) y el reintento de Q10 #6 con "Mateo Vargas perdió Estadística." | Cada una estima ≤ 3.000 tokens (plan §4.4); referencia en la nota ESC-199 d1. Si una supera el tope, falla y se reporta | D-57 |
| ESC-199 d2 | Igual; doble que invoca `consultar_notas` y luego responde | EST-001: "¿Qué dice el reglamento sobre la habilitación de las asignaturas?" (sin resumen) | Las 2 llamadas estiman ≤ 3.000 tokens | D-57, D-65 G3 |
| ESC-199 e1 `@abuso` | `ollama` doble que responde "Tu promedio es 4.39." y reporta ⌈0.75 × E⌉ − 1 | EST-001: "¿Cuál es mi promedio ponderado del periodo?" | 200 TNV; citas []; 1 llamada (sin reintento); `error` con `contextoTruncado: true`; advertencia en el log con la estimación y `prompt_tokens`, sin contenido | D-57 |
| ESC-199 e2–e4 | Igual, reportando ⌈0.75 × E⌉, ⌊1.04 × E⌋ o sin `usage` | Igual | "Tu promedio es 4.39."; `respondida`; sin `contextoTruncado` | D-57 |
| ESC-199 e5 | `openai` doble que reporta 10 | Igual | Igual que (e2): la detección solo aplica a `ollama` | D-57 |
| ESC-199 f | `ollama` con `Llm:Temperatura` 0.2 | Cualquiera, con reintento | El log de arranque registra 0.2; toda petición, también la del reintento, lleva `temperature` 0.2 y `seed` 1 | D-59 |

- **ESC-191:** P-01 con la frase de COMP-03 y `solo`; P-02 con `lugar`, `horario`, `montos` y `beneficio`; P-03 con `promedio` y `definitivas`; P-04 con `(archivo · sección)`; P-05 con `consecuencias` y `consejos`; P-06 con `sin definitiva` y `perdida`; P-07 con `herramientas`.
- **ESC-199 d1:** referencia: Q10 #5, 7.767 caracteres → 2.220 (2.436 reales); Q10 #6 con prelectura, 6.354 → 1.816 (1.984 reales).
