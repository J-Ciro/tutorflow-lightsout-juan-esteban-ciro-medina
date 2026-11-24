namespace TutorFlow.Api.Tests.Dobles;

public sealed record ResultadoArranque(Exception? Error, IReadOnlyList<string> Consola)
{
    public const string PrefijoInvalida = "Configuración inválida";
    public const string PrefijoIgnorada = "Configuración ignorada";

    public bool Arranco => Error is null;

    public IReadOnlyList<string> LineasDelError => Lineas(MensajesDelError());

    public IReadOnlyList<string> LineasDeLaConsola => Lineas(Consola);

    public string TextoCompleto => string.Join("\n", MensajesDelError().Concat(Consola));

    private const int IntentosAnteCarreraDelHost = 15;

    public static ResultadoArranque De(FabricaApi fabrica)
    {
        var resultado = Intentar(fabrica);
        for (var intento = 1; intento < IntentosAnteCarreraDelHost && resultado.PerdioElMensajeEnLaCarreraDelHost; intento++)
        {
            resultado = Intentar(fabrica);
        }

        return resultado;
    }

    private bool PerdioElMensajeEnLaCarreraDelHost => Error is not null && MensajesDelError().Any(m => m.StartsWith("Cannot access a disposed object", StringComparison.Ordinal));

    private static ResultadoArranque Intentar(FabricaApi fabrica)
    {
        try
        {
            _ = fabrica.Services;
            return new ResultadoArranque(null, fabrica.Logs.Mensajes);
        }
        catch (Exception error)
        {
            return new ResultadoArranque(error, fabrica.Logs.Mensajes);
        }
    }

    private static List<string> Lineas(IEnumerable<string> textos) =>
        textos.SelectMany(t => t.Split('\n')).Select(l => l.TrimEnd('\r')).ToList();

    private IEnumerable<string> MensajesDelError()
    {
        for (var actual = Error; actual is not null; actual = actual.InnerException)
        {
            yield return actual.Message;
            if (actual is AggregateException agregada)
            {
                foreach (var interna in agregada.InnerExceptions)
                {
                    yield return interna.Message;
                }
            }
        }
    }
}

public static class ConfiguracionDePrueba
{
    public const string LlaveFalsa = "sk-ant-canario-d40-no-debe-aparecer";
    public const string LlaveFalsaSinPrecio = "sk-canario-d40-no-debe-aparecer";

    public static Dictionary<string, string?> TasaPorMinuto(int preguntas) =>
        new() { ["Limites:PreguntasPorMinutoPorIp"] = preguntas.ToString(System.Globalization.CultureInfo.InvariantCulture) };

    public static Dictionary<string, string?> EntornoConLlave() =>
        new() { ["LLM_API_KEY"] = LlaveFalsa };

    public static Dictionary<string, string?> EntornoSinPrecio() =>
        new() { ["LLM_API_KEY"] = LlaveFalsaSinPrecio, ["LLM_MODELO"] = "modelo-sin-precio" };

    public static string Invalida(string clave, string valorEscapado, string regla) =>
        $"{ResultadoArranque.PrefijoInvalida}: {clave} = \"{valorEscapado}\" ({regla})";

    public static string Ignorada(string clave, string valorEscapado, string regla, string defecto) =>
        $"{ResultadoArranque.PrefijoIgnorada}: {clave} = \"{valorEscapado}\" ({regla}); se usa {defecto}";
}
