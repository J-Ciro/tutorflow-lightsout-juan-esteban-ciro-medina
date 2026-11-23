# SPEC.md · TutorFlow, especificación del sistema

| Campo | Valor |
| --- | --- |
| Versión | 1.7 · 2026-11-22 |
| Estado | **Aprobada** por el humano (2026-11-22); D-16 confirmada; backend en .NET 11 (INT-008). La 1.4 incorpora la corrección de la auditoría de la corrida 1 (415, `correlationId` UUID v7, D-26 y D-27). La 1.5 agrega el respaldo acotado de citas (D-28). La 1.6 hace determinista la no invención (D-29) y condiciona el respaldo de D-28. La 1.7 (decisión del orquestador) agrega las cifras como raíces de apoyo y el activador del resumen propio por asignatura matriculada (D-30) |
| Contenido | Lo transversal del sistema: objetivo, prioridades, actores, contrato, matriz de acceso y decisiones |
| Features | `specs/<feature-id>/spec.md` (§5) |
| Escenarios | `escenarios/<feature-id>/` |

Cada feature tiene su propia spec, que **extiende** este documento y no repite su contenido.

## 1. Objetivo y prioridades

**Qué:** TutorFlow es un asistente académico que responde preguntas de estudiantes y coordinadores de la Corporación Educativa Horizonte (ficticia) usando el reglamento vigente y los datos académicos del periodo 2026-2.

**Por qué:** hoy un estudiante tiene que leer cuatro documentos y hacer sus propios cálculos para saber su promedio, si perdió una asignatura por faltas o si puede aplicar a una beca. TutorFlow lo resuelve en una pregunta, con la regla citada y sin exponer datos de otras personas (Ley 1581, Art. 22 del reglamento).

**Cómo:** el código calcula y autoriza de forma determinista; un LLM solo redacta la respuesta a partir de las secciones del reglamento y de los datos ya autorizados y calculados.

Prioridades, en orden: **1) que nadie vea datos ajenos**, 2) que los cálculos sean exactos, 3) que las respuestas citen y no inventen, 4) costo bajo por pregunta.

## 2. Actores y roles

| Rol | Identificador | Puede ver |
| --- | --- | --- |
| `estudiante` | `EST-NNN`, debe existir en la API | Sus propios datos académicos y el reglamento |
| `coordinador` | `COORD-NN`, debe existir en la API | Los datos de los estudiantes de los programas a su cargo (`/coordinadores/{id}.programas`) y el reglamento |

## 3. Contrato de la API (fijo, no se modifica)

```
POST /api/asistente/preguntar
  Entrada: { "pregunta": string,
             "usuario": { "rol": "estudiante" | "coordinador", "id": string } }
  Salida:  { "respuesta": string,
             "citas": [ { "documento": string, "seccion": string } ],
             "usoDatosEstudiante": boolean,
             "tokens": number, "costoUsd": number }
  403 si el usuario pide datos que no le corresponden.

GET /api/salud  ->  200 { "estado": "ok" }

Arranque: docker compose up   (puerto 8080, variable LLM_API_KEY)
```

### 3.1 Códigos de respuesta

| Código | Cuándo | Cuerpo |
| --- | --- | --- |
| 200 | Pregunta respondida, incluida la respuesta "no encuentro esa información" | Salida del contrato |
| 400 | JSON inválido, campos faltantes, `rol` fuera de la lista, `id` con formato inválido, `pregunta` vacía o de más de 1.000 caracteres | `ProblemDetails` (RFC 9457) sin detalles internos |
| 403 | Ver SEG-01 a SEG-06 | **Salida del contrato** con la negativa (D-07) |
| 405 | Método distinto al definido | Vacío o `ProblemDetails` |
| 413 | Cuerpo de más de 16 KB | `ProblemDetails` |
| 415 | `POST /api/asistente/preguntar` con un `Content-Type` distinto de `application/json` (con o sin `charset`; el tipo de medio se compara sin distinguir mayúsculas) o sin `Content-Type`. No se llama al LLM ni a la API académica; la auditoría registra `resultado: invalida` | `ProblemDetails` |
| 429 | Se supera el límite de tasa por IP de SEG-13 | `ProblemDetails` con `Retry-After` |
| 503 | El LLM o la API académica no responden a tiempo, se supera el límite de herramientas o el presupuesto diario (SEG-13) | `ProblemDetails` genérico |
| 500 | Error no controlado | `ProblemDetails` con `traceId`, sin stack trace (SEG-12) |

