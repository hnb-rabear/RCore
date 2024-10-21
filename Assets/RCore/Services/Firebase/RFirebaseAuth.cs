/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if FIREBASE_AUTH
using Firebase;
using Firebase.Auth;
#endif

namespace RCore.Service
{
    public static class RFirebaseAuth
    {
#if FIREBASE_AUTH
        private static bool m_FetchingToken = true;
        private static Dictionary<string, FirebaseUser> m_UserByAuth;

        private static FirebaseAuth auth => FirebaseAuth.DefaultInstance;
        public static bool initialized { get; private set; }
        public static bool authenticated => auth.CurrentUser != null;

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            m_UserByAuth = new Dictionary<string, FirebaseUser>();
            auth.StateChanged += OnStateChanged;
            auth.IdTokenChanged += OnTokenChanged;
            OnStateChanged(auth, null);
        }

        public static void LogUserInfo()
        {
            if (auth.CurrentUser == null)
                Debug.Log("RFirebaseAuth:LogUserInfo Not signed in, unable to get info.");
            else
                Debug.Log(string.Format("RFirebaseAuth:LogUserInfo Current user info: anonymous: {0}, displayName: {1}, email: {2}, userId: {3}",
                    auth.CurrentUser.IsAnonymous, auth.CurrentUser.DisplayName, auth.CurrentUser.Email, auth.CurrentUser.UserId));
        }

        //======== Sign in

        public static Task SigninAnonymouslyAsync()
        {
            var task = auth.SignInAnonymouslyAsync();
            TimerEventsGlobal.Instance.WaitTask(task, () =>
            {
                LogTaskCompletion(task, "Sign-in");
                LogUserInfo();
            });
            return task;
        }

        /// <summary>
        /// Sign in with the token from your authentication server. (eg. GPGS)
        /// </summary>
        public static Task SignInWithCustomTokenAsync(string pToken)
        {
            var task = auth.SignInWithCustomTokenAsync(pToken);
            TimerEventsGlobal.Instance.WaitTask(task, () =>
            {
                LogTaskCompletion(task, "Sign-in");
                LogUserInfo();
            });
            return task;
        }

