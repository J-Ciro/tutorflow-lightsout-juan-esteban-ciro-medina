using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Pruebas.Soporte;

public sealed partial class BaseConocimientoFalsa : IBaseConocimiento
{
    public const string Reglamento = "reglamento_academico_2025.md";
    public const string Evaluacion = "evaluacion_y_calificaciones.md";
    public const string Bienestar = "bienestar_y_becas.md";
    public const int LongitudMinimaPalabra = 4;

    private readonly IReadOnlyList<SeccionDocumento> _secciones;

    private BaseConocimientoFalsa(IReadOnlyList<SeccionDocumento> secciones)
    {
        _secciones = secciones;
    }

    public static BaseConocimientoFalsa Vigente()
    {
        var carpeta = Path.Combine(DatosReferencia.RutaRepositorio(), "reglamento");
        var secciones = new List<SeccionDocumento>();
        secciones.AddRange(LeerArticulos(Path.Combine(carpeta, Reglamento)));
        secciones.AddRange(LeerSecciones(Path.Combine(carpeta, Evaluacion), Evaluacion));
        secciones.AddRange(LeerSecciones(Path.Combine(carpeta, Bienestar), Bienestar));
        return new BaseConocimientoFalsa(secciones);
    }

    public BaseConocimientoFalsa ConSeccion(SeccionDocumento seccion) =>
        new([.. _secciones, seccion]);

    public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo)
    {
        var palabras = Palabras(consulta);
        return _secciones
            .Select(s => (Seccion: s, Puntaje: palabras.Count(p => Normalizar(s.Titulo + " " + s.Texto).Contains(p, StringComparison.Ordinal))))
            .Where(x => x.Puntaje > 0)
            .OrderByDescending(x => x.Puntaje)
            .Take(maximo)
            .Select(x => x.Seccion)
            .ToList();
    }

    public bool ExisteCita(Cita cita) =>
        _secciones.Any(s => s.Documento == cita.Documento && s.Seccion == cita.Seccion);

    private static List<SeccionDocumento> LeerArticulos(string ruta) =>
        File.ReadAllLines(ruta)
            .Select(l => ArticuloRegex().Match(l))
            .Where(m => m.Success)
            .Select(m => new SeccionDocumento(
                Reglamento,
                $"Artículo {m.Groups["n"].Value}",
                m.Groups["t"].Value.Trim().TrimEnd('.'),
                m.Groups["x"].Value))
            .ToList();

    private static List<SeccionDocumento> LeerSecciones(string ruta, string documento)
    {
        var resultado = new List<SeccionDocumento>();
        string? numero = null;
        var titulo = string.Empty;
        var cuerpo = new StringBuilder();
        foreach (var linea in File.ReadAllLines(ruta))
        {
            if (!linea.StartsWith('#'))
            {
                cuerpo.AppendLine(linea);
                continue;
            }

            Cerrar(resultado, documento, numero, titulo, cuerpo);
            var encabezado = EncabezadoRegex().Match(linea);
            numero = encabezado.Success ? encabezado.Groups["n"].Value : null;
            titulo = encabezado.Success ? encabezado.Groups["t"].Value : string.Empty;
            cuerpo.Clear();
        }

        Cerrar(resultado, documento, numero, titulo, cuerpo);
        return resultado;
    }

    private static void Cerrar(List<SeccionDocumento> resultado, string documento, string? numero, string titulo, StringBuilder cuerpo)
    {
        if (numero is not null)
        {
            resultado.Add(new SeccionDocumento(documento, $"Sección {numero}", titulo, cuerpo.ToString()));
        }
    }

    private static string[] Palabras(string texto) =>
        Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= LongitudMinimaPalabra)
            .Distinct()
            .ToArray();

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sinTildes = descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(sinTildes.Select(c => char.IsLetterOrDigit(c) || c == '.' ? c : ' ').ToArray());
    }

    [GeneratedRegex(@"^\*\*Art[ií]culo (?<n>\d+)\.\s*(?<t>[^*]+?)\*\*\s*(?<x>.*)$")]
    private static partial Regex ArticuloRegex();

    [GeneratedRegex(@"^#{2,3}\s+(?<n>\d+(?:\.\d+)?)\.?\s+(?<t>.+)$")]
    private static partial Regex EncabezadoRegex();
}
