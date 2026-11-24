namespace TutorFlow.Api;

public static class EncabezadosDeSeguridad
{
    private static readonly (string Nombre, string Valor)[] Encabezados =
    [
        ("X-Content-Type-Options", "nosniff"),
        ("X-Frame-Options", "DENY"),
        ("Referrer-Policy", "no-referrer")
    ];

    public static IApplicationBuilder UsarEncabezadosDeSeguridad(this IApplicationBuilder aplicacion) =>
        aplicacion.Use(async (contexto, siguiente) =>
        {
            contexto.Response.OnStarting(() =>
            {
                foreach (var (nombre, valor) in Encabezados)
                {
                    contexto.Response.Headers[nombre] = valor;
                }

                return Task.CompletedTask;
            });
            await siguiente();
        });
}
