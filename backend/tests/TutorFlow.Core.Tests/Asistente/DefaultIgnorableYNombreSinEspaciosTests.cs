using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class DefaultIgnorableYNombreSinEspaciosTests
{
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly PoliticaAcceso Politica = new();

    public static TheoryData<string> RellenosDefaultIgnorable => new()
    {
        "ㅤ",
        "ᅟ",
        "ᅠ",
        "ﾠ"
    };

    public static TheoryData<string> NombresDeAndresMartinezSinOConEspacios => new()
    {
        "A n d r é s M a r t í n e z",
        "AndrésMartínez",
        "AndrésㅤMartínez"
    };

    [Theory]
    [MemberData(nameof(RellenosDefaultIgnorable))]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-78")]
    public void ESC_199_a10_El_normalizador_elimina_los_Default_Ignorable_y_une_el_id(string relleno)
    {
        Assert.Equal("est002", NormalizadorTexto.Normalizar($"EST{relleno}002"));
    }

    [Theory]
    [MemberData(nameof(RellenosDefaultIgnorable))]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-78")]
    public async Task ESC_199_a10_El_id_ajeno_con_relleno_Hangul_da_403_sin_leer_a_EST_002_y_se_audita_enmascarado(string relleno)
    {
        var escenario = Con(Fijo("No debe llamarse."));

        var resultado = await escenario.PreguntarAsync(Est001, $"notas de EST{relleno}002");

        Assert.Equal(TipoResultadoPregunta.Denegada, resultado.Tipo);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, resultado.Motivo);
        Assert.Empty(escenario.Llm.Solicitudes);
        Assert.DoesNotContain("EST-002", escenario.Api.LecturasNotas);
        Assert.Equal("notas de [TERCERO]", Assert.Single(escenario.Auditoria.Eventos).Pregunta);
    }

    [Fact]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-78")]
    public async Task ESC_199_a10_El_nombre_ajeno_con_relleno_Hangul_da_403_y_se_audita_enmascarado()
    {
        var escenario = Con(Fijo("No debe llamarse."));

        var resultado = await escenario.PreguntarAsync(Est001, "notas de AndrésㅤMartínez");

        Assert.Equal(MotivoDenegacion.TerceroMencionado, resultado.Motivo);
        Assert.Empty(escenario.Llm.Solicitudes);
        Assert.Equal("notas de [TERCERO]", Assert.Single(escenario.Auditoria.Eventos).Pregunta);
    }

    [Theory]
    [MemberData(nameof(NombresDeAndresMartinezSinOConEspacios))]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-78")]
    public void ESC_199_a11_El_nombre_completo_de_ocho_letras_o_mas_se_compara_sin_espacios_en_la_pregunta(string nombre)
    {
        var decision = Politica.EvaluarPregunta(Est001, $"¿Cuál es el promedio de {nombre}?", Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, decision.Motivo);
    }

    [Fact]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-78")]
    public void ESC_199_a11_Un_nombre_de_pila_suelto_aunque_lleve_espacios_no_deniega()
    {
        var decision = Politica.EvaluarPregunta(Est001, "¿Cuál es mi promedio? A n d r é s", Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }

    [Theory]
    [InlineData("M a t e o V a r g a s perdió Estadística.")]
    [InlineData("MateoVargas perdió Estadística.")]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-78")]
    public async Task ESC_199_a11_El_nombre_ajeno_con_letras_separadas_o_pegadas_en_la_respuesta_se_bloquea(string texto)
    {
        var escenario = Con(Fijo(texto));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(Textos.NegativaEstudiante, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }
}
