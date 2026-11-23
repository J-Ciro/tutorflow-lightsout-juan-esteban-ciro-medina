using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class CitasYNumerosTests
{
    private const string Reglamento = "reglamento_academico_2025.md";
    private const string Evaluacion = "evaluacion_y_calificaciones.md";
    private const string Bienestar = "bienestar_y_becas.md";
    private static readonly Usuario Estudiante = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Coordinador = new(Rol.Coordinador, "COORD-01");
    private static readonly Cita Articulo22 = new(Reglamento, "Artículo 22");

    private static readonly SeccionDocumento Articulo1 = new(Reglamento, "Artículo 1", "Principios generales", "Texto general sin cifras.");
    private static readonly SeccionDocumento Articulo2 = new(Reglamento, "Artículo 2", "Criterio especial", "El criterio de referencia es 4.40.");
    private static readonly SeccionDocumento Articulo5 = new(Reglamento, "Artículo 5", "Nota mínima", "Se aprueba con una definitiva suficiente.");
    private static readonly SeccionDocumento Seccion4 = new(Evaluacion, "Sección 4", "Promedio", "El promedio se pondera por créditos.");
    private static readonly SeccionDocumento HorarioBienestar = new(Bienestar, "Sección 2", "Horario de atención", "Bienestar atiende en la oficina principal.");

    [Fact]
    [Trait("ESC", "ESC-157")]
    [Trait("CA", "CA-05")]
    [Trait("SEG", "SEG-10c")]
    public async Task Numero_que_solo_aparece_en_una_seccion_no_entregada_no_se_verifica()
    {
        var indice = new IndiceFijo([Articulo1, Articulo2], [Articulo1]);
        var escenario = Con(Fijo($"Tu promedio es 4.40, según el {Reglamento} · Artículo 2"), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, PreguntaPromedio);

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
    }

    [Fact]
    [Trait("ESC", "ESC-157")]
    [Trait("SEG", "SEG-10c")]
    public async Task Numero_presente_en_una_seccion_entregada_se_verifica()
    {
        var indice = new IndiceFijo([Articulo1, Articulo2], [Articulo2]);
        var escenario = Con(Fijo("El criterio de referencia es 4.40."), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, "¿Cuál es el criterio de referencia?");

        Assert.Equal("El criterio de referencia es 4.40.", resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Fact]
    [Trait("ESC", "ESC-162")]
    [Trait("CA", "CA-05")]
    [Trait("COMP", "COMP-02")]
    public async Task Citas_de_documentos_son_solo_de_secciones_entregadas_y_no_hay_citas_de_datos_sin_lectura()
    {
        var indice = new IndiceFijo([Articulo1, Articulo5, Seccion4, HorarioBienestar], [HorarioBienestar]);
        var texto = $"Bienestar atiende en la oficina principal. {Reglamento} · Artículo 1, {Reglamento} · Artículo 5, {Evaluacion} · Sección 4, {Bienestar} · Sección 2";
        var escenario = Con(Fijo(texto), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, "¿Cuál es el horario de atención de Bienestar?");

        Assert.Equal([new Cita(Bienestar, "Sección 2")], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    public async Task Una_seccion_entregada_que_el_modelo_no_cito_no_se_agrega_como_cita_de_relleno()
    {
        var indice = new IndiceFijo([Articulo5, Seccion4], [Articulo5, Seccion4]);
        var escenario = Con(Fijo($"Se aprueba con una definitiva suficiente ({Reglamento} · Artículo 5)."), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, "¿Con qué nota se aprueba una asignatura?");

        Assert.Equal([new Cita(Reglamento, "Artículo 5")], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    public async Task Si_el_modelo_no_cita_ninguna_seccion_no_se_devuelven_citas_de_documentos()
    {
        var indice = new IndiceFijo([Articulo5, Seccion4], [Articulo5, Seccion4]);
        var escenario = Con(Fijo("Se aprueba con una definitiva suficiente."), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, "¿Con qué nota se aprueba una asignatura?");

        Assert.Empty(resultado.Respuesta!.Citas);
    }

    [Fact]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10a")]
    public async Task Una_cita_existente_en_el_indice_pero_no_entregada_se_descarta()
    {
        var indice = new IndiceFijo([Articulo5, Seccion4], [Articulo5]);
        var escenario = Con(Fijo($"Se promedia por créditos ({Evaluacion} · Sección 4)."), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, "¿Con qué nota se aprueba una asignatura?");

        Assert.Empty(resultado.Respuesta!.Citas);
    }

    [Fact]
    [Trait("ESC", "ESC-156")]
    [Trait("COMP", "COMP-02")]
    public async Task Negativa_tardia_de_coordinador_lleva_solo_el_Articulo_22_aunque_el_modelo_cite_otras_secciones()
    {
        var indice = new IndiceFijo([Articulo5, Seccion4], [Articulo5, Seccion4]);
        var escenario = Con((_, _) => Herramienta(HerramientasAsistente.Estudiante, """{"estudianteId":"EST-004"}"""), indice);

        var resultado = await escenario.PreguntarAsync(Coordinador, "¿Qué estudiantes de mi programa perdieron materias?");

        Assert.Equal(TipoResultadoPregunta.Denegada, resultado.Tipo);
        Assert.Equal([Articulo22], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("ESC", "ESC-156")]
    [Trait("COMP", "COMP-03")]
    public async Task Texto_de_COMP_03_lleva_citas_vacias()
    {
        var indice = new IndiceFijo([Articulo5, Seccion4], [Articulo5, Seccion4]);
        var escenario = Con(Fijo(Textos.SinInformacion), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, "¿Qué dice el reglamento sobre el uso de parqueaderos?");

        Assert.Equal(Textos.SinInformacion, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-156")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10b")]
    public async Task Fuga_de_un_tercero_lleva_el_texto_de_negativa_y_solo_el_Articulo_22()
    {
        var indice = new IndiceFijo([Articulo5, Seccion4], [Articulo5, Seccion4]);
        var texto = $"Andres Martinez sacó 2.6 en Cálculo ({Reglamento} · Artículo 5, {Evaluacion} · Sección 4)";
        var escenario = Con(Fijo(texto), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, PreguntaPromedio);

        Assert.Equal(Textos.NegativaEstudiante, resultado.Respuesta!.Respuesta);
        Assert.Equal([Articulo22], resultado.Respuesta.Citas.ToArray());
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
    }

    [Fact]
    [Trait("ESC", "ESC-156")]
    [Trait("COMP", "COMP-02")]
    [Trait("SEG", "SEG-10c")]
    public async Task Texto_no_verificable_lleva_citas_vacias_aunque_el_modelo_cite_secciones()
    {
        var indice = new IndiceFijo([Articulo5, Seccion4], [Articulo5, Seccion4]);
        var escenario = Con(Fijo($"Tu promedio es 9.99 ({Reglamento} · Artículo 5)"), indice);

        var resultado = await escenario.PreguntarAsync(Estudiante, "¿Cuál es el criterio para el promedio?");

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
    }
}
