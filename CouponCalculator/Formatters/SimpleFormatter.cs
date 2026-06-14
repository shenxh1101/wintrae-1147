using CouponCalculator.Models;

namespace CouponCalculator.Formatters;

public class SimpleFormatter : IResultFormatter
{
    public string Format(CalculationResult result)
    {
        var lines = FormatLines(result);
        return string.Join(Environment.NewLine, lines);
    }

    public List<string> FormatLines(CalculationResult result)
    {
        var lines = new List<string>();

        lines.Add($"商品总额: ¥{result.OriginalAmount:F2}");
        
        if (result.ProductDiscountAmount > 0)
        {
            lines.Add($"商品优惠: -¥{result.ProductDiscountAmount:F2}");
        }

        if (result.FreightDiscount > 0)
        {
            lines.Add($"运费优惠: -¥{result.FreightDiscount:F2}");
        }

        if (result.MemberDiscount > 0)
        {
            lines.Add($"会员折扣: -¥{result.MemberDiscount:F2}");
        }

        lines.Add($"应付总额: ¥{result.FinalAmount:F2}");

        return lines;
    }
}
