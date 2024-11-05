using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model;
using Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Frontend;
using Dynamicweb.Logging;
using Dynamicweb.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen
{
    [AddInName("AdyenCheckout"), AddInDescription("Adyen checkout handler"), AddInUseParameterGrouping(true)]
    public class AdyenCheckout : CheckoutHandler, IRemoteCapture, ICancelOrder, IFullReturn, IPartialReturn, ISavedCard, ICheckoutHandlerCallback
    {
        private static class Tags
        {
            public static string SessionId = "Adyen.PaymentSessionId";
            public static string SessionData = "Adyen.PaymentSessionData";
            public static string ClientKey = "Adyen.ClientKey";
            public static string Environment = "Adyen.Environment";
            public static string AmountCurrency = "Adyen.Currency";
            public static string AmountValue = "Adyen.Price";
            public static string AdyenJavaScriptUrl = "Adyen.JavaScriptUrl";
            public static string AdyenCssUrl = "Adyen.CssUrl";
            public static string AdyenJsIntegrityKey = "Adyen.JsIntegrityKey";
            public static string AdyenCssIntegrityKey = "Adyen.CssIntegrityKey";
            public static string AdyenPaymentMethods = "Adyen.PaymentMethods";
        }

        #region Fields

        private const string CardPaymentMethodName = "scheme";
        private const string RefundIdDelimeter = "_@_";
        private static ConcurrentDictionary<string, Order> _currentOrders = new ConcurrentDictionary<string, Order>();
        private const string FormTemplateFolder = "eCom7/CheckoutHandler/Adyen/Form";
        private const string CancelTemplateFolder = "eCom7/CheckoutHandler/Adyen/Cancel";
        private const string ErrorTemplateFolder = "eCom7/CheckoutHandler/Adyen/Error";
        private const string CardTemplateFolder = "eCom7/CheckoutHandler/Adyen/Card";

        private static readonly IList<PaymentResponse.PaymentResultCode> PositivePaymentResultCodes = new[]
        {
            PaymentResponse.PaymentResultCode.Authorised,
            PaymentResponse.PaymentResultCode.Received,
            PaymentResponse.PaymentResultCode.Pending
        };

        private AdyenUrlManager urlManager;
        private string paymentsTemplate;
        private string cancelTemplate;
        private string errorTemplate;
        private string storedCardTemplate;


        private AdyenUrlManager ApiUrlManager
        {
            get
            {
                if (urlManager is null)
                    urlManager = new AdyenUrlManager(GetEnvironmentType(), LiveUrlPrefix);
                return urlManager;
            }
        }

        #endregion

        #region AddIn parameters

        [AddInLabel("Merchant name"), AddInParameter("MerchantName"), AddInParameterEditor(typeof(TextParameterEditor), "")]
        public string MerchantName { get; set; }

        [AddInLabel("API Key"), AddInParameter("ApiKey"), AddInParameterEditor(typeof(TextParameterEditor), "")]
        public string ApiKey { get; set; }

        [AddInLabel("Client key"), AddInParameter("ClientKey"), AddInParameterEditor(typeof(TextParameterEditor), "")]
        public string ClientKey { get; set; }

        [AddInLabel("Live URL prefix"), AddInParameter("LiveUrlPrefix"), AddInParameterEditor(typeof(TextParameterEditor), "infoText=If it is not set, the test mode will be used, even if corresponding checkbox is not checked")]
        public string LiveUrlPrefix { get; set; }

        [AddInParameter("Allow save cards"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool AllowSaveCards { get; set; }

        [AddInParameter("Skip security code for one-off payments"), AddInParameterEditor(typeof(YesNoParameterEditor), "infoText=If not checked, the provider will redirect you to a template where you can enter the security code of your saved card. If checked, the provider will attempt to complete the payment transaction using the stored card data. Please note: SkipCvCForOneClick must be enabled on your Adyen account to make it work.")]
        public bool SkipCvCForOneClickPayment { get; set; }

        [AddInLabel("Test mode"), AddInParameter("TestMode"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool TestMode { get; set; }

        [AddInLabel("Debug mode"), AddInParameter("DebugMode"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool DebugMode { get; set; }

        [AddInLabel("Payments template"), AddInParameter("PaymentsTemplate"), AddInParameterGroup("Template settings"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=templates/{FormTemplateFolder}")]
        public string PaymentsTemplate
        {
            get => TemplateHelper.GetTemplateName(paymentsTemplate);
            set => paymentsTemplate = value;
        }

        [AddInLabel("Cancel template"), AddInParameter("CancelTemplate"), AddInParameterGroup("Template settings"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=templates/{CancelTemplateFolder}")]
        public string CancelTemplate
        {
            get => TemplateHelper.GetTemplateName(cancelTemplate);
            set => cancelTemplate = value;
        }

        [AddInLabel("Error template"), AddInParameter("ErrorTemplate"), AddInParameterGroup("Template settings"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=templates/{ErrorTemplateFolder}")]
        public string ErrorTemplate
        {
            get => TemplateHelper.GetTemplateName(errorTemplate);
            set => errorTemplate = value;
        }

        [AddInLabel("Stored card details template"), AddInParameter("StoredCardTemplate"), AddInParameterGroup("Template settings"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=templates/{CardTemplateFolder};infoText=Template where you can set security card code. It is used if the setting 'Skip security code for one-off payments' is unchecked.")]
        public string StoredCardTemplate
        {
            get => TemplateHelper.GetTemplateName(storedCardTemplate);
            set => storedCardTemplate = value;
        }

        [AddInLabel("HMAC key"), AddInParameter("HmacKey"), AddInParameterGroup("Notification settings"), AddInParameterEditor(typeof(TextParameterEditor), "infoText=If it is not set, notification processing will not be performed, even if it is configured in the Adyen control panel.")]
        public string HmacKey { get; set; }

        #endregion

        #region CheckoutHandler

        public override OutputResult BeginCheckout(Order order, CheckoutParameters parameters)
        {
            LogEvent(order, "Checkout started");

            if (string.IsNullOrEmpty(ApiKey))
                throw new ArgumentNullException(nameof(ApiKey), "API key reqired");

            if (NeedUseSavedCard(order, out PaymentCardToken savedCard))
            {
                if (SkipCvCForOneClickPayment)
                {
                    return PaymentMethodSelected(order, new()
                    {
                        PaymentMethod = new PaymentMethod
                        {
                            Type = CardPaymentMethodName,
                            StoredPaymentMethodId = savedCard.Token
                        }
                    });
                }
                else
                {
                    var paymentMethodsRequest = new PaymentMethodsRequest(order, MerchantName, true);
                    string paymentMethodsResponse;
                    try
                    {
                        paymentMethodsResponse = WebRequestHelper.Request(ApiUrlManager.GetPaymentMethodsUrl(), paymentMethodsRequest.ToJson(), GetEnvironmentType(), ApiKey);
                    }
                    catch (Exception ex) when (ex is not ThreadAbortException)
                    {
                        return OnError(order, ex.Message, ex, false);
                    }

                    var paymentMethodsData = Converter.Deserialize<PaymentMethodsResponse>(paymentMethodsResponse);
                    paymentMethodsData.SavedPaymentMethods = paymentMethodsData.SavedPaymentMethods.Where(method => method.CardToken.Equals(savedCard.Token, StringComparison.OrdinalIgnoreCase));

                    var storedMethodTemplate = new Template(TemplateHelper.GetTemplatePath(StoredCardTemplate, CardTemplateFolder));
                    SetCommonTemplateTags(storedMethodTemplate);
                    storedMethodTemplate.SetTag(Tags.AdyenPaymentMethods, Converter.Serialize(paymentMethodsData));

                    return new ContentOutputResult
                    {
                        Content = Render(order, storedMethodTemplate)
                    };
                }
            }

            bool saveCard = NeedSaveCard(order, CardPaymentMethodName, out var _);
            string callbackUrl = GetCallbackUrl(order, new NameValueCollection { ["action"] = "GatewayResponse" });
            var sessionsRequest = new SessionRequest(order, MerchantName, callbackUrl);
            if (saveCard)
            {
                sessionsRequest.ShopperReference = Helper.GetShopperReference(order);
                sessionsRequest.EnableOneClick = true;
                sessionsRequest.EnablePayOut = true;
            }
            SessionsResponse session;
            try
            {
                string sessionsResponse = WebRequestHelper.Request(ApiUrlManager.GetSessionsUrl(), sessionsRequest.ToJson(), GetEnvironmentType(), ApiKey);
                session = Converter.Deserialize<SessionsResponse>(sessionsResponse);
            }
            catch (Exception ex) when (ex is not ThreadAbortException)
            {
                return OnError(order, ex.Message, ex, false);
            }

            var paymentMethodsTemplate = new Template(TemplateHelper.GetTemplatePath(PaymentsTemplate, FormTemplateFolder));
            SetCommonTemplateTags(paymentMethodsTemplate);
            paymentMethodsTemplate.SetTag(Tags.SessionId, session.Id);
            paymentMethodsTemplate.SetTag(Tags.SessionData, session.SessionData);

            return new ContentOutputResult
            {
                Content = Render(order, paymentMethodsTemplate)
            };

            void SetCommonTemplateTags(Template template)
            {
                template.SetTag(Tags.Environment, GetEnvironmentType().ToString().ToLower());
                template.SetTag(Tags.ClientKey, ClientKey);
                template.SetTag(Tags.AmountCurrency, order.CurrencyCode);
                template.SetTag(Tags.AmountValue, order.Price.PricePIP);
                template.SetTag(Tags.AdyenCssUrl, ApiUrlManager.GetCssUrl());
                template.SetTag(Tags.AdyenJavaScriptUrl, ApiUrlManager.GetJavaScriptUrl());
                template.SetTag(Tags.AdyenJsIntegrityKey, ApiUrlManager.JsIntegrityKey);
                template.SetTag(Tags.AdyenCssIntegrityKey, ApiUrlManager.CssIntegrityKey);
            }
        }

        public override OutputResult HandleRequest(Order order)
        {
            LogEvent(null, "Redirected to Adyen CheckoutHandler");

            order = StartProcessingOrder(order);
            string originOrderId = order.Id;

            string action = Converter.ToString(Context.Current.Request["action"]);
            if (string.IsNullOrEmpty(action))
            {
                StopProcessingOrder(originOrderId);
                return ContentOutputResult.Empty;
            }

            var redirectResult = Converter.ToString(Context.Current.Request["redirectResult"]);
            try
            {
                switch (action)
                {
                    case "SelectMethod":
                        return PaymentMethodSelected(order, null);

                    case "GatewayResponse":
                        return HandleThirdPartyGatewayResponse(order, redirectResult);

                    case "UseSavedMethod":
                        return HandleSavedMethod();

                    default:
                        string message = $"Unknown action: '{action}'";
                        return OnError(order, message, null, Helper.IsAjaxRequest());
                }
            }
            catch (ThreadAbortException)
            {
                return ContentOutputResult.Empty;
            }
            catch (Exception ex)
            {
                return OnError(order, ex.Message, ex, Helper.IsAjaxRequest());
            }
            finally
            {
                StopProcessingOrder(originOrderId);
            }

            OutputResult HandleSavedMethod()
            {
                if (NeedUseSavedCard(order, out PaymentCardToken savedCard))
                {
                    string documentContent;
                    try
                    {
                        documentContent = WebRequestHelper.ReadRequestInputStream();
                    }
                    catch (Exception e)
                    {
                        return OnError(order, "Cannot read selected payment method data", e, true);
                    }

                    if (string.IsNullOrEmpty(documentContent))
                        return OnError(order, "Payment method is not selected", null, true);

                    var paymentMethodData = Converter.Deserialize<PaymentMethodData>(documentContent);
                    if (!paymentMethodData.PaymentMethod.StoredPaymentMethodId.Equals(savedCard.Token, StringComparison.OrdinalIgnoreCase))
                        return OnError(order, "Selected card is not equal to stored payment card", null, true);

                    string secirityCode = paymentMethodData.PaymentMethod.EncryptedSecurityCode;
                    return PaymentMethodSelected(order, new()
                    {
                        PaymentMethod = new()
                        {
                            Type = CardPaymentMethodName,
                            StoredPaymentMethodId = savedCard.Token,
                            EncryptedSecurityCode = secirityCode
                        }
                    });
                }

                return OnError(order, "Order was not set to use saved card.", null, Helper.IsAjaxRequest());
            }
        }

        private Order StartProcessingOrder(Order order)
        {
            var startTime = DateTime.Now;
            while (!_currentOrders.TryAdd(order.Id, order))
            {
                if (DateTime.Now.Subtract(startTime).TotalSeconds > 10)  // Adyen awaits response 10 sec 
                    break;

                Thread.Sleep(500);
            }
            return order;
        }

        private void StopProcessingOrder(string orderId)
        {
            _currentOrders.TryRemove(orderId, out _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="paymentMethodData">Saved card data to use.</param>
        private OutputResult PaymentMethodSelected(Order order, PaymentMethodData paymentMethodData)
        {
            LogEvent(order, "Payment method selected");

            bool doSaveCard = false;
            string cardName = string.Empty;
            var additionalAction = PaymentRequestType.Default;
            if (paymentMethodData is not null)
                additionalAction = PaymentRequestType.UseSavedCard;
            else
            {
                string documentContent;
                try
                {
                    documentContent = WebRequestHelper.ReadRequestInputStream();
                }
                catch (Exception e)
                {
                    return OnError(order, "Cannot read selected Payment method data", e, true);
                }

                if (string.IsNullOrEmpty(documentContent))
                    return OnError(order, "Payment method is not selected", null, true);

                paymentMethodData = Converter.Deserialize<PaymentMethodData>(documentContent);

                if (NeedSaveCard(order, paymentMethodData.PaymentMethod.Type, out cardName))
                {
                    doSaveCard = true;
                    additionalAction = PaymentRequestType.SaveCard;
                }
            }

            var paymentRequest = new PaymentRequest(order, paymentMethodData, MerchantName, GetCallbackUrl(order, new NameValueCollection { ["action"] = "GatewayResponse" }), additionalAction);
            try
            {
                string paymentMethodsResponse = WebRequestHelper.Request(ApiUrlManager.GetPaymentUrl(), paymentRequest.ToJson(), GetEnvironmentType(), ApiKey);
                var paymentResponse = Converter.Deserialize<PaymentResponse>(paymentMethodsResponse);
                return HandleAdyenPaymentResponse(order, paymentResponse, paymentMethodsResponse, doSaveCard, cardName, paymentMethodData.PaymentMethod.Type);
            }
            catch (Exception e) when (e is not ThreadAbortException)
            {
                return OnError(order, e.Message, e, Helper.IsAjaxRequest());
            }
        }

        /// <summary>
        /// Executes when customer returned from third-party payment provider page. E.g. Klarna, Paysafecard, a bank, etc.
        /// </summary>
        private OutputResult HandleThirdPartyGatewayResponse(Order order, string redirectResult)
        {
            LogEvent(order, "Gateway response handling");

            if (string.IsNullOrEmpty(order.TransactionToken))
            {
                LogEvent(order, "Order with empty TransactionToken");
                return ContentOutputResult.Empty;
            }

            var transactionData = Converter.Deserialize<PaymentTransactionData>(order.TransactionToken);
            var details = new Dictionary<string, string>();
            if (transactionData.RequestKeys is not null && transactionData.RequestKeys.Count > 0)
            {
                foreach (string key in transactionData.RequestKeys)
                    details[key] = Context.Current.Request[key];
            }

            if (!string.IsNullOrWhiteSpace(redirectResult))
                details["redirectResult"] = redirectResult;

            var postValues = new
            {
                details,
                paymentData = transactionData.PaymentData
            };

            return HandlePaymentDetailsRequest(order, Converter.SerializeCompact(postValues));
        }

        private OutputResult OrderCompleted(Order order, Amount transactionAmount)
        {
            LogEvent(order, "State ok");

            if (transactionAmount is not null && transactionAmount.Value.HasValue)
                order.TransactionAmount = transactionAmount.Value.Value / 100d;
            else
                order.TransactionAmount = 0;

            SetOrderComplete(order);
            CheckoutDone(order);

            if (Helper.IsAjaxRequest())
            {
                string receiptUrl = Helper.GetCartUrl(order, Converter.ToInt32(Context.Current.Request["ID"]));
                return Helper.EndRequest(new { redirectToReceipt = receiptUrl });
            }

            return PassToCart(order);
        }

        private OutputResult OrderRefused(Order order, string refusalReason)
        {
            order.TransactionAmount = 0;
            order.GatewayResult = refusalReason;
            string message = $"Payment was refused. RefusalReason: {refusalReason}";

            return OnError(order, message, null, Helper.IsAjaxRequest(), "Refused");
        }

        private OutputResult OrderCancelled(Order order)
        {
            order.TransactionAmount = 0;
            order.GatewayResult = "Payment has been cancelled";
            order.TransactionStatus = "Cancelled";

            string message = "Payment has been cancelled (either by the shopper or the merchant) before processing was completed";
            if (Helper.IsAjaxRequest())
                return OnError(order, message, null, true, "Cancelled");
            else
                CheckoutDone(order);

            var cancelTemplate = new Template(TemplateHelper.GetTemplatePath(CancelTemplate, CancelTemplateFolder));
            cancelTemplate.SetTag("CheckoutHandler:CancelMessage", message);
            var orderRenderer = new Frontend.Renderer();
            orderRenderer.RenderOrderDetails(cancelTemplate, order, true);

            return new ContentOutputResult
            {
                Content = cancelTemplate.Output()
            };
        }

        private OutputResult PaymentError(Order order, string reason)
        {
            order.TransactionAmount = 0;
            order.GatewayResult = reason;
            string message = $"There was an error when the payment was being processed. Reason: {reason}";

            return OnError(order, message, null, Helper.IsAjaxRequest());
        }

        /// <summary>
        /// As per documentation, this result means that Adyen do not yet know actual payment result state
        /// </summary>
        private OutputResult PaymentReceived(Order order)
        {
            LogEvent(order, "Paynemt Pending or Received");

            order.TransactionAmount = 0;
            order.GatewayResult = "Paynemt pending or received";
            order.TransactionStatus = "Check payment in Adyen control panel."; //database limit is 50

            SetOrderComplete(order);
            CheckoutDone(order);

            if (Helper.IsAjaxRequest())
            {
                string receiptUrl = Helper.GetCartUrl(order, Converter.ToInt32(Context.Current.Request["ID"]));
                return Helper.EndRequest(new { redirectToReceipt = receiptUrl });
            }

            return PassToCart(order);
        }

        private OutputResult HandleAdyenPaymentResponse(Order order, PaymentResponse response, string jsonResponse, bool doSaveCard, string cardName, string paymentMethodType)
        {
            if (response.Action is not null)
            {
                if (response.Action.Type.HasValue && response.Action.Type.Value is CheckoutPaymentsAction.CheckoutActionType.Redirect)
                {
                    var transactionData = new PaymentTransactionData(response.Action.PaymentData);
                    if (response.Details is not null)
                    {
                        foreach (InputDetail detail in response.Details)
                        {
                            if (!string.IsNullOrEmpty(detail.Key))
                                transactionData.RequestKeys.Add(detail.Key);
                        }
                    }

                    order.TransactionCardType = response.Action.PaymentMethodType;
                    order.TransactionToken = Converter.SerializeCompact(transactionData);
                    Services.Orders.Save(order);
                }

                if (!Helper.IsAjaxRequest())
                    return OnError(order, "Wrong request", null, false);

                return Helper.EndRequest(jsonResponse);
            }

            if (response.ResultCode.HasValue)
            {
                order.TransactionToken = null;
                order.TransactionStatus = Enum.GetName(typeof(PaymentResponse.PaymentResultCode), response.ResultCode);
                order.TransactionNumber = response.PspReference;

                if (PositivePaymentResultCodes.Contains(response.ResultCode.Value) && CardPaymentMethodName.Equals(paymentMethodType, StringComparison.OrdinalIgnoreCase))
                {
                    if (response.AdditionalData is null)
                        response.AdditionalData = new Dictionary<string, string>();

                    SetCardDetails(order, response.AdditionalData);

                    if (doSaveCard)
                    {
                        response.AdditionalData.TryGetValue("recurring.recurringDetailReference", out string cardToken);
                        SaveCard(order, cardName, cardToken);
                    }
                }

                switch (response.ResultCode)
                {
                    case PaymentResponse.PaymentResultCode.Authorised:
                        return OrderCompleted(order, response.Amount);

                    case PaymentResponse.PaymentResultCode.Refused:
                        return OrderRefused(order, response.RefusalReason);

                    case PaymentResponse.PaymentResultCode.Cancelled:
                        return OrderCancelled(order);

                    case PaymentResponse.PaymentResultCode.Error:
                        return PaymentError(order, response.RefusalReason);

                    case PaymentResponse.PaymentResultCode.Received:
                    case PaymentResponse.PaymentResultCode.Pending:
                        return PaymentReceived(order);

                    // These result codes should never be handled here
                    // They should be handled by the code above - "if (response.Action != null)"
                    case PaymentResponse.PaymentResultCode.AuthenticationFinished:
                    case PaymentResponse.PaymentResultCode.ChallengeShopper:
                    case PaymentResponse.PaymentResultCode.IdentifyShopper:
                    case PaymentResponse.PaymentResultCode.RedirectShopper:
                    case PaymentResponse.PaymentResultCode.PresentToShopper:
                    case PaymentResponse.PaymentResultCode.AuthenticationNotRequired:
                        throw new InvalidOperationException();

                    default:
                        return ContentOutputResult.Empty;
                }
            }

            return ContentOutputResult.Empty;
        }

        private OutputResult HandlePaymentDetailsRequest(Order order, string json)
        {
            try
            {
                var paymentDetailsResponse = WebRequestHelper.Request(ApiUrlManager.GetPaymentDetailsUrl(), json, GetEnvironmentType(), ApiKey);
                var response = Converter.Deserialize<PaymentResponse>(paymentDetailsResponse);

                var doSaveCard = NeedSaveCard(order, CardPaymentMethodName, out string cardName);
                return HandleAdyenPaymentResponse(order, response, paymentDetailsResponse, doSaveCard, cardName, CardPaymentMethodName);
            }
            catch (Exception e) when (e is not ThreadAbortException)
            {
                return OnError(order, e.Message, e, Helper.IsAjaxRequest());
            }
        }

        private void Callback(Order order, string jsonData)
        {
            LogEvent(order, "Notification callback started");

            if (string.IsNullOrEmpty(HmacKey))
            {
                LogError(order, "Notification callback failed with message: Specify HMAC key to handle notifications");
                throw new ArgumentNullException("HMAC key is empty");
            }

            var doHandleNotification = true;
            NotificationRequest requestData = null;
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                    doHandleNotification = false;

                if (doHandleNotification)
                {
                    requestData = Converter.Deserialize<NotificationRequest>(jsonData);
                    if (requestData is null)
                        doHandleNotification = false;
                }

                if (DebugMode)
                    LogEvent(order, "Notification contents: {0}", jsonData);
            }
            catch
            {
                doHandleNotification = false;
            }

            if (!doHandleNotification)
            {
                LogEvent(order, "Notification callback failed with message: Notification data is empty or has wrong format");
                return;
            }

            try
            {
                foreach (var container in requestData.NotificationItemContainers)
                {
                    NotificationRequestItem notificationItem = container.NotificationItem;
                    if (!Helper.IsValidHmac(notificationItem, HmacKey))
                    {
                        LogError(order, "Cannot handle notification item: HMAC validation failed");
                        continue;
                    }

                    if (string.IsNullOrEmpty(notificationItem.EventCode))
                    {
                        LogError(order, "Cannot handle notification item: event code is not defined");
                        continue;
                    }

                    LogEvent(order, "Notification event code: {0}", notificationItem.EventCode);

                    HandleNotificationItem(order, notificationItem);
                }

                LogEvent(order, "Notification callback finished");
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Notification callback failed with message: {0}", ex.Message);
            }
        }

        private void HandleNotificationItem(Order order, NotificationRequestItem notificationItem)
        {
            NotificationEventCode? eventCode = notificationItem.GetNotificationEventCode();
            bool isSuccess = Converter.ToBoolean(notificationItem.Success);

            switch (eventCode)
            {
                case NotificationEventCode.Authorisation:
                    Authorisation();
                    break;

                case NotificationEventCode.Capture:
                case NotificationEventCode.CaptureFailed:
                    Capture();
                    break;

                case NotificationEventCode.Cancellation:
                    Cancellation();
                    break;

                case NotificationEventCode.Refund:
                case NotificationEventCode.RefundFailed:
                    Refund();
                    break;

                default:
                    LogError(order, "Cannot handle notification item: event code is unknown or do not require the handling ({0})", notificationItem.EventCode);
                    break;
            }

            void Authorisation()
            {
                if (isSuccess)
                {
                    if (!order.Complete)
                    {
                        UpdateTransactionNumber(order, notificationItem.PspReference);
                        order.TransactionAmount = notificationItem.Amount.Value.Value / 100d;
                        SetOrderComplete(order);
                    }
                    else
                    {
                        order.TransactionAmount = notificationItem.Amount.Value.Value / 100d;
                        UpdateTransactionNumber(order, notificationItem.PspReference);
                        Services.Orders.Save(order);
                    }
                }
                else
                {
                    order.TransactionStatus = $"Authorisation failed: {notificationItem.Reason}";
                    if (DebugMode)
                        LogEvent(order, order.TransactionStatus);

                    Services.Orders.Save(order);
                }
            }

            void Capture()
            {
                if (eventCode is NotificationEventCode.Capture && isSuccess)
                {
                    UpdateTransactionNumber(order, notificationItem.PspReference);
                    order.TransactionAmount = notificationItem.Amount.Value.Value / 100d;
                    order.CaptureAmount = order.TransactionAmount;
                    order.CaptureInfo.Message = "Capture successful";
                    order.CaptureInfo.State = OrderCaptureInfo.OrderCaptureState.Success;
                    if (!order.Complete)
                        SetOrderComplete(order);
                    else
                        Services.Orders.Save(order);
                }
                else
                {
                    string message = string.IsNullOrEmpty(notificationItem.Reason)
                        ? "Capture failed due to a technical issue"
                        : $"Capture failed: {notificationItem.Reason}";

                    SetCaptureFailed(order, message);
                }
            }

            void Cancellation()
            {
                order.TransactionAmount = 0;
                if (isSuccess)
                {
                    Services.Taxes.CancelTaxes(order);
                    order.CaptureInfo = new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Cancel, string.Empty);
                    order.GatewayResult = "Payment has been cancelled";
                    order.TransactionStatus = "Cancelled";
                    UpdateTransactionNumber(order, notificationItem.OriginalReference);
                }
                else
                {
                    string message = $"Payment cancellation failed: {notificationItem.Reason}";
                    if (DebugMode)
                        LogEvent(order, message);
                    order.GatewayResult = message;
                    order.CaptureInfo = new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, message);
                    order.TransactionStatus = "Cancel operation failed";
                    UpdateTransactionNumber(order, notificationItem.OriginalReference);
                }
                Services.Orders.Save(order);
            }

            void Refund()
            {
                // only captured orders can receive this notifications
                bool doSave = false;
                if (order.TransactionAmount < 0.001)
                {
                    order.TransactionAmount = order.Price.Price;
                    doSave = true;
                }
                if (order.CaptureInfo.State is not OrderCaptureInfo.OrderCaptureState.Success)
                {
                    order.CaptureInfo.Message = "Capture successful";
                    order.CaptureInfo.State = OrderCaptureInfo.OrderCaptureState.Success;
                    doSave = true;
                }
                if (doSave)
                    Services.Orders.Save(order);

                double refundAmount = notificationItem.Amount.Value.Value / 100d;
                IEnumerable<OrderReturnInfo> existingOperations = order.ReturnOperations ?? new List<OrderReturnInfo>();
                bool isFailedNotification = (isSuccess && eventCode is NotificationEventCode.RefundFailed) || // literally: refund failed successfully
                                           (!isSuccess && eventCode is NotificationEventCode.Refund); // refund was not succeed

                if (notificationItem.MerchantReference.Contains(RefundIdDelimeter)) // operation was created on DW side
                {
                    int delimeterPosition = notificationItem.MerchantReference.IndexOf(RefundIdDelimeter) + RefundIdDelimeter.Length;
                    string operationGuidRaw = notificationItem.MerchantReference.Substring(delimeterPosition);
                    Guid operationGuid;
                    if (Guid.TryParse(operationGuidRaw, out operationGuid))
                    {
                        OrderReturnInfo operationToHandle = null;
                        foreach (OrderReturnInfo operation in existingOperations)
                        {
                            if (operation.Id.Equals(operationGuid))
                            {
                                operationToHandle = operation;
                                break;
                            }
                        }

                        if (operationToHandle != null)
                        {
                            // update operation state if needed
                            if (isFailedNotification && operationToHandle.State is not OrderReturnOperationState.Failed)
                            {
                                operationToHandle.State = OrderReturnOperationState.Failed;
                                operationToHandle.Message = notificationItem.Reason;
                                Services.Orders.Save(order);
                            }
                            else if (!isFailedNotification && operationToHandle.State is OrderReturnOperationState.Failed)
                            {
                                var fullyReturned = Helper.IsFullAmountReturned(order, existingOperations, refundAmount);
                                operationToHandle.State = fullyReturned ? OrderReturnOperationState.FullyReturned : OrderReturnOperationState.PartiallyReturned;
                                operationToHandle.Message = "Order refund successful";
                                Services.Orders.Save(order);
                            }
                            return;
                        }
                    }
                }

                var finishedOperation = existingOperations.FirstOrDefault(o => o.State is OrderReturnOperationState.FullyReturned);
                if (finishedOperation is not null)
                {
                    if (isFailedNotification)
                    {
                        // mark order as not fully returned. failed return operation will be added below.
                        finishedOperation.State = OrderReturnOperationState.PartiallyReturned;
                        Services.Orders.Save(order);
                    }
                    else
                        return; // refund completed, no additional actions needed                    
                }

                // create a new return operation
                if (isFailedNotification)
                {
                    string message = $"Refund failed: {notificationItem.Reason}";
                    if (DebugMode)
                        LogEvent(order, message);

                    OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, message, refundAmount, order);
                }
                else
                {
                    bool fullyReturned = Helper.IsFullAmountReturned(order, existingOperations, refundAmount);
                    var state = fullyReturned
                        ? OrderReturnOperationState.FullyReturned
                        : OrderReturnOperationState.PartiallyReturned;

                    OrderReturnInfo.SaveReturnOperation(state, "Order refund successful", refundAmount, order);
                }
            }
        }

        #endregion

        #region IRemoteCapture

        public OrderCaptureInfo Capture(Order order)
        {
            LogEvent(order, "Attempting capture");

            string errorText = Helper.GetOrderError(order);
            if (!string.IsNullOrEmpty(errorText))
            {
                LogEvent(order, errorText);
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, errorText);
            }

            try
            {
                var captureRequest = new ModificationRequest(order, MerchantName, true);

                string captureResponseRaw = WebRequestHelper.Request(ApiUrlManager.GetCaptureUrl(order.TransactionNumber), captureRequest.ToJson(), GetEnvironmentType(), ApiKey);
                var captureResponse = Converter.DeserializeCompact<ModificationResponse>(captureResponseRaw);

                return HandleCaptureResponse(order, captureResponse);
            }
            catch (Exception ex)
            {
                SetCaptureFailed(order, $"Capture failed with message: {ex.Message}");
            }

            return order.CaptureInfo;
        }

        private OrderCaptureInfo HandleCaptureResponse(Order order, ModificationResponse response)
        {
            if (response.NotificationItems is not null)
            {
                string infoTxt = string.Format("Payment was unsucceeded with error {0}/{1}", response.NotificationItems[0].Reason, response.Status);
                SetCaptureFailed(order, infoTxt);
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, infoTxt);
            }

            double capturedAmount = response.Amount.Value.Value / 100d;
            order.CaptureInfo.Message = response.Status;
            order.TransactionAmount = capturedAmount;

            LogEvent(order, string.Format("Message=\"{0}\" Amount=\"{1:f2}\"", "Capture successful", capturedAmount), DebuggingInfoType.CaptureResult);
            return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Capture successful");
        }

        #endregion

        #region ICancelOrder

        public bool CancelOrder(Order order)
        {
            LogEvent(order, "Attempting cancel");

            string errorText = Helper.GetOrderError(order);
            if (!string.IsNullOrEmpty(errorText))
            {
                LogEvent(order, errorText);
                return false;
            }

            try
            {
                var cancelRequest = new ModificationRequest(order, MerchantName, false);

                var cancelResponseRaw = WebRequestHelper.Request(ApiUrlManager.GetCancelUrl(order.TransactionNumber), cancelRequest.ToJson(), GetEnvironmentType(), ApiKey);
                var cancelResponse = Converter.DeserializeCompact<ModificationResponse>(cancelResponseRaw);

                if (cancelResponse.NotificationItems is not null)
                {
                    LogError(order, "Cancel order failed with error {0}/{1}", cancelResponse.NotificationItems[0].Reason, cancelResponse.Status);
                    return false;
                }

                LogEvent(order, "Cancel order succeed");
                return true;
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Cancel order failed with message: {0}", ex.Message);
                return false;
            }
        }

        #endregion

        #region IFullReturn, IPartialReturn

        public void FullReturn(Order order) => Refund(order, Converter.ToInt64(order.TransactionAmount));

        public void PartialReturn(Order order, Order originalOrder) => Refund(originalOrder, order.Price.PricePIP);

        private void Refund(Order order, long amountPIP)
        {
            LogEvent(order, "Refund started");

            double amount = amountPIP / 100d;
            Guid operationId = Guid.NewGuid();
            string errorText = Helper.GetOrderError(order);
            if (!string.IsNullOrEmpty(errorText))
            {
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorText, amount, order, operationId);
                LogError(order, errorText, DebuggingInfoType.ReturnResult);
                return;
            }

            if (order.CaptureInfo is null || order.CaptureInfo.State is not OrderCaptureInfo.OrderCaptureState.Success)
            {
                errorText = "Order must be captured before return";
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorText, amount, order, operationId);
                LogError(order, errorText, DebuggingInfoType.ReturnResult);
                return;
            }

            if (amount < 0.01)
            {
                errorText = "Refund amount must be more than 0";
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorText, amount, order, operationId);
                LogError(order, errorText, DebuggingInfoType.ReturnResult);
                return;
            }

            try
            {
                var refundRequest = new ModificationRequest(order, MerchantName, amountPIP);
                refundRequest.OrderId = $"{refundRequest.OrderId}{RefundIdDelimeter}{operationId}"; // Add some UID to find this refund on notification handling

                string refundResponseRaw = WebRequestHelper.Request(ApiUrlManager.GetRefundUrl(order.TransactionNumber), refundRequest.ToJson(), GetEnvironmentType(), ApiKey);
                var refundResponse = Converter.DeserializeCompact<ModificationResponse>(refundResponseRaw);

                if (refundResponse.NotificationItems is not null)
                {
                    errorText = string.Format("Refund failed with error {0}/{1}", refundResponse.NotificationItems[0].Reason, refundResponse.Status);
                    OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorText, amount, order, operationId);
                    LogError(order, errorText, DebuggingInfoType.ReturnResult);
                    return;
                }

                OrderReturnOperationState state = order.Price.Price - amount < 0.01
                    ? OrderReturnOperationState.FullyReturned
                    : OrderReturnOperationState.PartiallyReturned;

                OrderReturnInfo.SaveReturnOperation(state, "Order refund successful", amount, order, operationId);
            }
            catch (Exception ex)
            {
                LogError(order, ex, "Refund failed with message: {0}", ex.Message, DebuggingInfoType.ReturnResult);
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, ex.Message, amount, order, operationId);
            }
        }

        #endregion

        #region ISavedCard

        public void DeleteSavedCard(int savedCardId)
        {
            if (Services.PaymentCard.GetById(savedCardId) is not PaymentCardToken savedCard)
                return;

            try
            {
                var removeRequest = new RemoveSavedPaymentMethodRequest(savedCard, MerchantName);
                string response = WebRequestHelper.Request(ApiUrlManager.GetSavedPaymentMethodRemoveUrl(), removeRequest.ToJson(), GetEnvironmentType(), ApiKey);
            }
            catch { }
        }

        public string UseSavedCard(Order order)
        {
            OutputResult outputResult = BeginCheckout(order);

            if (outputResult is RedirectOutputResult redirectResult)
            {

                /*PassToCart part doesn't work because of changes in Redirect behavior.
                * We need to return RedirectOutputResult as OutputResult, and handle output result to make it work.
                * It means, that we need to change ISavedCard.UseSavedCard method, probably create new one (with OutputResult as returned type)
                * To make it work (temporarily), we use Response.Redirect here                 
                */
                Context.Current.Response.Redirect(redirectResult.RedirectUrl, redirectResult.IsPermanent);
                return string.Empty;
            }

            if (outputResult is ContentOutputResult contentResult)
                return contentResult.Content;

            return string.Empty;
        }

        public bool SavedCardSupported(Order order) => AllowSaveCards;

        private bool NeedSaveCard(Order order, string paymentMethodType, out string cardName)
        {
            if (AllowSaveCards &&
                order.CustomerAccessUserId > 0 &&
                (order.DoSaveCardToken || !string.IsNullOrEmpty(order.SavedCardDraftName)) &&
                CardPaymentMethodName.Equals(paymentMethodType, StringComparison.OrdinalIgnoreCase)) // payment method is card
            {
                cardName = !string.IsNullOrEmpty(order.SavedCardDraftName) ? order.SavedCardDraftName : order.Id;
                return true;
            }

            cardName = string.Empty;
            return false;
        }

        private bool NeedUseSavedCard(Order order, out PaymentCardToken savedCard)
        {
            savedCard = Services.PaymentCard.GetById(order.SavedCardId);
            return !string.IsNullOrEmpty(savedCard?.Token) && order.CustomerAccessUserId == savedCard.UserID;
        }

        private void SaveCard(Order order, string cardName, string cardToken)
        {
            if (!AllowSaveCards ||
                order.CustomerAccessUserId <= 0 ||
                string.IsNullOrEmpty(cardToken))
            {
                LogError(order, "Unable to save card userId: {0}; card token {1}. Make sure card and recurring token information enabled as additional data at gateway settings.", order.CustomerAccessUserId, cardToken);
                return;
            }

            PaymentCardToken savedCard = Services.PaymentCard.GetByUserId(order.CustomerAccessUserId).FirstOrDefault(t => t.Token.Equals(cardToken));
            if (savedCard is null)
                savedCard = Services.PaymentCard.CreatePaymentCard(order.CustomerAccessUserId, order.PaymentMethodId, cardName, order.TransactionCardType, order.TransactionCardNumber, cardToken);

            order.SavedCardId = savedCard.ID;
            Services.Orders.Save(order);

            LogEvent(order, "Saved card created: {0}", savedCard.Name);
        }

        #endregion

        #region ICheckoutHandlerCallback

        public static Order GetOrderFromCallback(CallbackData data)
        {
            var logger = LogManager.System.GetLogger(LogCategory.Health, "Checkout");

            NotificationRequest request = Converter.Deserialize<NotificationRequest>(data.Body);

            if (request?.NotificationItemContainers?.Count is null or 0)
            {
                logger.Error("Adyen notification processing: data is not found", null);
                return null;
            }

            if (request.NotificationItemContainers.Count > 1)
            {
                logger.Error("The webhook notification data should contain only one NotificationRequestItem.");
                return null;
            }

            string merchantReference = request.NotificationItemContainers[0].NotificationItem?.MerchantReference ?? "";
            string orderId = merchantReference.IndexOf(RefundIdDelimeter, StringComparison.OrdinalIgnoreCase) is int delimeterPosition && delimeterPosition > 0
                ? merchantReference.Substring(0, delimeterPosition)
                : merchantReference;

            Order order = Services.Orders.GetById(orderId);
            if (order is not null)
            {
                var message = new StringBuilder($"Adyen notification processing: {orderId}. Json data:");
                message.AppendLine(data.Body);
                Services.OrderDebuggingInfos.Save(order, message.ToString(), "Order", DebuggingInfoType.Undefined);
            }

            return order;
        }

        public OutputResult HandleCallback(Order order, CallbackData data)
        {
            Callback(order, data.Body);
            return ContentOutputResult.Empty;
        }

        #endregion

        #region Private methods

        private bool IsTestMode() => TestMode || string.IsNullOrEmpty(LiveUrlPrefix);

        private EnvironmentType GetEnvironmentType() => IsTestMode() ? EnvironmentType.Test : EnvironmentType.Live;

        private string GetCallbackUrl(Order order, NameValueCollection parameters) => Helper.GetCallbackUrl(GetBaseUrl(order), parameters);

        private OutputResult OnError(Order order, string message, Exception exception, bool isAjax, string transactionStatus = "Failed")
        {
            // Prepare data
            ServiceError serviceError = null;
            try
            {
                serviceError = Converter.Deserialize<ServiceError>(message);
                if (serviceError is not null)
                    message = serviceError.Message;
            }
            catch { }

            if (exception is not null)
                LogError(order, exception, message);
            else
                LogError(order, message);

            // Downgrade order, set context cart, etc
            order.TransactionStatus = transactionStatus;
            if (!order.Complete)
                CheckoutDone(order);

            // Show error
            if (isAjax)
                return Helper.EndRequest(serviceError ?? new ServiceError { ErrorCode = "Unknown", Message = message });

            if (string.IsNullOrWhiteSpace(ErrorTemplate))
                return PassToCart(order);

            var errorTemplate = new Template(TemplateHelper.GetTemplatePath(ErrorTemplate, ErrorTemplateFolder));
            errorTemplate.SetTag("CheckoutHandler:ErrorMessage", message);

            return new ContentOutputResult
            {
                Content = Render(order, errorTemplate)
            };
        }

        private void UpdateTransactionNumber(Order order, string transactionId)
        {
            if (!string.IsNullOrEmpty(transactionId) && !transactionId.Equals(order.TransactionNumber, StringComparison.OrdinalIgnoreCase))
            {
                if (DebugMode)
                    LogEvent(order, "Transaction number has changed. Old: '{0}', New: '{1}'", order.TransactionNumber, transactionId);
                order.TransactionNumber = transactionId;
            }
        }

        private void SetCaptureFailed(Order order, string message)
        {
            LogError(order, message);

            order.CaptureInfo.Message = message;
            order.CaptureInfo.State = OrderCaptureInfo.OrderCaptureState.Failed;
            Services.Orders.Save(order);
        }

        private void SetCardDetails(Order order, Dictionary<string, string> additionalData)
        {
            string lastFour;
            if (!additionalData.TryGetValue("cardSummary", out lastFour)) // this information is optionally included/excluded in response (Adyen Account settings)
                order.TransactionCardNumber = "<None>";
            else
                order.TransactionCardNumber = lastFour;

            string cardType;
            // "paymentMethod" value is more human-readable, but it could be optionally included/excluded in response
            // then try to use "cardPaymentMethod", if presented
            if (!additionalData.TryGetValue("paymentMethod", out cardType) || string.IsNullOrEmpty(cardType))
            {
                if (!additionalData.TryGetValue("cardPaymentMethod", out cardType) || string.IsNullOrEmpty(cardType))
                    cardType = "<Card>";
            }
            order.TransactionCardType = cardType;

            Services.Orders.Save(order);
        }

        #endregion
    }
}
