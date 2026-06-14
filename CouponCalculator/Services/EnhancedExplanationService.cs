using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class EnhancedExplanationService
{
    public Explanation GenerateExplanation(
        EnhancedCalculationResult result, 
        OrderContext context, 
        List<EnhancedCouponTrialResult> trialResults)
    {
        var explanation = new Explanation();

        foreach (var plan in result.CandidatePlans)
        {
            foreach (var coupon in plan.AppliedCoupons)
            {
                var appliedReason = new AppliedExplanation
                {
                    CouponId = coupon.CouponId,
                    CouponCode = coupon.CouponCode,
                    DiscountAmount = coupon.DiscountAmount,
                    Reason = coupon.Reason,
                    AppliedConditions = coupon.AppliedConditions
                };
                explanation.AppliedReasons.Add(appliedReason);
            }
        }

        foreach (var trial in trialResults.Where(t => !t.IsAvailable))
        {
            var rejectedReason = new RejectedExplanation
            {
                CouponId = trial.Coupon.CouponId,
                CouponCode = trial.Coupon.CouponCode,
                Reason = trial.StatusMessage,
                FailedConditions = trial.UnavailableReasons
            };
            explanation.RejectedReasons.Add(rejectedReason);
        }

        return explanation;
    }

    public string GenerateUserFriendlySummary(
        EnhancedCalculationResult result, 
        OrderContext context)
    {
        var lines = new List<string>();

        lines.Add("📋 优惠方案总结");
        lines.Add("");

        if (result.RecommendedPlan != null)
        {
            lines.Add($"✨ 推荐方案: {result.RecommendedPlan.PlanName}");
            lines.Add($"💰 预计节省: ¥{result.RecommendedPlan.TotalDiscountAmount:F2} 元");
            lines.Add($"📦 最终金额: ¥{result.RecommendedPlan.FinalTotalAmount:F2} 元");
            lines.Add("");

            if (result.RecommendedPlan.AppliedCoupons.Any())
            {
                lines.Add("已选优惠券:");
                foreach (var coupon in result.RecommendedPlan.AppliedCoupons)
                {
                    lines.Add($"  ✓ {coupon.CouponCode} ({coupon.TypeName}): -¥{coupon.DiscountAmount:F2}");
                    lines.Add($"    {coupon.Reason}");
                }
            }

            if (result.RecommendedPlan.MemberDiscount != null)
            {
                lines.Add($"  ✓ 会员折扣: -¥{result.RecommendedPlan.MemberDiscountAmount:F2}");
                lines.Add($"    {result.RecommendedPlan.MemberDiscount.Reason}");
            }
        }

        if (result.Comparison.ExcludedReasons.Any())
        {
            lines.Add("");
            lines.Add("❌ 未使用的优惠券:");
            foreach (var excluded in result.Comparison.ExcludedReasons.Take(3))
            {
                lines.Add($"  · {excluded.Reason}");
            }
        }

        if (result.CandidatePlans.Count > 1)
        {
            lines.Add("");
            lines.Add("📊 方案对比:");
            foreach (var plan in result.CandidatePlans.Take(3))
            {
                var marker = plan.IsRecommended ? "⭐" : "  ";
                lines.Add($"{marker} {plan.PlanName}: 节省 ¥{plan.TotalDiscountAmount:F2} 元, 最终 ¥{plan.FinalTotalAmount:F2} 元");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    public string GenerateComparisonTable(EnhancedCalculationResult result)
    {
        var lines = new List<string>();
        lines.Add("┌─────────────────────────────────────────────────────────────┐");
        lines.Add("│                    💡 优惠方案对比                           │");
        lines.Add("├─────────┬──────────┬──────────┬──────────┬───────────────────┤");
        lines.Add("│  方案   │  优惠券  │  节省    │  最终    │       备注         │");
        lines.Add("├─────────┼──────────┼──────────┼──────────┼───────────────────┤");

        foreach (var plan in result.CandidatePlans.Take(5))
        {
            var marker = plan.IsRecommended ? "⭐" : "  ";
            var planName = $"{marker}{plan.PlanName}".Substring(0, Math.Min(7, $"{marker}{plan.PlanName}".Length)).PadRight(7);
            var couponCount = plan.AppliedCoupons.Count.ToString().PadLeft(4);
            var discount = $"¥{plan.TotalDiscountAmount,7:F2}";
            var finalAmount = $"¥{plan.FinalTotalAmount,7:F2}";
            var note = plan.Advantages.FirstOrDefault() ?? "";
            if (note.Length > 17) note = note.Substring(0, 17) + "..";
            note = note.PadRight(17);

            lines.Add($"│ {planName} │{couponCount} 张 │{discount} │{finalAmount} │ {note} │");
        }

        lines.Add("└─────────┴──────────┴──────────┴──────────┴───────────────────┘");

        return string.Join(Environment.NewLine, lines);
    }

    public string GenerateRejectedReasonDetail(
        Coupon coupon, 
        EnhancedCouponTrialResult trialResult)
    {
        var lines = new List<string>();

        lines.Add($"券名: {coupon.CouponName}({coupon.CouponCode})");
        lines.Add($"类型: {GetCouponTypeName(coupon.Type)}");
        lines.Add($"优惠: {coupon.GetDiscountDescription()}");
        lines.Add("");

        if (trialResult.CheckResults.Any())
        {
            lines.Add("校验详情:");
            foreach (var check in trialResult.CheckResults)
            {
                var status = check.Passed ? "✅" : "❌";
                lines.Add($"  {status} {check.CheckName}");
                if (!check.Passed)
                {
                    lines.Add($"     {check.UserFriendlyMessage}");
                }
            }
        }

        if (trialResult.UnavailableReasons.Any())
        {
            lines.Add("");
            lines.Add("不可用原因:");
            foreach (var reason in trialResult.UnavailableReasons)
            {
                lines.Add($"  · {reason}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    public Dictionary<string, object> GenerateSettlementPageData(
        EnhancedCalculationResult result,
        OrderContext context)
    {
        var data = new Dictionary<string, object>();

        data["originalAmount"] = context.OriginalAmount;
        data["originalFreight"] = context.FreightAmount;
        data["originalTotal"] = context.OriginalAmount + context.FreightAmount;

        if (result.RecommendedPlan != null)
        {
            data["finalAmount"] = result.RecommendedPlan.FinalTotalAmount;
            data["totalDiscount"] = result.RecommendedPlan.TotalDiscountAmount;
            data["productDiscount"] = result.RecommendedPlan.ProductDiscountAmount;
            data["freightDiscount"] = result.RecommendedPlan.FreightDiscountAmount;
            data["memberDiscount"] = result.RecommendedPlan.MemberDiscountAmount;
        }
        else
        {
            data["finalAmount"] = context.OriginalAmount + context.FreightAmount;
            data["totalDiscount"] = 0m;
            data["productDiscount"] = 0m;
            data["freightDiscount"] = 0m;
            data["memberDiscount"] = 0m;
        }

        data["couponCount"] = result.CandidatePlans.FirstOrDefault()?.AppliedCoupons.Count ?? 0;
        data["availablePlans"] = result.CandidatePlans.Count;
        data["userFriendlySummary"] = GenerateUserFriendlySummary(result, context);
        data["comparisonTable"] = GenerateComparisonTable(result);

        var coupons = new List<Dictionary<string, object>>();
        foreach (var plan in result.CandidatePlans)
        {
            foreach (var coupon in plan.AppliedCoupons)
            {
                coupons.Add(new Dictionary<string, object>
                {
                    ["couponId"] = coupon.CouponId,
                    ["couponCode"] = coupon.CouponCode,
                    ["couponName"] = coupon.CouponName,
                    ["type"] = coupon.Type.ToString(),
                    ["typeName"] = coupon.TypeName,
                    ["discountAmount"] = coupon.DiscountAmount,
                    ["isRecommended"] = plan.IsRecommended
                });
            }
        }
        data["appliedCoupons"] = coupons;

        return data;
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
