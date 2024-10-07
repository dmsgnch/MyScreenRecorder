using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace MyScreenRecorder.Services;

public static class NotificationService
{
    public static void ShowToastNotification(string message)
    {
        string toastXmlString = $@"
            <toast>
                <visual>
                    <binding template='ToastGeneric'>
                        <text>Notification</text>
                        <text>{message}</text>
                    </binding>
                </visual>
            </toast>";

        XmlDocument toastXml = new XmlDocument();
        toastXml.LoadXml(toastXmlString);

        ToastNotification toast = new ToastNotification(toastXml);

        ToastNotificationManager.CreateToastNotifier().Show(toast);
    }
}