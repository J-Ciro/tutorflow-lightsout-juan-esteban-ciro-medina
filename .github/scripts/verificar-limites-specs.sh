#!/bin/sh
set -eu

RAIZ="${1:-.}"
FALLOS=0

falla() {
  echo "LIMITE EXCEDIDO: $1" >&2
  FALLOS=$((FALLOS + 1))
}

lineas() {
  awk 'END { print NR }' "$1"
}

limite_lineas() {
  archivo="$1"
  maximo="$2"
  [ -f "$archivo" ] || return 0
  total="$(lineas "$archivo")"
  if [ "$total" -gt "$maximo" ]; then
    falla "$archivo tiene $total lineas y el maximo es $maximo"
  fi
}

revisar_awk() {
  salida="$(LC_ALL=C awk -v archivo="$1" -v modo="$2" '
    { sub(/\r$/, "") }
    modo == "decisiones" {
      inicio = ($0 ~ /^[ \t]*([-*][ \t]+|#+[ \t]+)?[*_]*D-[0-9]/)
      corte = ($0 ~ /^[ \t]*$/ || $0 ~ /^[ \t]*#+[ \t]/)
      if (inicio || corte) {
        if (id != "" && n > 3) print archivo ":" desde ": la decision " id " ocupa " n " lineas y el maximo es 3"
        id = ""; n = 0
      }
      if (inicio) {
        id = $0
        sub(/^[ \t]*([-*][ \t]+|#+[ \t]+)?[*_]*/, "", id)
        sub(/[^A-Za-z0-9-].*$/, "", id)
        desde = NR
      }
      if (id != "") n++
    }
    {
      t = $0
      gsub(/[\200-\277]/, "", t)
      largo = length(t)
      if (largo > 400) print archivo ":" NR ": linea de " largo " caracteres y el maximo es 400"
    }
    modo == "spec" {
      if ($0 ~ /~~/) print archivo ":" NR ": texto tachado (~~)"
      if ($0 ~ /v1\.[0-9]/) print archivo ":" NR ": referencia de version (v1.N)"
      if ($0 ~ /^[ \t]*```/) {
        if (!dentro) {
          dentro = 1
          if ($0 !~ /^[ \t]*```gherkin/) print archivo ":" NR ": bloque de codigo cercado distinto de gherkin"
        } else {
          dentro = 0
        }
      }
    }
    END {
      if (modo == "decisiones" && id != "" && n > 3) print archivo ":" desde ": la decision " id " ocupa " n " lineas y el maximo es 3"
    }
  ' "$1")"
  if [ -n "$salida" ]; then
    echo "$salida" | while IFS= read -r linea; do
      echo "LIMITE EXCEDIDO: $linea" >&2
    done
    FALLOS=$((FALLOS + $(echo "$salida" | wc -l)))
  fi
}

limite_lineas "$RAIZ/SPEC.md" 60
limite_lineas "$RAIZ/constitution.md" 150

for dir in "$RAIZ"/specs/*/; do
  [ -d "$dir" ] || continue
  nombre="$(basename "$dir")"
  [ "$nombre" = "_plantilla" ] && continue
  limite_lineas "${dir}spec.md" 350
  limite_lineas "${dir}plan.md" 700
  [ -f "${dir}spec.md" ] && revisar_awk "${dir}spec.md" spec
  [ -f "${dir}plan.md" ] && revisar_awk "${dir}plan.md" plan
  [ -f "${dir}decisiones.md" ] && revisar_awk "${dir}decisiones.md" decisiones
done

if [ "$FALLOS" -gt 0 ]; then
  echo "$FALLOS violacion(es) de los limites de tamano de la documentacion" >&2
  exit 1
fi
echo "Limites de documentacion respetados"
