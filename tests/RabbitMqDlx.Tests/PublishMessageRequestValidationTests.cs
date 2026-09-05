using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FluentAssertions;
using RabbitMqDlx.Shared.Models;

namespace RabbitMqDlx.Tests;

public sealed class PublishMessageRequestValidationTests
{
    [Fact]
    public void Amount_Range_trTR_kulturunde_gecerli_tutari_kabul_eder()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");

            var request = new PublishMessageRequest
            {
                CustomerId = "cust-1",
                Amount = 10.50m,
                FailureMode = FailureMode.None
            };

            var results = new List<ValidationResult>();
            var ok = Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true);

            ok.Should().BeTrue(because: string.Join("; ", results.Select(r => r.ErrorMessage)));
            results.Should().BeEmpty();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Amount_Range_sifir_ve_negatif_reddedilir(decimal amount)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            var request = new PublishMessageRequest
            {
                CustomerId = "cust-1",
                Amount = amount,
                FailureMode = FailureMode.None
            };

            var results = new List<ValidationResult>();
            var ok = Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true);

            ok.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(PublishMessageRequest.Amount)));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
