using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class CheckoutPaymentsAction
    {
        public enum CheckoutActionType
        {
            [EnumMember(Value = "await")]
            Await = 1,
            
            [EnumMember(Value = "donate")]
            Donate = 2,
            
            [EnumMember(Value = "qrCode")]
            QrCode = 3,
            
            [EnumMember(Value = "redirect")]
            Redirect = 4,
            
            [EnumMember(Value = "sdk")]
            Sdk = 5,
            
            [EnumMember(Value = "threeDS2Challenge")]
            ThreeDS2Challenge = 6,
            
            [EnumMember(Value = "threeDS2Fingerprint")]
            ThreeDS2Fingerprint = 7,
            
            [EnumMember(Value = "voucher")]
            Voucher = 8,
            
            [EnumMember(Value = "wechatpaySDK")]
            WechatpaySDK = 9
        }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public CheckoutActionType? Type { get; set; }

        [DataMember(Name = "alternativeReference", EmitDefaultValue = false)]
        public string AlternativeReference { get; set; }

        [DataMember(Name = "data", EmitDefaultValue = false)]
        public object Data { get; set; }

        [DataMember(Name = "downloadUrl", EmitDefaultValue = false)]
        public string DownloadUrl { get; set; }

        [DataMember(Name = "expiresAt", EmitDefaultValue = false)]
        public string ExpiresAt { get; set; }

        [DataMember(Name = "entity", EmitDefaultValue = false)]
        public string Entity { get; set; }
        
        [DataMember(Name = "initialAmount", EmitDefaultValue = false)]
        public Amount InitialAmount { get; set; }

        [DataMember(Name = "instructionsUrl", EmitDefaultValue = false)]
        public string InstructionsUrl { get; set; }

        [DataMember(Name = "issuer", EmitDefaultValue = false)]
        public string Issuer { get; set; }

        [DataMember(Name = "maskedTelephoneNumber", EmitDefaultValue = false)]
        public string MaskedTelephoneNumber { get; set; }

        [DataMember(Name = "merchantName", EmitDefaultValue = false)]
        public string MerchantName { get; set; }

        [DataMember(Name = "merchantReference", EmitDefaultValue = false)]
        public string MerchantReference { get; set; }

        [DataMember(Name = "method", EmitDefaultValue = false)]
        public string Method { get; set; }

        [DataMember(Name = "paymentData", EmitDefaultValue = false)]
        public string PaymentData { get; set; }

        [DataMember(Name = "paymentMethodType", EmitDefaultValue = false)]
        public string PaymentMethodType { get; set; }

        [DataMember(Name = "subtype", EmitDefaultValue = false)]
        public string SubType { get; set; }

        [DataMember(Name = "qrCodeData", EmitDefaultValue = false)]
        public string QrCodeData { get; set; }

        [DataMember(Name = "reference", EmitDefaultValue = false)]
        public string Reference { get; set; }

        [DataMember(Name = "shopperEmail", EmitDefaultValue = false)]
        public string ShopperEmail { get; set; }

        [DataMember(Name = "shopperName", EmitDefaultValue = false)]
        public string ShopperName { get; set; }

        [DataMember(Name = "surcharge", EmitDefaultValue = false)]
        public Amount Surcharge { get; set; }

        [DataMember(Name = "token", EmitDefaultValue = false)]
        public string Token { get; set; }

        [DataMember(Name = "totalAmount", EmitDefaultValue = false)]
        public Amount TotalAmount { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }
    }
}