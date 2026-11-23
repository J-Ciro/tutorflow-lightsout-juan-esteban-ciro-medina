# Decisiones · 001 Asistente académico

> Regla: cada decisión ocupa como máximo 3 líneas (decisión · por qué · estado). Una corrección actualiza la regla en `spec.md` o `plan.md` y deja aquí su D; la historia está en git.

Origen: D-01…D-29 aprobadas por el humano; D-30…D-40 y D-53, decisiones del orquestador; D-41…D-52, plan de la ronda 4 aprobado por el humano; D-54…D-64, INT-020…INT-022; D-65, ambigüedades de QA; D-66, arbitraje de la ronda 4; D-67, INT-024 (humano); D-68 y D-69, INT-025 (humano).
Ninguna cambia el contrato ni la matriz de [`constitution.md`](../../constitution.md). D-11, D-12, D-18 y D-19 no existen.

## Decisiones

- **D-01** · La identidad viaja en el cuerpo sin autenticación: se acepta (contrato fijo), se valida contra la API (SEG-03) y se detecta la rotación (SEG-16).
  Por qué: el contrato no se puede cambiar; en producción vendría de un token firmado. · Estado: vigente; riesgo residual A07 aceptado por el humano.
- **D-02** · Solo se usa el reglamento 2025; el 2023 está derogado (Art. 3). · Por qué: el 2023 cambia resultados (15% frente a 20%, habilitación 2.0 frente a 1.5, cancelación semana 8 frente a 10). · Vigente.
- **D-03** · "Más del 15%" es estrictamente mayor, con aritmética entera (`faltas × 100 > 15 × clases`): 7/48 no pierde, 8/48 pierde. · Por qué: literal del Art. 11, sin flotante. · Vigente.
- **D-04** · `inasistencias: null` = sin registro: no se presume pérdida y se remite al coordinador (Art. 12). · Por qué: no inventar datos. · Vigente.
- **D-05** · Nota acumulada con cortes pendientes = Σ(peso × corte calificado), 1 decimal, con el porcentaje evaluado y su equivalente (EST-006: 2.4 sobre el 60% ≈ 4.0). · Por qué: da las dos lecturas. · Vigente.
- **D-06** · Una asignatura perdida por inasistencia cuenta 0.0 en el promedio (Evaluación §4). · Por qué: literal. · Vigente.
- **D-07** · El 403 lleva la salida del contrato con `TEXTO_NEGATIVA` elegido solo por el rol declarado, `citas` [Art. 22], `usoDatosEstudiante` false y `tokens`/`costoUsd` 0 (salvo D-22).
  Por qué: cumple a la vez "403" y "negativa" con textos constantes. · Vigente.
- **D-08** · ID inexistente o rol incoherente → el mismo 403 que un acceso ajeno. · Por qué: denegar lo normativo a IDs válidos rompería el uso legítimo; la existencia de un ID se puede inferir con una pregunta normativa (limitación mitigada por SEG-13 y SEG-16). · Vigente.
- **D-09** · Terceros: solo deniegan el nombre completo o un ID con forma de persona; un nombre suelto no (lo cubren SEG-06 y SEG-10); el estudiante puede nombrar a su coordinador. · Por qué: privacidad sin falsos 403. · Vigente.
- **D-10** · `rol` con mayúsculas o espacios → 400. · Por qué: el contrato fija la lista cerrada. · Vigente.
- **D-13** · La auditoría registra `datosConsultados` (estudiante, programa, recurso, asignatura) sin valores. · Por qué: cumple el PDF y minimiza datos (Ley 1581). · Vigente, confirmada por el humano.
- **D-14** · Citas estructuradas `{documento, seccion}`: "Secciones 3 y 4" son dos entradas; los datos usan "Datos del estudiante"/"Datos del programa" como documento.
  Por qué: lo define el contrato; riesgo de comparación literal en pruebas ocultas, asumido. · Vigente.
