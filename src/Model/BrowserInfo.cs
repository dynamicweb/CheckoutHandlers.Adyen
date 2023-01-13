using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class BrowserInfo
    {
        [DataMember(Name = "acceptHeader")]
        public string AcceptHeader { get; set; }

        [DataMember(Name = "colorDepth")]
        public int ColorDepth { get; set; }

        [DataMember(Name = "javaEnabled")]
        public bool JavaEnabled { get; set; }

        [DataMember(Name = "javaScriptEnabled", EmitDefaultValue = false)]
        public bool JavaScriptEnabled { get; set; }

        [DataMember(Name = "language")]
        public string Language { get; set; }

        [DataMember(Name = "screenHeight")]
        public int ScreenHeight { get; set; }

        [DataMember(Name = "screenWidth")]
        public int ScreenWidth { get; set; }

        [DataMember(Name = "timeZoneOffset")]
        public int TimeZoneOffset { get; set; }

        [DataMember(Name = "userAgent")]
        public string UserAgent { get; set; }
    }
}
