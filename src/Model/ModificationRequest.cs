using System.Runtime.Serialization;
using Dynamicweb.Ecommerce.Orders;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class ModificationRequest : RequestBase
    {
        [DataMember(Name = "modificationAmount", EmitDefaultValue = false)]
        public Amount Amount { get; set; }

        [DataMember(Name = "originalReference", EmitDefaultValue = false)]
        public string TransactionNumber { get; set; }

        public ModificationRequest() : base() 
        { 
        }

        public ModificationRequest(Order order, string merchantName, bool includeAmount) : base(order.Id, merchantName)
        {
            if (includeAmount) // cancel request must not contain amount information
            {
                Amount = new Amount(order);
            }
            TransactionNumber = order.TransactionNumber;
        }

        public ModificationRequest(Order order, string merchantName, long amount) : base(order.Id, merchantName)
        {
            Amount = new Amount
            {
                Currency = order.CurrencyCode,
                Value = amount,
            };
            TransactionNumber = order.TransactionNumber;
        }
    }
}
