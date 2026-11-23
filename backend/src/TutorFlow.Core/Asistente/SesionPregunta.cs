using System.Diagnostics;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed class SesionPregunta(
    PreguntaEntrada entrada,
    string ipOrigen,
    string correlationId,
    ApiAcademicaConPresupuesto api,
    DirectorioAcademico directorio)
{
    public PreguntaEntrada Entrada { get; } = entrada;

    public string IpOrigen { get; } = ipOrigen;

    public string CorrelationId { get; } = correlationId;

    public ApiAcademicaConPresupuesto Api { get; } = api;

    public Stopwatch Cronometro { get; } = Stopwatch.StartNew();

    public RegistroConsultas Registro { get; } = new();

    public DirectorioAcademico Directorio { get; } = directorio;

    public UsoAcumulado Uso { get; } = new();
}
