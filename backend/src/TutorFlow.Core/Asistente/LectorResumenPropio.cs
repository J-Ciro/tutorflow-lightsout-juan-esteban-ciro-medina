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

    public async Task<ActivacionResumen> VerificarMatriculaAsync(
        ContextoUsuario contexto,
        ActivacionResumen activacion,
        IApiAcademica api,
        RegistroConsultas registro,
        CancellationToken cancellationToken)
    {
        if (!activacion.RequiereVerificacion || !PuedeLeerSusDatos(contexto))
        {
            return activacion;
        }

        var notas = await api.ObtenerNotasAsync(contexto.Usuario.Id, cancellationToken);
        RegistrarVerificacion(contexto, notas, registro);
        var matriculada = notas.Any(nota => activacion.CodigosNombrados.Contains(AmbitoPersonal.Clave(nota.Codigo)));
        return activacion with { NotasVerificadas = notas, Matriculada = matriculada };
    }

    public async Task<ResumenEntregado?> LeerAsync(
        ContextoUsuario contexto,
        ActivacionResumen activacion,
        IApiAcademica api,
        RegistroConsultas registro,
        CancellationToken cancellationToken)
    {
        if (!activacion.Entrega || !PuedeLeerSusDatos(contexto))
        {
            return null;
        }

        var resumen = await ResumirAsync(contexto, activacion, api, cancellationToken);
        Registrar(contexto, resumen, registro);
        var entregado = FormateadorResumen.Resumir(resumen);
        registro.RegistrarReglas(entregado.EntradasDeReglas);
        registro.AgregarFuente(entregado.Texto);
        return entregado;
    }

    private static Task<ResumenEstudiante> ResumirAsync(
        ContextoUsuario contexto,
        ActivacionResumen activacion,
        IApiAcademica api,
        CancellationToken cancellationToken) =>
        activacion.NotasVerificadas is null
            ? ResumenesAcademicos.ResumirAsync(api, contexto.Usuario.Id, cancellationToken)
            : ResumenesAcademicos.ResumirAsync(api, contexto.Usuario.Id, activacion.NotasVerificadas, cancellationToken);

    private bool PuedeLeerSusDatos(ContextoUsuario contexto) =>
        _politicaAcceso.EvaluarLecturaEstudiante(contexto.Usuario, contexto.Usuario.Id, contexto.Directorio).Permitido;

    private static string ProgramaDe(ContextoUsuario contexto) =>
        contexto.Directorio.Estudiantes.First(e => e.Id == contexto.Usuario.Id).ProgramaId;

    private static void RegistrarVerificacion(ContextoUsuario contexto, IReadOnlyList<NotasAsignatura> notas, RegistroConsultas registro)
    {
        var datos = DatosLeidos.PorCodigos(contexto.Usuario.Id, ProgramaDe(contexto), notas.Select(n => n.Codigo), RecursoConsultado.Notas);
        registro.RegistrarVerificacion(contexto.Usuario.Id, [RecursoConsultado.Notas], datos);
    }

    private static void Registrar(ContextoUsuario contexto, ResumenEstudiante resumen, RegistroConsultas registro)
    {
        var id = contexto.Usuario.Id;
        registro.Registrar(id, [RecursoConsultado.Notas, RecursoConsultado.Asistencia], new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionNotas));
        registro.RegistrarCita(new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionAsistencia));
        registro.RegistrarDatos(DatosLeidos.PorAsignatura(resumen, ProgramaDe(contexto), RecursoConsultado.Notas, RecursoConsultado.Asistencia));
        registro.RegistrarResumen(resumen);
    }
}
