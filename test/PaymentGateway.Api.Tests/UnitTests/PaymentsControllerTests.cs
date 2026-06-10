using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.UnitTests;

public class PaymentsControllerCreateTests
{
    [Fact]
    public async Task CreatePayment_AcquirerFails_Returns503()
    {
        var controller = CreateController(AuthorisationOutcome.Failed);

        var result = await controller.CreatePaymentAsync(ValidRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_AcquirerRejects_ReturnsBadRequest()
    {
        var controller = CreateController(AuthorisationOutcome.Rejected);

        var result = await controller.CreatePaymentAsync(ValidRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_AcquirerSucceeds_ReturnsAuthorizedPayment()
    {
        var controller = CreateController(AuthorisationOutcome.Succeeded);

        var result = await controller.CreatePaymentAsync(ValidRequest(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PostPaymentResponse>(okResult.Value);
        Assert.Equal(PaymentStatus.Authorized, response.Status);
        Assert.NotEqual(default, response.Id);
    }

    [Fact]
    public async Task CreatePayment_AcquirerDeclines_ReturnsDeclinedPayment()
    {
        var controller = CreateController(AuthorisationOutcome.Declined);

        var result = await controller.CreatePaymentAsync(ValidRequest(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PostPaymentResponse>(okResult.Value);
        Assert.Equal(PaymentStatus.Declined, response.Status);
    }

    [Fact]
    public async Task CreatePayment_AcquirerSucceeds_SavesPaymentToRepository()
    {
        var repository = new PaymentsRepository();
        var controller = CreateController(AuthorisationOutcome.Succeeded, repository);
        var request = ValidRequest();

        var result = await controller.CreatePaymentAsync(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PostPaymentResponse>(okResult.Value);
        Assert.NotNull(repository.Get(response.Id));
    }

    [Fact]
    public async Task CreatePayment_AcquirerSucceeds_ReturnsCorrectCardDetails()
    {
        var controller = CreateController(AuthorisationOutcome.Succeeded);
        var request = ValidRequest();

        var result = await controller.CreatePaymentAsync(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PostPaymentResponse>(okResult.Value);
        Assert.Equal("0001", response.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, response.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, response.ExpiryYear);
        Assert.Equal(request.Amount, response.Amount);
        Assert.Equal(request.Currency, response.Currency);
    }

    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumber = "0000000000000001",
        ExpiryYear = DateTime.Now.AddYears(1).Year,
        ExpiryMonth = 1,
        Amount = 100,
        Currency = "GBP",
        Cvv = 123
    };

    private static PaymentsController CreateController(
        AuthorisationOutcome outcome)
    {
        var repository = new PaymentsRepository();
        var acquirer = new FakeAcquirerService(outcome);
        var validator = new FakeValidator();
        return new PaymentsController(repository, acquirer, validator);
    }

    private static PaymentsController CreateController(
        AuthorisationOutcome outcome,
        PaymentsRepository repository)
    {
        var acquirer = new FakeAcquirerService(outcome);
        var validator = new FakeValidator();
        return new PaymentsController(repository, acquirer, validator);
    }
}

public class FakeAcquirerService(AuthorisationOutcome outcome) : IAcquirerService
{
    public Task<AuthorisationOutcome> AuthorizeAsync(AuthorisationRequest request, CancellationToken cancellationToken)
        => Task.FromResult(outcome);
}

public class FakeValidator : IValidator<PostPaymentRequest>
{
    public ValidationResult Validate(PostPaymentRequest instance) => new ValidationResult();

    public Task<ValidationResult> ValidateAsync(PostPaymentRequest instance, CancellationToken cancellationToken = default)
        => Task.FromResult(new ValidationResult());

    public ValidationResult Validate(IValidationContext context) => throw new NotImplementedException();
    public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public IValidatorDescriptor CreateDescriptor() => throw new NotImplementedException();
    public bool CanValidateInstancesOfType(Type type) => type == typeof(PostPaymentRequest);
}