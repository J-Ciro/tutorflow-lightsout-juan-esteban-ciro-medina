# SPEC-NNN · <Nombre del requisito>

| Campo | Valor |
| --- | --- |
| Estado | borrador / aprobada / implementada |
| Requisito original | (texto literal del cliente) |
| Afecta | RN-xx, SEG-xx, CA-xx de `SPEC.md` (o "nuevo") |
| Rama | `feat/NNN-<slug>` |

## 1. Objetivo
Una o dos frases: qué valor entrega y a quién.

## 2. Alcance
- **Incluye:**
- **No incluye:**

## 3. Reglas de negocio
| ID | Regla | Fuente (documento · sección o dato) |
| --- | --- | --- |
| RN-NNN-01 | | |

## 4. Reglas de seguridad y permisos
| ID | Regla | Rol afectado | Respuesta ante violación |
| --- | --- | --- | --- |
| SEG-NNN-01 | | | 403 |

Matriz de acceso del requisito (rol × recurso → permitido/denegado).

## 5. Contrato
Cambios al contrato de API: **ninguno** / descripción (requiere aprobación humana).

## 6. Criterios de aceptación (Gherkin + SMART)

Formato obligatorio, igual que `SPEC.md` §11: por cada CA, una fila SMART y un bloque Gherkin en español.

### CA-NNN-01 · <comportamiento en una frase>

| E (específico) | M (medible) | A (alcanzable) | R (relevante) | C (comprobable) |
| --- | --- | --- | --- | --- |
| Un comportamiento, un actor | Valor, HTTP, cita, conteo o umbral (%, corridas) | Con qué datos o fuentes se cumple | Prioridad de §1 y reglas RN/SEG | Nivel de prueba (unitaria, integración, eval) + ESC |

```gherkin
# language: es
Característica: <capacidad>

  Esquema del escenario: <comportamiento>
    Dado el usuario "<rol>" con id "<usuario>"
    Cuando pregunta "<pregunta>"
    Entonces el código HTTP es <http>
    Y la "respuesta" contiene "<valor>"

    Ejemplos:
      | rol | usuario | pregunta | http | valor |
```

Reglas: cada `Entonces` lleva un valor literal o un umbral; prohibido "correctamente", "adecuadamente", "rápido" o "seguro". Un `Escenario` por comportamiento. Los casos de seguridad y cálculo exigen 100%; los que dependen del texto del LLM, 100% en 3 corridas.

## 7. Escenarios
Enlace a los escenarios nuevos en `escenarios/` con este mínimo por camino feliz: 1 feliz, 1 alterno, 1 de permisos y 1 de abuso.

## 8. Datos de referencia
Valores esperados calculados con su cálculo.

## 9. Decisiones
| ID | Ambigüedad | Decisión | Por qué | ¿Aprobación humana? |
| --- | --- | --- | --- | --- |

## 10. Preguntas abiertas
-
