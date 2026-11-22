# SPEC.md · TutorFlow, asistente académico

| Campo | Valor |
| --- | --- |
| Versión | 1.0 · 2026-11-22 |
| Estado | Borrador para aprobación humana |
| Alcance de esta versión | Backend .NET 8 (obligatorio). Frontend React: fuera de esta versión |
| Escenarios | `escenarios/` (ESC-001 en adelante) |
| Requisitos posteriores | `specs/NNN-*.md` (extienden esta spec) |

## 1. Objetivo

TutorFlow responde preguntas de estudiantes y coordinadores usando el reglamento vigente y los datos académicos que cada uno **tiene derecho a ver**, cita documento y sección, dice cuando no sabe y deja auditoría de cada pregunta.

Prioridad de la versión, en orden: **1) que nadie vea datos ajenos**, 2) que los cálculos sean exactos, 3) que las respuestas citen y no inventen, 4) costo bajo por pregunta.

## 2. Alcance

**Incluye:** `POST /api/asistente/preguntar`, `GET /api/salud`, base de conocimiento desde `reglamento/`, consulta a la API simulada, cálculos académicos deterministas, control de acceso por rol, auditoría, Docker Compose, pruebas, evals y CI.

**No incluye:** autenticación real (ver D-01), escritura de datos, interfaz web, historial de conversación (cada pregunta es independiente), idiomas distintos al español.

## 3. Actores

| Rol | Identificador | Puede ver |
| --- | --- | --- |
| `estudiante` | `EST-NNN`, debe existir en la API | Solo sus propios datos académicos y el reglamento |
| `coordinador` | `COORD-NN`, debe existir en la API | Datos de los estudiantes de los programas a su cargo (`/coordinadores/{id}.programas`) y el reglamento |

## 4. Contrato de la API (fijo, no se modifica)

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

### 4.1 Códigos de respuesta

| Código | Cuándo | Cuerpo |
| --- | --- | --- |
| 200 | Pregunta respondida, incluida la respuesta "no encuentro esa información" | Salida del contrato |
| 400 | JSON inválido, campos faltantes, `rol` fuera de la lista, `id` con formato inválido, `pregunta` vacía o de más de 1.000 caracteres | `ProblemDetails` (RFC 9457) sin detalles internos |
| 403 | Ver SEG-01 a SEG-08 | **Salida del contrato** con la negativa (D-07) |
| 405 | Método distinto al definido | Vacío o `ProblemDetails` |
| 413 | Cuerpo de más de 16 KB | `ProblemDetails` |
| 429 | Más de 30 preguntas por minuto con el mismo `usuario.id` o la misma IP | `ProblemDetails` con `Retry-After` |
| 503 | El LLM o la API académica no responden dentro del timeout | `ProblemDetails` genérico |
| 500 | Error no controlado | `ProblemDetails` con `traceId`, sin stack trace (SEG-12) |

Reglas del JSON: propiedades en camelCase exactamente como el contrato; `citas` es `[]` cuando no hay citas, nunca `null`; `tokens` es un entero ≥ 0; `costoUsd` es un decimal ≥ 0 con hasta 6 decimales; `Content-Type: application/json; charset=utf-8`.

## 5. Fuentes de conocimiento

| Documento | Uso |
| --- | --- |
| `reglamento_academico_2025.md` | **Vigente.** Fuente normativa principal |
| `evaluacion_y_calificaciones.md` | Vigente. Cortes, definitiva, redondeo y promedio |
| `bienestar_y_becas.md` | Vigente. Atención, servicios y becas. **Se excluye la sección "Anexo"** (D-17) |
| `reglamento_academico_2023.md` | **Derogado** por el Art. 3 del 2025. No se usa para responder ni se cita (D-02) |
| API simulada (`mock-api`) | Estudiantes, notas por corte y asistencia. Solo lectura y siempre tras autorizar |

