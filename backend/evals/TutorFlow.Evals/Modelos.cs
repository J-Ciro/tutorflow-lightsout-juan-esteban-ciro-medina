using System.Text.Json;

namespace TutorFlow.Evals;

public sealed record UsuarioEval(string Rol, string Id);

public sealed record ParejaEval(string Nombre, string Valor);

public sealed record GuionOffline(string Herramientas, string? EstudianteIdCoordinador, string? Texto);

public sealed record CasoEval(
    string Id,
    string Fuente,
    UsuarioEval Usuario,
    string Pregunta,
    int Http,
    IReadOnlyList<string> Contiene,
    IReadOnlyList<string> NoContiene,
    IReadOnlyList<string> CitasIncluye,
    IReadOnlyList<string> CitasProhibidas,
    bool CitasVacias,
    IReadOnlyList<string>? CitasExactas,
    int? CitasTotal,
    bool? UsoDatosEstudiante,
    string? RespuestaExacta,
    bool TokensCero,
    bool FiltroPrevio,
    bool RequiereLlm,
    bool SoloOffline,
    GuionOffline? Offline)
{
    public IReadOnlyList<ParejaEval>? Parejas { get; init; }

    public IReadOnlyList<IReadOnlyList<string>>? ContieneAlguna { get; init; }

    public bool Respondida { get; init; }

    public IReadOnlyList<string>? Cifras { get; init; }

    public IReadOnlyList<string>? CitasDocumentoExactas { get; init; }

    public int? UmbralCorridas { get; init; }

    public string Etiqueta => $"{Id} [{Usuario.Rol}:{Usuario.Id}] {Resumen(Pregunta)}";

    private static string Resumen(string texto) => texto.Length <= 48 ? texto : texto[..48] + "...";
}

public sealed record OpcionesEvals(string Url, bool Offline, string RutaCasos, int Corridas, int PausaMs, int UmbralP95Ms, string? Filtro, bool FaseV);

public sealed record Ejecucion(int Estado, string Cuerpo, double Milisegundos);

public sealed record ResultadoCaso(CasoEval Caso, int Corrida, IReadOnlyList<string> Fallos, Ejecucion? Ejecucion)
{
    public bool Omitido => Ejecucion is null;

    public bool Aprobado => !Omitido && Fallos.Count == 0;
}

public sealed record CitaEval(string Documento, string Seccion)
{
    public string Texto => $"{Documento} · {Seccion}";
}

public sealed record SalidaEval(string? Respuesta, IReadOnlyList<CitaEval>? Citas, bool UsoDatosEstudiante, int Tokens, decimal CostoUsd);

public static class Json
{
    public static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    public static SalidaEval? LeerSalida(string cuerpo)
    {
        try
        {
            return JsonSerializer.Deserialize<SalidaEval>(cuerpo, Opciones);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
