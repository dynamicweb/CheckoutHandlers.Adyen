using Dynamicweb.Ecommerce.Orders;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class ShopperName
    {
        [DataMember(Name = "firstName", EmitDefaultValue = false)]
        public string FirstName { get; set; }

        [DataMember(Name = "gender", EmitDefaultValue = false)]
        public string Gender { get; set; }

        [DataMember(Name = "lastName", EmitDefaultValue = false)]
        public string LastName { get; set; }

        public ShopperName()
        {
            Gender = "UNKNOWN";
        }

        public ShopperName(Order order) : this()
        {
            FirstName = order.CustomerFirstName;
            LastName = order.CustomerName;
        }
    }
}