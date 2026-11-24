---
name: nueva-spec
description: Convierte un requisito nuevo (texto libre del cliente) en una spec verificable con reglas de negocio, reglas de seguridad, criterios de aceptación y escenarios felices, alternos y de abuso, lista para que la fábrica la implemente. Úsala cuando el usuario comparta un requisito nuevo o un cambio.
---

# Nueva spec a partir de un requisito

Entrada: el texto del requisito (en `$ARGUMENTS` o en el mensaje).

## Pasos
1. **Rama:** `git switch -c docs/NNN-<slug> develop` (con `develop` actualizado). `NNN-<slug>` es el *feature-id*: siguiente número libre en `specs/` + slug en kebab-case.
2. **Análisis (delegado):** lanza el subagente `arquitecto-spec` con el requisito literal. Pídele `specs/NNN-<slug>/spec.md`, `plan.md` y `decisiones.md` según `specs/_plantilla/`, los escenarios en `escenarios/NNN-<slug>/` y la fila de la feature en la tabla de features de `SPEC.md`.
3. **Revisión adversarial (delegada):** cuando exista el borrador, lanza el subagente `seguridad` con esta instrucción: *"Revisa SOLO la spec `specs/NNN-<slug>/spec.md` y sus escenarios, no el código. ¿Qué vector de acceso indebido, inyección o fuga no está cubierto por un escenario, y qué escenario no tiene un resultado determinista? Enumera, para cada par de roles (incluido el mismo rol con otra identidad), las preguntas de un rol sobre las capacidades, datos, herramientas o permisos del otro, y exige un escenario para cada una (INT-024)."*. Incorpora sus faltantes como escenarios nuevos (vía `arquitecto-spec`).
4. **Puerta humana:** muestra al usuario un resumen de una pantalla (alcance, RN y SEG nuevas o cambiadas, número de CA y escenarios por tipo, decisiones `D-xx`, preguntas abiertas y los tamaños medidos con `wc -l` de `spec.md`, `plan.md`, `decisiones.md`, `SPEC.md` y `constitution.md`). **No continúes a implementación sin su aprobación.** Si corrige algo, aplícalo y registra la intervención con la skill `registrar-intervencion`.
5. **Commit:** `docs(spec): add NNN <slug> spec` (60 caracteres como máximo) **sin trailer** (solo toca `.md`: es un commit humano; AGENTS.md §1 regla 2).
6. Abre el PR hacia `develop` con la skill `abrir-pr`; tras el merge humano, el siguiente paso es `/implementar-spec NNN`.

## Criterios de calidad de la spec (verifícalos antes de la puerta humana)
- [ ] La spec, el plan y las decisiones siguen sus plantillas de `specs/_plantilla/` y no contienen guías de metodología.
- [ ] Los CA cumplen el checklist de la skill `criterios-gherkin-smart`.
- [ ] Cada `Entonces` es verificable por código (valor, código HTTP, cita, conteo o cadena prohibida) y cada umbral es numérico.
- [ ] Hay al menos un caso alterno, uno de permisos y uno de abuso por cada camino feliz.
- [ ] Los valores esperados calculados muestran su cálculo.
- [ ] No contradice `SPEC.md` ni `constitution.md` ni copia su contenido transversal; si lo modifica, lo dice explícitamente.
- [ ] El contrato de API no cambió, o el cambio está marcado como "requiere aprobación".
- [ ] Cada riesgo identificado tiene una decisión: eliminado, mitigado con un control verificable o aceptado con aprobación humana explícita. Ninguno queda solo "registrado" (INT-021).
- [ ] Tamaños (INT-023): `spec.md` ≤ 350 líneas, `plan.md` ≤ 700, cada D ≤ 3 líneas, `SPEC.md` ≤ 60, `constitution.md` ≤ 150, ninguna línea de más de 400 caracteres. El CI lo verifica.
- [ ] `spec.md` no contiene algoritmos, umbrales internos, nombres de clases, prompt, formatos internos, adaptadores, claves de configuración, pseudocódigo, ejemplos resueltos ni historia de versiones.
- [ ] Cada CA tiene como máximo 1 `Escenario` o `Esquema` con ≤ 5 filas y su "Detalle: ESC-…".
- [ ] Corrección por reorganización, no por acumulación: la regla se reemplaza en su sitio y la D va a `decisiones.md`, sin texto tachado ni "hasta la v1.x".
