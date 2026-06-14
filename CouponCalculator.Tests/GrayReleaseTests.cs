using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class GrayReleaseTests
{
    private readonly GrayReleaseManager _manager;
    
    public GrayReleaseTests()
    {
        _manager = new GrayReleaseManager();
    }
    
    [Fact]
    public void CreatePolicy_CreatesValidPolicy()
    {
        var scaleConfig = new GrayScaleConfig
        {
            ScaleType = ScaleType.Percentage,
            CurrentPercentage = 10,
            TargetPercentage = 100,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        
        var targets = new List<GrayTarget>
        {
            new GrayTarget
            {
                TargetType = GrayTargetType.Store,
                TargetIds = new List<string> { "STORE-001", "STORE-002" },
                MatchMode = GrayMatchMode.Include
            }
        };
        
        var policy = _manager.CreatePolicy("Test Policy", "VERSION-001", scaleConfig, targets);
        
        Assert.NotNull(policy);
        Assert.Equal("Test Policy", policy.PolicyName);
        Assert.Equal("VERSION-001", policy.RuleVersionId);
        Assert.Equal(GrayReleaseStatus.Draft, policy.Status);
    }
    
    [Fact]
    public void UpdatePolicyStatus_TransitionsValidly()
    {
        var policy = CreateTestPolicy();
        
        _manager.UpdatePolicyStatus(policy.PolicyId, GrayReleaseStatus.Ready);
        var updated = _manager.GetPolicy(policy.PolicyId);
        
        Assert.Equal(GrayReleaseStatus.Ready, updated.Status);
    }
    
    [Fact]
    public void UpdatePolicyStatus_InvalidTransition_ThrowsException()
    {
        var policy = CreateTestPolicy();
        _manager.UpdatePolicyStatus(policy.PolicyId, GrayReleaseStatus.Ready);
        
        Assert.Throws<InvalidOperationException>(() =>
            _manager.UpdatePolicyStatus(policy.PolicyId, GrayReleaseStatus.Draft));
    }
    
    [Fact]
    public async Task PlaybackOrderWithPolicy_ReturnsValidResult()
    {
        var policy = CreateTestPolicy();
        var order = new OrderRecord
        {
            OrderId = "ORDER-001",
            OriginalAmount = 200m,
            StoreId = "STORE-001",
            Channel = "APP",
            MemberLevel = "VIP"
        };
        
        var result = await _manager.PlaybackOrderWithPolicy(policy.PolicyId, order, new List<Coupon>());
        
        Assert.NotNull(result);
        Assert.Equal(policy.PolicyId, result.PolicyId);
        Assert.Equal(order.OrderId, result.OrderId);
    }
    
    [Fact]
    public async Task BatchPlaybackWithPolicy_ProcessesAllOrders()
    {
        var policy = CreateTestPolicy();
        var orders = new List<OrderRecord>
        {
            new OrderRecord { OrderId = "O1", OriginalAmount = 100m, StoreId = "STORE-001" },
            new OrderRecord { OrderId = "O2", OriginalAmount = 200m, StoreId = "STORE-002" },
            new OrderRecord { OrderId = "O3", OriginalAmount = 300m, StoreId = "STORE-003" }
        };
        
        var results = await _manager.BatchPlaybackWithPolicy(policy.PolicyId, orders);
        
        Assert.Equal(3, results.Count);
    }
    
    [Fact]
    public async Task CompareGrayScales_GeneratesComparisons()
    {
        var policy = CreateTestPolicy();
        var orders = Enumerable.Range(1, 50)
            .Select(i => new OrderRecord
            {
                OrderId = $"ORDER-{i}",
                OriginalAmount = 100m + i * 10,
                StoreId = $"STORE-{i % 5}",
                Channel = i % 2 == 0 ? "APP" : "WEB",
                MemberLevel = $"Level-{i % 3}"
            })
            .ToList();
        
        var result = await _manager.CompareGrayScales(policy.PolicyId, orders, new List<decimal> { 10, 25, 50, 100 });
        
        Assert.NotNull(result);
        Assert.Equal(4, result.ScaleComparisons.Count);
        Assert.Equal(50, result.OrderCount);
    }
    
    [Fact]
    public void AnalyzePlaybackResults_ReturnsAnalysis()
    {
        var policy = CreateTestPolicy();
        
        var analysis = _manager.AnalyzePlaybackResults(policy.PolicyId);
        
        Assert.NotNull(analysis);
        Assert.Equal(policy.PolicyId, analysis.PolicyId);
    }
    
    [Fact]
    public void GenerateGrayReleaseReport_ReturnsNonEmptyReport()
    {
        var policy = CreateTestPolicy();
        
        var report = _manager.GenerateGrayReleaseReport(policy.PolicyId);
        
        Assert.NotEmpty(report);
        Assert.Contains("灰度发布分析报告", report);
        Assert.Contains(policy.PolicyName, report);
    }
    
    [Fact]
    public void GetPoliciesByTarget_ReturnsMatchingPolicies()
    {
        var policy = CreateTestPolicy();
        
        var policies = _manager.GetPoliciesByTarget(GrayTargetType.Store.ToString(), "STORE-001");
        
        Assert.NotEmpty(policies);
    }
    
    [Fact]
    public void GetPoliciesByStatus_ReturnsMatchingPolicies()
    {
        CreateTestPolicy();
        
        var policies = _manager.GetPoliciesByStatus(GrayReleaseStatus.Draft);
        
        Assert.NotEmpty(policies);
        Assert.All(policies, p => Assert.Equal(GrayReleaseStatus.Draft, p.Status));
    }
    
    [Fact]
    public void DeletePolicy_RemovesPolicy()
    {
        var policy = CreateTestPolicy();
        
        var result = _manager.DeletePolicy(policy.PolicyId);
        
        Assert.True(result);
        Assert.Null(_manager.GetPolicy(policy.PolicyId));
    }
    
    [Fact]
    public void DeletePolicy_ActivePolicy_ThrowsException()
    {
        var policy = CreateTestPolicy();
        _manager.UpdatePolicyStatus(policy.PolicyId, GrayReleaseStatus.Active);
        
        Assert.Throws<InvalidOperationException>(() => _manager.DeletePolicy(policy.PolicyId));
    }
    
    [Fact]
    public void GrayReleasePolicy_GetProgress_ReturnsCorrectValue()
    {
        var policy = new GrayReleasePolicy
        {
            ScaleConfig = new GrayScaleConfig
            {
                CurrentPercentage = 50,
                TargetPercentage = 100
            }
        };
        
        var progress = policy.GetProgress();
        
        Assert.Equal(50m, progress);
    }
    
    private GrayReleasePolicy CreateTestPolicy()
    {
        var scaleConfig = new GrayScaleConfig
        {
            ScaleType = ScaleType.Percentage,
            CurrentPercentage = 10,
            TargetPercentage = 100,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        
        var targets = new List<GrayTarget>
        {
            new GrayTarget
            {
                TargetType = GrayTargetType.Store,
                TargetIds = new List<string> { "STORE-001", "STORE-002" },
                MatchMode = GrayMatchMode.Include
            }
        };
        
        return _manager.CreatePolicy($"Test-{Guid.NewGuid():N}", "VERSION-001", scaleConfig, targets);
    }
}

public class GrayPlaybackResultTests
{
    [Fact]
    public void GrayPlaybackResult_CalculatesDifference()
    {
        var result = new GrayPlaybackResult
        {
            PlaybackId = "P1",
            PolicyId = "POL-001",
            OrderId = "O1",
            IsEligible = true,
            OldVersionResult = new CalculationResult
            {
                RecommendedPlan = new CandidatePlan
                {
                    FinalTotalAmount = 100m,
                    TotalDiscountAmount = 20m
                }
            },
            NewVersionResult = new CalculationResult
            {
                RecommendedPlan = new CandidatePlan
                {
                    FinalTotalAmount = 95m,
                    TotalDiscountAmount = 25m
                }
            }
        };
        
        Assert.Equal(-5m, result.Difference);
        Assert.Equal(-0.05m, result.DifferencePercent);
    }
}

public class GrayScaleConfigTests
{
    [Fact]
    public void GrayScaleConfig_IsWithinSchedule_ReturnsTrue()
    {
        var config = new GrayScaleConfig
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        
        Assert.True(config.IsWithinSchedule());
    }
    
    [Fact]
    public void GrayScaleConfig_IsWithinSchedule_ReturnsFalse()
    {
        var config = new GrayScaleConfig
        {
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2)
        };
        
        Assert.False(config.IsWithinSchedule());
    }
}
