using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class TemplatePlaybackTests
{
    private readonly TemplatePlaybackService _service;
    private readonly CouponCalculatorEngine _engine;
    
    public TemplatePlaybackTests()
    {
        _service = new TemplatePlaybackService();
        _engine = new CouponCalculatorEngine();
    }
    
    [Fact]
    public void PlaybackWithTemplate_ReturnsValidResult()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 200m,
            Quantity = 1
        });
        
        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "COUPON-001",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m
            }
        };
        
        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        
        var snapshot = _service.CreateSnapshot(context, coupons, result, "ORDER-001");
        var template = _service.SaveAsTemplate(snapshot, "Test Template", "Test", TemplateType.Order, "System");
        
        var order = new OrderRecord
        {
            OrderId = "ORDER-002",
            OriginalAmount = 200m,
            StoreId = "STORE-001",
            Channel = "APP",
            MemberLevel = "VIP"
        };
        
        var playbackRequest = new TemplatePlaybackRequest { TemplateId = template.TemplateId };
        var playbackResult = _service.PlaybackWithTemplate(playbackRequest, order);
        
        Assert.NotNull(playbackResult);
        Assert.Equal(template.TemplateId, playbackResult.TemplateId);
    }
    
    [Fact]
    public void PlaybackWithTemplate_NonExistentTemplate_ReturnsWarning()
    {
        var order = new OrderRecord
        {
            OrderId = "ORDER-001",
            OriginalAmount = 100m
        };
        
        var request = new TemplatePlaybackRequest { TemplateId = "NON-EXISTENT" };
        var result = _service.PlaybackWithTemplate(request, order);
        
        Assert.False(result.IsMatch);
        Assert.Contains("不存在", result.Warnings.First());
    }
    
    [Fact]
    public void PlaybackWithTemplate_WithParameterOverrides_AppliesOverrides()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem { ProductId = "SKU-001", Price = 100m, Quantity = 1 });
        var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        
        var snapshot = _service.CreateSnapshot(context, new List<Coupon>(), result, "ORDER-001");
        var template = _service.SaveAsTemplate(snapshot, "Override Test", "Test", TemplateType.Order, "System");
        
        var order = new OrderRecord
        {
            OrderId = "ORDER-002",
            OriginalAmount = 200m,
            StoreId = "STORE-001"
        };
        
        var request = new TemplatePlaybackRequest
        {
            TemplateId = template.TemplateId,
            ParameterOverrides = new Dictionary<string, object>
            {
                { "Amount", 300m }
            }
        };
        
        var playbackResult = _service.PlaybackWithTemplate(request, order);
        
        Assert.NotNull(playbackResult);
    }
    
    [Fact]
    public void BatchPlaybackWithTemplate_ProcessesAllOrders()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem { ProductId = "SKU-001", Price = 100m, Quantity = 1 });
        var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        
        var snapshot = _service.CreateSnapshot(context, new List<Coupon>(), result, "ORDER-001");
        var template = _service.SaveAsTemplate(snapshot, "Batch Test", "Test", TemplateType.Order, "System");
        
        var orders = new List<OrderRecord>
        {
            new OrderRecord { OrderId = "O1", OriginalAmount = 100m },
            new OrderRecord { OrderId = "O2", OriginalAmount = 200m },
            new OrderRecord { OrderId = "O3", OriginalAmount = 300m }
        };
        
        var results = _service.BatchPlaybackWithTemplate(template.TemplateId, orders);
        
        Assert.Equal(3, results.Count);
    }
    
    [Fact]
    public void CompareTemplates_ReturnsComparison()
    {
        var context1 = _engine.CreateOrderContext("O1");
        context1.AddItem(new OrderItem { ProductId = "SKU-001", Price = 100m, Quantity = 1 });
        var r1 = _engine.CalculateOptimalEnhanced(context1, new List<Coupon>());
        var s1 = _service.CreateSnapshot(context1, new List<Coupon>(), r1, "O1");
        var t1 = _service.SaveAsTemplate(s1, "Template1", "Test", TemplateType.Order, "System");
        
        var context2 = _engine.CreateOrderContext("O2");
        context2.AddItem(new OrderItem { ProductId = "SKU-001", Price = 150m, Quantity = 1 });
        var r2 = _engine.CalculateOptimalEnhanced(context2, new List<Coupon>());
        var s2 = _service.CreateSnapshot(context2, new List<Coupon>(), r2, "O2");
        var t2 = _service.SaveAsTemplate(s2, "Template2", "Test", TemplateType.Order, "System");
        
        var comparison = _service.CompareTemplates(t1.TemplateId, t2.TemplateId);
        
        Assert.NotNull(comparison);
        Assert.Equal(t1.TemplateId, comparison.TemplateId1);
        Assert.Equal(t2.TemplateId, comparison.TemplateId2);
    }
    
    [Fact]
    public void CompareTemplates_NonExistent_HandlesGracefully()
    {
        var comparison = _service.CompareTemplates("NON-1", "NON-2");
        
        Assert.NotNull(comparison);
        Assert.Empty(comparison.Differences);
    }
    
    [Fact]
    public void GenerateUnifiedExplanation_ReturnsExplanation()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem { ProductId = "SKU-001", Price = 200m, Quantity = 1 });
        context.Member = new MemberInfo { MemberId = "M1", Level = MemberLevel.VIP };
        
        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                Type = CouponType.PercentOff,
                DiscountValue = 10m,
                MinOrderAmount = 100m
            }
        };
        
        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var snapshot = _service.CreateSnapshot(context, coupons, result, "ORDER-001");
        var template = _service.SaveAsTemplate(snapshot, "Explanation Test", "Test", TemplateType.Order, "System");
        
        var order = new OrderRecord
        {
            OrderId = "ORDER-002",
            OriginalAmount = 200m,
            StoreId = "STORE-001"
        };
        
        var explanation = _service.GenerateUnifiedExplanation(
            template.TemplateId, order, AudienceType.CustomerService);
        
        Assert.NotNull(explanation);
        Assert.Equal(order.OrderId, explanation.OrderId);
    }
    
    [Fact]
    public void GenerateAllAudienceExplanations_ReturnsAllTypes()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem { ProductId = "SKU-001", Price = 100m, Quantity = 1 });
        var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        
        var snapshot = _service.CreateSnapshot(context, new List<Coupon>(), result, "ORDER-001");
        var template = _service.SaveAsTemplate(snapshot, "All Audiences", "Test", TemplateType.Order, "System");
        
        var order = new OrderRecord { OrderId = "ORDER-002", OriginalAmount = 100m };
        
        var explanations = _service.GenerateAllAudienceExplanations(template.TemplateId, order);
        
        Assert.Equal(5, explanations.Count);
        Assert.Contains(explanations, e => e.Audience == AudienceType.Customer);
        Assert.Contains(explanations, e => e.Audience == AudienceType.CustomerService);
        Assert.Contains(explanations, e => e.Audience == AudienceType.Operations);
        Assert.Contains(explanations, e => e.Audience == AudienceType.Finance);
        Assert.Contains(explanations, e => e.Audience == AudienceType.Audit);
    }
    
    [Fact]
    public void FindSimilarTemplates_ReturnsMatches()
    {
        var context1 = _engine.CreateOrderContext("O1");
        context1.AddItem(new OrderItem { ProductId = "SKU-001", Price = 200m, Quantity = 1 });
        context1.Member = new MemberInfo { MemberId = "M1", Level = MemberLevel.VIP };
        var r1 = _engine.CalculateOptimalEnhanced(context1, new List<Coupon>());
        var s1 = _service.CreateSnapshot(context1, new List<Coupon>(), r1, "O1");
        _service.SaveAsTemplate(s1, "Similar 1", "Test", TemplateType.Order, "System");
        
        var context2 = _engine.CreateOrderContext("O2");
        context2.AddItem(new OrderItem { ProductId = "SKU-002", Price = 200m, Quantity = 1 });
        context2.Member = new MemberInfo { MemberId = "M2", Level = MemberLevel.VIP };
        var r2 = _engine.CalculateOptimalEnhanced(context2, new List<Coupon>());
        var s2 = _service.CreateSnapshot(context2, new List<Coupon>(), r2, "O2");
        _service.SaveAsTemplate(s2, "Similar 2", "Test", TemplateType.Order, "System");
        
        var similar = _service.FindSimilarTemplates(s2, 10);
        
        Assert.NotEmpty(similar);
    }
    
    [Fact]
    public void SearchTemplates_ReturnsFilteredResults()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem { ProductId = "SKU-001", Price = 100m, Quantity = 1 });
        var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        
        _service.SaveAsTemplate(
            _service.CreateSnapshot(context, new List<Coupon>(), result, "O1"),
            "Search Test 1", "Category1", TemplateType.Order, "User1");
        _service.SaveAsTemplate(
            _service.CreateSnapshot(context, new List<Coupon>(), result, "O2"),
            "Search Test 2", "Category2", TemplateType.Order, "User1");
        _service.SaveAsTemplate(
            _service.CreateSnapshot(context, new List<Coupon>(), result, "O3"),
            "Other", "Category1", TemplateType.Trial, "User2");
        
        var criteria = new TemplateSearchCriteria
        {
            Category = "Category1",
            CreatedBy = "User1",
            Type = TemplateType.Order
        };
        
        var searchResult = _service.SearchTemplates(criteria);
        
        Assert.Equal(2, searchResult.TotalCount);
        Assert.All(searchResult.Templates, t =>
        {
            Assert.Equal("Category1", t.Category);
            Assert.Equal("User1", t.CreatedBy);
        });
    }
    
    [Fact]
    public void CompareMultipleTemplates_ReturnsComparisons()
    {
        var templates = new List<string>();
        
        for (int i = 1; i <= 3; i++)
        {
            var context = _engine.CreateOrderContext($"O{i}");
            context.AddItem(new OrderItem { ProductId = "SKU-001", Price = 100m + i * 10, Quantity = 1 });
            var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
            var snapshot = _service.CreateSnapshot(context, new List<Coupon>(), result, $"O{i}");
            var template = _service.SaveAsTemplate(snapshot, $"T{i}", "Test", TemplateType.Order, "System");
            templates.Add(template.TemplateId);
        }
        
        var batchComparison = _service.CompareMultipleTemplates(templates);
        
        Assert.Equal(3, batchComparison.Comparisons.Count);
        Assert.Equal(3, batchComparison.TemplateIds.Count);
    }
    
    [Fact]
    public void CreateSnapshot_CapturesAllData()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem { ProductId = "SKU-001", Price = 200m, Quantity = 2 });
        context.FreightAmount = 10m;
        context.Member = new MemberInfo { MemberId = "M1", Level = MemberLevel.Gold };
        context.ExtendedData["StoreId"] = "S001";
        
        var coupons = new List<Coupon>
        {
            new Coupon { CouponId = "C1", CouponCode = "CODE1", Type = CouponType.AmountOff, DiscountValue = 20m }
        };
        
        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var snapshot = _service.CreateSnapshot(context, coupons, result, "ORDER-001");
        
        Assert.Equal("ORDER-001", snapshot.OrderId);
        Assert.NotNull(snapshot.OrderSnapshot);
        Assert.Equal(400m, snapshot.OrderSnapshot.OriginalAmount);
        Assert.Equal(2, snapshot.OrderSnapshot.ItemCount);
        Assert.NotNull(snapshot.OrderSnapshot.Member);
        Assert.Equal("Gold", snapshot.OrderSnapshot.Member.Level);
        Assert.Single(snapshot.CouponsSnapshot);
        Assert.Single(snapshot.AppliedCouponIds);
    }
}

