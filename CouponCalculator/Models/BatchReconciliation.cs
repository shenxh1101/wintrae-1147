namespace CouponCalculator.Models;

public class BatchReconciliationRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public ReconciliationScope Scope { get; set; } = new();
    
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    public List<string> OrderIds { get; set; } = new();
    public List<string>? StoreIds { get; set; }
    public List<string>? UserIds { get; set; }
    
    public List<string>? Campaigns { get; set; }
    
    public ReconciliationType ReconciliationType { get; set; }
    
    public FilterCriteria Filters { get; set; } = new();
    
    public ExportOptions ExportOptions { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class ReconciliationScope
{
    public ScopeType Type { get; set; }
    public List<string> TargetIds { get; set; } = new();
    
    public string GetScopeDescription()
    {
        if (TargetIds.Count == 0)
            return Type switch
            {
                ScopeType.Daily => "全量每日",
                ScopeType.Weekly => "全量每周",
                ScopeType.Monthly => "全量每月",
                ScopeType.Campaign => "全量活动",
                _ => "全量"
            };
        
        return $"{Type}: {string.Join(", ", TargetIds.Take(3))}" + 
               (TargetIds.Count > 3 ? $" 等{TargetIds.Count}个" : "");
    }
}

public enum ScopeType
{
    Daily,
    Weekly,
    Monthly,
    Campaign,
    Custom,
    Full
}

public enum ReconciliationType
{
    TrialVsOrder,
    OrderVsPayment,
    PaymentVsSettlement,
    FullFlow,
    RefundCheck,
    RuleChangeCheck,
    Custom
}

public class FilterCriteria
{
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinDiscount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinDiscountRate { get; set; }
    public decimal? MaxDiscountRate { get; set; }
    
    public List<string>? AffectedByRules { get; set; }
    public List<string>? AffectedByCoupons { get; set; }
    
    public bool? HasAnomaly { get; set; }
    public bool? HasChange { get; set; }
    public bool? HasRefund { get; set; }
    
    public List<string>? MemberLevels { get; set; }
    public List<string>? Channels { get; set; }
    public List<string>? Stores { get; set; }
    
    public int? TopNByDifference { get; set; }
    public decimal? TopNPercentile { get; set; }
}

public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public List<ExportColumn> Columns { get; set; } = new();
    public bool IncludeSummary { get; set; } = true;
    public bool IncludeDetails { get; set; } = true;
    public bool GroupBy { get; set; } = false;
    public string? GroupByField { get; set; }
}

public enum ExportFormat
{
    Csv,
    Excel,
    Json,
    Html,
    Pdf
}

