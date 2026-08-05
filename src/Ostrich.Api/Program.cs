using Microsoft.EntityFrameworkCore;
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
