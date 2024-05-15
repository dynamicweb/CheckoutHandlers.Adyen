using Dynamicweb.Updates;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Updates;

public class AdyenUpdateProvider : UpdateProvider
{
    private static Stream GetResourceStream(string name)
    {
        string resourceName = $"Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Updates.{name}";

        return Assembly.GetAssembly(typeof(AdyenUpdateProvider)).GetManifestResourceStream(resourceName);
    }

    public override IEnumerable<Update> GetUpdates()
    {
        return new List<Update>() {
            new FileUpdate("3cd0b762-78fd-42cd-ad96-9d8729690bf8", this, "/Files/Templates/eCom7/CheckoutHandler/Adyen/Form/Payments.cshtml", () => GetResourceStream("Payments.cshtml")),
            new FileUpdate("8b3cdbb9-d5d4-46a9-b323-33b266149448", this, "/Files/Templates/eCom7/CheckoutHandler/Adyen/Card/Card.cshtml", () => GetResourceStream("Card.cshtml"))
        };
    }

    /*
     * IMPORTANT!
     * Use a generated GUID string as id for an update
     * - Execute command in C# interactive window: Guid.NewGuid().ToString()
     */
}
