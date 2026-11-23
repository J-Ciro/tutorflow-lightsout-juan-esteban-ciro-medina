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

public sealed record ResultadoConversacion(EstadoConversacion Estado, string? Texto, UsoTokens Uso);

public sealed record EntradaConversacion(
    string Pregunta,
    IReadOnlyList<SeccionDocumento> Secciones,
    string? Resumen,
    string PromptSistema);

public sealed class ConversacionLlm
{
    private readonly IClienteLlm _clienteLlm;
    private readonly HerramientasAsistente _herramientas;
    private readonly LimitesAsistente _limites;

    public ConversacionLlm(IClienteLlm clienteLlm, HerramientasAsistente herramientas, LimitesAsistente limites)
    {
        _clienteLlm = clienteLlm;
        _herramientas = herramientas;
        _limites = limites;
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
        estado.Uso = new UsoTokens(estado.Uso.Entrada + respuesta.Uso.Entrada, estado.Uso.Salida + respuesta.Uso.Salida);
        estado.Invocaciones += respuesta.Llamadas.Count;
        var terminal = Terminal(estado, respuesta);
        return terminal ?? await EjecutarHerramientasAsync(estado, respuesta, cancellationToken);
    }

    private ResultadoConversacion? Terminal(Turnos estado, RespuestaLlm respuesta)
    {
        if (respuesta.Rechazada)
        {
            return new ResultadoConversacion(EstadoConversacion.Rechazada, null, estado.Uso);
        }

        if (respuesta.Llamadas.Count == 0)
        {
            return new ResultadoConversacion(EstadoConversacion.Completada, respuesta.Texto, estado.Uso);
        }

        return estado.Invocaciones > _limites.MaximoInvocacionesHerramientas
            ? new ResultadoConversacion(EstadoConversacion.LimiteHerramientas, null, estado.Uso)
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
                return new ResultadoConversacion(EstadoConversacion.IdAjeno, null, estado.Uso);
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
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TimeSpan.FromSeconds(_limites.TimeoutLlmSegundos));
        try
        {
            return await _clienteLlm.CompletarAsync(solicitud, limite.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("El proveedor del LLM no respondió a tiempo.");
        }
    }

    private sealed class Turnos(ContextoUsuario contexto, RegistroConsultas registro, List<MensajeLlm> mensajes, string promptSistema)
    {
        public ContextoUsuario Contexto { get; } = contexto;

        public RegistroConsultas Registro { get; } = registro;

        public List<MensajeLlm> Mensajes { get; } = mensajes;

        public string PromptSistema { get; } = promptSistema;

        public UsoTokens Uso { get; set; } = new(0, 0);

        public int Invocaciones { get; set; }
    }
}
