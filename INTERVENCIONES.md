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
- **Qué pasó:** (1) el inicio de `SPEC.md` mostraba filas de la tabla de decisiones (D-14, D-09, D-08, D-07 y D-01) pegadas antes del título. Las había insertado ahí un script de edición de la fábrica en el commit `2c176c2`, cuya expresión regular quedó mal escapada; las filas viejas seguían en la tabla. (2) La sección de criterios de aceptación incluía explicaciones de metodología (qué es SMART, umbrales generales, un ejemplo) en lugar de solo el producto.
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
- **Qué se cambió:** el hook `.githooks/commit-msg` ahora rechaza cualquier `Co-Authored-By` (también en merges), exige `Generado-por` cuando el commit toca algo distinto de `.md`, `.claude/` y `.githooks/`, y lo rechaza en los commits humanos. Se probaron 7 casos y todos dieron el resultado esperado. Se actualizaron `AGENTS.md` §1 y §6, `backend-dotnet`, `qa-calidad` y `nueva-spec`, y se avisó al agente que estaba trabajando. Como el repositorio todavía no se había compartido, a pedido del humano se reescribieron **solo los mensajes** de todo el historial (sin tocar el contenido: los árboles de las 16 ramas quedaron idénticos) y se recreó el repositorio privado, sin force push. Tras la reescritura, 0 de 65 commits violan la regla.
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

### INT-012 · 2026-11-22 · Rama develop y pull requests con título y descripción
- **Fase:** configuración de la fábrica
- **Qué pasó:** las ramas de trabajo se integraban directo a `main` con merges locales, y al abrir un PR en GitHub el título salía del nombre de la rama ("Feat/001 asistente academico") y la descripción quedaba vacía.
- **Qué hizo el humano:** pidió *"crear una rama develop y allí subir los cambios"* y señaló que *"las branches están quedando sin commits ni título automático"*.
- **Por qué:** un flujo con `develop` separa la integración continua de la entrega, y un PR con título y descripción deja trazabilidad en GitHub.
- **Qué se cambió:** se creó `develop` desde `main`. `AGENTS.md` §6 ahora dice que las ramas salen de `develop` y se integran por PR con "Create a merge commit" (nunca Squash ni Rebase), y que `develop` pasa a `main` solo en la entrega. El hook `pre-commit` bloquea los commits directos en `main` y `develop`. Nueva skill `abrir-pr`: publica la rama y genera el enlace de GitHub con el título en Conventional Commits y la descripción ya llena (evidencia, auditorías, intervenciones y lista de commits), sin depender de `gh`. Se agregó `.github/pull_request_template.md`, y se actualizaron `implementar-spec`, `nueva-spec`, `FABRICA.md`, `CLAUDE.md` y los disparadores del CI (aviso a `devops-cicd`).
- **Costo:** ~1 min humano.
- **Prevención:** el hook impide trabajar directo en `develop` o `main`, y la skill `abrir-pr` es el único camino de integración.

### INT-013 · 2026-11-22 · Microsoft.Extensions.AI y auditoría con detalle y persistencia
- **Fase:** implementación (corrida 1, durante el paso 2)
- **Qué pasó:** el adaptador compatible con OpenAI se estaba escribiendo a mano con `HttpClient`. La auditoría registraba solo los IDs de los estudiantes y los tipos de recurso (D-13), y los eventos salían únicamente por consola, así que se perdían si se eliminaba el contenedor.
- **Qué hizo el humano:** preguntó si se usaban `Microsoft.Extensions.AI` y `Microsoft.Agents.AI` y decidió *"usemos Microsoft.Extensions.AI"*. Preguntó también si se tenía en cuenta la auditoría (*"guardar las consultas que se hagan y demás logs"*), citando el requisito del PDF de registrar *"cada pregunta junto con los datos del estudiante que se consultaron"*. Ante las opciones, eligió el detalle sin valores y la persistencia en consola y archivo.
- **Por qué:** menos código HTTP propio con una librería mantenida, y una auditoría que cumple el PDF de forma verificable sin duplicar datos personales.
- **Qué se cambió:** se agregaron D-24 (Extensions.AI solo en Infrastructure, sin invocación automática de funciones, sin Agent Framework) y D-25 (archivo JSONL diario en un volumen de Docker), y se actualizó D-13. En la spec 001, §11.3 incorpora el campo `datosConsultados` (estudiante, programa, recurso y asignatura, sin valores) y la persistencia. CA-08 suma dos escenarios Gherkin, y se agregaron ESC-143 y ESC-144. Se avisó a `backend-dotnet` del cambio de adaptador. Pendiente por TDD: `qa-calidad` escribe las pruebas de ESC-143 y ESC-144, después las implementa `backend-dotnet` y `devops-cicd` agrega el volumen.
- **Costo:** ~2 min humanos.
- **Prevención:** los requisitos del PDF que admiten varias lecturas se confirman con el humano en lugar de resolverlos solo con una decisión D-xx.

