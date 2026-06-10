using System.Text.Json.Serialization;

using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services;

public interface IAcquirerService
{
    Task<AuthorisationOutcome> AuthorizeAsync(AuthorisationRequest request, CancellationToken cancellationToken);
}

public class AcquirerService : IAcquirerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public AcquirerService(HttpClient httpClient, ILogger<AcquirerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthorisationOutcome> AuthorizeAsync(AuthorisationRequest request, CancellationToken cancellationToken)
    {
        const string requestPath = "payments";
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(requestPath, request);

            if (!response.IsSuccessStatusCode)
            {
                return response.StatusCode switch
                {
                    System.Net.HttpStatusCode.BadRequest => AuthorisationOutcome.Rejected,
                    _ => AuthorisationOutcome.Failed
                };
            }

            var authorisationResponse = await response.Content.ReadFromJsonAsync<AuthorisationResponse>(cancellationToken);

            if (authorisationResponse is null)
                return AuthorisationOutcome.Failed;

            return authorisationResponse.Authorized ? AuthorisationOutcome.Succeeded : AuthorisationOutcome.Declined;
        }
        catch(TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Timeout occured while trying to contact acquirer at {Path}", requestPath);
            return AuthorisationOutcome.Failed;
        } 
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error occured with request to {RequestPath} Error: {ErrorMessage}", requestPath, ex.Message);
            throw;
        }
    }
}

public class AuthorisationRequest
{
    [JsonPropertyName("card_number")]
    public required string CardNumber { get; set; }

    [JsonPropertyName("expiry_date")]
    public required string ExpiryDate { get; set; }
    public required string Currency { get; set; }
    public int Amount { get; set; }
    public required string Cvv { get; set; }
}

internal class AuthorisationResponse
{
    public required bool Authorized { get; init; }
}
