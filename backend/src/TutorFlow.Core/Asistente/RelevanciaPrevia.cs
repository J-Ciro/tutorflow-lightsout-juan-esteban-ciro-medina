using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class RelevanciaPrevia
{
    private const string PalabraDelReglamentoDerogado = "2023";
    private const int LongitudDelAnio = 4;
    private const string PrimerSemestre = "1";
    private const string SegundoSemestre = "2";

    private static readonly HashSet<string> TerminosPropiosDelCoordinador = new(StringComparer.Ordinal)
    {
        "estudiante", "estudiantes", "programa", "programas"
    };

    private readonly PoliticaAcceso _politicaAcceso;
    private readonly int _relevanciaMinima;

    public RelevanciaPrevia(PoliticaAcceso politicaAcceso, int relevanciaMinima)
    {
        _politicaAcceso = politicaAcceso;
        _relevanciaMinima = relevanciaMinima;
    }

    public bool DebeResponderSinInformacion(ContextoUsuario contexto, string pregunta, IReadOnlyList<SeccionDocumento> secciones)
    {
        var palabras = TokenizadorTexto.Valores(pregunta);
        if (ActivadorResumenPropio.Activa(contexto, pregunta) || EsConsultaDeDatosDeCoordinador(contexto, palabras))
        {
            return PideOtroPeriodo(palabras);
        }

        return !palabras.Contains(PalabraDelReglamentoDerogado) && !HaySeccionRelevante(pregunta, secciones);
    }

    private bool EsConsultaDeDatosDeCoordinador(ContextoUsuario contexto, IReadOnlyList<string> palabras)
    {
        if (contexto.Usuario.Rol != Rol.Coordinador)
        {
            return false;
        }

        var programas = _politicaAcceso.ProgramasAutorizados(contexto.Usuario, contexto.Directorio);
        return programas.Count > 0
            && (PideTerminosDeDatos(palabras) || MencionaEstudianteDeSusProgramas(contexto, palabras, programas));
    }

    private static bool PideTerminosDeDatos(IReadOnlyList<string> palabras) =>
        palabras.Any(TerminosPropiosDelCoordinador.Contains) || ActivadorResumenPropio.PideDatosAcademicos(palabras);

    private static bool MencionaEstudianteDeSusProgramas(
        ContextoUsuario contexto,
        IReadOnlyList<string> palabras,
        IReadOnlyList<string> programas)
    {
        var propios = contexto.Directorio.Estudiantes.Where(e => programas.Contains(e.ProgramaId)).Select(e => e.Id).ToHashSet();
        return DetectorTerceros.PersonasMencionadas(string.Join(' ', palabras), contexto.Directorio).Any(propios.Contains);
    }

    private static bool PideOtroPeriodo(IReadOnlyList<string> palabras) =>
        palabras.Zip(palabras.Skip(1)).Any(par => EsPeriodo(par.First, par.Second) && $"{par.First}-{par.Second}" != Textos.Periodo);

    private static bool EsPeriodo(string anio, string semestre) =>
        anio.Length == LongitudDelAnio && anio.All(char.IsAsciiDigit) && (semestre == PrimerSemestre || semestre == SegundoSemestre);

    private bool HaySeccionRelevante(string pregunta, IReadOnlyList<SeccionDocumento> secciones)
    {
        var raices = RaicesDeContenido.De(pregunta).ToHashSet(StringComparer.Ordinal);
        var minimo = Math.Min(_relevanciaMinima, (raices.Count + 1) / 2);
        return raices.Count > 0 && secciones.Any(seccion => RaicesDeContenido.De($"{seccion.Titulo} {seccion.Texto}").Count(raices.Contains) >= minimo);
    }
}
