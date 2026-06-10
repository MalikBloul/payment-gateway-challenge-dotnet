namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    public required string CardNumber { get; set; }
    public required int ExpiryMonth { get; set; }
    public required int ExpiryYear { get; set; }
    public required string Currency { get; set; }
    public required int Amount { get; set; }
    public required int Cvv { get; set; }
    internal string GetLast4()
        => CardNumber.Length >= 4 
            ? CardNumber[^4..]
            : CardNumber;
}