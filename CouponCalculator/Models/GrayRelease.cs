namespace CouponCalculator.Models;

public class GrayReleasePolicy
{
    public string PolicyId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public GrayScaleConfig ScaleConfig { get; set; } = new();
    
    public List<GrayTarget> Targets { get; set; } = new();
    
    public List<GrayCondition> Conditions { get; set; } = new();
    
    public GrayReleaseStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    public List<GrayStage> Stages { get; set; } = new();
    
    public GrayReleaseMetrics Metrics { get; set; } = new();
    
    public bool IsEffectiveAt(DateTime time)
    {
        if (Status != GrayReleaseStatus.Active && Status != GrayReleaseStatus.InProgress)
            return false;
            
        if (EffectiveFrom.HasValue && time < EffectiveFrom.Value)
            return false;
            
        if (EffectiveTo.HasValue && time > EffectiveTo.Value)
            return false;
            
        return true;
    }
    
    public bool IsTargeted(string storeId, string userId, string channelId, MemberLevel memberLevel)
    {
        foreach (var condition in Conditions)
        {
            if (!condition.IsMatch(storeId, userId, channelId, memberLevel))
                return false;
        }
        
        if (Targets.Any(t => t.IsMatch(storeId, userId, channelId, memberLevel)))
            return true;
        
        return false;
    }
    
    public decimal GetGrayPercentage(string storeId, string userId, string channelId)
    {
        if (!IsEffectiveAt(DateTime.UtcNow))
            return 0m;
        
        if (IsTargeted(storeId, userId, channelId, MemberLevel.Normal))
            return 100m;
        
        return ScaleConfig.DefaultPercentage;
    }
}

public class GrayScaleConfig
{
    public decimal DefaultPercentage { get; set; } = 0m;
    public GrayScaleType ScaleType { get; set; }
    public int IncrementInterval { get; set; } = 24;
    public decimal IncrementStep { get; set; } = 10m;
    public decimal MaxPercentage { get; set; } = 100m;
}

public enum GrayScaleType
{
    Percentage,
    StoreBased,
    UserBased,
    ChannelBased,
    MemberLevelBased,
    Custom
}

public class GrayTarget
{
    public GrayTargetType TargetType { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public decimal Percentage { get; set; } = 100m;
    public bool IsIncluded { get; set; } = true;
    public int Priority { get; set; }
    
    public bool IsMatch(string storeId, string userId, string channelId, MemberLevel memberLevel)
    {
        return TargetType switch
        {
            GrayTargetType.Store => storeId == TargetId,
            GrayTargetType.User => userId == TargetId,
            GrayTargetType.Channel => channelId == TargetId,
            GrayTargetType.MemberLevel => memberLevel.ToString() == TargetId,
            _ => false
        };
    }
}

public enum GrayTargetType
{
    Store,
    User,
    Channel,
    MemberLevel,
    Region,
    Tag,
    Custom
}

public class GrayCondition
{
    public GrayConditionType ConditionType { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    
    public bool IsMatch(string storeId, string userId, string channelId, MemberLevel memberLevel)
    {
        if (ConditionType != GrayConditionType.TargetMatch)
            return true;
            
        return Operator switch
        {
            "=" => GetFieldValue(Field, storeId, userId, channelId, memberLevel) == Value,
            "!=" => GetFieldValue(Field, storeId, userId, channelId, memberLevel) != Value,
            "in" => Value.Split(',').Contains(GetFieldValue(Field, storeId, userId, channelId, memberLevel)),
            _ => true
        };
    }
    
    private string GetFieldValue(string field, string storeId, string userId, string channelId, MemberLevel memberLevel)
    {
        return field.ToLower() switch
        {
            "store" => storeId,
            "user" => userId,
            "channel" => channelId,
            "level" => memberLevel.ToString(),
            _ => ""
        };
    }
}

public enum GrayConditionType
{
    Always,
    TargetMatch,
    TimeRange,
    AmountRange,
    Custom
}

public enum GrayReleaseStatus
{
    Draft,
    Pending,
    InProgress,
    Paused,
    Completed,
    Cancelled
}

public class GrayStage
{
    public int StageNumber { get; set; }
    public string StageName { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public GrayStageStatus Status { get; set; }
    public List<GrayStageMetrics> Metrics { get; set; } = new();
}

public enum GrayStageStatus
{
    Pending,
    Executing,
    Completed,
    RolledBack
}

public class GrayStageMetrics
{
    public int TotalOrders { get; set; }
    public int AffectedOrders { get; set; }
    public decimal AverageDiscount { get; set; }
    public decimal NewAverageDiscount { get; set; }
    public decimal DiscountDifference { get; set; }
    public decimal ImpactRatio { get; set; }
}

public class GrayReleaseMetrics
{
    public int TotalOrders { get; set; }
    public int GrayOrders { get; set; }
    public int ControlOrders { get; set; }
    public decimal ControlAverageDiscount { get; set; }
    public decimal GrayAverageDiscount { get; set; }
    public decimal DiscountDifference { get; set; }
    public int OrdersWithIncreasedDiscount { get; set; }
    public int OrdersWithDecreasedDiscount { get; set; }
    public int OrdersWithNoChange { get; set; }
}

public class GrayPlaybackResult
{
    public string PlaybackId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyId { get; set; } = string.Empty;
    public DateTime PlaybackTime { get; set; }
    
