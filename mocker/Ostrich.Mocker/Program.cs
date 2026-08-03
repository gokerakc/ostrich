using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ostrich.Mocker;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<GeneratorOptions>(
    builder.Configuration.GetSection("Generator"));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetValue<string>("Redis:ConnectionString")!));

builder.Services.AddHostedService<PaymentGenerator>();

var host = builder.Build();
await host.RunAsync();