public class ExportColumn
{
    public string Field { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Width { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Format { get; set; }
}

public class BatchReconciliationResult
{
    public string ResultId { get; set; } = Guid.NewGuid().ToString();
    public string RequestId { get; set; } = string.Empty;
    
    public DateTime ProcessedAt { get; set; }
    public int TotalOrders { get; set; }
    public int ProcessedOrders { get; set; }
    public int FailedOrders { get; set; }
    
    public ReconciliationSummary Summary { get; set; } = new();
    public List<OrderReconciliationDetail> Details { get; set; } = new();
    
    public List<AnomalyOrder> Anomalies { get; set; } = new();
    
    public GroupedReconciliationResult GroupedResult { get; set; } = new();
    
    public BatchExportData ExportData { get; set; } = new();
    
    public string GenerateSummaryReport()
    {
        var report = new List<string>();
        
        report.Add("╔══════════════════════════════════════════════════════════════════════╗");
        report.Add("║              批量对账汇总报告                                         ║");
        report.Add("╚══════════════════════════════════════════════════════════════════════╝");
        report.Add("");
        report.Add($"对账批次: {ResultId}");
        report.Add($"处理时间: {ProcessedAt:yyyy-MM-dd HH:mm:ss}");
        report.Add($"总订单数: {TotalOrders}");
        report.Add($"处理成功: {ProcessedOrders}");
        report.Add($"处理失败: {FailedOrders}");
        report.Add("");
        
        report.Add("【汇总统计】");
        report.Add($"  原始总额: ¥{Summary.OriginalTotal:F2}");
        report.Add($"  优惠总额: ¥{Summary.DiscountTotal:F2}");
        report.Add($"  应付总额: ¥{Summary.FinalTotal:F2}");
        report.Add($"  平均优惠率: {Summary.AverageDiscountRate:P2}");
        report.Add("");
        
        report.Add("【差异统计】");
        report.Add($"  有差异订单: {Summary.OrdersWithDifference}");
        report.Add($"  差异总额: ¥{Summary.TotalDifference:F2}");
        report.Add($"  平均差异: ¥{Summary.AverageDifference:F2}");
        report.Add($"  最大差异: ¥{Summary.MaxDifference:F2}");
        report.Add($"  最小差异: ¥{Summary.MinDifference:F2}");
        report.Add("");
        
        if (Anomalies.Any())
        {
            report.Add("【异常订单】");
            report.Add($"  异常数量: {Anomalies.Count}");
            foreach (var anomaly in Anomalies.Take(5))
            {
                report.Add($"  · {anomaly.OrderId}: {anomaly.AnomalyType} - {anomaly.Description}");
            }
            if (Anomalies.Count > 5)
                report.Add($"  ... 还有 {Anomalies.Count - 5} 个异常");
            report.Add("");
        }
        
        if (GroupedResult.Groups.Any())
        {
            report.Add("【分组统计】");
            foreach (var group in GroupedResult.Groups.Take(10))
            {
                report.Add($"  {group.GroupName}:");
                report.Add($"    订单数: {group.OrderCount}");
                report.Add($"    差异总额: ¥{group.TotalDifference:F2}");
                report.Add($"    异常数: {group.AnomalyCount}");
            }
            report.Add("");
        }
        
        report.Add("【TOP 10 差异订单】");
        var topDiff = Summary.TopDifferenceOrders.Take(10);
        foreach (var order in topDiff)
        {
            report.Add($"  {order.OrderId}: ¥{order.Difference:F2} ({order.DifferencePercent:P2})");
        }
        
        report.Add("");
        report.Add("══════════════════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
    
    public string GenerateExportContent()
    {
        var lines = new List<string>();
        
        if (ExportData.Headers.Any())
        {
            lines.Add(string.Join(",", ExportData.Headers));
        }
        
        foreach (var row in ExportData.Rows)
        {
            lines.Add(string.Join(",", row.Select(v => $"\"{v}\"")));
        }
        
        return string.Join(Environment.NewLine, lines);
    }
}

public class ReconciliationSummary
{
    public int TotalOrders { get; set; }
    public int OrdersWithDifference { get; set; }
    public int OrdersWithAnomaly { get; set; }
    
    public decimal OriginalTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal FinalTotal { get; set; }
    public decimal AverageDiscountRate { get; set; }
    
    public decimal TotalDifference { get; set; }
    public decimal AverageDifference { get; set; }
    public decimal MaxDifference { get; set; }
    public decimal MinDifference { get; set; }
    
    public int IncreasedDiscountOrders { get; set; }
    public int DecreasedDiscountOrders { get; set; }
    public int NoChangeOrders { get; set; }
    
    public List<TopDifferenceOrder> TopDifferenceOrders { get; set; } = new();
    
    public Dictionary<string, int> DifferenceDistribution { get; set; } = new();
    public Dictionary<string, decimal> DifferenceByChannel { get; set; } = new();
    public Dictionary<string, decimal> DifferenceByStore { get; set; } = new();
    public Dictionary<string, decimal> DifferenceByMemberLevel { get; set; } = new();
}

public class TopDifferenceOrder
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Difference { get; set; }
    public decimal DifferencePercent { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class OrderReconciliationDetail
{
    public string OrderId { get; set; } = string.Empty;
    
    public decimal OriginalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    
    public decimal Stage1Amount { get; set; }
    public decimal Stage2Amount { get; set; }
    public decimal Difference { get; set; }
    public bool HasDifference { get; set; }
    
    public List<string> AppliedCoupons { get; set; } = new();
    public List<string> ChangeReasons { get; set; } = new();
    
    public bool IsAnomaly { get; set; }
    public string? AnomalyType { get; set; }
    public string? AnomalyDescription { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public string? StoreId { get; set; }
    public string? UserId { get; set; }
    public string? Channel { get; set; }
    public string? MemberLevel { get; set; }
    public string? CampaignId { get; set; }
}

public class AnomalyOrder
{
    public string OrderId { get; set; } = string.Empty;
    public string AnomalyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ImpactAmount { get; set; }
    public string Severity { get; set; } = string.Empty;
    public List<string> RelatedRules { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class GroupedReconciliationResult
{
    public List<ReconciliationGroup> Groups { get; set; } = new();
}

public class ReconciliationGroup
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupByField { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalDifference { get; set; }
    public decimal AverageDifference { get; set; }
    public int AnomalyCount { get; set; }
    public List<OrderReconciliationDetail> Orders { get; set; } = new();
}

public class BatchExportData
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public class CampaignReconciliation
{
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public DateTime CampaignStartDate { get; set; }
    public DateTime CampaignEndDate { get; set; }
    
    public int TotalOrders { get; set; }
    public decimal TotalOriginalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal AverageDiscountRate { get; set; }
    
    public List<RuleEffectiveness> RuleEffects { get; set; } = new();
    public List<CouponEffectiveness> CouponEffects { get; set; } = new();
    
    public decimal Budget { get; set; }
    public decimal ActualCost { get; set; }
    public decimal BudgetUsageRate { get; set; }
    
    public List<string> TopProducts { get; set; } = new();
    public List<string> TopChannels { get; set; } = new();
    
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
    
    public string GenerateCampaignReport()
    {
        var report = new List<string>();
        
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add($"          活动对账报告: {CampaignName}");
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add($"活动ID: {CampaignId}");
        report.Add($"活动时间: {CampaignStartDate:yyyy-MM-dd} 至 {CampaignEndDate:yyyy-MM-dd}");
        report.Add("");
        
        report.Add("【活动汇总】");
        report.Add($"  总订单数: {TotalOrders}");
        report.Add($"  原始总额: ¥{TotalOriginalAmount:F2}");
        report.Add($"  优惠总额: ¥{TotalDiscount:F2}");
        report.Add($"  平均优惠率: {AverageDiscountRate:P2}");
        report.Add("");
        
        report.Add("【预算使用】");
        report.Add($"  活动预算: ¥{Budget:F2}");
        report.Add($"  实际消耗: ¥{ActualCost:F2}");
        report.Add($"  预算使用率: {BudgetUsageRate:P2}");
        report.Add("");
        
        if (RuleEffects.Any())
        {
            report.Add("【规则效果】");
            foreach (var effect in RuleEffects.Take(5))
            {
                report.Add($"  {effect.RuleName}:");
                report.Add($"    应用次数: {effect.UsageCount}");
                report.Add($"    总优惠: ¥{effect.TotalDiscount:F2}");
                report.Add($"    平均优惠: ¥{effect.AverageDiscount:F2}");
            }
            report.Add("");
        }
        
        if (CouponEffects.Any())
        {
            report.Add("【优惠券效果 TOP 10】");
            foreach (var effect in CouponEffects.Take(10))
            {
                report.Add($"  {effect.CouponName}: {effect.UsageCount}次, ¥{effect.TotalDiscount:F2}");
            }
            report.Add("");
        }
        
        report.Add("═══════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class RuleEffectiveness
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal AverageDiscount { get; set; }
    public decimal ContributionRate { get; set; }
}

public class CouponEffectiveness
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal AverageDiscount { get; set; }
}