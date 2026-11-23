using System.Collections.Concurrent;

namespace TutorFlow.Api;

public sealed record ResultadoTasa(bool Permitido, int SegundosParaReintentar);

public sealed class LimitadorTasaPorIp
{
    private const int UmbralDeLimpieza = 10_000;

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _porIp = new();
    private readonly int? _limite;
    private readonly TimeSpan _ventana;
    private readonly TimeProvider _tiempo;

    public LimitadorTasaPorIp(int? limite, TimeSpan ventana, TimeProvider tiempo)
    {
        _limite = limite;
        _ventana = ventana;
        _tiempo = tiempo;
    }

    public ResultadoTasa Intentar(string ip)
    {
        if (_limite is not { } limite)
        {
            return new ResultadoTasa(true, 0);
        }

        var ahora = _tiempo.GetUtcNow();
        LimpiarSiHaceFalta(ahora);
        var cola = _porIp.GetOrAdd(ip, _ => new Queue<DateTimeOffset>());
        lock (cola)
        {
            Purgar(cola, ahora);
            if (cola.Count < limite)
            {
                cola.Enqueue(ahora);
                return new ResultadoTasa(true, 0);
            }

            return new ResultadoTasa(false, SegundosHastaLiberar(cola.Peek(), ahora));
        }
    }

    private int SegundosHastaLiberar(DateTimeOffset masAntigua, DateTimeOffset ahora) =>
        Math.Max(1, (int)Math.Ceiling((masAntigua + _ventana - ahora).TotalSeconds));

    private void Purgar(Queue<DateTimeOffset> cola, DateTimeOffset ahora)
    {
        while (cola.Count > 0 && ahora - cola.Peek() >= _ventana)
        {
            cola.Dequeue();
        }
    }

    private void LimpiarSiHaceFalta(DateTimeOffset ahora)
    {
        if (_porIp.Count < UmbralDeLimpieza)
        {
            return;
        }

        foreach (var (ip, cola) in _porIp)
        {
            lock (cola)
            {
                Purgar(cola, ahora);
                if (cola.Count == 0)
                {
                    _porIp.TryRemove(ip, out _);
                }
            }
        }
    }
}
