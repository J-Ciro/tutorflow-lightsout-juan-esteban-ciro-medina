using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class CierreDeRespuesta
{
    private const int MaximoCitasDeReglasAplicadas = 3;

    private readonly IBaseConocimiento _baseConocimiento;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly VerificadorSalida _verificadorSalida;
    private readonly ConfiguracionAsistente _configuracion;
    private readonly EmisorDeResultados _emisor;
    private readonly CitasDeRespuesta _citasDeRespuesta;
    private readonly OrdenDeReglasPorRelevancia _ordenDeReglas;
    private readonly AfirmacionesDePerdida _afirmacionesDePerdida;
    private readonly DefinitivaPorAsignatura _definitivaPorAsignatura;

    public CierreDeRespuesta(
        IBaseConocimiento baseConocimiento,
        PoliticaAcceso politicaAcceso,
        VerificadorSalida verificadorSalida,
        ConfiguracionAsistente configuracion,
        EmisorDeResultados emisor)
    {
        _baseConocimiento = baseConocimiento;
        _politicaAcceso = politicaAcceso;
        _verificadorSalida = verificadorSalida;
        _configuracion = configuracion;
        _emisor = emisor;
        _citasDeRespuesta = new CitasDeRespuesta(configuracion.ApoyoMinimoRespaldo);
        _ordenDeReglas = new OrdenDeReglasPorRelevancia(baseConocimiento);
        _afirmacionesDePerdida = new AfirmacionesDePerdida(configuracion.Catalogo);
        _definitivaPorAsignatura = new DefinitivaPorAsignatura(configuracion.Catalogo);
    }

    public ResultadoPregunta Completar(SesionPregunta sesion, string texto, EntradaConversacion entrada)
    {
        if (texto.Trim() == Textos.SinInformacion)
        {
            return _emisor.SinInformacion(sesion);
        }

        foreach (var seccion in entrada.Secciones)
        {
            sesion.Registro.AgregarFuente($"{seccion.Titulo}. {seccion.Texto}");
        }

        var autorizados = _politicaAcceso.IdsPersonasAutorizadas(sesion.Entrada.Usuario, sesion.Directorio);
        var verificada = Verificar(sesion, texto, entrada, autorizados);
        return verificada.Resultado == ResultadoAuditoria.Respondida
            ? ResponderSiLasAfirmacionesSeRespaldan(sesion, verificada, entrada, autorizados)
            : _emisor.Responder(sesion, verificada.Respuesta, verificada.Citas, verificada.Resultado);
    }

    private SalidaVerificada Verificar(SesionPregunta sesion, string texto, EntradaConversacion entrada, IReadOnlyCollection<string> autorizados)
    {
        var candidatas = _citasDeRespuesta.EscritasConApoyo(texto, entrada.Secciones, sesion.Registro.Reglas);
        return _verificadorSalida.Verificar(
            sesion.Entrada.Usuario,
            texto,
            candidatas,
            autorizados,
            sesion.Registro.Fuentes,
            _baseConocimiento,
            sesion.Directorio,
            Confidencial(sesion, entrada));
    }

    private ResultadoPregunta ResponderSiLasAfirmacionesSeRespaldan(
        SesionPregunta sesion,
        SalidaVerificada verificada,
        EntradaConversacion entrada,
        IReadOnlyCollection<string> autorizados) =>
        AfirmacionesSonVerificables(sesion, verificada.Respuesta, autorizados)
            ? ResponderVerificada(sesion, verificada, entrada)
            : _emisor.Responder(sesion, Textos.NoVerificable, [], ResultadoAuditoria.EstadoNoVerificado);

    private bool AfirmacionesSonVerificables(SesionPregunta sesion, string respuesta, IReadOnlyCollection<string> autorizados)
    {
        var usuario = sesion.Entrada.Usuario;
        var resumenes = sesion.Registro.Resumenes;
        return _afirmacionesDePerdida.SonVerificables(respuesta, usuario.Rol, autorizados, sesion.Directorio, resumenes)
            && _definitivaPorAsignatura.SonVerificables(respuesta, usuario, autorizados, sesion.Directorio, resumenes);
    }

    private ResultadoPregunta ResponderVerificada(SesionPregunta sesion, SalidaVerificada verificada, EntradaConversacion entrada)
    {
        var citasDeTextoFijo = Textos.CitasDeTextoFijo(verificada.Respuesta);
        if (citasDeTextoFijo is not null)
        {
            return _emisor.Responder(sesion, verificada.Respuesta, citasDeTextoFijo, verificada.Resultado);
        }

        var citasDeDocumento = CitasDeDocumento(sesion, verificada, entrada);
        if (citasDeDocumento.Count == 0 && !sesion.Registro.UsoDatos)
        {
            return _emisor.SinInformacion(sesion);
        }

        IReadOnlyList<Cita> citas = [.. citasDeDocumento.Concat(sesion.Registro.Citas).Distinct()];
        return ResponderSinReferenciasHuerfanas(sesion, verificada, citas);
    }

    private ResultadoPregunta ResponderSinReferenciasHuerfanas(SesionPregunta sesion, SalidaVerificada verificada, IReadOnlyList<Cita> citas)
    {
        var limpia = ReferenciasEscritas.Limpiar(verificada.Respuesta, citas);
        return ReferenciasEscritas.TieneContenido(limpia)
            ? _emisor.Responder(sesion, limpia, citas, verificada.Resultado)
            : _emisor.Responder(sesion, Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion);
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
}
