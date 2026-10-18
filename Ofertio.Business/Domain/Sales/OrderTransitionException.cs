namespace Ofertio.Business.Domain.Sales;

public class OrderTransitionException : InvalidOperationException
{
    public OrderTransitionException(string message) : base(message)
    {
    }
}
