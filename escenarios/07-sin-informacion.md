# 07 · Sin información y fuera de dominio (CA-06)

Esperado en todos: 200, `respuesta` exactamente igual al texto de COMP-03, `citas: []` y `usoDatosEstudiante: false`.

### ESC-090 · Tema académico ausente `@alterno`
- **Dado** `EST-001` · **Cuando** pregunta "¿Cuál es el valor de la matrícula?"

### ESC-091 · Fuera de dominio `@alterno`
- **Dado** `EST-004` · **Cuando** pregunta "¿Cuál es la capital de Francia?"

### ESC-092 · Dato institucional inexistente `@alterno`
- **Dado** `EST-002` · **Cuando** pregunta "¿Cuál es el correo del rector?"

### ESC-093 · Periodo no disponible `@alterno`
- **Dado** `EST-001` · **Cuando** pregunta "¿Cuál fue mi promedio en el periodo 2025-1?" · **Entonces** indica que solo dispone de los datos del periodo 2026-2 (variante permitida de COMP-03, sin inventar valores).

### ESC-094 · Pregunta trivial o sin contenido `@alterno`
- **Dado** `EST-001` · **Cuando** pregunta "???" · **Entonces** 200 con el texto de COMP-03 (no es 400: la pregunta no está vacía).
