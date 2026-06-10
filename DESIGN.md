# Overview
This is a payment gateway API built in ASP.NET Core that allows merchants to process card payments and retrieve previous payment details.

# Big Decisions
## Validation
- For complex validation of the `PostPaymentRequest` inline with the spec sheet, I introduced `FluentValidation` as an easier way to handle all the different validation rules and provide descriptive error responses.
- This also helps with testing as it gives you a bit more of a granular look into which properties are failing the validation than just `true` or `false`
- For simplicity and given the contraints the validator sits in the controller.

## Acquirer Service
- Introduced an acquirer service which handles contacting the acquirer service. This service returns an `AuthorisationOutcome` as based on the specs the `authorisation_code` is not used anywhere and error state is a little more complicated than `true` or `false`
- Spec was interpreted as payments are only ever created when there is a 200 response from the acquirer otherwise something went wrong
- Added simple retry for 503 errors from acquirer (even though will fail again as triggered by the card number, just to simulate a chance to retry a payment in real life rather than just returning straight to the caller)

## Removed the Rejected Enum status
- Based on my interpretation of the spec sheet, no payment is to be created when a response is rejected, so that enum member is not really used. In an ideal world the public facing PaymentStatus enum would be different to the internal one. 
- This had the added benefit of ensuring that OpenApi spec would have the correct two statuses as specified in the spec.

## Supported Currencies
- Opted against an enum and instead added to `appsettings.json` and brought in via IOptions, means you can just add all your supported currencies there without changing any code.
- Gives the flexibility to change the supported currencies while the app is running via `IOptionsMonitor`
- Currencies supported are probably not frequently changing but having an enum means that adding currencies requires code changes e.g. Different regions/environments may also support different currencies and it is just easier to bring them in via options.

## Testing
- Kept most of the original structure but refactored to use the WebApplicationFactory properly rather than newing it up every time in integrations tests. Tests just use the test acquirer supplied directly. This could be mocked but I thought would be better to test the whole flow there.
- Most Happy paths were integration tested. With a single BadRequest path checked also to make sure the validation was wired up.
- Chose to keep unit tests pretty basic, testing most of the scenarios could think of in terms of validation based on the spec. Opted to put this in unit tests rather than integration as running all these scenarios in an integration test could stack up quickly.

## Observability
- Opted for simple logging using `ILogger`, for simplicity.
- Made sure no PII in logs

# Future Directions
## Testing
- Validate the entire response body in integration tests to make sure fields are persisted and returned correctly.
- Add unit tests for all helper/extension methods.
- Add shared test helpers to reduce duplicated setup code across test classes.

## Persistence
- Swap out the persistance with real DB and make it asynchronous. The repo is registered as a singleton but is not thread-safe so maybe even adding a `ConcurrentBag` or `ConcurrentDictionary` could be a good idea.
- Add a `Payment` domain model so that persisted/domain model is not directly linked to external contract. We could just map from that domain model to any external contracts to keep it all very explicit who is seeing which properties, in what view.

## Currencies
- add more currencies and create a ValueObject to represent currencies rather than just the ISO code as a string. This could have things like the minimum transaction amount, the minor unit size as a decimal, etc.

## Security
- Add authentication so merchants can only retrieve their own payments. Currently any caller can 
retrieve any payment by ID which would be a serious issue in production.
- support api key auth

## Refactor
- Move a bit of the heavy orchestration logic out of the controller to keep it thin.
- Move the create payment into a service class that handles the whole flow and returns a `Result<T>` object to help with isolating the business logic to a more of a `UseCases`/`Application` layer.
- Add feature slices to make it a bit easier for a new developer to see what the project is about.

## Observability
- Back the existing `ILogger` calls with something like Serilog as a provider, adding enrichers for things like 
trace IDs and merchant context, and routing to a proper sink rather than stdout only.
- Would use OpenTelemetry with an OTLP exporter to ship traces, metrics and logs to a collector, giving better correlated observability.
- Would also ensure proper request -> response logging using middleware, ensuring that logs get stripped of PII and sensitive data like card details, auth tokens/keys etc.