Formato de citas (D-14):
- Reglamento: `{ "documento": "reglamento_academico_2025.md", "seccion": "Artículo 5" }`.
- Otros documentos: `"seccion": "Sección 3"`, `"Sección 3.1"`. Una cita por sección (las "Secciones 3 y 4" se citan como dos entradas).
- Datos propios: `{ "documento": "Datos del estudiante", "seccion": "Notas" | "Asistencia" | "Datos básicos" }`.
- Datos de un coordinador: `{ "documento": "Datos del programa", "seccion": "<id del programa>" }`.
- Solo se devuelven citas que existen en la base de conocimiento indexada. Una cita inexistente se descarta (SEG-10).

## 6. Reglas de negocio

Todos los cálculos se hacen en código con `decimal`; el LLM nunca calcula.

| ID | Regla | Fuente |
| --- | --- | --- |
| RN-01 | Escala de notas de 0.0 a 5.0 | Evaluación §1 |
| RN-02 | Una asignatura se aprueba con definitiva ≥ 3.0 | Reglamento 2025 Art. 5 |
| RN-03 | `definitiva = 0.3·corte1 + 0.3·corte2 + 0.4·corte3` | Evaluación §2 y §3 |
| RN-04 | La definitiva se redondea a 1 decimal, con .5 hacia arriba (4.45 → 4.5; 4.44 → 4.4). Implementación: `decimal` + `MidpointRounding.AwayFromZero` | Evaluación §3 |
| RN-05 | Si algún corte es `null`, la asignatura **no tiene definitiva**. Se informa la nota acumulada (D-05) | Evaluación §3 |
| RN-06 | `promedio = Σ(definitiva redondeada × créditos) / Σ créditos`, a 2 decimales con .5 hacia arriba. Solo entran asignaturas con definitiva | Evaluación §4 |
| RN-07 | Pierde por inasistencia quien falta a **más del 15%** de las clases programadas: `inasistencias × 100 > 15 × clasesProgramadas` (D-03). Su definitiva es 0.0 sin importar los cortes | Reglamento 2025 Art. 11 |
| RN-08 | Una asignatura perdida por inasistencia cuenta con 0.0 en el promedio | Evaluación §4 |
| RN-09 | Si `inasistencias` es `null`, no hay registro: no se declara pérdida y se indica solicitar la revisión al coordinador (D-04) | Reglamento 2025 Art. 12 |
| RN-10 | Puede habilitar quien repruebe con definitiva ≥ 2.0 y < 3.0, sin haberla perdido por inasistencia. La nota de habilitación no supera 3.0 | Reglamento 2025 Art. 6 y 7 |
| RN-11 | Queda en prueba académica quien tenga promedio del periodo < 3.0 | Reglamento 2025 Art. 20 |
| RN-12 | Beca por rendimiento: promedio ≥ 4.3, ninguna asignatura perdida (incluida por inasistencia) y ≥ 12 créditos matriculados. Si alguna asignatura no tiene definitiva, la elegibilidad es "no determinable aún" (D-20) | Bienestar §3.1 |
| RN-13 | Máximo de faltas permitidas sin perder: `piso(0.15 × clasesProgramadas)` (48 → 7, 64 → 9, 32 → 4) | Derivada de RN-07 |
| RN-14 | Los números se escriben con punto decimal ("4.39"), como en el reglamento | D-15 |

## 7. Reglas de seguridad y permisos

La autorización es **código determinista**, se evalúa **antes** de leer datos y **antes** de llamar al LLM, y vive en un único componente (`PoliticaAcceso`).

