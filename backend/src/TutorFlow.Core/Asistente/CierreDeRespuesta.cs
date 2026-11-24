using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed record CierreEvaluado(
    string Texto,
    IReadOnlyList<Cita> Citas,
    ResultadoAuditoria Resultado,
    bool? UsoDatos,
    bool Bloqueado,
    string? MotivoDeReintento)
{
    public static CierreEvaluado Entregable(string texto, IReadOnlyList<Cita> citas, ResultadoAuditoria resultado, bool? usoDatos = null) =>
        new(texto, citas, resultado, usoDatos, false, null);
}

internal sealed class CierreDeRespuesta
{
    private readonly IBaseConocimiento _baseConocimiento;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly VerificadorSalida _verificadorSalida;
    private readonly ConfiguracionAsistente _configuracion;
    private readonly CitasDeRespuesta _citasDeRespuesta;
    private readonly CitasDeReglasAplicadas _citasDeReglas;
    private readonly AfirmacionesDePerdida _afirmacionesDePerdida;
    private readonly DefinitivaPorAsignatura _definitivaPorAsignatura;
    private readonly AfirmacionesDeEstado _afirmacionesDeEstado;

    public CierreDeRespuesta(
        IBaseConocimiento baseConocimiento,
        PoliticaAcceso politicaAcceso,
        VerificadorSalida verificadorSalida,
        ConfiguracionAsistente configuracion)
    {
        _baseConocimiento = baseConocimiento;
        _politicaAcceso = politicaAcceso;
        _verificadorSalida = verificadorSalida;
        _configuracion = configuracion;
        _citasDeRespuesta = new CitasDeRespuesta(configuracion.ApoyoMinimoRespaldo);
        _definitivaPorAsignatura = new DefinitivaPorAsignatura(configuracion.Catalogo);
        _citasDeReglas = new CitasDeReglasAplicadas(baseConocimiento, _definitivaPorAsignatura);
        _afirmacionesDePerdida = new AfirmacionesDePerdida(configuracion.Catalogo);
        _afirmacionesDeEstado = new AfirmacionesDeEstado(configuracion.Catalogo);
    }

    public CierreEvaluado Evaluar(SesionPregunta sesion, string texto, EntradaConversacion entrada)
    {
        var propio = IntroduccionDelModelo(sesion, texto);
        if (propio.Trim() == Textos.SinInformacion)
        {
            return SinIntroduccion(sesion, entrada);
        }

        var escritas = EscritasValidas(sesion, propio, entrada);
        var limpio = Limpiar(propio, escritas);
        return ReferenciasEscritas.TieneContenido(limpio) || HayFuga(sesion, limpio, entrada)
            ? Verificar(sesion, limpio, escritas, entrada)
            : SinIntroduccion(sesion, entrada);
    }

    private static string IntroduccionDelModelo(SesionPregunta sesion, string texto) =>
        sesion.Desglose is null ? texto : IntroduccionDelDesglose.SinCifras(texto);

    private static string Limpiar(string texto, IReadOnlyList<Cita> escritas) =>
        Textos.CitasDeTextoFijo(texto) is null ? ReferenciasEscritas.Limpiar(texto, escritas) : texto;

    private CierreEvaluado SinIntroduccion(SesionPregunta sesion, EntradaConversacion entrada) =>
        sesion.Desglose is null
            ? CierreEvaluado.Entregable(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion)
            : Completar(sesion, string.Empty, [], entrada);

    private bool HayFuga(SesionPregunta sesion, string limpio, EntradaConversacion entrada) =>
        DetectorFugaConfiguracion.HayFuga(limpio, Confidencial(sesion, entrada), sesion.Registro.Fuentes);

    private IReadOnlyList<Cita> EscritasValidas(SesionPregunta sesion, string texto, EntradaConversacion entrada)
    {
        var usadas = _citasDeReglas.Usadas(sesion.Registro, ExtractorCitas.SinCitas(texto), sesion.Directorio);
        return _verificadorSalida.DescartarCitasInexistentes(
            _citasDeRespuesta.EscritasConApoyo(texto, entrada.Secciones, sesion.Registro.Reglas, usadas),
            _baseConocimiento);
    }

