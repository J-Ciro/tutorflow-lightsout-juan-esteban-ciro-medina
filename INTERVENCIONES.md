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
- **Qué pasó:** (1) el inicio de `SPEC.md` mostraba filas de la tabla de decisiones (D-14, D-09, D-08, D-07 y D-01) pegadas antes del título. Las había insertado ahí un script de edición de la fábrica en el commit `805814e`, cuya expresión regular quedó mal escapada; las filas viejas seguían en la tabla. (2) La sección de criterios de aceptación incluía explicaciones de metodología (qué es SMART, umbrales generales, un ejemplo) en lugar de solo el producto.
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

### INT-005 · 2026-11-22 · Pruebas de API con Postman CLI
- **Fase:** configuración de la fábrica
- **Qué pasó:** los agentes de backend y QA solo tenían `curl` para validar la API levantada. Al pedir Postman, la fábrica eligió primero Newman (el runner open source).
- **Qué hizo el humano:** pidió dar a los agentes de backend y QA acceso a la CLI de Postman *"para que también puedan validar no solo con curl"*. Ante la elección de Newman, preguntó *"¿pero por qué newman? postman tiene su CLI [...] y usa el comando npm install -g postman-cli"*.
- **Por qué:** Postman CLI es la herramienta oficial y la que se mantiene activamente; Newman tiene mucho menos desarrollo.
- **Qué se cambió:** `qa-calidad` escribe y ejecuta la colección `tests/postman/` (contrato, permisos y abuso deterministas, por ID de escenario); `backend-dotnet` la ejecuta antes de reportar (sin poder modificarla); `devops-cicd` la corre en el trabajo `docker` del CI con reporte JUnit; el orquestador la re-ejecuta en `implementar-spec`. La colección quedó en el plan de QA de la spec 001 y en `CLAUDE.md`. **Pendiente:** el permiso en `.claude/settings.json` (`postman collection run` y `curl` solo a localhost). La fábrica intentó agregarlo y el control de permisos de Claude Code lo bloqueó; lo agrega el humano (mientras tanto, cada ejecución pide aprobación).
- **Costo:** ~2 min humanos.
- **Prevención:** ante una herramienta pedida por nombre, usar la oficial que nombra el humano y preguntar si hay ambigüedad (se preguntó "podman" frente a "postman").

### INT-006 · 2026-11-22 · Plugin oficial de Postman, solo para validación local
- **Fase:** configuración de la fábrica
- **Qué pasó:** la fábrica había integrado Postman CLI con una colección v2.1 en `tests/postman/` y la había puesto a correr en el trabajo `docker` del CI (INT-005).
- **Qué hizo el humano:** pidió usar *"los plugins de postman cli que nos permiten hacer api-discovery, api-testing, api-mocking, performance-testing, api-monitoring"* e instaló el plugin (`claude plugin install postman@claude-plugins-official --scope project`), porque la instalación desde la sesión de la fábrica falló sin acceso a `github.com`. Después aclaró: *"Esto es más para ejecuciones locales que implementar postman en el CI CD"*.
- **Por qué:** Postman es la herramienta de validación de los agentes en local; el CI mantiene sus propios gates.
- **Qué se cambió:** se adoptó la convención del plugin: colecciones v3 en YAML en `postman/collections/tutorflow/`, entornos en `postman/environments/` y simulaciones en `postman/simulations/`. Skills por agente: `qa-calidad` (api-testing, collection-schema-v3, variables, performance-testing para RNF-01, api-simulation para los fallos de la API académica), `backend-dotnet` (api-testing, api-simulation) y `seguridad` (api-testing). El trabajo `docker` del CI volvió a una prueba de humo con `curl`. `AGENTS.md` §7 prohíbe usar la nube de Postman sin aprobación humana (login, workspaces, publicación, monitores, runners en la nube, escrituras por el MCP remoto). No se adoptaron `api-monitoring` (requiere un endpoint desplegado y monitores en la nube), `api-discovery` ni `api-documentation` (publicación en la nube): no aportan a una validación local.
- **Costo:** ~5 min humanos (incluye la instalación manual).
- **Prevención:** al integrar una herramienta, preguntar desde el principio si es para local, para el CI o para ambos.

