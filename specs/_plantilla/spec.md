# SPEC-NNN · <Nombre de la feature>

> Regla: ≤ 350 líneas; una sola feature; solo producto (comportamiento observable). Prohibido aquí: algoritmos, umbrales internos, nombres de clases, prompt, formatos internos, adaptadores, claves de configuración, pseudocódigo, prosa larga, repeticiones e historia de versiones. Lo técnico va en `plan.md`; las decisiones, en `decisiones.md`; lo global, en `constitution.md`. Una corrección actualiza la regla en su sitio, no agrega texto.

| Campo | Valor |
| --- | --- |
| Versión | 1.0 · AAAA-MM-DD · borrador / aprobada / implementada |
| Requisito original | (texto literal del cliente) |
| Reglas globales | [`constitution.md`](../../constitution.md) |
| Técnico | [`plan.md`](plan.md) · Decisiones: [`decisiones.md`](decisiones.md) · Escenarios: [`escenarios/NNN-<slug>/`](../../escenarios/) (ESC-N00…ESC-N99) |
| Afecta | Features o reglas existentes (IDs) o "nuevo" |

## 1. Objetivo
Una o dos oraciones: qué problema del usuario resuelve.

## 2. Alcance
**Dentro del alcance:** …

**Fuera del alcance:** …

## 3. Flujos de usuario
| Flujo | Pasos |
| --- | --- |
| F1 · <actor> <intención> | 1) … → 2) … → 3) resultado visible |

## 4. Requerimientos funcionales
- **RF-NNN-01** · <requerimiento> (actor). CA-NNN-01.

## 5. Comportamiento y experiencia
- **COMP-NNN-01** · <comportamiento observable, 1 o 2 líneas>.
- **UX-NNN-01** · <regla de experiencia>.

## 6. Reglas de negocio
- **RN-NNN-01** · <regla> (fuente: documento · sección o dato).

## 7. Seguridad y permisos
Solo lo nuevo o cambiado respecto de la matriz de `constitution.md` §3 (ampliarla requiere aprobación humana).
- **SEG-NNN-01** · <regla observable> → <respuesta ante la violación>.

## 8. Requerimientos no funcionales
- **RNF-NNN-01** · <requerimiento con medida>.

## 9. Criterios de aceptación
Gherkin en español según la skill `criterios-gherkin-smart`: un `Escenario` o `Esquema` por CA, ≤ 5 filas, con "Detalle: ESC-…" en el título. El umbral de cada CA está en `plan.md` (plan de QA).
```gherkin
# language: es
Característica: <feature>
  Esquema del escenario: CA-NNN-01 · <comportamiento> (Detalle: ESC-N00…)
    Dado …
    Cuando …
    Entonces …
    Ejemplos:
      | … |
```

## 10. Preguntas abiertas
- Ninguna.
