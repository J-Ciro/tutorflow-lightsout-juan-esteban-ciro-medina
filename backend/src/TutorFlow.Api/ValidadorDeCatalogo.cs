using System.Text.RegularExpressions;
using TutorFlow.Core;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api;

internal static partial class ValidadorDeCatalogo
{
    public const string SeccionAsignaturas = "Autorizacion:Asignaturas";
    public const string SeccionListaDeCodigos = "Autorizacion:CodigosAsignatura";
    private const string ReglaFormato = @"formato ^[A-Z]{2,4}-[0-9]{3}\z";

    private sealed record Entrada(string Indice, string? Codigo, string? Nombre)
    {
        public string ClaveDeCodigo => $"{SeccionAsignaturas}:{Indice}:Codigo";

        public string ClaveDeNombre => $"{SeccionAsignaturas}:{Indice}:Nombre";
    }

    private sealed record Incumplimiento(bool Falla, string Clave, string? Valor, string Regla);

    public static IEnumerable<string> Validar(IConfiguration configuracion)
    {
        var entradas = configuracion.GetSection(SeccionAsignaturas).GetChildren()
            .Select(hijo => new Entrada(hijo.Key, hijo["Codigo"], hijo["Nombre"]))
            .ToList();
        var delCatalogo = entradas.Count == 0
            ? [ValidadorDeConfiguracion.LineaInvalida(SeccionAsignaturas, string.Empty, "catálogo vacío")]
            : entradas.SelectMany((entrada, posicion) => LineasDe(entrada, entradas.Take(posicion).ToList()));
        return delCatalogo.Concat(LineasDeLaLista(configuracion, entradas));
    }

    private static IEnumerable<string> LineasDe(Entrada entrada, List<Entrada> anteriores)
    {
        var codigoRepetido = anteriores.FirstOrDefault(otra => !string.IsNullOrEmpty(entrada.Codigo) && otra.Codigo == entrada.Codigo);
        var nombreRepetido = anteriores.FirstOrDefault(otra => !EstaVacio(entrada.Nombre) && Normalizado(otra.Nombre) == Normalizado(entrada.Nombre));
        return Lineas(
            new Incumplimiento(!CumpleFormato(entrada.Codigo), entrada.ClaveDeCodigo, entrada.Codigo, ReglaFormato),
            new Incumplimiento(codigoRepetido is not null, entrada.ClaveDeCodigo, entrada.Codigo, $"código duplicado de {codigoRepetido?.ClaveDeCodigo}"),
            new Incumplimiento(EstaVacio(entrada.Nombre), entrada.ClaveDeNombre, entrada.Nombre, "nombre vacío"),
            new Incumplimiento(nombreRepetido is not null, entrada.ClaveDeNombre, entrada.Nombre, $"nombre duplicado de {nombreRepetido?.ClaveDeNombre}"));
    }

    private static IEnumerable<string> LineasDeLaLista(IConfiguration configuracion, List<Entrada> entradas)
    {
        var codigos = configuracion.GetSection(SeccionListaDeCodigos).GetChildren().Select(hijo => (hijo.Path, hijo.Value)).ToList();
        return codigos.SelectMany((codigo, posicion) =>
        {
            var formato = CumpleFormato(codigo.Value);
            var repetido = codigos.Take(posicion).FirstOrDefault(otro => otro.Value == codigo.Value);
            return Lineas(
                new Incumplimiento(!formato, codigo.Path, codigo.Value, ReglaFormato),
                new Incumplimiento(formato && repetido.Path is not null, codigo.Path, codigo.Value, $"código duplicado de {repetido.Path}"),
                new Incumplimiento(formato && entradas.All(entrada => entrada.Codigo != codigo.Value), codigo.Path, codigo.Value, $"no está en {SeccionAsignaturas}"));
        });
    }

    private static IEnumerable<string> Lineas(params Incumplimiento[] reglas) =>
        reglas.Where(regla => regla.Falla).Select(regla => ValidadorDeConfiguracion.LineaInvalida(regla.Clave, regla.Valor, regla.Regla));

    private static bool EstaVacio(string? texto) => string.IsNullOrWhiteSpace(texto);

    private static string Normalizado(string? texto) => NormalizadorTexto.Normalizar(texto ?? string.Empty);

    private static bool CumpleFormato(string? codigo) => codigo is not null && FormatoDeCodigo().IsMatch(codigo);

    [GeneratedRegex(@"^[A-Z]{2,4}-[0-9]{3}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex FormatoDeCodigo();
}
