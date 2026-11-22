---
name: criterios-gherkin-smart
description: Guía para redactar criterios de aceptación en Gherkin (BDD) en español que cumplan SMART (Específico, Medible, Alcanzable, Relevante, Comprobable), con su matriz de verificación en el plan de QA. Úsala siempre que escribas o revises la sección de criterios de aceptación de una spec en specs/<feature-id>/spec.md.
---

# Criterios de aceptación en Gherkin + SMART

La spec contiene **solo** los CA y la matriz de verificación. Esta guía (el *cómo* escribirlos) vive aquí y no se copia en la spec.

## SMART aplicado a un CA

| Letra | Significa | Se cumple cuando |
| --- | --- | --- |
| **E**specífico | Un solo comportamiento observable, con un actor y una acción concretos | Un `Escenario` por comportamiento; el título dice qué pasa, no "funciona bien" |
| **M**edible | El resultado es un valor, un código HTTP, una cita, un conteo o una cadena prohibida | Cada `Entonces` tiene un valor literal; el umbral va en la matriz de QA |
| **A**lcanzable | Se puede cumplir con el contrato, los datos y los documentos disponibles | Los valores esperados salen de los datos de referencia o de los documentos, con su cálculo |
| **R**elevante | Protege una prioridad de la spec y una regla RN/SEG/COMP | El CA aparece en la columna CA de un RF |
| **C**omprobable | Existe una prueba automática que lo ejecuta en el CI | El CA tiene una fila en la matriz de verificación (nivel, ESC y umbral) |

## Gherkin en español

Palabras clave: `# language: es`, `Característica`, `Antecedentes`, `Escenario`, `Esquema del escenario`, `Ejemplos`, `Dado`, `Cuando`, `Entonces`, `Y`, `Pero`.

```gherkin
# language: es
Característica: Control de acceso por rol

  Esquema del escenario: Acceso denegado antes de llamar al LLM
    Dado el usuario "<rol>" con id "<usuario>"
    Cuando pregunta "<pregunta>"
    Entonces el código HTTP es 403
    Y el LLM recibió 0 llamadas

    Ejemplos:
      | rol        | usuario | pregunta                          |
      | estudiante | EST-001 | ¿Cuáles son las notas de EST-002? |
```

## Reglas

1. `Esquema del escenario` + `Ejemplos` cuando el comportamiento es el mismo con distintos datos; no repitas escenarios casi iguales.
2. Cada `Entonces`/`Y` afirma algo verificable por código. **Prohibido:** "correctamente", "adecuadamente", "rápido", "seguro", "amigable", "según corresponda".
3. Los pasos describen comportamiento observable (HTTP, JSON, auditoría, llamadas a dobles), no detalles de implementación (clases, métodos).
4. Cada CA tiene **una fila en la matriz de verificación** del plan de QA: `CA | qué se verifica | nivel | escenarios | umbral`.
5. **Umbrales por defecto:** seguridad y cálculos al **100%** en cada corrida; lo que depende del texto del LLM al **100% en 3 corridas consecutivas**; tiempos con un número y una unidad (p. ej. "en menos de 500 ms p95").
6. Por cada camino feliz: al menos un caso alterno (límite, nulo, vacío), uno de permisos y uno de abuso.
7. Si un resultado no se puede fijar (texto libre del LLM), se afirma el código, las citas, `usoDatosEstudiante` y la ausencia de cadenas prohibidas.

## Checklist antes de entregar
- [ ] Cada CA tiene `# language: es` y al menos un `Escenario` o `Esquema del escenario`.
- [ ] Ningún paso usa palabras prohibidas.
- [ ] Cada CA está en la matriz de verificación con un umbral numérico.
- [ ] Cada CA está enlazado desde al menos un RF.
- [ ] Los valores esperados coinciden con los datos de referencia.
