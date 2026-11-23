using System.Diagnostics;
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

public sealed class LimitesAsistente
{
    public int MaximoCaracteresPregunta { get; init; } = 1000;
    public int MaximoBytesCuerpo { get; init; } = 16 * 1024;
    public int PreguntasPorMinutoPorIp { get; init; } = 60;
    public int MaximoSeccionesRecuperadas { get; init; } = 6;
    public int MaximoTokensSalida { get; init; } = 2000;
    public int MaximoInvocacionesHerramientas { get; init; } = 4;
    public int TimeoutLlmSegundos { get; init; } = 20;
    public int PresupuestoApiAcademicaSegundos { get; init; } = 6;
    public decimal PresupuestoDiarioUsd { get; init; } = 5m;
    public int MaximoIdentidadesPorIp { get; init; } = 3;
    public int VentanaIdentidadesMinutos { get; init; } = 10;
    public bool BloquearMultiplesIdentidades { get; init; }
}

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
    }

    public async Task<ResultadoPregunta> EjecutarAsync(
        PreguntaEntrada entrada,
        string ipOrigen,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var presupuestoApi = TimeSpan.FromSeconds(_limites.PresupuestoApiAcademicaSegundos);
        var sesion = new SesionPregunta(entrada, ipOrigen, correlationId, new ApiAcademicaConPresupuesto(_apiAcademica, presupuestoApi));
        sesion.Directorio = await ConstructorDirectorio.ConstruirAsync(entrada.Usuario, sesion.Api, _configuracion.CodigosAsignatura, cancellationToken);
        var decision = Autorizar(sesion);
        if (!decision.Permitido)
        {
            return Denegar(sesion, decision.Motivo, 0);
        }

        return _presupuesto.Agotado
            ? NoDisponible(sesion, ResultadoAuditoria.Error)
            : await ResponderAsync(sesion, cancellationToken);
    }

    private DecisionAcceso Autorizar(SesionPregunta sesion)
    {
        if (sesion.Directorio is not { } directorio)
        {
            return DecisionAcceso.Denegar(MotivoDenegacion.IdentidadInvalida);
        }

        var identidad = _politicaAcceso.EvaluarIdentidad(sesion.Entrada.Usuario, directorio);
        return identidad.Permitido
            ? _politicaAcceso.EvaluarPregunta(sesion.Entrada.Usuario, sesion.Entrada.Pregunta, directorio)
            : identidad;
    }

    private async Task<ResultadoPregunta> ResponderAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var directorio = sesion.Directorio!;
        var secciones = _baseConocimiento.BuscarSecciones(ConsultaDeRecuperacion.DeTexto(sesion.Entrada.Pregunta), _limites.MaximoSeccionesRecuperadas);
        var conversacion = new ConversacionLlm(_clienteLlm, new HerramientasAsistente(sesion.Api, _politicaAcceso), _limites, _configuracion);
        var contexto = new ContextoUsuario(sesion.Entrada.Usuario, directorio);
        var resultado = await conversacion.EjecutarAsync(contexto, sesion.Entrada.Pregunta, secciones, sesion.Registro, cancellationToken);
        sesion.Uso = resultado.Uso;
        _presupuesto.Registrar(CalculadoraCosto.Calcular(resultado.Uso, _configuracion.Precio));
        return resultado.Estado switch
        {
            EstadoConversacion.IdAjeno => Denegar(sesion, MotivoDenegacion.HerramientaIdAjeno, resultado.Uso.Total),
            EstadoConversacion.LimiteHerramientas => NoDisponible(sesion, ResultadoAuditoria.LimiteHerramientas),
            EstadoConversacion.Rechazada => Responder(sesion, Textos.NoVerificable, [], ResultadoAuditoria.Error),
            _ => Completar(sesion, resultado.Texto ?? string.Empty, secciones)
        };
    }

    private ResultadoPregunta Completar(SesionPregunta sesion, string texto, IReadOnlyList<SeccionDocumento> secciones)
    {
        if (texto.Trim() == Textos.SinInformacion)
        {
            return Responder(sesion, Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, usoDatos: false);
        }

        var fundamentos = _baseConocimiento.BuscarSecciones(ConsultaDeRecuperacion.DeTexto(texto), _limites.MaximoSeccionesRecuperadas);
        foreach (var seccion in secciones.Concat(fundamentos))
        {
            sesion.Registro.AgregarFuente($"{seccion.Titulo}. {seccion.Texto}");
        }

        var directorio = sesion.Directorio!;
        var autorizados = _politicaAcceso.IdsPersonasAutorizadas(sesion.Entrada.Usuario, directorio);
        var candidatas = ElegirCitasNormativas(texto, fundamentos, secciones);
        var verificada = _verificadorSalida.Verificar(sesion.Entrada.Usuario, texto, candidatas, autorizados, sesion.Registro.Fuentes, _baseConocimiento, directorio);
        return Responder(sesion, verificada.Respuesta, CitasFinales(sesion, verificada), verificada.Resultado);
    }

    private IReadOnlyList<Cita> CitasFinales(SesionPregunta sesion, SalidaVerificada verificada)
    {
        if (verificada.Resultado != ResultadoAuditoria.Respondida)
        {
            return verificada.Citas;
        }

        var reglas = _verificadorSalida.DescartarCitasInexistentes(sesion.Registro.Reglas, _baseConocimiento);
        return [.. verificada.Citas.Concat(reglas).Concat(sesion.Registro.Citas).Distinct()];
    }

    private IReadOnlyList<Cita> ElegirCitasNormativas(string texto, IReadOnlyList<SeccionDocumento> fundamentos, IReadOnlyList<SeccionDocumento> secciones)
    {
        var explicitas = _verificadorSalida.DescartarCitasInexistentes(ExtractorCitas.De(texto), _baseConocimiento);
        return explicitas.Count > 0
            ? explicitas
            : [.. fundamentos.Concat(secciones).Select(s => new Cita(s.Documento, s.Seccion)).Distinct()];
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
            sesion.Uso.Total,
            CalculadoraCosto.Calcular(sesion.Uso, _configuracion.Precio));
        Auditar(sesion, resultado, null, citas);
        return new ResultadoPregunta(TipoResultadoPregunta.Respondida, respuesta, resultado, null);
    }

    private ResultadoPregunta Denegar(SesionPregunta sesion, MotivoDenegacion? motivo, int tokens)
    {
        var costo = CalculadoraCosto.Calcular(sesion.Uso, _configuracion.Precio);
        var citas = new[] { Textos.CitaNegativa() };
        var respuesta = new RespuestaAsistente(Textos.Negativa(sesion.Entrada.Usuario.Rol), citas, false, tokens, costo);
        Auditar(sesion, ResultadoAuditoria.Denegada, motivo, citas);
        return new ResultadoPregunta(TipoResultadoPregunta.Denegada, respuesta, ResultadoAuditoria.Denegada, motivo);
    }

    private ResultadoPregunta NoDisponible(SesionPregunta sesion, ResultadoAuditoria resultado)
    {
        Auditar(sesion, resultado, null, []);
        return new ResultadoPregunta(TipoResultadoPregunta.NoDisponible, null, resultado, null);
    }

    private void Auditar(SesionPregunta sesion, ResultadoAuditoria resultado, MotivoDenegacion? motivo, IReadOnlyList<Cita> citas)
    {
        var usuario = sesion.Entrada.Usuario;
        var pregunta = AuditorPeticiones.SanearPregunta(usuario, sesion.Entrada.Pregunta, sesion.Directorio ?? ConstructorDirectorio.Vacio);
        var costo = CalculadoraCosto.Calcular(sesion.Uso, _configuracion.Precio);
        var registro = sesion.Registro;
        _auditor.Registrar(new EventoAuditoria(
            _auditor.AhoraUtc, sesion.CorrelationId, sesion.IpOrigen, usuario.Rol, usuario.Id, pregunta, resultado, motivo,
            registro.Estudiantes, registro.Recursos, citas, sesion.Uso.Total, costo,
            sesion.Cronometro.ElapsedMilliseconds, _configuracion.Precio is null)
        {
            DatosConsultados = registro.DatosConsultados
        });
    }

    private sealed class SesionPregunta(PreguntaEntrada entrada, string ipOrigen, string correlationId, ApiAcademicaConPresupuesto api)
    {
        public PreguntaEntrada Entrada { get; } = entrada;

        public string IpOrigen { get; } = ipOrigen;

        public string CorrelationId { get; } = correlationId;

        public ApiAcademicaConPresupuesto Api { get; } = api;

        public Stopwatch Cronometro { get; } = Stopwatch.StartNew();

        public RegistroConsultas Registro { get; } = new();

        public DirectorioAcademico? Directorio { get; set; }

        public UsoTokens Uso { get; set; } = new(0, 0);
    }
}
