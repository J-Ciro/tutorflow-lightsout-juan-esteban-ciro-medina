using System.Text.Json;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Infrastructure.ApiAcademica;

internal static class LectorJsonAcademico
{
    public static IReadOnlyList<T> Lista<T>(JsonElement? json, Func<JsonElement, T?> convertir)
        where T : class
    {
        if (json is not { ValueKind: JsonValueKind.Array } arreglo)
        {
            return [];
        }

        return arreglo.EnumerateArray().Select(convertir).OfType<T>().ToList();
    }

    public static Estudiante? Estudiante(JsonElement json)
    {
        var id = Texto(json, "id");
        var programaId = Texto(json, "programaId");
        var nombre = Texto(json, "nombre");
        return Identificadores.EsIdEstudiante(id) && Identificadores.EsIdPrograma(programaId) && nombre is not null
            ? new Estudiante(id!, nombre, programaId!)
            : null;
    }

    public static Coordinador? Coordinador(JsonElement json)
    {
        var id = Texto(json, "id");
        var nombre = Texto(json, "nombre");
        if (!Identificadores.EsIdCoordinador(id) || nombre is null)
        {
            return null;
        }

        var programas = ListaDeTextos(json, "programas").Where(Identificadores.EsIdPrograma).Distinct().ToList();
        return new Coordinador(id!, nombre, programas);
    }

    public static Programa? Programa(JsonElement json)
    {
        var id = Texto(json, "id");
        var nombre = Texto(json, "nombre");
        var coordinadorId = Texto(json, "coordinadorId");
        return Identificadores.EsIdPrograma(id) && nombre is not null
            ? new Programa(id!, nombre, Identificadores.EsIdCoordinador(coordinadorId) ? coordinadorId! : string.Empty)
            : null;
    }

    public static NotasAsignatura? Notas(JsonElement json)
    {
        var codigo = Texto(json, "codigo");
        var nombre = Texto(json, "asignatura");
        if (codigo is null || nombre is null || Entero(json, "creditos") is not { } creditos)
        {
            return null;
        }

        var cortes = new NotasCorte(Decimal(json, "corte1"), Decimal(json, "corte2"), Decimal(json, "corte3"));
        return new NotasAsignatura(codigo, nombre, creditos, cortes);
    }

    public static AsistenciaAsignatura? Asistencia(JsonElement json)
    {
        var codigo = Texto(json, "codigo");
        if (codigo is null || Entero(json, "clasesProgramadas") is not { } clases)
        {
            return null;
        }

        return new AsistenciaAsignatura(codigo, new Asistencia(clases, Entero(json, "inasistencias")));
    }

    private static string? Texto(JsonElement json, string propiedad) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(propiedad, out var valor)
        && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;

    private static decimal? Decimal(JsonElement json, string propiedad) =>
        json.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.Number
            ? valor.GetDecimal()
            : null;

    private static int? Entero(JsonElement json, string propiedad) =>
        json.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.Number && valor.TryGetInt32(out var numero)
            ? numero
            : null;

    private static IEnumerable<string> ListaDeTextos(JsonElement json, string propiedad)
    {
        if (!json.TryGetProperty(propiedad, out var valor) || valor.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return valor.EnumerateArray()
            .Where(elemento => elemento.ValueKind == JsonValueKind.String)
            .Select(elemento => elemento.GetString()!);
    }
}
