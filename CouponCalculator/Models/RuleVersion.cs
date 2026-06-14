namespace CouponCalculator.Models;

public class RuleVersion
{
    public string VersionId { get; set; } = string.Empty;
    public string VersionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public string FullVersion => $"{MajorVersion}.{MinorVersion}";
    
    public DateTime CreatedAt { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    public VersionStatus Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    public List<Coupon> Coupons { get; set; } = new();
    public List<DiscountRule> Rules { get; set; } = new();
    
    public Dictionary<string, string> ChangeLog { get; set; } = new();
    public List<string> AffectedScopes { get; set; } = new();
    
    public bool IsEffectiveAt(DateTime time)
    {
        if (Status != VersionStatus.Active)
            return false;
            
        if (EffectiveFrom.HasValue && time < EffectiveFrom.Value)
            return false;
            
        if (EffectiveTo.HasValue && time > EffectiveTo.Value)
            return false;
            
        return true;
    }
    
    public string GetStatusDescription()
    {
        return Status switch
        {
            VersionStatus.Draft => "草稿状态，未生效",
            VersionStatus.Pending => "待生效，已安排定时切换",
            VersionStatus.Active => "已生效，正在使用",
            VersionStatus.Deprecated => "已废弃，不再使用",
            VersionStatus.Archived => "已归档，历史版本",
            _ => Status.ToString()
        };
    }
}

public enum VersionStatus
{
    Draft,
    Pending,
    Active,
    Deprecated,
    Archived
}

public class RuleVersionPlan
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string FromVersionId { get; set; } = string.Empty;
    public string ToVersionId { get; set; } = string.Empty;
    
    public DateTime ScheduledTime { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public PlanStatus Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    public bool RequireApproval { get; set; }
    public List<string> ApprovedBy { get; set; } = new();
    
    public VersionDiffPreview Preview { get; set; } = new();
    
    public Dictionary<string, string> ExecutionConfig { get; set; } = new();
    
    public List<string> Notifications { get; set; } = new();
    
    public string GetStatusDescription()
    {
        return Status switch
        {
            PlanStatus.Created => "已创建，待审核",
            PlanStatus.Approved => "已审核，待执行",
            PlanStatus.Executing => "正在执行",
            PlanStatus.Completed => "已完成",
            PlanStatus.Cancelled => "已取消",
            PlanStatus.Failed => "执行失败",
            _ => Status.ToString()
        };
    }
}

public enum PlanStatus
{
    Created,
    Approved,
    Executing,
    Completed,
    Cancelled,
    Failed
}

public class VersionDiffPreview
{
    public List<RuleChange> AddedRules { get; set; } = new();
    public List<RuleChange> RemovedRules { get; set; } = new();
    public List<RuleChange> ModifiedRules { get; set; } = new();
    
    public List<CouponChange> AddedCoupons { get; set; } = new();
    public List<CouponChange> RemovedCoupons { get; set; } = new();
    public List<CouponChange> ModifiedCoupons { get; set; } = new();
    
    public ImpactAnalysis ImpactAnalysis { get; set; } = new();
    
    public string GenerateDiffReport()
    {
        var report = new List<string>();
        
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add("              规则版本变更预览报告                           ");
        report.Add("═══════════════════════════════════════════════════════════");
        
        if (AddedRules.Any())
        {
            report.Add("");
            report.Add("【新增规则】");
            foreach (var rule in AddedRules)
            {
                report.Add($"  + {rule.RuleName} ({rule.RuleId})");
                report.Add($"    类型: {rule.Type}, 优惠: {rule.DiscountValue}");
            }
        }
        
        if (RemovedRules.Any())
        {
            report.Add("");
            report.Add("【删除规则】");
            foreach (var rule in RemovedRules)
            {
                report.Add($"  - {rule.RuleName} ({rule.RuleId})");
            }
        }
        
        if (ModifiedRules.Any())
        {
            report.Add("");
            report.Add("【修改规则】");
            foreach (var rule in ModifiedRules)
            {
                report.Add($"  * {rule.RuleName} ({rule.RuleId})");
                report.Add($"    变化: {rule.ChangeDescription}");
            }
        }
        
        if (AddedCoupons.Any())
        {
            report.Add("");
            report.Add("【新增优惠券】");
            foreach (var coupon in AddedCoupons)
            {
                report.Add($"  + {coupon.CouponName} ({coupon.CouponCode})");
            }
        }
        
        if (RemovedCoupons.Any())
        {
            report.Add("");
            report.Add("【删除优惠券】");
            foreach (var coupon in RemovedCoupons)
            {
                report.Add($"  - {coupon.CouponName} ({coupon.CouponCode})");
            }
        }
        
        if (ModifiedCoupons.Any())
        {
            report.Add("");
            report.Add("【修改优惠券】");
            foreach (var coupon in ModifiedCoupons)
            {
                report.Add($"  * {coupon.CouponName} ({coupon.CouponCode})");
                report.Add($"    变化: {coupon.ChangeDescription}");
            }
        }
        
        report.Add("");
        report.Add("【影响分析】");
        report.Add($"  预估影响订单数: {ImpactAnalysis.EstimatedAffectedOrders}");
        report.Add($"  预估优惠变化: ¥{ImpactAnalysis.EstimatedDiscountChange:F2}");
        report.Add($"  风险等级: {ImpactAnalysis.RiskLevel}");
        
        if (ImpactAnalysis.Warnings.Any())
        {
            report.Add("");
            report.Add("【风险提示】");
            foreach (var warning in ImpactAnalysis.Warnings)
            {
                report.Add($"  ⚠ {warning}");
            }
        }
        
        report.Add("═══════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class RuleChange
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public DiscountRuleType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public string ChangeDescription { get; set; } = string.Empty;
    public Dictionary<string, object> OldValues { get; set; } = new();
    public Dictionary<string, object> NewValues { get; set; } = new();
}

public class CouponChange
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public Dictionary<string, object> OldValues { get; set; } = new();
    public Dictionary<string, object> NewValues { get; set; } = new();
}

public class ImpactAnalysis
{
    public int EstimatedAffectedOrders { get; set; }
    public decimal EstimatedDiscountChange { get; set; }
    public string RiskLevel { get; set; } = "低";
    public List<string> AffectedCategories { get; set; } = new();
    public List<string> AffectedUserGroups { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    
    public List<SampleImpactCase> SampleCases { get; set; } = new();
}

public class SampleImpactCase
{
    public string OrderId { get; set; } = string.Empty;
    public decimal OldDiscount { get; set; }
    public decimal NewDiscount { get; set; }
    public decimal Difference { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
}