- **D-15** · Separador decimal: punto. · Por qué: así lo usan el reglamento y las preguntas de ejemplo. · Vigente.
- **D-16** · LLM independiente del proveedor: se detecta por el prefijo de `LLM_API_KEY` y se puede forzar; sin llave, Ollama `qwen2.5:7b`; modelo pequeño por defecto. · Por qué: cualquier evaluador usa su llave; el LLM solo redacta. · Vigente, confirmada.
- **D-17** · El Anexo de `bienestar_y_becas.md` se excluye del índice y además se prueba la inyección indirecta con documentos sintéticos. · Por qué: material no confiable; defensa en profundidad. · Vigente.
- **D-20** · Beca con asignaturas sin definitiva → "no determinable aún". · Por qué: el promedio del periodo no está completo. · Vigente.
- **D-21** · La suplantación (SEG-16) solo se detecta y audita por defecto; el bloqueo es configurable. · Por qué: las pruebas ocultas usan varias identidades desde una IP. · Vigente; el comportamiento del bloqueo lo fija D-69.
- **D-22** · Si el modelo pide un ID ajeno: 403 tardío con el consumo real y `herramienta_id_ajeno`; se acepta que una inyección convierta una pregunta legítima en 403. · Por qué: fallar cerrado. · Vigente.
- **D-23** · En abuso, lo detectable antes del LLM da 403; lo no detectable se afirma por código, citas, `usoDatosEstudiante` y ausencia de datos de terceros, no por el texto libre. · Por qué: aserciones deterministas. · Vigente.
- **D-24** · `Microsoft.Extensions.AI` solo en Infrastructure tras `IClienteLlm`, sin invocación automática de funciones; Anthropic con su SDK; sin `Microsoft.Agents.AI`. · Por qué: menos HTTP propio, sin sobreingeniería. · Vigente.
- **D-25** · Auditoría en consola y en un JSONL diario en un volumen de Docker. · Por qué: sobrevive a reinicios. · Vigente, confirmada.
- **D-26** · `correlationId` UUID v7 generado por el servidor, igual en `X-Correlation-Id`, `traceId` y auditoría; el del cliente se ignora. · Por qué: estándar, ordenable y no falsificable. · Vigente, aprobada.
- **D-27** · El estudiante que pregunta por sus datos recibe su resumen calculado en la primera llamada (COMP-07); el coordinador no. · Por qué: los modelos pequeños no invocan la herramienta e inventan. · Vigente, aprobada.
- **D-28** · Si el modelo no escribe citas, respaldo acotado con secciones entregadas que apoyan la respuesta. · Por qué: evita `citas: []` en respuestas correctas sin citar de relleno.
  Estado: ajustada por D-29 (apoyo), D-31 (reglas aplicadas antes) y D-49 (1 sección).
- **D-29** · No invención determinista: umbral de relevancia previo al LLM, respuesta sin respaldo → COMP-03, citas condicionadas al apoyo, precio inventado → COMP-03. · Por qué: "no inventar" no puede depender del modelo. · Ajustada por D-49 y D-54.
- **D-30** · Las cifras con forma de nota o porcentaje cuentan como raíces de apoyo; el resumen propio se activa también con `puedo` + término específico o con una asignatura matriculada. · Por qué: tablas de valores y preguntas de datos propios. · Vigente.
- **D-31** · Con datos leídos, se citan hasta 3 secciones del bloque "Reglas aplicadas" ordenadas por relevancia (el orden fijo desempata). · Por qué: la cita normativa no depende del modelo. · Ajustada por D-49 y D-53.
- **D-32** · Solo las partes confidenciales del prompt cuentan como fuga; el bloque de ejemplos es ficticio y no confidencial. · Por qué: el ejemplo no es secreto. · Ajustada por D-58 (cifras admitidas si no están en las fuentes).
- **D-33** · Los números de una referencia a sección ("Sección 3.1", "Artículo 5") no se verifican como cifras. · Por qué: no son datos de la respuesta. · Vigente.
- **D-34** · Toda afirmación de pérdida sobre una persona autorizada se contrasta con los datos leídos (SEG-10f). · Por qué: una pérdida inventada es un dato falso sobre alguien. · Ajustada por D-42.
- **D-35** · Cualquier ID con forma de persona no autorizado en la salida bloquea, exista o no, con la misma salida (SEG-10b). · Por qué: no abrir otro canal para saber si un ID existe. · Vigente.
- **D-36** · Formas de pérdida ampliadas: construcciones nominales, asociación antes o después y sujeto coordinado. · Por qué: formatos reales del modelo.
  Estado: herencia en listas y negación por cercanía → sustituidas por D-42; el resto vigente.
