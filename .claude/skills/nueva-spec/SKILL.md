---
name: nueva-spec
description: Convierte un requisito nuevo (texto libre del cliente) en una spec verificable con reglas de negocio, reglas de seguridad, criterios de aceptación y escenarios felices, alternos y de abuso, lista para que la fábrica la implemente. Úsala cuando el usuario comparta un requisito nuevo o un cambio.
---

# Nueva spec a partir de un requisito

Entrada: el texto del requisito (en `$ARGUMENTS` o en el mensaje).

## Pasos
1. **Rama:** `git switch -c docs/NNN-<slug>` desde `main` actualizado. `NNN-<slug>` es el *feature-id*: siguiente número libre en `specs/` + slug en kebab-case.
2. **Análisis (delegado):** lanza el subagente `arquitecto-spec` con el requisito literal. Pídele `specs/NNN-<slug>/spec.md` según `specs/_plantilla/spec.md`, los escenarios en `escenarios/NNN-<slug>/` y la fila de la feature en el índice de `SPEC.md` §5.
3. **Revisión adversarial (delegada):** cuando exista el borrador, lanza el subagente `seguridad` con esta instrucción: *"Revisa SOLO la spec `specs/NNN-<slug>/spec.md` y sus escenarios, no el código. ¿Qué vector de acceso indebido, inyección o fuga no está cubierto por un escenario, y qué escenario no tiene un resultado determinista?"*. Incorpora sus faltantes como escenarios nuevos (vía `arquitecto-spec`).
4. **Puerta humana:** muestra al usuario un resumen de una pantalla (alcance, RN y SEG nuevas o cambiadas, número de CA y escenarios por tipo, decisiones `D-xx` y preguntas abiertas). **No continúes a implementación sin su aprobación.** Si corrige algo, aplícalo y registra la intervención con la skill `registrar-intervencion`.
5. **Commit:** `docs(spec): add NNN <slug> spec` (60 caracteres como máximo) **sin trailer** (solo toca `.md`: es un commit humano; AGENTS.md §1 regla 2).
6. Indica el siguiente paso: `/implementar-spec NNN`.

## Criterios de calidad de la spec (verifícalos antes de la puerta humana)
- [ ] La spec sigue `specs/_plantilla/spec.md` (RF, comportamiento y UX, backend, frontend, datos, CA y plan de QA) y no contiene guías de metodología.
- [ ] Los CA cumplen el checklist de la skill `criterios-gherkin-smart`.
- [ ] Cada `Entonces` es verificable por código (valor, código HTTP, cita, conteo o cadena prohibida) y cada umbral es numérico.
- [ ] Hay al menos un caso alterno, uno de permisos y uno de abuso por cada camino feliz.
- [ ] Los valores esperados calculados muestran su cálculo.
- [ ] No contradice `SPEC.md` ni copia su contenido transversal; si lo modifica, lo dice explícitamente.
- [ ] El contrato de API no cambió, o el cambio está marcado como "requiere aprobación".
