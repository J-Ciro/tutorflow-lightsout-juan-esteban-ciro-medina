namespace TutorFlow.Core.Asistente;

public sealed class LimitesAsistente
{
    public int MaximoCaracteresPregunta { get; init; } = 1000;
    public int MaximoBytesCuerpo { get; init; } = 16 * 1024;
    public int? PreguntasPorMinutoPorIp { get; init; }
    public int MaximoSeccionesRecuperadas { get; init; } = 6;
    public int MaximoTokensSalida { get; init; } = 2000;
    public int MaximoInvocacionesHerramientas { get; init; } = 4;
    public int TimeoutLlmSegundos { get; init; } = 20;
    public int PresupuestoApiAcademicaSegundos { get; init; } = 6;
    public decimal? PresupuestoDiarioUsd { get; init; }
    public int MaximoIdentidadesPorIp { get; init; } = 3;
    public int VentanaIdentidadesMinutos { get; init; } = 10;
    public bool BloquearMultiplesIdentidades { get; init; }
}
