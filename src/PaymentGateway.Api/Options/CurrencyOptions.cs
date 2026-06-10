namespace PaymentGateway.Api.Models;

public class CurrencyOptions
{
    public IEnumerable<string> SupportedCurrencies { get; init; } = [];
}