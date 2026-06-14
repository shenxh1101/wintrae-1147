namespace CouponCalculator.Models;

public class Coupon
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    
    public ApplicableScope ApplicableScope { get; set; } = new();
    public ExcludedScope ExcludedScope { get; set; } = new();
    public UsageCondition UsageCondition { get; set; } = new();
    
    public int Priority { get; set; }
    public string StackingGroup { get; set; } = string.Empty;
    public bool CanStackWithSameType { get; set; }
    
    public CouponLimit Limit { get; set; } = new();
    public CouponMetadata Metadata { get; set; } = new();

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    public string[] ApplicableProductIds 
    { 
        get => ApplicableScope.ProductIds.ToArray();
        set => ApplicableScope.ProductIds = value.ToList();
    }
    
    public string[] ApplicableCategoryIds 
    { 
        get => ApplicableScope.CategoryIds.ToArray();
        set => ApplicableScope.CategoryIds = value.ToList();
    }
    
    public string[] ExcludedProductIds 
    { 
        get => ExcludedScope.ProductIds.ToArray();
        set => ExcludedScope.ProductIds = value.ToList();
    }
    
    public decimal MinOrderAmount 
    { 
        get => UsageCondition.MinOrderAmount ?? 0;
        set => UsageCondition.MinOrderAmount = value;
    }
    
    public int MinQuantity 
    { 
        get => UsageCondition.MinQuantity ?? 0;
        set => UsageCondition.MinQuantity = value;
    }
    
    public MemberLevel? RequiredMemberLevel 
    { 
        get => UsageCondition.AllowedMemberLevels?.FirstOrDefault();
        set 
        {
            if (value.HasValue)
                UsageCondition.AllowedMemberLevels = new List<MemberLevel> { value.Value };
        }
    }

    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return now >= ValidFrom && now <= ValidTo;
    }

    public bool IsApplicableToProduct(string productId)
    {
        if (ExcludedScope.ProductIds.Contains(productId))
            return false;

        if (ApplicableScope.IsAllApplicable)
            return true;

        if (ApplicableScope.ProductIds.Count > 0)
            return ApplicableScope.ProductIds.Contains(productId);

        return true;
    }

    public bool IsApplicableToCategory(string categoryId)
    {
        if (ExcludedScope.CategoryIds.Contains(categoryId))
            return false;

        if (ApplicableScope.CategoryIds.Count == 0)
            return true;

        return ApplicableScope.CategoryIds.Contains(categoryId);
    }

    public bool IsApplicableToStore(string storeId)
    {
        if (ExcludedScope.StoreIds.Contains(storeId))
            return false;

        if (ApplicableScope.StoreIds.Count == 0)
            return true;

        return ApplicableScope.StoreIds.Contains(storeId);
    }

    public decimal GetApplicableAmount(OrderContext context)
    {
        var applicableItems = context.Items.Where(item => 
            !ExcludedScope.ProductIds.Contains(item.ProductId) &&
            !ExcludedScope.CategoryIds.Contains(item.CategoryId) &&
            IsApplicableToProduct(item.ProductId) &&
            IsApplicableToCategory(item.CategoryId)
        );

        if (ApplicableScope.ProductIds.Count > 0 || ApplicableScope.CategoryIds.Count > 0)
        {
            applicableItems = applicableItems.Where(item =>
                (ApplicableScope.ProductIds.Count == 0 || ApplicableScope.ProductIds.Contains(item.ProductId)) &&
                (ApplicableScope.CategoryIds.Count == 0 || ApplicableScope.CategoryIds.Contains(item.CategoryId))
            );
        }

        return applicableItems.Sum(item => item.TotalAmount);
    }

    public int GetApplicableQuantity(OrderContext context)
    {
        var applicableItems = context.Items.Where(item => 
            !ExcludedScope.ProductIds.Contains(item.ProductId) &&
            !ExcludedScope.CategoryIds.Contains(item.CategoryId) &&
            IsApplicableToProduct(item.ProductId) &&
            IsApplicableToCategory(item.CategoryId)
        );

        if (ApplicableScope.ProductIds.Count > 0 || ApplicableScope.CategoryIds.Count > 0)
        {
            applicableItems = applicableItems.Where(item =>
                (ApplicableScope.ProductIds.Count == 0 || ApplicableScope.ProductIds.Contains(item.ProductId)) &&
                (ApplicableScope.CategoryIds.Count == 0 || ApplicableScope.CategoryIds.Contains(item.CategoryId))
            );
        }

        return applicableItems.Sum(item => item.Quantity);
    }

    public string GetScopeDescription()
    {
        var descriptions = new List<string>();

        if (ApplicableScope.IsAllApplicable && !ExcludedScope.HasAnyExclusion)
        {
            return "全场通用";
        }

        if (!ApplicableScope.IsAllApplicable)
        {
            descriptions.Add(ApplicableScope.GetScopeDescription());
        }

        if (ExcludedScope.HasAnyExclusion)
        {
            descriptions.Add("不包含" + ExcludedScope.GetExclusionDescription().Replace("不包含", ""));
        }

        return string.Join("，", descriptions);
    }

    public string GetConditionSummary()
    {
        return UsageCondition.GetConditionSummary();
    }

    public string GetDiscountDescription()
    {
        return Type switch
        {
            CouponType.AmountOff => $"减{DiscountValue:F0}元",
            CouponType.DiscountRate => $"{DiscountValue * 100:F0}折",
            CouponType.QuantityDiscount => $"满{MinQuantity}件{DiscountValue * 100:F0}折",
            CouponType.FreeShipping => "免运费",
            CouponType.MemberExclusive => $"会员专享{DiscountValue * 100:F0}折",
            _ => $"{DiscountValue}"
        };
    }
}

public class CouponLimit
{
    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public int? MaxUsagePerDay { get; set; }
    public decimal? MaxDiscountPerUse { get; set; }
}

public class CouponMetadata
{
    public string? CampaignId { get; set; }
    public string? Tag { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string> CustomData { get; set; } = new();
}