### INT-014 · 2026-11-22 · Instrucciones directas al agente de backend: prompt en prosa y modelo sin constante
- **Fase:** implementación (corrida 1, paso 2)
- **Qué pasó:** mientras `backend-dotnet` implementaba, el humano le escribió directamente desde la vista del agente, fuera del orquestador.
- **Qué hizo el humano:** según el reporte del agente, confirmado por el humano: (1) que el prompt de sistema esté escrito en prosa, con la audiencia, las fuentes, las razones de cada regla, un ejemplo y la delimitación de los datos no confiables, en lugar de listas de prohibiciones; (2) que el modelo por defecto no sea una constante en el código, sino que salga solo de `appsettings.json` y de `LLM_MODELO`. Las palabras exactas no quedaron en la sesión del orquestador; quedan en la transcripción del agente.
- **Por qué:** un prompt que explica el porqué orienta mejor al modelo que una lista de "no hagas", y fijar el modelo en el código impide cambiarlo por configuración (D-16).
- **Qué se cambió:** commits `647bb37`, `182cea9` y `e9a3223` (prompt) y la resolución del modelo en Infrastructure. El agente quitó del prompt los ejemplos con datos inventados porque un modelo de 7B los copiaba en lugar de llamar a la herramienta.
- **Costo:** pendiente (lo indica el humano).
- **Prevención:** cuando el humano dé instrucciones directas a un agente, el agente las marca en su reporte como "pedido del humano" (así ocurrió aquí) y el orquestador las registra.

### INT-015 · 2026-11-23 · correlationId en UUID v7
- **Fase:** auditoría (corrida 1)
- **Qué pasó:** la auditoría usaba como `correlationId` el `TraceIdentifier` de Kestrel (`0HNQ8V8C5VI9M:00000004`), que no es un estándar, y los `ProblemDetails` de error devolvían otro valor (`traceId` W3C), así que una misma petición no se podía correlacionar.
- **Qué hizo el humano:** preguntó *"por qué el correlation id tiene ese formato en vez de UUID"*. Ante la propuesta, respondió *"Sí, usa UUID v7 y actualiza la spec para estar alineados"*, y con eso aprobó también el paquete de reglas correctivas de la auditoría, incluido el 415.
- **Por qué:** un identificador estándar y único por petición permite cruzar la respuesta, la auditoría y los reportes de error.
- **Qué se cambió:** D-26 en `SPEC.md`: UUID v7 generado por el servidor, el mismo valor en la auditoría, en el `traceId` de los errores y en el encabezado `X-Correlation-Id`, sin aceptar el del cliente. Lo aplica `arquitecto-spec` en la spec v1.4 junto con las demás reglas correctivas.
- **Costo:** ~1 min humano.
- **Prevención:** `qa-calidad` (fase B) revisa que los identificadores de trazabilidad sigan un estándar y sean consistentes entre respuesta y auditoría.

