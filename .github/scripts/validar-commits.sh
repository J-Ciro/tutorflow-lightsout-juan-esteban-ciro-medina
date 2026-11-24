#!/bin/bash
set -euo pipefail

RANGO="$1"
HOOK="$(mktemp)"
cp .githooks/commit-msg "$HOOK"
ORIGINAL="$(git rev-parse HEAD)"
FALLOS=0

for COMMIT in $(git rev-list --reverse "$RANGO"); do
  PADRE="$(git rev-parse "$COMMIT^")"
  git update-ref --no-deref HEAD "$PADRE"
  git read-tree "$COMMIT"
  git log -1 --format=%B "$COMMIT" > "$HOOK.msg"
  if ! sh "$HOOK" "$HOOK.msg"; then
    echo "Commit rechazado: $COMMIT" >&2
    FALLOS=$((FALLOS + 1))
  fi
done

git update-ref --no-deref HEAD "$ORIGINAL"
git read-tree "$ORIGINAL"
rm -f "$HOOK" "$HOOK.msg"

if [ "$FALLOS" -gt 0 ]; then
  echo "$FALLOS commit(s) incumplen .githooks/commit-msg" >&2
  exit 1
fi
