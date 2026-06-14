using CouponCalculator.Models;

namespace CouponCalculator.Rules;

public class MemberDiscountRule : BaseRule
{
    public MemberLevel RequiredLevel { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal? MaxDiscount { get; set; }

    public MemberDiscountRule()
    {
        Type = DiscountRuleType.MemberDiscount;
    }

    public override bool IsApplicable(OrderContext context)
    {
        if (context.Member == null)
            return false;

        return context.Member.Level >= RequiredLevel;
    }

    public override decimal CalculateDiscount(OrderContext context)
    {
        if (!IsApplicable(context))
            return 0;

        var discount = context.OriginalAmount * (1 - DiscountRate);

        if (MaxDiscount.HasValue)
        {
            discount = Math.Min(discount, MaxDiscount.Value);
        }

        return discount;
    }

    public override List<string> GetUnmetConditions(OrderContext context)
    {
        var conditions = new List<string>();

        if (context.Member == null)
        {
            conditions.Add("用户未登录或不是会员");
        }
        else if (context.Member.Level < RequiredLevel)
        {
            conditions.Add($"需要{GetMemberLevelName(RequiredLevel)}及以上会员，当前为{GetMemberLevelName(context.Member.Level)}");
        }

        return conditions;
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
