using System;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen
{
    internal class AdyenUrlManager
    {
        private const string ApiVersion = "v66";
        private const string RecurringApiVersion = "v49"; // Recurring API have different version
        private const string JsCssVersion = "3.10.1";

        private EnvironmentType _environment;
        private string _liveEndpointUrlPrefix;

        private bool IsTest => _environment == EnvironmentType.Test;

        public AdyenUrlManager(EnvironmentType environment, string liveEndpointUrlPrefix)
        {
            _environment = environment;
            _liveEndpointUrlPrefix = liveEndpointUrlPrefix;
        }

        public string GetCssUrl()
        {
            return IsTest
                ? string.Format("https://checkoutshopper-test.adyen.com/checkoutshopper/sdk/{0}/adyen.css", JsCssVersion)
                : string.Format("https://checkoutshopper-live.adyen.com/checkoutshopper/sdk/{0}/adyen.css", JsCssVersion);
        }

        public string GetJavaScriptUrl()
        {
            return IsTest
                ? string.Format("https://checkoutshopper-test.adyen.com/checkoutshopper/sdk/{0}/adyen.js", JsCssVersion)
                : string.Format("https://checkoutshopper-live.adyen.com/checkoutshopper/sdk/{0}/adyen.js", JsCssVersion);
        }

        public string GetPaymentMethodsUrl() => GetCheckoutEndpointUrl("paymentMethods");

        public string GetPaymentUrl() => GetCheckoutEndpointUrl("payments");

        public string GetPaymentDetailsUrl() => GetCheckoutEndpointUrl("payments/details");

        public string GetCaptureUrl() => GetStandardEndpointUrl("capture");

        public string GetCancelUrl() => GetStandardEndpointUrl("cancel");

        public string GetRefundUrl() => GetStandardEndpointUrl("refund");

        public string GetSavedPaymentMethodRemoveUrl() => GetRecurringEndpointUrl("disable");

        private string GetCheckoutEndpointUrl(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(nameof(method));
            }

            method = method.TrimStart(new[] { '/', '\\' });
            return IsTest
                ? string.Format("https://checkout-test.adyen.com/{0}/{1}", ApiVersion, method)
                : string.Format("https://{0}-checkout-live.adyenpayments.com/checkout/{1}/{2}", _liveEndpointUrlPrefix, ApiVersion, method);
        }

        private string GetStandardEndpointUrl(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(nameof(method));
            }

            method = method.TrimStart(new[] { '/', '\\' });

            return IsTest
                ? string.Format("https://pal-test.adyen.com/pal/servlet/Payment/{0}/{1}", ApiVersion, method)
                : string.Format("https://{0}-pal-live.adyenpayments.com/pal/servlet/Payment/{1}/{2}", _liveEndpointUrlPrefix, ApiVersion, method);
        }

        private string GetRecurringEndpointUrl(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(nameof(method));
            }

            method = method.TrimStart(new[] { '/', '\\' });

            return IsTest
                ? string.Format("https://pal-test.adyen.com/pal/servlet/Recurring/{0}/{1}", RecurringApiVersion, method)
                : string.Format("https://{0}-pal-live.adyenpayments.com/pal/servlet/Recurring/{1}/{2}", _liveEndpointUrlPrefix, RecurringApiVersion, method);
        }
    }
}
