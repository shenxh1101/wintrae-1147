namespace CouponCalculator.Models;

public enum CouponType
{
    AmountOff,
    DiscountRate,
    QuantityDiscount,
    FreeShipping,
    MemberExclusive
}

public enum MemberLevel
{
    Normal = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3,
    Diamond = 4
}

public enum DisplayFormat
{
    Simple,
    Detailed,
    Bill
}