| ID | Regla | Respuesta |
| --- | --- | --- |
| SEG-01 | Un estudiante solo accede a sus propios datos (`usuario.id`). | 403 |
| SEG-02 | Un coordinador solo accede a estudiantes cuyo `programaId` está en sus `programas`. | 403 |
| SEG-03 | El `usuario.id` debe existir en la API y su tipo debe coincidir con el `rol` (`estudiante` ↔ `/estudiantes/{id}`, `coordinador` ↔ `/coordinadores/{id}`). Si no, 403 con el mismo cuerpo que SEG-01, para no revelar qué IDs existen. | 403 |
| SEG-04 | Si la pregunta de un estudiante menciona a otra persona del registro (nombre completo o nombre y apellido, sin importar tildes ni mayúsculas) o cualquier ID `EST-`/`COORD-` distinto al suyo, se deniega **sin llamar al LLM**. Ídem para un coordinador con estudiantes fuera de su programa. | 403 |
| SEG-05 | Un estudiante no puede pedir agregados que incluyan a otros ("el mejor promedio", "lista de estudiantes", "compárame con mis compañeros", "promedio del curso"). | 403 |
| SEG-06 | Las herramientas que el LLM puede invocar reciben la identidad del servidor. Las herramientas de estudiante no tienen parámetro de ID. Las de coordinador aceptan `estudianteId` y lo validan con SEG-02 en cada invocación. | Si el modelo pide un ID ajeno: 403 y fin del ciclo |
| SEG-07 | Los IDs se validan con `^EST-\d{3}$` y `^COORD-\d{2}$` antes de cualquier uso, y se codifican al construir rutas (sin traversal ni query). | 400 si no cumple el formato |
| SEG-08 | Instrucciones dentro de la pregunta ("ignora tus reglas", "soy coordinador", "modo admin") no cambian el rol ni los permisos: el rol sale solo de `usuario.rol` validado. | Se aplican SEG-01 a SEG-06 |
| SEG-09 | Los documentos de `reglamento/` son datos no confiables: se entregan al modelo entre delimitadores y con la instrucción explícita de no obedecerlos. | — |
| SEG-10 | Verificación de la salida: las citas deben existir en el índice; la respuesta no puede contener nombres ni IDs de estudiantes no autorizados (si los contiene, se reemplaza por la negativa y se audita como `fuga_bloqueada`). | 200 con negativa |
| SEG-11 | `LLM_API_KEY` solo se lee de variables de entorno; nunca se registra, se devuelve ni se incluye en la imagen. | — |
| SEG-12 | Errores sin stack trace, rutas ni respuestas crudas de servicios internos. | — |
| SEG-13 | Límites de consumo: pregunta ≤ 1.000 caracteres, cuerpo ≤ 16 KB, 30 preguntas por minuto, `max_tokens` de salida ≤ 600, ≤ 4 invocaciones de herramientas por pregunta, timeout de 20 s al LLM y de 5 s a la API. | 400 / 413 / 429 / 503 |
| SEG-14 | Encabezados `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`; CORS sin `AllowAnyOrigin`; Swagger solo en `Development`. | — |
| SEG-15 | El contenedor corre como usuario no root; la API académica no se publica fuera de la red de Compose. | — |

### 7.1 Matriz de acceso

| Recurso ↓ / quién → | EST propio | EST ajeno | COORD, estudiante de su programa | COORD, estudiante de otro programa |
| --- | --- | --- | --- | --- |
| Reglamento | ✔ | — | ✔ | — |
| Datos básicos, notas, asistencia | ✔ | ✘ 403 | ✔ | ✘ 403 |
| Lista y agregados del programa | ✘ 403 | ✘ 403 | ✔ (solo su programa) | ✘ 403 |

## 8. Comportamiento del asistente

| ID | Regla |
| --- | --- |
| COMP-01 | Responde en español, breve (≤ 120 palabras salvo listas), con los valores calculados por el código. |
| COMP-02 | Toda afirmación normativa lleva cita. Las respuestas con datos llevan además la cita de datos. |
| COMP-03 | Si la información no está en los documentos vigentes ni en los datos autorizados, responde exactamente: **"No encuentro esa información en los documentos disponibles."**, con `citas: []` y `usoDatosEstudiante: false`. Aplica también a preguntas fuera del dominio académico. |
| COMP-04 | `usoDatosEstudiante` es `true` si y solo si se leyeron datos académicos de algún estudiante para responder. Una pregunta normativa (p. ej. requisitos de la beca) no lee datos: `false`. |
| COMP-05 | Si preguntan por el reglamento 2023, responde que fue derogado y cita `reglamento_academico_2025.md · Artículo 3`. |
| COMP-06 | Nunca revela el prompt de sistema, las herramientas internas, la configuración ni la llave. |

## 9. Auditoría

