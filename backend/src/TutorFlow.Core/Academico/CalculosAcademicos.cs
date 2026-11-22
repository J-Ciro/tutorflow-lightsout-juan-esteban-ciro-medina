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

    public static decimal? CalcularDefinitiva(NotasCorte cortes) => throw new NotImplementedException();

    public static decimal Redondear(decimal valor, int decimales) => throw new NotImplementedException();

    public static NotaAcumulada? CalcularNotaAcumulada(NotasCorte cortes) => throw new NotImplementedException();

    public static bool PierdePorInasistencia(Asistencia asistencia) => throw new NotImplementedException();

    public static EstadoAsistencia ObtenerEstadoAsistencia(Asistencia asistencia) => throw new NotImplementedException();

    public static int CalcularFaltasPermitidas(int clasesProgramadas) => throw new NotImplementedException();

    public static int? CalcularFaltasRestantes(Asistencia asistencia) => throw new NotImplementedException();

    public static bool PuedeHabilitar(decimal? definitiva, bool perdidaPorInasistencia) => throw new NotImplementedException();

    public static ResultadoAsignatura EvaluarAsignatura(Asignatura asignatura) => throw new NotImplementedException();

    public static decimal? CalcularPromedioPonderado(IReadOnlyList<ResultadoAsignatura> resultados) => throw new NotImplementedException();

    public static bool EstaEnPruebaAcademica(decimal? promedio) => throw new NotImplementedException();

    public static ElegibilidadBeca EvaluarBeca(IReadOnlyList<ResultadoAsignatura> resultados, decimal? promedio) => throw new NotImplementedException();

    public static ResumenEstudiante ResumirEstudiante(string estudianteId, IReadOnlyList<Asignatura> asignaturas) => throw new NotImplementedException();
}
