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

internal sealed partial record ReglaDeConfiguracion(
    string Clave,
    TipoDeValor Tipo,
    decimal Minimo,
    decimal Maximo,
    bool MinimoExclusivo,
    string Texto,
    string? Defecto = null)
{
    private const decimal MaximoPrecio = 1000m;

    public static IReadOnlyList<ReglaDeConfiguracion> Todas { get; } =
    [
        Entero("Conocimiento:RelevanciaMinima", 1, 10),
        Entero("Conocimiento:ApoyoMinimoRespaldo", 1, 10),
        Entero("Limites:MaximoCaracteresPregunta", 1, 1000),
        Entero("Limites:MaximoBytesCuerpo", 1024, 16384),
        Entero("Limites:PreguntasPorMinutoPorIp", 1, 10000),
        Entero("Limites:MaximoSeccionesRecuperadas", 1, 6),
        Entero("Limites:MaximoTokensSalida", 1, 2000),
        Entero("Limites:MaximoInvocacionesHerramientas", 1, 4),
        Entero("Limites:TimeoutLlmSegundos", 1, 60),
        Entero("Limites:PresupuestoApiAcademicaSegundos", 1, 30),
        new("Limites:PresupuestoDiarioUsd", TipoDeValor.Decimal, 0m, MaximoPrecio, true, "decimal mayor que 0 y hasta 1000"),
        Entero("Limites:MaximoIdentidadesPorIp", 1, 100),
        Entero("Limites:VentanaIdentidadesMinutos", 1, 1440),
        new("Limites:BloquearMultiplesIdentidades", TipoDeValor.Booleano, 0m, 0m, false, "true o false"),
        Precio("LLM_PRECIO_ENTRADA"),
        Precio("LLM_PRECIO_SALIDA")
    ];

    public static IReadOnlyList<ReglaDeConfiguracion> ConAdvertencia { get; } =
    [
        Reserva("Llm:ReservaPorPeticionUsd", "el estimado de 8000 tokens de entrada y 2000 de salida"),
        Reserva("Llm:ReservaSinPrecioUsd", "0.01")
    ];

    public static ReglaDeConfiguracion Precio(string clave) =>
        new(clave, TipoDeValor.Decimal, 0m, MaximoPrecio, false, "decimal de 0 a 1000");

    public bool Cumple(string valor) => Tipo switch
    {
        TipoDeValor.Booleano => bool.TryParse(valor, out _) && valor.Trim() == valor,
        TipoDeValor.Entero => FormatoEntero().IsMatch(valor) && EnRango(valor),
        _ => FormatoDecimal().IsMatch(valor) && EnRango(valor)
    };

    private static ReglaDeConfiguracion Entero(string clave, int minimo, int maximo) =>
        new(clave, TipoDeValor.Entero, minimo, maximo, false, $"entero de {minimo} a {maximo}");

    private static ReglaDeConfiguracion Reserva(string clave, string defecto) =>
        new(clave, TipoDeValor.Decimal, 0m, decimal.MaxValue, true, "decimal mayor que 0", defecto);

    private bool EnRango(string valor) =>
        decimal.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var numero)
        && (MinimoExclusivo ? numero > Minimo : numero >= Minimo)
        && numero <= Maximo;

    [GeneratedRegex(@"^-?[0-9]+\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex FormatoEntero();

    [GeneratedRegex(@"^-?[0-9]+(\.[0-9]+)?\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex FormatoDecimal();
}