### INT-007 · 2026-11-22 · El LLM debe funcionar con la llave de cualquier proveedor
- **Fase:** spec
- **Qué pasó:** al confirmar D-16, el humano eligió Anthropic como proveedor por defecto, y la fábrica empezó a especificar Anthropic (`claude-haiku-5-5`) como el proveedor fijo de la app, con Ollama solo para pruebas locales.
- **Qué hizo el humano:** aclaró: *"no debe de ser solo haiku o de anthropic, la idea es que indiferente a la API key que ponga pueda usarlo"*. Antes había pedido usar Ollama local para las pruebas y *"una API key para que cualquier persona al poner su api key pueda usar la app"*.
- **Por qué:** cualquier evaluador o usuario debe poder usar su propia llave, sea del proveedor que sea.
- **Qué se cambió:** D-16 se reescribió en `SPEC.md` y se añadió la §9.4 a la spec 001: puerto `IClienteLlm` con un adaptador de Anthropic (SDK oficial) y otro compatible con OpenAI (OpenAI, Groq, OpenRouter, Gemini, DeepSeek, Mistral y Ollama). El proveedor se detecta por el prefijo de la llave y se puede forzar con `LLM_PROVEEDOR`, `LLM_URL_BASE` y `LLM_MODELO`; sin llave se usa Ollama con `qwen2.5:7b`. Hay precios por modelo en la configuración y un escenario nuevo, ESC-119. El límite de salida pasó de 600 a 2.000 tokens configurables, porque en los modelos con razonamiento ese límite también lo incluye. Se actualizaron `AGENTS.md`, `CLAUDE.md`, `backend-dotnet` y `qa-calidad`.
- **Costo:** ~2 min humanos.
- **Prevención:** las decisiones de proveedor y dependencias externas se especifican tras un puerto desde el principio; antes de fijar un proveedor, se confirma si debe ser intercambiable.

### INT-008 · 2026-11-22 · Backend en .NET 11 en lugar de .NET 8
- **Fase:** spec
- **Qué pasó:** la fábrica fijó el backend en .NET 8 (el mínimo del PDF) en `AGENTS.md`, `CLAUDE.md`, la spec 001 y los agentes, y le pidió al humano instalar el SDK de .NET 8.
- **Qué hizo el humano:** indicó *"No vamos a usar dotnet 8 sino Dotnet 11"*.
- **Por qué:** decisión del humano; el PDF admite ".NET 8 o superior" y el SDK 11.0.100 ya es estable en `winget`.
- **Qué se cambió:** se pasó a .NET 11 (`net11.0`) en `AGENTS.md`, `CLAUDE.md`, la spec 001, `backend-dotnet` y `devops-cicd`. Las imágenes Docker pasan a `sdk:11.0` y `aspnet:11.0`, y el SDK se fija en `global.json`, que también lee el CI. Ya no se nombra una versión fija de C#.
- **Costo:** ~1 min humano.
- **Prevención:** la versión del runtime queda en una sola fuente (`global.json`) y no repetida en varios documentos.

### INT-009 · 2026-11-22 · TDD obligatorio y código sin comentarios
- **Fase:** implementación (corrida 1, durante el esqueleto)
- **Qué pasó:** `AGENTS.md` §3 pedía comentarios que explicaran el porqué y citaran la regla (`// RN-07…`), y el TDD solo estaba implícito en el ciclo de `implementar-spec`, sin una skill propia.
- **Qué hizo el humano:** entregó la skill `tdd` (*"esto es CRÍTICO antes de escribir cualquier LÍNEA de código"*) e indicó: *"NUNCA agregues comentarios al código, ya que el código debe de hablar por sí mismo y ser fácil de entender, leer y comprender"*.
- **Por qué:** el comportamiento se demuestra con una prueba en rojo antes del código, y la legibilidad se logra con nombres, no con comentarios.
- **Qué se cambió:** se agregó `.claude/skills/tdd/SKILL.md` tal como la entregó el humano. Se volvió obligatoria en `AGENTS.md` §4, `backend-dotnet`, `qa-calidad` e `implementar-spec`: la skill trae `disable-model-invocation`, así que los agentes la siguen porque se les ordena explícitamente leerla. La regla de comentarios se reemplazó por *"sin comentarios en el código"*: la trazabilidad a las RN va en los `[Trait]` de las pruebas, en la spec y en los commits, y `qa-calidad` trata cualquier comentario como hallazgo alto. Se quitaron los comentarios de los hooks de la fábrica y se avisó al agente que estaba creando el esqueleto.
- **Costo:** ~2 min humanos.
- **Prevención:** la revisión de `qa-calidad` (fase B) bloquea cualquier comentario en el código.