### INT-016 · 2026-11-23 · Patrón de orquestación con subagentes en paralelo
- **Fase:** configuración de la fábrica
- **Qué pasó:** el orquestador hacía trabajo directo (verificaba ejecutando comandos, editaba la spec, escribía scripts) y los subagentes trabajaban casi siempre de a uno, en el mismo directorio y compitiendo por el índice de git.
- **Qué hizo el humano:** pidió que *"la fábrica tenga la capacidad de lanzar varios sub agentes"* con *"el patrón de orquestación donde el modelo principal genera y da las órdenes y los sub agentes son quienes evalúan, editan, crean código, validan y entregan resultados al orquestador"*.
- **Por qué:** paralelizar reduce el tiempo de cada corrida, y separar al que decide de los que ejecutan mantiene la verificación independiente.
- **Qué se cambió:** `implementar-spec` se reescribió con el patrón de orquestación. El orquestador planifica tareas con archivos sin solapamiento y lanza hasta 4 subagentes en paralelo, cada uno en su `git worktree` y rama `feat/NNN-<slug>--<tarea>`. Integra con `merge --no-ff`, los conflictos los resuelve el agente dueño, y el orquestador no escribe código, pruebas ni spec ni verifica por su cuenta. Nueva fase V en `qa-calidad`: verificación independiente con salida cruda. Se actualizaron `FABRICA.md` y `CLAUDE.md`.
- **Costo:** ~1 min humano.
- **Prevención:** la skill obliga a escribir un plan de tareas (agente, archivos, dependencias) antes de lanzar a cualquier agente.

### INT-017 · 2026-11-23 · Logs de las sesiones incompletos
- **Fase:** configuración de la fábrica
- **Qué pasó:** `logs/` tenía una sola transcripción (una sesión corta de 9 líneas), aunque se llevaban varias sesiones de trabajo. El hook solo exportaba con `SessionEnd`. La sesión principal empezó antes de que existiera el hook y se retomó con `--continue`, así que nunca terminó. Además el hook ignoraba las 19 transcripciones de subagentes. Al revisar se encontró también que el archivo local de términos privados (`.claude/redacciones.local.txt`) había desaparecido, así que esos términos no se estaban redactando.
- **Qué hizo el humano:** señaló *"me preocupa del flujo de trabajo que solo se ha generado un log y llevamos ya varias sesiones"*.
- **Por qué:** el PDF exige guardar los registros de las sesiones de los agentes (regla 5), y la transparencia vale el 15 % de la evaluación.
- **Qué se cambió:** `exportar-log.js` ahora exporta la sesión del orquestador y todos sus subagentes a `logs/sesiones/<session-id>/{sesion.jsonl, subagentes/}`, de forma idempotente y con redacción. Tiene un modo `--todo` para recuperar lo pendiente, que exportó 21 transcripciones. Se agregó el hook `SubagentStop`, se recreó la lista de términos privados y se verificó con `grep` que no quedaran apariciones. Se actualizaron `logs/README.md` y la skill `cerrar-corrida`.
- **Costo:** ~1 min humano.
- **Prevención:** la exportación se dispara al terminar cada subagente y en `cerrar-corrida`, y esa skill verifica que la lista de redacción exista antes de hacer commit de los logs.

### INT-018 · 2026-11-23 · No inventar: el "no sé" pasa a ser determinista
- **Fase:** implementación (corrida 1, bucle de corrección)
- **Qué pasó:** la decisión de responder "no encuentro esa información" dependía del modelo. Una respuesta inventada sin números (como el "usa el botón de asistencia" de Ollama) no se bloqueaba, y con el respaldo acotado de citas (D-28) habría salido con 2 citas que parecían respaldarla.
- **Qué hizo el humano:** indicó que *"JAMÁS se debe de inventar información, hacer supuestos"*, y que si lo preguntado no está en las fuentes *"lo correcto sería poder responder con un mensaje claro de fallback"*, usando las herramientas y pasando el reglamento como contexto.
- **Por qué:** "no inventar" es un requisito del PDF y no puede depender del comportamiento de un modelo concreto.
- **Qué se cambió:** se encargó a `arquitecto-spec` la spec v1.6 con: umbral de relevancia previo al LLM (COMP-03 sin llamar al modelo), reemplazo por COMP-03 de toda respuesta sin cita válida ni datos, respaldo de citas condicionado a que la sección comparta términos con la respuesta (modifica D-28), D-29 y escenarios desde ESC-165. Después sigue el ciclo de TDD.
- **Costo:** ~2 min humanos.
- **Prevención:** cualquier regla de comportamiento del asistente que proteja al usuario debe tener una verificación en código, no solo una instrucción en el prompt.

