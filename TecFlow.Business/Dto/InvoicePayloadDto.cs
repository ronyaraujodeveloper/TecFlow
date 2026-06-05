namespace TecFlow.Business.Dto;

/// <summary>Payload estruturado mockado para futura fila/gateway de NF-e.</summary>
public class InvoicePayloadDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public InvoiceIssuerDto Issuer { get; set; } = new();
    public InvoiceCustomerDto Customer { get; set; } = new();
    public List<InvoiceLineDto> Lines { get; set; } = new();
    public InvoiceTotalsDto Totals { get; set; } = new();
}

public class InvoiceIssuerDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
}

public class InvoiceCustomerDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
}

public class InvoiceLineDto
{
    public int ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class InvoiceTotalsDto
{
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
