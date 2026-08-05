using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Ostrich.Api.Endpoints;
using Ostrich.Application.Services;
using Ostrich.Core.Services;
using Ostrich.Infrastructure.Data;
using Ostrich.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<IPaymentService, PaymentService>();
builder.Services.AddSingleton<PaymentMetrics>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Ostrich.Api"))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Ostrich.Payments")
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://localhost:18889");
        }))
    .WithLogging(l => l
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://localhost:18889");
        }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.MapPaymentEndpoints();

await app.RunAsync();