    private CierreEvaluado Verificar(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var autorizados = _politicaAcceso.IdsPersonasAutorizadas(sesion.Entrada.Usuario, sesion.Directorio);
        var verificada = VerificarSalida(sesion, limpio, escritas, autorizados, entrada);
        if (verificada.Resultado != ResultadoAuditoria.Respondida)
        {
            return new CierreEvaluado(verificada.Respuesta, verificada.Citas, verificada.Resultado, null, true, verificada.MotivoDeReintento);
        }

        var motivo = MotivoDeAfirmacionNoVerificable(sesion, limpio, autorizados);
        return motivo is null
            ? Completar(sesion, limpio, escritas, entrada)
            : new CierreEvaluado(Textos.NoVerificable, [], ResultadoAuditoria.EstadoNoVerificado, null, true, motivo);
    }

    private SalidaVerificada VerificarSalida(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, IReadOnlyCollection<string> autorizados, EntradaConversacion entrada) =>
        _verificadorSalida.Verificar(
            sesion.Entrada.Usuario,
            limpio,
            escritas,
            autorizados,
            sesion.Registro.Fuentes,
            _baseConocimiento,
            sesion.Directorio,
            Confidencial(sesion, entrada));

    private string? MotivoDeAfirmacionNoVerificable(SesionPregunta sesion, string respuesta, IReadOnlyCollection<string> autorizados)
    {
        var usuario = sesion.Entrada.Usuario;
        var bloques = BloquesDeRespuesta.De(respuesta);
        var resumenes = sesion.Registro.Resumenes;
        var contexto = new ContextoDeAfirmaciones(usuario, autorizados, sesion.Directorio, resumenes);
        return _afirmacionesDePerdida.Motivo(bloques, usuario, autorizados, sesion.Directorio, resumenes)
            ?? _afirmacionesDeEstado.Motivo(bloques, contexto)
            ?? _definitivaPorAsignatura.Motivo(bloques, usuario, autorizados, sesion.Directorio, resumenes)
            ?? CamposLigados.Motivo(bloques, contexto);
    }

    private CierreEvaluado Completar(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var citasDeTextoFijo = Textos.CitasDeTextoFijo(limpio);
        if (citasDeTextoFijo is not null)
        {
            return CierreEvaluado.Entregable(limpio, citasDeTextoFijo, ResultadoAuditoria.Respondida);
        }

        var completo = ConDesglose(sesion, limpio);
        var documentos = CitasDeDocumento(sesion, completo, escritas, entrada);
        if (documentos.Count == 0 && !sesion.Registro.UsoDatos)
        {
            return new CierreEvaluado(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, false, true, null);
        }

        IReadOnlyList<Cita> citas = [.. documentos.Concat(sesion.Registro.Citas).Distinct()];
        return CierreEvaluado.Entregable(completo, citas, ResultadoAuditoria.Respondida);
    }

    private static string ConDesglose(SesionPregunta sesion, string introduccion)
    {
        if (sesion.Desglose is not { } desglose)
        {
            return introduccion;
        }

        return introduccion.Length == 0 ? desglose : $"{introduccion}\n\n{desglose}";
    }

    private IReadOnlyList<Cita> CitasDeDocumento(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var usadas = _citasDeReglas.Usadas(sesion.Registro, limpio, sesion.Directorio);
        var vigentes = escritas.Where(cita => !sesion.Registro.Reglas.Contains(cita) || usadas.Contains(cita)).ToList();
        var reglas = _citasDeReglas.De(usadas, sesion.Entrada.Pregunta, vigentes);
        IReadOnlyList<Cita> sinRespaldo = [.. vigentes.Concat(reglas).Concat(_citasDeRespuesta.PorTema(limpio, entrada.Secciones)).Distinct()];
        return sinRespaldo.Count > 0 || sesion.Registro.UsoDatos ? sinRespaldo : _citasDeRespuesta.Respaldo(limpio, entrada.Secciones);
    }

    private MaterialConfidencial Confidencial(SesionPregunta sesion, EntradaConversacion entrada) =>
        new(
            _configuracion.MarcadorCanario,
            [ConstructorPrompts.Instrucciones(entrada.PromptSistema), .. DescripcionesDeHerramientas(sesion.Entrada.Usuario.Rol)],
            HerramientasAsistente.NombresInternos);

    private static IEnumerable<string> DescripcionesDeHerramientas(Rol rol) =>
        HerramientasAsistente.Definir(rol)
            .SelectMany(herramienta => herramienta.Parametros.Select(parametro => parametro.Descripcion).Prepend(herramienta.Descripcion));
}
