# SPEC-002 · Interfaz web

| Campo | Valor |
| --- | --- |
| Versión | 1.0 · 2026-11-23 · aprobada por el humano; implementación tras cerrar la 001 |
| Requisito original | Texto literal debajo de esta tabla |
| Reglas globales | [`constitution.md`](../../constitution.md): contrato (§2) y matriz de acceso (§3), sin cambios |
| Técnico | [`plan.md`](plan.md) · Decisiones: [`decisiones.md`](decisiones.md) · Escenarios: [`escenarios/002-interfaz-web/`](../../escenarios/002-interfaz-web/) (ESC-200…ESC-299) |
| Afecta | Consume SPEC-001 (§2 "Interfaz web", COMP-03, `TEXTO_NEGATIVA`, `TEXTO_NO_VERIFICABLE`); el perfil `web` depende de D-68 (proxy de confianza) y D-69 (bloqueo de identidades) de la 001, que se especifican allí; agrega un servicio opcional a Docker Compose y un trabajo al CI |

> Feature 002: interfaz web de TutorFlow en React + TypeScript + shadcn/ui (frontend/). Experiencia cómoda y llamativa para estudiantes y coordinadores, que consume el contrato de constitution.md §2 (POST /api/asistente/preguntar, GET /api/salud). Paleta #f7f7f7, #ff6a13, #2b1d19;
> look and feel inspirado en https://www.q10.com/ (tipografía libre similar, radios 20/12/999 px, sombras cálidas, transiciones cortas) sin usar marca, logos, textos ni fuentes de Q10.
> Debe mostrar respuesta como texto plano, citas (documento · sección), tokens y costo, estados de carga, sin información, negativa 403 y error de red, accesible (AA, foco, etiquetas), validada con Playwright MCP. El frontend no es barrera de seguridad. La implementación requiere aprobación humana explícita.

## 1. Objetivo
Que un estudiante o un coordinador pregunte a TutorFlow desde el navegador, en el móvil o en el escritorio, y entienda de un vistazo la respuesta, cuántos tokens usó y qué hacer cuando no hay respuesta; y que probar con varios usuarios de prueba desde la interfaz no degrade el servicio.

## 2. Alcance
**Dentro del alcance:**
- App web de una sola página en `frontend/`: selector de usuario de prueba, envío de preguntas, historial en memoria, respuesta y tokens.
- Estados de carga, sin información, negativa, errores y servicio caído; accesibilidad AA; diseño responsivo; seguridad del cliente.
- Servicio opcional en Compose con un proxy que conserva la IP del cliente; modo de desarrollo; pruebas y CI.

**Fuera del alcance:**
- Autenticación real (D-01 de la 001); persistencia de cualquier tipo; historial enviado al backend; cambios en el contrato o en la matriz de acceso; cambios en el backend distintos de D-68 y D-69 de la 001.
- Un backend en otro origen (CORS); acceso a la interfaz desde otra máquina; lo que la persona vea de su propia sesión con las herramientas de desarrollo de su navegador.
- Modo oscuro; idiomas distintos del español; marca, logos, textos y fuentes de Q10.
- Mostrar los documentos y secciones citados (D-002-38) y el costo: la API los devuelve en `citas` y `costoUsd` y la interfaz los valida, pero no los muestra (D-002-33).

## 3. Flujos de usuario
| Flujo | Pasos |
| --- | --- |
| F1 · Estudiante pregunta por el reglamento | 1) abre la app y ve el aviso de demostración → 2) elige "EST-001 · estudiante" → 3) escribe "¿Cuál es la nota mínima para aprobar una asignatura?" y pulsa Enter → 4) ve "Consultando…" → 5) ve la respuesta y "N tokens", sin el costo ni el nombre del documento citado |
| F2 · Coordinador consulta su programa | 1) elige "COORD-02 · coordinador" → 2) pulsa la pregunta de ejemplo "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?" → 3) la envía → 4) ve la respuesta, sus tokens y la insignia "Usó datos académicos" |
| F3 · Pregunta sin información | 1) pregunta "¿Qué dice el reglamento sobre el uso de parqueaderos?" → 2) ve el texto de COMP-03 de la 001 como mensaje informativo con una sugerencia |
| F4 · Pide datos ajenos | 1) EST-001 pregunta "¿Cuáles son las notas de EST-002?" → 2) ve "Acceso restringido" y la negativa del estudiante, sin tokens ni botón de reintento |
| F5 · Algo falla | 1) el servicio responde 503 o no hay red → 2) ve un mensaje claro, el código de soporte (si existe) y "Reintentar" → 3) si `/api/salud` falla, ve "Servicio no disponible" y "Comprobar ahora" |
| F6 · Cambia de usuario | 1) con 2 preguntas en pantalla elige otro usuario → 2) la conversación se borra y ve y oye el aviso de cambio de usuario → 3) puede preguntar enseguida, salvo que haya una espera pendiente |
| F7 · Demasiadas preguntas | 1) recibe el aviso de espera con cuenta regresiva → 2) puede cambiar de usuario y la espera sigue → 3) al terminar oye "Ya puedes preguntar de nuevo." y su pregunta sigue en el campo, sin enviarse sola |

