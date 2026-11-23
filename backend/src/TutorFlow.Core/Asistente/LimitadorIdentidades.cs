namespace TutorFlow.Core.Asistente;

public sealed record ResultadoIdentidades(bool Superado, bool SuperacionNueva, IReadOnlyList<string> Ids);

public sealed class LimitadorIdentidades
{
    private const int UmbralDeLimpieza = 10_000;

    private readonly Lock _candado = new();
    private readonly Dictionary<string, List<(DateTimeOffset Momento, string Id)>> _porIp = [];
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
            var entradas = ObtenerVigentes(ipOrigen, ahora);
            var antes = entradas.Select(e => e.Id).Distinct().Count();
            entradas.Add((ahora, usuarioId));
            var ids = entradas.Select(e => e.Id).Distinct().ToList();
            var superado = ids.Count > _limites.MaximoIdentidadesPorIp;
            return new ResultadoIdentidades(superado, superado && antes <= _limites.MaximoIdentidadesPorIp, ids);
        }
    }

    private List<(DateTimeOffset Momento, string Id)> ObtenerVigentes(string ipOrigen, DateTimeOffset ahora)
    {
        if (!_porIp.TryGetValue(ipOrigen, out var entradas))
        {
            entradas = [];
            _porIp[ipOrigen] = entradas;
        }

        entradas.RemoveAll(e => ahora - e.Momento > Ventana);
        return entradas;
    }

    private void LimpiarSiHaceFalta(DateTimeOffset ahora)
    {
        if (_porIp.Count < UmbralDeLimpieza)
        {
            return;
        }

        foreach (var ip in _porIp.Keys.ToList())
        {
            _porIp[ip].RemoveAll(e => ahora - e.Momento > Ventana);
            if (_porIp[ip].Count == 0)
            {
                _porIp.Remove(ip);
            }
        }
    }
}
