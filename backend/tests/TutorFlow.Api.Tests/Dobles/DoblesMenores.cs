using System.Threading.Channels;
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
    private readonly object _candado = new();
    private readonly List<TemporizadorControlado> _temporizadores = [];
    private readonly Channel<bool> _esperas = Channel.CreateUnbounded<bool>();
    private DateTimeOffset _ahora = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    public RelojControlado()
    {
        Tiempo = new TiempoDeRelojControlado(this);
    }

    public TimeProvider Tiempo { get; }

    public TimeSpan Transcurrido { get; private set; }

    public DateTimeOffset AhoraUtc => _ahora;

    public void Fijar(DateTimeOffset instante) => _ahora = instante;

    public void Avanzar(TimeSpan intervalo)
    {
        var meta = Transcurrido + intervalo;
        while (TomarVencido(meta) is { } temporizador)
        {
            MoverA(temporizador.Vence);
            temporizador.Disparar();
        }

        MoverA(meta);
    }

    public Task Pausar(TimeSpan duracion, CancellationToken cancellationToken)
    {
        var pausa = Task.Delay(duracion, Tiempo, cancellationToken);
        _esperas.Writer.TryWrite(true);
        return pausa;
    }

    public async Task<T> AvanzarHastaQueTermine<T>(Task<T> peticion)
    {
        using var fin = new CancellationTokenSource();
        while (!peticion.IsCompleted)
        {
            var espera = _esperas.Reader.ReadAsync(fin.Token).AsTask();
            if (await Task.WhenAny(peticion, espera) == peticion)
            {
                break;
            }

            AvanzarAlSiguienteTemporizador();
        }

        await fin.CancelAsync();
        return await peticion;
    }

    internal ITimer Crear(TimerCallback callback, object? state, TimeSpan vence, TimeSpan periodo)
    {
        var temporizador = new TemporizadorControlado(this, callback, state);
        temporizador.Change(vence, periodo);
        return temporizador;
    }

    internal void Programar(TemporizadorControlado temporizador)
    {
        lock (_candado)
        {
            if (!_temporizadores.Contains(temporizador))
            {
                _temporizadores.Add(temporizador);
            }
        }
    }

    internal void Retirar(TemporizadorControlado temporizador)
    {
        lock (_candado)
        {
            _temporizadores.Remove(temporizador);
        }
    }

    private void AvanzarAlSiguienteTemporizador()
    {
        TimeSpan? siguiente;
        lock (_candado)
        {
            siguiente = _temporizadores.Count == 0 ? null : _temporizadores.Min(t => t.Vence);
        }

        if (siguiente is { } vence)
        {
            Avanzar(vence - Transcurrido);
        }
    }

    private TemporizadorControlado? TomarVencido(TimeSpan meta)
    {
        lock (_candado)
        {
            return _temporizadores.Where(t => t.Vence <= meta).MinBy(t => t.Vence);
        }
    }

    private void MoverA(TimeSpan destino)
    {
        _ahora += destino - Transcurrido;
        Transcurrido = destino;
    }
}

public sealed class TemporizadorControlado(RelojControlado reloj, TimerCallback callback, object? state) : ITimer
{
    private TimeSpan _periodo;

    public TimeSpan Vence { get; private set; }

    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        reloj.Retirar(this);
        if (dueTime < TimeSpan.Zero)
        {
            return true;
        }

        _periodo = period;
        Vence = reloj.Transcurrido + dueTime;
        reloj.Programar(this);
        return true;
    }

    public void Disparar()
    {
        if (_periodo > TimeSpan.Zero)
        {
            Vence += _periodo;
        }
        else
        {
            reloj.Retirar(this);
        }

        callback(state);
    }

    public void Dispose() => reloj.Retirar(this);

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class TiempoDeRelojControlado(RelojControlado reloj) : TimeProvider
{
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override DateTimeOffset GetUtcNow() => reloj.AhoraUtc;

    public override long GetTimestamp() => reloj.Transcurrido.Ticks;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        reloj.Crear(callback, state, dueTime, period);
}

public sealed record EntradaDeLog(LogLevel Nivel, string Mensaje, string? Excepcion);

public sealed class CapturaLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _mensajes = new();
    private readonly ConcurrentQueue<EntradaDeLog> _entradas = new();

    public IReadOnlyList<string> Mensajes => [.. _mensajes];

    public IReadOnlyList<EntradaDeLog> Entradas => [.. _entradas];

    public ILogger CreateLogger(string categoryName) => new Registro(_mensajes, _entradas);

    public void Dispose()
    {
    }

    private sealed class Registro(ConcurrentQueue<string> destino, ConcurrentQueue<EntradaDeLog> entradas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            destino.Enqueue(formatter(state, exception) + exception);
            entradas.Enqueue(new EntradaDeLog(logLevel, formatter(state, exception), exception?.ToString()));
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