## 4. Requerimientos funcionales
- **RF-002-01** · Elegir la identidad de una lista cerrada de usuarios de prueba, siempre marcada como entorno de demostración (ambos roles). CA-002-01, CA-002-13.
- **RF-002-02** · Escribir y enviar una pregunta de 1 a 1000 caracteres, con contador y preguntas de ejemplo por rol. CA-002-02.
- **RF-002-03** · Mostrar la respuesta como texto plano, los tokens y si usó datos académicos; las citas y el costo del contrato se validan y no se muestran. CA-002-03, CA-002-08.
- **RF-002-04** · Mostrar "sin información", la negativa 403 y la respuesta no verificable como mensajes informativos, no como errores. CA-002-04, CA-002-13.
- **RF-002-05** · Mostrar cada error (400, 413, 415, 429, 500, 503, otros códigos, respuesta ilegible, red y tiempo agotado) con un texto fijo, una acción y el código de soporte. CA-002-05.
- **RF-002-06** · Indicar si el servicio está disponible según `GET /api/salud` e impedir preguntar mientras no lo está. CA-002-06.
- **RF-002-07** · Mantener el historial solo en memoria y borrarlo al cambiar de identidad, con "Nueva conversación", al recargar o al salir de la página. CA-002-07.
- **RF-002-08** · Ser usable con teclado y lector de pantalla con nivel WCAG 2.2 AA, desde 320 px de ancho hasta escritorio. CA-002-09, CA-002-10, CA-002-11.
- **RF-002-09** · Proteger el cliente: sin HTML interpretado, sin secretos, sin almacenamiento ni consola con datos, sin terceros y con política de contenido estricta. CA-002-08, CA-002-12.
- **RF-002-10** · Desplegarse como servicio opcional de Docker Compose, solo en el equipo local, sin alterar el arranque evaluado en el puerto 8080, y bloquear regresiones en el CI. CA-002-14.
- **RF-002-11** · Que el backend atribuya cada pregunta hecha por la interfaz a la IP del cliente y no a la del proxy, sin que el cliente pueda falsificarla. CA-002-15.

## 5. Comportamiento y experiencia
Textos fijos de la interfaz (los textos del asistente vienen del backend y se muestran sin cambios). `<espera>` es "<n> s" hasta 60 s y "mm:ss" por encima (p. ej. "02:00").

