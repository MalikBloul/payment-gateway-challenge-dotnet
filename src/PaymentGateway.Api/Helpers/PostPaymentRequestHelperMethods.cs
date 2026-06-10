using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Helpers;

public static class PostPaymentRequestHelperMethods
{
    public static AuthorisationRequest ToAuthorisationRequest(this PostPaymentRequest request)
        => new AuthorisationRequest
        {
            CardNumber = request.CardNumber,
            ExpiryDate = $"{request.ExpiryMonth:02d}/{request.ExpiryYear}",
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv.ToString()
        };
}
