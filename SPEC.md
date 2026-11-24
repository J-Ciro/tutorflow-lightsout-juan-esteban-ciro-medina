# SPEC.md · TutorFlow

| Campo | Valor |
| --- | --- |
| Versión | 2.0 · 2026-11-23 (reestructuración INT-023; reglas sin cambios respecto de la 1.12) |
| Reglas globales | [`constitution.md`](constitution.md): principios, contrato de la API, matriz de acceso y arquitectura |
| Reglas de los agentes | [`AGENTS.md`](AGENTS.md) y [`FABRICA.md`](FABRICA.md) |

## Objetivo

**Qué:** TutorFlow es un asistente académico que responde preguntas de estudiantes y coordinadores de la Corporación Educativa Horizonte (ficticia) con el reglamento vigente y los datos académicos del periodo 2026-2.

**Por qué:** hoy un estudiante lee cuatro documentos y hace sus propios cálculos para saber su promedio, si perdió una asignatura por faltas o si puede aplicar a una beca. TutorFlow lo resuelve en una pregunta, con la regla citada y sin exponer datos de otras personas (Ley 1581, Art. 22 del reglamento).

**Cómo:** el código calcula y autoriza de forma determinista; un LLM solo redacta a partir de las secciones del reglamento y de los datos ya autorizados y calculados.

## Actores

| Rol | Identificador | Puede ver |
| --- | --- | --- |
| `estudiante` | `EST-NNN` | Sus propios datos académicos y el reglamento |
| `coordinador` | `COORD-NN` | Los datos de los estudiantes de sus programas y el reglamento |

La matriz de acceso completa está en [`constitution.md`](constitution.md) §3.

## Features

| ID | Feature | Estado | Spec | Plan | Decisiones | Escenarios |
| --- | --- | --- | --- | --- | --- | --- |
| 001 | Asistente académico (PDF Q10) | Aprobada (spec 2.2) | [spec.md](specs/001-asistente-academico/spec.md) | [plan.md](specs/001-asistente-academico/plan.md) | [decisiones.md](specs/001-asistente-academico/decisiones.md) | [escenarios/001-asistente-academico/](escenarios/001-asistente-academico/) |

Una feature nueva agrega su fila con los cuatro enlaces. Sus decisiones van en su `decisiones.md` (`D-NNN-xx`); una regla que afecte a todo el sistema se propone para `constitution.md` con aprobación humana.