| Texto | Texto exacto |
| --- | --- |
| Aviso de demostración | Entorno de demostración: eliges un usuario de prueba y no hay inicio de sesión. Los permisos los decide el servidor. |
| Selector | Etiqueta "Usuario de prueba"; opción inicial "Elige un usuario de prueba"; opciones "<ID> · estudiante" y "<ID> · coordinador" |
| Campo | Etiqueta "Tu pregunta"; ayuda "Cada pregunta se responde por separado. Máximo 1000 caracteres."; contador "<n>/1000" |
| Sin identidad · Exceso | Elige un usuario de prueba para preguntar. · La pregunta supera los 1000 caracteres. |
| Botones | Preguntar · Consultando… · Reintentar · Volver a preguntar · Recargar · Comprobar ahora · Nueva conversación · Copiar código |
| Cargando (anuncio) | Consultando al asistente… |
| Metadatos · Insignia | "<tokens> tokens" · "Usó datos académicos" |
| Sugerencia sin información | Prueba con otra pregunta sobre el reglamento, tus notas o tu asistencia. |
| Título de la negativa | Acceso restringido |
| 400 | No pudimos procesar la pregunta. Revísala e intenta de nuevo. |
| 413 | La pregunta es demasiado grande para enviarla. Acórtala e intenta de nuevo. |
| 415 | La aplicación no pudo enviar la pregunta. Recarga la página. |
| 429 · fin de la espera (anuncio) | Hay muchas preguntas en este momento. Podrás preguntar de nuevo en <espera>. · Ya puedes preguntar de nuevo. |
| 503 | El asistente no está disponible en este momento. Intenta de nuevo en unos minutos. |
| 500 y otros códigos | Ocurrió un error inesperado. Intenta de nuevo. |
| Respuesta ilegible | Recibimos una respuesta que no pudimos leer. Intenta de nuevo. |
| Red o tiempo agotado | No pudimos conectar con el asistente. Revisa tu conexión e intenta de nuevo. |
| Código de soporte | Código de soporte: <X-Correlation-Id> |
| Salud | "Comprobando servicio…" · "Servicio en línea" · "Servicio no disponible" · aviso "El asistente no está disponible. Lo comprobamos de nuevo cada 30 s." |
| Cambio de identidad (aviso visible y anuncio) | Conversación borrada: ahora preguntas como <ID>. El cambio de usuario queda registrado en la auditoría de la demostración. |
| Cambio de identidad con espera pendiente (se añade) | La espera de <espera> se mantiene: el límite se aplica a todo el navegador. |

- **COMP-002-01** · El aviso de demostración está visible en la cabecera en todo momento y en todos los anchos.
- **COMP-002-02** · Respuesta 200: el texto se muestra tal cual, conservando los saltos de línea; debajo, los metadatos con `tokens` entero; la insignia solo si `usoDatosEstudiante` es true; ni `costoUsd` ni las citas se muestran ("US$" aparece 0 veces en la página; SEG-002-18).
- **COMP-002-03** · Una respuesta 200 cuyo texto es exactamente el de COMP-03 de la 001 se muestra como mensaje informativo con la sugerencia y los metadatos, sin "Reintentar" ni código de soporte.
- **COMP-002-04** · Un 403 con el cuerpo del contrato se muestra como mensaje informativo: "Acceso restringido" y el texto recibido; sin metadatos, "Reintentar" ni código de soporte.
- **COMP-002-05** · Una respuesta 200 cuyo texto es exactamente `TEXTO_NO_VERIFICABLE` de la 001 se muestra como aviso con "Volver a preguntar", que reenvía la misma pregunta.
- **COMP-002-06** · Cada error muestra su texto fijo y una acción: 400, 413 y 429 devuelven la pregunta al campo; 415 ofrece "Recargar"; 503, 500, otros códigos, ilegible y red ofrecen "Reintentar".
- **COMP-002-07** · Los errores con `X-Correlation-Id` en formato UUID v7 canónico muestran "Código de soporte" y "Copiar código"; un valor ausente o con otro formato no se muestra.
- **COMP-002-08** · Un cuerpo 200 o 403 que no cumple el contrato (campo ausente, tipo distinto, `respuesta` vacía, `citas` no lista, número negativo) es "Respuesta ilegible" y no se muestra nada de él.
- **COMP-002-09** · La salud se comprueba al cargar, cada 30 s con la pestaña visible, tras un error de red o un 503 y con "Comprobar ahora"; solo `200 {"estado":"ok"}` cuenta como "Servicio en línea".
- **COMP-002-10** · El historial muestra cada turno con la pregunta, la identidad que la hizo y su resultado; conserva los últimos 50 turnos.
- **COMP-002-11** · Mientras espera, "Preguntar" muestra "Consultando…" y está deshabilitado, el turno pendiente muestra un marcador de carga y solo hay una petición en curso; tras 35 s sin respuesta se cancela y se trata como error de red.
- **COMP-002-12** · 429: "Preguntar" queda deshabilitado `Retry-After` segundos (60 si falta o no es un entero de 1 a 3600) con una cuenta regresiva visible; el selector sigue habilitado; la espera se conserva al cambiar de identidad; al terminar se anuncia el fin y no se reenvía nada.
- **COMP-002-13** · Cambiar de una identidad a otra muestra y anuncia el aviso de cambio de identidad, sin bloquear ni pedir confirmación; con una espera pendiente, el aviso añade la frase de la espera.
- **UX-002-01** · Estética cálida y clara: fondo #f7f7f7 o blanco, acento #ff6a13, texto #2b1d19, tarjetas redondeadas, botones e insignias en píldora, sombras suaves; sin marca, logos, textos ni fuentes de Q10.
- **UX-002-02** · El texto sobre el naranja es siempre #2b1d19; el naranja nunca es color de texto sobre fondos claros.
- **UX-002-03** · Sin turnos y con identidad elegida, la bienvenida ofrece preguntas de ejemplo de su rol; sin identidad, 0 preguntas de ejemplo; pulsar una solo llena el campo y lo enfoca.
- **UX-002-04** · Enter envía y Mayús+Enter inserta un salto de línea; al enviar, el campo se vacía y el foco permanece en él.
- **UX-002-05** · La vista se desplaza al último turno; con movimiento reducido, sin animación.
- **UX-002-06** · Transiciones de 200 ms y elevación al pasar el cursor; con `prefers-reduced-motion: reduce`, sin transiciones ni elevación.
- **UX-002-07** · Móvil (< 768 px): una columna con el campo fijo abajo; escritorio: columna centrada.
- **UX-002-08** · Mensajes en español, en segunda persona y sin jerga técnica (sin "HTTP", "403", "JSON" ni "API"); el texto de 429 no culpa a la persona ni revela la actividad de otros.

