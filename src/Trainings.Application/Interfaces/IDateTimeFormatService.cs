using System.Globalization;

namespace Trainings.Application.Interfaces;

public interface IDateTimeFormatService
{
    CultureInfo GetCultureForCountry(string? countryCode);
    string GetDefaultCountry();
}
