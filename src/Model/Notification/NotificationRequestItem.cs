using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification
{
    [DataContract]
    public class NotificationRequestItem
    {
        public const string HmacSignatureKey = "hmacSignature";

        [DataMember(Name = "additionalData")]
        public Dictionary<string, string> AdditionalData { get; set; }

        [DataMember(Name = "amount")]
        public Amount Amount { get; set; }

        [DataMember(Name = "eventCode")]
        public string EventCode { get; set; }

        [DataMember(Name = "eventDate")]
        public string EventDate { get; set; }

        [DataMember(Name = "merchantAccountCode")]
        public string MerchantAccountCode { get; set; }

        [DataMember(Name = "merchantReference")]
        public string MerchantReference { get; set; }

        [DataMember(Name = "operations")]
        public List<string> Operations { get; set; }

        [DataMember(Name = "originalReference")]
        public string OriginalReference { get; set; }

        [DataMember(Name = "paymentMethod")]
        public string PaymentMethod { get; set; }

        [DataMember(Name = "pspReference")]
        public string PspReference { get; set; }

        [DataMember(Name = "reason")]
        public string Reason { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }

        public NotificationEventCode? GetNotificationEventCode()
        {
            switch (EventCode)
            {
                case "AUTHORISATION": return NotificationEventCode.Authorisation;
                case "CANCELLATION": return NotificationEventCode.Cancellation;
                case "CAPTURE": return NotificationEventCode.Capture;
                case "CAPTURE_FAILED": return NotificationEventCode.CaptureFailed;
                case "REFUND": return NotificationEventCode.Refund;
                case "REFUND_FAILED": return NotificationEventCode.RefundFailed;
                default: return null;
            }
        }
    }
}
