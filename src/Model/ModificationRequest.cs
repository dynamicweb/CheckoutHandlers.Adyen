using Dynamicweb.Ecommerce.Orders;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class ModificationRequest : RequestBase
    {
        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public Amount Amount { get; set; }

        public ModificationRequest() : base()
        {
        }

        public ModificationRequest(Order order, string merchantName, bool includeAmount) : base(order.Id, merchantName)
        {
            if (includeAmount) // cancel request must not contain amount information
            {
                Amount = new Amount(order);
            }
        }

        public ModificationRequest(Order order, string merchantName, long amount) : base(order.Id, merchantName)
        {
            Amount = new Amount
            {
                Currency = order.CurrencyCode,
                Value = amount,
            };
        }
    }
}
