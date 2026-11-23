using TutorFlow.Core.Asistente;

namespace TutorFlow.Api;

internal static class EnlazadorDeLimites
{
    private static readonly string[] ClavesOpcionales = ["PreguntasPorMinutoPorIp", "PresupuestoDiarioUsd"];

    public static void Enlazar(LimitesAsistente limites, IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection(ServiciosTutorFlow.ClaveSeccionLimites);
        var sinTope = ClavesOpcionales
            .Where(clave => string.IsNullOrWhiteSpace(seccion[clave]))
            .Select(clave => new KeyValuePair<string, string?>(clave, string.Empty));
        new ConfigurationBuilder().AddConfiguration(seccion).AddInMemoryCollection(sinTope).Build().Bind(limites);
    }
}
