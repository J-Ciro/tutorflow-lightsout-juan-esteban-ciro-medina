using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed record PreguntaEntrada(string Pregunta, Usuario Usuario);

public sealed record RespuestaAsistente(
    string Respuesta,
    IReadOnlyList<Cita> Citas,
    bool UsoDatosEstudiante,
    int Tokens,
    decimal CostoUsd);

public enum TipoResultadoPregunta
{
    Respondida,
    Denegada,
    NoDisponible
}

public sealed record ResultadoPregunta(
    TipoResultadoPregunta Tipo,
    RespuestaAsistente? Respuesta,
    ResultadoAuditoria Auditoria,
    MotivoDenegacion? Motivo);

public sealed class LimitesAsistente
{
    public int MaximoCaracteresPregunta { get; init; } = 1000;
    public int MaximoBytesCuerpo { get; init; } = 16 * 1024;
    public int PreguntasPorMinutoPorIp { get; init; } = 60;
    public int MaximoSeccionesRecuperadas { get; init; } = 6;
    public int MaximoTokensSalida { get; init; } = 2000;
    public int MaximoInvocacionesHerramientas { get; init; } = 4;
    public int TimeoutLlmSegundos { get; init; } = 20;
    public int PresupuestoApiAcademicaSegundos { get; init; } = 6;
    public decimal PresupuestoDiarioUsd { get; init; } = 5m;
    public int MaximoIdentidadesPorIp { get; init; } = 3;
    public int VentanaIdentidadesMinutos { get; init; } = 10;
    public bool BloquearMultiplesIdentidades { get; init; }
}

public sealed class PreguntarAsistente
{
    private readonly IApiAcademica _apiAcademica;
    private readonly IClienteLlm _clienteLlm;
    private readonly IBaseConocimiento _baseConocimiento;
    private readonly IAuditoria _auditoria;
    private readonly IReloj _reloj;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly VerificadorSalida _verificadorSalida;
    private readonly LimitesAsistente _limites;

    public PreguntarAsistente(
        IApiAcademica apiAcademica,
        IClienteLlm clienteLlm,
        IBaseConocimiento baseConocimiento,
        IAuditoria auditoria,
        IReloj reloj,
        PoliticaAcceso politicaAcceso,
        VerificadorSalida verificadorSalida,
        LimitesAsistente limites)
    {
        _apiAcademica = apiAcademica;
        _clienteLlm = clienteLlm;
        _baseConocimiento = baseConocimiento;
        _auditoria = auditoria;
        _reloj = reloj;
        _politicaAcceso = politicaAcceso;
        _verificadorSalida = verificadorSalida;
        _limites = limites;
    }

    public Task<ResultadoPregunta> EjecutarAsync(
        PreguntaEntrada entrada,
        string ipOrigen,
        string correlationId,
        CancellationToken cancellationToken) => throw new NotImplementedException();
}
