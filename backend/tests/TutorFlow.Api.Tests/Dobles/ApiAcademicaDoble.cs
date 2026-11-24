using System.Collections.Concurrent;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Dobles;

public sealed class ApiAcademicaDoble : IApiAcademica
{
    private readonly DatosAcademicos _datos;
    private readonly ConcurrentQueue<string> _llamadas = new();

    public ApiAcademicaDoble(DatosAcademicos? datos = null)
    {
        _datos = datos ?? DatosReferencia.Cargar();
    }

    public Func<string, CancellationToken, Task>? Interceptor { get; set; }

    public IReadOnlyList<string> Llamadas => [.. _llamadas];

    public int LecturasDatosDe(string estudianteId) =>
        Llamadas.Count(l => l == $"ObtenerNotas:{estudianteId}" || l == $"ObtenerAsistencia:{estudianteId}");

    public static ApiAcademicaDoble Caida() => new() { Interceptor = (_, _) => throw new HttpRequestException("api academica caida en http://localhost:4000") };

    public static ApiAcademicaDoble QueNuncaResponde(RelojControlado reloj) => new() { Interceptor = (_, ct) => reloj.Pausar(Timeout.InfiniteTimeSpan, ct) };

    public async Task<Estudiante?> ObtenerEstudianteAsync(string id, CancellationToken cancellationToken)
    {
        await Registrar("ObtenerEstudiante", id, cancellationToken);
        return _datos.Estudiantes.FirstOrDefault(e => e.Id == id);
    }

    public async Task<Coordinador?> ObtenerCoordinadorAsync(string id, CancellationToken cancellationToken)
    {
        await Registrar("ObtenerCoordinador", id, cancellationToken);
        return _datos.Coordinadores.FirstOrDefault(c => c.Id == id);
    }

    public async Task<IReadOnlyList<NotasAsignatura>> ObtenerNotasAsync(string estudianteId, CancellationToken cancellationToken)
    {
        await Registrar("ObtenerNotas", estudianteId, cancellationToken);
        return _datos.NotasDe(estudianteId);
    }

    public async Task<IReadOnlyList<AsistenciaAsignatura>> ObtenerAsistenciaAsync(string estudianteId, CancellationToken cancellationToken)
    {
        await Registrar("ObtenerAsistencia", estudianteId, cancellationToken);
        return _datos.AsistenciaDe(estudianteId);
    }

    public async Task<IReadOnlyList<Estudiante>> ObtenerEstudiantesDeProgramaAsync(string programaId, CancellationToken cancellationToken)
    {
        await Registrar("ObtenerEstudiantesDePrograma", programaId, cancellationToken);
        return _datos.Estudiantes.Where(e => e.ProgramaId == programaId).ToList();
    }

    public async Task<IReadOnlyList<Programa>> ObtenerProgramasAsync(CancellationToken cancellationToken)
    {
        await Registrar("ObtenerProgramas", string.Empty, cancellationToken);
        return _datos.Programas;
    }

    private async Task Registrar(string metodo, string argumento, CancellationToken cancellationToken)
    {
        _llamadas.Enqueue($"{metodo}:{argumento}");
        if (Interceptor is not null)
        {
            await Interceptor(metodo, cancellationToken);
        }
    }
}
