using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class ResponseRedirect
    {
        public enum MethodType
        {
            [EnumMember(Value = "GET")]
            GET = 1,

            [EnumMember(Value = "POST")]
            POST = 2
        }

        [DataMember(Name = "method", EmitDefaultValue = false)]
        public MethodType? Method { get; set; }

        [DataMember(Name = "data", EmitDefaultValue = false)]
        public Dictionary<string, string> Data { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }
    }
}