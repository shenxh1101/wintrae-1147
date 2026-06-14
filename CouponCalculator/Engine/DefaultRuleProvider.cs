using CouponCalculator.Models;

namespace CouponCalculator.Engine;

public class DefaultRuleProvider : IRuleProvider
{
    private readonly Dictionary<string, DiscountRule> _rules = new();

    public DefaultRuleProvider()
    {
        InitializeDefaultRules();
    }

    private void InitializeDefaultRules()
    {
        AddRule(new DiscountRule
        {
            RuleId = "RULE_AMOUNT_100",
            RuleName = "满100减10",
            Type = DiscountRuleType.AmountThreshold,
            Threshold = 100,
            DiscountValue = 10,
            Priority = 1
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_AMOUNT_200",
            RuleName = "满200减30",
            Type = DiscountRuleType.AmountThreshold,
            Threshold = 200,
            DiscountValue = 30,
            Priority = 2
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_AMOUNT_500",
            RuleName = "满500减80",
            Type = DiscountRuleType.AmountThreshold,
            Threshold = 500,
            DiscountValue = 80,
            Priority = 3
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_QUANTITY_2",
            RuleName = "满2件9折",
            Type = DiscountRuleType.QuantityThreshold,
            Threshold = 2,
            DiscountValue = 0.9m,
            Priority = 1
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_QUANTITY_3",
            RuleName = "满3件8折",
            Type = DiscountRuleType.QuantityThreshold,
            Threshold = 3,
            DiscountValue = 0.8m,
            Priority = 2
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_DISCOUNT_95",
            RuleName = "全场95折",
            Type = DiscountRuleType.DiscountRate,
            DiscountValue = 0.95m,
            MaxDiscount = 50,
            Priority = 0
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_DISCOUNT_90",
            RuleName = "全场9折",
            Type = DiscountRuleType.DiscountRate,
            DiscountValue = 0.9m,
            MaxDiscount = 100,
            Priority = 0
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_FREESHIPPING",
            RuleName = "免运费",
            Type = DiscountRuleType.FreeShipping,
            DiscountValue = 0,
            Priority = 0,
            IsStackable = true
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_MEMBER_SILVER",
            RuleName = "银卡会员95折",
            Type = DiscountRuleType.MemberDiscount,
            DiscountValue = 0.95m,
            Priority = 0
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_MEMBER_GOLD",
            RuleName = "金卡会员9折",
            Type = DiscountRuleType.MemberDiscount,
            DiscountValue = 0.9m,
            Priority = 0
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_MEMBER_PLATINUM",
            RuleName = "白金会员85折",
            Type = DiscountRuleType.MemberDiscount,
            DiscountValue = 0.85m,
            Priority = 0
        });

        AddRule(new DiscountRule
        {
            RuleId = "RULE_MEMBER_DIAMOND",
            RuleName = "钻石会员8折",
            Type = DiscountRuleType.MemberDiscount,
            DiscountValue = 0.8m,
            Priority = 0
        });
    }

    public IEnumerable<DiscountRule> GetRules()
    {
        return _rules.Values.OrderBy(r => r.Priority);
    }

    public DiscountRule? GetRule(string ruleId)
    {
        return _rules.TryGetValue(ruleId, out var rule) ? rule : null;
    }

    public void AddRule(DiscountRule rule)
    {
        if (!string.IsNullOrEmpty(rule.RuleId))
        {
            _rules[rule.RuleId] = rule;
        }
    }

    public void RemoveRule(string ruleId)
    {
        _rules.Remove(ruleId);
    }

    public void ClearRules()
    {
        _rules.Clear();
    }
}
