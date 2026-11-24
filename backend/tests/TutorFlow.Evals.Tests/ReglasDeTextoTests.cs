namespace TutorFlow.Evals.Tests;

public sealed class ReglasDeTextoTests
{
    public static TheoryData<string> RespuestasConLatex => new()
    {
        { @"La definitiva es \[ 0.3 \times c_1 + 0.3 \times c_2 + 0.4 \times c_3 \]." },
        { @"La definitiva es \( 0.3 c_1 \) más lo demás." },
        { @"Se calcula con \frac{30}{100} del corte." },
        { @"Pondera 0.3 \times corte 1." },
        { "La definitiva es $0.3 c_1 + 0.4 c_3$." }
    };

    public static TheoryData<string> RespuestasEnTextoPlano => new()
    {
        { "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3." },
        { "Cada corte pesa 30 %, 30 % y 40 %." },
        { "Tu promedio es 4.39." }
    };

    public static TheoryData<string> OracionesConHueco => new()
    {
        { "Requisitos: promedio 4.3 o más. Estos requisitos están, aunque no se mencionan." },
        { "Requisitos: promedio 4.3 o más. Estos requisitos están." },
        { "Requisitos: promedio 4.3 o más. Esta información está ." },
        { "Requisitos: promedio 4.3 o más. Figuran." },
        { "Requisitos: promedio 4.3 o más. Aparecen, aunque no se mencionan." }
    };

    public static TheoryData<string> OracionesSinHueco => new()
    {
        { "Bienestar atiende en el edificio B, piso 2." },
        { "El horario está publicado en la oficina." },
        { "Los requisitos figuran en la lista de la beca." }
    };

    [Theory]
    [MemberData(nameof(RespuestasConLatex))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public void ESC_197e_Toda_marca_de_latex_es_un_fallo(string respuesta)
    {
        Assert.NotEmpty(ReglasDeTexto.Latex(respuesta));
    }

    [Theory]
    [MemberData(nameof(RespuestasEnTextoPlano))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public void ESC_197e_El_texto_plano_con_porcentajes_no_es_latex(string respuesta)
    {
        Assert.Empty(ReglasDeTexto.Latex(respuesta));
    }

    [Theory]
    [MemberData(nameof(OracionesConHueco))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public void ESC_197e_Un_verbo_de_ubicacion_sin_complemento_es_un_fallo(string respuesta)
    {
        Assert.NotEmpty(ReglasDeTexto.HuecosTrasUbicacion(respuesta));
    }

    [Theory]
    [MemberData(nameof(OracionesSinHueco))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public void ESC_197e_Un_verbo_de_ubicacion_con_complemento_no_es_un_fallo(string respuesta)
    {
        Assert.Empty(ReglasDeTexto.HuecosTrasUbicacion(respuesta));
    }

    [Theory]
    [InlineData("Estos requisitos se respaldan en.")]
    [InlineData("La información se basa en.")]
    [InlineData("Lo dice el reglamento según.")]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-50")]
    public void ESC_197e_Una_oracion_terminada_en_preposicion_o_clausula_sigue_siendo_un_fallo(string respuesta)
    {
        Assert.NotEmpty(ReglasDeTexto.OracionesTruncadas(respuesta));
    }
}
