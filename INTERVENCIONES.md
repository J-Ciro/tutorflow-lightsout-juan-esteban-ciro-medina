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
