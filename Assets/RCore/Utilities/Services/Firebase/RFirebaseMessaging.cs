using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ACTIVE_FIREBASE_MESSAGING
using Firebase.Messaging;
#endif
using System.Threading.Tasks;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Service.RFirebase
{
    public class RFirebaseMessaging
    {
        #region Members

        private static RFirebaseMessaging mInstance;
        public static RFirebaseMessaging Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new RFirebaseMessaging();
                return mInstance;
            }
        }

        public Action<FirebaseMessage> onRecievedNotification;
        public Action<string> onRecivedToken;

        private bool mIntiailized;
        private FirebaseNotification mNotification;

        public FirebaseNotification Notification => mNotification;

        #endregion

        //=====================================

        #region Public

        public void Initialize()
        {
#if ACTIVE_FIREBASE_MESSAGING
            if (mIntiailized)
                return;

            FirebaseMessaging.MessageReceived += OnMessageRecieved;
            FirebaseMessaging.TokenReceived += OnTokenReceived;

            // This will display the prompt to request permission to receive
            // notifications if the prompt has not already been displayed before. (If
            // the user already responded to the prompt, thier decision is cached by
            // the OS and can be changed in the OS settings).
            var task = FirebaseMessaging.RequestPermissionAsync();
            WaitUtil.WaitTask(task, () =>
            {
                LogTaskCompletion(task, "RequestPermissionAsync");
            });
            mIntiailized = true;
#endif
        }

        #endregion

        //=====================================

        #region Private

#if ACTIVE_FIREBASE_MESSAGING
        private void OnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
#if DEVELOPMENT || UNITY_EDITOR
            Debug.Log("Received Registration Token: " + e.Token);
#endif
            onRecivedToken?.Invoke(e.Token);
        }

        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
        {
#if DEVELOPMENT || UNITY_EDITOR
            string log = "Received a new message\n";
            var notification = e.Message.Notification;
            if (notification != null)
            {
                var android = notification.Android;
                if (android != null)
                    log += $"android channel_id: {android.ChannelId}\n";
            }

            if (e.Message.From.Length > 0)
                log += $"from: {e.Message.From}\n";

            if (e.Message.Link != null)
                log += $"link: {e.Message.Link.ToString()}\n";

            if (e.Message.Data.Count > 0)
            {
                string logData = "data:\n";
                foreach (KeyValuePair<string, string> iter in e.Message.Data)
                    logData += $"\t{iter.Key}: {iter.Value}\n";
                log += logData;
            }
            Debug.Log(log);
#endif
            onRecievedNotification?.Invoke(new FirebaseMessage(e.Message));
        }

        private bool LogTaskCompletion(Task task, string operation)
        {
            bool complete = false;
            if (task.IsCanceled)
            {
                Debug.Log(operation + " canceled.");
            }
            else if (task.IsFaulted)
            {
                Debug.Log(operation + " encounted an error.");
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    string errorCode = "";
                    var firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                        errorCode = string.Format("Error.{0}: ", ((Error)firebaseEx.ErrorCode).ToString());

                    Debug.Log(errorCode + exception.ToString());
                }
            }
            else if (task.IsCompleted)
            {
                Debug.Log(operation + " completed");
                complete = true;
            }
            return complete;
        }
#endif

        #endregion
    }
}