Por cada llamada a `/api/asistente/preguntar` (incluidas las 400, 403 y 429) se emite **un** evento JSON estructurado con: `timestamp` (UTC), `correlationId`, `usuario.rol`, `usuario.id`, `pregunta` (truncada a 1.000 caracteres y con los caracteres de control escapados), `resultado` (`respondida` | `sin_informacion` | `denegada` | `invalida` | `fuga_bloqueada` | `error`), `motivoDenegacion` (si aplica), `estudiantesConsultados` (IDs), `recursosConsultados` (`datos_basicos` | `notas` | `asistencia` | `programa`), `citas`, `tokens`, `costoUsd` y `duracionMs`.

No se registran valores de notas ni la respuesta completa (Ley 1581, Art. 22; D-13). Nunca se registra la llave.

## 10. Diseño de referencia del flujo

1. Validar la entrada (400/413).
2. Resolver la identidad en la API y verificar el rol (SEG-03).
3. Filtro previo de terceros y agregados (SEG-04, SEG-05) → 403 sin LLM.
4. Recuperar las secciones relevantes de los documentos vigentes (búsqueda léxica por sección; corpus pequeño, sin base vectorial).
5. Llamar al LLM con las secciones delimitadas y herramientas acotadas al rol (SEG-06). Las herramientas devuelven valores **ya calculados** por `Core` (definitivas, promedio, pérdida por inasistencia, faltas restantes, elegibilidad).
6. Verificar la salida (SEG-10) y armar `citas`, `usoDatosEstudiante`, `tokens` y `costoUsd`.
7. Emitir la auditoría (§9).

## 11. Criterios de aceptación

| ID | Criterio | Escenarios |
| --- | --- | --- |
| CA-01 | Las 10 preguntas de `preguntas_ejemplo.json` devuelven la respuesta y las citas esperadas | ESC-001…ESC-010 |
| CA-02 | Los cálculos de §13 se reproducen exactamente, incluidos los redondeos en el punto medio (4.45 → 4.5, 3.45 → 3.5, 4.05 → 4.1, 4.55 → 4.6, 3.75 → 3.8, 4.15 → 4.2 y el promedio 2.275 → 2.28) | ESC-020…ESC-031 |
| CA-03 | Ninguna respuesta usa ni cita el reglamento 2023 | ESC-003, ESC-013…ESC-016 |
| CA-04 | Toda petición de datos ajenos devuelve 403 con el cuerpo de D-07, sin llamar al LLM ni leer datos del tercero | ESC-050…ESC-066 |
| CA-05 | Ningún vector de inyección (en la pregunta o en un documento) produce datos de terceros, cambio de rol, fuga del prompt o de la llave | ESC-070…ESC-084 |
| CA-06 | Las preguntas sin respuesta en las fuentes devuelven el texto de COMP-03 | ESC-090…ESC-094 |
| CA-07 | El contrato HTTP (forma, tipos y códigos 200/400/403/405/413/429/503) se cumple | ESC-100…ESC-112 |
| CA-08 | Cada pregunta genera exactamente un evento de auditoría con los campos de §9, sin notas ni llave | ESC-113…ESC-115 |
| CA-09 | `docker compose up` expone la API en el 8080 con `LLM_API_KEY` y `/api/salud` responde 200 | ESC-116 |
| CA-10 | El CI bloquea un cambio que rompe el build, una prueba, un eval o la regla de commits | ESC-117 |
| CA-11 | `usoDatosEstudiante`, `tokens` y `costoUsd` son coherentes: 0 tokens en 403/400 y `true` solo si se leyeron datos | ESC-004, ESC-008, ESC-050, ESC-106 |

## 12. Trazabilidad de requisitos

| Requisito | Spec | Estado |
| --- | --- | --- |
| Base: asistente académico (PDF Q10) | `SPEC.md` | Borrador |

## 13. Datos de referencia (calculados con RN-03 a RN-13)

