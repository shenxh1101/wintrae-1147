using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class ExplanationService
{
    public Explanation ExplainAppliedCoupon(Coupon coupon, OrderContext context, decimal discountAmount)
    {
        var explanation = new Explanation();
        var appliedReason = new AppliedExplanation
        {
            CouponId = coupon.CouponId,
            CouponCode = coupon.CouponCode,
            DiscountAmount = discountAmount
        };

        switch (coupon.Type)
        {
            case CouponType.AmountOff:
                appliedReason.Reason = $"满{coupon.MinOrderAmount:F0}减{coupon.DiscountValue:F0}优惠";
                appliedReason.AppliedConditions.Add($"订单金额 {context.OriginalAmount:F2} 元 >= {coupon.MinOrderAmount:F0} 元");
                break;

            case CouponType.DiscountRate:
                var rate = (1 - coupon.DiscountValue) * 100;
                appliedReason.Reason = $"享受 {rate:F0}% 折扣";
                appliedReason.AppliedConditions.Add($"折扣率 {coupon.DiscountValue * 100:F0}%");
                if (coupon.MaxDiscountAmount.HasValue)
                {
                    appliedReason.AppliedConditions.Add($"最高优惠 {coupon.MaxDiscountAmount:F2} 元");
                }
                break;

            case CouponType.QuantityDiscount:
                var discountRate = (1 - coupon.DiscountValue) * 100;
                appliedReason.Reason = $"满{coupon.MinQuantity}件享 {discountRate:F0}% 折扣";
                appliedReason.AppliedConditions.Add($"购买数量 {context.GetApplicableQuantityForCoupon(coupon)} 件 >= {coupon.MinQuantity} 件");
                break;

            case CouponType.FreeShipping:
                appliedReason.Reason = "免运费优惠";
                appliedReason.AppliedConditions.Add("运费券已使用");
                break;

            case CouponType.MemberExclusive:
                var memberRate = (1 - coupon.DiscountValue) * 100;
                appliedReason.Reason = $"会员专享 {memberRate:F0}% 折扣";
                if (context.Member != null)
                {
                    appliedReason.AppliedConditions.Add($"会员等级: {GetMemberLevelName(context.Member.Level)}");
                }
                break;
        }

        explanation.AppliedReasons.Add(appliedReason);
        return explanation;
    }

    public Explanation ExplainRejectedCoupon(Coupon coupon, OrderContext context, List<string> reasons)
    {
        var explanation = new Explanation();
        var rejectedReason = new RejectedExplanation
        {
            CouponId = coupon.CouponId,
            CouponCode = coupon.CouponCode,
            Reason = "无法使用该优惠券",
            FailedConditions = reasons
        };

        explanation.RejectedReasons.Add(rejectedReason);
        return explanation;
    }

    private static string GetMemberLevelName(MemberLevel level)
    {
        return level switch
        {
            MemberLevel.Normal => "普通会员",
            MemberLevel.Silver => "银卡会员",
            MemberLevel.Gold => "金卡会员",
            MemberLevel.Platinum => "白金会员",
            MemberLevel.Diamond => "钻石会员",
            _ => level.ToString()
        };
    }
}
