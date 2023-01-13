namespace Dynamicweb.Ecommerce.CheckoutHandlers.Adyen.Model.Notification
{
    // This is just a part of possible codes.
    // See https://docs.adyen.com/development-resources/webhooks/understand-notifications?tab=%23codeBlockIfBjr_Json#event-codes
    public enum NotificationEventCode
    {
        Authorisation,
        Cancellation,
        Capture,
        CaptureFailed,
        Refund,
        RefundFailed,
    }
}
