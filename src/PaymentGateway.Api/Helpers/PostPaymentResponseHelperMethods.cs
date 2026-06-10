using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Helpers;

public static class PostPaymentResponseHelperMethods
{
    public static GetPaymentResponse ToGetPaymentResponse(this PostPaymentResponse request)
        => new GetPaymentResponse
        {
            Id = request.Id,
            Status = request.Status,
            CardNumberLastFour = request.CardNumberLastFour,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount
        };
}