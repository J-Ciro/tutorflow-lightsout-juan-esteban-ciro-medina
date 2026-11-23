using System.Diagnostics;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class SesionPregunta(
    PreguntaEntrada entrada,
    string ipOrigen,
    string correlationId,
    ApiAcademicaConPresupuesto api)
{
    public PreguntaEntrada Entrada { get; } = entrada;

    public string IpOrigen { get; } = ipOrigen;

    public string CorrelationId { get; } = correlationId;

    public ApiAcademicaConPresupuesto Api { get; } = api;

    public Stopwatch Cronometro { get; } = Stopwatch.StartNew();

    public RegistroConsultas Registro { get; } = new();

    public DirectorioAcademico Directorio { get; set; } = ConstructorDirectorio.Vacio;

    public UsoAcumulado Uso { get; } = new();

    public bool ReservaHecha { get; set; }

    public IReadOnlyList<SeccionDocumento> Secciones { get; set; } = [];

    public ActivacionResumen Activacion { get; set; } = ActivacionResumen.Ninguna;
}