Las reglas `SEG-xx` y `D-xx` citadas se definen en [`specs/001-asistente-academico/spec.md` §7](specs/001-asistente-academico/spec.md) y en §6 de este documento.

Reglas del JSON: propiedades en camelCase exactamente como el contrato; `citas` es `[]` cuando no hay citas, nunca `null`; `tokens` es un entero ≥ 0; `costoUsd` es un decimal ≥ 0 con hasta 6 decimales; `Content-Type: application/json; charset=utf-8`. La forma de la entrada y de la salida exitosa no cambia con las reglas de esta sección.

**Identificador de correlación (D-26):** el servidor genera un `correlationId` **UUID v7** (`Guid.CreateVersion7()`) al recibir cada petición, en todos los endpoints y con cualquier código de respuesta. El mismo valor, en formato canónico de 36 caracteres en minúsculas, aparece en: el encabezado de respuesta `X-Correlation-Id`, el campo `traceId` de todo `ProblemDetails` y el campo `correlationId` de la auditoría. Nunca se acepta un identificador enviado por el cliente (`X-Correlation-Id`, `X-Request-Id`, `traceparent` u otro): se ignora y no se registra.

## 4. Matriz de acceso global

La autorización es **código determinista**, se evalúa **antes** de leer datos y **antes** de llamar al LLM, y vive en un único componente (`PoliticaAcceso`). Cada feature detalla sus reglas SEG y no puede ampliar esta matriz sin aprobación humana.

| Recurso ↓ / quién → | EST propio | EST ajeno | COORD, estudiante de su programa | COORD, estudiante de otro programa |
| --- | --- | --- | --- | --- |
| Reglamento | ✔ | — | ✔ | — |
| Datos básicos, notas, asistencia | ✔ | ✘ 403 | ✔ | ✘ 403 |
| Lista y agregados del programa | ✘ 403 | ✘ 403 | ✔ (solo su programa) | ✘ 403 |

## 5. Features

| ID | Feature | Spec | Escenarios | Estado |
| --- | --- | --- | --- | --- |
| 001 | Asistente académico (PDF Q10) | [specs/001-asistente-academico/spec.md](specs/001-asistente-academico/spec.md) | [escenarios/001-asistente-academico/](escenarios/001-asistente-academico/) | Aprobada |

## 6. Decisiones

Decisiones del sistema y de la feature 001. Las features nuevas registran las suyas en su spec (`D-NNN-xx`).

