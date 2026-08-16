using System.Globalization;
using Microsoft.Extensions.Configuration;
using Trainings.Application.Interfaces;

namespace Trainings.Infrastructure.Services;

public class DateTimeFormatService(IConfiguration configuration) : IDateTimeFormatService
{
    private readonly string _defaultCountry = (configuration["App:DefaultCountry"] ?? "CH").Trim().ToUpperInvariant();

    public CultureInfo GetCultureForCountry(string? countryCode)
    {
        string code = string.IsNullOrWhiteSpace(countryCode)
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