    public int TotalOrders { get; set; }
    public List<OrderPlaybackResult> OrderResults { get; set; } = new();
    
    public GrayPlaybackSummary Summary { get; set; } = new();
    
    public List<GrayImpactDetail> ImpactDetails { get; set; } = new();
    
    public string GeneratePlaybackReport()
    {
        var report = new List<string>();
        
        report.Add("╔════════════════════════════════════════════════════════════════╗");
        report.Add("║                灰度发布回放分析报告                            ║");
        report.Add("╚════════════════════════════════════════════════════════════════╝");
        report.Add("");
        report.Add($"回放ID: {PlaybackId}");
        report.Add($"策略名称: {PolicyId}");
        report.Add($"回放时间: {PlaybackTime:yyyy-MM-dd HH:mm:ss}");
        report.Add("");
        
        report.Add("【回放汇总】");
        report.Add($"  总订单数: {TotalOrders}");
        report.Add($"  灰度订单: {Summary.GrayOrders}");
        report.Add($"  对照订单: {Summary.ControlOrders}");
        report.Add($"  平均优惠(对照): ¥{Summary.ControlAverageDiscount:F2}");
        report.Add($"  平均优惠(灰度): ¥{Summary.GrayAverageDiscount:F2}");
        report.Add($"  优惠差异: ¥{Summary.DiscountDifference:F2}");
        report.Add("");
        
        report.Add("【影响分析】");
        report.Add($"  优惠增加订单: {Summary.OrdersWithIncreasedDiscount}");
        report.Add($"  优惠减少订单: {Summary.OrdersWithDecreasedDiscount}");
        report.Add($"  无变化订单: {Summary.OrdersWithNoChange}");
        report.Add($"  最大增加: ¥{Summary.MaxIncreasedDiscount:F2}");
        report.Add($"  最大减少: ¥{Summary.MaxDecreasedDiscount:F2}");
        report.Add("");
        
        if (Summary.TopIncreasedOrders.Any())
        {
            report.Add("【优惠增加 TOP 5】");
            foreach (var order in Summary.TopIncreasedOrders.Take(5))
            {
                report.Add($"  {order.OrderId}: +¥{order.Difference:F2}");
                report.Add($"    原因: {order.Reason}");
            }
            report.Add("");
        }
        
        if (Summary.TopDecreasedOrders.Any())
        {
            report.Add("【优惠减少 TOP 5】");
            foreach (var order in Summary.TopDecreasedOrders.Take(5))
            {
                report.Add($"  {order.OrderId}: -¥{order.Difference:F2}");
                report.Add($"    原因: {order.Reason}");
            }
            report.Add("");
        }
        
        report.Add("【灰度范围对比】");
        foreach (var detail in ImpactDetails)
        {
            report.Add($"  {detail.ScopeName}:");
            report.Add($"    订单数: {detail.OrderCount}");
            report.Add($"    优惠变化: ¥{detail.TotalDiscountDifference:F2}");
            report.Add($"    平均变化: ¥{detail.AverageDifference:F2}");
        }
        
        report.Add("╚════════════════════════════════════════════════════════════════╝");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class OrderPlaybackResult
{
    public string OrderId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public MemberLevel MemberLevel { get; set; }
    
    public decimal OriginalAmount { get; set; }
    public decimal ControlDiscount { get; set; }
    public decimal GrayDiscount { get; set; }
    public decimal Difference { get; set; }
    
    public bool IsGray { get; set; }
    public bool HasSignificantChange { get; set; }
    
    public List<string> ChangeReasons { get; set; } = new();
    public List<string> AffectedCoupons { get; set; } = new();
}

public class GrayPlaybackSummary
{
    public int GrayOrders { get; set; }
    public int ControlOrders { get; set; }
    public decimal ControlAverageDiscount { get; set; }
    public decimal GrayAverageDiscount { get; set; }
    public decimal DiscountDifference { get; set; }
    
    public int OrdersWithIncreasedDiscount { get; set; }
    public int OrdersWithDecreasedDiscount { get; set; }
    public int OrdersWithNoChange { get; set; }
    
    public decimal MaxIncreasedDiscount { get; set; }
    public decimal MaxDecreasedDiscount { get; set; }
    
    public List<OrderImpactSummary> TopIncreasedOrders { get; set; } = new();
    public List<OrderImpactSummary> TopDecreasedOrders { get; set; } = new();
}

public class OrderImpactSummary
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Difference { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class GrayImpactDetail
{
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalDiscountDifference { get; set; }
    public decimal AverageDifference { get; set; }
    public decimal MaxDifference { get; set; }
    public decimal MinDifference { get; set; }
}

public class BatchPlaybackRequest
{
    public string PolicyId { get; set; } = string.Empty;
    public List<string> OrderIds { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string>? StoreIds { get; set; }
    public List<string>? UserIds { get; set; }
    public List<string>? Channels { get; set; }
    public List<MemberLevel>? MemberLevels { get; set; }
    public int? MaxOrders { get; set; }
}