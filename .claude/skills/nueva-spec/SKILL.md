---
name: nueva-spec
description: Convierte un requisito nuevo (texto libre del cliente) en una spec verificable con reglas de negocio, reglas de seguridad, criterios de aceptación y escenarios felices, alternos y de abuso, lista para que la fábrica la implemente. Úsala cuando el usuario comparta un requisito nuevo o un cambio.
---

# Nueva spec a partir de un requisito

Entrada: el texto del requisito (en `$ARGUMENTS` o en el mensaje).

## Pasos
1. **Rama:** `git switch -c docs/NNN-<slug>` desde `main` actualizado. NNN = siguiente número libre en `specs/`.
2. **Análisis (delegado):** lanza el subagente `arquitecto-spec` con el requisito literal. Pídele `specs/NNN-<slug>.md` según `specs/_plantilla.md`, los escenarios nuevos en `escenarios/` y la fila de trazabilidad en `SPEC.md` §12.
3. **Revisión adversarial (delegada, en paralelo):** cuando exista el borrador, lanza a la vez `auditor-autorizacion` y `auditor-seguridad` con esta instrucción: *"Revisa SOLO la spec `specs/NNN-<slug>.md` y sus escenarios, no el código. ¿Qué vector de acceso indebido, inyección o fuga no está cubierto por un escenario?"*. Incorpora sus faltantes como escenarios nuevos (vía `arquitecto-spec`).
4. **Puerta humana:** muestra al usuario un resumen de una pantalla (alcance, RN y SEG nuevas o cambiadas, número de CA y escenarios por tipo, decisiones `D-xx` y preguntas abiertas). **No continúes a implementación sin su aprobación.** Si corrige algo, aplícalo y registra la intervención con la skill `registrar-intervencion`.
5. **Commit:** `docs(spec): add NNN <slug> spec` (60 caracteres como máximo) con el trailer `Generado-por: Claude Code`.
6. Indica el siguiente paso: `/implementar-spec NNN`.

## Criterios de calidad de la spec (verifícalos antes de la puerta humana)
- [ ] Cada CA es verificable por código (valor, código HTTP, cita o cadena prohibida).
- [ ] Hay al menos un caso alterno, uno de permisos y uno de abuso por cada camino feliz.
- [ ] Los valores esperados calculados muestran su cálculo.
- [ ] No contradice `SPEC.md`; si la modifica, lo dice explícitamente.
- [ ] El contrato de API no cambió, o el cambio está marcado como "requiere aprobación".
