using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class PaymentOrderLine
    {
        /// <summary>
        /// Item amount excluding the tax, in minor units (PricePIP).
        /// </summary>
        [DataMember(Name = "amountExcludingTax", EmitDefaultValue = false)]
        public long AmountExcludingTax { get; set; }

        /// <summary>
        /// Item amount including the tax, in minor units (PricePIP).
        /// </summary>
        [DataMember(Name = "amountIncludingTax", EmitDefaultValue = false)]
        public long AmountIncludingTax { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "quantity", EmitDefaultValue = false)]
        public int Quantity { get; set; }

        /// <summary>
        /// Tax amount, in minor units (PricePIP).
        /// </summary>
        [DataMember(Name = "taxAmount", EmitDefaultValue = false)]
        public long TaxAmount { get; set; }

        /// <summary>
        /// Tax percentage, in minor units (PricePIP).
        /// </summary>
        [DataMember(Name = "taxPercentage", EmitDefaultValue = false)]
        public long TaxPercentage { get; set; }
    }
}
