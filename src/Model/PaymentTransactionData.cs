using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class PaymentTransactionData
    {
        [DataMember(Name = "keys")]
        public IList<string> RequestKeys { get; set; }

        [DataMember(Name = "paymentData")]
        public string PaymentData { get; set; }

        public PaymentTransactionData()
        {
            RequestKeys = new List<string>();
        }

        public PaymentTransactionData(string paymentData) : this()
        {
            PaymentData = paymentData;
        }
    }
}