### INT-019 · 2026-11-23 · MCP de Playwright para validar el frontend
- **Fase:** configuración de la fábrica
- **Qué pasó:** la fábrica no tenía forma de validar una interfaz web en un navegador.
- **Qué hizo el humano:** pidió *"instalemos además el MCP de Playwright para que podamos luego de construir el front end validarlo"*.
- **Por qué:** la feature 002 (frontend) necesita verificar estados, flujos y accesibilidad en un navegador real, no solo con pruebas unitarias.
- **Qué se cambió:** servidor MCP `playwright` en `.mcp.json` a nivel de proyecto, con `@playwright/mcp@0.0.86` (paquete oficial de Microsoft, versión fija) en modo headless. El hook `commit-msg` y `AGENTS.md` tratan `.mcp.json` como configuración de agentes (commit humano). Se documentó su uso en `frontend-react`, en la fase V de `qa-calidad` y en `CLAUDE.md`. Claude Code lo deja pendiente de aprobación del humano al abrir la sesión.
- **Costo:** ~1 min humano.
- **Prevención:** las herramientas externas se fijan por versión y se documentan en el agente que las usa.

### INT-020 · 2026-11-23 12:18 · Los defectos de la fase V no se aceptan como riesgo
- **Fase:** auditoría
- **Qué pasó:** en la fase V de la ronda 3 (`1fdf8eb`) qa-calidad dio APROBADO con hallazgos abiertos. P6 salió bien 1 de cada 3 veces. P2, P3, P7 y P8 omitieron datos esperados. V-3 entregó la frase truncada "Estos requisitos están respaldados por.". V-4 atribuyó un horario al documento equivocado. Quedaron 12 hallazgos de calidad B-01…B-12 de severidad media y baja. El orquestador proponía corregir solo V-3 y los B medios, y registrar las omisiones como riesgo aceptado del modelo de 7B.
- **Qué hizo el humano:** *"Esos defectos que se encontraron se deben corregir, se deben comprender su razon de fallo, analizar en el codigo por que pasar y hacer la correccion que permita que el problema/error o mala definicion se corregia y no vuelva asuceder. Los temas de caldiad de codgio tambien se deben de corregir y lo ma simportante rebvisar el tema de las pregutnas por que algunas salen bien y otras mal y demas."*
- **Por qué:** un APROBADO con defectos conocidos no cierra la feature. Atribuir la inconsistencia al modelo sin diagnosticarla esconde posibles fallos de diseño, de los verificadores o de los datos que se entregan al modelo.
- **Costo:** ~2 min humanos.
- **Prevención:** aprobada por el humano y aplicada: en la fase V de `qa-calidad`, todo defecto observado con el LLM real se reporta con su causa raíz en el código (verificador que actuó, datos entregados, texto del modelo). En `FABRICA.md`, una condición de parada: la feature no se cierra con defectos abiertos ni con "riesgo aceptado" sin aprobación explícita del humano.

### INT-021 · 2026-11-23 · Los riesgos registrados se resuelven, no solo se anotan
- **Fase:** spec
- **Qué pasó:** la spec v1.11 (`eb95afc`) dejó varios riesgos solo anotados, sin decisión para resolverlos. Son estos: el umbral relativo del 50 % estimado a mano (D-49), la posible pérdida de la `Sección 2` en P4 por el respaldo de una sola sección, el contexto de 4.096 tokens de Ollama frente a los ~3.900 que ya usa P6 (D-52), la completitud de P7 y P8 sujeta a que el modelo copie el dato, las citas de P2, P5 y P6 sujetas a que el modelo escriba el término del campo (D-53), la atribución genérica que se elimina de más y los homoglifos y letras con trazo aceptados como riesgo (D-43). El orquestador los presentó como "riesgos registrados" y siguió con las pruebas.
- **Qué hizo el humano:** *"De esos riesgos que quedan registrados debes de analizalros con el agente arquitecto para tomar una desicion y evitar, corregir, solucionar, entender y mitrigar estos riesgos o eliminarlos."*
- **Por qué:** un riesgo anotado sin decisión es un defecto aplazado, igual que en INT-020.
- **Costo:** ~1 min humano.
- **Prevención:** aprobada por el humano y aplicada: en la skill `nueva-spec` y en `arquitecto-spec`, todo riesgo identificado lleva una decisión explícita: eliminarlo, mitigarlo con un control verificable o aceptarlo con la aprobación del humano.

