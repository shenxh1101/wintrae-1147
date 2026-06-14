namespace CouponCalculator.Models;

public class GrayRiskAssessment
{
    public string AssessmentId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyId { get; set; } = string.Empty;
    public DateTime AssessmentTime { get; set; }
    
    public RiskLevel OverallRiskLevel { get; set; }
    public decimal RiskScore { get; set; }
    
    public List<RiskFactor> RiskFactors { get; set; } = new();
    
    public RecommendedScaleRange RecommendedScale { get; set; } = new();
    public RollbackPlan RollbackPlan { get; set; } = new();
    
    public List<RiskyOrder> HighRiskOrders { get; set; } = new();
    public List<RiskyOrder> MediumRiskOrders { get; set; } = new();
    
    public Dictionary<string, RiskByScope> RiskByStore { get; set; } = new();
    public Dictionary<string, RiskByScope> RiskByChannel { get; set; } = new();
    public Dictionary<string, RiskByScope> RiskByMemberLevel { get; set; } = new();
    
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    
    public string GenerateRiskReport()
    {
        var report = new List<string>();
        
        report.Add("╔══════════════════════════════════════════════════════════════════════╗");
        report.Add("║              灰度发布风险评估报告                                    ║");
        report.Add("╚══════════════════════════════════════════════════════════════════════╝");
        report.Add($"评估ID: {AssessmentId}");
        report.Add($"策略ID: {PolicyId}");
        report.Add($"评估时间: {AssessmentTime:yyyy-MM-dd HH:mm:ss}");
        report.Add($"整体风险等级: {OverallRiskLevel}");
        report.Add($"风险评分: {RiskScore:F1}/100");
        report.Add("");
        
        report.Add("【风险因素分析】");
        foreach (var factor in RiskFactors)
        {
            var icon = factor.Level >= RiskLevel.High ? "⚠" : "○";
            report.Add($"  {icon} {factor.Name}: {factor.Description}");
            report.Add($"     影响程度: {factor.ImpactLevel}, 影响订单数: {factor.AffectedOrders}");
        }
        report.Add("");
        
        report.Add("【建议放量范围】");
        report.Add($"  推荐起始放量: {RecommendedScale.RecommendedStartPercentage}%");
        report.Add($"  最大安全放量: {RecommendedScale.MaxSafePercentage}%");
        report.Add($"  建议放量步长: {RecommendedScale.RecommendedStep}%");
        report.Add($"  建议观察周期: {RecommendedScale.RecommendedObservationHours}小时");
        if (!string.IsNullOrEmpty(RecommendedScale.Reason))
            report.Add($"  原因: {RecommendedScale.Reason}");
        report.Add("");
        
        report.Add("【回滚预案】");
        report.Add($"  回滚触发条件:");
        foreach (var trigger in RollbackPlan.TriggerConditions)
        {
            report.Add($"    - {trigger}");
        }
        report.Add($"  回滚操作步骤:");
        foreach (var step in RollbackPlan.RollbackSteps)
        {
            report.Add($"    {step.StepNumber}. {step.Description}");
        }
        report.Add($"  预估回滚耗时: {RollbackPlan.EstimatedRollbackTime}分钟");
        report.Add($"  回滚影响范围: {RollbackPlan.RollbackScope}");
        report.Add("");
        
        if (HighRiskOrders.Any())
        {
            report.Add($"【高风险订单】共 {HighRiskOrders.Count} 个");
            report.Add("  这些订单在正式切换时最可能出现问题，建议重点关注:");
            foreach (var order in HighRiskOrders.Take(10))
            {
                report.Add($"  ⚠ {order.OrderId}");
                report.Add($"     风险类型: {order.RiskType}");
                report.Add($"     风险描述: {order.RiskDescription}");
                report.Add($"     预估影响: ¥{order.EstimatedImpact:F2}");
                report.Add($"     建议: {order.SuggestedAction}");
            }
            if (HighRiskOrders.Count > 10)
                report.Add($"  ... 还有 {HighRiskOrders.Count - 10} 个高风险订单");
            report.Add("");
        }
        
        if (MediumRiskOrders.Any())
        {
            report.Add($"【中风险订单】共 {MediumRiskOrders.Count} 个");
            foreach (var order in MediumRiskOrders.Take(5))
            {
                report.Add($"  ○ {order.OrderId}: {order.RiskType} - ¥{order.EstimatedImpact:F2}");
            }
            report.Add("");
        }
        
        if (RiskByStore.Any())
        {
            report.Add("【按店铺风险分布】");
            foreach (var kvp in RiskByStore.OrderByDescending(x => x.Value.RiskScore).Take(5))
            {
                report.Add($"  {kvp.Key}: 风险评分 {kvp.Value.RiskScore:F1}, 订单数 {kvp.Value.OrderCount}");
            }
            report.Add("");
        }
        
        if (Warnings.Any())
        {
            report.Add("【风险警告】");
            foreach (var warning in Warnings)
            {
                report.Add($"  ⚠ {warning}");
            }
            report.Add("");
        }
        
        if (Recommendations.Any())
        {
            report.Add("【发布建议】");
            foreach (var rec in Recommendations)
            {
                report.Add($"  ✓ {rec}");
            }
            report.Add("");
        }
        
        report.Add("╚══════════════════════════════════════════════════════════════════════╝");
        
        return string.Join(Environment.NewLine, report);
    }
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public class RiskFactor
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel Level { get; set; }
    public int AffectedOrders { get; set; }
    public decimal ImpactAmount { get; set; }
    public string ImpactLevel { get; set; } = string.Empty;
    public List<string> AffectedScope { get; set; } = new();
}

