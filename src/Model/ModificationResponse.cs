using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class ModificationResponse
    {
        public enum ModificationResultCode
        {
            CaptureReceived,
            CancelReceived,
            RefundReceived,
            CancelOrRefundReceived,
            AdjustAuthorisationReceived,
            VoidPendingRefundReceived
        }

        [DataMember(Name = "response", EmitDefaultValue = false)]
        public string Response { get; set; }

        [DataMember(Name = "additionalData", EmitDefaultValue = false)]
        public Dictionary<string, string> AdditionalData { get; set; }

        [DataMember(Name = "pspReference", EmitDefaultValue = false)]
        public string PspReference { get; set; }

        [DataMember(Name = "status", EmitDefaultValue = false)]
        public string Status { get; set; }

        [DataMember(Name = "errorCode", EmitDefaultValue = false)]
        public string ErrorCode { get; set; }

        [DataMember(Name = "errorType", EmitDefaultValue = false)]
        public string ErrorType { get; set; }

        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        public ModificationResultCode? GetResultCode()
        {
            // Our serializer cannot deserialize the value to ModificationResultCode enum, Newtonsoft throws parse exception.
            // Looks like because of '[' and ']' characters.
            // Newtonsoft v12 deserialize it correctly.
            switch (Response.ToLower())
            {
                case "[capture-received]": return ModificationResultCode.CaptureReceived;
                case "[cancel-received]": return ModificationResultCode.CancelReceived;
                case "[refund-received]": return ModificationResultCode.RefundReceived;
                case "[cancelOrRefund-received]": return ModificationResultCode.CancelOrRefundReceived;
                case "[adjustAuthorisation-received]": return ModificationResultCode.AdjustAuthorisationReceived;
                case "[voidPendingRefund-received]": return ModificationResultCode.VoidPendingRefundReceived;
                default: return null;
            }
        }
    }
}
