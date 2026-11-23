namespace TutorFlow.Core.Asistente;

public sealed class PresupuestoDiario
{
    private const int SegundosMinimosParaReintentar = 1;

    private readonly Lock _candado = new();
    private readonly IReloj _reloj;
    private readonly decimal? _tope;
    private DateOnly _dia;
    private decimal _gastado;
    private decimal _reservado;

    public PresupuestoDiario(IReloj reloj, LimitesAsistente limites)
    {
        _reloj = reloj;
        _tope = limites.PresupuestoDiarioUsd;
        _dia = DiaActual();
    }

    public bool TryReservar(decimal reservaUsd)
    {
        if (_tope is not { } tope)
        {
            return true;
        }

        lock (_candado)
        {
            ReiniciarSiCambioElDia();
            var reserva = Math.Min(reservaUsd, tope);
            if (_gastado + _reservado + reserva > tope)
            {
                return false;
            }

            _reservado += reserva;
            return true;
        }
    }

    public void Sustituir(decimal reservaUsd, decimal costoRealUsd)
    {
        if (_tope is not { } tope)
        {
            return;
        }

        lock (_candado)
        {
            var reserva = Math.Min(reservaUsd, tope);
            _reservado = Math.Max(0m, _reservado - reserva);
            ReiniciarSiCambioElDia();
            _gastado += costoRealUsd;
        }
    }

    public int SegundosHastaElReinicio()
    {
        var ahora = _reloj.AhoraUtc;
        var medianoche = new DateTimeOffset(DateOnly.FromDateTime(ahora.UtcDateTime).AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return Math.Max(SegundosMinimosParaReintentar, (int)Math.Ceiling((medianoche - ahora).TotalSeconds));
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
