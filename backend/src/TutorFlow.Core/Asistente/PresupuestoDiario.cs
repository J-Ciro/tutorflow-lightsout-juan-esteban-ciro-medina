namespace TutorFlow.Core.Asistente;

public sealed class PresupuestoDiario
{
    private readonly Lock _candado = new();
    private readonly IReloj _reloj;
    private readonly LimitesAsistente _limites;
    private DateOnly _dia;
    private decimal _gastado;
    private decimal _reservado;

    public PresupuestoDiario(IReloj reloj, LimitesAsistente limites)
    {
        _reloj = reloj;
        _limites = limites;
        _dia = DiaActual();
    }

    public bool TryReservar(decimal reservaUsd)
    {
        lock (_candado)
        {
            ReiniciarSiCambioElDia();
            var reserva = Math.Min(reservaUsd, _limites.PresupuestoDiarioUsd);
            if (_gastado + _reservado + reserva > _limites.PresupuestoDiarioUsd)
            {
                return false;
            }

            _reservado += reserva;
            return true;
        }
    }

    public void Sustituir(decimal reservaUsd, decimal costoRealUsd)
    {
        lock (_candado)
        {
            var reserva = Math.Min(reservaUsd, _limites.PresupuestoDiarioUsd);
            _reservado = Math.Max(0m, _reservado - reserva);
            ReiniciarSiCambioElDia();
            _gastado += costoRealUsd;
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