### INT-010 · 2026-11-22 · Trailer solo en commits de agentes y nunca Co-Authored-By
- **Fase:** configuración de la fábrica
- **Qué pasó:** todos los commits, incluidos los que solo tocaban Markdown y configuración de agentes, llevaban `Generado-por: Claude Code` y `Co-Authored-By: Claude …`. El hook solo exigía el trailer cuando había código y no impedía ninguna de las dos cosas en los demás casos.
- **Qué hizo el humano:** indicó: *"JAMÁS agregues co-autoría de tipo Co-Authored-By"* y señaló que no se había respetado la regla *"cada commit de un agente debe llevar el trailer Generado-por: <herramienta>. Nuestros commits solo tocan archivos .md y las configuraciones de los agentes, es decir los commits humanos"*.
- **Por qué:** el trailer debe distinguir el trabajo de los agentes del trabajo humano. Si aparece en todos los commits, deja de informar.
- **Qué se cambió:** el hook `.githooks/commit-msg` ahora rechaza cualquier `Co-Authored-By` (también en merges), exige `Generado-por` cuando el commit toca algo distinto de `.md`, `.claude/` y `.githooks/`, y lo rechaza en los commits humanos. Se probaron 7 casos y todos dieron el resultado esperado. Se actualizaron `AGENTS.md` §1 y §6, `backend-dotnet`, `qa-calidad` y `nueva-spec`, y se avisó al agente que estaba trabajando. Los commits anteriores no se reescriben (el PDF prohíbe reescribir el historial).
- **Costo:** ~1 min humano.
- **Prevención:** el hook bloquea las tres violaciones antes de crear el commit.

### INT-011 · 2026-11-22 · Separar backend y frontend en carpetas propias
- **Fase:** implementación (corrida 1, tras las pruebas en rojo)
- **Qué pasó:** la solución .NET (`src/`, `tests/`, `evals/`, `postman/`, `global.json` y las propiedades compartidas) estaba mezclada en la raíz con las specs, los escenarios y la configuración de la fábrica.
- **Qué hizo el humano:** pidió *"tener una carpeta donde tengamos back y front, permitiendo así tener todo más ordenado"* y aprobó la estructura propuesta.
- **Por qué:** separar el producto (backend y frontend) de la documentación y de la fábrica hace el repositorio más fácil de recorrer.
- **Qué se cambió:** todo el .NET pasó a `backend/` con `git mv`, conservando la historia. Las carpetas `src` y `tests` estaban bloqueadas por Windows y se movieron archivo por archivo. Se creó `frontend/`, que reemplaza a `web/`. `docker-compose.yml`, `mock-api/` y `reglamento/` quedan en la raíz. Tres rutas de las pruebas y del runner ahora incluyen `backend/`. Antes y después del cambio se obtuvo el mismo resultado: 380 y 316 pruebas en rojo, 7 y 14 en verde, evals en fallo y la colección de Postman sin errores de lint. Se actualizaron `AGENTS.md` §2 (los comandos `dotnet` y `postman` se ejecutan desde `backend/`), `CLAUDE.md`, `FABRICA.md`, los agentes, `implementar-spec` y el plan de QA de la spec 001. Además se documentó en `AGENTS.md` §3 la excepción CA1707 en las pruebas.
- **Costo:** ~2 min humanos.
- **Prevención:** la estructura queda fijada en `AGENTS.md` §2; cualquier carpeta nueva de producto va dentro de `backend/` o `frontend/`.
