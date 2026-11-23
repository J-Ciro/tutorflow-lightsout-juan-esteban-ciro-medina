# Decisiones · 001 Asistente académico

> Regla: cada decisión ocupa como máximo 3 líneas (decisión · por qué · estado). Una corrección actualiza la regla en `spec.md` o `plan.md` y deja aquí su D; la historia está en git.

Origen: D-01…D-29 aprobadas por el humano; D-30…D-40 y D-53, decisiones del orquestador; D-41…D-52, plan de la ronda 4 aprobado por el humano; D-54…D-64, INT-020…INT-022. Ninguna cambia el contrato ni la matriz de acceso de [`constitution.md`](../../constitution.md). D-11, D-12, D-18 y D-19 no existen (números no usados).

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
- **D-21** · La suplantación (SEG-16) solo se detecta y audita por defecto; el bloqueo es configurable. · Por qué: las pruebas ocultas usan varias identidades desde una IP. · Vigente.
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
- **D-51** · Plegado Unicode con tablas propias generadas de `UnicodeData.txt` como recurso incrustado. · Por qué: con `InvariantGlobalization` `Normalize` no hace nada. · Ajustada por D-61 y D-62 E6.
- **D-52** · Fase V: 5 corridas por pregunta con el defecto de temperatura; umbrales 5 de 5, no se bajan. · Por qué: el resultado no debe depender del azar. · Ajustada por D-59 y D-63 F8, F9.
- **D-53** · Las citas de reglas aplicadas se ligan al campo que usa la respuesta final verificada. · Por qué: una regla solo respalda lo que la respuesta dice. · Ajustada por D-56.
- **D-54** · Entrega de secciones: la primera, la primera relevante, las de puntaje ≥ 55% y las relevantes con 2 raíces y ≥ 40%. · Por qué: medido con el índice real, 0 secciones necesarias perdidas y márgenes > 5 puntos. · Vigente.
- **D-55** · Citas por tema de sección entregada (pesos y redondeo). · Por qué: Q10 #4 necesita la `Sección 2` y el modelo la escribe 1 de 32 veces. · Vigente.
- **D-56** · El campo definitiva también se usa por valor (par asignatura-cifra igual a la definitiva calculada). · Por qué: la cita no debe depender de la palabra. · Vigente.
- **D-57** · Contexto de Ollama: estimación de tokens, tope probado de 3.000 y detección del truncado que falla cerrado. · Por qué: `/v1` ignora `num_ctx` y el truncado es silencioso. · Vigente.
- **D-58** · Bloque de ejemplos normativo (EJ-1…EJ-4), presentación de la `Sección 3.1` y cifras de ejemplo fuera de las fuentes. · Por qué: medido, los ejemplos fijan el dato completo. · Vigente.
- **D-59** · `Llm:Temperatura` decimal de 0 a 0.3, por defecto 0, validada al arrancar. · Por qué: INT-022; ningún valor > 0 mejoró un criterio. · Vigente.
- **D-60** · Un nombre de documento escrito con espacios es una sola referencia; la eliminación de más queda mitigada (la fuente sigue en `citas`). · Por qué: restos ilegibles. · Vigente.
- **D-61** · Tabla de confusables (UTS #39) y 8 letras con trazo en SEG-04a. · Por qué: elimina el riesgo aceptado de homoglifos sin falsos 403. · Vigente.
- **D-62** · Ambigüedades E1–E7: E1 el reintento depende solo del verificador; E2 motivos sin persona para el estudiante; E3 valor monetario sin símbolo; E4 el encabezado de columnas es un bloque;
  E5 prueba del timeout con 1 s (750 + 500 ms, < 2 s); E6 recurso `TutorFlow.Core.Autorizacion.PlegadoUnicode.tsv` con `VersionUnicode` y `VersionConfusables`; E7 el propio ID plegado no deniega. · Vigente.
- **D-63** · Ambigüedades F1–F10: F1 mayúscula solo tras texto eliminado y sin cortar en `;`; F2 la resuelve D-55; F3 "Reglas aplicadas" termina en punto; F4 "3.9." se entrega; F5 promedio por cifra con 2 decimales; F6 espacios al borde inválidos en toda clave numérica;
  F7 un paso por regla del prompt; F8 `respondida` por HTTP = 200 sin texto fijo; F9 tabla de la fase V en `REPORTE.md`; F10 el coordinador sin programas no está exento. · Vigente.
- **D-64** · Un par asignatura-cifra sin la palabra "definitiva" se verifica contra la definitiva, los cortes y la acumulada. · Por qué: "Contabilidad: 3.3." se entregaba. · Vigente.

## Riesgos y su decisión

Ningún riesgo queda aceptado salvo D-01 (aprobado por el humano). Evidencia de la ronda 4 en `diag-riesgos/`.

| Riesgo | Decisión | Control verificable | D | CA / ESC |
| --- | --- | --- | --- | --- |
| Umbral relativo estimado y excepción de relevancia que entregaba 6 secciones | Mitigado: 55% o relevante con 2 raíces y 40%, más la primera relevante | 13 conjuntos fijados con el índice real; 0 necesarias perdidas | D-54 | CA-22, ESC-198 a |
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
| Homoglifos y letras con trazo sin plegar | Eliminado: tabla de confusables | 5 variantes con 403; 3 textos legítimos sin 403 | D-61 | CA-04, CA-24, ESC-186 f, ESC-199 a, b |
