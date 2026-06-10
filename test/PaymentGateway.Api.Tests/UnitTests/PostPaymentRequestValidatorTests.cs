using FluentValidation.TestHelper;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;


namespace PaymentGateway.Api.Tests.UnitTests;

public class PostPaymentRequestValidatorTests
{
    private readonly Random _random = new Random();

    [Fact]
    public void CardNumber_IsValid_PassesValidation()
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r => r.CardNumber = "4111111111111111");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.CardNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")] // too short
    [InlineData("41111111111111111111")] // too long (20 digits)
    [InlineData("4111-1111-1111-1111")] // invalid chars
    [InlineData("4111abcd1111abcd")] // mixed invalid
    public void CardNumber_IsInvalid_FailsValidation(string cardNumber)
    {

        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r => r.CardNumber = cardNumber);

        var response = validator.TestValidate(request);

        response.ShouldHaveValidationErrorFor(r => r.CardNumber);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(23)]
    [InlineData(-2)]
    public void ExpiryMonth_Invalid_FailsValidation(int month)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r => r.ExpiryMonth = month);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
    }

    [Theory]
    [InlineData(2025, 12)]
    [InlineData(2024, 1)]
    public void Card_IsExpired_FailsValidation(int year, int month)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r =>
        {
            r.ExpiryYear = year;
            r.ExpiryMonth = month;
        });

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("CAD")]
    public void Currency_NotSupported_FailsValidation(string currency)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r =>
            r.Currency = currency);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("EUR")]
    public void Currency_Supported_PassesValidation(string currency)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r =>
            r.Currency = currency);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(10000)]
    public void Cvv_Invalid_FailsValidation(int cvv)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r =>
            r.Cvv = cvv);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Cvv);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(9999)]
    public void Cvv_Valid_PassesValidation(int cvv)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r =>
            r.Cvv = cvv);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Cvv);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(9999)]
    public void Amount_Valid_PassesValidation(int amount)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r =>
            r.Amount = amount);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }


    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    public void Amount_InValid_FailsValidation(int amount)
    {
        var validator = CreateValidator();

        var request = CreatePostPaymentRequest(r =>
            r.Amount = amount);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    private PostPaymentRequest CreatePostPaymentRequest(Action<PostPaymentRequest> overrides = null)
    {
        var paymentRequest = new PostPaymentRequest
        {
            CardNumber = "0000000000000000",
            ExpiryYear = _random.Next(DateTime.Now.AddYears(1).Year, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999)
        };

        overrides?.Invoke(paymentRequest);
        return paymentRequest;
    }

    private static PostPaymentRequestValidator CreateValidator()
    {
        var options = Options.Create(new CurrencyOptions
        {
            SupportedCurrencies = new[] { "AUD", "EUR", "GBP" }
        });

        return new PostPaymentRequestValidator(CreateTimeProvider(), options);
    }

    private static FakeTimeProvider CreateTimeProvider()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTime(2026, 06, 09));
        return timeProvider;
    }
}
