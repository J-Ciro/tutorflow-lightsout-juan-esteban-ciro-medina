using System.Security.Cryptography;

namespace TutorFlow.Core.Asistente;

public sealed class ConfiguracionAsistente
{
    private const int BytesDelMarcadorAleatorio = 32;
    private const int TokensDeEntradaReservados = 8000;
    private const int TokensDeSalidaReservados = 2000;

    public ConfiguracionAsistente(
        PrecioModelo? precio,
        string? marcadorCanarioConfigurado,
        IReadOnlyCollection<string> codigosAsignatura,
        decimal? reservaPorPeticionUsd = null)
    {
        Precio = precio;
        MarcadorCanario = string.IsNullOrWhiteSpace(marcadorCanarioConfigurado)
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(BytesDelMarcadorAleatorio))
            : marcadorCanarioConfigurado;
        CodigosAsignatura = codigosAsignatura;
        ReservaPorPeticionUsd = reservaPorPeticionUsd ?? ReservaPorDefecto(precio);
    }

    public PrecioModelo? Precio { get; }

    public string MarcadorCanario { get; }

    public IReadOnlyCollection<string> CodigosAsignatura { get; }

    public decimal ReservaPorPeticionUsd { get; }

    private static decimal ReservaPorDefecto(PrecioModelo? precio) =>
        CalculadoraCosto.Calcular(new UsoTokens(TokensDeEntradaReservados, TokensDeSalidaReservados), precio);
}
