using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class NormalizacionNfkcTests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly PoliticaAcceso Politica = new();
    private static readonly VerificadorSalida Verificador = new();
    private static readonly string[] SoloEst001 = ["EST-001"];

    public static TheoryData<string> VariantesDeEst002 => new()
    {
        "𝐄𝐒𝐓-𝟎𝟎𝟐",
        "𝙴𝚂𝚃-𝟶𝟶𝟸",
        "𝗘𝗦𝗧-𝟬𝟬𝟮",
        "ＥＳＴ-００２",
        "EST-⁰⁰²",
        "EST-₀₀₂",
        "ⒺⓈⓉ-002",
        "EST-⓪⓪②"
    };

    [Theory]
    [MemberData(nameof(VariantesDeEst002))]
    [Trait("ESC", "ESC-149")]
    [Trait("SEG", "SEG-04a")]
    public void Normalizador_pliega_las_variantes_de_compatibilidad_a_est_002(string variante)
    {
        Assert.Equal("est 002", NormalizadorTexto.Normalizar(variante));
    }

    [Theory]
    [InlineData("ﬁnal", "final")]
    [InlineData("ﬂujo de ﬀ", "flujo de ff")]
    [InlineData("ﬃ", "ffi")]
    [InlineData("𝐂𝐚𝐥𝐜𝐮𝐥𝐨", "calculo")]
    [Trait("SEG", "SEG-04a")]
    public void Normalizador_pliega_ligaduras_y_alfanumericos_matematicos(string entrada, string esperado)
    {
        Assert.Equal(esperado, NormalizadorTexto.Normalizar(entrada));
    }

    [Theory]
    [MemberData(nameof(VariantesDeEst002))]
    [Trait("ESC", "ESC-149")]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "SEG-04")]
    public void Detector_de_terceros_deniega_las_variantes_de_compatibilidad_de_un_id_ajeno(string variante)
    {
        var decision = Politica.EvaluarPregunta(Est001, $"¿Cuáles son las notas de {variante}?", Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(VariantesDeEst002))]
    [Trait("ESC", "ESC-149")]
    [Trait("SEG", "SEG-10b")]
    public void Verificador_bloquea_la_variante_de_compatibilidad_de_un_id_ajeno_en_la_respuesta(string variante)
    {
        Assert.True(Verificador.ContienePersonaNoAutorizada($"Las notas de {variante} son bajas", SoloEst001, Directorio));
    }

    [Theory]
    [MemberData(nameof(VariantesDeEst002))]
    [Trait("ESC", "ESC-149")]
    [Trait("SEG", "SEG-04a")]
    public void Enmascarado_de_auditoria_cubre_las_variantes_de_compatibilidad(string variante)
    {
        var resultado = DetectorTerceros.EnmascararTerceros(Est001, $"Notas de {variante}", Directorio);

        Assert.Equal("Notas de [TERCERO]", resultado);
    }

    [Fact]
    [Trait("SEG", "SEG-04a")]
    public void El_id_propio_en_variante_de_compatibilidad_no_deniega()
    {
        var decision = Politica.EvaluarPregunta(Est001, "¿Cuáles son las notas de 𝐄𝐒𝐓-𝟎𝟎𝟏?", Directorio);

        Assert.True(decision.Permitido);
    }

    [Fact]
    [Trait("ESC", "ESC-120")]
    [Trait("SEG", "SEG-04a")]
    public void El_codigo_de_asignatura_en_variante_de_compatibilidad_sigue_sin_ser_persona()
    {
        var decision = Politica.EvaluarPregunta(Est001, "¿Cómo voy en 𝐄𝐒𝐓-𝟏𝟎𝟑?", Directorio);

        Assert.True(decision.Permitido);
    }
}
