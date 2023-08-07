using Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class ModificationResponse
    {
        [DataMember(Name = "merchantAccount", EmitDefaultValue = false)]
        public string MerchantAccount { get; set; }

        [DataMember(Name = "paymentPspReference", EmitDefaultValue = false)]
        public string PaymentPspReference { get; set; }

        [DataMember(Name = "pspReference", EmitDefaultValue = false)]
        public string PspReference { get; set; }

        [DataMember(Name = "reference", EmitDefaultValue = false)]
        public string Reference { get; set; }

        [DataMember(Name = "status", EmitDefaultValue = false)]
        public string Status { get; set; }

        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public Amount Amount { get; set; }

        [DataMember(Name = "notificationItems", EmitDefaultValue = false)]
        public List<NotificationRequestItem> NotificationItems { get; set; }
    }
}
