using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class StackingEngine : IStackingEngine
{
    public bool CanStack(Coupon coupon1, Coupon coupon2)
    {
        if (coupon1.CouponId == coupon2.CouponId)
            return false;

        if (!string.IsNullOrEmpty(coupon1.StackingGroup) && 
            coupon1.StackingGroup == coupon2.StackingGroup)
        {
            return false;
        }

        if (coupon1.Type == CouponType.FreeShipping && coupon2.Type == CouponType.FreeShipping)
            return false;

        if (!coupon1.CanStackWithSameType && coupon1.Type == coupon2.Type)
            return false;

        if (coupon1.Type == CouponType.MemberExclusive || coupon2.Type == CouponType.MemberExclusive)
        {
            if (coupon1.Type == coupon2.Type)
                return false;
        }

        return true;
    }

    public List<Coupon> GetStackableCoupons(List<Coupon> coupons)
    {
        if (coupons.Count <= 1)
            return coupons;

        var result = new List<Coupon>();
        var usedGroups = new HashSet<string>();

        var sortedCoupons = coupons.OrderByDescending(c => c.Priority).ThenByDescending(c => c.DiscountValue).ToList();

        foreach (var coupon in sortedCoupons)
        {
            if (!string.IsNullOrEmpty(coupon.StackingGroup))
            {
                if (usedGroups.Contains(coupon.StackingGroup))
                    continue;
                usedGroups.Add(coupon.StackingGroup);
            }

            var canAdd = true;
            foreach (var existing in result)
            {
                if (!CanStack(coupon, existing))
                {
                    canAdd = false;
                    break;
                }
            }

            if (canAdd)
            {
                result.Add(coupon);
            }
        }

        return result;
    }

    public List<List<Coupon>> GetValidCombinations(List<Coupon> coupons)
    {
        var result = new List<List<Coupon>>();
        var sortedCoupons = coupons.OrderByDescending(c => c.Priority).ToList();

        GenerateCombinations(sortedCoupons, 0, new List<Coupon>(), result);

        return result;
    }

    private void GenerateCombinations(List<Coupon> coupons, int index, List<Coupon> current, List<List<Coupon>> result)
    {
        if (index >= coupons.Count)
        {
            result.Add(new List<Coupon>(current));
            return;
        }

        current.Add(coupons[index]);
        if (IsValidCombination(current))
        {
            GenerateCombinations(coupons, index + 1, current, result);
        }
        current.RemoveAt(current.Count - 1);

        GenerateCombinations(coupons, index + 1, current, result);
    }

    private bool IsValidCombination(List<Coupon> combination)
    {
        var usedGroups = new HashSet<string>();

        foreach (var coupon in combination)
        {
            if (!string.IsNullOrEmpty(coupon.StackingGroup))
            {
                if (usedGroups.Contains(coupon.StackingGroup))
                    return false;
                usedGroups.Add(coupon.StackingGroup);
            }
        }

        for (int i = 0; i < combination.Count; i++)
        {
            for (int j = i + 1; j < combination.Count; j++)
            {
                if (!CanStack(combination[i], combination[j]))
                    return false;
            }
        }

        return true;
    }
}
