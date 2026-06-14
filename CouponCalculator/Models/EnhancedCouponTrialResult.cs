namespace CouponCalculator.Models;

public class EnhancedCouponTrialResult
{
    public Coupon Coupon { get; set; } = null!;
    public bool IsAvailable { get; set; }
    
    public TrialStatus Status { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    
    public decimal DiscountAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal ApplicableAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    
    public List<TrialCheckResult> CheckResults { get; set; } = new();
    public List<string> UnavailableReasons { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public List<string> ApplicableProductIds { get; set; } = new();
    public List<string> ApplicableCategoryIds { get; set; } = new();
    public List<string> ExcludedProductIds { get; set; } = new();
    
    public string ScopeDescription { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    
    public UserFriendlyExplanation UserExplanation { get; set; } = new();
}

public enum TrialStatus
{
    Available,
    Unavailable,
    PartiallyAvailable,
    Expired,
    NotYetValid,
    ThresholdNotMet,
    MemberLevelNotMet,
    ScopeNotMatched,
    AlreadyUsed,
    MaxUsageReached
}

public class TrialCheckResult
{
    public string CheckName { get; set; } = string.Empty;
    public CheckType Type { get; set; }
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserFriendlyMessage { get; set; } = string.Empty;
    public object? Expected { get; set; }
    public object? Actual { get; set; }
}

public enum CheckType
{
    Validity,
    TimeRange,
    Threshold,
    Quantity,
    MemberLevel,
    UserRestriction,
    StoreRestriction,
    Scope,
    UsageLimit,
    StackingConflict,
    ProductMatch,
    CategoryMatch
}

public class UserFriendlyExplanation
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Highlights { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public string? NextStep { get; set; }
}
