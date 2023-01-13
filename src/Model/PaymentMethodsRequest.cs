using System.Runtime.Serialization;
using Dynamicweb.Ecommerce.Orders;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class PaymentMethodsRequest : RequestBase
    {
        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public Amount Amount { get; set; }

        [DataMember(Name = "channel", EmitDefaultValue = false)]
        public string Channel { get; set; }

        [DataMember(Name = "countryCode", EmitDefaultValue = false)]
        public string CountryCode { get; set; }
        
        [DataMember(Name = "shopperReference", EmitDefaultValue = false)]
        public int CustomerUserId { get; set; }

        public PaymentMethodsRequest() : base()
        {
            Channel = "Web";
        }

        public PaymentMethodsRequest(Order order, string merchantName) : this(order, merchantName, false)
        {
        }

        public PaymentMethodsRequest(Order order, string merchantName, bool includeUserId) : base(order.Id, merchantName)
        {
            Channel = "Web";
            Amount = new Amount(order);
            CountryCode = order.CustomerCountryCode;
            if (includeUserId) // do not send user id to prevent auto-saving payment data on Adyen
            {
                CustomerUserId = order.CustomerAccessUserId;
            }
        }
    }
}
