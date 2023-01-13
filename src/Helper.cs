using Dynamicweb.Configuration;
using Dynamicweb.Core;
using Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification;
using Dynamicweb.Ecommerce.Orders;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen
{
    internal static class Helper
    {
        public static bool IsAjaxRequest()
        {
            return "application/json".Equals(Context.Current.Request.Headers["Content-Type"], StringComparison.OrdinalIgnoreCase);
        }

        public static void EndRequest<T>(T obj) => EndRequest(Converter.SerializeCompact(obj));

        public static void EndRequest(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                Context.Current.Response.Clear();
                Context.Current.Response.Write(json);
                Context.Current.Response.Flush();
            }
            Context.Current.Response.End();
        }

        public static string GetCallbackUrl(string baseUrl, NameValueCollection parameters)
        {
            var uriBuilder = new UriBuilder(baseUrl);
            if (parameters != null)
            {
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                foreach (string key in parameters)
                {
                    query[key] = parameters[key];
                }
                uriBuilder.Query = query.ToString();
            }

            return uriBuilder.Uri.ToString();
        }

        public static string GetCartUrl(Order order, int pageId)
        {
            var url = Context.Current.Request.Url;
            var disablePortNumber = SystemConfiguration.Instance.GetBoolean("/Globalsettings/System/http/DisableBaseHrefPort");
            var port = disablePortNumber ? "" : url.IsDefaultPort ? string.Empty : string.Format(":{0}", url.Port).ToString();
            var urlString = new StringBuilder(string.Format("{0}://{1}{2}", url.Scheme, url.Host, port));

            urlString.AppendFormat("/Default.aspx?ID={0}", pageId);
            if (order.Complete || order.IsQuote)
            {
                urlString.AppendFormat("&CompletedOrderId={0}", order.Id);
                urlString.AppendFormat("&CompletedOrderSecret={0}", order.Secret);
            }

            return urlString.ToString();
        }

        public static string GetOrderError(Order order)
        {
            var errorText = string.Empty;
            if (order == null)
            {
                errorText = "Order is not set";
            }
            else if (string.IsNullOrEmpty(order.Id))
            {
                errorText = "Order id is not set";
            }
            else if (string.IsNullOrEmpty(order.TransactionNumber))
            {
                errorText = "Transaction number is not set";
            }

            return errorText;
        }

        public static bool IsFullAmountReturned(Order order, IEnumerable<OrderReturnInfo> operations, double refundAmount)
        {
            double totalRefundedAmount = GetTotalRefundedAmount(operations.Where(o => o.State != OrderReturnOperationState.Failed));
            return Math.Abs(order.TransactionAmount - (refundAmount + totalRefundedAmount)) < 0.001;
        }

        private static double GetTotalRefundedAmount(IEnumerable<OrderReturnInfo> operations)
        {
            double result = 0d;
            foreach (var refundOperation in operations)
            {
                result += refundOperation.Amount;
            }
            return result;
        }

        #region HMAC validator

        public static bool IsValidHmac(NotificationRequestItem notificationRequestItem, string key)
        {
            var notificationRequestItemData = GetDataToSign(notificationRequestItem);
            var expectedSign = CalculateHmac(notificationRequestItemData, key);
            var merchantSign = notificationRequestItem.AdditionalData[NotificationRequestItem.HmacSignatureKey];

            return string.Equals(expectedSign, merchantSign);
        }

        private static string GetDataToSign(NotificationRequestItem notificationRequestItem)
        {
            var amount = notificationRequestItem.Amount;
            var signedDataList = new List<string>
            {
                notificationRequestItem.PspReference,
                notificationRequestItem.OriginalReference,
                notificationRequestItem.MerchantAccountCode,
                notificationRequestItem.MerchantReference,
                Converter.ToString(amount.Value),
                amount.Currency,
                notificationRequestItem.EventCode,
                notificationRequestItem.Success.ToString().ToLower()
            };

            return string.Join(":", signedDataList);
        }

        private static string CalculateHmac(string signingstring, string hmacKey)
        {
            byte[] key = PackH(hmacKey);
            byte[] data = Encoding.UTF8.GetBytes(signingstring);

            try
            {
                using (var hmac = new HMACSHA256(key))
                {
                    // Compute the hmac on input data bytes
                    byte[] rawHmac = hmac.ComputeHash(data);

                    // Base64-encode the hmac
                    return Convert.ToBase64String(rawHmac);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to generate HMAC: " + e.Message);
            }
        }

        private static byte[] PackH(string hex)
        {
            if ((hex.Length % 2) == 1)
            {
                hex += '0';
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        #endregion
    }
}
