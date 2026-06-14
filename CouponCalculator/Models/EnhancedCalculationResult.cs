namespace CouponCalculator.Models;

public class EnhancedCalculationResult
{
    public string OrderId { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal OriginalFreight { get; set; }
    public decimal OriginalTotal => OriginalAmount + OriginalFreight;
    
    public List<CandidatePlan> CandidatePlans { get; set; } = new();
    public CandidatePlan? RecommendedPlan { get; set; }
    
    public PlanComparison Comparison { get; set; } = new();
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public CalculationMetadata Metadata { get; set; } = new();
}

public class CandidatePlan
{
    public string PlanId { get; set; } = Guid.NewGuid().ToString();
    public string PlanName { get; set; } = string.Empty;
    public string PlanDescription { get; set; } = string.Empty;
    public int Rank { get; set; }
    public bool IsRecommended { get; set; }
    
    public List<AppliedCouponDetail> AppliedCoupons { get; set; } = new();
    public MemberDiscountDetail? MemberDiscount { get; set; }
    public FreightDiscountDetail? FreightDiscount { get; set; }
    
    public decimal ProductDiscountAmount { get; set; }
    public decimal MemberDiscountAmount { get; set; }
    public decimal FreightDiscountAmount { get; set; }
    public decimal TotalDiscountAmount => ProductDiscountAmount + MemberDiscountAmount + FreightDiscountAmount;
    
    public decimal FinalProductAmount => OriginalAmount - ProductDiscountAmount - MemberDiscountAmount;
    public decimal FinalFreightAmount { get; set; }
    public decimal FinalTotalAmount => FinalProductAmount + FinalFreightAmount;
    
    public decimal OriginalAmount { get; set; }
    public decimal OriginalFreight { get; set; }
    
    public List<string> Advantages { get; set; } = new();
    public List<string> Limitations { get; set; } = new();
    
    public PlanSuitability Suitability { get; set; } = new();
}

public class AppliedCouponDetail
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public string TypeName => GetTypeName();
    
    public decimal DiscountAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal ApplicableAmount { get; set; }
    
    public string ScopeDescription { get; set; } = string.Empty;
    public List<string> AppliedProductNames { get; set; } = new();
    public List<string> AppliedCategoryNames { get; set; } = new();
    
    public string Reason { get; set; } = string.Empty;
    public List<string> AppliedConditions { get; set; } = new();
    
    private string GetTypeName()
    {
        return Type switch
        {
            CouponType.AmountOff => "满减券",
            CouponType.DiscountRate => "折扣券",
            CouponType.QuantityDiscount => "满件折",
            CouponType.FreeShipping => "免运费券",
            CouponType.MemberExclusive => "会员专享",
            _ => "优惠券"
        };
    }
}

public class MemberDiscountDetail
{
    public MemberLevel MemberLevel { get; set; }
    public string MemberLevelName { get; set; } = string.Empty;
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class FreightDiscountDetail
{
    public decimal OriginalFreight { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalFreight { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class PlanComparison
{
    public int TotalPlansGenerated { get; set; }
    public int PlansCount => AllPlans.Count;
    
    public List<PlanSummary> AllPlans { get; set; } = new();
    
    public PlanSummary? BestPlan { get; set; }
    public PlanSummary? RecommendedPlanSummary { get; set; }
    
    public List<ExcludedPlanReason> ExcludedReasons { get; set; } = new();
    
    public string ComparisonSummary { get; set; } = string.Empty;
}

public class PlanSummary
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalAmount { get; set; }
    public int CouponCount { get; set; }
    public bool IsRecommended { get; set; }
}

public class ExcludedPlanReason
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
    public decimal PotentialDiscount { get; set; }
}

public class PlanSuitability
{
    public bool IsOptimal { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsSimplest { get; set; }
    public bool IsMostSavings { get; set; }
    
    public string RecommendationReason { get; set; } = string.Empty;
    public List<string> Tips { get; set; } = new();
}

public class CalculationMetadata
{
    public int TotalCouponsProvided { get; set; }
    public int TotalCouponsAvailable { get; set; }
    public int TotalCouponsRejected { get; set; }
    public int PlansGenerated { get; set; }
    public long CalculationTimeMs { get; set; }
    public List<string> Warnings { get; set; } = new();
}
