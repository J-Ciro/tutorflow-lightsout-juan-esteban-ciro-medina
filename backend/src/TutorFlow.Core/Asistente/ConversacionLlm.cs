using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public enum EstadoConversacion
{
    Completada,
    Rechazada,
    LimiteHerramientas,
    IdAjeno,
    ContextoTruncado
}

public sealed record ResultadoConversacion(
    EstadoConversacion Estado,
    string? Texto,
    string? IdSolicitado = null,
    IReadOnlyList<MensajeLlm>? Mensajes = null,
    int Invocaciones = 0);

internal sealed record EntradaConversacion(
    string Pregunta,
    IReadOnlyList<SeccionDocumento> Secciones,
    ResumenEntregado? Resumen,
    string? Prelectura,
    string PromptSistema);

internal sealed class ConversacionLlm
{
    private readonly IClienteLlm _clienteLlm;
    private readonly HerramientasAsistente _herramientas;
    private readonly LimitesAsistente _limites;
    private readonly UsoAcumulado _uso;
    private readonly ConfiguracionAsistente _configuracion;

    public ConversacionLlm(
        IClienteLlm clienteLlm,
        HerramientasAsistente herramientas,
        LimitesAsistente limites,
        UsoAcumulado uso,
        ConfiguracionAsistente configuracion)
    {
        _clienteLlm = clienteLlm;
        _herramientas = herramientas;
        _limites = limites;
        _uso = uso;
        _configuracion = configuracion;
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
            [ConstructorPrompts.MensajeInicial(entrada.Pregunta, entrada.Secciones, entrada.Resumen, entrada.Prelectura)],
            entrada.PromptSistema);
        return await CorrerAsync(estado, cancellationToken);
    }

    public async Task<ResultadoConversacion> ReintentarAsync(
        ContextoUsuario contexto,
        string promptSistema,
        ResultadoConversacion anterior,
        string instruccion,
        RegistroConsultas registro,
        CancellationToken cancellationToken)
    {
        List<MensajeLlm> mensajes =
        [
            .. anterior.Mensajes ?? [],
            new MensajeLlm(RolMensaje.Asistente, anterior.Texto ?? string.Empty, null, []),
            new MensajeLlm(RolMensaje.Usuario, instruccion, null, [])
        ];
        return await CorrerAsync(new Turnos(contexto, registro, mensajes, promptSistema) { Invocaciones = anterior.Invocaciones }, cancellationToken);
    }

    private async Task<ResultadoConversacion> CorrerAsync(Turnos estado, CancellationToken cancellationToken)
    {
        ResultadoConversacion? final = null;
        while (final is null)
        {
            final = await AvanzarAsync(estado, cancellationToken);
        }

        return final;
    }

    private async Task<ResultadoConversacion?> AvanzarAsync(Turnos estado, CancellationToken cancellationToken)
    {
        var respuesta = await _clienteLlm.CompletarAsync(Solicitar(estado), cancellationToken);
        _uso.Sumar(respuesta.Uso);
        estado.Invocaciones += respuesta.Llamadas.Count;
        var terminal = Terminal(estado, respuesta);
        return terminal ?? await EjecutarHerramientasAsync(estado, respuesta, cancellationToken);
    }

    private ResultadoConversacion? Terminal(Turnos estado, RespuestaLlm respuesta)
    {
        if (respuesta.ContextoTruncado is { } truncado)
        {
            return ContextoTruncado(truncado);
        }

        if (respuesta.Rechazada)
        {
            return new ResultadoConversacion(EstadoConversacion.Rechazada, null);
        }

        return respuesta.Llamadas.Count == 0 ? Completada(estado, respuesta) : LimiteSuperado(estado);
    }

    private static ResultadoConversacion Completada(Turnos estado, RespuestaLlm respuesta) =>
        new(EstadoConversacion.Completada, respuesta.Texto, null, [.. estado.Mensajes], estado.Invocaciones);

    private ResultadoConversacion? LimiteSuperado(Turnos estado) =>
        estado.Invocaciones > _limites.MaximoInvocacionesHerramientas
            ? new ResultadoConversacion(EstadoConversacion.LimiteHerramientas, null)
            : null;

    private ResultadoConversacion ContextoTruncado(ContextoTruncadoDetectado truncado)
    {
        _configuracion.Advertir($"Contexto del proveedor local truncado: estimación de {truncado.TokensEstimados} tokens y prompt_tokens reportados {truncado.TokensReportados}.");
        return new ResultadoConversacion(EstadoConversacion.ContextoTruncado, null);
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

    private SolicitudLlm Solicitar(Turnos estado) =>
        new(
            estado.PromptSistema,
            [.. estado.Mensajes],
            HerramientasAsistente.Definir(estado.Contexto.Usuario.Rol),
            _limites.MaximoTokensSalida,
            _configuracion.Ajustes.Temperatura,
            _configuracion.Ajustes.Semilla);

    private sealed class Turnos(ContextoUsuario contexto, RegistroConsultas registro, List<MensajeLlm> mensajes, string promptSistema)
    {
        public ContextoUsuario Contexto { get; } = contexto;

        public RegistroConsultas Registro { get; } = registro;

        public List<MensajeLlm> Mensajes { get; } = mensajes;

        public string PromptSistema { get; } = promptSistema;

        public int Invocaciones { get; set; }
    }
}
