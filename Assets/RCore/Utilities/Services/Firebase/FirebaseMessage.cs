using System;
using System.Collections.Generic;

namespace RCore.Service
{
    public class FirebaseMessage
    {
        public string ErrorDescription { get; private set; }
        public string Error { get; private set; }
        public string Priority { get; private set; }
        public string MessageType { get; private set; }
        public string MessageId { get; private set; }
        public Byte[] RawData { get; private set; }
        public IDictionary<string, string> Data { get; private set; }
        public string CollapseKey { get; private set; }
        public string To { get; private set; }
        public string From { get; private set; }
        public Uri Link { get; private set; }
        public FirebaseNotification Notification { get; private set; }
        public TimeSpan TimeToLive { get; private set; }
        public bool NotificationOpened { get; private set; }

#if ACTIVE_FIREBASE_MESSAGING
        public FirebaseMessage(Firebase.Messaging.FirebaseMessage pMessage)
        {
            ErrorDescription = pMessage.ErrorDescription;
            Error = pMessage.Error;
            Priority = pMessage.Priority;
            MessageType = pMessage.MessageType;
            MessageId = pMessage.MessageId;
            RawData = pMessage.RawData;
            Data = pMessage.Data;
            CollapseKey = pMessage.CollapseKey;
            To = pMessage.To;
            From = pMessage.From;
            Link = pMessage.Link;
            Notification = new FirebaseNotification(pMessage.Notification);
            TimeToLive = pMessage.TimeToLive;
            NotificationOpened = pMessage.NotificationOpened;
        }
#endif
    }

    public class FirebaseNotification
    {
        public string Title { get; private set; }
        public string Body { get; private set; }
        public string Icon { get; private set; }
        public string Sound { get; private set; }
        public string Badge { get; private set; }
        public string Tag { get; private set; }
        public string Color { get; private set; }
        public string ClickAction { get; private set; }
        public string BodyLocalizationKey { get; private set; }
        public IEnumerable<string> BodyLocalizationArgs { get; private set; }
        public string TitleLocalizationKey { get; private set; }
        public IEnumerable<string> TitleLocalizationArgs { get; private set; }

#if ACTIVE_FIREBASE_MESSAGING
        public FirebaseNotification(Firebase.Messaging.FirebaseNotification pNotification)
        {
            Title = pNotification != null ? pNotification.Title : string.Empty;
            Body = pNotification != null ? pNotification.Body : string.Empty;
            Icon = pNotification != null ? pNotification.Icon : string.Empty;
            Sound = pNotification != null ? pNotification.Sound : string.Empty;
            Badge = pNotification != null ? pNotification.Badge : string.Empty;
            Tag = pNotification != null ? pNotification.Tag : string.Empty;
            Color = pNotification != null ? pNotification.Color : string.Empty;
            ClickAction = pNotification != null ? pNotification.ClickAction : string.Empty;
            BodyLocalizationKey = pNotification != null ? pNotification.BodyLocalizationKey : string.Empty;
            BodyLocalizationArgs = pNotification != null ? pNotification.BodyLocalizationArgs : null;
            TitleLocalizationKey = pNotification != null ? pNotification.TitleLocalizationKey : string.Empty;
            TitleLocalizationArgs = pNotification != null ? pNotification.TitleLocalizationArgs : null;
        }
#endif
    }
}