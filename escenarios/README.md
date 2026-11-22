# Escenarios de aceptación

Lenguaje natural, formato **Dado / Cuando / Entonces**. Cada escenario se convierte en al menos una prueba o eval que lo referencia por su ID (`qa-pruebas`).

## Convenciones

- **ID:** `ESC-NNN`, único y nunca reutilizado. Un escenario retirado se marca como `~~retirado~~` y no se borra.
- **Etiquetas:** `@feliz`, `@alterno` (límites, nulos, vacíos), `@permisos`, `@abuso` (ataque deliberado), `@contrato`, `@operacion`.
- **Trazabilidad:** cada escenario indica su CA y las reglas (RN/SEG/COMP) de `SPEC.md`.
- **"Contiene"** significa que la respuesta incluye el texto (sin distinguir mayúsculas ni tildes); los números se comparan literalmente con punto decimal.
- **"No contiene datos de terceros"** significa que la respuesta no incluye nombres ni IDs de estudiantes que el usuario no puede ver, ni sus notas.
- Cuando un escenario dice **"sin llamar al LLM"**, la prueba de integración lo verifica con un doble que falla si se invoca.

## Índice

| Archivo | Rango | CA |
| --- | --- | --- |
| [01-preguntas-ejemplo.md](01-preguntas-ejemplo.md) | ESC-001…010 | CA-01 |
| [02-reglamento.md](02-reglamento.md) | ESC-011…019 | CA-03 |
| [03-datos-estudiante.md](03-datos-estudiante.md) | ESC-020…039 | CA-02 |
| [04-coordinador.md](04-coordinador.md) | ESC-040…049 | CA-02, CA-04 |
| [05-permisos.md](05-permisos.md) | ESC-050…069 | CA-04 |
| [06-inyeccion-y-abuso.md](06-inyeccion-y-abuso.md) | ESC-070…089 | CA-05 |
| [07-sin-informacion.md](07-sin-informacion.md) | ESC-090…099 | CA-06 |
| [08-contrato-y-operacion.md](08-contrato-y-operacion.md) | ESC-100…119 | CA-07…CA-11 |

Requisitos nuevos: continúan la numeración en el siguiente bloque libre de 100 (ESC-200…, ESC-300…).
