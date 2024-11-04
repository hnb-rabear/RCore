using System;
using System.Collections;
#if UNITY_NOTIFICATION
using System.Collections;
using Unity.Notifications;
#endif

namespace RCore.Notification
{
	/// <summary>
	/// A type that handles notifications for a specific game platform
	/// </summary>
	public class NotificationsPlatform : IDisposable
	{
		/// <summary>
		/// Fired when a notification is received.
		/// </summary>
		public event Action<GameNotification> NotificationReceived;

		public GameNotification CreateNotification()
		{
			return new GameNotification();
		}
		
#if UNITY_NOTIFICATION
        /// <summary>
        /// Create the platform with provided notification settings.
        /// </summary>
        public NotificationsPlatform(NotificationCenterArgs args)
        {
            NotificationCenter.Initialize(args);
            NotificationCenter.OnNotificationReceived += OnLocalNotificationReceived;
        }

        /// <summary>
        /// Request permission to send notifications.
        /// </summary>
        public IEnumerator RequestNotificationPermission()
        {
            return NotificationCenter.RequestPermission();
        }

        /// <summary>
        /// Schedules a notification to be delivered.
        /// </summary>
        /// <param name="gameNotification">The notification to deliver.</param>
        /// <exception cref="ArgumentNullException"><paramref name="gameNotification"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="gameNotification"/> isn't of the correct type.</exception>
        public void ScheduleNotification(GameNotification gameNotification, DateTime deliveryTime)
        {
#if UNITY_NOTIFICATION
            if (gameNotification == null)
            {
                throw new ArgumentNullException(nameof(gameNotification));
            }

            int notificationId = NotificationCenter.ScheduleNotification(gameNotification.InternalNotification, new NotificationDateTimeSchedule(deliveryTime));
            gameNotification.Id = notificationId;
#endif
        }

        /// <summary>
        /// Cancels a scheduled notification.
        /// </summary>
        /// <param name="notificationId">The ID of a previously scheduled notification.</param>
        public void CancelNotification(int notificationId)
        {
            NotificationCenter.CancelScheduledNotification(notificationId);
        }

        /// <summary>
        /// Dismiss a displayed notification.
        /// </summary>
        /// <param name="notificationId">The ID of a previously scheduled notification that is being displayed to the user.</param>
        public void DismissNotification(int notificationId)
        {
            NotificationCenter.CancelDeliveredNotification(notificationId);
        }

        /// <summary>
        /// Cancels all scheduled notifications.
        /// </summary>
        public void CancelAllScheduledNotifications()
        {
            NotificationCenter.CancelAllScheduledNotifications();
        }

        /// <summary>
        /// Dismisses all displayed notifications.
        /// </summary>
        public void DismissAllDisplayedNotifications()
        {
            NotificationCenter.CancelAllDeliveredNotifications();
        }

        /// <summary>
        /// Use this to retrieve the last local or remote notification received by the app.
        /// </summary>
        /// <remarks>
        /// On Android the last notification is not cleared until the application is explicitly quit.
        /// </remarks>
        /// <returns>
        /// Returns the last local or remote notification used to open the app or clicked on by the user. If no
        /// notification is available it returns null.
        /// </returns>
        public GameNotification GetLastNotification()
        {
            var notification = NotificationCenter.LastRespondedNotification;

            if (notification.HasValue)
            {
                return new GameNotification(notification.Value);
            }

            return null;
        }

        /// <summary>
        /// Performs any initialization or processing necessary on foregrounding the application.
        /// </summary>
        public void OnForeground()
        {
            NotificationCenter.ClearBadge();
        }

        /// <summary>
        /// Performs any processing necessary on backgrounding or closing the application.
        /// </summary>
        public void OnBackground() {}

        /// <summary>
        /// Unregister delegates.
        /// </summary>
        public void Dispose()
        {
            NotificationCenter.OnNotificationReceived -= OnLocalNotificationReceived;
        }

        // Event handler for receiving local notifications.
        private void OnLocalNotificationReceived(Unity.Notifications.Notification notification)
        {
            // Create a new AndroidGameNotification out of the delivered notification, but only
            // if the event is registered
            NotificationReceived?.Invoke(new GameNotification(notification));
        }
#else
		public void Dispose() { }
		public void OnBackground() { }
		public void ScheduleNotification(GameNotification gameNotification, DateTime deliveryTime) { }
		public void CancelNotification(int notificationId) { }
		public void CancelAllScheduledNotifications() { }
		public void DismissNotification(int notificationId) { }
		public void DismissAllDisplayedNotifications() { }
		public GameNotification GetLastNotification() => null;
		public void OnForeground() { }
		public IEnumerator RequestNotificationPermission() { yield break; }
#endif
	}
}