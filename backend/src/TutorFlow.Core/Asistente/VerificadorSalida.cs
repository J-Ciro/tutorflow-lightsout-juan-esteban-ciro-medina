using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed record SalidaVerificada(string Respuesta, IReadOnlyList<Cita> Citas, ResultadoAuditoria Resultado);

public sealed partial class VerificadorSalida
{
    private const string DocumentoDerogado = "reglamento_academico_2023.md";

    private readonly HashSet<string> _documentosDerogados = new HashSet<string> { DocumentoDerogado };
    private readonly Regex _numero = Numero();
    private readonly Regex _notaOPorcentaje = NotaOPorcentaje();
    private readonly Func<string, string> _normalizar = NormalizadorTexto.Normalizar;

    public SalidaVerificada Verificar(
        Usuario usuario,
        string respuesta,
        IReadOnlyList<Cita> citas,
        IReadOnlyCollection<string> idsPersonasAutorizadas,
        IReadOnlyCollection<string> textosFuente,
        IBaseConocimiento baseConocimiento,
        DirectorioAcademico directorio,
        MaterialConfidencial? confidencial = null) =>
        RechazoPorPersona(usuario, respuesta, idsPersonasAutorizadas, directorio)
        ?? RechazoPorConfiguracion(respuesta, textosFuente, confidencial)
        ?? RechazoPorValorMonetario(respuesta, textosFuente)
        ?? RechazoPorNumero(respuesta, textosFuente)
        ?? new SalidaVerificada(respuesta, DescartarCitasInexistentes(citas, baseConocimiento), ResultadoAuditoria.Respondida);

    private SalidaVerificada? RechazoPorPersona(
        Usuario usuario,
        string respuesta,
        IReadOnlyCollection<string> idsPersonasAutorizadas,
        DirectorioAcademico directorio) =>
        ContienePersonaNoAutorizada(respuesta, idsPersonasAutorizadas, directorio)
            ? new SalidaVerificada(Textos.Negativa(usuario.Rol), [Textos.CitaNegativa()], ResultadoAuditoria.FugaBloqueada)
            : null;

    private static SalidaVerificada? RechazoPorConfiguracion(
        string respuesta,
        IReadOnlyCollection<string> textosFuente,
        MaterialConfidencial? confidencial) =>
        confidencial is not null && DetectorFugaConfiguracion.HayFuga(respuesta, confidencial, textosFuente)
            ? new SalidaVerificada(Textos.SinInformacion, [], ResultadoAuditoria.FugaBloqueada)
            : null;

    private static SalidaVerificada? RechazoPorValorMonetario(string respuesta, IReadOnlyCollection<string> textosFuente) =>
        DetectorValorMonetario.HayValorNoRespaldado(respuesta, textosFuente)
            ? new SalidaVerificada(Textos.SinInformacion, [], ResultadoAuditoria.SinInformacion)
            : null;

    private SalidaVerificada? RechazoPorNumero(string respuesta, IReadOnlyCollection<string> textosFuente) =>
        TodosLosNumerosSonVerificables(respuesta, textosFuente)
            ? null
            : new SalidaVerificada(Textos.NoVerificable, [], ResultadoAuditoria.NumeroNoVerificado);

    public IReadOnlyList<Cita> DescartarCitasInexistentes(IReadOnlyList<Cita> citas, IBaseConocimiento baseConocimiento) =>
        citas.Where(cita => !_documentosDerogados.Contains(cita.Documento) && baseConocimiento.ExisteCita(cita)).ToList();

    public bool ContienePersonaNoAutorizada(string respuesta, IReadOnlyCollection<string> idsPersonasAutorizadas, DirectorioAcademico directorio)
    {
        var normalizada = _normalizar(respuesta);
        return DetectorTerceros.PersonasMencionadas(normalizada, directorio).Any(id => !idsPersonasAutorizadas.Contains(id))
            || DetectorTerceros.MencionaIdFueraDe(normalizada, idsPersonasAutorizadas, directorio);
    }

    public bool TodosLosNumerosSonVerificables(string respuesta, IReadOnlyCollection<string> textosFuente)
    {
        var numerosDeFuentes = textosFuente
            .SelectMany(texto => _numero.Matches(texto).Select(coincidencia => coincidencia.Value))
            .ToHashSet();
        return NumerosAVerificar(respuesta).All(numerosDeFuentes.Contains);
    }

    private IEnumerable<string> NumerosAVerificar(string respuesta) =>
        _notaOPorcentaje.Matches(ReferenciasASeccion.Quitar(TokenizadorTexto.Plegado(respuesta)))
            .Select(coincidencia => coincidencia.Groups["numero"].Value);

    [GeneratedRegex(@"(?<![0-9.])[0-9]+(\.[0-9]+)?", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Numero();

    [GeneratedRegex(@"(?<![0-9.])(?<numero>[0-9]\.[0-9]{1,2})(?![0-9])|(?<![0-9.])(?<numero>[0-9]+(\.[0-9]+)?)(?=\s*%)", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex NotaOPorcentaje();
}
