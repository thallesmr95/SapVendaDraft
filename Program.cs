using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Força uso de TLS 1.2

// Registra um HttpClient nomeado que ignora SSL (para testes)
builder.Services.AddHttpClient("NoSsl")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
