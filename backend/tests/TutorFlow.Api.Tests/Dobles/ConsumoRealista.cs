using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Dobles;

public static class ConsumoRealista
{
    private const decimal CaracteresPorToken = 3.5m;
    private const decimal FactorReal = 1.04m;
    private const decimal TokensPorMillon = 1_000_000m;

    public static int Estimados(SolicitudLlm solicitud) =>
        EstimadosDeCaracteres(solicitud.PromptSistema.Length + solicitud.Mensajes.Sum(m => m.Contenido.Length));

    public static int EstimadosDeCaracteres(int caracteres) => (int)Math.Ceiling(caracteres / CaracteresPorToken);

    public static int Reportados(SolicitudLlm solicitud) => Reportados(Estimados(solicitud));

    public static int Reportados(int estimados) => (int)Math.Ceiling(FactorReal * estimados);

    public static int TokensDe(IEnumerable<SolicitudLlm> solicitudes, int salida) => solicitudes.Sum(s => Reportados(s) + salida);

    public static decimal CostoDe(int entrada, int salida, decimal precioEntrada, decimal precioSalida) =>
        (entrada * precioEntrada + salida * precioSalida) / TokensPorMillon;
}
