namespace TutorFlow.Core.Asistente;

public enum RolMensaje
{
    Usuario,
    Asistente,
    Herramienta
}

public sealed record ParametroHerramienta(string Nombre, string Descripcion, bool Requerido);

public sealed record HerramientaLlm(string Nombre, string Descripcion, IReadOnlyList<ParametroHerramienta> Parametros);

public sealed record LlamadaHerramienta(string Id, string Nombre, string ArgumentosJson);

public sealed record MensajeLlm(
    RolMensaje Rol,
    string Contenido,
    string? IdLlamada,
    IReadOnlyList<LlamadaHerramienta> Llamadas);

public sealed record SolicitudLlm(
    string PromptSistema,
    IReadOnlyList<MensajeLlm> Mensajes,
    IReadOnlyList<HerramientaLlm> Herramientas,
    int MaximoTokensSalida);

public sealed record UsoTokens(int Entrada, int Salida)
{
    public int Total => Entrada + Salida;
}

public sealed record RespuestaLlm(
    string? Texto,
    IReadOnlyList<LlamadaHerramienta> Llamadas,
    UsoTokens Uso,
    bool Rechazada);

public sealed record PrecioModelo(decimal EntradaPorMillon, decimal SalidaPorMillon);

public static class CalculadoraCosto
{
    public const int TokensPorMillon = 1_000_000;
    public const int DecimalesCosto = 6;

    public static decimal Calcular(UsoTokens uso, PrecioModelo? precio)
    {
        if (precio is null)
        {
            return 0m;
        }

        var costo = (uso.Entrada * precio.EntradaPorMillon + uso.Salida * precio.SalidaPorMillon) / TokensPorMillon;
        return Math.Round(costo, DecimalesCosto, MidpointRounding.AwayFromZero);
    }
}
