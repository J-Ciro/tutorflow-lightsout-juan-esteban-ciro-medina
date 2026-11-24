namespace TutorFlow.Core.Asistente;

public sealed record ResultadoIdentidades(bool SuperacionNueva, IReadOnlyList<string> Ids);

public sealed class LimitadorIdentidades
{
    private const int UmbralDeLimpieza = 1_000;
    private const int SegundosEntreLimpiezas = 10;

    private static readonly TimeSpan EntreLimpiezas = TimeSpan.FromSeconds(SegundosEntreLimpiezas);

    private readonly Lock _candado = new();
    private readonly Dictionary<string, VentanaDeIp> _porIp = [];
    private readonly IReloj _reloj;
    private readonly LimitesAsistente _limites;
    private DateTimeOffset? _ultimaLimpieza;

    public LimitadorIdentidades(IReloj reloj, LimitesAsistente limites)
    {
        _reloj = reloj;
        _limites = limites;
    }

    private TimeSpan Ventana => TimeSpan.FromMinutes(_limites.VentanaIdentidadesMinutos);

    public ResultadoIdentidades Registrar(string ipOrigen, string usuarioId)
    {
        lock (_candado)
        {
            var ahora = _reloj.AhoraUtc;
            LimpiarSiHaceFalta(ahora);
            var ventana = ObtenerVigente(ipOrigen, ahora);
            return ventana.Contiene(usuarioId)
                ? Renovar(ventana, usuarioId, ahora)
                : Admitir(ventana, usuarioId, ahora);
        }
    }

    private static ResultadoIdentidades Renovar(VentanaDeIp ventana, string usuarioId, DateTimeOffset ahora)
    {
        ventana.Registrar(usuarioId, ahora);
        return new ResultadoIdentidades(false, ventana.Ids);
    }

    private ResultadoIdentidades Admitir(VentanaDeIp ventana, string usuarioId, DateTimeOffset ahora)
    {
        var superada = ventana.Entradas.Count >= _limites.MaximoIdentidadesPorIp;
        ventana.Registrar(usuarioId, ahora);
        return new ResultadoIdentidades(superada, ventana.Ids);
    }

    private VentanaDeIp ObtenerVigente(string ipOrigen, DateTimeOffset ahora)
    {
        if (!_porIp.TryGetValue(ipOrigen, out var ventana))
        {
            ventana = new VentanaDeIp();
            _porIp[ipOrigen] = ventana;
        }

        ventana.Purgar(ahora, Ventana);
        return ventana;
    }

    private void LimpiarSiHaceFalta(DateTimeOffset ahora)
    {
        if (_porIp.Count < UmbralDeLimpieza || !HayQueLimpiar(ahora))
        {
            return;
        }

        _ultimaLimpieza = ahora;
        foreach (var (ip, ventana) in _porIp)
        {
            ventana.Purgar(ahora, Ventana);
            if (ventana.EstaVacia)
            {
                _porIp.Remove(ip);
            }
        }
    }

    private bool HayQueLimpiar(DateTimeOffset ahora) =>
        _ultimaLimpieza is not { } anterior || ahora - anterior >= EntreLimpiezas;

    private sealed class VentanaDeIp
    {
        public List<(DateTimeOffset Momento, string Id)> Entradas { get; } = [];

        public bool EstaVacia => Entradas.Count == 0;

        public IReadOnlyList<string> Ids => [.. Entradas.Select(e => e.Id)];

        public bool Contiene(string usuarioId) => Entradas.Exists(e => e.Id == usuarioId);

        public void Registrar(string usuarioId, DateTimeOffset ahora)
        {
            Entradas.RemoveAll(e => e.Id == usuarioId);
            Entradas.Add((ahora, usuarioId));
        }

        public void Purgar(DateTimeOffset ahora, TimeSpan ventana) =>
            Entradas.RemoveAll(e => ahora - e.Momento >= ventana);
    }
}