## 6. Reglas de negocio
Ninguna nueva. La interfaz no calcula ni interpreta datos académicos: muestra lo que entrega el contrato (RN-01…RN-14 de la 001 se aplican en el backend).

## 7. Seguridad y permisos
La matriz de [`constitution.md`](../../constitution.md) §3 no cambia. **La interfaz no es una barrera de seguridad:** toda autorización la decide el backend.

- **SEG-002-01** · La respuesta, la pregunta y los mensajes se muestran como texto: ningún HTML ni Markdown se interpreta y ningún texto se convierte en enlace → se ve literal.
- **SEG-002-02** · La página se sirve con una política de contenido que solo admite scripts, estilos, fuentes, imágenes y conexiones del propio origen e impide ser embebida → el navegador bloquea lo demás.
- **SEG-002-03** · El cliente no contiene secretos: ninguna variable de entorno entra al build y una variable `VITE_*` en el entorno impide construir.
- **SEG-002-04** · Ninguna pregunta, respuesta, identidad ni dato académico se guarda en el navegador (almacenamiento local o de sesión, IndexedDB, cookies, caché) ni se escribe en la consola.
- **SEG-002-05** · La interfaz solo pide recursos a su propio origen: sin fuentes, analítica, recursos de terceros ni backend en otro origen.
- **SEG-002-06** · La identidad solo se elige en el selector; parámetros de la URL o valores fuera de la lista no seleccionan identidad → "Preguntar" deshabilitado y 0 peticiones.
- **SEG-002-07** · Los errores nunca muestran el título, el detalle ni otro contenido del cuerpo de error del servidor.
- **SEG-002-08** · Al cambiar de identidad, la petición en curso se cancela y su resultado se descarta → nunca aparece bajo la nueva identidad; la región de anuncios y los códigos de soporte de la identidad anterior desaparecen.
- **SEG-002-09** · El cuerpo enviado tiene exactamente `pregunta` y `usuario {rol, id}`; la interfaz no envía encabezados de correlación.
- **SEG-002-10** · El build no contiene nombres de personas ni de programas de la API académica, nombres de herramientas del backend, direcciones internas ni mapas de código fuente.
- **SEG-002-11** · Sin marca de Q10: el build no contiene "q10" y la app no hace peticiones a dominios de Q10.
- **SEG-002-12** · Al salir de la página (navegar, cerrar, volver atrás) se borran historial, identidad, campo y anuncios; una página restaurada por el navegador (caché de ida y vuelta, reabrir pestaña o restauración de formularios) empieza sin identidad, sin turnos y con el campo vacío.
- **SEG-002-13** · Los caracteres de control bidireccional y de ancho cero de la respuesta y la pregunta se muestran aislados: no reordenan ni ocultan el texto que los rodea (metadatos, otros turnos).
- **SEG-002-14** · Por el puerto de la interfaz solo se alcanzan las 2 rutas del contrato con su método (POST y GET); otra ruta, método o variante codificada responde 404, 403 o 405 del servidor web sin llegar al backend, o llega a la ruta del contrato sin consulta.
- **SEG-002-15** · El backend registra y limita cada pregunta hecha por la interfaz con la IP del cliente, distinta de la del proxy; `X-Forwarded-For`, `Forwarded` o `X-Real-IP` enviados por el cliente no cambian esa IP ni por el puerto de la interfaz ni por el 8080.
- **SEG-002-16** · Por defecto el proxy no limita la tasa de preguntas (D-70 de la 001); solo la limita si el operador lo activa. Un 429 del proxy o del backend llega a la interfaz con su `Retry-After`. Las respuestas de `/api/*` llevan `Cache-Control: no-store` y nunca se guardan en el proxy.
- **SEG-002-17** · La interfaz del perfil `web` solo escucha en 127.0.0.1: desde otra máquina no se puede usar el selector de usuarios.
- **SEG-002-18** · Ningún `documento` ni `seccion` de `citas` llega a la página (texto, atributos ni anuncios), con 1 o con 50 citas, en una respuesta 200 o en una negativa 403 → 0 apariciones en el DOM.

