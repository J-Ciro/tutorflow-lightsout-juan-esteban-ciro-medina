using Microsoft.Extensions.Options;
using TutorFlow.Api;
using TutorFlow.Core.Asistente;
using TutorFlow.Infrastructure.ApiAcademica;
using TutorFlow.Infrastructure.Llm;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OpcionesApiAcademica>()
    .BindConfiguration(OpcionesApiAcademica.Seccion)
    .Validate(o => Uri.IsWellFormedUriString(o.UrlBase, UriKind.Absolute), "ApiAcademica:UrlBase debe ser una URL absoluta")
    .ValidateOnStart();
builder.Services.AddOptions<OpcionesLlm>()
    .BindConfiguration(OpcionesLlm.Seccion)
    .Validate(o => o.Proveedores.Count > 0, "Llm:Proveedores no puede estar vacio")
    .ValidateOnStart();
builder.Services.AddOptions<LimitesAsistente>()
    .BindConfiguration("Limites")
    .Validate(l => l.MaximoCaracteresPregunta > 0, "Limites:MaximoCaracteresPregunta debe ser positivo")
    .ValidateOnStart();

var app = builder.Build();

app.MapGet("/api/salud", () => Results.Ok(new EstadoSalud("ok")));

app.MapPost("/api/asistente/preguntar", (PreguntaSolicitud solicitud) => Results.Problem(statusCode: StatusCodes.Status501NotImplemented));

app.Run();

public partial class Program;
