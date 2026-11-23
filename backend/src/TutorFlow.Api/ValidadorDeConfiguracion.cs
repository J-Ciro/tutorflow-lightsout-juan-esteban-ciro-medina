namespace TutorFlow.Api;

public static class ValidadorDeConfiguracion
{
    internal const string PrefijoInvalida = "Configuración inválida";
    internal const string PrefijoIgnorada = "Configuración ignorada";
    private const string SeccionModelos = "Llm:Proveedores";
    private const string PrecioDeEntrada = "PrecioEntradaPorMillon";
    private const string PrecioDeSalida = "PrecioSalidaPorMillon";

    public static IReadOnlyList<string> Validar(IConfiguration configuracion) =>
        [.. ReglasDeError(configuracion)
            .Where(regla => EsInvalido(configuracion, regla))
            .Select(regla => LineaInvalida(regla.Clave, configuracion[regla.Clave], regla.Texto))
            .Concat(ValidadorDeCatalogo.Validar(configuracion))
            .Concat(ProxyConfiable.Invalidas(configuracion))];

    internal static string LineaInvalida(string clave, string? valor, string regla) =>
        $"{PrefijoInvalida}: {clave} = \"{ValorDeMensaje.De(valor)}\" ({regla})";

    internal static IReadOnlyList<string> Advertencias(IConfiguration configuracion) =>
        [.. ReglaDeConfiguracion.ConAdvertencia
            .Where(regla => EsInvalido(configuracion, regla))
            .Select(regla => $"{PrefijoIgnorada}: {regla.Clave} = \"{ValorDeMensaje.De(configuracion[regla.Clave])}\" ({regla.Texto}); se usa {regla.Defecto}")];

    internal static decimal? Efectivo(IConfiguration configuracion, string clave) =>
        ReglaDeConfiguracion.Buscar(clave).LeerValido(configuracion[clave]);

    private static IEnumerable<ReglaDeConfiguracion> ReglasDeError(IConfiguration configuracion) =>
        ReglaDeConfiguracion.Todas.Concat(ClavesDePrecioPorModelo(configuracion).Select(ReglaDeConfiguracion.Precio));

    private static IEnumerable<string> ClavesDePrecioPorModelo(IConfiguration configuracion) =>
        configuracion.GetSection(SeccionModelos).GetChildren()
            .SelectMany(proveedor => proveedor.GetSection("Modelos").GetChildren())
            .SelectMany(modelo => new[] { $"{modelo.Path}:{PrecioDeEntrada}", $"{modelo.Path}:{PrecioDeSalida}" });

    private static bool EsInvalido(IConfiguration configuracion, ReglaDeConfiguracion regla)
    {
        var valor = configuracion[regla.Clave];
        return !string.IsNullOrWhiteSpace(valor) && !regla.Cumple(valor);
    }
}
