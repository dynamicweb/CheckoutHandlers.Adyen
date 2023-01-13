using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification
{
    [DataContract]
    public class NotificationRequestItemContainer
    {
        [DataMember(Name = "NotificationRequestItem")]
        public NotificationRequestItem NotificationItem { get; set; }
    }
}
