namespace TutorFlow.Core.Asistente;

public sealed record ResultadoIdentidades(bool Superado, bool SuperacionNueva, IReadOnlyList<string> Ids)
{
    public int SegundosParaReintentar { get; init; }
}

public sealed class LimitadorIdentidades
{
    private const int UmbralDeLimpieza = 10_000;

    private readonly Lock _candado = new();
    private readonly Dictionary<string, VentanaDeIp> _porIp = [];
    private readonly IReloj _reloj;
    private readonly LimitesAsistente _limites;

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
        return new ResultadoIdentidades(false, false, ventana.Ids);
    }

    private ResultadoIdentidades Admitir(VentanaDeIp ventana, string usuarioId, DateTimeOffset ahora)
    {
        if (ventana.Entradas.Count < _limites.MaximoIdentidadesPorIp)
        {
            ventana.Registrar(usuarioId, ahora);
            return new ResultadoIdentidades(false, false, ventana.Ids);
        }

        var segundos = SegundosHastaVencer(ventana.Entradas[0].Momento, ahora);
        var nueva = !ventana.Reportes.ContainsKey(usuarioId);
        IReadOnlyList<string> ids = [.. ventana.Ids, usuarioId];
        if (_limites.BloquearMultiplesIdentidades)
        {
            ventana.Reportes.TryAdd(usuarioId, ahora);
        }
        else
        {
            ventana.Registrar(usuarioId, ahora);
        }

        return new ResultadoIdentidades(true, nueva, ids) { SegundosParaReintentar = segundos };
    }

    private int SegundosHastaVencer(DateTimeOffset masAntigua, DateTimeOffset ahora) =>
        Math.Max(1, (int)Math.Ceiling((masAntigua + Ventana - ahora).TotalSeconds));

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
        if (_porIp.Count < UmbralDeLimpieza)
        {
            return;
        }

        foreach (var (ip, ventana) in _porIp.ToList())
        {
            ventana.Purgar(ahora, Ventana);
            if (ventana.EstaVacia)
            {
                _porIp.Remove(ip);
            }
        }
    }

    private sealed class VentanaDeIp
    {
        public List<(DateTimeOffset Momento, string Id)> Entradas { get; } = [];

        public Dictionary<string, DateTimeOffset> Reportes { get; } = [];

        public bool EstaVacia => Entradas.Count == 0 && Reportes.Count == 0;

        public IReadOnlyList<string> Ids => [.. Entradas.Select(e => e.Id)];

        public bool Contiene(string usuarioId) => Entradas.Exists(e => e.Id == usuarioId);

        public void Registrar(string usuarioId, DateTimeOffset ahora)
        {
            Entradas.RemoveAll(e => e.Id == usuarioId);
            Entradas.Add((ahora, usuarioId));
            Reportes.Remove(usuarioId);
        }

        public void Purgar(DateTimeOffset ahora, TimeSpan ventana)
        {
            Entradas.RemoveAll(e => ahora - e.Momento >= ventana);
            foreach (var id in Reportes.Where(r => ahora - r.Value >= ventana).Select(r => r.Key).ToList())
            {
                Reportes.Remove(id);
            }
        }
    }
}
