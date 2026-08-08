using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ostrich.Application.Services;
using Ostrich.Core.Models;
using Ostrich.Core.Services;
using Ostrich.Infrastructure.Data;

namespace Ostrich.IntegrationTests;

public class PaymentApiTests : IClassFixture<AppFixture>
{
    private static readonly Guid TestAccountId = Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001");

    private readonly AppFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PaymentApiTests(AppFixture fixture)
    {
        _fixture = fixture;
        _dbFactory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    }

    private async Task SeedAsync(Payment payment)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task List_ReturnsPagedProcessedPayments()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(),
            Amount = 100m, Currency = "USD",
            Merchant = "TestMerchant", AccountId = TestAccountId,
            Status = "Processed", ProcessedAt = DateTime.UtcNow
        };
        await SeedAsync(payment);

        var response = await _fixture.HttpClient.GetAsync("/payments?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PaymentDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Contains(result.Items, p => p.Id == payment.Id);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsPayment()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(),
            Amount = 50m, Currency = "EUR",
            Merchant = "GetMerchant", AccountId = TestAccountId,
            Status = "Processed", ProcessedAt = DateTime.UtcNow
        };
        await SeedAsync(payment);

        var response = await _fixture.HttpClient.GetAsync($"/payments/{payment.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(dto);
        Assert.Equal(payment.Id, dto.Id);
        Assert.Equal(payment.Amount, dto.Amount);
        Assert.Equal(payment.Currency, dto.Currency);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _fixture.HttpClient.GetAsync(
            $"/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Refund_Processed_ReturnsAccepted()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(),
            Amount = 200m, Currency = "TRY",
            Merchant = "RefundMerchant", AccountId = TestAccountId,
            Status = "Processed", ProcessedAt = DateTime.UtcNow
        };
        await SeedAsync(payment);

        var response = await _fixture.HttpClient.PostAsync(
            $"/payments/{payment.Id}/refund", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var context = await _dbFactory.CreateDbContextAsync();
        var updated = await context.Payments.FindAsync(payment.Id);
        Assert.Equal("Refunded", updated!.Status);
    }

    [Fact]
    public async Task Refund_AlreadyRefunded_ReturnsConflict()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(),
            Amount = 300m, Currency = "USD",
            Merchant = "ConflictMerchant", AccountId = TestAccountId,
            Status = "Refunded", RefundedAt = DateTime.UtcNow
        };
        await SeedAsync(payment);

        var response = await _fixture.HttpClient.PostAsync(
            $"/payments/{payment.Id}/refund", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Refund_Pending_CancelsPayment()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(), ExternalId = Guid.NewGuid(),
            Amount = 400m, Currency = "GBP",
            Merchant = "CancelMerchant", AccountId = TestAccountId,
            Status = "Pending"
        };
        await SeedAsync(payment);

        var response = await _fixture.HttpClient.PostAsync(
            $"/payments/{payment.Id}/refund", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var context = await _dbFactory.CreateDbContextAsync();
        var updated = await context.Payments.FindAsync(payment.Id);
        Assert.Equal("Cancelled", updated!.Status);
    }

    [Fact]
    public async Task Refund_NotFound_Returns404()
    {
        var response = await _fixture.HttpClient.PostAsync(
            $"/payments/{Guid.NewGuid()}/refund", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
