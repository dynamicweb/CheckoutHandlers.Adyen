using Dynamicweb.Core;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public abstract class RequestBase
    {
        [DataMember(Name = "merchantAccount")]
        public string MerchantAccount { get; set; }

        [DataMember(Name = "reference")]
        public string OrderId { get; set; }

        public RequestBase() { }

        protected RequestBase(string orderId, string merchantName)
        {
            MerchantAccount = merchantName;
            OrderId = orderId;
        }

        public virtual string ToJson() => Converter.Serialize(this);
    }
}
