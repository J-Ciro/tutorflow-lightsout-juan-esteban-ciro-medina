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

### INT-002 · 2026-11-22 · Criterios de aceptación sin formato Gherkin ni SMART
- **Fase:** spec
- **Qué pasó:** la fábrica escribió los criterios de aceptación de `SPEC.md` §11 como una tabla de una línea por CA ("Las 10 preguntas... devuelven la respuesta esperada"), sin un formato ejecutable ni métricas explícitas.
- **Qué hizo el humano:** indicó: *"el lenguaje que se llega a utilizar en este caso de Criterios de Aceptación ya que deberían de estar en formato Gherkin BDD y además teniendo en cuenta la metodología SMART asegurando que estos sean Específico, Medible, Alcanzable, Relevante y Comprobable"*.
- **Por qué:** un CA en Gherkin se traduce directamente a pruebas, y SMART obliga a fijar umbrales medibles en lugar de criterios ambiguos.
- **Qué se cambió:** `SPEC.md` §11 se reescribió: 13 CA en Gherkin en español con `Esquema del escenario` y `Ejemplos`, cada uno con su fila SMART y umbrales (100% en seguridad y cálculos; 100% en 3 corridas en los evals con LLM). Se actualizaron `specs/_plantilla.md`, `arquitecto-spec`, `qa-calidad` y el checklist de `nueva-spec`.
- **Costo:** ~2 min humanos.
- **Prevención:** la plantilla y el checklist de `nueva-spec` exigen Gherkin + SMART, así que la corrida en vivo ya sale con este formato.

### INT-003 · 2026-11-22 · SPEC corrupta al inicio y con contenido que no es del producto
- **Fase:** spec
- **Qué pasó:** (1) el inicio de `SPEC.md` mostraba filas de la tabla de decisiones (D-14, D-09, D-08, D-07 y D-01) pegadas antes del título. Las había insertado ahí un script de edición de la fábrica en el commit `53fd241`, cuya expresión regular quedó mal escapada; las filas viejas seguían en la tabla. (2) La sección de criterios de aceptación incluía explicaciones de metodología (qué es SMART, umbrales generales, un ejemplo) en lugar de solo el producto.
- **Qué hizo el humano:** señaló que el inicio *"parece que el contenido está cortado o incorrecto"* y pidió *"quitar explicaciones como es el caso de Criterios de Aceptación [...] esto es mejor tenerlo como Skills, en algún agente o en CLAUDE.md"*, y que la SPEC cubra *"Requerimientos Funcionales, los Criterios de Aceptación, backend, front si lo realizara esta SPEC u otra, temas de datos, funcional, UX/UI, y qa"*.
- **Por qué:** la SPEC debe describir solo qué se construye, cómo, por qué y cómo se verifica; la guía de redacción es conocimiento de la fábrica.
- **Qué se cambió:** se reconstruyó `SPEC.md` (v1.1) con la estructura contexto → alcance → actores → RF → comportamiento y UX → RN → SEG → RNF → backend → frontend y UX/UI → datos → CA en Gherkin → plan de QA → decisiones → trazabilidad. Se añadieron RF-01…11, UX-01…05, RNF-01…08, la integración con la API académica y el plan de QA (niveles, matriz CA → nivel → ESC → umbral, casos límite, dobles y criterios de salida). La guía Gherkin + SMART pasó a la skill `criterios-gherkin-smart`, y `specs/_plantilla.md` sigue la misma estructura.
- **Costo:** ~3 min humanos.
- **Prevención:** las ediciones por script se hacen con operaciones de texto sin regex y se verifican antes del commit (`head`, `grep` de filas duplicadas o pegadas). El checklist de `nueva-spec` exige la estructura de la plantilla sin guías de metodología (ver `REPORTE.md` §4).

### INT-004 · 2026-11-22 · CLAUDE.md sin contexto del proyecto
- **Fase:** configuración de la fábrica
- **Qué pasó:** `CLAUDE.md` solo importaba `AGENTS.md` (con reglas de código y de seguridad) y tenía notas de uso, sin explicar el proyecto.
- **Qué hizo el humano:** pidió que `CLAUDE.md` explique de qué trata el proyecto, su fin y su contexto, con el stack tecnológico, el mapa del repositorio, los comandos esenciales y las rutas de skills y agentes, en máximo 200 *(interpretado como líneas)*, y *"no debes de agregar Formateos de código, lint rules, Mejores prácticas, ni bloqueos de seguridad"*.
- **Por qué:** `CLAUDE.md` se carga en cada sesión: debe dar contexto y orientación, no reglas.
- **Qué se cambió:** se reescribió `CLAUDE.md` (≈70 líneas) con qué es, para qué, contexto clave, stack, mapa, agentes y skills, y comandos. Se quitó la importación de `AGENTS.md`: queda como referencia.
- **Costo:** ~2 min humanos.
- **Prevención:** las reglas viven en `AGENTS.md`, los agentes y las skills; `CLAUDE.md` solo da contexto.
