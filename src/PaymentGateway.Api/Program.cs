using System.Net;
using FluentValidation;
using Microsoft.Extensions.Options;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validators;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IValidator<PostPaymentRequest>, PostPaymentRequestValidator>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<CurrencyOptions>(builder.Configuration.GetSection(nameof(CurrencyOptions)));
builder.Services.Configure<AcquirerOptions>(builder.Configuration.GetSection(nameof(AcquirerOptions)));
builder.Services.AddScoped<IAcquirerService, AcquirerService>();
builder.Services.AddSingleton<PaymentsRepository>();

builder.Services.AddHttpClient<IAcquirerService, AcquirerService>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<AcquirerOptions>>();
        client.BaseAddress = options.Value.BaseUrl;
    })
    .AddPolicyHandler(GetRetryPolicy());

var app = builder.Build();

app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePages();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions.HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(2), retryCount: 2));
} 

public partial class Program { }