using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Environment;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model
{
    [DataContract]
    public class PaymentRequest : RequestBase
    {
        [DataMember(Name = "amount")]
        public Amount Amount { get; set; }

        [DataMember(Name = "billingAddress", EmitDefaultValue = false)]
        public BillingAddress BillingAddress { get; set; }

        [DataMember(Name = "browserInfo", EmitDefaultValue = false)]
        public BrowserInfo BrowserInfo { get; set; }

        [DataMember(Name = "channel", EmitDefaultValue = false)]
        public string Channel { get; set; }

        [DataMember(Name = "countryCode", EmitDefaultValue = false)]
        public string CountryCode { get; set; }

        [DataMember(Name = "lineItems", EmitDefaultValue = false)]
        public IEnumerable<PaymentOrderLine> LineItems { get; set; }

        [DataMember(Name = "paymentMethod")]
        public PaymentMethod PaymentMethod { get; set; }

        [DataMember(Name = "returnUrl")]
        public string ReturnUrl { get; set; }

        [DataMember(Name = "riskData", EmitDefaultValue = false)]
        public RiskData RiskData { get; set; }

        [DataMember(Name = "shopperEmail", EmitDefaultValue = false)]
        public string ShopperEmail { get; set; }

        [DataMember(Name = "shopperIP", EmitDefaultValue = false)]
        public string ShopperIP { get; set; }

        [DataMember(Name = "shopperLocale", EmitDefaultValue = false)]
        public string ShopperLocale { get; set; }

        [DataMember(Name = "shopperName", EmitDefaultValue = false)]
        public ShopperName ShopperName { get; set; }

        [DataMember(Name = "shopperReference", EmitDefaultValue = false)]
        public string ShopperReference { get; set; }

        [DataMember(Name = "telephoneNumber", EmitDefaultValue = false)]
        public string TelephoneNumber { get; set; }

        [DataMember(Name = "storePaymentMethod")]
        public bool StorePaymentMethod { get; set; }

        [DataMember(Name = "recurringProcessingModel", EmitDefaultValue = false)]
        public string RecurringProcessingModel { get; set; }

        [DataMember(Name = "shopperInteraction", EmitDefaultValue = false)]
        public string ShopperInteraction { get; set; }

        public PaymentRequest() : base()
        {
            Channel = "Web";
        }

        public PaymentRequest(Order order, PaymentMethodData paymentMethodData, string merchantName, string callbackUrl)
            : this(order, paymentMethodData, merchantName, callbackUrl, PaymentRequestType.Default)
        {
        }

        public PaymentRequest(Order order, PaymentMethodData paymentMethodData, string merchantName, string callbackUrl, PaymentRequestType additionalAction) : base(order.Id, merchantName)
        {
            Channel = "Web";
            Amount = new Amount(order);
            BillingAddress = new BillingAddress(order);
            BrowserInfo = paymentMethodData.BrowserInfo;
            CountryCode = order.CustomerCountryCode;
            LineItems = ConvertOrderLines(order, order.OrderLines);
            PaymentMethod = paymentMethodData.PaymentMethod;
            ReturnUrl = callbackUrl;
            RiskData = paymentMethodData.RiskData;
            ShopperEmail = order.CustomerEmail;
            ShopperIP = order.Ip;
            ShopperLocale = ExecutingContext.GetCulture(true).Name;
            ShopperName = new ShopperName(order);
            TelephoneNumber = order.CustomerPhone;

            switch (additionalAction)
            {
                case PaymentRequestType.UseSavedCard:
                    ShopperReference = $"user ID:{order.CustomerAccessUserId}";
                    RecurringProcessingModel = "CardOnFile";
                    ShopperInteraction = "ContAuth";
                    break;

                case PaymentRequestType.SaveCard:
                    ShopperReference = $"user ID:{order.CustomerAccessUserId}";
                    RecurringProcessingModel = "CardOnFile";
                    ShopperInteraction = "Ecommerce";
                    StorePaymentMethod = true;
                    break;

                case PaymentRequestType.Default:
                default:
                    ShopperReference = null;
                    RecurringProcessingModel = null;
                    ShopperInteraction = null;
                    StorePaymentMethod = false;
                    break;
            }
        }

        internal static IEnumerable<PaymentOrderLine> ConvertOrderLines(Order order, OrderLineCollection orderLines)
        {
            var result = new List<PaymentOrderLine>();
            foreach (var orderLine in orderLines)
            {
                if (orderLine.OrderLineType != OrderLineType.Product)
                {
                    continue;
                }
                result.Add(new PaymentOrderLine
                {
                    Id = orderLine.Id,
                    Description = orderLine.Product.Name,
                    Quantity = Converter.ToInt32(orderLine.Quantity),
                    AmountExcludingTax = PriceHelper.ConvertToPIP(order.Currency, orderLine.UnitPrice.PriceWithoutVAT),
                    AmountIncludingTax = PriceHelper.ConvertToPIP(order.Currency, orderLine.UnitPrice.PriceWithVAT),
                    TaxAmount = PriceHelper.ConvertToPIP(order.Currency, orderLine.UnitPrice.VAT),
                    TaxPercentage = PriceHelper.ConvertToPIP(order.Currency, orderLine.Price.VATPercent),
                });
            }

            return result;
        }
    }
}
