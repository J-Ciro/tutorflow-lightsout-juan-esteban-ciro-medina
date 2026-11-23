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
    }

    public CierreEvaluado Evaluar(SesionPregunta sesion, string texto, EntradaConversacion entrada)
    {
        if (texto.Trim() == Textos.SinInformacion)
        {
            return CierreEvaluado.Entregable(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, usoDatos: false);
        }

        foreach (var seccion in entrada.Secciones)
        {
            sesion.Registro.AgregarFuente(ConstructorPrompts.ContenidoEntregado(seccion));
        }

        var escritas = EscritasValidas(sesion, texto, entrada);
        var limpio = Textos.CitasDeTextoFijo(texto) is null ? ReferenciasEscritas.Limpiar(texto, escritas) : texto;
        return ReferenciasEscritas.TieneContenido(limpio)
            ? Verificar(sesion, limpio, escritas, entrada)
            : CierreEvaluado.Entregable(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion);
    }

    private IReadOnlyList<Cita> EscritasValidas(SesionPregunta sesion, string texto, EntradaConversacion entrada) =>
        _verificadorSalida.DescartarCitasInexistentes(
            _citasDeRespuesta.EscritasConApoyo(texto, entrada.Secciones, sesion.Registro.Reglas),
            _baseConocimiento);

    private CierreEvaluado Verificar(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var autorizados = _politicaAcceso.IdsPersonasAutorizadas(sesion.Entrada.Usuario, sesion.Directorio);
        var verificada = _verificadorSalida.Verificar(
            sesion.Entrada.Usuario,
            limpio,
            escritas,
            autorizados,
            sesion.Registro.Fuentes,
            _baseConocimiento,
            sesion.Directorio,
            Confidencial(sesion, entrada));
        if (verificada.Resultado != ResultadoAuditoria.Respondida)
        {
            return new CierreEvaluado(verificada.Respuesta, verificada.Citas, verificada.Resultado, null, true, verificada.MotivoDeReintento);
        }

        var motivo = MotivoDeAfirmacionNoVerificable(sesion, limpio, autorizados);
        return motivo is null
            ? Completar(sesion, limpio, escritas, entrada)
            : new CierreEvaluado(Textos.NoVerificable, [], ResultadoAuditoria.EstadoNoVerificado, null, true, motivo);
    }

    private string? MotivoDeAfirmacionNoVerificable(SesionPregunta sesion, string respuesta, IReadOnlyCollection<string> autorizados)
    {
        var usuario = sesion.Entrada.Usuario;
        var bloques = BloquesDeRespuesta.De(respuesta);
        var resumenes = sesion.Registro.Resumenes;
        return _afirmacionesDePerdida.Motivo(bloques, usuario, autorizados, sesion.Directorio, resumenes)
            ?? _definitivaPorAsignatura.Motivo(bloques, usuario, autorizados, sesion.Directorio, resumenes);
    }

    private CierreEvaluado Completar(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var citasDeTextoFijo = Textos.CitasDeTextoFijo(limpio);
        if (citasDeTextoFijo is not null)
        {
            return CierreEvaluado.Entregable(limpio, citasDeTextoFijo, ResultadoAuditoria.Respondida);
        }

        var documentos = CitasDeDocumento(sesion, limpio, escritas, entrada);
        if (documentos.Count == 0 && !sesion.Registro.UsoDatos)
        {
            return new CierreEvaluado(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, false, true, null);
        }

        IReadOnlyList<Cita> citas = [.. documentos.Concat(sesion.Registro.Citas).Distinct()];
        return CierreEvaluado.Entregable(limpio, citas, ResultadoAuditoria.Respondida);
    }

    private IReadOnlyList<Cita> CitasDeDocumento(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var reglas = _citasDeReglas.De(sesion.Registro, sesion.Entrada.Pregunta, limpio, escritas, sesion.Directorio);
        IReadOnlyList<Cita> sinRespaldo = [.. escritas.Concat(reglas).Concat(_citasDeRespuesta.PorTema(limpio, entrada.Secciones)).Distinct()];
        return sinRespaldo.Count > 0 ? sinRespaldo : _citasDeRespuesta.Respaldo(limpio, entrada.Secciones);
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