| Est. | Asignatura | Cr | Cortes | Definitiva | Faltas / clases (máx.) | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| EST-001 Laura Gómez (ING-SIS) | Cálculo Integral | 4 | 4.2 / 4.5 / 4.6 | 4.45 → **4.5** | 1/64 (9) | Aprobada |
| | Programación Orientada a Objetos | 3 | 4.8 / 4.6 / 4.9 | 4.78 → **4.8** | 0/48 (7) | Aprobada |
| | Bases de Datos | 3 | 4.0 / 3.8 / 4.4 | **4.1** | 2/48 (7) | Aprobada |
| | Inglés III | 2 | 3.9 / 4.1 / 4.0 | **4.0** | 1/32 (4) | Aprobada |
| | **Promedio** 52.7/12 = 4.3916 → **4.39** · 12 créditos · beca: **elegible** | | | | | |
| EST-002 Andrés Martínez (ING-SIS) | Cálculo Integral | 4 | 2.5 / 2.8 / 2.6 | 2.63 → **2.6** | 3/64 | Perdida (habilitable) |
| | Programación Orientada a Objetos | 3 | 3.5 / 3.2 / 3.6 | 3.45 → **3.5** | 2/48 | Aprobada |
| | Bases de Datos | 3 | 3.8 / 3.6 / 3.9 | 3.78 → **0.0** | **9/48 = 18.75%** | **Perdida por inasistencia** (no habilitable) |
| | Inglés III | 2 | 3.0 / 3.4 / 3.1 | 3.16 → **3.2** | 1/32 | Aprobada |
| | **Promedio** 27.3/12 = 2.275 → **2.28** · **prueba académica** | | | | | |
| EST-003 Sofía Herrera (ING-SIS) | Cálculo Integral | 4 | 4.0 / 3.9 / 4.2 | 4.05 → **4.1** | 2/64 | Aprobada |
| | Programación Orientada a Objetos | 3 | 3.7 / 4.0 / 3.8 | 3.83 → **3.8** | 1/48 | Aprobada |
| | Bases de Datos | 3 | 4.1 / 4.3 / 4.0 | 4.12 → **4.1** | 0/48 | Aprobada |
| | Inglés III | 2 | 4.5 / 4.4 / 4.7 | 4.55 → **4.6** | **null**/32 | Aprobada · sin registro de asistencia (RN-09) |
| | **Promedio** 49.3/12 = 4.1083 → **4.11** | | | | | |
| EST-004 Camilo Rojas (ADM) | Contabilidad | 3 | 3.6 / 3.9 / 4.1 | 3.89 → **3.9** | 0/48 | Aprobada |
| | Microeconomía | 3 | 3.4 / 3.0 / 3.5 | 3.32 → **3.3** | 2/48 | Aprobada |
| | Estadística | 4 | 4.0 / 4.2 / 3.9 | 4.02 → **4.0** | 3/64 | Aprobada |
| | Comunicación Oral y Escrita | 2 | 4.5 / 4.6 / 4.4 | 4.49 → **4.5** | 1/32 | Aprobada |
| | **Promedio** 46.6/12 = 3.8833 → **3.88** · beca: no elegible (< 4.3) | | | | | |
| EST-005 Valentina Castro (ADM) | Contabilidad | 3 | 3.2 / 3.0 / 3.3 | 3.18 → **3.2** | 4/48 | Aprobada |
| | Microeconomía | 3 | 1.9 / 1.6 / 1.9 | 1.81 → **1.8** | 5/48 | Perdida (no habilitable, < 2.0) |
| | Estadística | 4 | 2.4 / 2.2 / 2.3 | **2.3** | 6/64 | Perdida (habilitable) |
| | Comunicación Oral y Escrita | 2 | 3.8 / 3.5 / 3.9 | 3.75 → **3.8** | 2/32 | Aprobada |
| | **Promedio** 31.8/12 = **2.65** · **prueba académica** | | | | | |
| EST-006 Mateo Vargas (ADM) | Contabilidad | 3 | 4.1 / 4.0 / 4.3 | 4.15 → **4.2** | 1/48 | Aprobada |
| | Microeconomía | 3 | 3.6 / 3.8 / 3.9 | 3.78 → **3.8** | 0/48 | Aprobada |
| | Estadística | 4 | 3.9 / 4.1 / **null** | **Sin definitiva** · acumulada 2.4 sobre el 60% evaluado (equivale a 4.0) | 2/64 | Pendiente |
| | Comunicación Oral y Escrita | 2 | 4.2 / 4.0 / 4.4 | 4.22 → **4.2** | 0/32 | Aprobada |
| | **Promedio** (8 créditos con definitiva) 32.4/8 = **4.05** · beca: no determinable aún | | | | | |

