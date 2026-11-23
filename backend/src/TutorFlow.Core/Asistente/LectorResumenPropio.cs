using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class LectorResumenPropio
{
    private readonly PoliticaAcceso _politicaAcceso;

    public LectorResumenPropio(PoliticaAcceso politicaAcceso)
    {
        _politicaAcceso = politicaAcceso;
    }

    public async Task<string?> LeerAsync(
        ContextoUsuario contexto,
        string pregunta,
        IApiAcademica api,
        RegistroConsultas registro,
        CancellationToken cancellationToken)
    {
        var usuario = contexto.Usuario;
        if (!ActivadorResumenPropio.Activa(usuario.Rol, pregunta) || !PuedeLeerSusDatos(contexto))
        {
            return null;
        }

        var resumen = await ResumenesAcademicos.ResumirAsync(api, usuario.Id, cancellationToken);
        Registrar(contexto, resumen, registro);
        var texto = FormateadorResumen.Resumir(resumen);
        registro.AgregarFuente(texto);
        return texto;
    }

    private bool PuedeLeerSusDatos(ContextoUsuario contexto) =>
        _politicaAcceso.EvaluarLecturaEstudiante(contexto.Usuario, contexto.Usuario.Id, contexto.Directorio).Permitido;

    private static void Registrar(ContextoUsuario contexto, ResumenEstudiante resumen, RegistroConsultas registro)
    {
        var id = contexto.Usuario.Id;
        var programaId = contexto.Directorio.Estudiantes.First(e => e.Id == id).ProgramaId;
        registro.Registrar(id, [RecursoConsultado.Notas, RecursoConsultado.Asistencia], new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionNotas));
        registro.RegistrarCita(new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionAsistencia));
        registro.RegistrarDatos(DatosLeidos.PorAsignatura(resumen, programaId, RecursoConsultado.Notas, RecursoConsultado.Asistencia));
    }
}
