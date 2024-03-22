using System;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen
{
    internal class AdyenUrlManager
    {
        private const string ApiVersion = "v71";
        private const string JsCssVersion = "5.59.0";

        private EnvironmentType _environment;
        private string _liveEndpointUrlPrefix;

        private bool IsTest => _environment == EnvironmentType.Test;

        public string JsIntegrityKey => "sha384-O8p0CLZyOw1jkmYN7ZwJxWzd+sDYRFGpLEffqc+dKye24gFImbU72did4PC7ysTY";

        public string CssIntegrityKey => "sha384-zgFNrGzbwuX5qJLys75cOUIGru/BoEzhGMyC07I3OSdHqXuhUfoDPVG03G+61oF4";

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

        public string GetSessionsUrl() => GetCheckoutEndpointUrl("sessions");

        public string GetPaymentMethodsUrl() => GetCheckoutEndpointUrl("paymentMethods");

        public string GetPaymentUrl() => GetCheckoutEndpointUrl("payments");

        public string GetPaymentDetailsUrl() => GetCheckoutEndpointUrl("payments/details");

        public string GetCaptureUrl(string paymentPspReference) => GetCheckoutEndpointUrl($"/payments/{paymentPspReference}/captures");

        public string GetCancelUrl(string paymentPspReference) => GetCheckoutEndpointUrl($"/payments/{paymentPspReference}/cancels");

        public string GetRefundUrl(string paymentPspReference) => GetCheckoutEndpointUrl($"/payments/{paymentPspReference}/refunds");

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

        private string GetRecurringEndpointUrl(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(nameof(method));
            }

            method = method.TrimStart(new[] { '/', '\\' });

            return IsTest
                ? string.Format("https://pal-test.adyen.com/pal/servlet/Recurring/{0}/{1}", ApiVersion, method)
                : string.Format("https://{0}-pal-live.adyenpayments.com/pal/servlet/Recurring/{1}/{2}", _liveEndpointUrlPrefix, ApiVersion, method);
        }
    }
}
