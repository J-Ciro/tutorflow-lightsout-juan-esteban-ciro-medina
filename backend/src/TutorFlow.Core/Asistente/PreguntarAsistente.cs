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
    }

    public async Task<ResultadoPregunta> EjecutarAsync(
        PreguntaEntrada entrada,
        string ipOrigen,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var presupuestoApi = TimeSpan.FromSeconds(_limites.PresupuestoApiAcademicaSegundos);
        var api = new ApiAcademicaConPresupuesto(_apiAcademica, presupuestoApi);
        var directorio = await ConstructorDirectorio.ConstruirAsync(entrada.Usuario, api, _configuracion.CodigosAsignatura, cancellationToken);
        if (directorio is not null)
        {
            _auditor.RecordarPersonas(directorio);
        }

        var sesion = new SesionPregunta(entrada, ipOrigen, correlationId, api, directorio ?? ConstructorDirectorio.Vacio);
        var decision = Autorizar(sesion);
        return decision.Permitido
            ? await ResponderConReservaAsync(sesion, cancellationToken)
            : Denegar(sesion, decision.Motivo, 0);
    }

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

        try
        {
            return await ResponderConTimeoutAsync(sesion, cancellationToken);
        }
        finally
        {
            _presupuesto.Sustituir(reserva, CalculadoraCosto.Calcular(sesion.Uso.Valor, _configuracion.Precio));
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
        var secciones = _baseConocimiento.BuscarSecciones(ConsultaDeRecuperacion.DeTexto(pregunta), _limites.MaximoSeccionesRecuperadas);
        var resumen = await _lectorResumen.LeerAsync(contexto, pregunta, sesion.Api, sesion.Registro, cancellationToken);
        return new EntradaConversacion(pregunta, secciones, resumen, ConstructorPrompts.PromptSistema(contexto.Usuario.Rol, _configuracion.MarcadorCanario));
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
        var candidatas = CitasEntregadasYCitadas(texto, entrada, sesion.Registro.Reglas);
        var verificada = _verificadorSalida.Verificar(sesion.Entrada.Usuario, texto, candidatas, autorizados, sesion.Registro.Fuentes, _baseConocimiento, directorio, Confidencial(entrada));
        return Responder(sesion, verificada.Respuesta, CitasFinales(sesion, verificada), verificada.Resultado);
    }

    private MaterialConfidencial Confidencial(EntradaConversacion entrada) =>
        new(_configuracion.MarcadorCanario, entrada.PromptSistema, HerramientasAsistente.NombresInternos);

    private static IReadOnlyList<Cita> CitasFinales(SesionPregunta sesion, SalidaVerificada verificada) =>
        verificada.Resultado == ResultadoAuditoria.Respondida
            ? [.. verificada.Citas.Concat(sesion.Registro.Citas).Distinct()]
            : verificada.Citas;

    private static IReadOnlyList<Cita> CitasEntregadasYCitadas(string texto, EntradaConversacion entrada, IReadOnlyList<Cita> reglasDeHerramientas)
    {
        var entregadas = entrada.Secciones.Select(s => new Cita(s.Documento, s.Seccion))
            .Concat(entrada.Resumen?.Reglas ?? [])
            .Concat(reglasDeHerramientas)
            .ToHashSet();
        return [.. ExtractorCitas.De(texto).Where(entregadas.Contains)];
    }

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
            sesion.Cronometro.ElapsedMilliseconds, _configuracion.Precio is null)
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
