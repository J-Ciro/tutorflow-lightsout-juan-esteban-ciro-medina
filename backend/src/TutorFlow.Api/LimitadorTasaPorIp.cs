namespace TutorFlow.Api;

public sealed record ResultadoTasa(bool Permitido, int SegundosParaReintentar);

public sealed class LimitadorTasaPorIp
{
    private const int UmbralDeLimpieza = 10_000;
    private const int SegundosDeVentana = 60;
    private const int SegundosDeReinicio = 600;
    private const int SegundosDelPrimerBloqueo = 30;
    private const int NivelMaximo = 3;

    private readonly Lock _candado = new();
    private readonly Dictionary<string, EstadoDeIp> _porIp = [];
    private readonly int _limite;
    private readonly TimeProvider _tiempo;

    public LimitadorTasaPorIp(int limite, TimeProvider tiempo)
    {
        _limite = limite;
        _tiempo = tiempo;
    }

    public ResultadoTasa Intentar(string ip)
    {
        lock (_candado)
        {
            var ahora = _tiempo.GetUtcNow();
            PurgarSiHaceFalta(ahora);
            if (!_porIp.TryGetValue(ip, out var estado))
            {
                estado = new EstadoDeIp();
                _porIp[ip] = estado;
            }

            return Decidir(estado, ahora);
        }
    }

    private ResultadoTasa Decidir(EstadoDeIp estado, DateTimeOffset ahora)
    {
        if (ahora < estado.BloqueadaHasta)
        {
            return Denegado(SegundosRestantes(estado.BloqueadaHasta, ahora));
        }

        estado.DescartarMarcasFueraDeVentana(ahora);
        if (estado.Marcas.Count < _limite)
        {
            estado.Marcas.Enqueue(ahora);
            return new ResultadoTasa(true, 0);
        }

        return Bloquear(estado, ahora);
    }

    private static ResultadoTasa Bloquear(EstadoDeIp estado, DateTimeOffset ahora)
    {
        estado.Marcas.Clear();
        var reinicia = estado.FinUltimoBloqueo is not { } fin || (ahora - fin).TotalSeconds >= SegundosDeReinicio;
        estado.Nivel = reinicia ? 1 : Math.Min(estado.Nivel + 1, NivelMaximo);
        var duracion = SegundosDelPrimerBloqueo << (estado.Nivel - 1);
        estado.BloqueadaHasta = ahora.AddSeconds(duracion);
        estado.FinUltimoBloqueo = estado.BloqueadaHasta;
        return Denegado(duracion);
    }

    private static ResultadoTasa Denegado(int segundos) => new(false, segundos);

    private static int SegundosRestantes(DateTimeOffset hasta, DateTimeOffset ahora) =>
        Math.Max(1, (int)Math.Ceiling((hasta - ahora).TotalSeconds));

    private void PurgarSiHaceFalta(DateTimeOffset ahora)
    {
        if (_porIp.Count < UmbralDeLimpieza)
        {
            return;
        }

        foreach (var (ip, estado) in _porIp.ToList())
        {
            if (estado.EstaInactivo(ahora))
            {
                _porIp.Remove(ip);
            }
        }
    }

    private sealed class EstadoDeIp
    {
        public Queue<DateTimeOffset> Marcas { get; } = new();

        public int Nivel { get; set; }

        public DateTimeOffset BloqueadaHasta { get; set; }

        public DateTimeOffset? FinUltimoBloqueo { get; set; }

        public void DescartarMarcasFueraDeVentana(DateTimeOffset ahora)
        {
            while (Marcas.Count > 0 && (ahora - Marcas.Peek()).TotalSeconds >= SegundosDeVentana)
            {
                Marcas.Dequeue();
            }
        }

        public bool EstaInactivo(DateTimeOffset ahora)
        {
            DescartarMarcasFueraDeVentana(ahora);
            var sinBloqueoReciente = FinUltimoBloqueo is not { } fin || (ahora - fin).TotalSeconds >= SegundosDeReinicio;
            return Marcas.Count == 0 && sinBloqueoReciente;
        }
    }
}
