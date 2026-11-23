using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed record PreguntaEntrada(string Pregunta, Usuario Usuario);

public sealed record RespuestaAsistente(
    string Respuesta,
    IReadOnlyList<Cita> Citas,
    bool UsoDatosEstudiante,
    int Tokens,
    decimal CostoUsd);

public enum TipoResultadoPregunta
{
    Respondida,
    Denegada,
    NoDisponible
}

public sealed record ResultadoPregunta(
    TipoResultadoPregunta Tipo,
    RespuestaAsistente? Respuesta,
    ResultadoAuditoria Auditoria,
    MotivoDenegacion? Motivo);

public sealed class PreguntarAsistente
{
    private const int MaximoCitasDeReglasAplicadas = 3;

    private readonly IApiAcademica _apiAcademica;
    private readonly IClienteLlm _clienteLlm;
    private readonly IBaseConocimiento _baseConocimiento;
    private readonly AuditorPeticiones _auditor;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly VerificadorSalida _verificadorSalida;
    private readonly LimitesAsistente _limites;
    private readonly ConfiguracionAsistente _configuracion;
    private readonly PresupuestoDiario _presupuesto;
    private readonly LectorResumenPropio _lectorResumen;
    private readonly RelevanciaPrevia _relevancia;
    private readonly CitasDeRespuesta _citasDeRespuesta;
    private readonly OrdenDeReglasPorRelevancia _ordenDeReglas;
    private readonly AfirmacionesDePerdida _afirmacionesDePerdida;

    public PreguntarAsistente(
        IApiAcademica apiAcademica,
        IClienteLlm clienteLlm,
        IBaseConocimiento baseConocimiento,
        AuditorPeticiones auditor,
        PoliticaAcceso politicaAcceso,
        VerificadorSalida verificadorSalida,
        LimitesAsistente limites,
        ConfiguracionAsistente configuracion,
        PresupuestoDiario presupuesto)
    {
        _apiAcademica = apiAcademica;
        _clienteLlm = clienteLlm;
        _baseConocimiento = baseConocimiento;
        _auditor = auditor;
        _politicaAcceso = politicaAcceso;
        _verificadorSalida = verificadorSalida;
        _limites = limites;
        _configuracion = configuracion;
        _presupuesto = presupuesto;
        _lectorResumen = new LectorResumenPropio(politicaAcceso);
        _relevancia = new RelevanciaPrevia(politicaAcceso, configuracion.RelevanciaMinima);
        _citasDeRespuesta = new CitasDeRespuesta(configuracion.ApoyoMinimoRespaldo);
        _ordenDeReglas = new OrdenDeReglasPorRelevancia(baseConocimiento);
        _afirmacionesDePerdida = new AfirmacionesDePerdida(configuracion.Catalogo);
    }

    public async Task<ResultadoPregunta> EjecutarAsync(
        PreguntaEntrada entrada,
        string ipOrigen,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var presupuestoApi = TimeSpan.FromSeconds(_limites.PresupuestoApiAcademicaSegundos);
        var api = new ApiAcademicaConPresupuesto(_apiAcademica, presupuestoApi);
        var sesion = new SesionPregunta(entrada, ipOrigen, correlationId, api);
        try
        {
            return await ResponderSesionAsync(sesion, cancellationToken);
        }
        catch (Exception)
        {
            Auditar(sesion, ResultadoAuditoria.Error, null, []);
            throw;
        }
    }

    private async Task<ResultadoPregunta> ResponderSesionAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var directorio = await ConstructorDirectorio.ConstruirAsync(sesion.Entrada.Usuario, sesion.Api, _configuracion.CodigosAsignatura, cancellationToken);
        if (directorio is not null)
        {
            _auditor.RecordarPersonas(directorio);
            sesion.Directorio = directorio;
        }

