using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IAcquirerService _acquirerService;
    private readonly IValidator<PostPaymentRequest> _postPaymentRequestValidator;
    private readonly ILogger _logger;

    public PaymentsController(PaymentsRepository paymentsRepository,
        IAcquirerService acquirerService,
        IValidator<PostPaymentRequest> postPaymentRequestValidator,
        ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _acquirerService = acquirerService;
        _postPaymentRequestValidator = postPaymentRequestValidator;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GetPaymentResponse> GetPayment(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment is null)
            return NotFound();

        return Ok(payment.ToGetPaymentResponse());
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> CreatePaymentAsync([FromBody] PostPaymentRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _postPaymentRequestValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var authorisationRequest = request.ToAuthorisationRequest();
        var authorisationOutcome = await _acquirerService.AuthorizeAsync(authorisationRequest, cancellationToken);

        if (authorisationOutcome == Models.AuthorisationOutcome.Failed)
        {
            _logger.LogWarning("Payment authorisation failed for request with currency {Currency} and amount {Amount}", request.Currency, request.Amount);
            return StatusCode(503, "Payment service temporarily unavailable. Please retry.");
        }

        if (authorisationOutcome == Models.AuthorisationOutcome.Rejected)
        {
            _logger.LogWarning("Payment authorisation was rejected for request with currency {Currency} and amount {Amount}", request.Currency, request.Amount);
            return BadRequest("Card details are invalid, please check the card details and retry request");
        }

        var response = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = authorisationOutcome == Models.AuthorisationOutcome.Succeeded 
                ? Models.PaymentStatus.Authorized 
                : Models.PaymentStatus.Declined,
            CardNumberLastFour = request.GetLast4(),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Amount = request.Amount,
            Currency = request.Currency
        };
        
        _paymentsRepository.Add(response);
        _logger.LogInformation("Payment with Id: {PaymentId} was saved with Status: { PaymentStatus }", response.Id, response.Status);
        return Ok(response);
    }
}