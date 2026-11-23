using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed class FaseDelModelo
{
    private const string InicioDeLaInstruccion = "Tu respuesta anterior no se pudo verificar: ";
    private const string FinDeLaInstruccion = ". Responde de nuevo usando solo los datos y las secciones entregados.";

    private readonly IClienteLlm _clienteLlm;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly LimitesAsistente _limites;
    private readonly ConfiguracionAsistente _configuracion;
    private readonly PresupuestoDiario _presupuesto;
    private readonly EmisorDeResultados _emisor;
    private readonly CierreDeRespuesta _cierre;
    private readonly LectorResumenPropio _lectorResumen;

    public FaseDelModelo(
        IClienteLlm clienteLlm,
        PoliticaAcceso politicaAcceso,
        LimitesAsistente limites,
        ConfiguracionAsistente configuracion,
        PresupuestoDiario presupuesto,
        EmisorDeResultados emisor,
        CierreDeRespuesta cierre)
    {
        _clienteLlm = clienteLlm;
        _politicaAcceso = politicaAcceso;
        _limites = limites;
        _configuracion = configuracion;
        _presupuesto = presupuesto;
        _emisor = emisor;
        _cierre = cierre;
        _lectorResumen = new LectorResumenPropio(politicaAcceso);
    }

    public async Task<ResultadoPregunta> ResponderAsync(SesionPregunta sesion, CancellationToken limite, CancellationToken cliente)
    {
        var contexto = new ContextoUsuario(sesion.Entrada.Usuario, sesion.Directorio);
        var herramientas = new HerramientasAsistente(sesion.Api, _politicaAcceso);
        var entrada = await PrepararEntradaAsync(sesion, contexto, herramientas, limite);
        var conversacion = new ConversacionLlm(_clienteLlm, herramientas, _limites, sesion.Uso, _configuracion);
        var resultado = await conversacion.EjecutarAsync(contexto, entrada, sesion.Registro, limite);
        return resultado.Estado == EstadoConversacion.Completada
            ? await CerrarAsync(new Turno(sesion, contexto, entrada, conversacion, resultado), limite, cliente)
            : Terminal(sesion, resultado);
    }

    private async Task<ResultadoPregunta> CerrarAsync(Turno turno, CancellationToken limite, CancellationToken cliente)
    {
        var primero = _cierre.Evaluar(turno.Sesion, turno.Resultado.Texto ?? string.Empty, turno.Entrada);
        if (primero.MotivoDeReintento is null || !_presupuesto.TryReservar(_configuracion.ReservaPorPeticionUsd))
        {
            return Emitir(turno.Sesion, primero);
        }

        turno.Sesion.ReservasExtra++;
        turno.Sesion.Reintento = true;
        try
        {
            return await ReintentarAsync(turno, primero, limite);
        }
        catch (OperationCanceledException) when (!cliente.IsCancellationRequested)
        {
            return Emitir(turno.Sesion, primero);
        }
    }

    private async Task<ResultadoPregunta> ReintentarAsync(Turno turno, CierreEvaluado primero, CancellationToken limite)
    {
        var instruccion = $"{InicioDeLaInstruccion}{primero.MotivoDeReintento}{FinDeLaInstruccion}";
        var segundo = await turno.Conversacion.ReintentarAsync(turno.Contexto, turno.Entrada.PromptSistema, turno.Resultado, instruccion, turno.Sesion.Registro, limite);
        return segundo.Estado switch
        {
            EstadoConversacion.Completada => Emitir(turno.Sesion, ElegirEntre(primero, _cierre.Evaluar(turno.Sesion, segundo.Texto ?? string.Empty, turno.Entrada))),
            EstadoConversacion.IdAjeno => _emisor.Denegar(turno.Sesion, MotivoDenegacion.HerramientaIdAjeno, turno.Sesion.Uso.Valor.Total, segundo.IdSolicitado),
            _ => Emitir(turno.Sesion, primero)
        };
    }

    private static CierreEvaluado ElegirEntre(CierreEvaluado primero, CierreEvaluado segundo) => segundo.Bloqueado ? primero : segundo;

    private ResultadoPregunta Emitir(SesionPregunta sesion, CierreEvaluado cierre) =>
        _emisor.Responder(sesion, cierre.Texto, cierre.Citas, cierre.Resultado, cierre.UsoDatos);

    private ResultadoPregunta Terminal(SesionPregunta sesion, ResultadoConversacion resultado) => resultado.Estado switch
    {
        EstadoConversacion.IdAjeno => _emisor.Denegar(sesion, MotivoDenegacion.HerramientaIdAjeno, sesion.Uso.Valor.Total, resultado.IdSolicitado),
        EstadoConversacion.LimiteHerramientas => _emisor.NoDisponible(sesion, ResultadoAuditoria.LimiteHerramientas),
        EstadoConversacion.ContextoTruncado => ResponderContextoTruncado(sesion),
        _ => _emisor.Responder(sesion, Textos.NoVerificable, [], ResultadoAuditoria.Error)
    };

    private ResultadoPregunta ResponderContextoTruncado(SesionPregunta sesion)
    {
        sesion.ContextoTruncado = true;
        return _emisor.Responder(sesion, Textos.NoVerificable, [], ResultadoAuditoria.Error);
    }

    private async Task<EntradaConversacion> PrepararEntradaAsync(SesionPregunta sesion, ContextoUsuario contexto, HerramientasAsistente herramientas, CancellationToken cancellationToken)
    {
        var resumen = await _lectorResumen.LeerAsync(contexto, sesion.Activacion, sesion.Api, sesion.Registro, cancellationToken);
        var prelectura = await PrelecturaAsync(sesion, contexto, herramientas, cancellationToken);
        foreach (var seccion in sesion.Secciones)
        {
            sesion.Registro.AgregarFuente(ConstructorPrompts.ContenidoEntregado(seccion));
        }

        return new EntradaConversacion(sesion.Entrada.Pregunta, sesion.Secciones, resumen, prelectura, ConstructorPrompts.PromptSistema(contexto.Usuario.Rol, _configuracion.MarcadorCanario));
    }

    private async Task<string?> PrelecturaAsync(SesionPregunta sesion, ContextoUsuario contexto, HerramientasAsistente herramientas, CancellationToken cancellationToken)
    {
        var programas = _politicaAcceso.ProgramasAutorizados(contexto.Usuario, contexto.Directorio);
        var palabras = TokenizadorTexto.Valores(sesion.Entrada.Pregunta);
        return ActivadorPrelectura.Activa(contexto, palabras, programas)
            ? await herramientas.LeerProgramaAsync(contexto, sesion.Registro, cancellationToken)
            : null;
    }

    private sealed record Turno(SesionPregunta Sesion, ContextoUsuario Contexto, EntradaConversacion Entrada, ConversacionLlm Conversacion, ResultadoConversacion Resultado);
}
