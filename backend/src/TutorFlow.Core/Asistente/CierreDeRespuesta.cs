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
        if (texto.Trim() == Textos.SinInformacion)
        {
            return SinIntroduccion(sesion, entrada);
        }

        var escritas = EscritasValidas(sesion, texto, entrada);
        var previo = RechazoPrevio(sesion, Limpiar(texto, escritas), entrada);
        return previo is null ? EvaluarPodado(sesion, texto, escritas, entrada) : Bloqueado(previo);
    }

    private CierreEvaluado EvaluarPodado(SesionPregunta sesion, string texto, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var podado = Podar(sesion, texto, escritas);
        var finales = podado == texto ? escritas : EscritasValidas(sesion, podado, entrada);
        var limpio = SinCitasDelBloque(sesion, Limpiar(podado, finales));
        return ReferenciasEscritas.TieneContenido(limpio) || HayFuga(sesion, limpio, entrada)
            ? Verificar(sesion, limpio, finales, entrada)
            : SinIntroduccion(sesion, entrada);
    }

    private static string SinCitasDelBloque(SesionPregunta sesion, string limpio) =>
        sesion.Bloque is null || Textos.CitasDeTextoFijo(limpio) is not null ? limpio : PodaDeOraciones.SinCitasEntreParentesis(limpio);

    private string Podar(SesionPregunta sesion, string texto, IReadOnlyList<Cita> escritas)
    {
        if (Textos.CitasDeTextoFijo(texto) is not null)
        {
            return texto;
        }

        var catalogo = CatalogoAsignaturas.Vigente(_configuracion.Catalogo, sesion.Directorio);
        var contexto = new ContextoDePoda(sesion.Directorio, catalogo, escritas, NombresDelBloque(sesion));
        return sesion.Bloque is null ? PodaDeOraciones.FueraDelBloque(texto, contexto) : PodaDeOraciones.EnModoBloque(texto, contexto);
    }

    private static List<string> NombresDelBloque(SesionPregunta sesion) =>
        [.. sesion.Directorio.Estudiantes
            .Where(estudiante => sesion.PersonasDelBloque.Contains(estudiante.Id))
            .Select(estudiante => NormalizadorTexto.Normalizar(estudiante.Nombre))];

    private SalidaVerificada? RechazoPrevio(SesionPregunta sesion, string limpio, EntradaConversacion entrada)
    {
        var autorizados = _politicaAcceso.IdsPersonasAutorizadas(sesion.Entrada.Usuario, sesion.Directorio);
        return _verificadorSalida.RechazoPrevio(sesion.Entrada.Usuario, limpio, autorizados, sesion.Registro.Fuentes, sesion.Directorio, Confidencial(sesion, entrada));
    }

    private static CierreEvaluado Bloqueado(SalidaVerificada verificada) =>
        new(verificada.Respuesta, verificada.Citas, verificada.Resultado, null, true, verificada.MotivoDeReintento);

    private static string Limpiar(string texto, IReadOnlyList<Cita> escritas) =>
        Textos.CitasDeTextoFijo(texto) is null ? ReferenciasEscritas.Limpiar(texto, escritas) : texto;

    private CierreEvaluado SinIntroduccion(SesionPregunta sesion, EntradaConversacion entrada) =>
        sesion.Bloque is null
            ? CierreEvaluado.Entregable(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion)
            : Completar(sesion, string.Empty, [], entrada);

    private bool HayFuga(SesionPregunta sesion, string limpio, EntradaConversacion entrada) =>
        DetectorFugaConfiguracion.HayFuga(limpio, Confidencial(sesion, entrada), sesion.Registro.Fuentes);

    private IReadOnlyList<Cita> EscritasValidas(SesionPregunta sesion, string texto, EntradaConversacion entrada)
    {
        var respuestaFinal = sesion.Bloque is null ? texto : $"{texto}\n{sesion.Bloque}";
        IReadOnlyCollection<Cita> secciones = sesion.Registro.UsoDatos ? CitasDeReglasAplicadas.SeccionesDeLaTabla : sesion.Registro.Reglas;
        var usadas = _citasDeReglas.Usadas(sesion.Registro, ExtractorCitas.SinCitas(respuestaFinal), sesion.Directorio);
        return _verificadorSalida.DescartarCitasInexistentes(
            _citasDeRespuesta.EscritasConApoyo(texto, entrada.Secciones, secciones, usadas),
            _baseConocimiento);
    }

    private CierreEvaluado Verificar(SesionPregunta sesion, string limpio, IReadOnlyList<Cita> escritas, EntradaConversacion entrada)
    {
        var autorizados = _politicaAcceso.IdsPersonasAutorizadas(sesion.Entrada.Usuario, sesion.Directorio);
        var verificada = VerificarSalida(sesion, limpio, escritas, autorizados, entrada);
        if (verificada.Resultado != ResultadoAuditoria.Respondida)
        {
            return Bloqueado(verificada);
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

        var completo = ConBloque(sesion, limpio); var documentos = CitasDeDocumento(sesion, completo, escritas, entrada);
        if (documentos.Count == 0 && !sesion.Registro.UsoDatos)
        {
            return new CierreEvaluado(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion, false, true, null);
        }

        IReadOnlyList<Cita> citas = [.. documentos.Concat(sesion.Registro.Citas).Distinct()];
        return CierreEvaluado.Entregable(DatosObligatorios.Anexar(completo, citas), citas, ResultadoAuditoria.Respondida);
    }

    private static string ConBloque(SesionPregunta sesion, string introduccion)
    {
        if (sesion.Bloque is not { } bloque)
        {
            return introduccion;
        }

        return introduccion.Length == 0 ? bloque : $"{introduccion}\n\n{bloque}";
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
