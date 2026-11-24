using Microsoft.Extensions.Http;

namespace TutorFlow.Api;

public sealed class SinRedireccionesLlm(string nombreCliente) : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
        constructor =>
        {
            next(constructor);
            if (constructor.Name == nombreCliente)
            {
                DesactivarRedirecciones(constructor.PrimaryHandler);
            }
        };

    private static void DesactivarRedirecciones(HttpMessageHandler manejador)
    {
        switch (manejador)
        {
            case SocketsHttpHandler sockets:
                sockets.AllowAutoRedirect = false;
                sockets.UseCookies = false;
                break;
            case HttpClientHandler cliente:
                cliente.AllowAutoRedirect = false;
                cliente.UseCookies = false;
                break;
        }
    }
}
