using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class CalculadoraCostoTests
{
    [Fact]
    [Trait("CA", "CA-11")]
    public void Mil_tokens_de_entrada_y_doscientos_de_salida_a_1_y_5_cuestan_0_002()
    {
        var costo = CalculadoraCosto.Calcular(new UsoTokens(1000, 200), new PrecioModelo(1.0m, 5.0m));

        Assert.Equal(0.002m, costo);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    public void Tokens_totales_suman_entrada_y_salida()
    {
        Assert.Equal(1200, new UsoTokens(1000, 200).Total);
    }

    public static TheoryData<int, int, decimal, decimal, decimal> CasosDeCosto => new()
    {
        { 5, 0, 0.1m, 0.5m, 0.000001m },
        { 0, 3, 0.1m, 0.5m, 0.000002m },
        { 1000000, 1000000, 0.1m, 0.5m, 0.6m },
        { 0, 0, 1.0m, 5.0m, 0.0m },
        { 1500, 0, 0.0m, 0.0m, 0.0m }
    };

    [Theory]
    [MemberData(nameof(CasosDeCosto))]
    [Trait("CA", "CA-11")]
    public void Costo_se_redondea_a_seis_decimales_con_medio_hacia_arriba(int entrada, int salida, decimal precioEntrada, decimal precioSalida, decimal esperado)
    {
        var costo = CalculadoraCosto.Calcular(new UsoTokens(entrada, salida), new PrecioModelo(precioEntrada, precioSalida));

        Assert.Equal(esperado, costo);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    public void Modelo_sin_precio_reporta_costo_cero()
    {
        Assert.Equal(0m, CalculadoraCosto.Calcular(new UsoTokens(1000, 200), null));
    }
}

public sealed class TextosTests
{
    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "D-07")]
    public void Negativa_del_estudiante_es_el_texto_constante_de_la_spec()
    {
        Assert.Equal("No puedo compartir esa información: solo puedes consultar tus propios datos académicos.", Textos.Negativa(Rol.Estudiante));
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "D-07")]
    public void Negativa_del_coordinador_es_el_texto_constante_de_la_spec()
    {
        Assert.Equal("No puedo compartir esa información: solo puedes consultar los datos de los estudiantes de tu programa.", Textos.Negativa(Rol.Coordinador));
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "D-07")]
    public void Cita_de_la_negativa_es_el_articulo_22_del_reglamento_2025()
    {
        var cita = Textos.CitaNegativa();

        Assert.Equal("reglamento_academico_2025.md", cita.Documento);
        Assert.Equal("Artículo 22", cita.Seccion);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    public void Textos_fijos_coinciden_con_la_spec()
    {
        Assert.Equal("No encuentro esa información en los documentos disponibles.", Textos.SinInformacion);
        Assert.Equal("No pude verificar los datos de la respuesta. Intenta de nuevo o reformula la pregunta.", Textos.NoVerificable);
    }
}
