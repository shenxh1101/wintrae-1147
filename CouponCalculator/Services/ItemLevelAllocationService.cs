using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class ItemLevelAllocationService
{
    public ItemLevelDiscountBreakdown CalculateItemBreakdown(
        OrderContext context,
        EnhancedCalculationResult enhancedResult)
    {
        var breakdown = new ItemLevelDiscountBreakdown
        {
            OrderId = context.OrderId,
            TotalOriginalAmount = context.OriginalAmount,
            CalculatedAt = DateTime.UtcNow
        };
        
        if (enhancedResult.RecommendedPlan == null)
        {
            breakdown.TotalFinalAmount = context.OriginalAmount + context.FreightAmount;
            return breakdown;
        }
        
        var plan = enhancedResult.RecommendedPlan;
        breakdown.TotalDiscountAmount = plan.TotalDiscountAmount;
        breakdown.TotalFinalAmount = plan.FinalTotalAmount;
        
        foreach (var item in context.Items)
        {
            var itemDetail = CalculateItemDetail(item, context, plan);
            breakdown.ItemDetails.Add(itemDetail);
        }
        
        breakdown.FreightDetail = CalculateFreightDetail(context, plan);
        breakdown.MemberDetail = CalculateMemberDetail(context, plan);
        
        breakdown.CouponAllocations = CalculateCouponAllocations(context, plan, breakdown.ItemDetails);
        
        return breakdown;
    }
    
    private ItemDiscountDetail CalculateItemDetail(
        OrderItem item,
        OrderContext context,
        CandidatePlan plan)
    {
        var detail = new ItemDiscountDetail
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            CategoryId = item.CategoryId,
            OriginalAmount = item.Price,
            Quantity = item.Quantity,
            TotalOriginal = item.TotalAmount
        };
        
        var applicableCoupons = plan.AppliedCoupons
            .Where(c => c.Type != CouponType.FreeShipping)
            .Where(c => IsCouponApplicableToItem(c.CouponId, item, plan))
            .ToList();
        
        foreach (var coupon in applicableCoupons)
        {
            var allocation = CalculateCouponAllocationForItem(coupon, item, context, plan);
            detail.CouponDiscounts.Add(allocation);
            detail.TotalCouponDiscount += allocation.AllocatedAmount;
            detail.AppliedCouponIds.Add(coupon.CouponId);
        }
        
        if (plan.MemberDiscountAmount > 0 && plan.MemberDiscount != null)
        {
            var memberRatio = item.TotalAmount / context.OriginalAmount;
            detail.MemberDiscount = plan.MemberDiscountAmount * memberRatio;
        }
        
        detail.TotalDiscount = detail.TotalCouponDiscount + detail.MemberDiscount;
        detail.FinalAmount = detail.TotalOriginal - detail.TotalDiscount;
        
        return detail;
    }
    
    private CouponItemAllocation CalculateCouponAllocationForItem(
        AppliedCouponDetail coupon,
        OrderItem item,
        OrderContext context,
        CandidatePlan plan)
    {
        var allocation = new CouponItemAllocation
        {
            CouponId = coupon.CouponId,
            CouponCode = coupon.CouponCode,
            CouponName = coupon.CouponName,
            CouponType = coupon.Type,
            AllocationMethod = DetermineAllocationMethod(coupon.Type)
        };
        
        switch (coupon.Type)
        {
            case CouponType.AmountOff:
                var totalApplicableAmount = GetTotalApplicableAmount(coupon.CouponId, context, plan);
                var itemRatio = item.TotalAmount / totalApplicableAmount;
                
                allocation.AllocatedAmount = coupon.DiscountAmount * itemRatio;
                allocation.AllocationRatio = itemRatio;
                allocation.AllocationReason = $"按商品金额比例分摊 (商品金额 ¥{item.TotalAmount:F2} / 适用总额 ¥{totalApplicableAmount:F2})";
                allocation.IsProportional = true;
                break;
                
            case CouponType.DiscountRate:
                allocation.ApplicableAmount = item.TotalAmount;
                allocation.DiscountRate = coupon.DiscountAmount / item.TotalAmount;
                allocation.AllocatedAmount = coupon.DiscountAmount;
                allocation.AllocationRatio = 1.0m;
                allocation.AllocationReason = $"直接折扣 (折扣率 {(1 - allocation.DiscountRate):P2})";
                allocation.IsFixed = true;
                break;
                
            case CouponType.QuantityDiscount:
                var applicableQty = GetApplicableQuantity(coupon.CouponId, context, plan);
                var qtyRatio = (decimal)item.Quantity / applicableQty;
                
                allocation.AllocatedAmount = coupon.DiscountAmount * qtyRatio;
                allocation.AllocationRatio = qtyRatio;
                allocation.AllocationReason = $"按商品数量比例分摊 (商品数量 {item.Quantity} / 适用总数 {applicableQty})";
                allocation.IsProportional = true;
                break;
                
            case CouponType.MemberExclusive:
                allocation.ApplicableAmount = item.TotalAmount;
                allocation.AllocatedAmount = coupon.DiscountAmount;
                allocation.AllocationRatio = 1.0m;
                allocation.AllocationReason = "会员专享折扣直接应用";
                allocation.IsFixed = true;
                break;
                
            default:
                allocation.AllocatedAmount = 0;
                allocation.AllocationReason = "未应用优惠";
                break;
        }
        
        return allocation;
    }
    
    private FreightDiscountDetail CalculateFreightDetail(OrderContext context, CandidatePlan plan)
    {
        var detail = new FreightDiscountDetail
        {
            OriginalFreight = context.FreightAmount,
            FinalFreight = plan.FinalFreightAmount,
            DiscountAmount = plan.FreightDiscountAmount
        };
        
        if (plan.FreightDiscountAmount > 0)
        {
            var freeShippingCoupon = plan.AppliedCoupons.FirstOrDefault(c => c.Type == CouponType.FreeShipping);
            if (freeShippingCoupon != null)
            {
                detail.Source = $"免运费券: {freeShippingCoupon.CouponName}";
                detail.Reason = freeShippingCoupon.Reason;
            }
            else
            {
                detail.Source = "系统自动免运费";
                detail.Reason = "满足免运费条件";
            }
        }
        
        return detail;
    }
    
    private MemberDiscountDetail CalculateMemberDetail(OrderContext context, CandidatePlan plan)
    {
        var detail = new MemberDiscountDetail();
        
        if (plan.MemberDiscount != null && context.Member != null)
        {
            detail.MemberLevel = context.Member.Level;
            detail.MemberLevelName = GetMemberLevelName(context.Member.Level);
            detail.DiscountRate = context.Member.GetDiscountRate();
            detail.DiscountAmount = plan.MemberDiscountAmount;
            detail.Reason = plan.MemberDiscount.Reason;
        }
        
        return detail;
    }
    
    private List<CouponAllocation> CalculateCouponAllocations(
        OrderContext context,
        CandidatePlan plan,
        List<ItemDiscountDetail> itemDetails)
    {
        var allocations = new List<CouponAllocation>();
        
        foreach (var coupon in plan.AppliedCoupons.Where(c => c.Type != CouponType.FreeShipping))
        {
            var allocation = new CouponAllocation
            {
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                CouponName = coupon.CouponName,
                CouponType = coupon.Type,
                TotalDiscountAmount = coupon.DiscountAmount,
                AllocationStrategy = DetermineAllocationMethod(coupon.Type)
            };
            
            var relatedItems = itemDetails
                .Where(i => i.AppliedCouponIds.Contains(coupon.CouponId))
                .ToList();
            
            foreach (var item in relatedItems)
            {
                var itemAllocation = item.CouponDiscounts
                    .FirstOrDefault(c => c.CouponId == coupon.CouponId);
                
                if (itemAllocation != null)
                {
                    allocation.ItemAllocations.Add(new ItemAllocationDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Amount = itemAllocation.AllocatedAmount,
                        Ratio = itemAllocation.AllocationRatio,
                        Reason = itemAllocation.AllocationReason
                    });
                    
                    allocation.TotalApplicableAmount += item.TotalOriginal;
                }
            }
            
            allocations.Add(allocation);
        }
        
        return allocations;
    }
    
    public RefundCalculation CalculateRefund(
        OrderContext originalContext,
        ItemLevelDiscountBreakdown originalBreakdown,
        List<RefundItemRequest> refundItems,
        RefundMethod method)
    {
        var refund = new RefundCalculation
        {
            OriginalOrderId = originalContext.OrderId,
            OriginalCalculationId = originalBreakdown.CalculationId,
            RefundOrderId = $"REFUND-{originalContext.OrderId}",
            RefundMethod = method,
            CalculatedAt = DateTime.UtcNow
        };
        
        refund.OriginalTotalAmount = originalBreakdown.TotalOriginalAmount;
        refund.OriginalTotalDiscount = originalBreakdown.TotalDiscountAmount;
        
        foreach (var request in refundItems)
        {
            var originalItem = originalBreakdown.ItemDetails
                .FirstOrDefault(i => i.ProductId == request.ProductId);
            
            if (originalItem == null) continue;
            
            var refundDetail = CalculateRefundItemDetail(
                originalItem, 
                request, 
                method,
                originalBreakdown);
            
            refund.RefundItems.Add(refundDetail);
            refund.RefundAmount += refundDetail.RefundAmount;
            refund.RefundDiscount += refundDetail.RefundDiscount;
        }
        
        refund.RefundCoupons = CalculateRefundCoupons(refund.RefundItems, originalBreakdown, method);
        
        return refund;
    }
    
    private RefundItemDetail CalculateRefundItemDetail(
        ItemDiscountDetail originalItem,
        RefundItemRequest request,
        RefundMethod method,
        ItemLevelDiscountBreakdown originalBreakdown)
    {
        var detail = new RefundItemDetail
        {
            ProductId = originalItem.ProductId,
            ProductName = originalItem.ProductName,
            OriginalQuantity = originalItem.Quantity,
            RefundQuantity = request.RefundQuantity,
            OriginalAmount = originalItem.TotalOriginal,
            OriginalDiscount = originalItem.TotalDiscount
        };
        
        var refundRatio = (decimal)request.RefundQuantity / originalItem.Quantity;
        
        switch (method)
        {
            case RefundMethod.Proportional:
                detail.RefundAmount = originalItem.TotalOriginal * refundRatio;
                detail.RefundDiscount = originalItem.TotalDiscount * refundRatio;
                break;
                
            case RefundMethod.OriginalRatio:
                detail.RefundAmount = originalItem.TotalOriginal * refundRatio;
                detail.RefundDiscount = originalItem.TotalDiscount * refundRatio;
                break;
                
            case RefundMethod.Recalculate:
                detail.RefundAmount = originalItem.OriginalAmount * request.RefundQuantity;
                detail.RefundDiscount = originalItem.TotalDiscount * refundRatio;
                break;
                
            case RefundMethod.FixedAmount:
                detail.RefundAmount = request.FixedRefundAmount ?? originalItem.TotalOriginal * refundRatio;
                detail.RefundDiscount = 0;
                break;
        }
        
        foreach (var coupon in originalItem.CouponDiscounts)
        {
            var refundAmount = coupon.AllocatedAmount * refundRatio;
            
            detail.CouponRefunds.Add(new RefundCouponAllocation
            {
                CouponId = coupon.CouponId,
                CouponName = coupon.CouponName,
                OriginalAllocation = coupon.AllocatedAmount,
                RefundAmount = refundAmount,
                RefundRatio = refundRatio
            });
        }
        
        return detail;
    }
    
    private List<RefundCouponDetail> CalculateRefundCoupons(
        List<RefundItemDetail> refundItems,
        ItemLevelDiscountBreakdown originalBreakdown,
        RefundMethod method)
    {
        var refundCoupons = new List<RefundCouponDetail>();
        
        foreach (var allocation in originalBreakdown.CouponAllocations)
        {
            var totalRefund = refundItems
                .SelectMany(i => i.CouponRefunds)
                .Where(c => c.CouponId == allocation.CouponId)
                .Sum(c => c.RefundAmount);
            
            if (totalRefund > 0)
            {
                var refundRatio = totalRefund / allocation.TotalDiscountAmount;
                
                refundCoupons.Add(new RefundCouponDetail
                {
                    CouponId = allocation.CouponId,
                    CouponCode = allocation.CouponCode,
                    CouponName = allocation.CouponName,
                    OriginalDiscount = allocation.TotalDiscountAmount,
                    RefundDiscount = totalRefund,
                    HandleMethod = DetermineCouponHandleMethod(refundRatio, method),
                    IsReturned = refundRatio >= 1.0m,
                    IsPartiallyUsed = refundRatio < 1.0m && refundRatio > 0
                });
            }
        }
        
        return refundCoupons;
    }
    
    private string DetermineCouponHandleMethod(decimal refundRatio, RefundMethod method)
    {
        if (refundRatio >= 1.0m)
            return "优惠券全部退回，可重新使用";
        
        if (refundRatio > 0.5m)
            return "优惠券部分退回，剩余部分失效";
        
        return "优惠券部分退回，剩余部分继续有效";
    }
    
    private bool IsCouponApplicableToItem(string couponId, OrderItem item, CandidatePlan plan)
    {
        var coupon = plan.AppliedCoupons.FirstOrDefault(c => c.CouponId == couponId);
        if (coupon == null) return false;
        
        return true;
    }
    
    private decimal GetTotalApplicableAmount(string couponId, OrderContext context, CandidatePlan plan)
    {
        return context.OriginalAmount;
    }
    
    private int GetApplicableQuantity(string couponId, OrderContext context, CandidatePlan plan)
    {
        return context.Items.Sum(i => i.Quantity);
    }
    
    private string DetermineAllocationMethod(CouponType type)
    {
        return type switch
        {
            CouponType.AmountOff => "按金额比例分摊",
            CouponType.DiscountRate => "直接折扣",
            CouponType.QuantityDiscount => "按数量比例分摊",
            CouponType.MemberExclusive => "会员专享折扣",
            _ => "其他方式"
        };
    }
    
    private static string GetMemberLevelName(MemberLevel level)
    {
        return level switch
        {
            MemberLevel.Normal => "普通会员",
            MemberLevel.Silver => "银卡会员",
            MemberLevel.Gold => "金卡会员",
            MemberLevel.Platinum => "白金会员",
            MemberLevel.Diamond => "钻石会员",
            _ => level.ToString()
        };
    }
}

public class RefundItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int RefundQuantity { get; set; }
    public decimal? FixedRefundAmount { get; set; }
    public string RefundReason { get; set; } = string.Empty;
}