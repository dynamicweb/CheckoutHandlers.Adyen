using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class PaymentResponse
    {
        /// <summary>
        /// The result of the payment.
        /// </summary>
        public enum PaymentResultCode
        {
            /// <summary>
            /// The payment has been successfully authenticated with 3D Secure 2. Returned for 3D Secure 2 authentication-only transactions.
            /// </summary>
            [EnumMember(Value = "AuthenticationFinished")]
            AuthenticationFinished = 0,

            /// <summary>
            /// The payment was successfully authorised. This state serves as an indicator to proceed with the delivery of goods and services. This is a final state.
            /// </summary>
            [EnumMember(Value = "Authorised")]
            Authorised = 1,

            /// <summary>
            /// Indicates the payment has been cancelled (either by the shopper or the merchant) before processing was completed. This is a final state.
            /// </summary>
            [EnumMember(Value = "Cancelled")]
            Cancelled = 2,

            /// <summary>
            /// The issuer requires further shopper interaction before the payment can be authenticated. Returned for 3D Secure 2 transactions.
            /// </summary>
            [EnumMember(Value = "ChallengeShopper")]
            ChallengeShopper = 3,

            /// <summary>
            /// There was an error when the payment was being processed. The reason is given in the &#x60;refusalReason&#x60; field. This is a final state.
            /// </summary>
            [EnumMember(Value = "Error")]
            Error = 4,

            /// <summary>
            /// The issuer requires the shopper&#x27;s device fingerprint before the payment can be authenticated. Returned for 3D Secure 2 transactions.
            /// </summary>
            [EnumMember(Value = "IdentifyShopper")]
            IdentifyShopper = 5,

            /// <summary>
            /// Indicates that it is not possible to obtain the final status of the payment. This can happen if the systems providing final status information for the payment are unavailable, or if the shopper needs to take further action to complete the payment. 
            /// For more information on handling a pending payment, refer to [Payments with pending status](https://docs.adyen.com/development-resources/payments-with-pending-status).
            /// </summary>
            [EnumMember(Value = "Pending")]
            Pending = 6,

            /// <summary>
            /// Indicates the payment has successfully been received by Adyen, and will be processed. This is the initial state for all payments.
            /// </summary>
            [EnumMember(Value = "Received")]
            Received = 7,

            /// <summary>
            /// Indicates the shopper should be redirected to an external web page or app to complete the authorisation.
            /// </summary>
            [EnumMember(Value = "RedirectShopper")]
            RedirectShopper = 8,

            /// <summary>
            /// Indicates the payment was refused. The reason is given in the &#x60;refusalReason&#x60; field. This is a final state.
            /// </summary>
            [EnumMember(Value = "Refused")]
            Refused = 9,

            [EnumMember(Value = "PresentToShopper")]
            PresentToShopper = 10,

            [EnumMember(Value = "AuthenticationNotRequired")]
            AuthenticationNotRequired = 11
        }

        [DataMember(Name = "resultCode", EmitDefaultValue = false)]
        public PaymentResultCode? ResultCode { get; set; }

        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public Amount Amount { get; set; }

        [DataMember(Name = "additionalData", EmitDefaultValue = false)]
        public Dictionary<string, string> AdditionalData { get; set; }

        [DataMember(Name = "authentication", EmitDefaultValue = false)]
        public Dictionary<string, string> Authentication { get; set; }

        [DataMember(Name = "details", EmitDefaultValue = false)]
        public List<InputDetail> Details { get; set; }

        [DataMember(Name = "merchantReference", EmitDefaultValue = false)]
        public string MerchantReference { get; set; }

        [DataMember(Name = "outputDetails", EmitDefaultValue = false)]
        public Dictionary<string, string> OutputDetails { get; set; }

        [DataMember(Name = "paymentData", EmitDefaultValue = false)]
        public string PaymentData { get; set; }

        [DataMember(Name = "pspReference", EmitDefaultValue = false)]
        public string PspReference { get; set; }

        [DataMember(Name = "redirect", EmitDefaultValue = false)]
        public ResponseRedirect Redirect { get; set; }

        [DataMember(Name = "refusalReason", EmitDefaultValue = false)]
        public string RefusalReason { get; set; }

        [DataMember(Name = "refusalReasonCode", EmitDefaultValue = false)]
        public string RefusalReasonCode { get; set; }

        [DataMember(Name = "action", EmitDefaultValue = false)]
        public CheckoutPaymentsAction Action { get; set; }

        [DataMember(Name = "threeDS2Result", EmitDefaultValue = false)]
        public ThreeDS2Result ThreeDS2Result { get; set; }

        [DataMember(Name = "serviceError", EmitDefaultValue = false)]
        public ServiceError ServiceError { get; set; }
    }
}
