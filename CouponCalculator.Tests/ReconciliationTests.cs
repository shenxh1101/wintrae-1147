using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class ReconciliationTests
{
    private readonly CouponCalculatorEngine _engine;
    private readonly ItemLevelAllocationService _allocationService;
    private readonly ReconciliationService _reconciliationService;

    public ReconciliationTests()
    {
        _engine = new CouponCalculatorEngine();
        _allocationService = new ItemLevelAllocationService();
        _reconciliationService = new ReconciliationService();
    }

    [Fact]
    public void CreateCheckpoint_ShouldRecordCalculationState()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        var checkpoint = _reconciliationService.CreateCheckpoint(
            context,
            result,
            enhancedResult,
            breakdown,
            CalculationStage.Trial,
            "User",
            "Customer",
            "V1.0");

        Assert.NotNull(checkpoint);
        Assert.Equal(context.OrderId, checkpoint.OrderId);
        Assert.Equal(CalculationStage.Trial, checkpoint.Stage);
        Assert.NotNull(checkpoint.ContextSnapshot);
        Assert.Equal("User", checkpoint.Operator);
    }

    [Fact]
    public void CompareCheckpoints_WithSameResult_ShouldShowNoChanges()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        var checkpoint1 = _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        var checkpoint2 = _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.OrderCreated, "System", "System", "V1.0");

        var comparison = _reconciliationService.CompareCheckpoints(
            checkpoint1.CheckpointId,
            checkpoint2.CheckpointId);

        Assert.False(comparison.HasChanges);
        Assert.Equal(ImpactLevel.None, comparison.ImpactLevel);
    }

    [Fact]
    public void CompareCheckpoints_WithDifferentAmount_ShouldShowChanges()
    {
        var context1 = _engine.CreateOrderContext();
        context1.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result1 = _engine.CalculateOptimal(context1, new List<Coupon>());
        var enhancedResult1 = _engine.CalculateOptimalEnhanced(context1, new List<Coupon>());
        var breakdown1 = _allocationService.CalculateItemBreakdown(context1, enhancedResult1);

        var checkpoint1 = _reconciliationService.CreateCheckpoint(
            context1, result1, enhancedResult1, breakdown1,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        var context2 = _engine.CreateOrderContext(context1.OrderId);
        context2.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 2
        });

        var result2 = _engine.CalculateOptimal(context2, new List<Coupon>());
        var enhancedResult2 = _engine.CalculateOptimalEnhanced(context2, new List<Coupon>());
        var breakdown2 = _allocationService.CalculateItemBreakdown(context2, enhancedResult2);

        var checkpoint2 = _reconciliationService.CreateCheckpoint(
            context2, result2, enhancedResult2, breakdown2,
            CalculationStage.OrderCreated, "System", "System", "V1.0");

        var comparison = _reconciliationService.CompareCheckpoints(
            checkpoint1.CheckpointId,
            checkpoint2.CheckpointId);

        Assert.True(comparison.HasChanges);
        Assert.NotEmpty(comparison.Differences);
        Assert.Contains(comparison.Differences, d => d.Type == DifferenceType.Amount);
    }

    [Fact]
    public void CompareCheckpoints_WithCouponChange_ShouldShowCouponDifference()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 200m,
            Quantity = 1
        });

        var result1 = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult1 = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown1 = _allocationService.CalculateItemBreakdown(context, enhancedResult1);

        var checkpoint1 = _reconciliationService.CreateCheckpoint(
            context, result1, enhancedResult1, breakdown1,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 200m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result2 = _engine.CalculateOptimal(context, coupons);
        var enhancedResult2 = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown2 = _allocationService.CalculateItemBreakdown(context, enhancedResult2);

        var checkpoint2 = _reconciliationService.CreateCheckpoint(
            context, result2, enhancedResult2, breakdown2,
            CalculationStage.PaymentPending, "Payment", "System", "V1.0");

        var comparison = _reconciliationService.CompareCheckpoints(
            checkpoint1.CheckpointId,
            checkpoint2.CheckpointId);

        Assert.True(comparison.HasChanges);
        Assert.Contains(comparison.Differences, d => d.Type == DifferenceType.CouponApplied);
    }

    [Fact]
    public void GenerateReconciliationReport_ShouldIncludeAllCheckpoints()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.OrderCreated, "System", "System", "V1.0");

        _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.PaymentCompleted, "Payment", "System", "V1.0");

        var report = _reconciliationService.GenerateReconciliationReport(context.OrderId);

        Assert.NotNull(report);
        Assert.Equal(3, report.Checkpoints.Count);
        Assert.Equal(2, report.Comparisons.Count);
        Assert.NotNull(report.Summary);
    }

    [Fact]
    public void GetCheckpointsByOrder_ShouldReturnOrderCheckpoints()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        var checkpoints = _reconciliationService.GetCheckpointsByOrder("ORDER-001");

        Assert.Single(checkpoints);
        Assert.Equal("ORDER-001", checkpoints.First().OrderId);
    }

    [Fact]
    public void GetLatestCheckpoint_ShouldReturnMostRecent()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.PaymentCompleted, "Payment", "System", "V1.0");

        var latest = _reconciliationService.GetLatestCheckpoint(context.OrderId);

        Assert.NotNull(latest);
        Assert.Equal(CalculationStage.PaymentCompleted, latest.Stage);
    }

    [Fact]
    public void FindInconsistentCalculations_ShouldIdentifyChanges()
    {
        var context1 = _engine.CreateOrderContext();
        context1.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result1 = _engine.CalculateOptimal(context1, new List<Coupon>());
        var enhancedResult1 = _engine.CalculateOptimalEnhanced(context1, new List<Coupon>());
        var breakdown1 = _allocationService.CalculateItemBreakdown(context1, enhancedResult1);

        _reconciliationService.CreateCheckpoint(
            context1, result1, enhancedResult1, breakdown1,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        var context2 = _engine.CreateOrderOrderContext(context1.OrderId);
        context2.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 2
        });

        var result2 = _engine.CalculateOptimal(context2, new List<Coupon>());
        var enhancedResult2 = _engine.CalculateOptimalEnhanced(context2, new List<Coupon>());
        var breakdown2 = _allocationService.CalculateItemBreakdown(context2, enhancedResult2);

        _reconciliationService.CreateCheckpoint(
            context2, result2, enhancedResult2, breakdown2,
            CalculationStage.PaymentPending, "Payment", "System", "V1.0");

        var inconsistencies = _reconciliationService.FindInconsistentCalculations(
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));

        Assert.NotEmpty(inconsistencies);
    }

    [Fact]
    public void GenerateComparisonReport_ShouldReturnReadableText()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        var checkpoint1 = _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        var checkpoint2 = _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.OrderCreated, "System", "System", "V1.0");

        var comparison = _reconciliationService.CompareCheckpoints(
            checkpoint1.CheckpointId,
            checkpoint2.CheckpointId);

        var report = comparison.GenerateComparisonReport();

        Assert.Contains("计算结果对比报告", report);
        Assert.Contains("对比阶段", report);
    }

    [Fact]
    public void GenerateFullReport_ShouldReturnCompleteReport()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        var report = _reconciliationService.GenerateReconciliationReport(context.OrderId);
        var fullReport = report.GenerateFullReport();

        Assert.Contains("订单优惠对账与排查报告", fullReport);
        Assert.Contains("计算节点时间线", fullReport);
        Assert.Contains("对账汇总", fullReport);
    }

    [Fact]
    public void Checkpoint_ShouldIncludeContextSnapshot()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 2,
            CategoryId = "CATE-001"
        });
        context.Member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Gold
        };
        context.FreightAmount = 15m;

        var result = _engine.CalculateOptimal(context, new List<Coupon>());
        var enhancedResult = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, enhancedResult);

        var checkpoint = _reconciliationService.CreateCheckpoint(
            context, result, enhancedResult, breakdown,
            CalculationStage.Trial, "User", "Customer", "V1.0");

        Assert.NotNull(checkpoint.ContextSnapshot);
        Assert.Equal(200m, checkpoint.ContextSnapshot.OriginalAmount);
        Assert.Equal(15m, checkpoint.ContextSnapshot.FreightAmount);
        Assert.Single(checkpoint.ContextSnapshot.Items);
        Assert.NotNull(checkpoint.ContextSnapshot.Member);
        Assert.Equal(MemberLevel.Gold, checkpoint.ContextSnapshot.Member.Level);
    }
}