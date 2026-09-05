using Microsoft.Extensions.Options;
using RabbitMqDlx.Consumer.Services;
using RabbitMqDlx.Shared.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<RabbitMqOptions>, RabbitMqOptionsValidator>();
builder.Services.AddHostedService<OrderConsumerService>();

var host = builder.Build();
host.Run();
