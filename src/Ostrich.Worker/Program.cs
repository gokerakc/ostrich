using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ostrich.Application.Services;
using Ostrich.Core.Services;
using Ostrich.Infrastructure.Data;
using Ostrich.Infrastructure.Services;
using Ostrich.Worker;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetValue<string>("Redis:ConnectionString")!));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<IStreamConsumer, RedisStreamConsumer>();
builder.Services.AddSingleton<IPaymentService, PaymentService>();
builder.Services.AddHostedService<PaymentProcessor>();

var host = builder.Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
}

await host.RunAsync();