### INT-022 · 2026-11-23 · Rango de temperatura del LLM
- **Fase:** spec
- **Qué pasó:** la spec v1.11 (D-47, §9.4) fijó la temperatura en 0 en todos los adaptadores.
- **Qué hizo el humano:** *"La temperatura podemos ajustarla de 0 hasta MAXIMO 0.3"*.
- **Por qué:** se admite un rango acotado para equilibrar el determinismo con la calidad de la respuesta, sin pasar nunca de 0.3.
- **Costo:** ~1 min humano.
- **Prevención:** D-47 se ajusta para permitir una temperatura entre 0 y 0.3, como configuración validada al arrancar (RNF-05). Un valor fuera del rango impide arrancar. El valor por defecto se elige con la medición del punto 4.

### INT-023 · 2026-11-23 · La spec creció demasiado y mezcla producto con implementación
- **Fase:** spec
- **Qué pasó:** `specs/001-asistente-academico/spec.md` llegó a 2.585 líneas y unas 52.000 palabras, con 24 CA. Detalla algoritmos de verificación, umbrales, prompt, formatos, adaptadores del LLM y validación de la configuración. `SPEC.md` llegó a unas 15.000 palabras por las filas de decisiones D-01…D-53. Cada ronda de corrección (v1.4…v1.12) añadió texto en lugar de reorganizarlo.
- **Qué hizo el humano:** *"He notado que la sepc ha crecido demasiado, esto se debe de evitar y de no comter este tipo de errores ya que se vuelve dificil de leer para el humano y dificil incluso de leer para ti como herramienta agentica de codigo"*. Compartió una guía. Una buena spec tiene alcance de una sola feature, flujos de usuario cortos, criterios de aceptación comprobables, límites explícitos de alcance, solo las reglas de dominio y los casos borde que afectan lo visible, y cero ambigüedades. No lleva detalles técnicos de implementación (van en `plan.md`) ni reglas globales del proyecto (van en `constitution.md`). Tampoco junta varias features, ni incluye código o pseudocódigo, ni prosa larga o repetida.
- **Por qué:** una spec ilegible no sirve como fuente de verdad, ni para el humano ni para los agentes, y desperdicia contexto.
- **Costo:** ~3 min humanos.
- **Prevención:** aplicada tras la decisión del humano (una feature, tres archivos): `constitution.md` (global), `SPEC.md` (índice), `spec.md` (producto, ≤ 350 líneas), `plan.md` (técnico, ≤ 700) y `decisiones.md` (≤ 3 líneas por D); ninguna línea de más de 400 caracteres; escenarios por tema. Reglas en `arquitecto-spec`, `nueva-spec` y las plantillas; "corrección por reorganización, no por acumulación". El CI lo verifica (`.github/scripts/verificar-limites-specs.sh`). Una verificación independiente confirmó que no se perdió ninguna regla.

### INT-024 · 2026-11-23 · El estudiante no debe descubrir qué puede consultar su coordinador
- **Fase:** spec
- **Qué pasó:** la spec 001 y sus escenarios no cubrían que un estudiante pregunte qué puede consultar o ver su coordinador. Solo existían D-09 (el estudiante puede nombrar a su coordinador) y ESC-018 (pregunta normativa hecha por un coordinador). Ni la fábrica ni las auditorías de seguridad detectaron el hueco.
- **Qué hizo el humano:** *"no se si tuviste encuenta evitar que un estuidante pueda consultar que cosas PUEDE consultar su coordinador"*. Decidió la opción "Solo lo del reglamento": se responde lo que dice el reglamento con su cita (Art. 22) y se niega todo lo demás. Eso incluye los datos y las herramientas del coordinador, los estudiantes que ve, lo que vería si preguntara por alguien, simular ser el coordinador y sus permisos en el sistema.
- **Por qué:** describir las capacidades de otro rol expone el modelo de permisos y la configuración, y abre caminos a datos de terceros.
- **Costo:** ~2 min humanos.
- **Decisión adicional del humano:** *"Sí, que aplique también entre coordinadores"*: la misma regla rige cuando un coordinador pregunta por las capacidades de otro coordinador (o del estudiante en el sistema).
- **Prevención:** aprobada por el humano y aplicada: en `nueva-spec`, la revisión adversarial de `seguridad` debe enumerar, por cada par de roles, las preguntas de un rol sobre las capacidades, datos o permisos del otro, y exigir un escenario para cada una.

