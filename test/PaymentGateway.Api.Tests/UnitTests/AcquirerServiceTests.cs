using System.Net;
using System.Net.Http.Json;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.UnitTests;

public class AcquirerServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_AuthorizedResponse_ReturnsSucceeded()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { authorized = true })
        };
        var service = CreateService(response);

        // Act
        var result = await service.AuthorizeAsync(ValidRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(AuthorisationOutcome.Succeeded, result);
    }

    [Fact]
    public async Task AuthorizeAsync_DeclinedResponse_ReturnsDeclined()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { authorized = false })
        };
        var service = CreateService(response);

        // Act
        var result = await service.AuthorizeAsync(ValidRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(AuthorisationOutcome.Declined, result);
    }

    [Fact]
    public async Task AuthorizeAsync_BadRequestResponse_ReturnsRejected()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var service = CreateService(response);

        // Act
        var result = await service.AuthorizeAsync(ValidRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(AuthorisationOutcome.Rejected, result);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task AuthorizeAsync_NonBadRequestErrorResponse_ReturnsFailed(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        var service = CreateService(response);

        // Act
        var result = await service.AuthorizeAsync(ValidRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(AuthorisationOutcome.Failed, result);
    }

    [Fact]
    public async Task AuthorizeAsync_NullResponseBody_ReturnsFailed()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
            {
                Headers = { ContentType = new("application/json") }
            }
        };
        var service = CreateService(response);

        // Act
        var result = await service.AuthorizeAsync(ValidRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(AuthorisationOutcome.Failed, result);
    }

    [Fact]
    public async Task AuthorizeAsync_Timeout_ReturnsFailed()
    {
        // Arrange
        var handler = new TimeoutHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://acquirer.test/") };
        var service = new AcquirerService(httpClient);

        // Act
        var result = await service.AuthorizeAsync(ValidRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(AuthorisationOutcome.Failed, result);
    }

    private static AcquirerService CreateService(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://acquirer.test/") };
        return new AcquirerService(httpClient);
    }

    private static AuthorisationRequest ValidRequest() => new()
    {
        CardNumber = "0000000000000001",
        ExpiryDate = "12/2030",
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };
}

public class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}

public class TimeoutHttpMessageHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        throw new TaskCanceledException("Timeout", new TimeoutException());
    }
}