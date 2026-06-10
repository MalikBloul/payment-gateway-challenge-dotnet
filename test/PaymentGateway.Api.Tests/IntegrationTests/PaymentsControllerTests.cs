using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.IntegrationTests;

public class PaymentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Random _random = new();
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentsControllerTests(WebApplicationFactory<Program> webApplicationFactory)
    {
        _factory = webApplicationFactory;
    }

    [Fact]
    public async Task Get_ValidId_RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = Models.PaymentStatus.Authorized,
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        var paymentsRepository = _factory.Services.GetRequiredService<PaymentsRepository>();
        paymentsRepository.Add(payment);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse!.Id);
    }

    [Theory]
    [InlineData("0000000000000001", PaymentStatus.Authorized)]
    [InlineData("0000000000000002", PaymentStatus.Declined)]
    public async Task Create_ValidRequest_CreatesAPaymentWithCorrectStatus(string cardNumber, PaymentStatus status)
    {
        // Arrange
        var paymentRequest = new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryYear = _random.Next(DateTime.Now.AddYears(1).Year, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999)
        };

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", paymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.NotEqual(default, paymentResponse.Id);
        Assert.Equal(status, paymentResponse!.Status);
    }

    [Fact]
    public async Task Create_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var paymentRequest = new PostPaymentRequest
        {
            CardNumber = "",
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999)
        };

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments", paymentRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_NotSavedId_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}