### INT-026 · 2026-11-23 · Agentes que entregan el informe con procesos vivos
- **Fase:** otro
- **Qué pasó:** tres agentes entregaron su informe y quedaron "esperando" porque dejaron procesos de shell en segundo plano. Uno era de `seguridad` (ronda 3) y dos de `qa-calidad` (frente de datos de la ronda 4 y cierre de pruebas). Eran bucles de `dotnet test` y sondas. Un bucle vivo bloqueó un archivo y provocó un fallo de compilación MSB3026.
- **Qué hizo el humano:** *"Se debe de revisar por que hay 3 agentes 1 de seguridad y 2 de calidad en un estado de wating on 1 shell?"*. Aprobó la regla de prevención.
- **Por qué:** los procesos huérfanos consumen recursos, bloquean archivos y puertos, y dejan el estado de la fábrica opaco.
- **Costo:** ~1 min humano.
- **Prevención:** aprobada por el humano y aplicada: `AGENTS.md` §7 prohíbe entregar el informe con procesos propios vivos y exige la línea "Procesos en segundo plano: ninguno". El orquestador detuvo los tres agentes y verificó que no quedaban procesos `dotnet`, `testhost` ni contenedores.

### INT-027 · 2026-11-23 · Los controles por IP no pueden bloquear al evaluador
- **Fase:** spec
- **Qué pasó:** la 001 aplica un límite de 60 preguntas por minuto por IP antes de validar la identidad (SEG-13). También tiene detección de identidades múltiples por IP (SEG-16, D-69) y un presupuesto diario. El evaluador de Q10 probablemente correrá su batería oculta desde una sola IP, con muchas identidades y peticiones rápidas (las denegaciones del filtro previo tardan milisegundos). Puede recibir 429 donde espera 403 o 200.
- **Qué hizo el humano:** *"Hay que tener bien el tema de la IP ya que puede que el evaluador de esta prueba tenica lo bloquee por IP y demas enotnces ten eso en cuenta, esot no puede ser un bloqueante"*.
- **Por qué:** un control de abuso que hace fallar las pruebas ocultas del evaluador anula la entrega.
- **Costo:** ~1 min humano.
- **Prevención:** el análisis de toda regla de límites o bloqueos considera al evaluador automático como un cliente legítimo: una IP, muchas identidades, ráfagas. Los valores por defecto nunca lo bloquean; endurecerlos es una configuración explícita.

### INT-029 · 2026-11-23 · Subredes fijas en el compose por defecto
- **Fase:** CI
- **Qué pasó:** con D-68, `docker-compose.yml` por defecto creaba la red `web-api` con la subred fija `172.30.80.0/29` y una IP fija para `api` (merge `68502db`).
- **Qué hizo el humano:** objetó que las IP estáticas pueden fallar si quien ejecuta el proyecto tiene otros servicios, VPN o redes que se solapan, o en GitHub Actions. La objeción llegó a través del agente `devops-cicd`.
- **Por qué:** un "Pool overlaps" en la máquina del evaluador haría fallar el `docker compose up` evaluado. Eso contradice INT-027.
- **Costo:** ~1 min humano.
- **Prevención:** `docker-compose.yml` por defecto no fija subredes ni IP. Toda topología con valores fijos va en un archivo de override opcional (`docker-compose.web.yml`), y el CI verifica que el compose por defecto no declara `ipam` ni `ipv4_address`.

