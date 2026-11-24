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
    public const decimal ReservaSinPrecioPorDefectoUsd = 0.01m;

    public ConfiguracionAsistente(
        PrecioModelo? precio,
        string? marcadorCanarioConfigurado,
        IReadOnlyCollection<string>? codigosAsignatura = null,
        decimal? reservaPorPeticionUsd = null,
        int relevanciaMinima = RelevanciaMinimaPorDefecto, int apoyoMinimoRespaldo = ApoyoMinimoPorDefecto,
        IReadOnlyList<AsignaturaCatalogo>? catalogo = null,
        decimal reservaSinPrecioUsd = ReservaSinPrecioPorDefectoUsd,
        Action<string>? advertir = null, AjustesDelModelo? ajustes = null)
    {
        Catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
        Advertir = AdvertenciaOSilencio(advertir);
        Precio = precio;
        MarcadorCanario = MarcadorOAleatorio(marcadorCanarioConfigurado);
        CodigosAsignatura = CodigosOCatalogo(codigosAsignatura, Catalogo);
        Ajustes = ajustes ?? new AjustesDelModelo();
        ReservaPorPeticionUsd = reservaPorPeticionUsd is > 0m ? reservaPorPeticionUsd.Value : ReservaPorDefecto(precio, reservaSinPrecioUsd);
        RelevanciaMinima = Math.Max(MinimoPermitido, relevanciaMinima);
        ApoyoMinimoRespaldo = Math.Max(MinimoPermitido, apoyoMinimoRespaldo);
    }

    private readonly HashSet<string> _advertidas = [];

    public Action<string> Advertir { get; }

    public void AdvertirUnaVez(string mensaje)
    {
        lock (_advertidas)
        {
            if (_advertidas.Add(mensaje))
            {
                Advertir(mensaje);
            }
        }
    }

    public AjustesDelModelo Ajustes { get; }

    public PrecioModelo? Precio { get; }

    public string MarcadorCanario { get; }

    public IReadOnlyCollection<string> CodigosAsignatura { get; }

    public IReadOnlyList<AsignaturaCatalogo> Catalogo { get; }

    public decimal ReservaPorPeticionUsd { get; }

    public int RelevanciaMinima { get; }

    public int ApoyoMinimoRespaldo { get; }

    private static string MarcadorOAleatorio(string? configurado) =>
        string.IsNullOrWhiteSpace(configurado) ? Convert.ToHexString(RandomNumberGenerator.GetBytes(BytesDelMarcadorAleatorio)) : configurado;

    private static Action<string> AdvertenciaOSilencio(Action<string>? advertir) => advertir ?? (_ => { });

    private static IReadOnlyCollection<string> CodigosOCatalogo(IReadOnlyCollection<string>? codigos, IReadOnlyList<AsignaturaCatalogo> catalogo) =>
        codigos ?? [.. catalogo.Select(asignatura => asignatura.Codigo)];

    public decimal GastoDe(UsoTokens uso, int llamadas = 1) =>
        Precio is null ? ReservaPorPeticionUsd * llamadas : CalculadoraCosto.Calcular(uso, Precio);

    private static decimal ReservaPorDefecto(PrecioModelo? precio, decimal reservaSinPrecioUsd) =>
        precio is null
            ? (reservaSinPrecioUsd > 0m ? reservaSinPrecioUsd : ReservaSinPrecioPorDefectoUsd)
            : CalculadoraCosto.Calcular(new UsoTokens(TokensDeEntradaReservados, TokensDeSalidaReservados), precio);
}
