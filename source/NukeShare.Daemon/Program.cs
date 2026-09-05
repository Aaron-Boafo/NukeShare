using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://127.0.0.1:7654";

builder.WebHost.UseUrls(urls);
builder.Services.AddOpenApi();

if (args.Contains("--background"))
{
    builder.Logging.ClearProviders();
}

var app = builder.Build();

if (app.Environment.IsDevelopment() || args.Contains("--interactive"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "running",
    pid = Environment.ProcessId,
    timestamp = DateTime.UtcNow
}));

app.Run();