public class UnifiedExplanationTests
{
    [Fact]
    public void GenerateForAudience_Customer_ReturnsSimpleExplanation()
    {
        var explanation = new UnifiedExplanation
        {
            OrderId = "ORDER-001",
            Audience = AudienceType.Customer,
            Content = new UnifiedExplanationContent
            {
                FinalAmount = 180m,
                AppliedDiscounts = new List<AppliedDiscountInfo>
                {
                    new AppliedDiscountInfo { Name = "满减券", Amount = 20m }
                },
                CustomerMessage = "感谢购买"
            }
        };
        
        var result = explanation.GenerateCustomerExplanation();
        
        Assert.Contains("ORDER-001", result);
        Assert.Contains("180", result);
        Assert.Contains("满减券", result);
    }
    
    [Fact]
    public void GenerateForAudience_CustomerService_ReturnsDetailedExplanation()
    {
        var explanation = new UnifiedExplanation
        {
            OrderId = "ORDER-001",
            Audience = AudienceType.CustomerService,
            Content = new UnifiedExplanationContent
            {
                OriginalAmount = 200m,
                TotalDiscount = 20m,
                FinalAmount = 180m,
                DiscountDetails = new List<DiscountDetailInfo>
                {
                    new DiscountDetailInfo { Type = "满减券", Amount = 20m, Reason = "订单满100可用" }
                }
            }
        };
        
        var result = explanation.GenerateCustomerServiceExplanation();
        
        Assert.Contains("客服口径", result);
        Assert.Contains("200", result);
        Assert.Contains("180", result);
        Assert.Contains("订单满100可用", result);
    }
    
