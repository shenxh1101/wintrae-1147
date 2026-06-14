using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class CouponValidator : ICouponValidator
{
    public ValidationResult Validate(Coupon coupon, OrderContext context)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = GetUnavailableReasons(coupon, context);

        if (errors.Any())
        {
            result.IsValid = false;
            result.Errors = errors;
        }

        return result;
    }

    public List<string> GetUnavailableReasons(Coupon coupon, OrderContext context)
    {
        var reasons = new List<string>();

        if (!coupon.IsValid())
        {
            reasons.Add($"优惠券已过期或尚未生效 (有效期: {coupon.ValidFrom:yyyy-MM-dd} 至 {coupon.ValidTo:yyyy-MM-dd})");
        }

        if (coupon.RequiredMemberLevel.HasValue && context.Member != null)
        {
            if (context.Member.Level < coupon.RequiredMemberLevel.Value)
            {
                reasons.Add($"需要{GetMemberLevelName(coupon.RequiredMemberLevel.Value)}及以上会员，当前为{GetMemberLevelName(context.Member.Level)}");
            }
        }

        if (context.OriginalAmount < coupon.MinOrderAmount)
        {
            reasons.Add($"订单金额 {context.OriginalAmount:F2} 元未达到最低要求 {coupon.MinOrderAmount:F2} 元");
        }

        var applicableQty = context.GetApplicableQuantityForCoupon(coupon);
        if (applicableQty < coupon.MinQuantity)
        {
            reasons.Add($"适用商品数量 {applicableQty} 件未达到最低要求 {coupon.MinQuantity} 件");
        }

        if (coupon.ApplicableProductIds.Length > 0 || coupon.ApplicableCategoryIds.Length > 0)
        {
            var applicableAmount = context.GetApplicableAmountForCoupon(coupon);
            if (applicableAmount == 0)
            {
                reasons.Add("订单中无可适用的商品");
            }
        }

        return reasons;
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
