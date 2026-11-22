# INTERVENCIONES.md · Registro de intervenciones humanas

Cada corrección o redirección manual del humano sobre el trabajo de los agentes queda aquí (regla lights-out 4). Formato y procedimiento: skill `registrar-intervencion`.

**Qué cuenta como intervención:** solo cuando el humano corrige, rechaza o reorienta algo que produjo la fábrica. No cuentan: dar el requisito inicial, aprobar una spec sin cambios ni los problemas del entorno (esos van en `REPORTE.md`).

Intervenir no descalifica; esconderlo sí.

### INT-001 · 2026-11-22 · Registro de intervenciones y roles de agentes demasiado segregados
- **Fase:** configuración de la fábrica
- **Qué pasó:** (1) la fábrica registró en `INTERVENCIONES.md` entradas que no eran correcciones del humano: el arranque del repositorio y la falta de .NET SDK, Docker y `gh` en el entorno. (2) La fábrica creó 8 agentes, entre ellos `auditor-autorizacion` y `revisor-codigo` separados de `auditor-seguridad` y `qa-pruebas`.
- **Qué hizo el humano:** indicó: *"En las intervenciones debe de solo estar intervenciones que yo te haga [...] y no por cosas como que me faltaba instalar x,z,y o cosas donde nunca te pedí un cambio"* y *"no hay que tener agentes separados [...] no está bien que se segreguen tanto los roles o agentes en específico"*.
- **Por qué:** las intervenciones deben medir la autonomía real de la fábrica, y un agente por subtema añade costo y coordinación sin aportar calidad.
- **Qué se cambió:** se retiraron las entradas que no correspondían (el bloqueo de entorno pasó a `REPORTE.md` §0). Los agentes se consolidaron en 6: `auditor-seguridad` + `auditor-autorizacion` → `seguridad` (OWASP, vulnerabilidades, roles, bypass e inyección), y `qa-pruebas` + `revisor-codigo` → `qa-calidad` (pruebas y evals primero, revisión de calidad después).
- **Costo:** ~5 min humanos.
- **Prevención:** la skill `registrar-intervencion` define qué cuenta y qué no; `FABRICA.md` §1 fija el principio "un agente por disciplina, no por subtema".
