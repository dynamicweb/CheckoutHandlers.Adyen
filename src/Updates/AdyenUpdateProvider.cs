using Dynamicweb.Updates;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Updates
{
    public class AdyenUpdateProvider : UpdateProvider
    {
        public override IEnumerable<Update> GetUpdates()
        {
            var type = GetType();

            return new List<Update>() {
                new FileUpdate("1", this, "/Files/Templates/eCom7/CheckoutHandler/Adyen/Form/Payments.cshtml", () => {
                    return type.Assembly.GetManifestResourceStream($"{type.Namespace}.Payments.cshtml");
                })
            };
        }
    }
}
