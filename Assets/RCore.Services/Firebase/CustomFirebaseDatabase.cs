#if FIREBASE_DATABASE
using Firebase;
using Firebase.Database;
#endif
using System;

namespace RCore.Service
{
    public class CustomFirebaseDatabase
    {
        //-- UserData
        //---- UserProfile

        private static CustomFirebaseDatabase mInstance;
        public static CustomFirebaseDatabase Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new CustomFirebaseDatabase();
                return mInstance;
            }
        }

#if FIREBASE_DATABASE
        public RDatabaseReference userProfile;

        public CustomFirebaseDatabase(DatabaseReference pParent)
        {
            var userData = pParent.Child("UserData");
            userProfile = new RDatabaseReference("UserProfile", userData);
        }
        public CustomFirebaseDatabase()
        {

            var userData = FirebaseDatabase.DefaultInstance.GetReference("UserData");
            userProfile = new RDatabaseReference("UserProfile", userData);
        }

        public void RegisterNewuser(string pId, string pUserJsonData, Action<bool> pOnFinished)
        {
            userProfile.SetJsonData(pId, pUserJsonData, pOnFinished);
        }
        public void SaveUserDataToCloud(string pId, string pUserJsonData, Action<bool> pOnFinished)
        {
            userProfile.SetJsonDataPriority(pId, pUserJsonData, pOnFinished);
        }
        public void GetUserData(string pId, Action<string, bool> pOnFinished)
        {
            userProfile.GetData(pId, pOnFinished);
        }
        public void RemoveUserData(string pId, Action<bool> pOnFinished)
        {
            userProfile.RemoveData(pId, pOnFinished);
        }

        /// <summary>
        /// Used to get a dictionary of uses, that fit the condition
        /// Most used for PVP feature or Leaderboard
        /// </summary>
        /// <param name="limit">Limitation of dictionary</param>
        /// <param name="orderBy">Condition to search for eg. power/level/etc... </param>
        /// <param name="endAt">Stop query when reaching this value</param>
        /// <param name="pOnFinished">Callback with value is a json list (This list can be parsed to a Dictionary)</param>
        public void GetListUsersData(int limit, string orderBy, int endAt, Action<bool, string> pOnFinished)
        {
            var task = userProfile.reference
                .OrderByChild(orderBy)
                .EndAt(endAt)
                .LimitToLast(limit)
                .GetValueAsync();
            TimerEventsGlobal.Instance.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                string jsonData = "";
                if (success)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot != null)
                    {
                        if (snapshot.Exists)
                            jsonData = snapshot.GetRawJsonValue();
                    }
                }
                pOnFinished?.Invoke(success, jsonData);
            });
        }
#else
        public CustomFirebaseDatabase(object pParent) { }
        public CustomFirebaseDatabase() { }
        public void RegisterNewuser(string pId, string pUserJsonData, Action<bool> pOnFinished) { }
        public void SaveUserDataToCloud(string pId, string pUserJsonData, Action<bool> pOnFinished) { }
        public void GetUserData(string pId, Action<string, bool> pOnFinished) { }
        public void RemoveUserData(string pId, Action<bool> pOnFinished) { }
        public void GetListUsersData(int limit, string orderBy, int endAt, Action<bool, string> pOnFinished) { }
#endif
    }
}