namespace CouponCalculator.Models;

public class CalculationCheckpoint
{
    public string CheckpointId { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    
    public CalculationStage Stage { get; set; }
    public string StageDescription { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string OperatorRole { get; set; } = string.Empty;
    
    public CalculationResult Result { get; set; } = new();
    public EnhancedCalculationResult EnhancedResult { get; set; } = new();
    public ItemLevelDiscountBreakdown ItemBreakdown { get; set; } = new();
    
    public OrderContextSnapshot ContextSnapshot { get; set; } = new();
    public List<CouponSnapshot> CouponSnapshots { get; set; } = new();
    
    public string RuleVersionId { get; set; } = string.Empty;
    public string RuleVersionName { get; set; } = string.Empty;
    
    public List<string> AppliedRuleIds { get; set; } = new();
    public List<string> AppliedCouponIds { get; set; } = new();
    
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    
    public string GetStageDescription()
    {
        return Stage switch
        {
            CalculationStage.Trial => "试算阶段",
            CalculationStage.OrderCreated => "下单阶段",
            CalculationStage.PaymentPending => "支付前复算",
            CalculationStage.PaymentCompleted => "支付完成",
            CalculationStage.Refund => "退款重算",
            CalculationStage.Adjustment => "售后调整",
            _ => Stage.ToString()
        };
    }
}

public enum CalculationStage
{
    Trial,
    OrderCreated,
    PaymentPending,
    PaymentCompleted,
    Refund,
    Adjustment
}

public class OrderContextSnapshot
{
    public decimal OriginalAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public int ItemCount { get; set; }
    public int TotalQuantity { get; set; }
    
    public List<ItemSnapshot> Items { get; set; } = new();
    public MemberSnapshot? Member { get; set; }
    public ShippingSnapshot? Shipping { get; set; }
    
    public Dictionary<string, string> ExtendedData { get; set; } = new();
}

public class ItemSnapshot
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string CategoryId { get; set; } = string.Empty;
}

public class MemberSnapshot
{
    public string MemberId { get; set; } = string.Empty;
    public MemberLevel Level { get; set; }
    public int Points { get; set; }
}

public class ShippingSnapshot
{
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal CalculatedFreight { get; set; }
}

public class CouponSnapshot
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CalculationComparison
{
    public string ComparisonId { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    
    public string FromCheckpointId { get; set; } = string.Empty;
    public string ToCheckpointId { get; set; } = string.Empty;
    
    public CalculationStage FromStage { get; set; }
    public CalculationStage ToStage { get; set; }
    
    public DateTime ComparisonTime { get; set; }
    
    public List<ResultDifference> Differences { get; set; } = new();
    
    public bool HasChanges { get; set; }
    public decimal AmountDifference { get; set; }
    public decimal DiscountDifference { get; set; }
    
    public List<string> ChangeReasons { get; set; } = new();
    public List<string> TriggeredRules { get; set; } = new();
    
    public ImpactLevel ImpactLevel { get; set; }
    public List<string> ImpactWarnings { get; set; } = new();
    
    public string GenerateComparisonReport()
    {
        var report = new List<string>();
        
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add("              计算结果对比报告                                ");
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add($"订单号: {OrderId}");
        report.Add($"对比时间: {ComparisonTime:yyyy-MM-dd HH:mm:ss}");
        report.Add($"对比阶段: {GetStageDescription(FromStage)} → {GetStageDescription(ToStage)}");
        report.Add("");
        
        if (!HasChanges)
        {
            report.Add("【对比结果】");
            report.Add("  ✓ 无变化，计算结果一致");
            report.Add("");
        }
        else
        {
            report.Add("【变化汇总】");
            report.Add($"  金额变化: ¥{AmountDifference:F2}");
            report.Add($"  优惠变化: ¥{DiscountDifference:F2}");
            report.Add($"  影响等级: {ImpactLevel}");
            report.Add("");
            
            report.Add("【差异明细】");
            foreach (var diff in Differences)
            {
                report.Add($"  {diff.FieldName}");
                report.Add($"    原值: {diff.OldValue}");
                report.Add($"    新值: {diff.NewValue}");
                report.Add($"    变化: {diff.ChangeDescription}");
                report.Add($"    原因: {diff.ChangeReason}");
                report.Add("");
            }
            
            if (ChangeReasons.Any())
            {
                report.Add("【变化原因】");
                foreach (var reason in ChangeReasons)
                {
                    report.Add($"  · {reason}");
                }
                report.Add("");
            }
            
            if (TriggeredRules.Any())
            {
                report.Add("【触发规则】");
                foreach (var rule in TriggeredRules)
                {
                    report.Add($"  · {rule}");
                }
                report.Add("");
            }
            
            if (ImpactWarnings.Any())
            {
                report.Add("【风险提示】");
                foreach (var warning in ImpactWarnings)
                {
                    report.Add($"  ⚠ {warning}");
                }
                report.Add("");
            }
        }
        
        report.Add("═══════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
    
    private static string GetStageDescription(CalculationStage stage)
    {
        return stage switch
        {
            CalculationStage.Trial => "试算",
            CalculationStage.OrderCreated => "下单",
            CalculationStage.PaymentPending => "支付前",
            CalculationStage.PaymentCompleted => "支付完成",
            CalculationStage.Refund => "退款",
            CalculationStage.Adjustment => "调整",
            _ => stage.ToString()
        };
    }
}

public class ResultDifference
{
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
    public string TriggeredRule { get; set; } = string.Empty;
    
    public DifferenceType Type { get; set; }
    public DifferenceSeverity Severity { get; set; }
    
    public List<string> RelatedItems { get; set; } = new();
    public List<string> RelatedCoupons { get; set; } = new();
}

public enum DifferenceType
{
    Amount,
    Discount,
    CouponApplied,
    CouponRemoved,
    MemberDiscount,
    Freight,
    RuleChange,
    ContextChange
}

public enum DifferenceSeverity
{
    Minor,
    Moderate,
    Major,
    Critical
}

public enum ImpactLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public class ReconciliationReport
{
    public string ReportId { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    
    public List<CalculationCheckpoint> Checkpoints { get; set; } = new();
    public List<CalculationComparison> Comparisons { get; set; } = new();
    
    public ReconciliationSummary Summary { get; set; } = new();
    
    public List<string> AuditTrail { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    
    public string GenerateFullReport()
    {
        var report = new List<string>();
        
        report.Add("╔══════════════════════════════════════════════════════════════╗");
        report.Add("║              订单优惠对账与排查报告                              ║");
        report.Add("╚══════════════════════════════════════════════════════════════╝");
        report.Add("");
        report.Add($"订单号: {OrderId}");
        report.Add($"报告生成时间: {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        report.Add($"生成人: {GeneratedBy}");
        report.Add("");
        
        report.Add("【计算节点时间线】");
        foreach (var checkpoint in Checkpoints.OrderBy(c => c.Timestamp))
        {
            report.Add($"  {checkpoint.Timestamp:HH:mm:ss} - {checkpoint.GetStageDescription()}");
            report.Add($"    金额: ¥{checkpoint.Result.OriginalAmount:F2} → ¥{checkpoint.Result.FinalAmount:F2}");
            report.Add($"    优惠: ¥{checkpoint.Result.TotalDiscountAmount:F2}");
            report.Add($"    应用券数: {checkpoint.AppliedCouponIds.Count}");
        }
        report.Add("");
        
        if (Comparisons.Any())
        {
            report.Add("【节点间对比】");
            foreach (var comparison in Comparisons)
            {
                var changeMarker = comparison.HasChanges ? "✗ 有变化" : "✓ 无变化";
                report.Add($"  {comparison.FromStage} → {comparison.ToStage}: {changeMarker}");
                
                if (comparison.HasChanges)
                {
                    report.Add($"    金额差: ¥{comparison.AmountDifference:F2}");
                    report.Add($"    优惠差: ¥{comparison.DiscountDifference:F2}");
                    report.Add($"    影响等级: {comparison.ImpactLevel}");
                    
                    foreach (var reason in comparison.ChangeReasons.Take(3))
                    {
                        report.Add($"    · {reason}");
                    }
                }
            }
            report.Add("");
        }
        
        report.Add("【对账汇总】");
        report.Add($"  总计算次数: {Summary.TotalCalculations}");
        report.Add($"  有变化次数: {Summary.ChangedCalculations}");
        report.Add($"  最大金额波动: ¥{Summary.MaxAmountFluctuation:F2}");
        report.Add($"  最大优惠波动: ¥{Summary.MaxDiscountFluctuation:F2}");
        report.Add($"  最终金额: ¥{Summary.FinalAmount:F2}");
        report.Add($"  最终优惠: ¥{Summary.FinalDiscount:F2}");
        report.Add($"  一致性状态: {Summary.ConsistencyStatus}");
        report.Add("");
        
        if (AuditTrail.Any())
        {
            report.Add("【审计轨迹】");
            foreach (var trail in AuditTrail.Take(10))
            {
                report.Add($"  · {trail}");
            }
            report.Add("");
        }
        
        if (Recommendations.Any())
        {
            report.Add("【建议】");
            foreach (var rec in Recommendations)
            {
                report.Add($"  → {rec}");
            }
            report.Add("");
        }
        
        if (Summary.ConsistencyStatus != "一致")
        {
            report.Add("【⚠ 警告】");
            report.Add($"  计算结果存在不一致，建议人工核查");
            report.Add($"  不一致原因: {Summary.InconsistencyReason}");
        }
        
        report.Add("══════════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class ReconciliationSummary
{
    public int TotalCalculations { get; set; }
    public int ChangedCalculations { get; set; }
    
    public decimal MaxAmountFluctuation { get; set; }
    public decimal MaxDiscountFluctuation { get; set; }
    
    public decimal FinalAmount { get; set; }
    public decimal FinalDiscount { get; set; }
    
    public string ConsistencyStatus { get; set; } = "一致";
    public string InconsistencyReason { get; set; } = string.Empty;
    
    public List<string> AllAppliedCoupons { get; set; } = new();
    public List<string> AllTriggeredRules { get; set; } = new();
}