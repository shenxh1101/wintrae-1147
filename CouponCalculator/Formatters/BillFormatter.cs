using CouponCalculator.Models;

namespace CouponCalculator.Formatters;

public class BillFormatter : IResultFormatter
{
    public string Format(CalculationResult result)
    {
        var lines = FormatLines(result);
        return string.Join(Environment.NewLine, lines);
    }

    public List<string> FormatLines(CalculationResult result)
    {
        var lines = new List<string>();
        var width = 40;

        lines.Add(AlignCenter("● 订单账单 ●", width));
        lines.Add(new string('─', width));

        lines.Add(AlignLeft("商品总额", width));
        lines.Add(AlignRight($"¥{result.OriginalAmount:F2}", width));

        if (result.ProductDiscountAmount > 0)
        {
            lines.Add(AlignLeft("  商品优惠", width));
            lines.Add(AlignRight($"-¥{result.ProductDiscountAmount:F2}", width));
        }

        if (result.FreightDiscount > 0)
        {
            lines.Add(AlignLeft("  运费优惠", width));
            lines.Add(AlignRight($"-¥{result.FreightDiscount:F2}", width));
        }

        if (result.MemberDiscount > 0)
        {
            lines.Add(AlignLeft("  会员折扣", width));
            lines.Add(AlignRight($"-¥{result.MemberDiscount:F2}", width));
        }

        lines.Add(new string('─', width));

        lines.Add(AlignLeft("应付总额", width, true));
        lines.Add(AlignRight($"¥{result.FinalAmount:F2}", width, true));

        if (result.AppliedCoupons.Any())
        {
            lines.Add("");
            lines.Add(AlignCenter("已使用优惠券", width));
            lines.Add(new string('─', width));

            foreach (var coupon in result.AppliedCoupons)
            {
                var couponLine = $"{coupon.CouponCode} ({GetCouponTypeName(coupon.Type)})";
                lines.Add(AlignLeft(couponLine, width));
                lines.Add(AlignRight($"-¥{coupon.DiscountAmount:F2}", width));
            }
        }

        if (result.Explanation.RejectedReasons.Any())
        {
            lines.Add("");
            lines.Add(AlignCenter("未使用优惠券及原因", width));
            lines.Add(new string('─', width));

            foreach (var rejected in result.Explanation.RejectedReasons)
            {
                lines.Add(AlignLeft($"· {rejected.CouponCode}", width));
                foreach (var reason in rejected.FailedConditions.Take(2))
                {
                    var shortReason = reason.Length > 35 ? reason.Substring(0, 35) + "..." : reason;
                    lines.Add($"  {shortReason}");
                }
            }
        }

        lines.Add(new string('═', width));
        lines.Add(AlignCenter($"计算时间: {result.CalculatedAt:yyyy-MM-dd HH:mm:ss}", width));

        return lines;
    }

    private static string AlignLeft(string text, int width, bool bold = false)
    {
        var padding = width - GetDisplayLength(text);
        return (bold ? "★ " : "  ") + text + new string(' ', Math.Max(0, padding));
    }

    private static string AlignRight(string text, int width, bool bold = false)
    {
        var padding = width - GetDisplayLength(text) - (bold ? 2 : 2);
        return (bold ? "★ " : "  ") + new string(' ', Math.Max(0, padding)) + text;
    }

    private static string AlignCenter(string text, int width)
    {
        var padding = (width - GetDisplayLength(text)) / 2;
        return new string(' ', Math.Max(0, padding)) + text;
    }

    private static int GetDisplayLength(string text)
    {
        return text.Length;
    }

    private static string GetCouponTypeName(CouponType type)
    {
        return type switch
        {
            CouponType.AmountOff => "满减",
            CouponType.DiscountRate => "折扣",
            CouponType.QuantityDiscount => "满件",
            CouponType.FreeShipping => "免运",
            CouponType.MemberExclusive => "会员",
            _ => type.ToString()
        };
    }
}