| ID | Ambigüedad | Decisión | Por qué |
| --- | --- | --- | --- |
| D-01 | El contrato envía la identidad en el cuerpo, sin autenticación | Se acepta tal cual (contrato fijo), se valida contra la API (SEG-03) y se detecta la rotación de identidades (SEG-16). **Riesgo residual crítico, aceptado y documentado (A07):** cualquiera que conozca un ID puede hacerse pasar por esa persona. En producción la identidad vendría de un token firmado (JWT/OIDC) y `usuario` se ignoraría | No se puede cambiar el contrato; se mitiga y se hace visible |
| D-02 | Hay dos reglamentos con reglas distintas | Solo se usa el 2025; el 2023 está derogado (Art. 3) | Usar el 2023 cambia resultados (15% frente a 20%, habilitación 2.0 frente a 1.5, cancelación semana 8 frente a 10) |
| D-03 | "Más del 15%" | Estrictamente mayor, con aritmética entera: 7/48 no pierde y 8/48 pierde | Literal del Art. 11 y sin errores de flotante |
| D-04 | `inasistencias: null` | Sin registro: no se presume pérdida y se remite al coordinador (Art. 12) | No inventar datos |
| D-05 | Qué es la "nota acumulada" con cortes pendientes | Σ(peso × corte calificado), 1 decimal, junto con el porcentaje evaluado y su equivalente sobre lo evaluado (EST-006: 2.4 sobre el 60% ≈ 4.0) | Da ambas lecturas sin ambigüedad |
| D-06 | Asignatura perdida por inasistencia en el promedio | 0.0 (Evaluación §4) | Literal |
| D-07 | Cuerpo del 403 | Salida del contrato con `respuesta` = `TEXTO_NEGATIVA`, elegido **solo por el `rol` declarado**: estudiante → "No puedo compartir esa información: solo puedes consultar tus propios datos académicos."; coordinador → "No puedo compartir esa información: solo puedes consultar los datos de los estudiantes de tu programa.". `citas`: [`reglamento_academico_2025.md` · `Artículo 22`], `usoDatosEstudiante: false`, `tokens: 0` y `costoUsd: 0` (salvo el 403 tardío, D-22) | Cumple a la vez "403" y "negativa"; textos constantes y comprobables |
| D-08 | ID inexistente o rol incoherente | 403 con el mismo cuerpo que un acceso ajeno. **Limitación reconocida:** con una pregunta normativa, un ID válido responde 200 y uno inexistente 403, así que la existencia de un ID se puede descubrir. Se mitiga con el límite por IP (SEG-13) y la detección de SEG-16 | Denegar también lo normativo a los IDs válidos rompería el uso legítimo |
| D-09 | Mención de terceros | Se aplica solo SEG-04 y SEG-04a: nombre completo o ID con forma de persona. Un nombre o apellido suelto no deniega; esa brecha la cubren SEG-06 y SEG-10. El estudiante sí puede mencionar al coordinador de su programa | Equilibrio entre privacidad y falsos 403 en preguntas legítimas |
| D-10 | `rol` con mayúsculas o espacios | 400: lista cerrada exacta | El contrato fija los valores |
| D-13 | "Registrar los datos del estudiante consultados" frente a la Ley 1581 | Se registra el detalle de lo consultado (`datosConsultados`: estudiante, programa, recurso y asignatura), **sin los valores** (cortes, definitivas, faltas, promedios). Confirmado por el humano | Cumple el PDF de forma verificable y minimiza los datos personales |
| D-14 | Formato de citas | §11.1. Riesgo: la cita esperada "Secciones 3 y 4" se devuelve como dos entradas, y "Datos del estudiante" como `documento`, con la sección en `seccion`. Si las pruebas ocultas comparan la cadena literal, podrían fallar; se elige el formato estructurado del contrato | El contrato define `{documento, seccion}` por cita |
| D-15 | Separador decimal | Punto | Así lo usan el reglamento y las preguntas de ejemplo |
| D-16 | Proveedor de LLM | **Independiente del proveedor:** cualquier persona usa la app con su propia `LLM_API_KEY`. El proveedor se detecta por el prefijo de la llave (tabla en la spec 001 §9.4) y se puede forzar con `LLM_PROVEEDOR`, `LLM_URL_BASE` y `LLM_MODELO`. Sin llave, la app usa Ollama local (`qwen2.5:7b`), que es el entorno de pruebas del humano. Cada proveedor usa un modelo pequeño por defecto, porque el LLM solo redacta: los cálculos y permisos son código. Confirmada por el humano | Cualquier evaluador puede usar su propia llave; el costo es el 15% de la evaluación |
| D-17 | El anexo de `bienestar_y_becas.md` contiene instrucciones dirigidas al asistente | Se excluye del índice (no es contenido normativo) **y** además se prueba la resistencia a la inyección indirecta con documentos sintéticos (defensa en profundidad) | Material no confiable |
| D-20 | Beca con asignaturas sin definitiva | "No determinable aún" | El promedio del periodo no está completo |
| D-21 | Bloquear o solo detectar la suplantación (SEG-16) | Solo detectar y auditar por defecto; el bloqueo es configurable | Las pruebas ocultas de Q10 probablemente usan varias identidades desde una misma IP; bloquear las haría fallar |
| D-22 | 403 cuando el modelo pide un ID ajeno (SEG-06) | 403 tardío con el consumo real de tokens y costo, y `motivoDenegacion: herramienta_id_ajeno`. Se acepta que una inyección indirecta convierta una pregunta legítima en 403: fallar cerrado es preferible a filtrar datos | No ocultar costos reales; ante la duda, denegar |
| D-23 | Texto exacto en casos de abuso | Las inyecciones con terceros o agregados detectables → 403 (SEG-04/05). Las que no se detectan antes del LLM → se verifica la ausencia de datos de terceros, el código y `usoDatosEstudiante`, no el texto libre | Las aserciones deben ser deterministas |
| D-24 | Cliente del LLM compatible con OpenAI | Se usa `Microsoft.Extensions.AI` (`IChatClient` + integración OpenAI) **solo en Infrastructure**, detrás del puerto `IClienteLlm`. Sin invocación automática de funciones: el ciclo de herramientas lo controla el caso de uso (SEG-06, máximo 4 invocaciones, 403 tardío). Anthropic sigue con su SDK oficial. No se usa `Microsoft.Agents.AI`: una pregunta por petición, sin historial ni multiagente | Menos código HTTP propio; Agent Framework sería sobreingeniería |
| D-25 | Persistencia de la auditoría | Consola y archivo JSONL diario en un volumen de Docker. Confirmado por el humano | Los eventos sobreviven a reinicios y se pueden revisar |
| D-26 | Identificador de correlación | UUID v7 generado por el servidor en cada petición (`Guid.CreateVersion7()`), igual en `X-Correlation-Id`, en el `traceId` de todo `ProblemDetails` y en el `correlationId` de la auditoría; el valor del cliente se ignora (§3.1). Reemplaza al `TraceIdentifier` de Kestrel (`0HN…:00000004`). Aprobada por el humano | Formato estándar y ordenable por tiempo; un solo valor correlaciona la respuesta con la auditoría. El `TraceIdentifier` no es estándar y no coincidía con el `traceId`; aceptar el valor del cliente permitiría falsear o colisionar la auditoría |
| D-27 | El modelo pequeño no invoca la herramienta e inventa los datos propios | Cuando un estudiante pregunta por sus propios datos (activador de COMP-07), el backend calcula su resumen autorizado y lo entrega en la **primera** llamada al LLM como dato no confiable delimitado (SEG-09); las herramientas siguen disponibles para el detalle. Los coordinadores no reciben resumen. Aprobada por el humano | Con `qwen2.5:7b` y otros modelos pequeños el modelo no llama a la herramienta e inventa; el resumen elimina esa dependencia y ahorra una ida y vuelta al LLM (menos tokens y latencia). No amplía permisos: son los mismos datos que la herramienta del estudiante ya entrega |
| D-28 | El modelo responde bien pero no escribe la cita (COMP-02) | **Respaldo acotado:** se devuelven las citas de documento que el modelo escribió, que corresponden a secciones entregadas y (desde la v1.6, D-29) cuya sección comparte con la respuesta al menos 2 raíces de contenido (SEG-10a). Si tras SEG-10a no queda ninguna (no escribió citas o todas eran de secciones no entregadas) y la respuesta no es una negativa (403, `TEXTO_NEGATIVA`), el texto de COMP-03 ni `TEXTO_NO_VERIFICABLE`, se agregan como máximo las **2 primeras secciones entregadas, en orden de ranking de la recuperación, que comparten con el texto de la respuesta al menos 2 raíces de contenido** (`Conocimiento:ApoyoMinimoRespaldo`; condición agregada por D-29 en la v1.6); si ninguna la cumple y no se leyeron datos, la respuesta se reemplaza por COMP-03 (SEG-10e, D-29). Las citas de datos se agregan por lo efectivamente leído. Negativas: solo `Artículo 22`; COMP-03 y `TEXTO_NO_VERIFICABLE`: `[]`. Tope: citas escritas válidas + 2 + datos. Reemplaza la regla estricta de la v1.4. Aprobada por el humano | La regla estricta dejaba `citas: []` cuando el modelo respondía bien sin escribir la cita, un riesgo para CA-01; el relleno con todas las secciones entregadas (de 6 a 15 citas) era el otro extremo y degradaba la precisión. Un máximo de 2 secciones entregadas equilibra trazabilidad y precisión, y nunca cita una sección que el modelo no vio. Sin la condición de D-29, una respuesta inventada saldría con 2 citas que parecen respaldarla |
| D-29 | "No sé" decidido por el modelo: si inventa una respuesta sin números ni citas, nada la bloquea, y el respaldo de D-28 le agregaría 2 citas | **No invención determinista**, en tres puntos (spec 001 COMP-02, COMP-03, SEG-10c, SEG-10e y §11.1): (1) **umbral de relevancia previo al LLM:** una sección recuperada es relevante si contiene al menos `min(Conocimiento:RelevanciaMinima, ⌈r/2⌉)` de las `r` raíces de contenido distintas de la pregunta (prefijos de 4 caracteres tras normalizar, sin palabras vacías ni genéricas del corpus; por defecto `RelevanciaMinima` = 2); si ninguna lo es y la pregunta no está exenta (activador de COMP-07, coordinador con programas y términos de datos o un estudiante suyo, o mención de `2023`), COMP-03 sin llamar al LLM, `tokens: 0`, `costoUsd: 0` y auditoría `sin_informacion`; lo mismo si se piden datos de un periodo distinto de 2026-2; (2) **respuesta sin respaldo:** sin cita escrita válida, sin datos leídos y sin sección de apoyo, la respuesta se reemplaza por COMP-03 con `citas: []`; (3) **citas y respaldo condicionados:** una cita escrita por el modelo solo se devuelve si su sección comparte al menos 2 raíces con la respuesta (`Conocimiento:ApoyoMinimoRespaldo`); si no, se descarta igual que una no entregada (SEG-10a). D-28 solo agrega como respaldo secciones que cumplen la misma condición. Si se leyeron datos, una cita escrita a una sección del bloque "Reglas aplicadas" de esos datos (spec 001 §11.1, p. ej. `Artículo 5` para asignaturas perdidas) cumple la condición aunque no comparta raíces con la respuesta; sin lectura de datos no hay excepción. Además, un valor monetario que no está en las fuentes se reemplaza por COMP-03 (SEG-10c). Pedida y aprobada por el humano | "Nunca inventar" no puede depender del modelo: un modelo pequeño responde con seguridad lo que no sabe. Un umbral determinista además ahorra la llamada al LLM (tokens y costo en 0) en las preguntas fuera de dominio. El mínimo relativo `⌈r/2⌉` evita rechazar preguntas cortas y legítimas ("¿Qué becas hay?"); las exenciones mantienen las preguntas de datos propios y de coordinador, cuya fuente son los datos y no las secciones. Las fuentes vigentes no contienen precios, así que un precio en la respuesta siempre es inventado. Una cita escrita a una sección que no comparte términos con la respuesta no la respalda: aceptarla dejaría pasar una invención con apariencia de fuente |
| D-30 | (1) Una respuesta correcta que resume una tabla de valores comparte con su sección casi solo cifras: "30% corte 1, 30% corte 2 y 40% corte 3, redondeada a una cifra decimal" comparte con `evaluacion_y_calificaciones.md · Sección 2` solo `cort`, así que la condición de apoyo de D-29 descartaba la cita que exige CA-01 (caso 4). (2) El activador de COMP-07 exigía un marcador de primera persona: "¿Puedo habilitar Bases de Datos?" (`EST-002`) no recibía el resumen, no superaba el umbral y terminaba en COMP-03 sin LLM (un falso "no sé"), y "¿Qué materias puedo habilitar?" llegaba al LLM sin sus datos | (1) **Cifras como raíces de apoyo** (spec 001 §11.1): en la condición de apoyo (COMP-02, SEG-10a, SEG-10e y el respaldo de D-28) las raíces de la respuesta y de la sección incluyen las cifras con forma de nota (`\d\.\d{1,2}`: `3.0`, `4.3`) y los porcentajes (`30%`, `40%`), comparadas como valor literal normalizado. Los enteros sin `%` no cuentan y el umbral de relevancia de COMP-03 (1) sigue usando solo palabras. (2) **Activador ampliado de COMP-07**, solo para estudiantes: además de (i) primera persona + término de datos, (ii) `puedo` + un término de datos distinto de los genéricos `materia(s)`/`asignatura(s)`, sin genérico indefinido (`una materia`, `una asignatura`, `una nota`, `un estudiante`), y (iii) el nombre completo o el código (SEG-04a) de una asignatura en la que el estudiante está matriculado, comprobado primero contra el catálogo de asignaturas del periodo, sin leer datos, y después con la lectura de sus propias notas. Si no está matriculado, no hay resumen, la lectura no cuenta para `usoDatosEstudiante` ni genera citas y sí se audita. Mencionar "asignatura" o "materia" en genérico no activa. La activación exime del umbral (COMP-03 (1) a). Decisión del orquestador; no cambia el contrato ni la matriz de acceso | (1) SEG-10c ya exige que toda cifra con forma de nota o de porcentaje de la respuesta esté en lo entregado, así que contarla como apoyo no permite inventar: una cifra compartida con la sección es un valor que la sección contiene. Los enteros no los verifica SEG-10c y son ubicuos (horas, pisos, semanas): con ellos, "las tutorías son los lunes de 8 a 12" se apoyaría en `bienestar_y_becas.md · Sección 1` (`lune`, `8` y `12`) sin decir la verdad. En el umbral, las cifras de la pregunta no indican el tema. Efecto aceptado: "Una asignatura se aprueba con 3.0." ahora también se apoya en el `Artículo 6` (`asig` y `3.0`), así que el respaldo puede citar el `Artículo 5` y el `Artículo 6`. (2) Una pregunta que nombra una asignatura propia o que pregunta qué puede hacer con sus datos (`puedo habilitar`, `puedo aplicar a la beca`) es de datos propios; el resumen evita que un modelo pequeño invente o diga "no sé". No amplía permisos: son los datos del propio estudiante (SEG-01), los mismos que su herramienta entrega. El genérico indefinido y el genérico solo mantienen normativas "¿Con qué nota puedo habilitar una materia?" y "¿Hasta qué semana puedo cancelar una asignatura?" |
