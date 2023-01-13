using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class PaymentMethodsResponse
    {
        [DataMember(Name = "storedPaymentMethods", EmitDefaultValue = false)]
        public IEnumerable<SavedPaymentMethod> SavedPaymentMethods { get; set; }
    }
}
