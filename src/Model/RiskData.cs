using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class RiskData
    {
        [DataMember(Name = "clientData", EmitDefaultValue = false)]
        public string ClientData { get; set; }
    }
}
