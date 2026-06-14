namespace CouponCalculator.Models;

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public bool IsShippingRequired { get; set; } = true;
    public Dictionary<string, object> ExtendedData { get; set; } = new();

    public decimal TotalAmount => Price * Quantity;
}
