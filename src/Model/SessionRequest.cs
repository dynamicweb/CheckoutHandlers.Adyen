using Dynamicweb.Ecommerce.Orders;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class SessionRequest : RequestBase
    {
        [DataMember(Name = "amount")]
        public Amount Amount { get; set; }

        [DataMember(Name = "channel", EmitDefaultValue = false)]
        public string Channel { get; set; }

        [DataMember(Name = "countryCode", EmitDefaultValue = false)]
        public string CountryCode { get; set; }

        [DataMember(Name = "returnUrl")]
        public string ReturnUrl { get; set; }

        [DataMember(Name = "shopperReference", EmitDefaultValue = false)]
        public string ShopperReference { get; set; }

        [DataMember(Name = "enablePayOut", EmitDefaultValue = true)]
        public bool EnablePayOut { get; set; }

        [DataMember(Name = "enableOneClick", EmitDefaultValue = true)]
        public bool EnableOneClick { get; set; }

        [DataMember(Name = "lineItems", EmitDefaultValue = false)]
        public IEnumerable<PaymentOrderLine> LineItems { get; set; }

        [DataMember(Name = "shopperName", EmitDefaultValue = false)]
        public ShopperName ShopperName { get; set; }

        public SessionRequest() : base()
        {
            Channel = "Web";
        }


        public SessionRequest(Order order, string merchantName, string callbackUrl) : base(order.Id, merchantName)
        {
            Channel = "Web";
            Amount = new Amount(order);
            CountryCode = order.CustomerCountryCode;
            ReturnUrl = callbackUrl;
            ShopperName = new ShopperName(order);
            LineItems = PaymentRequest.ConvertOrderLines(order, order.OrderLines);
        }
    }
}