public class RecommendedScaleRange
{
    public decimal RecommendedStartPercentage { get; set; } = 5m;
    public decimal MaxSafePercentage { get; set; } = 50m;
    public decimal RecommendedStep { get; set; } = 5m;
    public int RecommendedObservationHours { get; set; } = 24;
    public string Reason { get; set; } = string.Empty;
    
    public List<ScaleStage> ScaleStages { get; set; } = new();
    
    public string GenerateScalePlan()
    {
        var plan = new List<string>();
        plan.Add("建议放量计划:");
        foreach (var stage in ScaleStages)
        {
            plan.Add($"  第{stage.StageNumber}阶段: {stage.Percentage}% - 观察{stage.ObservationHours}小时");
            if (!string.IsNullOrEmpty(stage.Checkpoint))
                plan.Add($"    检查点: {stage.Checkpoint}");
        }
        return string.Join(Environment.NewLine, plan);
    }
}

public class ScaleStage
{
    public int StageNumber { get; set; }
    public decimal Percentage { get; set; }
    public int ObservationHours { get; set; }
    public string Checkpoint { get; set; } = string.Empty;
    public List<string> SuccessCriteria { get; set; } = new();
    public List<string> RollbackTriggers { get; set; } = new();
}

public class RollbackPlan
{
    public string PlanId { get; set; } = Guid.NewGuid().ToString();
    public string PlanName { get; set; } = string.Empty;
    
    public List<string> TriggerConditions { get; set; } = new();
    public List<RollbackStep> RollbackSteps { get; set; } = new();
    
    public int EstimatedRollbackTime { get; set; } = 5;
    public string RollbackScope { get; set; } = string.Empty;
    public List<string> AffectedSystems { get; set; } = new();
    
    public List<string> PreRollbackChecks { get; set; } = new();
    public List<string> PostRollbackVerifications { get; set; } = new();
    
    public string EmergencyContact { get; set; } = string.Empty;
    public string RollbackScript { get; set; } = string.Empty;
    
    public string GenerateRollbackGuide()
    {
        var guide = new List<string>();
        
        guide.Add("╔══════════════════════════════════════════════════════════════╗");
        guide.Add("║              回滚操作指南                                    ║");
        guide.Add("╚══════════════════════════════════════════════════════════════╝");
        guide.Add("");
        
        guide.Add("【触发条件】");
        foreach (var trigger in TriggerConditions)
        {
            guide.Add($"  - {trigger}");
        }
        guide.Add("");
        
        guide.Add("【回滚前检查】");
        foreach (var check in PreRollbackChecks)
        {
            guide.Add($"  ✓ {check}");
        }
        guide.Add("");
        
        guide.Add("【回滚步骤】");
        foreach (var step in RollbackSteps)
        {
            guide.Add($"  {step.StepNumber}. {step.Description}");
            if (!string.IsNullOrEmpty(step.Command))
                guide.Add($"     执行: {step.Command}");
            if (!string.IsNullOrEmpty(step.ExpectedResult))
                guide.Add($"     预期结果: {step.ExpectedResult}");
        }
        guide.Add("");
        
        guide.Add($"【预估耗时】{EstimatedRollbackTime}分钟");
        guide.Add($"【影响范围】{RollbackScope}");
        guide.Add("");
        
        guide.Add("【回滚后验证】");
        foreach (var verification in PostRollbackVerifications)
        {
            guide.Add($"  ✓ {verification}");
        }
        guide.Add("");
        
        if (!string.IsNullOrEmpty(EmergencyContact))
            guide.Add($"【紧急联系】{EmergencyContact}");
        
        guide.Add("╚══════════════════════════════════════════════════════════════╝");
        
        return string.Join(Environment.NewLine, guide);
    }
}

public class RollbackStep
{
    public int StepNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
    public bool IsCritical { get; set; }
}

