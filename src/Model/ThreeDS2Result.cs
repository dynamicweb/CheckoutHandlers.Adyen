using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class ThreeDS2Result
    {
        [DataMember(Name = "authenticationValue", EmitDefaultValue = false)]
        public string AuthenticationValue { get; set; }

        [DataMember(Name = "eci", EmitDefaultValue = false)]
        public string ECI { get; set; }

        [DataMember(Name = "threeDSServerTransID", EmitDefaultValue = false)]
        public string ThreeDSServerTransID { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public string TimeStamp { get; set; }

        [DataMember(Name = "transStatus")]
        public string TransStatus { get; set; }

        [DataMember(Name = "dsTransID")]
        public string DsTransID { get; set; }

        [DataMember(Name = "transStatusReason", EmitDefaultValue = false)]
        public string TransStatusReason { get; set; }
        
        [DataMember(Name = "messageVersion", EmitDefaultValue = false)]
        public string MessageVersion { get; set; }
    }
}