using System.Globalization;
using System.Text.RegularExpressions;
using TutorFlow.Core;

namespace TutorFlow.Api;

internal enum TipoDeValor
{
    Entero,
    Decimal,
    Booleano
}

internal readonly record struct RangoNumerico(decimal Minimo, decimal Maximo, bool MinimoExclusivo = false)
{
    public bool Contiene(decimal numero) =>
        (MinimoExclusivo ? numero > Minimo : numero >= Minimo) && numero <= Maximo;
}

internal sealed partial record ReglaDeConfiguracion(
    string Clave,
    TipoDeValor Tipo,
    RangoNumerico Rango,
    string Texto,
    string? Defecto = null)
{
    internal const decimal MaximoDecimal = 1000m;
    private const decimal MaximaTemperatura = 0.3m;
    private const int SemillaMaxima = int.MaxValue;
    private const int PuntajeMaximo = 100;

    public static IReadOnlyList<ReglaDeConfiguracion> Todas { get; } =
    [
        Entero("Conocimiento:RelevanciaMinima", 1, 10),
        Entero("Conocimiento:ApoyoMinimoRespaldo", 1, 10),
        Entero("Conocimiento:PuntajeRelativoMinimo", 1, PuntajeMaximo),
        Entero("Conocimiento:PuntajeRelativoRelevante", 1, PuntajeMaximo),
        Entero("Llm:Semilla", 0, SemillaMaxima),
        new("Llm:Temperatura", TipoDeValor.Decimal, new RangoNumerico(0m, MaximaTemperatura), "decimal de 0 a 0.3"),
        Entero("Limites:MaximoCaracteresPregunta", 1, 1000),
        Entero("Limites:MaximoBytesCuerpo", 1024, 16384),
        Entero("Limites:PreguntasPorMinutoPorIp", 1, 10000),
        Entero("Limites:MaximoSeccionesRecuperadas", 1, 6),
        Entero("Limites:MaximoTokensSalida", 1, 2000),
        Entero("Limites:MaximoInvocacionesHerramientas", 1, 4),
        Entero("Limites:TimeoutLlmSegundos", 1, 60),
        Entero("Limites:PresupuestoApiAcademicaSegundos", 1, 30),
        new("Limites:PresupuestoDiarioUsd", TipoDeValor.Decimal, new RangoNumerico(0m, MaximoDecimal, true), "decimal mayor que 0 y hasta 1000"),
        Entero("Limites:MaximoIdentidadesPorIp", 1, 100),
        Entero("Limites:VentanaIdentidadesMinutos", 1, 1440),
        Precio("LLM_PRECIO_ENTRADA"),
        Precio("LLM_PRECIO_SALIDA")
    ];

    public static IReadOnlyList<ReglaDeConfiguracion> ConAdvertencia { get; } =
    [
        Reserva("Llm:ReservaPorPeticionUsd", "el estimado de 8000 tokens de entrada y 2000 de salida"),
        Reserva("Llm:ReservaSinPrecioUsd", "0.01")
    ];

    public static ReglaDeConfiguracion Buscar(string clave) =>
        Todas.Concat(ConAdvertencia).First(regla => regla.Clave == clave);

    public static ReglaDeConfiguracion Precio(string clave) =>
        new(clave, TipoDeValor.Decimal, new RangoNumerico(0m, MaximoDecimal), "decimal de 0 a 1000");

    public bool Cumple(string valor) => Tipo switch
    {
        TipoDeValor.Booleano => bool.TryParse(valor, out _) && valor.Trim() == valor,
        TipoDeValor.Entero => FormatoEntero().IsMatch(valor) && EnRango(valor),
        _ => FormatoDecimal().IsMatch(valor) && EnRango(valor)
    };

    public decimal? LeerValido(string? valor) =>
        valor is not null && Tipo != TipoDeValor.Booleano && Cumple(valor) ? Convertir(valor) : null;

    private static ReglaDeConfiguracion Entero(string clave, int minimo, int maximo) =>
        new(clave, TipoDeValor.Entero, new RangoNumerico(minimo, maximo), $"entero de {minimo} a {maximo}");

    private static ReglaDeConfiguracion Reserva(string clave, string defecto) =>
        new(clave, TipoDeValor.Decimal, new RangoNumerico(0m, decimal.MaxValue, true), "decimal mayor que 0", defecto);

    private static decimal Convertir(string valor) =>
        decimal.Parse(valor, NumberStyles.Float, CultureInfo.InvariantCulture);

    private bool EnRango(string valor) =>
        decimal.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var numero) && Rango.Contiene(numero);

    [GeneratedRegex(@"^-?[0-9]+\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex FormatoEntero();

    [GeneratedRegex(@"^-?[0-9]+(\.[0-9]+)?\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex FormatoDecimal();
}