public class RiskyOrder
{
    public string OrderId { get; set; } = string.Empty;
    public string RiskType { get; set; } = string.Empty;
    public string RiskDescription { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public decimal EstimatedImpact { get; set; }
    public string SuggestedAction { get; set; } = string.Empty;
    
    public string StoreId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string MemberLevel { get; set; } = string.Empty;
    
    public decimal OldDiscount { get; set; }
    public decimal NewDiscount { get; set; }
    public decimal Difference { get; set; }
    
    public List<string> RiskReasons { get; set; } = new();
    public List<string> AffectedCoupons { get; set; } = new();
    
    public string GenerateOrderRiskDetail()
    {
        var detail = new List<string>();
        detail.Add($"订单 {OrderId} 风险分析:");
        detail.Add($"  风险类型: {RiskType}");
        detail.Add($"  风险等级: {RiskLevel}");
        detail.Add($"  风险描述: {RiskDescription}");
        detail.Add($"  预估影响: ¥{EstimatedImpact:F2}");
        detail.Add($"  原优惠: ¥{OldDiscount:F2} → 新优惠: ¥{NewDiscount:F2}");
        detail.Add($"  差异: ¥{Difference:F2}");
        detail.Add($"  建议: {SuggestedAction}");
        return string.Join(Environment.NewLine, detail);
    }
}

public class RiskByScope
{
    public string ScopeId { get; set; } = string.Empty;
    public string ScopeName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal TotalDifference { get; set; }
    public decimal AverageDifference { get; set; }
    public decimal MaxDifference { get; set; }
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
}

public class GrayReleaseDecision
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyId { get; set; } = string.Empty;
    public DateTime DecisionTime { get; set; }
    
    public DecisionType Type { get; set; }
    public DecisionResult Result { get; set; }
    
    public string Reason { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    
    public decimal ApprovedPercentage { get; set; }
    public List<string> Conditions { get; set; } = new();
    
    public RiskAssessmentSummary RiskSummary { get; set; } = new();
}

public enum DecisionType
{
    Proceed,
    Pause,
    Rollback,
    AdjustScale,
    Cancel
}

public enum DecisionResult
{
    Approved,
    Rejected,
    Pending,
    Escalated
}

public class RiskAssessmentSummary
{
    public RiskLevel OverallLevel { get; set; }
    public decimal RiskScore { get; set; }
    public int HighRiskOrderCount { get; set; }
    public decimal MaxPotentialImpact { get; set; }
}

public class GrayReleaseMonitoring
{
    public string MonitoringId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyId { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    public decimal CurrentPercentage { get; set; }
    
    public List<MonitoringMetric> Metrics { get; set; } = new();
    public List<MonitoringAlert> Alerts { get; set; } = new();
    
    public MonitoringStatus Status { get; set; }
    
    public decimal TotalOrdersProcessed { get; set; }
    public decimal TotalDiscountDifference { get; set; }
    public decimal AverageDifference { get; set; }
    
    public string GenerateMonitoringReport()
    {
        var report = new List<string>();
        
        report.Add("╔══════════════════════════════════════════════════════════════╗");
        report.Add("║              灰度发布监控报告                                ║");
        report.Add("╚══════════════════════════════════════════════════════════════╝");
        report.Add($"监控ID: {MonitoringId}");
        report.Add($"策略ID: {PolicyId}");
        report.Add($"当前放量: {CurrentPercentage}%");
        report.Add($"状态: {Status}");
        report.Add($"处理订单数: {TotalOrdersProcessed}");
        report.Add($"累计差异: ¥{TotalDiscountDifference:F2}");
        report.Add($"平均差异: ¥{AverageDifference:F2}");
        report.Add("");
        
        if (Alerts.Any())
        {
            report.Add($"【告警】共 {Alerts.Count} 个");
            foreach (var alert in Alerts.OrderByDescending(a => a.Severity))
            {
                var icon = alert.Severity == AlertSeverity.Critical ? "⚠⚠" : 
                           alert.Severity == AlertSeverity.High ? "⚠" : "○";
                report.Add($"  {icon} [{alert.Timestamp:HH:mm:ss}] {alert.Message}");
            }
            report.Add("");
        }
        
        report.Add("【监控指标】");
        foreach (var metric in Metrics)
        {
            var status = metric.IsNormal ? "正常" : "异常";
            report.Add($"  {metric.Name}: {metric.Value:F2} ({status})");
            if (!string.IsNullOrEmpty(metric.Threshold))
                report.Add($"    阈值: {metric.Threshold}");
        }
        
        report.Add("╚══════════════════════════════════════════════════════════════╝");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class MonitoringMetric
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal ThresholdValue { get; set; }
    public string Threshold { get; set; } = string.Empty;
    public bool IsNormal { get; set; }
    public string Trend { get; set; } = string.Empty;
}

public class MonitoringAlert
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public decimal ActualValue { get; set; }
    public decimal ThresholdValue { get; set; }
    public string SuggestedAction { get; set; } = string.Empty;
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum MonitoringStatus
{
    Normal,
    Warning,
    Critical,
    Paused,
    RolledBack
}