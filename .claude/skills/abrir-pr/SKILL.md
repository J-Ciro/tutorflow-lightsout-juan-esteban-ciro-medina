---
name: abrir-pr
description: Publica una rama y prepara su pull request hacia develop (o de develop hacia main en la entrega) con título en Conventional Commits y descripción completa con la lista de commits, generando el enlace de GitHub ya lleno. Úsala al terminar una rama de trabajo o al cerrar implementar-spec.
---

# Abrir un pull request

Entrada: la rama a integrar (por defecto, la rama actual) y la base (`develop`; `main` solo para la entrega).

1. **Verifica la rama:** sin cambios pendientes, todos los commits cumplen `.githooks/commit-msg` y la base está actualizada (`git fetch origin`).
2. **Publica la rama:** `git push -u origin <rama>`. Nunca `--force`.
3. **Título:** Conventional Commits en inglés, 60 caracteres como máximo, que resuma la rama completa (p. ej. `feat(001): academic assistant backend`). Nunca el nombre de la rama tal cual.
4. **Descripción:** llena `.github/pull_request_template.md` con datos reales: spec, CA/ESC cubiertos, salida real de build, pruebas, evals y Postman, veredictos de `seguridad` y `qa-calidad`, intervenciones (INT-xxx) y la lista de commits (`git log --format='- %h %s' <base>..<rama>`). Nada inventado: si un dato no se midió, escribe "pendiente".
5. **Enlace:** genera la URL de comparación con el título y la descripción codificados (`encodeURIComponent`):
   `https://github.com/J-Ciro/tutorflow-lightsout-juan-esteban-ciro-medina/compare/<base>...<rama>?expand=1&title=<título>&body=<descripción>`
   Si la URL pasa de 8.000 caracteres, deja en la URL solo el título y entrega la descripción aparte para pegarla.
6. **Entrega al humano** el enlace y la descripción. El humano revisa y hace el merge en GitHub con **"Create a merge commit"**: nunca *Squash* ni *Rebase* (el PDF prohíbe aplastar el historial). Después, `git switch develop && git pull`.
