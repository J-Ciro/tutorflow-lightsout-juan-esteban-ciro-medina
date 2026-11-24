using System.Text.Json;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Pruebas.Soporte;

public sealed record NotasRegistro(string EstudianteId, NotasAsignatura Notas);

public sealed record AsistenciaRegistro(string EstudianteId, AsistenciaAsignatura Asistencia);

public sealed record DatosAcademicos(
    IReadOnlyList<Programa> Programas,
    IReadOnlyList<Coordinador> Coordinadores,
    IReadOnlyList<Estudiante> Estudiantes,
    IReadOnlyList<NotasRegistro> Notas,
    IReadOnlyList<AsistenciaRegistro> Asistencia)
{
    public DirectorioAcademico Directorio => new(
        Estudiantes,
        Coordinadores,
        Programas,
        Notas.Select(n => n.Notas.Codigo).Distinct().ToList());

    public IReadOnlyList<NotasAsignatura> NotasDe(string estudianteId) =>
        Notas.Where(n => n.EstudianteId == estudianteId).Select(n => n.Notas).ToList();

    public IReadOnlyList<AsistenciaAsignatura> AsistenciaDe(string estudianteId) =>
        Asistencia.Where(a => a.EstudianteId == estudianteId).Select(a => a.Asistencia).ToList();

    public IReadOnlyList<Asignatura> AsignaturasDe(string estudianteId)
    {
        var asistencia = AsistenciaDe(estudianteId);
        return NotasDe(estudianteId)
            .Select(n => new Asignatura(
                n.Codigo,
                n.Nombre,
                n.Creditos,
                n.Cortes,
                asistencia.Single(a => a.Codigo == n.Codigo).Asistencia))
            .ToList();
    }

    public DatosAcademicos ConCoordinador(Coordinador coordinador) =>
        this with { Coordinadores = [.. Coordinadores.Where(c => c.Id != coordinador.Id), coordinador] };

    public DatosAcademicos ConNombreDeAsignatura(string estudianteId, string codigo, string nombre) =>
        this with
        {
            Notas = Notas
                .Select(n => n.EstudianteId == estudianteId && n.Notas.Codigo == codigo
                    ? n with { Notas = n.Notas with { Nombre = nombre } }
                    : n)
                .ToList()
        };
}

public static class DatosReferencia
{
    private static readonly Lazy<DatosAcademicos> Datos = new(LeerDatos);

    public static DatosAcademicos Cargar() => Datos.Value;

    public static string RutaRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "mock-api", "datos.json")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new DirectoryNotFoundException("No se encontro mock-api/datos.json");
    }

    private static DatosAcademicos LeerDatos()
    {
        using var documento = JsonDocument.Parse(File.ReadAllText(Path.Combine(RutaRepositorio(), "mock-api", "datos.json")));
        var raiz = documento.RootElement;
        return new DatosAcademicos(
            Elementos(raiz, "programas", p => new Programa(Texto(p, "id"), Texto(p, "nombre"), Texto(p, "coordinadorId"))),
            Elementos(raiz, "coordinadores", c => new Coordinador(
                Texto(c, "id"),
                Texto(c, "nombre"),
                c.GetProperty("programas").EnumerateArray().Select(x => x.GetString()!).ToList())),
            Elementos(raiz, "estudiantes", e => new Estudiante(Texto(e, "id"), Texto(e, "nombre"), Texto(e, "programaId"))),
            Elementos(raiz, "notas", n => new NotasRegistro(
                Texto(n, "estudianteId"),
                new NotasAsignatura(
                    Texto(n, "codigo"),
                    Texto(n, "asignatura"),
                    n.GetProperty("creditos").GetInt32(),
                    new NotasCorte(Decimal(n, "corte1"), Decimal(n, "corte2"), Decimal(n, "corte3"))))),
            Elementos(raiz, "asistencia", a => new AsistenciaRegistro(
                Texto(a, "estudianteId"),
                new AsistenciaAsignatura(
                    Texto(a, "codigo"),
                    new Asistencia(a.GetProperty("clasesProgramadas").GetInt32(), Entero(a, "inasistencias"))))));
    }

    private static List<T> Elementos<T>(JsonElement raiz, string propiedad, Func<JsonElement, T> mapear) =>
        raiz.GetProperty(propiedad).EnumerateArray().Select(mapear).ToList();

    private static string Texto(JsonElement elemento, string propiedad) => elemento.GetProperty(propiedad).GetString()!;

    private static decimal? Decimal(JsonElement elemento, string propiedad) =>
        elemento.GetProperty(propiedad) is { ValueKind: JsonValueKind.Number } valor ? valor.GetDecimal() : null;

    private static int? Entero(JsonElement elemento, string propiedad) =>
        elemento.GetProperty(propiedad) is { ValueKind: JsonValueKind.Number } valor ? valor.GetInt32() : null;
}