        var decision = Autorizar(sesion);
        return decision.Permitido
            ? await ResponderSiEsRelevanteAsync(sesion, cancellationToken)
            : Denegar(sesion, decision.Motivo, 0);
    }

    private async Task<ResultadoPregunta> ResponderSiEsRelevanteAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var pregunta = sesion.Entrada.Pregunta;
        var contexto = new ContextoUsuario(sesion.Entrada.Usuario, sesion.Directorio);
        sesion.Secciones = _baseConocimiento.BuscarSecciones(ConsultaDeRecuperacion.DeTexto(pregunta), _limites.MaximoSeccionesRecuperadas);
        var activacion = ActivadorResumenPropio.Evaluar(contexto, pregunta, _configuracion.Catalogo);
        if (_relevancia.PeriodoNoDisponible(contexto, pregunta, activacion))
        {
            return SinInformacionPrevia(sesion);
        }

        sesion.Activacion = await _lectorResumen.VerificarMatriculaAsync(contexto, activacion, sesion.Api, sesion.Registro, cancellationToken);
        return _relevancia.DebeResponderSinInformacion(contexto, pregunta, sesion.Secciones, sesion.Activacion)
            ? SinInformacionPrevia(sesion)
            : await ResponderConReservaAsync(sesion, cancellationToken);
    }

    private ResultadoPregunta SinInformacionPrevia(SesionPregunta sesion) =>
        Responder(sesion, Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, usoDatos: false);

    private DecisionAcceso Autorizar(SesionPregunta sesion)
    {
        var identidad = _politicaAcceso.EvaluarIdentidad(sesion.Entrada.Usuario, sesion.Directorio);
        return identidad.Permitido
            ? _politicaAcceso.EvaluarPregunta(sesion.Entrada.Usuario, sesion.Entrada.Pregunta, sesion.Directorio)
            : identidad;
    }

    private async Task<ResultadoPregunta> ResponderConReservaAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var reserva = _configuracion.ReservaPorPeticionUsd;
        if (!_presupuesto.TryReservar(reserva))
        {
            return NoDisponible(sesion, ResultadoAuditoria.Error);
        }

        sesion.ReservaHecha = true;
        try
        {
            return await ResponderConTimeoutAsync(sesion, cancellationToken);
        }
        finally
        {
            _presupuesto.Sustituir(reserva, _configuracion.GastoDe(sesion.Uso.Valor));
        }
    }

    private async Task<ResultadoPregunta> ResponderConTimeoutAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TimeSpan.FromSeconds(_limites.TimeoutLlmSegundos));
        try
        {
            return await ResponderAsync(sesion, limite.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("El tiempo total de la petición se agotó.");
        }
    }

    private async Task<ResultadoPregunta> ResponderAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var usuario = sesion.Entrada.Usuario;
        var contexto = new ContextoUsuario(usuario, sesion.Directorio);
        var entrada = await PrepararEntradaAsync(sesion, contexto, cancellationToken);
        var conversacion = new ConversacionLlm(_clienteLlm, new HerramientasAsistente(sesion.Api, _politicaAcceso), _limites, sesion.Uso);
        var resultado = await conversacion.EjecutarAsync(contexto, entrada, sesion.Registro, cancellationToken);
        return resultado.Estado switch
        {
            EstadoConversacion.IdAjeno => Denegar(sesion, MotivoDenegacion.HerramientaIdAjeno, sesion.Uso.Valor.Total, resultado.IdSolicitado),
            EstadoConversacion.LimiteHerramientas => NoDisponible(sesion, ResultadoAuditoria.LimiteHerramientas),
            EstadoConversacion.Rechazada => Responder(sesion, Textos.NoVerificable, [], ResultadoAuditoria.Error),
            _ => Completar(sesion, resultado.Texto ?? string.Empty, entrada)
        };
    }

    private async Task<EntradaConversacion> PrepararEntradaAsync(SesionPregunta sesion, ContextoUsuario contexto, CancellationToken cancellationToken)
    {
        var pregunta = sesion.Entrada.Pregunta;
        var resumen = await _lectorResumen.LeerAsync(contexto, sesion.Activacion, sesion.Api, sesion.Registro, cancellationToken);
        return new EntradaConversacion(pregunta, sesion.Secciones, resumen, ConstructorPrompts.PromptSistema(contexto.Usuario.Rol, _configuracion.MarcadorCanario));
    }

    private ResultadoPregunta Completar(SesionPregunta sesion, string texto, EntradaConversacion entrada)
    {
        if (texto.Trim() == Textos.SinInformacion)
        {
            return Responder(sesion, Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, usoDatos: false);
        }

        foreach (var seccion in entrada.Secciones)
        {
            sesion.Registro.AgregarFuente($"{seccion.Titulo}. {seccion.Texto}");
        }

        var directorio = sesion.Directorio;
        var autorizados = _politicaAcceso.IdsPersonasAutorizadas(sesion.Entrada.Usuario, directorio);
        var candidatas = _citasDeRespuesta.EscritasConApoyo(texto, entrada.Secciones, sesion.Registro.Reglas);
        var verificada = _verificadorSalida.Verificar(sesion.Entrada.Usuario, texto, candidatas, autorizados, sesion.Registro.Fuentes, _baseConocimiento, directorio, Confidencial(sesion, entrada));
        return verificada.Resultado == ResultadoAuditoria.Respondida
            ? ResponderSiLasPerdidasSeRespaldan(sesion, verificada, entrada, autorizados)
            : Responder(sesion, verificada.Respuesta, verificada.Citas, verificada.Resultado);
    }

    private ResultadoPregunta ResponderSiLasPerdidasSeRespaldan(
        SesionPregunta sesion,
        SalidaVerificada verificada,
        EntradaConversacion entrada,
        IReadOnlyCollection<string> autorizados) =>
        _afirmacionesDePerdida.SonVerificables(verificada.Respuesta, autorizados, sesion.Directorio, sesion.Registro.Resumenes)
            ? ResponderVerificada(sesion, verificada, entrada)
            : Responder(sesion, Textos.NoVerificable, [], ResultadoAuditoria.EstadoNoVerificado);

    private ResultadoPregunta ResponderVerificada(SesionPregunta sesion, SalidaVerificada verificada, EntradaConversacion entrada)
    {
        var citasDeTextoFijo = Textos.CitasDeTextoFijo(verificada.Respuesta);
        if (citasDeTextoFijo is not null)
        {
            return Responder(sesion, verificada.Respuesta, citasDeTextoFijo, verificada.Resultado);
        }

        var citasDeDocumento = CitasDeDocumento(sesion, verificada, entrada);
        if (citasDeDocumento.Count == 0 && !sesion.Registro.UsoDatos)
        {
            return Responder(sesion, Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, usoDatos: false);
        }

        IReadOnlyList<Cita> citas = [.. citasDeDocumento.Concat(sesion.Registro.Citas).Distinct()];
        return Responder(sesion, verificada.Respuesta, citas, verificada.Resultado);
    }

    private IReadOnlyList<Cita> CitasDeDocumento(SesionPregunta sesion, SalidaVerificada verificada, EntradaConversacion entrada)
    {
        var reglasAplicadas = _ordenDeReglas.Ordenar(sesion.Registro.Reglas, sesion.Entrada.Pregunta).Take(MaximoCitasDeReglasAplicadas);
        IReadOnlyList<Cita> escritasYReglas = [.. verificada.Citas.Concat(reglasAplicadas).Distinct()];
        return escritasYReglas.Count > 0
            ? escritasYReglas
            : _citasDeRespuesta.Respaldo(verificada.Respuesta, entrada.Secciones);
    }

    private MaterialConfidencial Confidencial(SesionPregunta sesion, EntradaConversacion entrada) =>
        new(
            _configuracion.MarcadorCanario,
            [ConstructorPrompts.Instrucciones(entrada.PromptSistema), .. DescripcionesDeHerramientas(sesion.Entrada.Usuario.Rol)],
            HerramientasAsistente.NombresInternos);

    private static IEnumerable<string> DescripcionesDeHerramientas(Rol rol) =>
        HerramientasAsistente.Definir(rol)
            .SelectMany(herramienta => herramienta.Parametros.Select(parametro => parametro.Descripcion).Prepend(herramienta.Descripcion));

    private ResultadoPregunta Responder(
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

    private ResultadoPregunta Denegar(SesionPregunta sesion, MotivoDenegacion? motivo, int tokens, string? idSolicitado = null)
    {
        var costo = CalculadoraCosto.Calcular(sesion.Uso.Valor, _configuracion.Precio);
        var citas = new[] { Textos.CitaNegativa() };
        var respuesta = new RespuestaAsistente(Textos.Negativa(sesion.Entrada.Usuario.Rol), citas, false, tokens, costo);
        Auditar(sesion, ResultadoAuditoria.Denegada, motivo, citas, idSolicitado);
        return new ResultadoPregunta(TipoResultadoPregunta.Denegada, respuesta, ResultadoAuditoria.Denegada, motivo);
    }

    private ResultadoPregunta NoDisponible(SesionPregunta sesion, ResultadoAuditoria resultado)
    {
        Auditar(sesion, resultado, null, []);
        return new ResultadoPregunta(TipoResultadoPregunta.NoDisponible, null, resultado, null);
    }

    private void Auditar(
        SesionPregunta sesion,
        ResultadoAuditoria resultado,
        MotivoDenegacion? motivo,
        IReadOnlyList<Cita> citas,
        string? idSolicitado = null)
    {
        var usuario = sesion.Entrada.Usuario;
        var pregunta = PreguntaAuditable(sesion);
        var costo = CalculadoraCosto.Calcular(sesion.Uso.Valor, _configuracion.Precio);
        var registro = sesion.Registro;
        _auditor.Registrar(new EventoAuditoria(
            _auditor.AhoraUtc, sesion.CorrelationId, sesion.IpOrigen, usuario.Rol, usuario.Id, pregunta, resultado, motivo,
            registro.Estudiantes, registro.Recursos, citas, sesion.Uso.Valor.Total, costo,
            sesion.Cronometro.ElapsedMilliseconds, sesion.ReservaHecha && _configuracion.Precio is null)
        {
            DatosConsultados = registro.DatosConsultados,
            IdSolicitado = idSolicitado
        });
    }

    private string PreguntaAuditable(SesionPregunta sesion)
    {
        var usuario = sesion.Entrada.Usuario;
        return _politicaAcceso.EvaluarIdentidad(usuario, sesion.Directorio).Permitido
            ? AuditorPeticiones.SanearPregunta(usuario, sesion.Entrada.Pregunta, sesion.Directorio)
            : _auditor.PreguntaSinIdentidad(sesion.Entrada.Pregunta);
    }
}
