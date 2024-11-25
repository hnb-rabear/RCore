namespace RCore.Service
{
	/// <summary>
	/// Represents a notification that will be delivered for this application.
	/// </summary>
	public class GameNotification
	{
#if UNITY_NOTIFICATION
		private Unity.Notifications.Notification m_internalNotification;

		/// <summary>
		/// Gets the internal notification object used by the mobile notifications system.
		/// </summary>
		public Unity.Notifications.Notification InternalNotification => m_internalNotification;
		 
		/// <summary>
		/// A unique integer identifier for this notification.
		/// If null, will be generated automatically once the notification is delivered
		/// Is null on some platforms if not explicitly set.
		/// </summary>
		public int? Id { get => m_internalNotification.Identifier; set => m_internalNotification.Identifier = value; }

		/// <summary>
		/// The title message for the notification.
		/// </summary>
		public string Title { get => m_internalNotification.Title; set => m_internalNotification.Title = value; }

		/// <summary>
		/// The body message for the notification.
		/// </summary>
		public string Body { get => m_internalNotification.Text; set => m_internalNotification.Text = value; }

		/// <summary>
		/// Optional arbitrary data for the notification.
		/// </summary>
		public string Data { get => m_internalNotification.Data; set => m_internalNotification.Data = value; }

		/// <summary>
		/// The badge number for this notification. No badge number will be shown if null.
		/// </summary>
		/// <value>The number displayed on the app badge.</value>
		public int BadgeNumber { get => m_internalNotification.Badge; set => m_internalNotification.Badge = value; }

		public GameNotification()
		{
			m_internalNotification = new Unity.Notifications.Notification
			{
				ShowInForeground = true // Deliver in foreground by default
			};
		}
		
		public GameNotification(Unity.Notifications.Notification notification)
		{
			this.m_internalNotification = notification;
		}
#else
		public int? Id { get; set; }
		public string Title { get; set; }
		public string Body { get; set; }
		public string Data { get; set; }
		public int BadgeNumber { get; set; }
		public GameNotification() { }
		public GameNotification(object notification) { }
#endif
	}
}