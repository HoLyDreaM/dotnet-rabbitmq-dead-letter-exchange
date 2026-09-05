using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RabbitMqDlx.Producer.Services;
using RabbitMqDlx.Shared.Models;
using RabbitMqDlx.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<RabbitMqOptions>, RabbitMqOptionsValidator>();
builder.Services.AddSingleton<IOrderPublisher, OrderPublisher>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "RabbitMqDlx.Producer" }));

app.MapPost("/api/messages", async (
    [FromBody] PublishMessageRequest request,
    IOrderPublisher publisher,
    CancellationToken cancellationToken) =>
{
    if (request is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [""] = ["Gövde zorunludur."]
        });
    }

    var validation = new List<ValidationResult>();
    if (!Validator.TryValidateObject(request, new ValidationContext(request), validation, validateAllProperties: true))
    {
        var errors = validation
            .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "")
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Geçersiz").ToArray());
        return Results.ValidationProblem(errors);
    }

    if (!Enum.IsDefined(typeof(FailureMode), request.FailureMode))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.FailureMode)] = ["FailureMode None, Transient veya Poison olmalıdır."]
        });
    }

    var message = new OrderMessage
    {
        OrderId = request.OrderId == Guid.Empty ? Guid.NewGuid() : request.OrderId,
        CustomerId = request.CustomerId,
        Amount = request.Amount,
        FailureMode = request.FailureMode,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    await publisher.PublishAsync(message, cancellationToken);

    return Results.Accepted(
        $"/api/messages/{message.OrderId}",
        new
        {
            message.OrderId,
            message.CustomerId,
            message.Amount,
            message.FailureMode,
            message.CreatedAtUtc,
            status = "AcceptedAfterConfirm"
        });
});

app.Run();

public sealed class PublishMessageRequest
{
    public Guid OrderId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string CustomerId { get; set; } = string.Empty;

    // double overload: string Range(typeof(decimal), "0.01", ...) tr-TR kültüründe 500 üretir
    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [Required]
    public FailureMode FailureMode { get; set; } = FailureMode.None;
}

// WebApplicationFactory / entegrasyon testleri için
public partial class Program;
