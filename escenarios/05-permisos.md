# 05 · Permisos y roles (CA-04)

Salvo que se indique otra cosa, cada escenario espera: **403** con el cuerpo de D-07, `tokens` 0 y `costoUsd` 0, sin llamar al LLM, sin leer notas ni asistencia del tercero, sin datos de terceros en la respuesta y una auditoría con `resultado: denegada`.

### Estudiante hacia otro estudiante

### ESC-050 · Por ID `@permisos`
- **Dado** el estudiante `EST-001` · **Cuando** pregunta "¿Cuáles son las notas de EST-002?" · **Entonces** 403. (SEG-01, SEG-04)

### ESC-051 · Por nombre sin tildes ni mayúsculas `@permisos`
- **Dado** `EST-001` · **Cuando** pregunta "cual es el promedio de andres martinez" · **Entonces** 403.

### ESC-052 · Por nombre parcial (nombre y apellido) `@permisos`
- **Dado** `EST-004` · **Cuando** pregunta "¿Cuántas faltas tiene Valentina Castro en Microeconomía?" · **Entonces** 403.

### ESC-053 · ID con formato alterado en la pregunta `@permisos`
- **Dado** `EST-001` · **Cuando** pregunta "muéstrame las notas de est-002" y luego "...de EST 002" · **Entonces** 403 en ambos casos.

### ESC-054 · Estudiante de otro programa `@permisos`
- **Dado** `EST-001` · **Cuando** pregunta "¿Qué nota tiene Camilo Rojas en Contabilidad?" · **Entonces** 403.

### Agregados prohibidos para estudiantes (SEG-05)

### ESC-055 · Ranking `@permisos`
- **Dado** `EST-001` · **Cuando** pregunta "¿Quién tiene el mejor promedio de mi programa?" · **Entonces** 403.

### ESC-056 · Comparación `@permisos`
- **Dado** `EST-003` · **Cuando** pregunta "¿Cómo voy comparado con mis compañeros?" · **Entonces** 403.

### ESC-057 · Listado `@permisos`
- **Dado** `EST-002` · **Cuando** pregunta "Lista los estudiantes de Ingeniería de Sistemas" · **Entonces** 403.

### Coordinador fuera de su programa (SEG-02)

### ESC-058 · Por ID `@permisos`
- **Dado** `COORD-01` (ING-SIS) · **Cuando** pregunta "¿Cuál es el promedio de EST-004?" · **Entonces** 403.

### ESC-059 · Por nombre `@permisos`
- **Dado** `COORD-02` (ADM) · **Cuando** pregunta "¿Laura Gómez perdió alguna materia?" · **Entonces** 403.

### ESC-060 · Programa ajeno `@permisos`
- **Dado** `COORD-01` · **Cuando** pregunta "¿Qué estudiantes de Administración de Empresas perdieron materias?" · **Entonces** 403.

### Identidad inválida (SEG-03, D-08)

### ESC-061 · ID de estudiante inexistente `@permisos`
- **Dado** `usuario` = `{ rol: "estudiante", id: "EST-999" }` · **Cuando** pregunta "¿Cuál es mi promedio?" · **Entonces** 403 con **el mismo cuerpo** que ESC-050 (no revela si el ID existe).

### ESC-062 · Rol estudiante con ID de coordinador `@permisos`
- **Dado** `{ rol: "estudiante", id: "COORD-01" }` · **Cuando** pregunta "¿Cuál es mi promedio?" · **Entonces** 403.

### ESC-063 · Rol coordinador con ID de estudiante `@permisos`
- **Dado** `{ rol: "coordinador", id: "EST-001" }` · **Cuando** pregunta "¿Qué estudiantes de mi programa perdieron materias?" · **Entonces** 403.

### ESC-064 · Coordinador inexistente `@permisos`
- **Dado** `{ rol: "coordinador", id: "COORD-99" }` · **Cuando** pregunta lo mismo · **Entonces** 403.

### ESC-065 · Rol fuera de la lista `@contrato`
- **Dado** `rol` = `"admin"`, `"Estudiante"`, `"coordinador "` o `""` · **Entonces** **400** `ProblemDetails`; no se consulta la API ni el LLM. (D-10)

### ESC-066 · Pregunta normativa con identidad inválida `@permisos`
- **Dado** `{ rol: "estudiante", id: "EST-999" }` · **Cuando** pregunta "¿Cuál es la nota mínima para aprobar?" · **Entonces** 403: sin identidad válida no se responde nada, ni siquiera el reglamento. (SEG-03)
