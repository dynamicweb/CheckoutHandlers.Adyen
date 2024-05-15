using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model;

[DataContract]
public class SavedPaymentMethod
{
    [DataMember(Name = "id", EmitDefaultValue = false)]
    public string CardToken { get; set; }

    [DataMember(Name = "type", EmitDefaultValue = false)]
    public string PaymentMethodType { get; set; }

    [DataMember(Name = "brand", EmitDefaultValue = false)]
    public string Brand { get; set; }

    [DataMember(Name = "expiryMonth", EmitDefaultValue = false)]
    public string ExpiryMonth { get; set; }

    [DataMember(Name = "expiryYear", EmitDefaultValue = false)]
    public string ExpiryYear { get; set; }

    [DataMember(Name = "holderName", EmitDefaultValue = false)]
    public string HolderName { get; set; }

    [DataMember(Name = "lastFour", EmitDefaultValue = false)]
    public string LastFour { get; set; }

    [DataMember(Name = "name", EmitDefaultValue = false)]
    public string Name { get; set; }

    [DataMember(Name = "networkTxReference", EmitDefaultValue = false)]
    public string NetworkTxReference { get; set; }

    [DataMember(Name = "supportedShopperInteractions", EmitDefaultValue = false)]
    public string[] SupportedShopperInteractions { get; set; }

    [DataMember(Name = "supportedRecurringProcessingModels", EmitDefaultValue = false)]
    public string[] SupportedRecurringProcessingModels { get; set; }

    public PaymentMethod ToPaymentMethod()
    {
        return new()
        {
            Type = PaymentMethodType,
            StoredPaymentMethodId = CardToken,
        };
    }
}
