namespace TutorFlow.Infrastructure.Llm;

public static class ResolutorProveedorLlm
{
    public static ProveedorResuelto Resolver(EntornoLlm entorno, OpcionesLlm opciones)
    {
        var urlExplicita = Limpiar(entorno.UrlBase);
        var determinacion = DeterminacionDelProveedor.Determinar(Limpiar(entorno.Proveedor), Limpiar(entorno.Llave), Limpiar(entorno.Modelo), urlExplicita);
        var proveedor = determinacion.Proveedor;
        var configuracion = ConfiguracionDe(proveedor, opciones);
        var modelo = ElegirModelo(entorno, configuracion);
        var precio = ResolverPrecio(entorno, configuracion, modelo);
        return new ProveedorResuelto(
            proveedor,
            AdaptadorDe(proveedor),
            ElegirUrl(proveedor, urlExplicita, configuracion),
            modelo,
            precio.Entrada,
            precio.Salida,
            precio.Desconocido,
            determinacion.Conflicto);
    }

    private static string ElegirModelo(EntornoLlm entorno, ConfiguracionProveedor? configuracion) =>
        Limpiar(entorno.Modelo) ?? configuracion?.ModeloPorDefecto ?? string.Empty;

    private static string ElegirUrl(ProveedorLlm proveedor, string? urlExplicita, ConfiguracionProveedor? configuracion) =>
        proveedor == ProveedorLlm.Desconocido ? string.Empty : urlExplicita ?? configuracion?.UrlBase ?? string.Empty;

    private static PrecioResuelto ResolverPrecio(EntornoLlm entorno, ConfiguracionProveedor? configuracion, string modelo)
    {
        var configurado = BuscarModelo(configuracion, modelo);
        return PrecioResuelto.De(
            entorno.PrecioEntrada ?? configurado?.PrecioEntradaPorMillon,
            entorno.PrecioSalida ?? configurado?.PrecioSalidaPorMillon);
    }

    private static ModeloConfigurado? BuscarModelo(ConfiguracionProveedor? configuracion, string modelo) =>
        configuracion?.Modelos.FirstOrDefault(m => m.Nombre == modelo);

    private sealed record PrecioResuelto(decimal Entrada, decimal Salida, bool Desconocido)
    {
        public static PrecioResuelto De(decimal? entrada, decimal? salida) =>
            new(entrada.GetValueOrDefault(), salida.GetValueOrDefault(), !entrada.HasValue || !salida.HasValue);
    }

    private static ConfiguracionProveedor? ConfiguracionDe(ProveedorLlm proveedor, OpcionesLlm opciones)
    {
        var clave = proveedor switch
        {
            ProveedorLlm.OpenAiCompatible or ProveedorLlm.Desconocido => nameof(ProveedorLlm.OpenAi),
            _ => proveedor.ToString()
        };
        return opciones.Proveedores.FirstOrDefault(par => string.Equals(par.Key, clave, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static AdaptadorLlm AdaptadorDe(ProveedorLlm proveedor) => proveedor switch
    {
        ProveedorLlm.Anthropic => AdaptadorLlm.Anthropic,
        ProveedorLlm.Desconocido => AdaptadorLlm.NoConfigurado,
        _ => AdaptadorLlm.CompatibleOpenAi
    };

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