- **D-37** · Las referencias a documentos escritas en el texto que no corresponden a una cita devuelta se eliminan. · Por qué: una referencia inventada parece una fuente. · Ajustada por D-41 (antes de SEG-10, con separador) y D-50.
- **D-38** · La definitiva afirmada para una asignatura debe ser la calculada para ella (SEG-10g), con exclusión de umbrales comparativos. · Por qué: presentar la acumulada como definitiva es falso.
  Estado: negación por cercanía y verbo de requisito → sustituidos por D-42; el resto vigente.
- **D-39** · Modelo sin precio: reserva y gasto fijos `Llm:ReservaSinPrecioUsd` (0.01 USD), `costoUsd` 0 y `precioDesconocido: true`; Ollama tiene precio 0. · Por qué: sin precio el presupuesto no frenaba. · Vigente.
- **D-40** · La configuración se valida al arrancar: falla rápida con una línea por valor inválido o defecto seguro con advertencia; catálogo validado. · Por qué: un control desactivado en silencio es invisible. · Ajustada por D-43 y D-63 F6.
- **D-41** · Lo entregado es lo verificado: citas → limpieza → SEG-10 sobre el texto final; lo eliminado deja un espacio; cifra partida verificada. · Por qué: verificar un texto y entregar otro abre un canal.
  Estado: vigente; sustituye la verificación del texto original de la v1.10.
- **D-42** · SEG-10f y SEG-10g se verifican por bloque y fallan cerradas; negación solo si gobierna; requisito solo en "necesitas (una) definitiva de". · Por qué: el formato deja de importar. · Vigente; sustituye partes de D-34, D-36 y D-38.
- **D-43** · Normalización completa (NFD + marcas `Mn`, dígitos `Nd`, letras sueltas), agregados nuevos, aviso único por código, escape `Zl`/`Zp`/`Cf`, reserva ≤ 0 con defecto. · Por qué: hallazgos N-04…N-07.
  Estado: vigente; su riesgo aceptado de homoglifos → sustituido por D-61.
