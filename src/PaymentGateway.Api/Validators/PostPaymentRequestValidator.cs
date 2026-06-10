using FluentValidation;

using Microsoft.Extensions.Options;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validators;

public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    private readonly TimeProvider _timeProvider;
    private readonly HashSet<string> _supportedCurrencies;
    public PostPaymentRequestValidator(TimeProvider timeProvider, IOptions<CurrencyOptions> options)
    {
        _timeProvider = timeProvider;
        _supportedCurrencies = options.Value.SupportedCurrencies.ToHashSet();

        RuleFor(r => r.CardNumber)
            .NotEmpty()
                .WithMessage("Must supply card number")
            .Must(BeAllDigits)
                .WithMessage("Card numbers must only contain digits (0-9)")
            .MinimumLength(14)
            .MaximumLength(19)
                .WithMessage("Card length must be between 14-19 digits");

        RuleFor(x => x.ExpiryMonth)
           .InclusiveBetween(1, 12)
           .WithMessage("Expiry month must be between 1 and 12");

        RuleFor(x => x.ExpiryYear)
            .GreaterThan(0)
            .WithMessage("Expiry year cannot be negative");

        RuleFor(x => x)
            .Must(NotBeExpired)
            .WithMessage("Card is expired");

        RuleFor(x => x.Currency)
            .Length(3)
                .WithMessage("Invalid ISO currency code (must be 3 characters)")
            .Must(BeASupportedCurrency)
                .WithMessage($"Currency not supported. Supported ISO currencies: {string.Join(", ", _supportedCurrencies)}");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Cvv)
            .InclusiveBetween(100, 9999)
                .WithMessage("Cvv must be between 3-4 digits");
    }

    private static bool BeAllDigits(string cardNumber)
        => cardNumber.All(c => char.IsDigit(c));

    private bool NotBeExpired(PostPaymentRequest request)
    {
        var now = _timeProvider.GetUtcNow();

        var currentYear = now.Year;
        var currentMonth = now.Month;

        if (request.ExpiryYear > currentYear)
            return true;

        if (request.ExpiryYear == currentYear && request.ExpiryMonth >= currentMonth)
            return true;

        return false;
    }

    private bool BeASupportedCurrency(string currency)
        => _supportedCurrencies.Contains(currency);
}
