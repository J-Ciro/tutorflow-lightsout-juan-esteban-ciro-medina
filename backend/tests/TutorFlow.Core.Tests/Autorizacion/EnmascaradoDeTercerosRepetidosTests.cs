using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class EnmascaradoDeTercerosRepetidosTests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");

    public static TheoryData<string, string> TercerosRepetidos => new()
    {
        { "Notas de Andrés Martínez y también de Andrés Martínez", "Notas de [TERCERO] y también de [TERCERO]" },
        { "Andrés Martínez, Andrés Martínez y Andrés Martínez", "[TERCERO], [TERCERO] y [TERCERO]" },
        { "notas de EST-002 y de EST-002", "notas de [TERCERO] y de [TERCERO]" },
        { "EST-002, EST002 y est 002", "[TERCERO], [TERCERO] y [TERCERO]" },
        { "Andrés Martínez (EST-002) y de nuevo Andrés Martínez", "[TERCERO] ([TERCERO]) y de nuevo [TERCERO]" },
        { "EST-002 es Andrés Martínez; repito EST-002", "[TERCERO] es [TERCERO]; repito [TERCERO]" },
        { "Andrés Martínez y Martínez Andrés", "[TERCERO] y [TERCERO]" },
        { "ANDRES MARTINEZ, andrés martínez y Andrés Martínez", "[TERCERO], [TERCERO] y [TERCERO]" },
        { "Camilo Rojas y Valentina Castro, otra vez Camilo Rojas y Valentina Castro", "[TERCERO] y [TERCERO], otra vez [TERCERO] y [TERCERO]" }
    };

    public static TheoryData<string, string> RepetidosEnIdentidadNoValidada => new()
    {
        { "Notas de Laura Gómez y de Laura Gómez", "Notas de [TERCERO] y de [TERCERO]" },
        { "Laura Gómez (EST-001) y Laura Gómez", "[TERCERO] ([TERCERO]) y [TERCERO]" }
    };

    [Theory]
    [MemberData(nameof(TercerosRepetidos))]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "SEG-04a")]
    [Trait("HALLAZGO", "H-01")]
    public void Un_tercero_mencionado_varias_veces_se_enmascara_en_todas_sus_ocurrencias(string pregunta, string esperada)
    {
        var resultado = DetectorTerceros.EnmascararTerceros(Est001, pregunta, Directorio);

        Assert.Equal(esperada, resultado);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "SEG-04a")]
    [Trait("HALLAZGO", "H-01")]
    public void Lo_propio_repetido_no_se_enmascara_pero_el_tercero_repetido_si()
    {
        var resultado = DetectorTerceros.EnmascararTerceros(Est001, "Laura Gómez, Andrés Martínez, Laura Gómez y Andrés Martínez", Directorio);

        Assert.Equal("Laura Gómez, [TERCERO], Laura Gómez y [TERCERO]", resultado);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "SEG-04a")]
    [Trait("HALLAZGO", "H-01")]
    public void El_coordinador_repetido_no_enmascara_a_sus_estudiantes_pero_si_a_los_de_otro_programa()
    {
        var resultado = DetectorTerceros.EnmascararTerceros(Coord01, "Andrés Martínez, Camilo Rojas, Andrés Martínez y Camilo Rojas", Directorio);

        Assert.Equal("Andrés Martínez, [TERCERO], Andrés Martínez y [TERCERO]", resultado);
    }

    [Theory]
    [MemberData(nameof(RepetidosEnIdentidadNoValidada))]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "SEG-04a")]
    [Trait("HALLAZGO", "H-01")]
    public void Sin_identidad_validada_toda_persona_repetida_se_enmascara_en_todas_sus_ocurrencias(string pregunta, string esperada)
    {
        var resultado = DetectorTerceros.EnmascararPersonas(pregunta, Directorio);

        Assert.Equal(esperada, resultado);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "SEG-04a")]
    [Trait("HALLAZGO", "H-01")]
    public void Ningun_fragmento_del_nombre_del_tercero_sobrevive_al_enmascarado_repetido()
    {
        var resultado = DetectorTerceros.EnmascararTerceros(Est001, "¿Qué nota tiene Andrés Martínez? Insisto: Andrés Martínez, la de Andrés Martínez.", Directorio);

        Assert.DoesNotContain("Andrés", resultado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Martínez", resultado, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, resultado.Split("[TERCERO]").Length - 1);
    }
}