- **D-44** · El núcleo entrega los datos calculados con formato normativo (una línea por campo, sin mezclar pérdida y falta de definitiva). · Por qué: el modelo copia lo que recibe. · Vigente.
- **D-45** · Prelectura del programa para el coordinador con un activador (COMP-08). · Por qué: el modelo pequeño no invoca la herramienta; no amplía el acceso. · Vigente.
- **D-46** · Prompt en pasos cortos con reglas verificables P-01…P-07; el texto exacto lo elige la implementación. · Por qué: el modelo pequeño sigue mejor pasos cortos. · Ajustada por D-58.
- **D-47** · Llamada determinista con semilla `Llm:Semilla` (Anthropic sin semilla). · Por qué: un fallo debe reproducirse. · Estado: temperatura 0 fija → sustituida por D-59; semilla vigente.
- **D-48** · Un único reintento acotado tras SEG-10c, d, f o g; nunca tras SEG-10b. · Por qué: con temperatura 0 el modelo repite el error si no se le dice cuál fue; SEG-10b repetiría un dato ajeno. · Vigente.
- **D-49** · (1) reglas aplicadas solo con relevancia > 0; (2) umbral relativo de recuperación del 50% → sustituido por D-54; (3) respaldo de 1 sola sección. · Por qué: citas de relevancia 0 degradan la precisión. · (1) y (3) vigentes.
- **D-50** · Limpieza ampliada: oración de atribución huérfana, restos solapados, atribución genérica sin cita y frase de COMP-03 anexada. · Por qué: textos truncados o mal atribuidos. · Vigente; mitigada por D-60.
- **D-51** · Plegado Unicode con tablas propias generadas de `UnicodeData.txt` como recurso incrustado. · Por qué: con `InvariantGlobalization` `Normalize` no hace nada. · Ajustada por D-61, D-62 E6 y D-65 G2.
- **D-52** · Fase V: 5 corridas por pregunta con el defecto de temperatura; umbrales 5 de 5, no se bajan. · Por qué: el resultado no debe depender del azar. · Ajustada por D-59 y D-63 F8, F9.
- **D-53** · Las citas de reglas aplicadas se ligan al campo que usa la respuesta final verificada. · Por qué: una regla solo respalda lo que la respuesta dice. · Ajustada por D-56.
- **D-54** · Entrega de secciones: la primera, la primera relevante, las de puntaje ≥ 55% y las relevantes con 2 raíces y ≥ 40%. · Por qué: medido con el índice real, 0 secciones necesarias perdidas y márgenes > 5 puntos. · Vigente.
- **D-55** · Citas por tema de sección entregada (pesos y redondeo). · Por qué: Q10 #4 necesita la `Sección 2` y el modelo la escribe 1 de 32 veces. · Vigente.
- **D-56** · El campo definitiva también se usa por valor (par asignatura-cifra igual a la definitiva calculada). · Por qué: la cita no debe depender de la palabra. · Vigente.
- **D-57** · Contexto de Ollama: estimación de tokens, tope probado de 3.000 y detección del truncado que falla cerrado. · Por qué: `/v1` ignora `num_ctx` y el truncado es silencioso. · Vigente; estimación precisada por D-65 G4.
- **D-58** · Bloque de ejemplos normativo (EJ-1…EJ-4), presentación de la `Sección 3.1` y cifras de ejemplo fuera de las fuentes. · Por qué: medido, los ejemplos fijan el dato completo. · Vigente.
- **D-59** · `Llm:Temperatura` decimal de 0 a 0.3, por defecto 0, validada al arrancar. · Por qué: INT-022; ningún valor > 0 mejoró un criterio. · Vigente.
- **D-60** · Un nombre de documento escrito con espacios es una sola referencia; la eliminación de más queda mitigada (la fuente sigue en `citas`). · Por qué: restos ilegibles. · Vigente.
- **D-61** · Tabla de confusables (UTS #39) y 8 letras con trazo en SEG-04a. · Por qué: elimina el riesgo aceptado de homoglifos sin falsos 403. · Vigente.
- **D-62** · Ambigüedades E1–E7: E1 el reintento depende solo del verificador; E2 motivos sin persona para el estudiante; E3 valor monetario sin símbolo; E4 el encabezado de columnas es un bloque;
  E5 prueba del timeout con 1 s (750 + 500 ms, < 2 s); E6 recurso `TutorFlow.Core.Autorizacion.PlegadoUnicode.tsv` con `VersionUnicode` y `VersionConfusables`; E7 el propio ID plegado no deniega. · Vigente.
- **D-63** · Ambigüedades F1–F10: F1 mayúscula solo tras texto eliminado y sin cortar en `;`; F2 la resuelve D-55; F3 "Reglas aplicadas" termina en punto; F4 "3.9." se entrega; F5 promedio por cifra con 2 decimales; F6 espacios al borde inválidos en toda clave numérica;
  F7 un paso por regla del prompt; F8 `respondida` por HTTP = 200 sin texto fijo; F9 tabla de la fase V en `REPORTE.md`; F10 el coordinador sin programas no está exento. · Vigente.
- **D-64** · Un par asignatura-cifra sin la palabra "definitiva" se verifica contra la definitiva, los cortes y la acumulada. · Por qué: "Contabilidad: 3.3." se entregaba. · Vigente.
- **D-65** · Ambigüedades G1–G6 de QA: G1 un corte solo pasa SEG-10c si llegó por `consultar_cortes`; G2 esquema del recurso de plegado (plan §9.1); G3 el turno de herramienta de ESC-199 d2 es `consultar_notas` sin resumen; G4 la estimación cuenta `messages[].content` con el mensaje `system`, sin `tools`;
  G5 CA-14 exige el Art. 22 en ESC-018 solo con cita escrita; G6 los límites de 55% y 40% se prueban por sensibilidad ⌊P⌋ / ⌊P⌋ + 1 con el índice real. · Por qué: la opción más simple y verificable; no cambia contrato, permisos ni reglas de producto. · Vigente; G5 → sustituida por D-66 B2.
- **D-66** · Arbitraje R4: A1 `đST-002` → `dst 002`, no es ID (ESC-199 a9); A2–A4, A7 pruebas obsoletas (§8.3, §6.5, D-54, §11.1 (b)); A5 (humano) sin mínimo de `prompt_tokens` y todo doble a ⌈1.04 × E⌉; A6 el registro en caché no es lectura (ESC-190 g); B1 oración genérica sin marcador personal no exige pérdida (ESC-035); B2 sin prelectura en normativas genéricas (ESC-190 j–m);
  B3 evals ESC-023, 155e, 176a obsoletos; C1 nota mínima y equivalente; C2 prompt de 11 pasos conforme; C3 (d) solo con `.`/`!`/`?` y sin cifra; C4 COMP-03 siempre `citas: []`; C5 sin precio, 0.01 por reserva; C6 `conf` sin `Nd` ni trazo; C7 `D` solo ASCII; D `consultar_cortes` con reglas aplicadas. · Por qué: sin bloqueos indebidos ni evasiones (INT-020). · Vigente; A5 aprobada por el humano.
- **D-67** · (Humano, INT-024) Las capacidades de otro rol solo se responden con el Art. 22: SEG-04c da 403 `capacidad_ajena` a rol ajeno + capacidad con contenido (qué, cuáles, cuántos) o con permisos, herramientas, lo que vería o simulación; pasan quién y sí o no. Entre coordinadores, igual; del coordinador a un estudiante, solo lo segundo. SEG-10d (d)(2): toda herramienta.
  Por qué: describir capacidades ajenas expone el modelo de permisos; la frontera léxica cerrada no bloquea normativas (INT-020). · Vigente; decisión del humano; no amplía la matriz ni cambia el contrato (403 de D-07).
- **D-68** · (Humano, INT-025) Proxy de confianza (SEG-19): `Proxy:IpsConfiables`, IPs individuales, vacía por defecto (IP inválida, red o rango → no arranca). Solo una conexión desde esa lista aporta el último valor de `X-Forwarded-For` (`ForwardLimit` 1, sin redes ni loopback implícitos); en Compose, solo la IP fija del contenedor `web` en la red `web-api`.
  Por qué: tras el proxy de la 002 todos compartían su IP (D-002-14); confiar en una subred o en el gateway del bridge dejaría falsificar la IP desde el host por el 8080. · Vigente; decisión del humano; no cambia el contrato ni la matriz.
- **D-69** · (Humano, INT-025) Corrige SEG-16 con bloqueo: solo la identidad nueva que supera el umbral recibe 429; las registradas siguen atendiéndose; el intento bloqueado no se registra; `Retry-After` = segundos, hacia arriba y mínimo 1, hasta que vence la entrada más antigua; un evento por identidad nueva y ventana.
  Por qué: el bloqueo alcanzaba a las 3 primeras y cada intento renovaba la ventana (bloqueo indefinido). · Vigente; decisión del humano; por defecto sigue solo auditando (D-21).

## Riesgos y su decisión

Ningún riesgo queda aceptado salvo D-01 (aprobado por el humano). Evidencia de la ronda 4 en `diag-riesgos/`.

| Riesgo | Decisión | Control verificable | D | CA / ESC |
| --- | --- | --- | --- | --- |
| Umbral relativo estimado y excepción de relevancia que entregaba 6 secciones | Mitigado: 55% o relevante con 2 raíces y 40%, más la primera relevante | 13 conjuntos fijados con el índice real; 0 necesarias perdidas; sensibilidad ⌊P⌋ / ⌊P⌋ + 1 de las reglas (3) y (4) | D-54, D-65 | CA-22, ESC-198 a |
| `Artículo 16` a 0.4 puntos del corte en Q10 #1 | Mitigado: queda 5.4 puntos por debajo del 55% | Conjunto de Q10 #1 fijado | D-54 | CA-22, ESC-198 a |
| `Sección 2` ausente en Q10 #4 | Eliminado: citas por tema | P4 con y sin cita escrita; 0 términos en 528 respuestas ajenas | D-55 | CA-01, CA-22, ESC-198 b–d |
| Citas de P2 y P5 dependientes de la palabra "definitiva" | Eliminado: campo definitiva por valor | P2 sin la palabra; Q10 #5 mínima | D-56 | CA-22, ESC-198 e, f |
| Cita de P6 dependiente de `perdidas` | Mitigado: las dos salidas incluyen el `Artículo 5` | ESC-194 g y fase V de P6 | D-56 | CA-01, ESC-197 |
| Par asignatura-cifra falso sin "definitiva" | Eliminado: se verifica contra definitiva, cortes y acumulada | 2 casos que fallan y 3 que pasan | D-64 | CA-17, CA-22, ESC-198 g |
| Contexto de 4.096 tokens de Ollama | Mitigado: tope de 3.000 estimados, detección del truncado, `OLLAMA_CONTEXT_LENGTH` documentado | Prueba del tope; 3 casos de detección y 1 de otro proveedor | D-57 | CA-23, ESC-199 d, e |
| Completitud de P2, P3, P7 y P8 sujeta al modelo | Mitigado: ejemplos normativos y presentación de la `Sección 3.1` | Fase V 5/5; marcas EJ-1…EJ-4; invariancia | D-58 | CA-01, CA-23, ESC-197, ESC-198 h–j |
| Documento del ejemplo citado como fuente | Eliminado: nunca está en `citas` y la limpieza lo quita | Respuesta limpiada | D-58 | ESC-198 k |
| Cifra copiada de un ejemplo igual a un dato real | Eliminado: cifras de ejemplo fuera de toda fuente | Prueba de las cifras del bloque | D-58 | CA-23, ESC-198 i |
| Temperatura mayor que 0 | Mitigado: rango [0, 0.3], defecto 0 | Arranque fallido fuera de rango; `temperature` en el cuerpo | D-59 | CA-15, CA-23, ESC-192, ESC-199 c, f |
| Atribución genérica con un resto suelto | Eliminado: nombre con espacios | Texto real limpiado | D-60 | CA-14, ESC-198 l |
| Atribución genérica eliminada de más | Mitigado: nunca de menos; la fuente queda en `citas` | Fase V: 0 atribuciones sin cita en 50 respuestas | D-60 | CA-01, CA-14, ESC-198 l |
| Homoglifos y letras con trazo sin plegar | Eliminado: tabla de confusables | 5 variantes con 403; 3 textos legítimos sin 403; esquema del recurso verificado fila por fila | D-61, D-65 | CA-04, CA-24, ESC-186 f, ESC-196, ESC-199 a, b |
| Art. 22 de una pregunta normativa del coordinador sin citar: la prelectura da reglas aplicadas y desplaza el respaldo | Eliminado: sin prelectura en normativas genéricas; el respaldo cita el Art. 22 | ESC-018 a y b con exactamente [Art. 22] y uso false | D-66 B2 | CA-14, CA-19, ESC-018, ESC-190 j, k |
| Exclusión normativa que quita la prelectura a una pregunta de datos | Mitigado: `mi`, `mis`, `nuestro(s)` o un estudiante suyo la anulan y las herramientas siguen disponibles | ESC-190 l con prelectura; ESC-190 m con 403 previo | D-66 B2 | CA-19, ESC-190 l, m |
| Oración normativa genérica bloqueada como pérdida del estudiante (ESC-011) | Eliminado: (ii) solo con marcador personal o fragmento nominal; (i) siempre | ESC-035 a, b pasan; c–e fallan; f con negativa | D-66 B1 | CA-06, ESC-011, ESC-035 |
| Truncado no detectado por un mínimo de 100 `prompt_tokens` agregado sin D | Eliminado: se evalúa todo `usage` | `prompt_tokens` 10 → truncado; todos los dobles a ⌈1.04 × E⌉; 0 `contextoTruncado` con los dobles (CA-11) | D-66 A5 (humano) | CA-23, ESC-199 e |
| Falso truncado si Ollama reporta menos `prompt_tokens` por la caché de prompt | Mitigado: medido 1.04–1.10 × E en corridas repetidas | 0 `contextoTruncado` en la fase V con Ollama | D-57, D-66 A5 | CA-23 |
| Línea de datos de 2 palabras con cifra o ítem sin punto eliminados por la limpieza | Eliminado: (d) exige `.`, `!` o `?` y ninguna cifra | ESC-178 k, l | D-66 C3 | CA-14, ESC-178 |
| Un usuario descubre lo que ve o hace otro rol u otro coordinador (INT-024) | Eliminado en el filtro previo: SEG-04c con listas cerradas | ESC-037 a–h, 038 a–g, 039 a–d con 403, 0 llamadas y 0 lecturas | D-67 | CA-04 |
| Falso 403 en normativas o en preguntas del coordinador sobre sus estudiantes | Mitigado: sin contenido ni (D) pasa; con un estudiante solo deniega (D) | ESC-036, 037 i–k, 039 e, f, 018, 123, 126, 165 d sin 403 | D-67 | CA-12 |
| Escape léxico (`c00rdinador`, sinónimo fuera de lista) | Mitigado: el estudiante nunca recibe herramientas del coordinador y SEG-10d bloquea identificadores de cualquier rol | ESC-038 i (COMP-03 sin LLM), 038 j (`fuga_bloqueada`); espía con 0 herramientas del coordinador | D-67 | CA-04 |
| Simulación en dos turnos | Eliminado: sin historial; cada petición se filtra sola | ESC-038 h | D-67 | CA-04 |
| Confusable de un dígito `Nd` plegado a otro dígito | Eliminado: `conf` sin orígenes `Nd` | Esquema fila por fila: 0 orígenes `conf` `Nd` | D-66 C6 | CA-24, ESC-199 b |
| IP falsificada con `X-Forwarded-For` (límite, SEG-16 o `ipOrigen` ajenos) | Eliminado: solo cuenta desde una IP exacta de la lista, vacía por defecto; loopback y redes nunca implícitos | Cabecera ignorada sin lista, desde otra IP, desde loopback y por el 8080 de Compose | D-68 | CA-13, ESC-047, 049, 067, 095 a |
| Gateway del bridge o subred confiados: el host falsifica la IP por el 8080 | Eliminado: solo IPs individuales (red o rango no arranca) y red `web-api` dedicada e interna | ESC-096 a, b, e sin arrancar; ESC-095 a con la IP real | D-68 | CA-13, CA-15 |
| IP compartida tras el proxy de la 002 (D-002-14) | Eliminado con la IP fija del contenedor `web` configurada | Dos clientes con límites independientes; 4.ª identidad con la IP del cliente; ESC-095 b | D-68 | CA-13, ESC-068, 069 |
| Cadena de `X-Forwarded-For` con valores puestos por el cliente | Mitigado: solo el último valor (`ForwardLimit` 1), el que escribe el proxy; el proxy de la 002 sobrescribe | "198.51.100.1, 203.0.113.7" → 203.0.113.7; ESC-095 c | D-68 | CA-13, ESC-048 b |
| Valor no IP o con cabeceras `Forwarded`, `X-Forwarded-Host`/`-Proto` inyectadas | Eliminado: solo se lee `X-Forwarded-For`; un valor inválido conserva la IP de la conexión | ESC-048 e con `ipOrigen` "10.0.0.5" | D-68 | CA-13 |
| Bloqueo indefinido y legítimos bloqueados con SEG-16 activado | Eliminado: solo la identidad nueva; el intento bloqueado no se registra | ESC-141, 098, 099 con reloj controlado | D-69 | CA-13 |
| Eventos `multiples_identidades` repetidos por reintentos | Eliminado: uno por identidad nueva y ventana | ESC-097, 098 b | D-69 | CA-13 |
| Con bloqueo, 429 (ID válido nuevo) frente a 403 (ID inexistente) revela si un ID existe | Mitigado: la misma inferencia ya existe con una normativa (D-08); acotada por SEG-13 y por el bloqueo por defecto desactivado | ESC-141 c | D-08, D-69 | CA-13 |
