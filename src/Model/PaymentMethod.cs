using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class PaymentMethod
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "number", EmitDefaultValue = false)]
        public string Number { get; set; }

        [DataMember(Name = "expiryMonth", EmitDefaultValue = false)]
        public string ExpiryMonth { get; set; }

        [DataMember(Name = "expiryYear", EmitDefaultValue = false)]
        public string ExpiryYear { get; set; }

        [DataMember(Name = "holderName", EmitDefaultValue = false)]
        public string HolderName { get; set; }

        [DataMember(Name = "cvc", EmitDefaultValue = false)]
        public string Cvc { get; set; }

        [DataMember(Name = "installmentConfigurationKey", EmitDefaultValue = false)]
        public string InstallmentConfigurationKey { get; set; }

        [DataMember(Name = "encryptedCardNumber", EmitDefaultValue = false)]
        public string EncryptedCardNumber { get; set; }

        [DataMember(Name = "encryptedExpiryMonth", EmitDefaultValue = false)]
        public string EncryptedExpiryMonth { get; set; }

        [DataMember(Name = "encryptedExpiryYear", EmitDefaultValue = false)]
        public string EncryptedExpiryYear { get; set; }

        [DataMember(Name = "encryptedSecurityCode", EmitDefaultValue = false)]
        public string EncryptedSecurityCode { get; set; }

        [DataMember(Name = "storedPaymentMethodId", EmitDefaultValue = false)]
        public string StoredPaymentMethodId { get; set; }

        [DataMember(Name = "storeDetails", EmitDefaultValue = false)]
        public bool StoreDetails { get; set; }

        [DataMember(Name = "issuer", EmitDefaultValue = false)]
        public string Issuer { get; set; }

        [DataMember(Name = "sepa.ownerName", EmitDefaultValue = false)]
        public string SepaOwnerName { get; set; }

        [DataMember(Name = "sepa.ibanNumber", EmitDefaultValue = false)]
        public string SepaIbanNumber { get; set; }

        [DataMember(Name = "additionalData.applepay.token", EmitDefaultValue = false)]
        public string ApplePayToken { get; set; }

        [DataMember(Name = "paywithgoogle.token", EmitDefaultValue = false)]
        public string GooglePayToken { get; set; }
    }
}
