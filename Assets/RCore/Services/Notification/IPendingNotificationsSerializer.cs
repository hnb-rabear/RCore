using System.Collections.Generic;

namespace RCore.Notification
{
    /// <summary>
    /// Responsible for the serialization and deserialization of pending notifications for a
    /// <see cref="NotificationsManager"/> that is in <see
    /// cref="NotificationsManager.OperatingMode.RescheduleAfterClearing"/> mode.
    /// </summary>
    public interface IPendingNotificationsSerializer
    {
        /// <summary>
        /// Save a list of pending notifications.
        /// </summary>
        /// <param name="notifications">The collection notifications to save.</param>
        void Serialize(IList<PendingNotification> notifications);

        /// <summary>
        /// Retrieve a saved list of pending notifications.
        /// </summary>
        /// <returns>The deserialized collection of pending notifications, or null if the file did not exist.</returns>
        IList<PendingNotification> Deserialize(NotificationsPlatform platform);
    }
}