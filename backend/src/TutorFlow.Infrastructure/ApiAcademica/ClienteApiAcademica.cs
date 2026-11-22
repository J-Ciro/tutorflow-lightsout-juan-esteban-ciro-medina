using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.ApiAcademica;

public sealed class OpcionesApiAcademica
{
    public const string Seccion = "ApiAcademica";

    public string UrlBase { get; init; } = "http://localhost:4000";
    public int PresupuestoSegundos { get; init; } = 6;
}

public sealed class ClienteApiAcademica : IApiAcademica
{
    private readonly HttpClient _httpClient;

    public ClienteApiAcademica(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient Cliente => _httpClient;

    public Task<Estudiante?> ObtenerEstudianteAsync(string id, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<Coordinador?> ObtenerCoordinadorAsync(string id, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<IReadOnlyList<NotasAsignatura>> ObtenerNotasAsync(string estudianteId, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<IReadOnlyList<AsistenciaAsignatura>> ObtenerAsistenciaAsync(string estudianteId, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<IReadOnlyList<Estudiante>> ObtenerEstudiantesDeProgramaAsync(string programaId, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<IReadOnlyList<Programa>> ObtenerProgramasAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
