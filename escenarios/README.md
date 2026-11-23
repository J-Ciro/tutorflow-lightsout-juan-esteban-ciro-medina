# Escenarios de aceptación

Los escenarios se agrupan por feature en `escenarios/<feature-id>/`, con el mismo *feature-id* de `specs/<feature-id>/`, y dentro de cada feature **por tema**, nunca por ronda de corrección. Cada archivo enlaza su spec en el encabezado. Cada escenario se convierte en al menos una prueba o eval que lo referencia por su ID (`qa-calidad`).

## Convenciones

- **Formato:** una tabla por tema con las columnas `ID | Actor | Entrada | Resultado esperado | Regla`. Una fila por caso; las filas no pasan de ~400 caracteres (un texto más largo va en un bullet bajo la tabla). Las condiciones comunes se escriben una sola vez, sobre la tabla.
- **ID:** `ESC-NNN`, único y nunca reutilizado. Los casos de un escenario se distinguen con una letra (`ESC-175 j`). Un escenario retirado se marca como `~~retirado~~` y no se borra.
- **Corrección:** cambiar un resultado actualiza la fila en su sitio y registra la D en `decisiones.md`; no se agrega historia ("hasta la v1.x…"): la historia está en git.
- **Etiquetas:** `@feliz`, `@alterno` (límites, nulos, vacíos), `@permisos`, `@abuso` (ataque deliberado), `@contrato`, `@operacion`.
- **Trazabilidad:** cada fila indica las reglas (RN, SEG, COMP, D) que verifica; cada archivo indica sus CA.
- **"Contiene"** = la respuesta incluye el texto (sin distinguir mayúsculas ni tildes); los números se comparan literalmente con punto decimal. **"Sin datos de terceros"** = sin nombres, IDs ni notas de personas que el usuario no puede ver.
- **"Sin LLM"** = la prueba de integración usa un doble que falla si se invoca.
- **Determinismo:** cada escenario tiene un único resultado esperado. Si el texto libre del LLM no se puede fijar, se afirman el código HTTP, las citas, `usoDatosEstudiante` y la ausencia de datos de terceros (D-23).

## Notación

| Abreviatura | Significado |
| --- | --- |
| COMP-03 | La `respuesta` es exactamente "No encuentro esa información en los documentos disponibles." |
| NEG | `TEXTO_NEGATIVA` del rol del usuario (spec §5) |
| TNV | `TEXTO_NO_VERIFICABLE`: "No pude verificar los datos de la respuesta. Intenta de nuevo o reformula la pregunta." |
| uso · costo | `usoDatosEstudiante` · `costoUsd` |
| citas ⊇ [X] · exactamente [X] | `citas` incluye X · `citas` es X, en ese orden |
| Art. N | `reglamento_academico_2025.md · Artículo N` |
| Sec. 2, Sec. 3, Sec. 4 | `evaluacion_y_calificaciones.md · Sección N` |
| Sec. 3.1, Sec. 3.2, bienestar · Sec. N | `bienestar_y_becas.md · Sección N` |
| Notas · Asistencia · Datos básicos | `Datos del estudiante · <sección>` |
| Q10 #N | La pregunta N de `preguntas_ejemplo.json`, con su usuario |
| doble · "pregunta" → "texto" | El LLM doble; el usuario pregunta y el doble responde ese texto |
| `respondida`, `denegada`… | Valor de `resultado` (o de `motivoDenegacion`) en la auditoría |

## Bloques de IDs por feature

| Feature | Carpeta | Bloque |
| --- | --- | --- |
| 001 · Asistente académico | [001-asistente-academico/](001-asistente-academico/) | ESC-001…ESC-199 |
| 002 · (siguiente) | `002-<slug>/` | ESC-200…ESC-299 |

Los IDs son globales e inmutables: una feature nueva toma el siguiente bloque de 100 libre.

**Continuación del bloque 001.** El bloque tiene 11 números que nunca se usaron (ESC-035 se usó en D-66 y ESC-036…039 en D-67): ESC-047…049, ESC-067…069 y ESC-095…099. Un caso nuevo de la 001 usa, en este orden:

