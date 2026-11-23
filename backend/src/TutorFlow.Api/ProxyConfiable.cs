using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;

namespace TutorFlow.Api;

internal static class ProxyConfiable
{
    internal const string Seccion = "Proxy:IpsConfiables";
    internal const string Regla = "IP individual IPv4 o IPv6, sin red, rango, puerto ni zona";
    private const int OctetosIpv4 = 4;
    private const int MaximoOcteto = 255;
    private const int DigitosMaximosOcteto = 3;

    public static IReadOnlyList<IPAddress> Efectivas(IConfiguration configuracion) =>
    [
        .. configuracion.GetSection(Seccion).GetChildren()
            .Select(entrada => entrada.Value)
            .Where(valor => !string.IsNullOrEmpty(valor) && EsIpIndividual(valor))
            .Select(valor => Normalizar(IPAddress.Parse(valor!)))
            .Distinct()
    ];

    public static IReadOnlyList<string> Invalidas(IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection(Seccion);
        var escalar = string.IsNullOrEmpty(seccion.Value)
            ? []
            : new[] { ValidadorDeConfiguracion.LineaInvalida(Seccion, seccion.Value, Regla) };
        var entradas = seccion.GetChildren()
            .Where(entrada => !string.IsNullOrEmpty(entrada.Value) && !EsIpIndividual(entrada.Value))
            .Select(entrada => ValidadorDeConfiguracion.LineaInvalida(entrada.Path, entrada.Value, Regla));
        return [.. escalar, .. entradas];
    }

    public static IApplicationBuilder UsarProxyConfiable(this IApplicationBuilder aplicacion, IConfiguration configuracion)
    {
        var ips = Efectivas(configuracion);
        if (ips.Count == 0)
        {
            return aplicacion;
        }

        var opciones = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
            ForwardLimit = 1
        };
        opciones.KnownIPNetworks.Clear();
        opciones.KnownProxies.Clear();
        foreach (var ip in ips)
        {
            opciones.KnownProxies.Add(ip);
        }

        return aplicacion.UseForwardedHeaders(opciones);
    }

    private static bool EsIpIndividual(string? valor) => valor switch
    {
        null => false,
        _ when valor.Contains(':') => EsIpv6(valor),
        _ => EsIpv4Estricta(valor) && valor != IPAddress.Any.ToString()
    };

    private static bool EsIpv4Estricta(string valor)
    {
        var octetos = valor.Split('.');
        return octetos.Length == OctetosIpv4 && octetos.All(EsOcteto);
    }

    private static bool EsOcteto(string texto) =>
        texto.Length is >= 1 and <= DigitosMaximosOcteto
        && texto.All(char.IsAsciiDigit)
        && (texto.Length == 1 || texto[0] != '0')
        && int.Parse(texto, CultureInfo.InvariantCulture) <= MaximoOcteto;

    private static bool EsIpv6(string valor) =>
        valor.All(caracter => char.IsAsciiHexDigit(caracter) || caracter is ':' or '.')
        && IPAddress.TryParse(valor, out var direccion)
        && direccion.AddressFamily == AddressFamily.InterNetworkV6
        && !EsNoEspecificada(direccion);

    private static bool EsNoEspecificada(IPAddress direccion)
    {
        var normalizada = Normalizar(direccion);
        return normalizada.Equals(IPAddress.IPv6Any) || normalizada.Equals(IPAddress.Any);
    }

    private static IPAddress Normalizar(IPAddress direccion) =>
        direccion.IsIPv4MappedToIPv6 ? direccion.MapToIPv4() : direccion;
}
