using System.Globalization;
using System.Text.RegularExpressions;

namespace TutorFlow.Evals;

public static partial class TablaFaseV
{
    private const string NoAplica = "n/a";
    private const string SinFallos = "Sin corridas fallidas.";
    private const int TiempoLimiteMs = 1000;

    public static void Imprimir(IReadOnlyList<ResultadoCaso> resultados, EntornoFaseV entorno, TextWriter salida)
    {
        salida.WriteLine();
        salida.WriteLine("Tabla de la fase V (ESC-197 f), para copiar a REPORTE.md:");
        salida.WriteLine();
        ImprimirRegistro(entorno, salida);
        salida.WriteLine();
        ImprimirAciertos(resultados, salida);
        salida.WriteLine();
        ImprimirFallidas(resultados, salida);
    }

    public static string Pregunta(CasoEval caso)
    {
        var coincidencia = NumeroDePregunta().Match(caso.Fuente);
        return coincidencia.Success ? $"P{coincidencia.Groups["n"].Value}" : caso.Id;
    }

    private static void ImprimirRegistro(EntornoFaseV entorno, TextWriter salida)
    {
        salida.WriteLine("| Fecha UTC | Commit | Proveedor | Modelo | Llm:Temperatura | Llm:Semilla | OLLAMA_CONTEXT_LENGTH |");
        salida.WriteLine("| --- | --- | --- | --- | --- | --- | --- |");
        salida.WriteLine($"| {entorno.FechaUtc} | {entorno.Commit} | {entorno.Proveedor} | {entorno.Modelo} | {entorno.Temperatura} | {entorno.Semilla} | {entorno.ContextoDeOllama} |");
    }

    private static void ImprimirAciertos(IReadOnlyList<ResultadoCaso> resultados, TextWriter salida)
    {
        salida.WriteLine($"| Pregunta | {string.Join(" | ", Criterios.DeLaTabla)} | Tokens | costoUsd |");
        salida.WriteLine($"| --- | {string.Join(" | ", Criterios.DeLaTabla.Select(_ => "---"))} | --- | --- |");
        foreach (var grupo in resultados.Where(r => !r.Omitido).GroupBy(r => r.Caso.Id))
        {
            salida.WriteLine(Fila(grupo.ToList()));
        }
    }

    private static string Fila(List<ResultadoCaso> corridas)
    {
        var caso = corridas[0].Caso;
        var aciertos = Criterios.DeLaTabla.Select(criterio => Aciertos(caso, corridas, criterio));
        var consumo = corridas.Select(c => c.Ejecucion is { Estado: 200 } e ? Json.LeerSalida(e.Cuerpo) : null).Where(s => s is not null).ToList();
        var tokens = consumo.Sum(s => s!.Tokens);
        var costo = consumo.Sum(s => s!.CostoUsd);
        return $"| {Pregunta(caso)} | {string.Join(" | ", aciertos)} | {tokens} | {costo.ToString("F6", CultureInfo.InvariantCulture)} |";
    }

    private static string Aciertos(CasoEval caso, List<ResultadoCaso> corridas, string criterio) =>
        Criterios.Aplica(caso, criterio)
            ? $"{corridas.Count(c => !c.Fallos.Any(f => Criterios.De(f) == criterio))}/{corridas.Count}"
            : NoAplica;

    private static void ImprimirFallidas(IReadOnlyList<ResultadoCaso> resultados, TextWriter salida)
    {
        var fallidas = resultados.Where(r => !r.Omitido && r.Fallos.Count > 0).ToList();
        salida.WriteLine(fallidas.Count == 0 ? SinFallos : "Texto real de cada corrida fallida:");
        foreach (var fallida in fallidas)
        {
            ImprimirFallida(fallida, salida);
        }
    }

    private static void ImprimirFallida(ResultadoCaso fallida, TextWriter salida)
    {
        var criterios = string.Join(", ", fallida.Fallos.Select(Criterios.De).Distinct());
        salida.WriteLine();
        salida.WriteLine($"- {Pregunta(fallida.Caso)}, corrida {fallida.Corrida}, criterio: {criterios}");
        foreach (var fallo in fallida.Fallos)
        {
            salida.WriteLine($"  - {fallo}");
        }

        salida.WriteLine("  ```text");
        salida.WriteLine(TextoReal(fallida));
        salida.WriteLine("  ```");
    }

    private static string TextoReal(ResultadoCaso corrida)
    {
        var cuerpo = corrida.Ejecucion?.Cuerpo ?? string.Empty;
        return Json.LeerSalida(cuerpo)?.Respuesta ?? cuerpo;
    }

    [GeneratedRegex(@"pregunta (?<n>\d+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteMs)]
    private static partial Regex NumeroDePregunta();
}
