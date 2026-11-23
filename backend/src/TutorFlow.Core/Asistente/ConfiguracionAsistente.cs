using System.Security.Cryptography;

namespace TutorFlow.Core.Asistente;

public sealed class ConfiguracionAsistente
{
    private const int BytesDelMarcadorAleatorio = 32;
    private const int TokensDeEntradaReservados = 8000;
    private const int TokensDeSalidaReservados = 2000;
    private const int MinimoPermitido = 1;

    public const int RelevanciaMinimaPorDefecto = 2;
    public const int ApoyoMinimoPorDefecto = 2;

    public ConfiguracionAsistente(
        PrecioModelo? precio,
        string? marcadorCanarioConfigurado,
        IReadOnlyCollection<string> codigosAsignatura,
        decimal? reservaPorPeticionUsd = null,
        int relevanciaMinima = RelevanciaMinimaPorDefecto,
        int apoyoMinimoRespaldo = ApoyoMinimoPorDefecto)
    {
        Precio = precio;
        MarcadorCanario = string.IsNullOrWhiteSpace(marcadorCanarioConfigurado)
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(BytesDelMarcadorAleatorio))
            : marcadorCanarioConfigurado;
        CodigosAsignatura = codigosAsignatura;
        ReservaPorPeticionUsd = reservaPorPeticionUsd ?? ReservaPorDefecto(precio);
        RelevanciaMinima = Math.Max(MinimoPermitido, relevanciaMinima);
        ApoyoMinimoRespaldo = Math.Max(MinimoPermitido, apoyoMinimoRespaldo);
    }

    public PrecioModelo? Precio { get; }

    public string MarcadorCanario { get; }

    public IReadOnlyCollection<string> CodigosAsignatura { get; }

    public decimal ReservaPorPeticionUsd { get; }

    public int RelevanciaMinima { get; }

    public int ApoyoMinimoRespaldo { get; }

    private static decimal ReservaPorDefecto(PrecioModelo? precio) =>
        CalculadoraCosto.Calcular(new UsoTokens(TokensDeEntradaReservados, TokensDeSalidaReservados), precio);
}
