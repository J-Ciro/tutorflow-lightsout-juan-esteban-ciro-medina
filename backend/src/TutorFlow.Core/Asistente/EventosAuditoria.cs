using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public enum ResultadoAuditoria
{
    Respondida,
    SinInformacion,
    Denegada,
    Invalida,
    FugaBloqueada,
    NumeroNoVerificado,
    EstadoNoVerificado,
    LimiteHerramientas,
    Error
}

public enum RecursoConsultado
{
    DatosBasicos,
    Notas,
    Asistencia,
    Programa
}

public sealed record DatoConsultado(
    string EstudianteId,
    string ProgramaId,
    RecursoConsultado Recurso,
    string? Asignatura = null);

public sealed record EventoAuditoria(
    DateTimeOffset Timestamp,
    string CorrelationId,
    string IpOrigen,
    Rol? Rol,
    string? UsuarioId,
    string Pregunta,
    ResultadoAuditoria Resultado,
    MotivoDenegacion? MotivoDenegacion,
    IReadOnlyList<string> EstudiantesConsultados,
    IReadOnlyList<RecursoConsultado> RecursosConsultados,
    IReadOnlyList<Cita> Citas,
    int Tokens,
    decimal CostoUsd,
    long DuracionMs,
    bool PrecioDesconocido)
{
    public IReadOnlyList<DatoConsultado> DatosConsultados { get; init; } = [];

    public string? IdSolicitado { get; init; }
}

public sealed record EventoMultiplesIdentidades(
    DateTimeOffset Timestamp,
    string IpOrigen,
    IReadOnlyList<string> UsuarioIds);
