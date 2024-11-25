using System;
using System.Collections.Generic;
using System.IO;

namespace RCore.Service
{
	/// <summary>
	/// Standard serializer used by the <see cref="NotificationsManager"/> if no others
	/// are provided. Saves a simple binary format.
	/// </summary>
	public class DefaultSerializer : IPendingNotificationsSerializer
	{
		private readonly string m_filename;

		public DefaultSerializer(string filename)
		{
			this.m_filename = filename;
		}

		public void Serialize(IList<PendingNotification> notifications)
		{
			try
			{
				using var file = new FileStream(m_filename, FileMode.Create);
				using var writer = new BinaryWriter(file);
				
				writer.Write(1); // Write version number
				writer.Write(notifications.Count); // Write list length
				foreach (var notificationToSave in notifications)
				{
					var notification = notificationToSave.Notification;
					writer.Write(notification.Id.HasValue);
					if (notification.Id.HasValue)
						writer.Write(notification.Id.Value);
					writer.Write(notification.Title ?? "");
					writer.Write(notification.Body ?? "");
					//writer.Write(notification.Subtitle ?? "");
					writer.Write(notification.Data ?? "");
					writer.Write(notification.BadgeNumber);
					writer.Write(notificationToSave.DeliveryTime.Ticks);
				}

				writer.Flush();
			}
			catch (IOException e)
			{
				Debug.LogException(e);
			}
		}

		public IList<PendingNotification> Deserialize(NotificationsPlatform platform)
		{
			if (!File.Exists(m_filename))
				return null;

			try
			{
				using var file = new FileStream(m_filename, FileMode.Open);
				using var reader = new BinaryReader(file);
				var version = reader.ReadByte();
				int numElements = reader.ReadInt32();
				var result = new List<PendingNotification>(numElements);
				for (var i = 0; i < numElements; ++i)
				{
					var notification = platform.CreateNotification();
					bool hasValue = reader.ReadBoolean();
					if (hasValue)
						notification.Id = reader.ReadInt32();
					notification.Title = reader.ReadString();
					notification.Body = reader.ReadString();
					// Data, introduced in version 1
					if (version > 0)
						notification.Data = reader.ReadString();
					notification.BadgeNumber = reader.ReadInt32();
					var deliveryTime = new DateTime(reader.ReadInt64(), DateTimeKind.Local);

					result.Add(new PendingNotification(notification, deliveryTime));
				}

				return result;
			}
			catch (IOException e)
			{
				Debug.LogException(e);
				return null;
			}
		}
	}
}