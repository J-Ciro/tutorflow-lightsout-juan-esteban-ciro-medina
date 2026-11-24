namespace TutorFlow.Core.Asistente;

public enum RolMensaje
{
    Usuario,
    Asistente,
    Herramienta
}

public sealed record ParametroHerramienta(string Nombre, string Descripcion, bool Requerido);

public sealed record HerramientaLlm(string Nombre, string Descripcion, IReadOnlyList<ParametroHerramienta> Parametros);

public sealed class DatoOpacoDelProveedor(object valor)
{
    public object Valor { get; } = valor;

    public override string ToString() => nameof(DatoOpacoDelProveedor);

    public override bool Equals(object? obj) => obj is DatoOpacoDelProveedor;

    public override int GetHashCode() => 0;
}

public sealed record LlamadaHerramienta(string Id, string Nombre, string ArgumentosJson, DatoOpacoDelProveedor? DatoOpaco = null)
{
    public bool Equals(LlamadaHerramienta? otra) =>
        otra is not null && Id == otra.Id && Nombre == otra.Nombre && ArgumentosJson == otra.ArgumentosJson;

    public override int GetHashCode() => HashCode.Combine(Id, Nombre, ArgumentosJson);

    private bool PrintMembers(System.Text.StringBuilder constructor)
    {
        constructor.Append($"Id = {Id}, Nombre = {Nombre}, ArgumentosJson = {ArgumentosJson}");
        return true;
    }
}

public sealed record MensajeLlm(
    RolMensaje Rol,
    string Contenido,
    string? IdLlamada,
    IReadOnlyList<LlamadaHerramienta> Llamadas);

public sealed record SolicitudLlm(
    string PromptSistema,
    IReadOnlyList<MensajeLlm> Mensajes,
    IReadOnlyList<HerramientaLlm> Herramientas,
    int MaximoTokensSalida,
    decimal Temperatura = 0m);

public sealed record AjustesDelModelo(decimal Temperatura = 0m);

public sealed record ContextoTruncadoDetectado(int TokensEstimados, int TokensReportados);

public sealed record UsoTokens(int Entrada, int Salida)
{
    public int Total => Entrada + Salida;
}

public sealed record RespuestaLlm(
    string? Texto,
    IReadOnlyList<LlamadaHerramienta> Llamadas,
    UsoTokens Uso,
    bool Rechazada,
    ContextoTruncadoDetectado? ContextoTruncado = null);

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
