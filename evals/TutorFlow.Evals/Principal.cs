using System.Text.Json;

namespace TutorFlow.Evals;

public static class Principal
{
    private const string UrlPorDefecto = "http://localhost:8080";
    private const int PausaPorDefectoMs = 1100;
    private const int UmbralP95PorDefectoMs = 500;
    private const int SalidaFallos = 1;
    private const int SalidaSinCasos = 2;

    public static async Task<int> Main(string[] args)
    {
        var opciones = LeerOpciones(args);
        var casos = CargarCasos(opciones);
        if (casos.Count == 0)
        {
            Console.Error.WriteLine("No se encontraron casos de eval.");
            return SalidaSinCasos;
        }

        var resultados = await Ejecutar(opciones, casos);
        var reporte = new Reporte(resultados, opciones);
        reporte.Imprimir(Console.Out);
        return reporte.Exitoso ? 0 : SalidaFallos;
    }

    private static OpcionesEvals LeerOpciones(string[] args)
    {
        var valores = ParesDeArgumentos(args);
        return new OpcionesEvals(
            valores.GetValueOrDefault("--url") ?? UrlPorDefecto,
            args.Contains("--offline"),
            valores.GetValueOrDefault("--casos", Path.Combine(Pruebas.Soporte.DatosReferencia.RutaRepositorio(), "evals", "casos")),
            int.Parse(valores.GetValueOrDefault("--corridas", "1"), System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(valores.GetValueOrDefault("--pausa-ms", PausaPorDefectoMs.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(valores.GetValueOrDefault("--umbral-p95-ms", UmbralP95PorDefectoMs.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.CultureInfo.InvariantCulture),
            valores.GetValueOrDefault("--filtro"));
    }

    private static Dictionary<string, string> ParesDeArgumentos(string[] args) =>
        args.Select((valor, indice) => (valor, indice))
            .Where(x => x.valor.StartsWith("--", StringComparison.Ordinal) && x.indice + 1 < args.Length && !args[x.indice + 1].StartsWith("--", StringComparison.Ordinal))
            .ToDictionary(x => x.valor, x => args[x.indice + 1]);

    private static List<CasoEval> CargarCasos(OpcionesEvals opciones) =>
        Directory.GetFiles(opciones.RutaCasos, "*.json")
            .Order(StringComparer.Ordinal)
            .SelectMany(ruta => JsonSerializer.Deserialize<List<CasoEval>>(File.ReadAllText(ruta), Json.Opciones) ?? [])
            .Where(c => opciones.Filtro is null || c.Id.StartsWith(opciones.Filtro, StringComparison.Ordinal))
            .ToList();

    private static async Task<List<ResultadoCaso>> Ejecutar(OpcionesEvals opciones, List<CasoEval> casos)
    {
        using var cliente = new HttpClient { BaseAddress = new Uri(opciones.Url), Timeout = TimeSpan.FromSeconds(60) };
        IEjecutor ejecutor = opciones.Offline ? new EjecutorEnProceso() : new EjecutorHttp(cliente, opciones.PausaMs);
        var llave = Environment.GetEnvironmentVariable("LLM_API_KEY");
        var resultados = new List<ResultadoCaso>();
        for (var corrida = 1; corrida <= opciones.Corridas; corrida++)
        {
            foreach (var caso in casos)
            {
                resultados.Add(await EjecutarCaso(ejecutor, caso, corrida, llave));
            }
        }

        return resultados;
    }

    private static async Task<ResultadoCaso> EjecutarCaso(IEjecutor ejecutor, CasoEval caso, int corrida, string? llave)
    {
        try
        {
            var ejecucion = await ejecutor.EjecutarAsync(caso, CancellationToken.None);
            return ejecucion is null
                ? new ResultadoCaso(caso, corrida, [], null)
                : new ResultadoCaso(caso, corrida, Evaluador.Evaluar(caso, ejecucion, llave), ejecucion);
        }
        catch (HttpRequestException error)
        {
            return new ResultadoCaso(caso, corrida, [$"error de red: {error.Message}"], new Ejecucion(0, string.Empty, 0));
        }
    }
}
