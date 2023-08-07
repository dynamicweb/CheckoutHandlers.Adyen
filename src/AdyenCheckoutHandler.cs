using Dynamicweb.Core;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model;
using Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen
{
    [AddInName("AdyenCheckout"), AddInDescription("Adyen checkout handler"), AddInUseParameterGrouping(true)]
    public class AdyenCheckout : CheckoutHandler, IRemoteCapture, ICancelOrder, IFullReturn, IPartialReturn, ISavedCard
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
        }

        #region Fields

        private const string CardPaymentMethodName = "scheme";
        private const string RefundIdDelimeter = "_@_";
        private static ConcurrentDictionary<string, Order> _currentOrders = new ConcurrentDictionary<string, Order>();
        private const string FormTemplateFolder = "eCom7/CheckoutHandler/Adyen/Form";
        private const string CancelTemplateFolder = "eCom7/CheckoutHandler/Adyen/Cancel";
        private const string ErrorTemplateFolder = "eCom7/CheckoutHandler/Adyen/Error";

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

        private AdyenUrlManager ApiUrlManager
        {
            get { return urlManager ?? (urlManager = new AdyenUrlManager(GetEnvironmentType(), LiveUrlPrefix)); }
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

        [AddInLabel("Test mode"), AddInParameter("TestMode"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool TestMode { get; set; }

        [AddInLabel("Debug mode"), AddInParameter("DebugMode"), AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        public bool DebugMode { get; set; }

        [AddInLabel("Payments template"), AddInParameter("PaymentsTemplate"), AddInParameterGroup("Template settings"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=templates/{FormTemplateFolder}")]
        public string PaymentsTemplate
        {
            get
            {
                return TemplateHelper.GetTemplateName(paymentsTemplate);
            }
            set => paymentsTemplate = value;
        }

        [AddInLabel("Cancel template"), AddInParameter("CancelTemplate"), AddInParameterGroup("Template settings"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=templates/{CancelTemplateFolder}")]
        public string CancelTemplate
        {
            get
            {
                return TemplateHelper.GetTemplateName(cancelTemplate);
            }
            set => cancelTemplate = value;
        }

        [AddInLabel("Error template"), AddInParameter("ErrorTemplate"), AddInParameterGroup("Template settings"), AddInParameterEditor(typeof(TemplateParameterEditor), $"folder=templates/{ErrorTemplateFolder}")]
        public string ErrorTemplate
        {
            get
            {
                return TemplateHelper.GetTemplateName(errorTemplate);
            }
            set => errorTemplate = value;
        }

        [AddInLabel("HMAC key"), AddInParameter("HmacKey"), AddInParameterGroup("Notification settings"), AddInParameterEditor(typeof(TextParameterEditor), "infoText=If it is not set, notification processing will not be performed, even if it is configured in the Adyen control panel.")]
        public string HmacKey { get; set; }

        #endregion

        #region CheckoutHandler

        public override string StartCheckout(Order order)
        {
            LogEvent(order, "Checkout started");

            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new ArgumentNullException(nameof(ApiKey), "API key reqired");
            }

            if (NeedUseSavedCard(order, out var savedCard))
            {
                PaymentMethodSelected(order, new PaymentMethodData { PaymentMethod = new PaymentMethod() { Type = CardPaymentMethodName, StoredPaymentMethodId = savedCard.Token } });
                return string.Empty;
            }
            var saveCard = NeedSaveCard(order, CardPaymentMethodName, out var savedCardName);
            var sessionsRequest = new SessionRequest(order, MerchantName, GetCallbackUrl(order, new NameValueCollection { { "action", "GatewayResponse" } }));
            if (saveCard)
            {
                sessionsRequest.ShopperReference = $"user ID:{order.CustomerAccessUserId}";
                sessionsRequest.EnableOneClick = true;
                sessionsRequest.EnablePayOut = true;
            }
            SessionsResponse session = null;
            try
            {
                var sessionsResponse = WebRequestHelper.Request(ApiUrlManager.GetSessionsUrl(), sessionsRequest.ToJson(), GetEnvironmentType(), ApiKey);
                session = Converter.Deserialize<SessionsResponse>(sessionsResponse);

            }
            catch (Exception ex) when (!(ex is ThreadAbortException))
            {
                OnError(order, ex.Message, ex, false);
            }

            var paymentMethodsTemplate = new Template(TemplateHelper.GetTemplatePath(PaymentsTemplate, FormTemplateFolder));
            paymentMethodsTemplate.SetTag(Tags.Environment, GetEnvironmentType().ToString().ToLower());
            paymentMethodsTemplate.SetTag(Tags.ClientKey, ClientKey);
            paymentMethodsTemplate.SetTag(Tags.SessionId, session.Id);
            paymentMethodsTemplate.SetTag(Tags.SessionData, session.SessionData);
            paymentMethodsTemplate.SetTag(Tags.AmountCurrency, order.CurrencyCode);
            paymentMethodsTemplate.SetTag(Tags.AmountValue, order.Price.PricePIP);
            paymentMethodsTemplate.SetTag(Tags.AdyenCssUrl, ApiUrlManager.GetCssUrl());
            paymentMethodsTemplate.SetTag(Tags.AdyenJavaScriptUrl, ApiUrlManager.GetJavaScriptUrl());

            return Render(order, paymentMethodsTemplate);
        }

        public override string Redirect(Order order)
        {
            LogEvent(null, "Redirected to Adyen CheckoutHandler");

            order = StartProcessingOrder(order);
            var originOrderId = order.Id;

            var action = Converter.ToString(Context.Current.Request["action"]);
            if (string.IsNullOrEmpty(action))
            {
                try
                {
                    string jsonData;
                    using (var inputStream = new StreamReader(Context.Current.Request.InputStream))
                    {
                        jsonData = inputStream.ReadToEnd();
                    }

                    Callback(order, jsonData);
                    return null;
                }
                finally
                {
                    StopProcessingOrder(originOrderId);
                }
            }

            var redirectResult = Converter.ToString(Context.Current.Request["redirectResult"]);
            try
            {
                switch (action)
                {
                    case "SelectMethod":
                        PaymentMethodSelected(order, null);
                        return null;

                    case "GatewayResponse":
                        return HandleThirdPartyGatewayResponse(order, redirectResult);

                    default:
                        var message = $"Unknown action: '{action}'";
                        return OnError(order, message, null, Helper.IsAjaxRequest());
                }
            }
            catch (ThreadAbortException)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                var message = $"Unhandled exception with message: {ex.Message}";
                return OnError(order, message, ex, Helper.IsAjaxRequest());
            }
            finally
            {
                StopProcessingOrder(originOrderId);
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
            _currentOrders.TryRemove(orderId, out Order outOrder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="paymentMethodData">Saved card data to use.</param>
        private void PaymentMethodSelected(Order order, PaymentMethodData paymentMethodData)
        {
            LogEvent(order, "Payment method selected");

            Context.Current.Response.Clear();

            var doSaveCard = false;
            var cardName = string.Empty;
            var additionalAction = PaymentRequestType.Default;
            if (paymentMethodData != null)
            {
                additionalAction = PaymentRequestType.UseSavedCard;
            }
            else
            {
                var documentContent = string.Empty;
                try
                {
                    documentContent = WebRequestHelper.ReadRequestInputStream();
                }
                catch (Exception e)
                {
                    OnError(order, "Cannot read selected Payment method data", e, true);
                }

                if (string.IsNullOrEmpty(documentContent))
                {
                    OnError(order, "Payment method is not selected", null, true);
                }

                paymentMethodData = Converter.Deserialize<PaymentMethodData>(documentContent);

                if (NeedSaveCard(order, paymentMethodData.PaymentMethod.Type, out cardName))
                {
                    doSaveCard = true;
                    additionalAction = PaymentRequestType.SaveCard;
                }
            }

            var paymentRequest = new PaymentRequest(order, paymentMethodData, MerchantName, GetCallbackUrl(order, new NameValueCollection { { "action", "GatewayResponse" } }), additionalAction);
            try
            {
                var paymentMethodsResponse = WebRequestHelper.Request(ApiUrlManager.GetPaymentUrl(), paymentRequest.ToJson(), GetEnvironmentType(), ApiKey);
                var paymentResponse = Converter.Deserialize<PaymentResponse>(paymentMethodsResponse);

                HandleAdyenPaymentResponse(order, paymentResponse, paymentMethodsResponse, doSaveCard, cardName, paymentMethodData.PaymentMethod.Type);
            }
            catch (Exception e) when (!(e is ThreadAbortException))
            {
                OnError(order, e.Message, e, true);
            }
        }

        /// <summary>
        /// Executes when customer returned from third-party payment provider page. E.g. Klarna, Paysafecard, a bank, etc.
        /// </summary>
        private string HandleThirdPartyGatewayResponse(Order order, string redirectResult)
        {
            LogEvent(order, "Gateway response handling");

            if (string.IsNullOrEmpty(order.TransactionToken))
            {
                LogEvent(order, "Order with empty TransactionToken");
                return null;
            }

            var transactionData = Converter.Deserialize<PaymentTransactionData>(order.TransactionToken);
            var details = new Dictionary<string, string>();
            if (transactionData.RequestKeys != null && transactionData.RequestKeys.Count > 0)
            {
                foreach (var key in transactionData.RequestKeys)
                {
                    details[key] = Context.Current.Request[key];
                }
            }

            if (!string.IsNullOrWhiteSpace(redirectResult))
            {
                details["redirectResult"] = redirectResult;
            }

            var postValues = new
            {
                details,
                paymentData = transactionData.PaymentData,
            };

            return HandlePaymentDetailsRequest(order, Converter.SerializeCompact(postValues));
        }

        private void OrderCompleted(Order order, Amount transactionAmount)
        {
            LogEvent(order, "State ok");

            if (transactionAmount != null && transactionAmount.Value.HasValue)
            {
                order.TransactionAmount = transactionAmount.Value.Value / 100d;
            }
            else
            {
                order.TransactionAmount = 0;
            }

            SetOrderComplete(order);
            CheckoutDone(order);

            if (Helper.IsAjaxRequest())
            {
                var receiptUrl = Helper.GetCartUrl(order, Converter.ToInt32(Context.Current.Request["ID"]));
                Helper.EndRequest(new { redirectToReceipt = receiptUrl });
            }

            RedirectToCart(order);
        }

        private string OrderRefused(Order order, string refusalReason)
        {
            order.TransactionAmount = 0;
            order.GatewayResult = refusalReason;
            var message = $"Payment was refused. RefusalReason: {refusalReason}";

            return OnError(order, message, null, Helper.IsAjaxRequest(), "Refused");
        }

        private string OrderCancelled(Order order)
        {
            order.TransactionAmount = 0;
            order.GatewayResult = "Payment has been cancelled";
            order.TransactionStatus = "Cancelled";

            var message = "Payment has been cancelled (either by the shopper or the merchant) before processing was completed";
            if (Helper.IsAjaxRequest())
            {
                OnError(order, message, null, true, "Cancelled");
            }
            else
            {
                CheckoutDone(order);
            }

            var cancelTemplate = new Template(TemplateHelper.GetTemplatePath(CancelTemplate, CancelTemplateFolder));
            cancelTemplate.SetTag("CheckoutHandler:CancelMessage", message);
            var orderRenderer = new Frontend.Renderer();
            orderRenderer.RenderOrderDetails(cancelTemplate, order, true);

            return cancelTemplate.Output();
        }

        private string PaymentError(Order order, string reason)
        {
            order.TransactionAmount = 0;
            order.GatewayResult = reason;
            var message = $"There was an error when the payment was being processed. Reason: {reason}";

            return OnError(order, message, null, Helper.IsAjaxRequest());
        }

        /// <summary>
        /// As per documentation, this result means that Adyen do not yet know actual payment result state
        /// </summary>
        private void PaymentReceived(Order order)
        {
            LogEvent(order, "Paynemt Pending or Received");

            order.TransactionAmount = 0;
            order.GatewayResult = "Paynemt pending or received";
            order.TransactionStatus = "Check payment in Adyen control panel."; //database limit is 50

            SetOrderComplete(order);
            CheckoutDone(order);

            if (Helper.IsAjaxRequest())
            {
                var receiptUrl = Helper.GetCartUrl(order, Converter.ToInt32(Context.Current.Request["ID"]));
                Helper.EndRequest(new { redirectToReceipt = receiptUrl });
            }

            RedirectToCart(order);
        }

        private string HandleAdyenPaymentResponse(Order order, PaymentResponse response, string jsonResponse, bool doSaveCard, string cardName, string paymentMethodType)
        {
            if (response.Action != null)
            {
                if (response.Action.Type.HasValue && response.Action.Type.Value == CheckoutPaymentsAction.CheckoutActionType.Redirect)
                {
                    var transactionData = new PaymentTransactionData(response.Action.PaymentData);
                    if (response.Details != null)
                    {
                        foreach (var detail in response.Details)
                        {
                            if (!string.IsNullOrEmpty(detail.Key))
                            {
                                transactionData.RequestKeys.Add(detail.Key);
                            }
                        }
                    }

                    order.TransactionCardType = response.Action.PaymentMethodType;
                    order.TransactionToken = Converter.SerializeCompact(transactionData);
                    Save(order);
                }

                if (!Helper.IsAjaxRequest())
                {
                    return OnError(order, "Wrong request", null, false);
                }

                Helper.EndRequest(jsonResponse);
                return null;
            }

            if (response.ResultCode.HasValue)
            {
                order.TransactionToken = null;
                order.TransactionStatus = Enum.GetName(typeof(PaymentResponse.PaymentResultCode), response.ResultCode);
                order.TransactionNumber = response.PspReference;

                if (PositivePaymentResultCodes.Contains(response.ResultCode.Value) && CardPaymentMethodName.Equals(paymentMethodType, StringComparison.OrdinalIgnoreCase))
                {
                    if (response.AdditionalData is null)
                    {
                        response.AdditionalData = new Dictionary<string, string>();
                    }

                    SetCardDetails(order, response.AdditionalData);

                    if (doSaveCard)
                    {
                        string cardToken;
                        response.AdditionalData.TryGetValue("recurring.recurringDetailReference", out cardToken);
                        SaveCard(order, cardName, cardToken);
                    }
                }

                switch (response.ResultCode)
                {
                    case PaymentResponse.PaymentResultCode.Authorised:
                        OrderCompleted(order, response.Amount);
                        return null;

                    case PaymentResponse.PaymentResultCode.Refused:
                        return OrderRefused(order, response.RefusalReason);

                    case PaymentResponse.PaymentResultCode.Cancelled:
                        return OrderCancelled(order);

                    case PaymentResponse.PaymentResultCode.Error:
                        return PaymentError(order, response.RefusalReason);

                    case PaymentResponse.PaymentResultCode.Received:
                    case PaymentResponse.PaymentResultCode.Pending:
                        PaymentReceived(order);
                        return null;

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
                        return null;
                }
            }

            return null;
        }

        private string HandlePaymentDetailsRequest(Order order, string json)
        {
            try
            {
                var paymentDetailsResponse = WebRequestHelper.Request(ApiUrlManager.GetPaymentDetailsUrl(), json, GetEnvironmentType(), ApiKey);
                var response = Converter.Deserialize<PaymentResponse>(paymentDetailsResponse);

                var doSaveCard = NeedSaveCard(order, CardPaymentMethodName, out var cardName);
                return HandleAdyenPaymentResponse(order, response, paymentDetailsResponse, doSaveCard, cardName, CardPaymentMethodName);
            }
            catch (Exception e) when (!(e is ThreadAbortException))
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
                {
                    doHandleNotification = false;
                }

                if (doHandleNotification)
                {
                    requestData = Converter.Deserialize<NotificationRequest>(jsonData);
                    if (requestData == null)
                    {
                        doHandleNotification = false;
                    }
                }

                if (DebugMode)
                {
                    LogEvent(order, "Notification contents: {0}", jsonData);
                }
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
                    var notificationItem = container.NotificationItem;
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
            var eventCode = notificationItem.GetNotificationEventCode();
            switch (eventCode)
            {
                case NotificationEventCode.Authorisation:
                    if (notificationItem.Success)
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
                            Save(order);
                        }
                    }
                    else
                    {
                        order.TransactionStatus = $"Authorisation failed: {notificationItem.Reason}";
                        if (DebugMode)
                        {
                            LogEvent(order, order.TransactionStatus);
                        }

                        Save(order);
                    }
                    break;

                case NotificationEventCode.Capture:
                case NotificationEventCode.CaptureFailed:
                    if (eventCode == NotificationEventCode.Capture && notificationItem.Success)
                    {
                        UpdateTransactionNumber(order, notificationItem.PspReference);
                        order.TransactionAmount = notificationItem.Amount.Value.Value / 100d;
                        order.CaptureAmount = order.TransactionAmount;
                        order.CaptureInfo.Message = "Capture successful";
                        order.CaptureInfo.State = OrderCaptureInfo.OrderCaptureState.Success;
                        if (!order.Complete)
                        {
                            SetOrderComplete(order);
                        }
                        else
                        {
                            Save(order);
                        }
                    }
                    else
                    {
                        var message = string.IsNullOrEmpty(notificationItem.Reason)
                            ? "Capture failed due to a technical issue"
                            : $"Capture failed: {notificationItem.Reason}";

                        SetCaptureFailed(order, message);
                    }
                    break;

                case NotificationEventCode.Cancellation:
                    order.TransactionAmount = 0;
                    if (notificationItem.Success)
                    {
                        Services.Taxes.CancelTaxes(order);
                        order.CaptureInfo = new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Cancel, string.Empty);
                        order.GatewayResult = "Payment has been cancelled";
                        order.TransactionStatus = "Cancelled";
                        UpdateTransactionNumber(order, notificationItem.OriginalReference);
                    }
                    else
                    {
                        var message = $"Payment cancellation failed: {notificationItem.Reason}";
                        if (DebugMode)
                        {
                            LogEvent(order, message);
                        }
                        order.GatewayResult = message;
                        order.CaptureInfo = new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, message);
                        order.TransactionStatus = "Cancel operation failed";
                        UpdateTransactionNumber(order, notificationItem.OriginalReference);
                    }
                    Save(order);
                    break;

                case NotificationEventCode.Refund:
                case NotificationEventCode.RefundFailed:
                    // only captured orders can receive this notifications
                    var doSave = false;
                    if (order.TransactionAmount < 0.001)
                    {
                        order.TransactionAmount = order.Price.Price;
                        doSave = true;
                    }
                    if (order.CaptureInfo.State != OrderCaptureInfo.OrderCaptureState.Success)
                    {
                        order.CaptureInfo.Message = "Capture successful";
                        order.CaptureInfo.State = OrderCaptureInfo.OrderCaptureState.Success;
                        doSave = true;
                    }
                    if (doSave)
                    {
                        Save(order);
                    }

                    var refundAmount = notificationItem.Amount.Value.Value / 100d;
                    var existingOperations = order.ReturnOperations ?? new List<OrderReturnInfo>();
                    var isFailedNotification = (notificationItem.Success && eventCode == NotificationEventCode.RefundFailed) || // literally: refund failed successfully
                                               (!notificationItem.Success && eventCode == NotificationEventCode.Refund); // refund was not succeed

                    if (notificationItem.MerchantReference.Contains(RefundIdDelimeter)) // operation was created on DW side
                    {
                        var delimeterPosition = notificationItem.MerchantReference.IndexOf(RefundIdDelimeter) + RefundIdDelimeter.Length;
                        var operationGuidRaw = notificationItem.MerchantReference.Substring(delimeterPosition);
                        Guid operationGuid;
                        if (Guid.TryParse(operationGuidRaw, out operationGuid))
                        {
                            OrderReturnInfo operationToHandle = null;
                            foreach (var operation in existingOperations)
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
                                if (isFailedNotification && operationToHandle.State != OrderReturnOperationState.Failed)
                                {
                                    operationToHandle.State = OrderReturnOperationState.Failed;
                                    operationToHandle.Message = notificationItem.Reason;
                                    Save(order);
                                }
                                else if (!isFailedNotification && operationToHandle.State == OrderReturnOperationState.Failed)
                                {
                                    var fullyReturned = Helper.IsFullAmountReturned(order, existingOperations, refundAmount);
                                    operationToHandle.State = fullyReturned ? OrderReturnOperationState.FullyReturned : OrderReturnOperationState.PartiallyReturned;
                                    operationToHandle.Message = "Order refund successful";
                                    Save(order);
                                }
                                return;
                            }
                        }
                    }

                    var finishedOperation = existingOperations.FirstOrDefault(o => o.State == OrderReturnOperationState.FullyReturned);
                    if (finishedOperation != null)
                    {
                        if (isFailedNotification)
                        {
                            // mark order as not fully returned. failed return operation will be added below.
                            finishedOperation.State = OrderReturnOperationState.PartiallyReturned;
                            Save(order);
                        }
                        else
                        {
                            return; // refund completed, no additional actions needed
                        }
                    }

                    // create a new return operation
                    if (isFailedNotification)
                    {
                        var message = $"Refund failed: {notificationItem.Reason}";
                        if (DebugMode)
                        {
                            LogEvent(order, message);
                        }

                        OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, message, refundAmount, order);
                    }
                    else
                    {
                        var fullyReturned = Helper.IsFullAmountReturned(order, existingOperations, refundAmount);
                        var state = fullyReturned
                            ? OrderReturnOperationState.FullyReturned
                            : OrderReturnOperationState.PartiallyReturned;

                        OrderReturnInfo.SaveReturnOperation(state, "Order refund successful", refundAmount, order);
                    }
                    break;

                default:
                    LogError(order, "Cannot handle notification item: event code is unknown or do not require the handling ({0})", notificationItem.EventCode);
                    break;
            }
        }

        #endregion

        #region IRemoteCapture

        public OrderCaptureInfo Capture(Order order)
        {
            LogEvent(order, "Attempting capture");

            var errorText = Helper.GetOrderError(order);
            if (!string.IsNullOrEmpty(errorText))
            {
                LogEvent(order, errorText);
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, errorText);
            }

            try
            {
                var captureRequest = new ModificationRequest(order, MerchantName, true);

                var captureResponseRaw = WebRequestHelper.Request(ApiUrlManager.GetCaptureUrl(order.TransactionNumber), captureRequest.ToJson(), GetEnvironmentType(), ApiKey);
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
            if (response.NotificationItems != null)
            {
                string infoTxt = string.Format("Payment was unsucceeded with error {0}/{1}", response.NotificationItems[0].Reason, response.Status);
                SetCaptureFailed(order, infoTxt);
                return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Failed, infoTxt);
            }

            order.CaptureInfo.Message = response.Status;
            order.TransactionAmount = response.Amount.Value.Value;

            LogEvent(order, "Capture successful", DebuggingInfoType.CaptureResult);
            return new OrderCaptureInfo(OrderCaptureInfo.OrderCaptureState.Success, "Capture successful");
        }

        #endregion

        #region ICancelOrder

        public bool CancelOrder(Order order)
        {
            LogEvent(order, "Attempting cancel");

            var errorText = Helper.GetOrderError(order);
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

                if (cancelResponse.NotificationItems != null)
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

            var amount = amountPIP / 100d;
            var operationId = Guid.NewGuid();
            var errorText = Helper.GetOrderError(order);
            if (!string.IsNullOrEmpty(errorText))
            {
                OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorText, amount, order, operationId);
                LogError(order, errorText, DebuggingInfoType.ReturnResult);
                return;
            }

            if (order.CaptureInfo == null || order.CaptureInfo.State != OrderCaptureInfo.OrderCaptureState.Success)
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

                var refundResponseRaw = WebRequestHelper.Request(ApiUrlManager.GetRefundUrl(order.TransactionNumber), refundRequest.ToJson(), GetEnvironmentType(), ApiKey);
                var refundResponse = Converter.DeserializeCompact<ModificationResponse>(refundResponseRaw);

                if (refundResponse.NotificationItems != null)
                {
                    errorText = string.Format("Refund failed with error {0}/{1}", refundResponse.NotificationItems[0].Reason, refundResponse.Status);
                    OrderReturnInfo.SaveReturnOperation(OrderReturnOperationState.Failed, errorText, amount, order, operationId);
                    LogError(order, errorText, DebuggingInfoType.ReturnResult);
                    return;
                }

                OrderReturnOperationState state;
                if (order.Price.Price - amount < 0.01)
                {
                    state = OrderReturnOperationState.FullyReturned;
                }
                else
                {
                    state = OrderReturnOperationState.PartiallyReturned;
                }

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
            PaymentCardToken savedCard = Services.PaymentCard.GetById(savedCardId);
            if (savedCard == null)
            {
                return;
            }

            try
            {
                var removeRequest = new RemoveSavedPaymentMethodRequest(savedCard, MerchantName);
                var result = WebRequestHelper.Request(ApiUrlManager.GetSavedPaymentMethodRemoveUrl(), removeRequest.ToJson(), GetEnvironmentType(), ApiKey);
            }
            catch { }
        }

        public string UseSavedCard(Order order) => StartCheckout(order);

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
            if (savedCard == null)
            {
                savedCard = Services.PaymentCard.CreatePaymentCard(order.CustomerAccessUserId, order.PaymentMethodId, cardName, order.TransactionCardType, order.TransactionCardNumber, cardToken);
            }

            order.SavedCardId = savedCard.ID;
            Save(order);

            LogEvent(order, "Saved card created: {0}", savedCard.Name);
        }

        #endregion

        #region Private methods

        private bool IsTestMode() => TestMode || string.IsNullOrEmpty(LiveUrlPrefix);

        private EnvironmentType GetEnvironmentType() => IsTestMode() ? EnvironmentType.Test : EnvironmentType.Live;

        private string GetCallbackUrl(Order order, NameValueCollection parameters) => Helper.GetCallbackUrl(GetBaseUrl(order), parameters);

        private string OnError(Order order, string message, Exception exception, bool isAjax, string transactionStatus = "Failed")
        {
            // Prepare data
            ServiceError serviceError = null;
            try
            {
                serviceError = Converter.Deserialize<ServiceError>(message);
                if (serviceError != null)
                {
                    message = serviceError.Message;
                }
            }
            catch { }

            if (exception != null)
            {
                LogError(order, exception, message);
            }
            else
            {
                LogError(order, message);
            }

            // Downgrade order, set context cart, etc
            order.TransactionStatus = transactionStatus;
            if (!order.Complete)
            {
                CheckoutDone(order);
            }

            // Show error
            if (isAjax)
            {
                if (serviceError != null)
                {
                    Helper.EndRequest(serviceError);
                }
                else
                {
                    Helper.EndRequest(new ServiceError { ErrorCode = "Unknown", Message = message });
                }

                return null;
            }

            if (string.IsNullOrWhiteSpace(ErrorTemplate))
            {
                RedirectToCart(order);
            }

            var errorTemplate = new Template(TemplateHelper.GetTemplatePath(ErrorTemplate, ErrorTemplateFolder));
            errorTemplate.SetTag("CheckoutHandler:ErrorMessage", message);

            return Render(order, errorTemplate);
        }

        private void UpdateTransactionNumber(Order order, string trnasactionId)
        {
            if (!string.IsNullOrEmpty(trnasactionId) && !trnasactionId.Equals(order.TransactionNumber, StringComparison.OrdinalIgnoreCase))
            {
                if (DebugMode)
                {
                    LogEvent(order, "Transaction number has changed. Old: '{0}', New: '{1}'", order.TransactionNumber, trnasactionId);
                }
                order.TransactionNumber = trnasactionId;
            }
        }

        private void SetCaptureFailed(Order order, string message)
        {
            LogError(order, message);

            order.CaptureInfo.Message = message;
            order.CaptureInfo.State = OrderCaptureInfo.OrderCaptureState.Failed;
            Save(order);
        }

        private static void Save(Order order) => Services.Orders.Save(order);

        private void SetCardDetails(Order order, Dictionary<string, string> additionalData)
        {
            string lastFour;
            if (!additionalData.TryGetValue("cardSummary", out lastFour)) // this information is optionally included/excluded in response (Adyen Account settings)
            {
                order.TransactionCardNumber = "<None>";
            }
            else
            {
                order.TransactionCardNumber = lastFour;
            }

            string cardType;
            // "paymentMethod" value is more human-readable, but it could be optionally included/excluded in response
            // then try to use "cardPaymentMethod", if presented
            if (!additionalData.TryGetValue("paymentMethod", out cardType) || string.IsNullOrEmpty(cardType))
            {
                if (!additionalData.TryGetValue("cardPaymentMethod", out cardType) || string.IsNullOrEmpty(cardType))
                {
                    cardType = "<Card>";
                }
            }
            order.TransactionCardType = cardType;

            Save(order);
        }

        #endregion
    }
}
