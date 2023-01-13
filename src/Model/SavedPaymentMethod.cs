using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class SavedPaymentMethod
    {
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string CardToken { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string PaymentMethodType { get; set; }

        public PaymentMethod ToPaymentMethod()
        {
            return new PaymentMethod
            {
                Type = PaymentMethodType,
                StoredPaymentMethodId = CardToken,
            };
        }
    }
}
