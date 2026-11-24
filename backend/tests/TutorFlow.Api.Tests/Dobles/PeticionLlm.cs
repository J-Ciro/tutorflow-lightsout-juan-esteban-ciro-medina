namespace TutorFlow.Api.Tests.Dobles;

public sealed record PeticionLlm(Uri Uri, IReadOnlyDictionary<string, string> Encabezados, string Cuerpo);
