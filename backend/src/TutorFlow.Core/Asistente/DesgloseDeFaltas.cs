using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class DesgloseDeFaltas
{
    private static readonly HashSet<string> Terminos = new(StringComparer.Ordinal)
    {
        "falta", "faltas", "inasistencia", "inasistencias", "asistencia"
    };

    private readonly PoliticaAcceso _politicaAcceso;
    private readonly IReadOnlyList<AsignaturaCatalogo> _catalogo;

    public DesgloseDeFaltas(PoliticaAcceso politicaAcceso, IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        _politicaAcceso = politicaAcceso;
        _catalogo = catalogo;
    }

    public async Task<string?> PrepararAsync(SesionPregunta sesion, ContextoUsuario contexto, CancellationToken cancellationToken)
    {
        var palabras = TokenizadorTexto.Valores(sesion.Entrada.Pregunta);
        var personaId = palabras.Any(Terminos.Contains) ? PersonaDelDesglose(sesion, contexto, palabras) : null;
        if (personaId is null)
        {
            return null;
        }

        var resumen = await ResumenDeAsync(sesion, contexto, personaId, cancellationToken);
        var claves = CatalogoAsignaturas.CodigosNombrados(sesion.Entrada.Pregunta, CatalogoAsignaturas.Vigente(_catalogo, sesion.Directorio), sesion.Directorio.CodigosAsignatura);
        return resumen is null ? null : TextoDelDesglose.De(resumen, claves, contexto.Usuario.Rol == Rol.Coordinador);
    }

    private string? PersonaDelDesglose(SesionPregunta sesion, ContextoUsuario contexto, IReadOnlyList<string> palabras)
    {
        if (contexto.Usuario.Rol == Rol.Estudiante)
        {
            return sesion.Activacion.Entrega ? contexto.Usuario.Id : null;
        }

        var programas = _politicaAcceso.ProgramasAutorizados(contexto.Usuario, contexto.Directorio);
        var mencionados = ActivadorPrelectura.EstudiantesMencionadosDeSusProgramas(contexto, palabras, programas);
        return mencionados.Count == 1 ? mencionados[0] : null;
    }

    private async Task<ResumenEstudiante?> ResumenDeAsync(SesionPregunta sesion, ContextoUsuario contexto, string personaId, CancellationToken cancellationToken)
    {
        if (sesion.Registro.Resumenes.TryGetValue(personaId, out var leido))
        {
            return leido;
        }

        if (!_politicaAcceso.EvaluarLecturaEstudiante(contexto.Usuario, personaId, contexto.Directorio).Permitido)
        {
            return null;
        }

        var resumen = await ResumenesAcademicos.ResumirAsync(sesion.Api, personaId, cancellationToken);
        Registrar(sesion.Registro, contexto.Directorio, resumen);
        return resumen;
    }

    private static void Registrar(RegistroConsultas registro, DirectorioAcademico directorio, ResumenEstudiante resumen)
    {
        var programaId = directorio.Estudiantes.First(estudiante => estudiante.Id == resumen.EstudianteId).ProgramaId;
        registro.Registrar(resumen.EstudianteId, [RecursoConsultado.Notas, RecursoConsultado.Asistencia], new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionAsistencia));
        registro.RegistrarDatos(DatosLeidos.PorAsignatura(resumen, programaId, RecursoConsultado.Notas, RecursoConsultado.Asistencia));
        registro.RegistrarResumen(resumen);
        registro.RegistrarReglas(ReglasAplicadas.DeNotasYAsistencia([resumen]));
    }
}