1. uno de esos números libres, en el archivo de su tema;
2. una letra de un escenario existente del mismo tema;
3. agotados ambos, la propuesta es pasar a IDs con prefijo de feature (`ESC-001-200` en adelante, sin chocar con el bloque de la 002), lo que **requiere aprobación humana**.

## Índice de la feature 001

| Archivo | Tema | Escenarios | CA |
| --- | --- | --- | --- |
| [01-preguntas-ejemplo.md](001-asistente-academico/01-preguntas-ejemplo.md) | Las 10 preguntas de Q10 y la fase V | ESC-001…010, ESC-197 | CA-01 |
| [02-reglamento.md](001-asistente-academico/02-reglamento.md) | Reglamento vigente y derogado | ESC-011…019 | CA-03 (ESC-011…016), CA-14 (ESC-017…019) |
| [03-datos-estudiante.md](001-asistente-academico/03-datos-estudiante.md) | Cálculos, resumen propio y formato normativo | ESC-020…034, ESC-154, ESC-155, ESC-172, ESC-189 | CA-02, CA-14, CA-18 |
| [04-coordinador.md](001-asistente-academico/04-coordinador.md) | Programa del coordinador y prelectura | ESC-040…046, ESC-135, ESC-136, ESC-190 | CA-02, CA-04, CA-19 |
| [05-permisos.md](001-asistente-academico/05-permisos.md) | Permisos, normalización, homoglifos y capacidades de otro rol | ESC-036…039, ESC-050…066, ESC-130…134, ESC-137…139, ESC-149, ESC-186, ESC-187, ESC-196, ESC-199 a1–a9, b | CA-04, CA-12, CA-24 |
| [06-inyeccion-y-abuso.md](001-asistente-academico/06-inyeccion-y-abuso.md) | Inyección, fugas e IDs en la salida | ESC-070…089, ESC-145…147, ESC-157, ESC-161, ESC-163, ESC-173, ESC-174, ESC-176 | CA-05 |
| [07-sin-informacion.md](001-asistente-academico/07-sin-informacion.md) | No invención: umbral, exenciones, respaldo y apoyo | ESC-090…094, ESC-165…171 | CA-06, CA-14 |
| [08-contrato-y-operacion.md](001-asistente-academico/08-contrato-y-operacion.md) | Contrato, límites, auditoría, despliegue y CI | ESC-100…119, ESC-143, ESC-144, ESC-148, ESC-150…153, ESC-158…160, ESC-180 | CA-07…CA-11 |
| [09-falsos-positivos-y-suplantacion.md](001-asistente-academico/09-falsos-positivos-y-suplantacion.md) | Sin falsos 403 y suplantación | ESC-120…129, ESC-140…142 | CA-12, CA-13 |
| [10-verificacion-de-datos.md](001-asistente-academico/10-verificacion-de-datos.md) | Texto final verificado, pérdidas, definitivas y reintento | ESC-035, ESC-175, ESC-179, ESC-183…185, ESC-193, ESC-199 g1–g4, h1, h2 | CA-06, CA-16, CA-17, CA-21 |
| [11-citas-y-limpieza.md](001-asistente-academico/11-citas-y-limpieza.md) | Citas, recuperación y limpieza de referencias | ESC-156, ESC-162, ESC-164, ESC-177, ESC-178, ESC-194, ESC-195, ESC-198 | CA-01, CA-14, CA-22 |
| [12-configuracion-y-proveedor.md](001-asistente-academico/12-configuracion-y-proveedor.md) | Configuración, prompt y proveedor del LLM | ESC-181, ESC-182, ESC-188, ESC-191, ESC-192, ESC-199 c1, c2, d, e1–e5, f | CA-15, CA-20, CA-23 |

`10-salida-trazabilidad-y-limites.md`, `11-ronda-4-verificacion-datos-y-determinismo.md` y `12-v1-12-riesgos-y-ambiguedades.md` quedaron retirados (organizados por ronda) y deben eliminarse con `git rm`.
