namespace TutorFlow.Api;

public sealed record ResultadoTasa(bool Permitido, int SegundosParaReintentar, bool IniciaBloqueo = false);

public sealed class LimitadorTasaPorIp
{
    private const int UmbralDePurga = 1_000;
    private const int SegundosEntrePurgas = 10;
    private const int SegundosDeVentana = 60;
    private const int SegundosDeReinicio = 600;
    private const int SegundosDelPrimerBloqueo = 30;
    private const int NivelMaximo = 3;

    private static readonly TimeSpan Ventana = TimeSpan.FromSeconds(SegundosDeVentana);
    private static readonly TimeSpan Reinicio = TimeSpan.FromSeconds(SegundosDeReinicio);
    private static readonly TimeSpan EntrePurgas = TimeSpan.FromSeconds(SegundosEntrePurgas);

    private readonly Lock _candado = new();
    private readonly Dictionary<string, EstadoDeIp> _porIp = [];
    private readonly int _limite;
    private readonly TimeProvider _tiempo;
    private long? _ultimaPurga;

    public LimitadorTasaPorIp(int limite, TimeProvider tiempo)
    {
        _limite = limite;
        _tiempo = tiempo;
    }

    public ResultadoTasa Intentar(string ip)
    {
        lock (_candado)
        {
            var ahora = _tiempo.GetTimestamp();
            PurgarSiHaceFalta(ahora);
            if (!_porIp.TryGetValue(ip, out var estado))
            {
                estado = new EstadoDeIp();
                _porIp[ip] = estado;
            }

            return Decidir(estado, ahora);
        }
    }

    private ResultadoTasa Decidir(EstadoDeIp estado, long ahora)
    {
        if (estado.EstaBloqueada(_tiempo, ahora))
        {
            return new ResultadoTasa(false, estado.SegundosRestantes(_tiempo, ahora));
        }

        estado.DescartarMarcasFueraDeVentana(_tiempo, ahora);
        if (estado.Marcas.Count < _limite)
        {
            estado.Marcas.Enqueue(ahora);
            return new ResultadoTasa(true, 0);
        }

        return Bloquear(estado, ahora);
    }

    private ResultadoTasa Bloquear(EstadoDeIp estado, long ahora)
    {
        estado.Marcas.Clear();
        var reinicia = estado.TiempoDesdeElFinDelUltimoBloqueo(_tiempo, ahora) is not { } transcurrido || transcurrido >= Reinicio;
        estado.Nivel = reinicia ? 1 : Math.Min(estado.Nivel + 1, NivelMaximo);
        var duracion = SegundosDelPrimerBloqueo << (estado.Nivel - 1);
        estado.IniciarBloqueo(ahora, TimeSpan.FromSeconds(duracion));
        return new ResultadoTasa(false, duracion, true);
    }

    private void PurgarSiHaceFalta(long ahora)
    {
        if (_porIp.Count < UmbralDePurga || !HayQuePurgar(ahora))
        {
            return;
        }

        _ultimaPurga = ahora;
        foreach (var (ip, estado) in _porIp)
        {
            if (estado.EstaInactivo(_tiempo, ahora))
            {
                _porIp.Remove(ip);
            }
        }
    }

    private bool HayQuePurgar(long ahora) =>
        _ultimaPurga is not { } anterior || _tiempo.GetElapsedTime(anterior, ahora) >= EntrePurgas;

    private sealed class EstadoDeIp
    {
        private long? _inicioDelBloqueo;
        private TimeSpan _duracionDelBloqueo;

        public Queue<long> Marcas { get; } = new();

        public int Nivel { get; set; }

        public void IniciarBloqueo(long ahora, TimeSpan duracion)
        {
            _inicioDelBloqueo = ahora;
            _duracionDelBloqueo = duracion;
        }

        public bool EstaBloqueada(TimeProvider tiempo, long ahora) =>
            _inicioDelBloqueo is { } inicio && tiempo.GetElapsedTime(inicio, ahora) < _duracionDelBloqueo;

        public int SegundosRestantes(TimeProvider tiempo, long ahora) =>
            Math.Max(1, (int)Math.Ceiling((_duracionDelBloqueo - tiempo.GetElapsedTime(_inicioDelBloqueo!.Value, ahora)).TotalSeconds));

        public TimeSpan? TiempoDesdeElFinDelUltimoBloqueo(TimeProvider tiempo, long ahora) =>
            _inicioDelBloqueo is { } inicio ? tiempo.GetElapsedTime(inicio, ahora) - _duracionDelBloqueo : null;

        public void DescartarMarcasFueraDeVentana(TimeProvider tiempo, long ahora)
        {
            while (Marcas.Count > 0 && tiempo.GetElapsedTime(Marcas.Peek(), ahora) >= Ventana)
            {
                Marcas.Dequeue();
            }
        }

        public bool EstaInactivo(TimeProvider tiempo, long ahora)
        {
            DescartarMarcasFueraDeVentana(tiempo, ahora);
            var desdeElFin = TiempoDesdeElFinDelUltimoBloqueo(tiempo, ahora);
            return Marcas.Count == 0 && (desdeElFin is null || desdeElFin >= Reinicio);
        }
    }
}
