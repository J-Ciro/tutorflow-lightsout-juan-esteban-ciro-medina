using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public enum EstadoConversacion
{
    Completada,
    Rechazada,
    LimiteHerramientas,
    IdAjeno
}

public sealed record ResultadoConversacion(EstadoConversacion Estado, string? Texto, string? IdSolicitado = null);

internal sealed record EntradaConversacion(
    string Pregunta,
    IReadOnlyList<SeccionDocumento> Secciones,
    ResumenEntregado? Resumen,
    string PromptSistema);

internal sealed class ConversacionLlm
{
    private readonly IClienteLlm _clienteLlm;
    private readonly HerramientasAsistente _herramientas;
    private readonly LimitesAsistente _limites;
    private readonly UsoAcumulado _uso;

    public ConversacionLlm(IClienteLlm clienteLlm, HerramientasAsistente herramientas, LimitesAsistente limites, UsoAcumulado uso)
    {
        _clienteLlm = clienteLlm;
        _herramientas = herramientas;
        _limites = limites;
        _uso = uso;
    }

    public async Task<ResultadoConversacion> EjecutarAsync(
        ContextoUsuario contexto,
        EntradaConversacion entrada,
        RegistroConsultas registro,
        CancellationToken cancellationToken)
    {
        var estado = new Turnos(
            contexto,
            registro,
            [ConstructorPrompts.MensajeInicial(entrada.Pregunta, entrada.Secciones, entrada.Resumen)],
            entrada.PromptSistema);
        ResultadoConversacion? final = null;
        while (final is null)
        {
            final = await AvanzarAsync(estado, cancellationToken);
        }

        return final;
    }

    private async Task<ResultadoConversacion?> AvanzarAsync(Turnos estado, CancellationToken cancellationToken)
    {
        var respuesta = await LlamarAsync(estado, cancellationToken);
        _uso.Sumar(respuesta.Uso);
        estado.Invocaciones += respuesta.Llamadas.Count;
        var terminal = Terminal(estado, respuesta);
        return terminal ?? await EjecutarHerramientasAsync(estado, respuesta, cancellationToken);
    }

    private ResultadoConversacion? Terminal(Turnos estado, RespuestaLlm respuesta)
    {
        if (respuesta.Rechazada)
        {
            return new ResultadoConversacion(EstadoConversacion.Rechazada, null);
        }

        if (respuesta.Llamadas.Count == 0)
        {
            return new ResultadoConversacion(EstadoConversacion.Completada, respuesta.Texto);
        }

        return estado.Invocaciones > _limites.MaximoInvocacionesHerramientas
            ? new ResultadoConversacion(EstadoConversacion.LimiteHerramientas, null)
            : null;
    }

    private async Task<ResultadoConversacion?> EjecutarHerramientasAsync(Turnos estado, RespuestaLlm respuesta, CancellationToken cancellationToken)
    {
        estado.Mensajes.Add(new MensajeLlm(RolMensaje.Asistente, respuesta.Texto ?? string.Empty, null, respuesta.Llamadas));
        foreach (var llamada in respuesta.Llamadas)
        {
            var resultado = await _herramientas.EjecutarAsync(llamada, estado.Contexto, estado.Registro, cancellationToken);
            if (resultado.IdAjeno)
            {
                return new ResultadoConversacion(EstadoConversacion.IdAjeno, null, resultado.IdSolicitado);
            }

            estado.Registro.AgregarFuente(resultado.Contenido);
            estado.Mensajes.Add(new MensajeLlm(RolMensaje.Herramienta, resultado.Contenido, llamada.Id, []));
        }

        return null;
    }

    private async Task<RespuestaLlm> LlamarAsync(Turnos estado, CancellationToken cancellationToken)
    {
        var solicitud = new SolicitudLlm(
            estado.PromptSistema,
            [.. estado.Mensajes],
            HerramientasAsistente.Definir(estado.Contexto.Usuario.Rol),
            _limites.MaximoTokensSalida);
        return await _clienteLlm.CompletarAsync(solicitud, cancellationToken);
    }

    private sealed class Turnos(ContextoUsuario contexto, RegistroConsultas registro, List<MensajeLlm> mensajes, string promptSistema)
    {
        public ContextoUsuario Contexto { get; } = contexto;

        public RegistroConsultas Registro { get; } = registro;

        public List<MensajeLlm> Mensajes { get; } = mensajes;

        public string PromptSistema { get; } = promptSistema;

        public int Invocaciones { get; set; }
    }
}
