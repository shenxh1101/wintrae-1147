using CouponCalculator.Models;

namespace CouponCalculator.Engine;

public interface IRuleProvider
{
    IEnumerable<DiscountRule> GetRules();
    DiscountRule? GetRule(string ruleId);
    void AddRule(DiscountRule rule);
    void RemoveRule(string ruleId);
    void ClearRules();
}
