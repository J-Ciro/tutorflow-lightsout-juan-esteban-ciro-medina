using TutorFlow.Core.Academico;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public interface IApiAcademica
{
    Task<Estudiante?> ObtenerEstudianteAsync(string id, CancellationToken cancellationToken);

    Task<Coordinador?> ObtenerCoordinadorAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotasAsignatura>> ObtenerNotasAsync(string estudianteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AsistenciaAsignatura>> ObtenerAsistenciaAsync(string estudianteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Estudiante>> ObtenerEstudiantesDeProgramaAsync(string programaId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Programa>> ObtenerProgramasAsync(CancellationToken cancellationToken);
}

public interface IBaseConocimiento
{
    IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo);

    bool ExisteCita(Cita cita);
}

public interface IAuditoria
{
    void Registrar(EventoAuditoria evento);

    void RegistrarMultiplesIdentidades(EventoMultiplesIdentidades evento);
}

public interface IReloj
{
    DateTimeOffset AhoraUtc { get; }
}

public interface IClienteLlm
{
    Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken);
}
