using Dynamicweb.Core;
using Dynamicweb.Core.Json.Settings;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public abstract class RequestBase
    {
        /// <summary>
        /// Compact serializer settings. Do not excludes default value.
        /// </summary>
        protected static readonly JsonSettings JsonSettings = new JsonSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        };

        [DataMember(Name = "merchantAccount")]
        public string MerchantAccount { get; set; }

        [DataMember(Name = "reference")]
        public string OrderId { get; set; }

        public RequestBase() { }

        protected RequestBase(string orderId, string merchantName)
        {
            MerchantAccount = merchantName;
            OrderId = orderId;
        }

        public virtual string ToJson() => Converter.Serialize(this, JsonSettings);
    }
}