    [Fact]
    public void GenerateForAudience_Operations_ReturnsOperationsExplanation()
    {
        var explanation = new UnifiedExplanation
        {
            OrderId = "ORDER-001",
            Audience = AudienceType.Operations,
            Content = new UnifiedExplanationContent
            {
                OriginalAmount = 200m,
                TotalDiscount = 20m,
                DiscountRate = 0.1m,
                FinalAmount = 180m,
                AppliedRules = new List<AppliedRuleInfo>
                {
                    new AppliedRuleInfo { RuleName = "新人券", Contribution = 20m }
                }
            }
        };
        
        var result = explanation.GenerateOperationsExplanation();
        
        Assert.Contains("运营口径", result);
        Assert.Contains("10%", result);
        Assert.Contains("新人券", result);
    }
    
    [Fact]
    public void GenerateForAudience_Finance_ReturnsFinanceExplanation()
    {
        var explanation = new UnifiedExplanation
        {
            OrderId = "ORDER-001",
            Audience = AudienceType.Finance,
            Content = new UnifiedExplanationContent
            {
                OriginalAmount = 200m,
                TotalDiscount = 20m,
                FinalAmount = 180m,
                Freight = 10m,
                DiscountDetails = new List<DiscountDetailInfo>
                {
                    new DiscountDetailInfo { Type = "优惠", Amount = 20m, TaxTreatment = "不征税" }
                }
            }
        };
        
        var result = explanation.GenerateFinanceExplanation();
        
        Assert.Contains("财务口径", result);
        Assert.Contains("190", result);
        Assert.Contains("净应付", result);
        Assert.Contains("不征税", result);
    }
}

public class OrderRecord
{
    public string OrderId { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string MemberLevel { get; set; } = string.Empty;
}
