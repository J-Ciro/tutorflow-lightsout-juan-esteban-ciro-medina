using TutorFlow.Core.Asistente;

namespace TutorFlow.Api;

internal static class EnlazadorDeLimites
{
    private static readonly string[] ClavesOpcionales = ["PreguntasPorMinutoPorIp", "PresupuestoDiarioUsd"];

    public static void Enlazar(LimitesAsistente limites, IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection(ServiciosTutorFlow.ClaveSeccionLimites);
        var conValor = seccion.AsEnumerable(makePathsRelative: true)
            .Where(par => !(ClavesOpcionales.Contains(par.Key) && string.IsNullOrWhiteSpace(par.Value)));
        new ConfigurationBuilder().AddInMemoryCollection(conValor).Build().Bind(limites);
    }
}
