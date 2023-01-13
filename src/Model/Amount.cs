using Dynamicweb.Ecommerce.Orders;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class Amount
    {
        /// <summary>
        /// The three-character [ISO currency code](https://docs.adyen.com/developers/development-resources/currency-codes).
        /// </summary>
        [DataMember(Name = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// The transaction amount needs to be represented in minor units according to the [following table](https://docs.adyen.com/developers/development-resources/currency-codes).
        /// </summary>
        [DataMember(Name = "value")]
        public long? Value { get; set; }

        public Amount() { }

        public Amount(Order order)
        {
            Currency = order.CurrencyCode;
            Value = order.Price.PricePIP;
        }
    }
}
