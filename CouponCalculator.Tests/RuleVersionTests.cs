using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class RuleVersionTests
{
    private readonly RuleVersionManager _versionManager;

    public RuleVersionTests()
    {
        _versionManager = new RuleVersionManager();
    }

    [Fact]
    public void CreateVersion_ShouldCreateNewVersion()
    {
        var coupons = new List<Coupon>
        {
            new Coupon { CouponId = "C1", Type = CouponType.AmountOff, DiscountValue = 20m }
        };

        var rules = new List<DiscountRule>
        {
            new DiscountRule { RuleId = "R1", Type = DiscountRuleType.AmountThreshold }
        };

        var version = _versionManager.CreateVersion("V1.0", "初始版本", coupons, rules, "Admin");

        Assert.NotNull(version);
        Assert.Equal("V1.0", version.VersionName);
        Assert.Equal(VersionStatus.Draft, version.Status);
        Assert.Single(version.Coupons);
        Assert.Single(version.Rules);
    }

    [Fact]
    public void CreateSwitchPlan_ShouldGenerateDiffPreview()
    {
        var oldCoupons = new List<Coupon>
        {
            new Coupon { CouponId = "C1", CouponCode = "OLD", Type = CouponType.AmountOff, DiscountValue = 20m }
        };

        var newCoupons = new List<Coupon>
        {
            new Coupon { CouponId = "C1", CouponCode = "NEW", Type = CouponType.AmountOff, DiscountValue = 25m },
            new Coupon { CouponId = "C2", CouponCode = "NEW2", Type = CouponType.DiscountRate, DiscountValue = 0.9m }
        };

        var oldVersion = _versionManager.CreateVersion("V1.0", "旧版本", oldCoupons, new List<DiscountRule>(), "Admin");
        var newVersion = _versionManager.CreateVersion("V2.0", "新版本", newCoupons, new List<DiscountRule>(), "Admin");

        var plan = _versionManager.CreateSwitchPlan(
            oldVersion.VersionId,
            newVersion.VersionId,
            DateTime.UtcNow.AddDays(1),
            "Admin");

        Assert.NotNull(plan);
        Assert.NotNull(plan.Preview);
        Assert.Single(plan.Preview.AddedCoupons);
        Assert.Single(plan.Preview.ModifiedCoupons);
    }

    [Fact]
    public void GenerateDiffPreview_ShouldIdentifyChanges()
    {
        var fromVersion = new RuleVersion
        {
            Coupons = new List<Coupon>
            {
                new Coupon { CouponId = "C1", DiscountValue = 20m, MinOrderAmount = 100m }
            },
            Rules = new List<DiscountRule>
            {
                new DiscountRule { RuleId = "R1", DiscountValue = 20m }
            }
        };

        var toVersion = new RuleVersion
        {
            Coupons = new List<Coupon>
            {
                new Coupon { CouponId = "C1", DiscountValue = 25m, MinOrderAmount = 100m },
                new Coupon { CouponId = "C2", DiscountValue = 30m }
            },
            Rules = new List<DiscountRule>
            {
                new DiscountRule { RuleId = "R1", DiscountValue = 25m }
            }
        };

        var preview = _versionManager.GenerateDiffPreview(fromVersion, toVersion);

        Assert.Single(preview.AddedCoupons);
        Assert.Single(preview.ModifiedCoupons);
        Assert.Single(preview.ModifiedRules);
        Assert.NotNull(preview.ImpactAnalysis);
    }

    [Fact]
    public void ApprovePlan_ShouldUpdateStatus()
    {
        var version1 = _versionManager.CreateVersion("V1", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");
        var version2 = _versionManager.CreateVersion("V2", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");

        var plan = _versionManager.CreateSwitchPlan(
            version1.VersionId,
            version2.VersionId,
            DateTime.UtcNow.AddDays(1),
            "Admin");

        plan.RequireApproval = true;

        var result = _versionManager.ApprovePlan(plan.PlanId, "Manager-A");

        Assert.True(result);
        Assert.Single(plan.ApprovedBy);
        Assert.Equal(PlanStatus.Created, plan.Status);

        _versionManager.ApprovePlan(plan.PlanId, "Manager-B");
        Assert.Equal(PlanStatus.Approved, plan.Status);
    }

    [Fact]
    public void ExecutePlan_ShouldSwitchVersions()
    {
        var version1 = _versionManager.CreateVersion("V1", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");
        var version2 = _versionManager.CreateVersion("V2", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");

        var plan = _versionManager.CreateSwitchPlan(
            version1.VersionId,
            version2.VersionId,
            DateTime.UtcNow.AddDays(1),
            "Admin");

        _versionManager.ApprovePlan(plan.PlanId, "Manager");
        plan.Status = PlanStatus.Approved;

        var result = _versionManager.ExecutePlan(plan.PlanId);

        Assert.True(result);
        Assert.Equal(PlanStatus.Completed, plan.Status);
        Assert.Equal(VersionStatus.Deprecated, version1.Status);
        Assert.Equal(VersionStatus.Active, version2.Status);
    }

    [Fact]
    public void CancelPlan_ShouldUpdateStatus()
    {
        var version1 = _versionManager.CreateVersion("V1", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");
        var version2 = _versionManager.CreateVersion("V2", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");

        var plan = _versionManager.CreateSwitchPlan(
            version1.VersionId,
            version2.VersionId,
            DateTime.UtcNow.AddDays(1),
            "Admin");

        var result = _versionManager.CancelPlan(plan.PlanId);

        Assert.True(result);
        Assert.Equal(PlanStatus.Cancelled, plan.Status);
    }

    [Fact]
    public void GetVersionsByStatus_ShouldFilterCorrectly()
    {
        _versionManager.CreateVersion("V1", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");
        _versionManager.CreateVersion("V2", "", new List<Coupon>(), new List<DiscountRule>(), "Admin");

        var draftVersions = _versionManager.GetVersionsByStatus(VersionStatus.Draft);

        Assert.Equal(2, draftVersions.Count);
    }

    [Fact]
    public void GenerateDiffReport_ShouldReturnReadableText()
    {
        var preview = new VersionDiffPreview
        {
            AddedCoupons = new List<CouponChange>
            {
                new CouponChange { CouponName = "新券", CouponCode = "NEW" }
            },
            ModifiedRules = new List<RuleChange>
            {
                new RuleChange { RuleName = "满减规则", ChangeDescription = "优惠值从20改为25" }
            },
            ImpactAnalysis = new ImpactAnalysis
            {
                EstimatedAffectedOrders = 1000,
                EstimatedDiscountChange = 500m,
                RiskLevel = "中",
                Warnings = new List<string> { "可能影响大额订单" }
            }
        };

        var report = preview.GenerateDiffReport();

        Assert.Contains("新增优惠券", report);
        Assert.Contains("新券", report);
        Assert.Contains("修改规则", report);
        Assert.Contains("满减规则", report);
        Assert.Contains("影响分析", report);
        Assert.Contains("风险提示", report);
    }
}