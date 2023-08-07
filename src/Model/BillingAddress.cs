using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Orders;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class BillingAddress
    {
        [DataMember(Name = "city")]
        public string City { get; set; }

        [DataMember(Name = "country")]
        public string Country { get; set; }

        [DataMember(Name = "houseNumberOrName")]
        public string HouseNumberOrName { get; set; }

        [DataMember(Name = "postalCode")]
        public string PostalCode { get; set; }

        [DataMember(Name = "stateOrProvince", EmitDefaultValue = false)]
        public string StateOrProvince { get; set; }

        [DataMember(Name = "street")]
        public string Street { get; set; }

        public BillingAddress() { }

        public BillingAddress(Order order)
        {
            City = Converter.ToString(order.CustomerCity);
            Country = Converter.ToString(order.CustomerCountryCode);
            HouseNumberOrName = Converter.ToString(order.CustomerHouseNumber);
            PostalCode = Converter.ToString(order.CustomerZip);
            StateOrProvince = order.CustomerRegion;
            Street = Converter.ToString(order.CustomerAddress);
        }
    }
}