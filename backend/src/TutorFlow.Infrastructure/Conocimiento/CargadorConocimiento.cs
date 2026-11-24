using System.Text;
using System.Text.RegularExpressions;
using TutorFlow.Core;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Infrastructure.Conocimiento;

public sealed class OpcionesConocimiento
{
    public const string Seccion = "Conocimiento";

    public string? RutaReglamento { get; init; }

    public IReadOnlyList<string> DocumentosVigentes { get; init; } = [];
}

public static partial class CargadorConocimiento
{
    private const string CarpetaReglamento = "reglamento";
    private const string PrefijoArticulo = "Artículo";
    private const string PrefijoSeccion = "Sección";

    public static BaseConocimientoEnMemoria Cargar(OpcionesConocimiento opciones, UmbralesDeEntrega? umbrales = null)
    {
        var carpeta = ResolverCarpeta(opciones.RutaReglamento);
        var secciones = opciones.DocumentosVigentes
            .SelectMany(documento => LeerDocumento(Path.Combine(carpeta, documento), documento))
            .ToList();
        return new BaseConocimientoEnMemoria(secciones, umbrales);
    }

    public static string ResolverCarpeta(string? rutaConfigurada)
    {
        if (!string.IsNullOrWhiteSpace(rutaConfigurada))
        {
            return rutaConfigurada;
        }

        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !Directory.Exists(Path.Combine(directorio.FullName, CarpetaReglamento)))
        {
            directorio = directorio.Parent;
        }

        return directorio is null
            ? throw new DirectoryNotFoundException("No se encontró la carpeta del reglamento.")
            : Path.Combine(directorio.FullName, CarpetaReglamento);
    }

    private static List<SeccionDocumento> LeerDocumento(string ruta, string documento)
    {
        var secciones = new List<SeccionDocumento>();
        var actual = default(SeccionEnConstruccion);
        foreach (var linea in File.ReadLines(ruta))
        {
            actual = Procesar(linea, documento, actual, secciones);
        }

        Cerrar(actual, secciones);
        return secciones;
    }

    private static SeccionEnConstruccion? Procesar(string linea, string documento, SeccionEnConstruccion? actual, List<SeccionDocumento> secciones)
    {
        var articulo = ArticuloRegex().Match(linea);
        if (articulo.Success)
        {
            Cerrar(actual, secciones);
            return new SeccionEnConstruccion(documento, $"{PrefijoArticulo} {articulo.Groups["n"].Value}", Titulo(articulo.Groups["t"].Value), articulo.Groups["x"].Value);
        }

        if (linea.StartsWith('#'))
        {
            Cerrar(actual, secciones);
            return AbrirDesdeEncabezado(linea, documento);
        }

        actual?.Texto.AppendLine(linea);
        return actual;
    }

    private static SeccionEnConstruccion? AbrirDesdeEncabezado(string linea, string documento)
    {
        var encabezado = EncabezadoRegex().Match(linea);
        return encabezado.Success
            ? new SeccionEnConstruccion(documento, $"{PrefijoSeccion} {encabezado.Groups["n"].Value}", Titulo(encabezado.Groups["t"].Value), string.Empty)
            : null;
    }

    private static void Cerrar(SeccionEnConstruccion? actual, List<SeccionDocumento> secciones)
    {
        if (actual is not null)
        {
            secciones.Add(new SeccionDocumento(actual.Documento, actual.Seccion, actual.Titulo, actual.Texto.ToString().Trim()));
        }
    }

    private static string Titulo(string texto) => texto.Trim().TrimEnd('.');

    [GeneratedRegex(@"^\*\*Art[ií]culo (?<n>\d+)\.\s*(?<t>[^*]+?)\*\*\s*(?<x>.*)$", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ArticuloRegex();

    [GeneratedRegex(@"^#{2,3}\s+(?<n>\d+(?:\.\d+)?)\.?\s+(?<t>.+)$", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex EncabezadoRegex();

    private sealed class SeccionEnConstruccion
    {
        public SeccionEnConstruccion(string documento, string seccion, string titulo, string textoInicial)
        {
            Documento = documento;
            Seccion = seccion;
            Titulo = titulo;
            Texto = new StringBuilder(textoInicial);
        }

        public string Documento { get; }

        public string Seccion { get; }

        public string Titulo { get; }

        public StringBuilder Texto { get; }
    }
}
