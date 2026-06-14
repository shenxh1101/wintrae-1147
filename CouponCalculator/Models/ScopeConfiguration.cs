namespace CouponCalculator.Models;

public class ApplicableScope
{
    public List<string> ProductIds { get; set; } = new();
    public List<string> CategoryIds { get; set; } = new();
    public List<string> StoreIds { get; set; } = new();
    public List<string> BrandIds { get; set; } = new();
    public List<string> TagIds { get; set; } = new();

    public bool IsAllApplicable => 
        ProductIds.Count == 0 && 
        CategoryIds.Count == 0 && 
        StoreIds.Count == 0 && 
        BrandIds.Count == 0 && 
        TagIds.Count == 0;

    public string GetScopeDescription()
    {
        var parts = new List<string>();

        if (IsAllApplicable)
            return "全场商品";

        if (ProductIds.Count > 0)
            parts.Add($"{ProductIds.Count}个指定商品");
        if (CategoryIds.Count > 0)
            parts.Add($"{CategoryIds.Count}个指定类目");
        if (StoreIds.Count > 0)
            parts.Add($"{StoreIds.Count}个指定门店");
        if (BrandIds.Count > 0)
            parts.Add($"{BrandIds.Count}个指定品牌");
        if (TagIds.Count > 0)
            parts.Add($"{TagIds.Count}个指定标签");

        return string.Join("、", parts);
    }
}

public class ExcludedScope
{
    public List<string> ProductIds { get; set; } = new();
    public List<string> CategoryIds { get; set; } = new();
    public List<string> StoreIds { get; set; } = new();

    public bool HasAnyExclusion => 
        ProductIds.Count > 0 || 
        CategoryIds.Count > 0 || 
        StoreIds.Count > 0;

    public string GetExclusionDescription()
    {
        var parts = new List<string>();

        if (ProductIds.Count > 0)
            parts.Add($"{ProductIds.Count}个指定商品");
        if (CategoryIds.Count > 0)
            parts.Add($"{CategoryIds.Count}个指定类目");
        if (StoreIds.Count > 0)
            parts.Add($"{StoreIds.Count}个指定门店");

        return parts.Count > 0 ? $"不包含{string.Join("、", parts)}" : "";
    }
}

public class UsageCondition
{
    public decimal? MinOrderAmount { get; set; }
    public int? MinQuantity { get; set; }
    public decimal? MaxOrderAmount { get; set; }
    public int? MaxQuantity { get; set; }
    public List<MemberLevel>? AllowedMemberLevels { get; set; }
    public List<string>? AllowedUserIds { get; set; }
    public List<string>? AllowedStoreIds { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public bool? RequireFirstOrder { get; set; }
    public decimal? MinDistanceKm { get; set; }
    public decimal? MaxDistanceKm { get; set; }

    public List<string> GetUnmetConditions(OrderContext context)
    {
        var unmetConditions = new List<string>();

        if (MinOrderAmount.HasValue && context.OriginalAmount < MinOrderAmount.Value)
        {
            unmetConditions.Add($"订单金额 {context.OriginalAmount:F2}元 未达到最低要求 {MinOrderAmount.Value:F2}元");
        }

        if (MaxOrderAmount.HasValue && context.OriginalAmount > MaxOrderAmount.Value)
        {
            unmetConditions.Add($"订单金额 {context.OriginalAmount:F2}元 超过最高限制 {MaxOrderAmount.Value:F2}元");
        }

        if (MinQuantity.HasValue)
        {
            var totalQty = context.Items.Sum(i => i.Quantity);
            if (totalQty < MinQuantity.Value)
            {
                unmetConditions.Add($"商品数量 {totalQty}件 未达到最低要求 {MinQuantity.Value}件");
            }
        }

        if (MaxQuantity.HasValue)
        {
            var totalQty = context.Items.Sum(i => i.Quantity);
            if (totalQty > MaxQuantity.Value)
            {
                unmetConditions.Add($"商品数量 {totalQty}件 超过最高限制 {MaxQuantity.Value}件");
            }
        }

        if (AllowedMemberLevels != null && AllowedMemberLevels.Count > 0 && context.Member != null)
        {
            if (!AllowedMemberLevels.Contains(context.Member.Level))
            {
                var currentLevelName = GetMemberLevelName(context.Member.Level);
                var allowedNames = string.Join("、", AllowedMemberLevels.Select(GetMemberLevelName));
                unmetConditions.Add($"您的会员等级为{currentLevelName}，仅限 {allowedNames} 使用");
            }
        }

        if (AllowedUserIds != null && AllowedUserIds.Count > 0 && context.Member != null)
        {
            if (!AllowedUserIds.Contains(context.Member.MemberId))
            {
                unmetConditions.Add("此券不适用于您的账户");
            }
        }

        if (AllowedStoreIds != null && AllowedStoreIds.Count > 0 && context.ExtendedData.TryGetValue("StoreId", out var storeId))
        {
            if (!AllowedStoreIds.Contains(storeId?.ToString() ?? ""))
            {
                unmetConditions.Add("此券不适用于当前门店");
            }
        }

        if (ValidFrom.HasValue && DateTime.UtcNow < ValidFrom.Value)
        {
            unmetConditions.Add($"优惠券将在 {ValidFrom.Value:yyyy-MM-dd HH:mm} 开始生效");
        }

        if (ValidTo.HasValue && DateTime.UtcNow > ValidTo.Value)
        {
            unmetConditions.Add($"优惠券已于 {ValidTo.Value:yyyy-MM-dd HH:mm} 到期");
        }

        if (RequireFirstOrder == true && context.ExtendedData.TryGetValue("OrderCount", out var orderCount))
        {
            if (Convert.ToInt32(orderCount) > 0)
            {
                unmetConditions.Add("此券仅限新用户首单使用");
            }
        }

        return unmetConditions;
    }

    public string GetConditionSummary()
    {
        var conditions = new List<string>();

        if (MinOrderAmount.HasValue)
            conditions.Add($"满{MinOrderAmount.Value:F0}元");
        if (MinQuantity.HasValue)
            conditions.Add($"满{MinQuantity.Value}件");
        if (AllowedMemberLevels != null && AllowedMemberLevels.Count > 0)
            conditions.Add($"限{string.Join("/", AllowedMemberLevels.Select(GetMemberLevelName))}");
        if (RequireFirstOrder == true)
            conditions.Add("新客专享");
        if (ValidFrom.HasValue || ValidTo.HasValue)
        {
            var dateRange = "";
            if (ValidFrom.HasValue)
                dateRange += ValidFrom.Value.ToString("MM-dd");
            dateRange += "至";
            if (ValidTo.HasValue)
                dateRange += ValidTo.Value.ToString("MM-dd");
            conditions.Add(dateRange);
        }

        return conditions.Count > 0 ? string.Join("，", conditions) : "无门槛";
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
