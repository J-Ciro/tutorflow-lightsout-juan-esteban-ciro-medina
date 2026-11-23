# Escenarios de aceptación

Lenguaje natural, formato **Dado / Cuando / Entonces**. Los escenarios se agrupan por feature en `escenarios/<feature-id>/`, con el mismo *feature-id* de `specs/<feature-id>/spec.md`, y cada archivo enlaza su spec en el encabezado. Cada escenario se convierte en al menos una prueba o eval que lo referencia por su ID (`qa-calidad`).

## Convenciones

- **ID:** `ESC-NNN`, único y nunca reutilizado. Un escenario retirado se marca como `~~retirado~~` y no se borra.
- **Etiquetas:** `@feliz`, `@alterno` (límites, nulos, vacíos), `@permisos`, `@abuso` (ataque deliberado), `@contrato`, `@operacion`.
- **Trazabilidad:** cada escenario indica su CA y las reglas (RN/SEG/COMP) de la spec de su feature.
- **"Contiene"** significa que la respuesta incluye el texto (sin distinguir mayúsculas ni tildes); los números se comparan literalmente con punto decimal.
- **"No contiene datos de terceros"** significa que la respuesta no incluye nombres ni IDs de estudiantes que el usuario no puede ver, ni sus notas.
- Cuando un escenario dice **"sin llamar al LLM"**, la prueba de integración lo verifica con un doble que falla si se invoca.

## Bloques de IDs por feature

| Feature | Carpeta | Bloque de IDs |
| --- | --- | --- |
| 001 · Asistente académico | [001-asistente-academico/](001-asistente-academico/) | ESC-001…ESC-199 |
| 002 · (siguiente) | `002-<slug>/` | ESC-200…ESC-299 |

Los IDs son globales e inmutables: una feature nueva toma el siguiente bloque de 100 libre.

## Índice de la feature 001

| Archivo | Rango | CA |
| --- | --- | --- |
| [01-preguntas-ejemplo.md](001-asistente-academico/01-preguntas-ejemplo.md) | ESC-001…010 | CA-01 |
| [02-reglamento.md](001-asistente-academico/02-reglamento.md) | ESC-011…019 | CA-03 |
| [03-datos-estudiante.md](001-asistente-academico/03-datos-estudiante.md) | ESC-020…039 | CA-02 |
| [04-coordinador.md](001-asistente-academico/04-coordinador.md) | ESC-040…049 | CA-02, CA-04 |
| [05-permisos.md](001-asistente-academico/05-permisos.md) | ESC-050…069 | CA-04 |
| [06-inyeccion-y-abuso.md](001-asistente-academico/06-inyeccion-y-abuso.md) | ESC-070…089 | CA-05 |
| [07-sin-informacion.md](001-asistente-academico/07-sin-informacion.md) | ESC-090…099, ESC-165…170 | CA-06, CA-14 |
| [08-contrato-y-operacion.md](001-asistente-academico/08-contrato-y-operacion.md) | ESC-100…119, ESC-143…144 | CA-07…CA-11 |
| [09-falsos-positivos-y-suplantacion.md](001-asistente-academico/09-falsos-positivos-y-suplantacion.md) | ESC-120…142 | CA-04, CA-12, CA-13 |
| [10-salida-trazabilidad-y-limites.md](001-asistente-academico/10-salida-trazabilidad-y-limites.md) | ESC-145…164 | CA-04, CA-05, CA-07, CA-08, CA-14 |

Siguiente ID libre de la feature 001: ESC-171.

**Regla de determinismo:** cada escenario tiene un único resultado esperado. Si el texto libre del LLM no se puede fijar, se afirman el código HTTP, las citas, `usoDatosEstudiante` y la ausencia de datos de terceros (D-23).

