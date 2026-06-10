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

    public AcquirerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthorisationOutcome> AuthorizeAsync(AuthorisationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            const string requestPath = "payments";
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
            return AuthorisationOutcome.Failed;
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
