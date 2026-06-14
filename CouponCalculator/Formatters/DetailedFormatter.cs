using CouponCalculator.Models;

namespace CouponCalculator.Formatters;

public class DetailedFormatter : IResultFormatter
{
    public string Format(CalculationResult result)
    {
        var lines = FormatLines(result);
        return string.Join(Environment.NewLine, lines);
    }

    public List<string> FormatLines(CalculationResult result)
    {
        var lines = new List<string>();

        lines.Add("═══════════════════════════════════════════");
        lines.Add("               订单优惠明细                    ");
        lines.Add("═══════════════════════════════════════════");

        lines.Add($"订单号: {result.AppliedCoupons.FirstOrDefault()?.AppliedAt:yyyyMMddHHmmss}");

        lines.Add("");
        lines.Add("【商品信息】");
        lines.Add($"  商品总额: ¥{result.OriginalAmount:F2}");

        if (result.AppliedCoupons.Any())
        {
            lines.Add("");
            lines.Add("【优惠券信息】");
            foreach (var coupon in result.AppliedCoupons)
            {
                lines.Add($"  · {coupon.CouponCode} ({GetCouponTypeName(coupon.Type)})");
                lines.Add($"    优惠金额: -¥{coupon.DiscountAmount:F2}");
                if (!string.IsNullOrEmpty(coupon.Description))
                {
                    lines.Add($"    说明: {coupon.Description}");
                }
            }
        }

        if (result.ProductDiscountAmount > 0)
        {
            lines.Add("");
            lines.Add($"【商品优惠】: -¥{result.ProductDiscountAmount:F2}");
        }

        if (result.FreightDiscount > 0)
        {
            lines.Add("");
            lines.Add($"【运费优惠】: -¥{result.FreightDiscount:F2}");
            lines.Add($"  运费: ¥{result.OriginalFreight:F2} → ¥{result.FinalFreight:F2}");
        }

        if (result.MemberDiscount > 0)
        {
            lines.Add("");
            lines.Add($"【会员折扣】: -¥{result.MemberDiscount:F2}");
        }

        lines.Add("");
        lines.Add("───────────────────────────────────────────");
        lines.Add($"  总优惠金额: -¥{result.TotalDiscountAmount:F2}");
        lines.Add($"  应付总额:   ¥{result.FinalAmount:F2}");
        lines.Add("═══════════════════════════════════════════");

        if (result.Explanation.RejectedReasons.Any())
        {
            lines.Add("");
            lines.Add("【未使用的优惠券】");
            foreach (var rejected in result.Explanation.RejectedReasons)
            {
                lines.Add($"  · {rejected.CouponCode}");
                foreach (var reason in rejected.FailedConditions)
                {
                    lines.Add($"    - {reason}");
                }
            }
        }

        return lines;
    }

    private static string GetCouponTypeName(CouponType type)
    {
        return type switch
        {
            CouponType.AmountOff => "满减券",
            CouponType.DiscountRate => "折扣券",
            CouponType.QuantityDiscount => "满件折",
            CouponType.FreeShipping => "免运费券",
            CouponType.MemberExclusive => "会员专享",
            _ => type.ToString()
        };
    }
}