## 8. Requerimientos no funcionales
- **RNF-002-01** · Accesibilidad: 0 violaciones WCAG 2.2 A y AA en cada estado; contraste ≥ 4.5:1 en texto y ≥ 3:1 en bordes de campos, iconos y foco.
- **RNF-002-02** · Responsivo: sin desplazamiento horizontal a 320, 360, 768 y 1280 px; controles principales de al menos 44 × 44 px.
- **RNF-002-03** · "Consultando…" aparece en ≤ 200 ms tras enviar.
- **RNF-002-04** · La carga inicial transfiere ≤ 250 KB comprimidos de JavaScript y CSS.
- **RNF-002-05** · Funciona en las versiones actuales de Chromium, Firefox y WebKit.
- **RNF-002-06** · Mantenibilidad: reglas de código del frontend en `plan.md` §4.8 y [`AGENTS.md`](../../AGENTS.md).
- **RNF-002-07** · Disponibilidad del perfil `web`: tras reiniciar o recrear el backend, la interfaz vuelve a responder `/api/salud` en ≤ 30 s sin reiniciar el contenedor web.

## 9. Criterios de aceptación
Un escenario o esquema por CA; el detalle está en los ESC indicados y el umbral en `plan.md` §9.2. "Doble del backend" = respuesta simulada en el navegador con el código, el cuerpo y los encabezados de la fila.
```gherkin
# language: es
Característica: Interfaz web de TutorFlow
  Esquema del escenario: CA-002-01 · Identidad de una lista cerrada y marcada como demostración (Detalle: ESC-200…206)
    Dado la app abierta en un ancho de <ancho> px sin identidad elegida
    Cuando la persona elige "<opcion>" y pregunta "¿Cuál es la nota mínima para aprobar una asignatura?"
    Entonces se envía 1 petición con "usuario" exactamente {"rol":"<rol>","id":"<id>"}
    Y el aviso "Entorno de demostración: eliges un usuario de prueba y no hay inicio de sesión. Los permisos los decide el servidor." está visible y la página tiene 0 campos de texto además de "Tu pregunta"
    Ejemplos:
      | ancho | opcion                 | rol         | id       |
      | 360   | EST-001 · estudiante   | estudiante  | EST-001  |
      | 1280  | EST-006 · estudiante   | estudiante  | EST-006  |
      | 360   | COORD-01 · coordinador | coordinador | COORD-01 |
      | 1280  | COORD-02 · coordinador | coordinador | COORD-02 |
  Esquema del escenario: CA-002-02 · La pregunta se valida antes de enviarla (Detalle: ESC-215…222)
    Dado la identidad "EST-001" y el servicio en línea
    Cuando escribe <entrada> y pulsa Enter <veces> vez o veces
    Entonces el contador muestra "<contador>", "Preguntar" está <estado> y se enviaron <peticiones> peticiones
    Ejemplos:
      | entrada                                    | veces | contador  | estado        | peticiones |
      | una pregunta vacía                         | 1     | 0/1000    | deshabilitado | 0          |
      | 3 espacios                                 | 1     | 3/1000    | deshabilitado | 0          |
      | 1000 letras "a"                            | 1     | 1000/1000 | habilitado    | 1          |
      | 999 letras "a" y el emoji "😀"             | 1     | 1001/1000 | deshabilitado | 0          |
      | "¿Cuál es mi promedio?" con el doble lento | 2     | 0/1000    | deshabilitado | 1          |
  Esquema del escenario: CA-002-03 · Respuesta con tokens, sin costo ni documentos citados (Detalle: ESC-223…229, ESC-273 b)
    Dado la identidad "<id>" y un doble del backend que responde 200 con <cuerpo>
    Cuando envía una pregunta
    Entonces la tarjeta muestra el texto <texto> y "<metadatos>"
    Y la insignia "Usó datos académicos" aparece <insignia> veces
    Y "US$", el valor de "costoUsd" y cada "documento" y "seccion" de "citas" aparecen 0 veces en el DOM
    Ejemplos:
      | id       | cuerpo                                       | texto                             | metadatos   | insignia |
      | EST-001  | el de ESC-223 (1 cita)                       | exacto de ESC-223                 | 812 tokens  | 0        |
      | EST-001  | el de ESC-224, con 5 líneas (2 citas)        | en 5 líneas, en el orden recibido | 1530 tokens | 1        |
      | COORD-02 | el de ESC-225 (2 citas)                      | exacto de ESC-225                 | 2048 tokens | 1        |
      | EST-001  | el de ESC-223 con 50 citas                   | exacto de ESC-223                 | 812 tokens  | 0        |
  Esquema del escenario: CA-002-04 · Sin información, negativa y no verificable son mensajes informativos (Detalle: ESC-230…234)
    Dado la identidad "<id>" y un doble del backend que responde <http> con "respuesta" "<texto>", "citas" <citas>, "tokens" 0 y "costoUsd" 0
    Cuando envía una pregunta
    Entonces se muestra "<titulo>" con el texto exacto, la acción "<accion>" y 0 líneas "Código de soporte"
    Y los metadatos del turno son <metadatos>, y "US$" y el texto de las citas aparecen 0 veces en el DOM
    Ejemplos:
      | id       | http | texto                                                                                                     | citas     | titulo             | metadatos  | accion             |
      | EST-001  | 200  | No encuentro esa información en los documentos disponibles.                                               | []        | (sin título)       | "0 tokens" | ninguna            |
      | EST-001  | 403  | No puedo compartir esa información: solo puedes consultar tus propios datos académicos.                   | [Art. 22] | Acceso restringido | (ninguno)  | ninguna            |
      | COORD-01 | 403  | No puedo compartir esa información: solo puedes consultar los datos de los estudiantes de tu programa.   | [Art. 22] | Acceso restringido | (ninguno)  | ninguna            |
      | EST-001  | 200  | No pude verificar los datos de la respuesta. Intenta de nuevo o reformula la pregunta.                    | []        | (sin título)       | "0 tokens" | Volver a preguntar |
  Esquema del escenario: CA-002-05 · Cada error tiene texto fijo, acción y código de soporte (Detalle: ESC-235…249)
    Dado la identidad "EST-001" y un doble del backend que responde <respuesta> con "X-Correlation-Id" "019a5f3e-7c1d-7b2a-9e4f-0123456789ab"
    Cuando envía "¿Cuál es mi promedio?"
    Entonces se muestra exactamente "<texto>" con la acción "<accion>", "Código de soporte" aparece <soporte> veces y el texto "System." aparece 0 veces
    Ejemplos:
      | respuesta                                       | texto                                                                          | accion                                       | soporte |
      | 400 con detalle "System.Text.Json falló"        | No pudimos procesar la pregunta. Revísala e intenta de nuevo.                  | la pregunta vuelve al campo                  | 1       |
      | 429 con "Retry-After: 120"                      | Hay muchas preguntas en este momento. Podrás preguntar de nuevo en 02:00.      | Preguntar deshabilitado 120 s, selector habilitado, 0 reenvíos | 1 |
      | 503                                             | El asistente no está disponible en este momento. Intenta de nuevo en unos minutos. | Reintentar                               | 1       |
      | 200 con "citas": null                           | Recibimos una respuesta que no pudimos leer. Intenta de nuevo.                 | Reintentar                                   | 1       |
      | un fallo de red, sin respuesta                  | No pudimos conectar con el asistente. Revisa tu conexión e intenta de nuevo.   | Reintentar                                   | 0       |
  Esquema del escenario: CA-002-06 · Estado del servicio según la salud (Detalle: ESC-250…256)
    Dado la identidad "EST-001" y un doble de "GET /api/salud" que responde <salud>
    Cuando la app comprueba la salud
    Entonces el indicador dice "<indicador>" y "Preguntar" está <estado>
    Ejemplos:
      | salud                                        | indicador              | estado        |
      | 200 {"estado":"ok"}                          | Servicio en línea      | habilitado    |
      | 503                                          | Servicio no disponible | deshabilitado |
      | un fallo de red                              | Servicio no disponible | deshabilitado |
      | 200 {"estado":"degradado"}                   | Servicio no disponible | deshabilitado |
      | 503 y, 30 s después, 200 {"estado":"ok"}     | Servicio en línea      | habilitado    |
  Esquema del escenario: CA-002-07 · Historial solo en memoria y borrado al cambiar de identidad o salir (Detalle: ESC-207…214)
    Dado la identidad "EST-002" con <turnos> turnos respondidos
    Cuando <accion>
    Entonces el historial tiene <despues> turnos, la identidad elegida es <identidad> y el DOM contiene 0 veces el texto de las respuestas borradas
    Ejemplos:
      | turnos | accion                                                                        | despues | identidad |
      | 2      | elige "COORD-01 · coordinador"                                                | 0       | COORD-01  |
      | 1      | elige "EST-001 · estudiante" mientras una pregunta espera respuesta del doble | 0       | EST-001   |
      | 2      | navega a "about:blank" y vuelve con Atrás                                     | 0       | ninguna   |
      | 2      | recarga la página                                                             | 0       | ninguna   |
      | 50     | envía una pregunta más                                                        | 50      | EST-002   |
  Esquema del escenario: CA-002-08 · Contenido malicioso se muestra como texto (Detalle: ESC-270…278)
    Dado la identidad "EST-001" y un doble del backend que responde 200 con <campo> igual a "<valor>"
    Cuando envía una pregunta
    Entonces <resultado>, la tarjeta tiene 0 elementos "img", "script", "a" y "b", y "window.__xss" no está definido
    Ejemplos:
      | campo                  | valor                                                    | resultado                                          |
      | "respuesta"            | <img src=x onerror="window.__xss=1">                     | la tarjeta muestra "<valor>" literal               |
      | "respuesta"            | <script>window.__xss=1</script>                          | la tarjeta muestra "<valor>" literal               |
      | "respuesta"            | **Negrita** y [enlace](javascript:window.__xss=1)        | la tarjeta muestra "<valor>" literal               |
      | "citas[0].documento"   | <b onclick="window.__xss=1">reglamento</b>               | el texto de las citas aparece 0 veces en el DOM    |
  Esquema del escenario: CA-002-09 · Accesibilidad AA en cada estado (Detalle: ESC-257…260)
    Dado la app en el estado "<estado>" a 360 px y a 1280 px
    Cuando se ejecuta la revisión automática con las reglas WCAG 2.2 A y AA
    Entonces hay 0 violaciones y cada par de colores de plan.md §8.1 cumple su contraste mínimo
    Ejemplos:
      | estado                                    |
      | bienvenida sin identidad                  |
      | cargando                                  |
      | respuesta de ESC-224 con insignia         |
      | negativa 403                              |
      | espera de 429 tras cambiar de identidad   |
  Escenario: CA-002-10 · Teclado, foco y anuncios (Detalle: ESC-261…265)
    Dado la app a 1280 px y solo el teclado
    Cuando la persona usa Tab para elegir "EST-001 · estudiante", escribe "¿Cuál es mi promedio?" y pulsa Enter
    Entonces cada control enfocado muestra un contorno de 2 px con contraste ≥ 3:1 y ningún control queda fuera del orden de tabulación
    Y la región de anuncios recibe "Consultando al asistente…" y luego el texto de la respuesta, y el foco queda en "Tu pregunta"
  Esquema del escenario: CA-002-11 · Responsivo y movimiento reducido (Detalle: ESC-266…269)
    Dado la app con la respuesta de ESC-224 en un ancho de <ancho> px y movimiento reducido <reducido>
    Cuando se pasa el cursor o se enfoca "Preguntar"
    Entonces el desplazamiento horizontal es 0 px y la duración de la transición es <duracion>
    Ejemplos:
      | ancho | reducido | duracion |
      | 320   | no       | 200 ms   |
      | 360   | sí       | 0 ms     |
      | 768   | no       | 200 ms   |
      | 1280  | sí       | 0 ms     |
  Escenario: CA-002-12 · Privacidad, secretos y política de contenido (Detalle: ESC-279…289)
    Dado el build de producción servido por el contenedor web
    Cuando una persona recorre los flujos F1 a F7
    Entonces el almacenamiento local, el de sesión, IndexedDB y las cookies tienen 0 entradas y la consola tiene 0 mensajes con la pregunta, la respuesta o "EST-"
    Y el 100% de las peticiones van al mismo origen, hay 0 violaciones de la política de contenido y el build contiene 0 veces "LLM_API_KEY", "q10", "consultar_" y "api:8080"
  Esquema del escenario: CA-002-13 · La interfaz no es barrera: el backend real decide (Detalle: ESC-201, ESC-205, ESC-206, ESC-231, ESC-290…292)
    Dado el sistema con el perfil web, sin llave del LLM, y la identidad "<id>"
    Cuando pregunta "<pregunta>"
    Entonces se muestra "<resultado>", los metadatos del turno son <metadatos> y "US$" aparece 0 veces en el DOM
    Ejemplos:
      | id       | pregunta                                              | resultado                                                                                              | metadatos  |
      | EST-001  | ¿Cuáles son las notas de EST-002?                     | No puedo compartir esa información: solo puedes consultar tus propios datos académicos.               | (ninguno)  |
      | EST-001  | ¿Cuáles son las notas de EST-999?                     | No puedo compartir esa información: solo puedes consultar tus propios datos académicos.               | (ninguno)  |
      | COORD-01 | ¿Qué estudiantes de ADM perdieron materias?           | No puedo compartir esa información: solo puedes consultar los datos de los estudiantes de tu programa. | (ninguno)  |
      | EST-001  | ¿Qué dice el reglamento sobre el uso de parqueaderos? | No encuentro esa información en los documentos disponibles.                                            | "0 tokens" |
  Esquema del escenario: CA-002-14 · Despliegue opcional, local y CI (Detalle: ESC-293…298)
    Dado el repositorio con el frontend
    Cuando <accion>
    Entonces <resultado>
    Ejemplos:
      | accion                                                    | resultado                                                                                   |
      | se ejecuta "docker compose up" sin perfil                 | no existe el contenedor web y "GET localhost:8080/api/salud" responde 200                   |
      | se ejecuta "docker compose --profile web up"              | "GET 127.0.0.1:5173/" responde 200 text/html y el puerto 5173 escucha solo en 127.0.0.1     |
      | se reinicia el servicio "api" con el perfil web activo    | "GET 127.0.0.1:5173/api/salud" vuelve a responder 200 en ≤ 30 s sin reiniciar el contenedor web |
      | un pull request agrega "dangerouslySetInnerHTML"          | el trabajo "frontend" del CI falla                                                          |
      | un pull request agrega un paquete con un script de instalación | el script no se ejecuta en el CI y el trabajo "frontend" falla si el paquete no está en la lista |
  Esquema del escenario: CA-002-15 · El proxy conserva la IP del cliente, no limita por defecto y solo reenvía el contrato (Detalle: ESC-294, ESC-295, ESC-299)
    Dado el sistema con el perfil web recién iniciado y el bloqueo de identidades desactivado
    Cuando el cliente local <accion>
    Entonces <resultado>
    Ejemplos:
      | accion                                                                          | resultado                                                                                       |
      | pregunta como EST-001…EST-004 por 5173                                          | los 4 eventos de auditoría tienen la misma "ipOrigen", distinta de las 2 IP de "web", y hay 1 evento "multiples_identidades" con los 4 IDs |
      | pregunta por 5173 con "X-Forwarded-For: 203.0.113.7"                             | la "ipOrigen" del evento es distinta de 203.0.113.7 y de las 2 IP de "web"                       |
      | pregunta por 8080 con "X-Forwarded-For: 203.0.113.7"                             | la "ipOrigen" del evento es distinta de 203.0.113.7                                              |
      | envía 30 preguntas en menos de 5 s por 5173 con la configuración por defecto     | las 30 llegan al backend y responden 200, con 0 respuestas 429                                   |
      | envía "OPTIONS /api/salud" y "GET /api/otra" por 5173                            | 405 o 403 y 404 del servidor web, y 0 peticiones al backend                                      |
```

## 10. Preguntas abiertas
- Ninguna. D-002-14 fue aprobada por el humano como limitación del entorno.
