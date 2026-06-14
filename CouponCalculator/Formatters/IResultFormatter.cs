using CouponCalculator.Models;

namespace CouponCalculator.Formatters;

public interface IResultFormatter
{
    string Format(CalculationResult result);
    List<string> FormatLines(CalculationResult result);
}
