using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Dobles;

public sealed class AuditoriaEnMemoria : IAuditoria
{
    private readonly ConcurrentQueue<EventoAuditoria> _eventos = new();
    private readonly ConcurrentQueue<EventoMultiplesIdentidades> _multiples = new();

    public IReadOnlyList<EventoAuditoria> Eventos => [.. _eventos];

    public IReadOnlyList<EventoMultiplesIdentidades> MultiplesIdentidades => [.. _multiples];

    public void Registrar(EventoAuditoria evento) => _eventos.Enqueue(evento);

    public void RegistrarMultiplesIdentidades(EventoMultiplesIdentidades evento) => _multiples.Enqueue(evento);
}

public sealed class RelojControlado : IReloj
{
    private DateTimeOffset _ahora = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    public DateTimeOffset AhoraUtc => _ahora;

    public void Avanzar(TimeSpan intervalo) => _ahora += intervalo;
}

public sealed class CapturaLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _mensajes = new();

    public IReadOnlyList<string> Mensajes => [.. _mensajes];

    public ILogger CreateLogger(string categoryName) => new Registro(_mensajes);

    public void Dispose()
    {
    }

    private sealed class Registro(ConcurrentQueue<string> destino) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            destino.Enqueue(formatter(state, exception) + exception);
        }
    }
}

public sealed class FiltroIpRemota : IStartupFilter
{
    public const string Encabezado = "X-Test-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use(async (contexto, siguiente) =>
            {
                if (contexto.Request.Headers.TryGetValue(Encabezado, out var ip) && IPAddress.TryParse(ip.ToString(), out var direccion))
                {
                    contexto.Connection.RemoteIpAddress = direccion;
                }

                await siguiente();
            });
            next(app);
        };
}