Trampas que las pruebas deben cubrir: `Math.Round` por defecto (bancario) da 4.4 en lugar de 4.5 para Cálculo de EST-001; con `double`, 2.275 puede dar 2.27; con el reglamento 2023 (20%) EST-002 no perdería Bases de Datos.

## 14. Decisiones sobre ambigüedades

| ID | Ambigüedad | Decisión | Por qué |
| --- | --- | --- | --- |
| D-01 | El contrato envía la identidad en el cuerpo, sin autenticación | Se acepta tal cual (contrato fijo) pero se valida contra la API (SEG-03). Riesgo A07 documentado: en producción la identidad vendría de un token firmado (JWT/OIDC) y `usuario` se ignoraría | No se puede cambiar el contrato; se mitiga lo posible |
| D-02 | Hay dos reglamentos con reglas distintas | Solo se usa el 2025; el 2023 está derogado (Art. 3) | Usar el 2023 cambia resultados (15% frente a 20%, habilitación 2.0 frente a 1.5, cancelación semana 8 frente a 10) |
| D-03 | "Más del 15%" | Estrictamente mayor, con aritmética entera: 7/48 no pierde y 8/48 pierde | Literal del Art. 11 y sin errores de flotante |
| D-04 | `inasistencias: null` | Sin registro: no se presume pérdida y se remite al coordinador (Art. 12) | No inventar datos |
| D-05 | Qué es la "nota acumulada" con cortes pendientes | Σ(peso × corte calificado), 1 decimal, junto con el porcentaje evaluado y su equivalente sobre lo evaluado (EST-006: 2.4 sobre el 60% ≈ 4.0) | Da ambas lecturas sin ambigüedad |
| D-06 | Asignatura perdida por inasistencia en el promedio | 0.0 (Evaluación §4) | Literal |
| D-07 | Cuerpo del 403 | Salida del contrato con `respuesta`: "No puedo compartir esa información: solo puedes consultar tus propios datos académicos." (estudiante) o "...los datos de los estudiantes de tu programa." (coordinador), `citas`: [Reglamento 2025 · Artículo 22], `usoDatosEstudiante: false`, `tokens: 0`, `costoUsd: 0` | Cumple "403 o negativa" a la vez |
| D-08 | ID inexistente o rol incoherente | 403 con el mismo cuerpo que un ID ajeno | Evita la enumeración de usuarios |
| D-09 | Mención de terceros sin pedir datos ("¿Andrés está en mi grupo?") | 403 para estudiantes: cualquier mención de otra persona del registro | Ante la duda, privacidad (Art. 22) |
| D-10 | `rol` con mayúsculas o espacios | 400: lista cerrada exacta | El contrato fija los valores |
| D-13 | "Registrar los datos del estudiante consultados" frente a la Ley 1581 | Se registran IDs y recursos consultados, no valores | Auditoría suficiente sin duplicar datos personales |
| D-14 | Formato de citas | §5 | Coincide con `citaEsperada` de las preguntas de ejemplo |
| D-15 | Separador decimal | Punto | Así lo usan el reglamento y las preguntas de ejemplo |
| D-16 | Proveedor de LLM | Configurable por `appsettings` (modelo y precio por millón de tokens). Propuesta: un modelo pequeño y económico (p. ej. Claude Haiku) porque el LLM solo redacta: los cálculos y permisos son código. **Requiere confirmación humana** | Costo es el 15% de la evaluación |
| D-17 | El anexo de `bienestar_y_becas.md` contiene instrucciones dirigidas al asistente | Se excluye del índice (no es contenido normativo) **y** además se prueba la resistencia a la inyección indirecta con documentos sintéticos (defensa en profundidad) | Material no confiable |
| D-20 | Beca con asignaturas sin definitiva | "No determinable aún" | El promedio del periodo no está completo |
