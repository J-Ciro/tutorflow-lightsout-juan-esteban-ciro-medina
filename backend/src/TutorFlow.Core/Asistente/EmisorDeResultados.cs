using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class EmisorDeResultados
{
    private readonly AuditorPeticiones _auditor;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly ConfiguracionAsistente _configuracion;

    public EmisorDeResultados(AuditorPeticiones auditor, PoliticaAcceso politicaAcceso, ConfiguracionAsistente configuracion)
    {
        _auditor = auditor;
        _politicaAcceso = politicaAcceso;
        _configuracion = configuracion;
    }

    public ResultadoPregunta SinInformacion(SesionPregunta sesion) =>
        Responder(sesion, Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, usoDatos: false);

    public ResultadoPregunta Responder(
        SesionPregunta sesion,
        string texto,
        IReadOnlyList<Cita> citas,
        ResultadoAuditoria resultado,
        bool? usoDatos = null)
    {
        var respuesta = new RespuestaAsistente(
            texto,
            citas,
            usoDatos ?? sesion.Registro.UsoDatos,
            sesion.Uso.Valor.Total,
            CalculadoraCosto.Calcular(sesion.Uso.Valor, _configuracion.Precio));
        Auditar(sesion, resultado, null, citas);
        return new ResultadoPregunta(TipoResultadoPregunta.Respondida, respuesta, resultado, null);
    }

    public ResultadoPregunta Denegar(SesionPregunta sesion, MotivoDenegacion? motivo, int tokens, string? idSolicitado = null)
    {
        var costo = CalculadoraCosto.Calcular(sesion.Uso.Valor, _configuracion.Precio);
        var citas = new[] { Textos.CitaNegativa() };
        var respuesta = new RespuestaAsistente(Textos.Negativa(sesion.Entrada.Usuario.Rol), citas, false, tokens, costo);
        Auditar(sesion, ResultadoAuditoria.Denegada, motivo, citas, idSolicitado);
        return new ResultadoPregunta(TipoResultadoPregunta.Denegada, respuesta, ResultadoAuditoria.Denegada, motivo);
    }

    public ResultadoPregunta NoDisponible(SesionPregunta sesion, ResultadoAuditoria resultado)
    {
        Auditar(sesion, resultado, null, []);
        return new ResultadoPregunta(TipoResultadoPregunta.NoDisponible, null, resultado, null);
    }

    public void Auditar(
        SesionPregunta sesion,
        ResultadoAuditoria resultado,
        MotivoDenegacion? motivo,
        IReadOnlyList<Cita> citas,
        string? idSolicitado = null)
    {
        _auditor.Registrar(ConstruirEvento(sesion, resultado, motivo, citas, idSolicitado));
    }

    private EventoAuditoria ConstruirEvento(SesionPregunta sesion, ResultadoAuditoria resultado, MotivoDenegacion? motivo, IReadOnlyList<Cita> citas, string? idSolicitado)
    {
        var usuario = sesion.Entrada.Usuario;
        var pregunta = PreguntaAuditable(sesion);
        var costo = CalculadoraCosto.Calcular(sesion.Uso.Valor, _configuracion.Precio);
        var registro = sesion.Registro;
        return new EventoAuditoria(
            _auditor.AhoraUtc, sesion.CorrelationId, sesion.IpOrigen, usuario.Rol, usuario.Id, pregunta, resultado, motivo,
            registro.Estudiantes, registro.Recursos, citas, sesion.Uso.Valor.Total, costo,
            sesion.MilisegundosTranscurridos, sesion.ReservaHecha && _configuracion.Precio is null)
        {
            DatosConsultados = registro.DatosConsultados,
            IdSolicitado = idSolicitado,
            Reintento = sesion.Reintento,
            ContextoTruncado = sesion.ContextoTruncado
        };
    }

    private string PreguntaAuditable(SesionPregunta sesion)
    {
        var usuario = sesion.Entrada.Usuario;
        return _politicaAcceso.EvaluarIdentidad(usuario, sesion.Directorio).Permitido
            ? AuditorPeticiones.SanearPregunta(usuario, sesion.Entrada.Pregunta, sesion.Directorio)
            : _auditor.PreguntaSinIdentidad(sesion.Entrada.Pregunta);
    }
}