        public static Task SigninWithEmailAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword)
        {
            if (pSignInAndFetchProfile)
            {
                var task = auth.SignInAndRetrieveDataWithCredentialAsync(EmailAuthProvider.GetCredential(pEmail, pPassword));
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Sign-in");
                    LogUserInfo();
                });
                return task;
            }
            else
            {
                var task = auth.SignInWithEmailAndPasswordAsync(pEmail, pPassword);
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Sign-in");
                    LogUserInfo();
                });
                return task;
            }
        }

        /// <summary>
        /// This is functionally equivalent to the Signin() function.  However, it
        /// illustrates the use of Credentials, which can be aquired from many
        /// different sources of authentication.
        /// </summary>
        public static Task SigninWithEmailCredentialAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword)
        {
            if (pSignInAndFetchProfile)
            {
                var task = auth.SignInAndRetrieveDataWithCredentialAsync(EmailAuthProvider.GetCredential(pEmail, pPassword));
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Sign-in");
                    LogUserInfo();
                });
                return task;
            }
            else
            {
                var task = auth.SignInWithCredentialAsync(EmailAuthProvider.GetCredential(pEmail, pPassword));
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Sign-in");
                    LogUserInfo();
                });
                return task;
            }
        }

        /// <summary>
        /// Reauthenticate the user with the current email / password.
        /// </summary>
        public static Task ReauthenticateAsync(bool signInAndFetchProfile, string email, string password)
        {
            var user = auth.CurrentUser;
            if (user == null)
            {
                Debug.Log("Not signed in, unable to reauthenticate user.");
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetException(new Exception("Not signed in"));
                return tcs.Task;
            }
            var cred = EmailAuthProvider.GetCredential(email, password);
            if (signInAndFetchProfile)
            {
                var task = user.ReauthenticateAndRetrieveDataAsync(cred);
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Reauthentication");
                });
                return task;
            }
            else
            {
                var task = user.ReauthenticateAsync(cred);
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Reauthentication");
                });
                return task;
            }
        }

        /// <summary>
        /// Unlink the email credential from the currently logged in user.
        /// </summary>
        public static Task UnlinkEmailAsync(string pEmail, string pPassword)
        {
            if (auth.CurrentUser == null)
            {
                Debug.Log("Not signed in, unable to unlink");
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetException(new Exception("Not signed in"));
                return tcs.Task;
            }
            var task = auth.CurrentUser.UnlinkAsync(EmailAuthProvider.GetCredential(pEmail, pPassword).Provider);
            TimerEventsGlobal.Instance.WaitTask(task, () => { LogTaskCompletion(task, "Unlinking"); });
            return task;
        }


        //========

        public static void ReloadUser()
        {
            var task = auth.CurrentUser.ReloadAsync();
            TimerEventsGlobal.Instance.WaitTask(task, () =>
            {
                LogTaskCompletion(task, "Reload");
                LogUserInfo();
            });
        }

        public static void SignOut()
        {
            auth.SignOut();
        }

        public static Task DeleteUserAsync()
        {
            if (auth.CurrentUser != null)
            {
                var task = auth.CurrentUser.DeleteAsync();
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Delete user");
                });
                return task;
            }
            else
            {
                Debug.Log("Sign-in before deleting user.");
                // Return a finished task.
                return Task.FromResult(0);
            }
        }

        //========

        public static void GetUserToken()
        {
            if (auth.CurrentUser == null)
            {
                Debug.Log("RFirebaseAuth:GetUserToken Not signed in, unable to get token.");
                return;
            }
            m_FetchingToken = true;
            var task = auth.CurrentUser.TokenAsync(false);
            TimerEventsGlobal.Instance.WaitTask(task, () =>
            {
                m_FetchingToken = false;
                LogTaskCompletion(task, "User token fetch");
            });
        }

        private static bool LogTaskCompletion(Task task, string operation)
        {
            if (task.IsCanceled)
                Debug.Log($"RFirebaseAuth:{operation} canceled.");

            else if (task.IsFaulted)
            {
                Debug.Log($"RFirebaseAuth:{operation} encounter an error.");
                foreach (var exception in task.Exception.Flatten().InnerExceptions)
                {
                    string authErrorCode = "";
                    if (exception is FirebaseException firebaseEx)
                        authErrorCode = string.Format("AuthError.{0}: ", ((AuthError)firebaseEx.ErrorCode).ToString());

                    Debug.Log($"RFirebaseAuth:{operation} {authErrorCode}");
                }
            }
            else if (task.IsCompleted)
                Debug.Log($"RFirebaseAuth:{operation} completed");

            return task.IsCompleted;
        }

        private static void OnTokenChanged(object sender, EventArgs e)
        {
            var senderAuth = sender as FirebaseAuth;
            if (senderAuth == auth && senderAuth.CurrentUser != null && !m_FetchingToken)
            {
                var task = senderAuth.CurrentUser.TokenAsync(false);
                TimerEventsGlobal.Instance.WaitTask(task, () =>
                {
                    Debug.Log(string.Format("[RFirebaseAuth:OnTokenChanged] Token[0:8] = {0}", task.Result.Substring(0, 8)));
                });
            }
        }

        private static void OnStateChanged(object sender, EventArgs e)
        {
            var senderAuth = sender as FirebaseAuth;
            FirebaseUser user = null;
            if (senderAuth != null)
                m_UserByAuth.TryGetValue(senderAuth.App.Name, out user);
            if (senderAuth == auth && senderAuth.CurrentUser != user)
            {
                bool signedIn = user != senderAuth.CurrentUser && senderAuth.CurrentUser != null;
                if (!signedIn && user != null)
                {
                    Debug.Log("RFirebaseAuth:OnStateChanged user" + user.UserId + " sign out!");
                }
                user = senderAuth.CurrentUser;
                m_UserByAuth[senderAuth.App.Name] = user;
                if (signedIn)
                {
                    Debug.Log("RFirebaseAuth:OnStateChanged user" + user.UserId + " sign in!");
                }
            }
        }
#else
        public static bool initialized { get; private set; }
        public static bool logged => false;
        public static void Initialize() { }
        public static void LogUserInfo() { }
        public static void SignOut() { }
        public static Task DeleteUserAsync() { return Task.FromResult(0); }
        public static Task SigninAnonymouslyAsync() { return Task.FromResult(0); }
        public static Task SignInWithCustomTokenAsync(string pToken) { return Task.FromResult(0); }
        public static Task SigninWithEmailAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword) { return Task.FromResult(0); }
        public static Task SigninWithEmailCredentialAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword) { return Task.FromResult(0); }
        public static Task LinkWithEmailCredentialAsync(bool signInAndFetchProfile, string email, string password) { return Task.FromResult(0); }
        public static Task ReauthenticateAsync(bool signInAndFetchProfile, string email, string password) { return Task.FromResult(0); }
        public static Task UnlinkEmailAsync(string pEmail, string pPassword) { return Task.FromResult(0); }
        public static void ReloadUser() { }
        public static void GetUserToken() { }
#endif
    }
}