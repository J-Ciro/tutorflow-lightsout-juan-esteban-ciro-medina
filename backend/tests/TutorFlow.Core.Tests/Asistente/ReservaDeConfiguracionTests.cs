using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ReservaDeConfiguracionTests
{
    private const decimal ReservaSinPrecio = 0.01m;
    private const decimal ReservaEstimada = 0.0018m;
    private static readonly PrecioModelo Haiku = new(0.10m, 0.50m);

    public static TheoryData<decimal> ReservasSinPrecioNoPositivas => new() { 0m, -0.5m, -0.0001m };

    public static TheoryData<decimal> ReservasPorPeticionNoPositivas => new() { 0m, -1m, -0.0001m };

    private static ConfiguracionAsistente Configurar(PrecioModelo? precio, decimal? reservaPorPeticion = null, decimal reservaSinPrecio = ReservaSinPrecio) =>
        new(precio: precio, marcadorCanarioConfigurado: null, reservaPorPeticionUsd: reservaPorPeticion, catalogo: CatalogoDePrueba.Asignaturas, reservaSinPrecioUsd: reservaSinPrecio);

    [Theory]
    [MemberData(nameof(ReservasSinPrecioNoPositivas))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "SEG-13")]
    public void ESC_188d_Una_reserva_sin_precio_no_positiva_que_llega_al_nucleo_usa_el_defecto_de_001(decimal reserva)
    {
        var configuracion = Configurar(null, reservaSinPrecio: reserva);

        Assert.Equal(ReservaSinPrecio, configuracion.ReservaPorPeticionUsd);
    }

    [Theory]
    [MemberData(nameof(ReservasPorPeticionNoPositivas))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "SEG-13")]
    public void ESC_188d_Una_reserva_por_peticion_no_positiva_que_llega_al_nucleo_usa_el_estimado_de_tokens(decimal reserva)
    {
        var configuracion = Configurar(Haiku, reserva);

        Assert.Equal(ReservaEstimada, configuracion.ReservaPorPeticionUsd);
    }

    [Theory]
    [MemberData(nameof(ReservasPorPeticionNoPositivas))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "SEG-13")]
    public void ESC_188d_Una_reserva_por_peticion_no_positiva_sin_precio_usa_el_defecto_sin_precio(decimal reserva)
    {
        var configuracion = Configurar(null, reserva);

        Assert.Equal(ReservaSinPrecio, configuracion.ReservaPorPeticionUsd);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "SEG-13")]
    public void ESC_188d_Una_reserva_positiva_valida_se_respeta_y_sin_reserva_se_estima()
    {
        Assert.Equal(0.003m, Configurar(Haiku, 0.003m).ReservaPorPeticionUsd);
        Assert.Equal(ReservaEstimada, Configurar(Haiku).ReservaPorPeticionUsd);
        Assert.Equal(0.02m, Configurar(null, reservaSinPrecio: 0.02m).ReservaPorPeticionUsd);
    }
}
