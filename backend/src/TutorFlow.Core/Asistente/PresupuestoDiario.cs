namespace TutorFlow.Core.Asistente;

public sealed class PresupuestoDiario
{
    private readonly Lock _candado = new();
    private readonly IReloj _reloj;
    private readonly LimitesAsistente _limites;
    private DateOnly _dia;
    private decimal _gastado;

    public PresupuestoDiario(IReloj reloj, LimitesAsistente limites)
    {
        _reloj = reloj;
        _limites = limites;
        _dia = DiaActual();
    }

    public bool Agotado
    {
        get
        {
            lock (_candado)
            {
                ReiniciarSiCambioElDia();
                return _gastado >= _limites.PresupuestoDiarioUsd;
            }
        }
    }

    public void Registrar(decimal costoUsd)
    {
        lock (_candado)
        {
            ReiniciarSiCambioElDia();
            _gastado += costoUsd;
        }
    }

    private DateOnly DiaActual() => DateOnly.FromDateTime(_reloj.AhoraUtc.UtcDateTime);

    private void ReiniciarSiCambioElDia()
    {
        var hoy = DiaActual();
        if (hoy == _dia)
        {
            return;
        }

        _dia = hoy;
        _gastado = 0m;
    }
}
