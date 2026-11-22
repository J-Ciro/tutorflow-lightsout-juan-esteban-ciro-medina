const fs = require("fs");
const path = require("path");

const REDACCIONES = [
  [/[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g, "[correo-redactado]"],
  [/sk-[A-Za-z0-9_-]{16,}/g, "[llave-redactada]"],
  [/(LLM_API_KEY["'=:\s]+)[^\s"',}]+/g, "$1[llave-redactada]"],
];

function terminosLocales(raiz) {
  const archivo = path.join(raiz, ".claude", "redacciones.local.txt");
  if (!fs.existsSync(archivo)) return [];
  return fs.readFileSync(archivo, "utf8").split(/\r?\n/).map((t) => t.trim()).filter(Boolean);
}

function redactar(texto, raiz) {
  const base = REDACCIONES.reduce((acc, [patron, reemplazo]) => acc.replace(patron, reemplazo), texto);
  return terminosLocales(raiz).reduce((acc, termino) => acc.split(termino).join("[redactado]"), base);
}

let entrada = "";
process.stdin.on("data", (c) => (entrada += c));
process.stdin.on("end", () => {
  try {
    const { session_id, transcript_path } = JSON.parse(entrada);
    if (!transcript_path || !fs.existsSync(transcript_path)) return;
    const raiz = process.env.CLAUDE_PROJECT_DIR || process.cwd();
    const destinoDir = path.join(raiz, "logs", "sesiones");
    fs.mkdirSync(destinoDir, { recursive: true });
    const fecha = new Date().toISOString().slice(0, 10);
    const destino = path.join(destinoDir, `${fecha}_${session_id}.jsonl`);
    fs.writeFileSync(destino, redactar(fs.readFileSync(transcript_path, "utf8"), raiz));
  } catch (e) {
    process.stderr.write(`exportar-log: ${e.message}\n`);
  }
});
