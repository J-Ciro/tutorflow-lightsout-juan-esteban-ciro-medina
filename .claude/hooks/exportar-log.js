const fs = require("fs");
const path = require("path");
const os = require("os");

const REDACCIONES = [
  [/[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g, "[correo-redactado]"],
  [/sk-[A-Za-z0-9_-]{16,}/g, "[llave-redactada]"],
  [/(LLM_API_KEY["'=:\s]+)[^\s"',}]+/g, "$1[llave-redactada]"],
];

function raizProyecto() {
  return process.env.CLAUDE_PROJECT_DIR || process.cwd();
}

function terminosLocales(raiz) {
  const archivo = path.join(raiz, ".claude", "redacciones.local.txt");
  if (!fs.existsSync(archivo)) return [];
  return fs.readFileSync(archivo, "utf8").split(/\r?\n/).map((t) => t.trim()).filter(Boolean);
}

function redactar(texto, terminos) {
  const base = REDACCIONES.reduce((acc, [patron, reemplazo]) => acc.replace(patron, reemplazo), texto);
  return terminos.reduce((acc, termino) => acc.split(termino).join("[redactado]"), base);
}

function copiarRedactado(origen, destino, terminos) {
  fs.mkdirSync(path.dirname(destino), { recursive: true });
  fs.writeFileSync(destino, redactar(fs.readFileSync(origen, "utf8"), terminos));
}

function exportarSesion(transcripcion, raiz, terminos) {
  if (!transcripcion || !fs.existsSync(transcripcion)) return 0;
  const idSesion = path.basename(transcripcion, ".jsonl");
  const destino = path.join(raiz, "logs", "sesiones", idSesion);
  copiarRedactado(transcripcion, path.join(destino, "sesion.jsonl"), terminos);
  const carpetaSubagentes = path.join(path.dirname(transcripcion), idSesion, "subagents");
  if (!fs.existsSync(carpetaSubagentes)) return 1;
  const subagentes = fs.readdirSync(carpetaSubagentes).filter((f) => f.endsWith(".jsonl"));
  subagentes.forEach((f) => copiarRedactado(path.join(carpetaSubagentes, f), path.join(destino, "subagentes", f), terminos));
  return 1 + subagentes.length;
}

function carpetaTranscripciones(raiz) {
  const codificada = path.resolve(raiz).replace(/[^A-Za-z0-9]/g, "-");
  return path.join(os.homedir(), ".claude", "projects", codificada);
}

function exportarTodo(raiz, terminos) {
  const carpeta = carpetaTranscripciones(raiz);
  if (!fs.existsSync(carpeta)) return 0;
  return fs.readdirSync(carpeta)
    .filter((f) => f.endsWith(".jsonl"))
    .reduce((total, f) => total + exportarSesion(path.join(carpeta, f), raiz, terminos), 0);
}

function exportarDesdeHook(entrada, raiz, terminos) {
  const { transcript_path } = JSON.parse(entrada);
  return exportarSesion(transcript_path, raiz, terminos);
}

const raiz = raizProyecto();
const terminos = terminosLocales(raiz);

if (process.argv.includes("--todo")) {
  process.stdout.write(`exportar-log: ${exportarTodo(raiz, terminos)} transcripciones exportadas\n`);
} else {
  let entrada = "";
  process.stdin.on("data", (c) => (entrada += c));
  process.stdin.on("end", () => {
    try {
      exportarDesdeHook(entrada, raiz, terminos);
    } catch (e) {
      process.stderr.write(`exportar-log: ${e.message}\n`);
    }
  });
}