### INT-030 · 2026-11-23 · Bloqueo simple por cantidad de peticiones, sin complejidad innecesaria
- **Fase:** spec
- **Qué pasó:** para atribuir la IP detrás del proxy de la interfaz, la fábrica fue acumulando mecanismos: un proxy de confianza con red e IP fijas (D-68), un bloqueo opcional por identidades múltiples (D-69), límites sin tope por defecto (D-70) y, tras INT-029, la propuesta de un archivo de override con subredes configurables.
- **Qué hizo el humano:** *"No, simplemente no debes de ahcer bloqueos asi de ese estilo, realmente no debes de generar complejidad de manera innecesaria si se desea bloquear una IP debe ser por la cantidad de peticiones y bloquear por 30 segundos luegp 60, luegop 120, para que peuda vovler a intentar y ya esta."* Además eligió dos opciones: el umbral queda activo y holgado, en 600 peticiones por minuto por IP, y se eliminan D-68 y D-69.
- **Por qué:** la complejidad no aportaba valor proporcional y añadía riesgos de despliegue (subredes fijas, INT-029).
- **Costo:** ~2 min humanos.
- **Prevención:** aprobada por el humano y aplicada, con su ajuste: *"ante cualquier solucion debe de ser evualuada con otro sub agente y presetnar las soluciones al arquitecto al agente de back y front para tomar una desicion"*. `FABRICA.md` §2 y `arquitecto-spec` exigen que toda solución la evalúe un subagente distinto del que la propone y que las alternativas, empezando por la más simple, se decidan entre `arquitecto-spec`, `backend-dotnet` y `frontend-react`. Si añade infraestructura, requiere además aprobación humana.

### INT-031 · 2026-11-24 · La llave del LLM no se filtra por su formato
- **Fase:** spec
- **Qué pasó:** con una llave de Gemini que no empieza por `AIza`, el backend no resolvió el proveedor ("la llave tiene un prefijo desconocido y falta LLM_URL_BASE") y las preguntas con LLM responderían 503. D-16 decidía el proveedor por el prefijo de la llave.
- **Qué hizo el humano:** *"Ten encuyenta que esa es la llave que proporciono entonces no hay que tener un fiultraod de llves, simplemente se usa la API key que sea indifrente de su valor para eso instalamos Microsoft.Extension.IA"*.
- **Por qué:** los formatos de llave cambian y el evaluador usará la suya. Rechazarla por su forma bloquea la evaluación (INT-027).
- **Costo:** ~1 min humano.
- **Prevención:** la llave nunca se valida ni se rechaza por su formato. El proveedor se resuelve con información explícita (`LLM_PROVEEDOR`, el nombre del modelo en `LLM_MODELO`, `LLM_URL_BASE`) y el prefijo de la llave queda solo como pista. Decisión técnica por INT-030.

### INT-032 · 2026-11-24 · La semilla del LLM se elimina en vez de filtrarse
- **Fase:** spec
- **Qué pasó:** Gemini rechazaba todas las preguntas con 400 (`Unknown name "seed": Cannot find field.`). La fábrica propuso dejar de enviar `seed` solo cuando el proveedor es Gemini, con una regla por proveedor en el adaptador.
- **Qué hizo el humano:** *"Y para que necesitamos ese "seed" realmente no podmeos validar cada proovedor, por que no lo quitamos simplemnete?"*.
- **Por qué:** la semilla no garantiza determinismo, no todos los proveedores la admiten y mantener una lista por proveedor es complejidad innecesaria (INT-030). La estabilidad ya la dan la temperatura 0 (INT-022) y la verificación determinista del texto final.
- **Costo:** ~1 min humano.
- **Prevención:** ninguna regla nueva: INT-030 ya exige empezar por la alternativa más simple. La fábrica no la aplicó porque la propuesta salió de un agente de diagnóstico sin pasar por la evaluación entre disciplinas; D-85 elimina la semilla del producto y sus pruebas.

### INT-033 · 2026-11-24 · Las faltas se responden por asignatura y exactas
- **Fase:** spec
- **Qué pasó:** ante el residual de D-76 ("Tienes 9 faltas de 48 clases en Bases de Datos" se aceptaba porque el 9 es un valor de otra asignatura del estudiante), la fábrica ofreció tres variantes de verificación y el humano eligió "validar el par" (faltas y clases de alguna asignatura). Ninguna opción definía qué debe ver el usuario.
- **Qué hizo el humano:** *"No deberia de poder tener la capacidad de que si trengo 9 faltas decirme de que clases son no solo solar 9 faltas de 4 clases en 1 sola clase deberia decirme todas  ya si pregunto cuantas faltas tengo en Bases de datos deberian de ser el valor exacto de esa clase o de esas clases"*.
- **Por qué:** un total sin desglose no le sirve al estudiante, y un número por asignatura que no es el de esa asignatura es un dato falso aunque pertenezca a la persona.
- **Costo:** ~2 min humanos.
- **Prevención:** aprobada por el humano y aplicada en `arquitecto-spec`: ante un defecto de exactitud, definir primero el comportamiento observable para el usuario (qué dato, con qué desglose) y solo después la verificación que lo garantiza.

