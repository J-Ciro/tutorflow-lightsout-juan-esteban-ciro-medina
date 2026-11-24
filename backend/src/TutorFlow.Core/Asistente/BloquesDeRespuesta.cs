using System.Text.RegularExpressions;

namespace TutorFlow.Core.Asistente;

internal sealed record BloqueDeTexto(string Texto, int InicioDeLaOracion);

internal enum TipoDeLinea
{
    Vacia,
    Separadora,
    Fila,
    Item,
    Encabezado,
    Texto
}

internal sealed record LineaPreparada(string Texto, TipoDeLinea Tipo);

internal static partial class BloquesDeRespuesta
{
    public static IReadOnlyList<BloqueDeTexto> De(string respuesta)
    {
        var estado = new EstadoDeBloques();
        return [.. SaltoDeLinea().Split(ComaDecimal.APunto(respuesta)).Select(Preparar).SelectMany(estado.Procesar)];
    }

    internal static LineaPreparada Preparar(string linea)
    {
        var sinCita = linea.Trim().TrimStart('>').Trim();
        var eraNegrita = Negrita().IsMatch(sinCita);
        var texto = sinCita.Replace("**", string.Empty, StringComparison.Ordinal).Replace("__", string.Empty, StringComparison.Ordinal).Trim();
        return new LineaPreparada(texto, Clasificar(texto, eraNegrita));
    }

    private static TipoDeLinea Clasificar(string texto, bool eraNegrita)
    {
        if (texto.Length == 0)
        {
            return TipoDeLinea.Vacia;
        }

        return texto.StartsWith('|') ? DeTabla(texto) : ItemDeLista().IsMatch(texto) ? TipoDeLinea.Item : EsEncabezado(texto, eraNegrita) ? TipoDeLinea.Encabezado : TipoDeLinea.Texto;
    }

    private static TipoDeLinea DeTabla(string texto) =>
        Separador().IsMatch(texto) ? TipoDeLinea.Separadora : TipoDeLinea.Fila;

    private static bool EsEncabezado(string texto, bool eraNegrita) =>
        texto.EndsWith(':') || eraNegrita || texto.StartsWith('#');

    internal static IEnumerable<BloqueDeTexto> Oraciones(string linea, string contexto)
    {
        var prefijo = contexto.Length == 0 ? string.Empty : contexto + " ";
        return CorteDeOracion().Split(linea)
            .Where(oracion => !string.IsNullOrWhiteSpace(oracion))
            .Select(oracion => new BloqueDeTexto(prefijo + oracion, prefijo.Length));
    }

    [GeneratedRegex(@"\r?\n", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SaltoDeLinea();

    [GeneratedRegex(@"^(\*\*|__).+(\*\*|__):?$", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Negrita();

    [GeneratedRegex(@"^[|\-:\s]+$", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Separador();

    [GeneratedRegex(@"^(?:[-*•]|\d+[.)])\s", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ItemDeLista();

    [GeneratedRegex(@"(?<=[.!?;])(?=\s|$)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CorteDeOracion();
}

internal sealed class EstadoDeBloques
{
    private string? _encabezado;
    private string? _textoPrevio;
    private string? _contextoDeLista;
    private string? _encabezadoDeColumnas;
    private bool _ultimaFueVacia;

    public IEnumerable<BloqueDeTexto> Procesar(LineaPreparada linea)
    {
        var bloques = DeLaLinea(linea);
        RecordarLaLinea(linea);
        return bloques;
    }

    private List<BloqueDeTexto> DeLaLinea(LineaPreparada linea) => linea.Tipo switch
    {
        TipoDeLinea.Vacia or TipoDeLinea.Separadora => [],
        TipoDeLinea.Fila => DeFila(linea),
        TipoDeLinea.Item => DeItem(linea),
        TipoDeLinea.Encabezado => DeEncabezado(linea),
        _ => DeTexto(linea)
    };

    private void RecordarLaLinea(LineaPreparada linea)
    {
        _ultimaFueVacia = linea.Tipo == TipoDeLinea.Vacia || (linea.Tipo == TipoDeLinea.Separadora && _ultimaFueVacia);
        _encabezadoDeColumnas = EsDeTabla(linea) ? _encabezadoDeColumnas : null;
        _textoPrevio = linea.Tipo == TipoDeLinea.Texto ? linea.Texto : null;
    }

    private static bool EsDeTabla(LineaPreparada linea) => linea.Tipo is TipoDeLinea.Fila or TipoDeLinea.Separadora;

    private List<BloqueDeTexto> DeFila(LineaPreparada linea)
    {
        var contexto = _encabezadoDeColumnas is null ? string.Empty : $"{_encabezadoDeColumnas} {_encabezado}".Trim();
        _encabezadoDeColumnas ??= linea.Texto;
        return [.. BloquesDeRespuesta.Oraciones(linea.Texto, contexto)];
    }

    private List<BloqueDeTexto> DeItem(LineaPreparada linea)
    {
        _contextoDeLista = _textoPrevio ?? _contextoDeLista;
        return [.. BloquesDeRespuesta.Oraciones(linea.Texto, _encabezado ?? _contextoDeLista ?? string.Empty)];
    }

    private List<BloqueDeTexto> DeEncabezado(LineaPreparada linea)
    {
        _encabezado = linea.Texto;
        _contextoDeLista = null;
        return [.. BloquesDeRespuesta.Oraciones(linea.Texto, string.Empty)];
    }

    private List<BloqueDeTexto> DeTexto(LineaPreparada linea)
    {
        _encabezado = _ultimaFueVacia ? null : _encabezado;
        _contextoDeLista = null;
        return [.. BloquesDeRespuesta.Oraciones(linea.Texto, _encabezado ?? string.Empty)];
    }
}
