using Dynamicweb.Ecommerce.Cart;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class RemoveSavedPaymentMethodRequest : RequestBase
    {
        [DataMember(Name = "shopperReference")]
        public string UserId { get; set; }

        public RemoveSavedPaymentMethodRequest(PaymentCardToken savedMethod, string merchantName) : base(null, merchantName)
        {
            UserId = savedMethod.UserID.ToString();
        }
    }
}
