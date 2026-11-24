using System.Globalization;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class PerdidaPorValor
{
    private static readonly Cita NotaMinima = new(ReglasAplicadas.DocumentoReglamento, Textos.SeccionNotaMinima);

    private readonly DefinitivaPorAsignatura _definitivas;

    public PerdidaPorValor(DefinitivaPorAsignatura definitivas)
    {
        _definitivas = definitivas;
    }

    public bool Usa(Cita regla, string respuesta, RegistroConsultas registro, DirectorioAcademico directorio) =>
        regla == NotaMinima
        && _definitivas.ParesDe(respuesta, directorio).Any(par => registro.Resumenes.Values.Any(resumen => TienePerdida(resumen, par)));

    private static bool TienePerdida(ResumenEstudiante resumen, (string Clave, string Cifra) par) =>
        resumen.Asignaturas.Any(resultado =>
            resultado.Estado == EstadoAsignatura.Perdida
            && AmbitoPersonal.Clave(resultado.Asignatura.Codigo) == par.Clave
            && resultado.Definitiva == decimal.Parse(par.Cifra, CultureInfo.InvariantCulture));
}
