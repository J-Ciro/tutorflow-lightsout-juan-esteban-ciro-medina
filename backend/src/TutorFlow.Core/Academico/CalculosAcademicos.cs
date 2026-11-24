namespace TutorFlow.Core.Academico;

public static class CalculosAcademicos
{
    public const decimal PorcentajeMaximoInasistencia = 15m;
    public const decimal NotaMinimaAprobacion = 3.0m;
    public const decimal NotaMinimaHabilitacion = 2.0m;
    public const decimal NotaMaximaHabilitacion = 3.0m;
    public const decimal PromedioMinimoBeca = 4.3m;
    public const decimal PromedioPruebaAcademica = 3.0m;
    public const int CreditosMinimosBeca = 12;
    public const decimal PesoCorte1 = 0.3m;
    public const decimal PesoCorte2 = 0.3m;
    public const decimal PesoCorte3 = 0.4m;
    public const decimal NotaMinima = 0.0m;
    public const decimal NotaMaxima = 5.0m;
    public const int DecimalesDefinitiva = 1;
    public const int DecimalesPromedio = 2;
    public const int BasePorcentual = 100;

    public static decimal? CalcularDefinitiva(NotasCorte cortes)
    {
        if (cortes.Corte1 is not { } corte1 || cortes.Corte2 is not { } corte2 || cortes.Corte3 is not { } corte3)
        {
            return null;
        }

        var sinRedondear = PesoCorte1 * corte1 + PesoCorte2 * corte2 + PesoCorte3 * corte3;
        return Redondear(sinRedondear, DecimalesDefinitiva);
    }

    public static decimal Redondear(decimal valor, int decimales) =>
        Math.Round(valor, decimales, MidpointRounding.AwayFromZero);

    private static List<(decimal Peso, decimal Corte)> AportesEvaluados(NotasCorte cortes) =>
        new (decimal Peso, decimal? Corte)[]
        {
            (PesoCorte1, cortes.Corte1),
            (PesoCorte2, cortes.Corte2),
            (PesoCorte3, cortes.Corte3)
        }.Where(aporte => aporte.Corte is not null).Select(aporte => (aporte.Peso, aporte.Corte.GetValueOrDefault())).ToList();

    public static NotaAcumulada? CalcularNotaAcumulada(NotasCorte cortes)
    {
        var aportes = AportesEvaluados(cortes);
        if (aportes.Count == 0)
        {
            return null;
        }

        var pesoEvaluado = aportes.Sum(aporte => aporte.Peso);
        var acumulada = aportes.Sum(aporte => aporte.Peso * aporte.Corte);
        return new NotaAcumulada(
            Redondear(acumulada, DecimalesDefinitiva),
            pesoEvaluado * BasePorcentual,
            Redondear(acumulada / pesoEvaluado, DecimalesDefinitiva));
    }

    public static bool PierdePorInasistencia(Asistencia asistencia) =>
        asistencia.Inasistencias is { } faltas
        && faltas * BasePorcentual > PorcentajeMaximoInasistencia * asistencia.ClasesProgramadas;

    public static EstadoAsistencia ObtenerEstadoAsistencia(Asistencia asistencia) =>
        asistencia.Inasistencias.HasValue ? EstadoAsistencia.Registrada : EstadoAsistencia.SinRegistro;

    public static int CalcularFaltasPermitidas(int clasesProgramadas) =>
        clasesProgramadas * (int)PorcentajeMaximoInasistencia / BasePorcentual;

    public static int? CalcularFaltasRestantes(Asistencia asistencia) =>
        asistencia.Inasistencias is { } faltas
            ? Math.Max(0, CalcularFaltasPermitidas(asistencia.ClasesProgramadas) - faltas)
            : null;

    public static bool PuedeHabilitar(decimal? definitiva, bool perdidaPorInasistencia) =>
        !perdidaPorInasistencia
        && definitiva is { } nota
        && nota >= NotaMinimaHabilitacion
        && nota < NotaMaximaHabilitacion;

    public static ResultadoAsignatura EvaluarAsignatura(Asignatura asignatura)
    {
        var perdidaPorInasistencia = PierdePorInasistencia(asignatura.Asistencia);
        var definitiva = perdidaPorInasistencia ? NotaMinima : CalcularDefinitiva(asignatura.Cortes);
        var estado = DeterminarEstado(perdidaPorInasistencia, definitiva);
        return new ResultadoAsignatura(
            asignatura,
            definitiva,
            estado,
            PuedeHabilitar(definitiva, perdidaPorInasistencia),
            ObtenerEstadoAsistencia(asignatura.Asistencia),
            CalcularFaltasPermitidas(asignatura.Asistencia.ClasesProgramadas),
            CalcularFaltasRestantes(asignatura.Asistencia),
            estado == EstadoAsignatura.SinDefinitiva ? CalcularNotaAcumulada(asignatura.Cortes) : null);
    }

    public static decimal? CalcularPromedioPonderado(IReadOnlyList<ResultadoAsignatura> resultados)
    {
        var conDefinitiva = resultados
            .Where(resultado => resultado.Definitiva.HasValue)
            .Select(resultado => (Definitiva: resultado.Definitiva.GetValueOrDefault(), resultado.Asignatura.Creditos))
            .ToList();
        var creditos = conDefinitiva.Sum(resultado => resultado.Creditos);
        if (creditos == 0)
        {
            return null;
        }

        var puntos = conDefinitiva.Sum(resultado => resultado.Definitiva * resultado.Creditos);
        return Redondear(puntos / creditos, DecimalesPromedio);
    }

    public static bool EstaEnPruebaAcademica(decimal? promedio) =>
        promedio is { } valor && valor < PromedioPruebaAcademica;

    public static ElegibilidadBeca EvaluarBeca(IReadOnlyList<ResultadoAsignatura> resultados, decimal? promedio)
    {
        if (resultados.Any(EstaPerdida))
        {
            return ElegibilidadBeca.NoElegible;
        }

        if (resultados.Any(resultado => resultado.Estado == EstadoAsignatura.SinDefinitiva))
        {
            return ElegibilidadBeca.NoDeterminable;
        }

        var creditos = resultados.Sum(resultado => resultado.Asignatura.Creditos);
        return promedio >= PromedioMinimoBeca && creditos >= CreditosMinimosBeca
            ? ElegibilidadBeca.Elegible
            : ElegibilidadBeca.NoElegible;
    }

    public static ResumenEstudiante ResumirEstudiante(string estudianteId, IReadOnlyList<Asignatura> asignaturas)
    {
        var resultados = asignaturas.Select(EvaluarAsignatura).ToList();
        var promedio = CalcularPromedioPonderado(resultados);
        return new ResumenEstudiante(
            estudianteId,
            resultados,
            promedio,
            EstaEnPruebaAcademica(promedio),
            resultados.Sum(resultado => resultado.Asignatura.Creditos),
            EvaluarBeca(resultados, promedio));
    }

    private static bool EstaPerdida(ResultadoAsignatura resultado) =>
        resultado.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia;

    private static EstadoAsignatura DeterminarEstado(bool perdidaPorInasistencia, decimal? definitiva) =>
        (perdidaPorInasistencia, definitiva) switch
        {
            (true, _) => EstadoAsignatura.PerdidaPorInasistencia,
            (_, null) => EstadoAsignatura.SinDefinitiva,
            (_, { } nota) when nota >= NotaMinimaAprobacion => EstadoAsignatura.Aprobada,
            _ => EstadoAsignatura.Perdida
        };
}
