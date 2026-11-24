using System.Net;
using System.Text.Json;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Infrastructure.ApiAcademica;

public sealed class OpcionesApiAcademica
{
    public const string Seccion = "ApiAcademica";

    public string UrlBase { get; init; } = "http://localhost:4000";
}

public sealed class ClienteApiAcademica : IApiAcademica
{
    private readonly HttpClient _httpClient;

    public ClienteApiAcademica(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Estudiante?> ObtenerEstudianteAsync(string id, CancellationToken cancellationToken)
    {
        if (!Identificadores.EsIdEstudiante(id))
        {
            return null;
        }

        var json = await LeerAsync($"estudiantes/{Uri.EscapeDataString(id)}", cancellationToken);
        return json is { } elemento ? LectorJsonAcademico.Estudiante(elemento) : null;
    }

    public async Task<Coordinador?> ObtenerCoordinadorAsync(string id, CancellationToken cancellationToken)
    {
        if (!Identificadores.EsIdCoordinador(id))
        {
            return null;
        }

        var json = await LeerAsync($"coordinadores/{Uri.EscapeDataString(id)}", cancellationToken);
        return json is { } elemento ? LectorJsonAcademico.Coordinador(elemento) : null;
    }

    public async Task<IReadOnlyList<NotasAsignatura>> ObtenerNotasAsync(string estudianteId, CancellationToken cancellationToken)
    {
        if (!Identificadores.EsIdEstudiante(estudianteId))
        {
            return [];
        }

        var json = await LeerAsync($"estudiantes/{Uri.EscapeDataString(estudianteId)}/notas", cancellationToken);
        return LectorJsonAcademico.Lista(json, LectorJsonAcademico.Notas);
    }

    public async Task<IReadOnlyList<AsistenciaAsignatura>> ObtenerAsistenciaAsync(string estudianteId, CancellationToken cancellationToken)
    {
        if (!Identificadores.EsIdEstudiante(estudianteId))
        {
            return [];
        }

        var json = await LeerAsync($"estudiantes/{Uri.EscapeDataString(estudianteId)}/asistencia", cancellationToken);
        return LectorJsonAcademico.Lista(json, LectorJsonAcademico.Asistencia);
    }

    public async Task<IReadOnlyList<Estudiante>> ObtenerEstudiantesDeProgramaAsync(string programaId, CancellationToken cancellationToken)
    {
        if (!Identificadores.EsIdPrograma(programaId))
        {
            return [];
        }

        var json = await LeerAsync($"programas/{Uri.EscapeDataString(programaId)}/estudiantes", cancellationToken);
        return LectorJsonAcademico.Lista(json, LectorJsonAcademico.Estudiante);
    }

    public async Task<IReadOnlyList<Programa>> ObtenerProgramasAsync(CancellationToken cancellationToken)
    {
        var json = await LeerAsync("programas", cancellationToken);
        return LectorJsonAcademico.Lista(json, LectorJsonAcademico.Programa);
    }

    private async Task<JsonElement?> LeerAsync(string rutaRelativa, CancellationToken cancellationToken)
    {
        using var respuesta = await _httpClient.GetAsync(rutaRelativa, cancellationToken);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        var contenido = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        using var documento = JsonDocument.Parse(contenido);
        return documento.RootElement.Clone();
    }
}
