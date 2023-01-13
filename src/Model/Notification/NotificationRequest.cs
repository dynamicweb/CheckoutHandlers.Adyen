using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification
{
    [DataContract]
    public class NotificationRequest
    {
        [DataMember(Name = "live")]
        public string Live { get; set; }

        [DataMember(Name = "notificationItems")]
        public List<NotificationRequestItemContainer> NotificationItemContainers { get; set; }
    }
}