### INT-034 · 2026-11-24 · Solo se validan las pruebas afectadas por el cambio
- **Fase:** pruebas
- **Qué pasó:** el plan de cierre de la 001 repetía en la fase V la validación completa (todas las pruebas, cobertura, evals, Postman, Docker y las 10 preguntas de Q10 × 5 con Gemini), aunque gran parte ya estaba validada y no cambia con la ronda.
- **Qué hizo el humano:** *"No ejecutes validaciones que son innecesarias ya sea o por que ya se validaron y volverlas a vlaidar no aprotan valor ya que digamos de 100 test solo se modifcaron 50 solo se ejcutan los 50 afectados no 100 ya que el resultado de los otros 50 sera el mismo"*.
- **Por qué:** repetir validaciones cuyo resultado no puede cambiar gasta tiempo y tokens sin aportar información.
- **Costo:** ~1 min humano.
- **Prevención:** aprobada por el humano y aplicada: en `implementar-spec` y `qa-calidad`, la fase V ejecuta solo lo afectado por el diff. Las pruebas de los archivos de prueba cambiados y de las clases de producción cambiadas o de sus dependientes se seleccionan con `dotnet test --filter`. Del LLM real se miden solo las preguntas cuyo camino toca el cambio. Se ejecutan solo los artefactos de Postman y Docker que se tocaron. La suite completa queda para el CI, que no gasta tokens.

### INT-035 · 2026-11-24 · costoUsd para cualquier modelo
- **Fase:** spec
- **Qué pasó:** con Gemini `gemini-3.5-flash-lite`, `costoUsd` salía en 0 porque `appsettings.json` solo tenía precio para `gemini-2.0-flash`. La fábrica propuso añadir solo ese modelo a la tabla.
- **Qué hizo el humano:** *"pero entonces lo de costoUSd en 0 esta mal en el appsettings por que deberia de salir no solo para egemini-2.0 sinno para el modelo que sea que se use"*. Después indicó la fuente del precio: *"y para gemini peudes leer sus precios \"https://aistudio.google.com/docs/models/gemini-3.5-flash-lite\""*.
- **Por qué:** el evaluador usará su propio modelo y llave; un costo en 0 sin aviso es un dato falso y viola el principio 7 de la constitución (configuración visible).
- **Costo:** ~2 min humanos.
- **Prevención:** D-105. El precio de cualquier modelo se puede dar con `LLM_PRECIO_ENTRADA_POR_MILLON` y `LLM_PRECIO_SALIDA_POR_MILLON`, con una tabla de modelos habituales como respaldo. Un modelo sin precio produce una advertencia exacta al arrancar, en lugar de un 0 silencioso.

### INT-036 · 2026-11-24 · El precio no sale de variables ni de tablas escritas a mano
- **Fase:** spec
- **Qué pasó:** D-105 (commit `426b711`) resolvía el precio de cualquier modelo con `LLM_PRECIO_ENTRADA` y `LLM_PRECIO_SALIDA` más una tabla en `appsettings.json`.
- **Qué hizo el humano:** *"Pero no tiene sentido agregar esas variables de LLM_PRECIO LLM PRECIO SALIDA y demas por que a la final los preicos pueden varios por modelo, el modelo tal vez ya deja de estar deprecado y se usara alguno difrente y demas, entonces pro ello no creo que sea la mejor soluicion"*. Eligió descargar el precio del catálogo público al arrancar.
- **Por qué:** los precios y los modelos cambian; un precio escrito a mano envejece y obliga a reconfigurar.
- **Costo:** ~2 min humanos.
- **Prevención:** D-105 se reemplaza. El precio del modelo en uso se toma al arrancar del catálogo público de OpenRouter, la tabla queda solo como respaldo sin red y el arranque avisa si no hay precio.
