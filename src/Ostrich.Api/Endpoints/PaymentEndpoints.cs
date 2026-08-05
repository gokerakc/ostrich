using Ostrich.Application.Services;

namespace Ostrich.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/payments", ListPayments);
        builder.MapGet("/payments/{id:guid}", GetPayment);
        builder.MapPost("/payments/{id:guid}/refund", RefundPayment);
        return builder;
    }

    private static async Task<IResult> ListPayments(
        IPaymentService service, int page = 1, int pageSize = 20)
    {
        var result = await service.GetPaymentsAsync(page, pageSize);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPayment(IPaymentService service, Guid id)
    {
        var payment = await service.GetPaymentAsync(id);
        return payment is null ? Results.NotFound() : Results.Ok(payment);
    }
    
    private static async Task<IResult> RefundPayment(IPaymentService service, Guid id)
    {
        var result = await service.RefundPaymentAsync(id);

        if(result.IsRefunded)
            return Results.Accepted();

        return result.Reason switch
        {
            "PaymentNotFound" => Results.NotFound("Payment not found"),
            "AlreadyRefunded" => Results.Conflict("Payment already refunded"),
            "PaymentCancelled" => Results.Ok("Payment cancelled"),
            _ => Results.InternalServerError()
        };
    }
}
