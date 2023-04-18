using System;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class SessionsResponse
    {
        [DataMember(Name = "amount")]
        public Amount Amount { get; set; }

        [DataMember(Name = "merchantAccount")]
        public string MerchantAccount { get; set; }

        [DataMember(Name = "reference")]
        public string OrderId { get; set; }

        [DataMember(Name = "countryCode", EmitDefaultValue = false)]
        public string CountryCode { get; set; }

        [DataMember(Name = "returnUrl")]
        public string ReturnUrl { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "sessionData")]
        public string SessionData { get; set; }

        [DataMember(Name = "expiresAt")]
        public DateTime ExpiresAt { get; set; }

    }
}
