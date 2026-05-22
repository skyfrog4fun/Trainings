using System.Globalization;
using Microsoft.Extensions.Configuration;
using Trainings.Application.Interfaces;

namespace Trainings.Infrastructure.Services;

public class DateTimeFormatService : IDateTimeFormatService
{
    private readonly string _defaultCountry;

    public DateTimeFormatService(IConfiguration configuration)
    {
        _defaultCountry = (configuration["App:DefaultCountry"] ?? "CH").Trim().ToUpperInvariant();
    }

    public CultureInfo GetCultureForCountry(string? countryCode)
    {
        var code = string.IsNullOrWhiteSpace(countryCode)
            ? _defaultCountry
            : countryCode.Trim().ToUpperInvariant();

        return code switch
        {
            "CH" => new CultureInfo("de-CH"),
            "DE" => new CultureInfo("de-DE"),
            "AT" => new CultureInfo("de-AT"),
            "FR" => new CultureInfo("fr-FR"),
            "IT" => new CultureInfo("it-IT"),
            "US" => new CultureInfo("en-US"),
            "GB" => new CultureInfo("en-GB"),
            _ => new CultureInfo("en-CH")
        };
    }

    public string GetDefaultCountry() => _defaultCountry;
}
