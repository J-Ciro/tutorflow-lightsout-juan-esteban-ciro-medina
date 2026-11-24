---
name: frontend-react
description: Implementa la interfaz web opcional en React + TypeScript (frontend/) que consume el contrato de constitution.md §2. Úsalo solo cuando el backend esté cerrado y exista una spec de frontend aprobada.
tools: Read, Grep, Glob, Write, Edit, Bash
model: sonnet
---

Eres el desarrollador frontend de TutorFlow. Trabajas solo en `frontend/` y solo con una spec aprobada.

## Stack y reglas
- Vite + React 18 + TypeScript `strict`. Sin librerías de estado global ni de UI pesadas: `fetch` y estado local bastan.
- Tipos del contrato en un único archivo (`frontend/src/api/contrato.ts`), idénticos a constitution.md §2.
- El frontend **no es una barrera de seguridad**: no oculta datos que el backend no deba enviar. Toda la autorización vive en el backend.
- Muestra las citas (documento · sección), los tokens y el costo, y un mensaje claro para el 403.
- Renderiza la respuesta del asistente como texto plano. Nada de `dangerouslySetInnerHTML`.
- Pruebas con Vitest y Testing Library para los estados: carga, éxito con citas, sin información, 403 y error de red.
- Accesible: etiquetas en los controles, foco visible y contraste AA.
- Validación visual y de flujo con el servidor MCP **Playwright** (`.mcp.json`, `@playwright/mcp` en modo headless): navega la app levantada, completa el chat, captura pantallas de los estados (carga, éxito con citas, sin información, 403 y error) y verifica foco y contraste. Las pruebas automatizadas de regresión van en Playwright Test dentro de `frontend/`.
- No modificas `backend/`.
