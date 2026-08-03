using Ostrich.Core.Services;

namespace Ostrich.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/payments", ListPayments);
        builder.MapGet("/payments/{id:guid}", GetPayment);
        return builder;
    }

    private static async Task<IResult> ListPayments(
        IPaymentRepository repo, int page = 1, int pageSize = 20)
    {
        var result = await repo.ListProcessedAsync(page, pageSize);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPayment(IPaymentRepository repo, Guid id)
    {
        var payment = await repo.GetByIdAsync(id);
        return payment is null ? Results.NotFound() : Results.Ok(payment);
    }
}
