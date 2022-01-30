/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RCore.Common;
#if ACTIVE_FIREBASE_AUTH
using Firebase;
using Firebase.Auth;
#endif

namespace RCore.Service.RFirebase.Auth
{
    public class RFirebaseAuth
    {
        private static RFirebaseAuth mInstance;
        public static RFirebaseAuth Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new RFirebaseAuth();
                return mInstance;
            }
        }

#if ACTIVE_FIREBASE_AUTH
        private bool mFetchingToken = true;
        private Dictionary<string, FirebaseUser> mUserByAuth;
        private FirebaseAuth mAuth;

        public bool initialized { get; private set; }
        public bool authenticated { get { return mAuth.CurrentUser != null; } }

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            mUserByAuth = new Dictionary<string, FirebaseUser>();
            mAuth = FirebaseAuth.DefaultInstance;
            mAuth.StateChanged += OnStateChanged;
            mAuth.IdTokenChanged += OnTokenChanged;
            OnStateChanged(mAuth, null);
        }

        public void LogUserInfo()
        {
            if (mAuth.CurrentUser == null)
                Debug.Log("FirebaseAuth::GetUserInfo Not signed in, unable to get info.");
            else
                Debug.Log(string.Format("FirebaseAuth::GetUserInfo Current user info: anonymous: {0}, displayName: {1}, email: {2}, userId: {3}",
                    mAuth.CurrentUser.IsAnonymous, mAuth.CurrentUser.DisplayName, mAuth.CurrentUser.Email, mAuth.CurrentUser.UserId));
        }

        //======== Sign in

        public Task SigninAnonymouslyAsync()
        {
            var task = mAuth.SignInAnonymouslyAsync();
            WaitUtil.WaitTask(task, () =>
            {
                LogTaskCompletion(task, "Sign-in");
                LogUserInfo();
            });
            return task;
        }

        /// <summary>
        /// Sign in with the token from your authentication server. (eg. GPGS)
        /// </summary>
        public Task SignInWithCustomTokenAsync(string pToken)
        {
            var task = mAuth.SignInWithCustomTokenAsync(pToken);
            WaitUtil.WaitTask(task, () =>
            {
                LogTaskCompletion(task, "Sign-in");
                LogUserInfo();
            });
            return task;
        }

        public Task SigninWithEmailAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword)
        {
            if (pSignInAndFetchProfile)
            {
                var task = mAuth.SignInAndRetrieveDataWithCredentialAsync(EmailAuthProvider.GetCredential(pEmail, pPassword));
                WaitUtil.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Sign-in");
                    LogUserInfo();
                });
                return task;
            }
            else
            {
                var task = mAuth.SignInWithEmailAndPasswordAsync(pEmail, pPassword);
                WaitUtil.WaitTask(task, () =>
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
        public Task SigninWithEmailCredentialAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword)
        {
            if (pSignInAndFetchProfile)
            {
                var task = mAuth.SignInAndRetrieveDataWithCredentialAsync(EmailAuthProvider.GetCredential(pEmail, pPassword));
                WaitUtil.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Sign-in");
                    LogUserInfo();
                });
                return task;
            }
            else
            {
                var task = mAuth.SignInWithCredentialAsync(EmailAuthProvider.GetCredential(pEmail, pPassword));
                WaitUtil.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Sign-in");
                    LogUserInfo();
                });
                return task;
            }
        }

        /// <summary>
        /// Link the current user with an email / password credential.
        /// </summary>
        public Task LinkWithEmailCredentialAsync(bool signInAndFetchProfile, string email, string password)
        {
            if (mAuth.CurrentUser == null)
            {
                Debug.Log("Not signed in, unable to link credential to user.");
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetException(new Exception("Not signed in"));
                return tcs.Task;
            }
            Credential cred = EmailAuthProvider.GetCredential(email, password);
            if (signInAndFetchProfile)
            {
                var task = mAuth.CurrentUser.LinkAndRetrieveDataWithCredentialAsync(cred);
                WaitUtil.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Link Credential");
                });
                return task;
            }
            else
            {
                var task = mAuth.CurrentUser.LinkWithCredentialAsync(cred);
                WaitUtil.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Link Credential");
                });
                return task;
            }
        }

        /// <summary>
        /// Reauthenticate the user with the current email / password.
        /// </summary>
        public Task ReauthenticateAsync(bool signInAndFetchProfile, string email, string password)
        {
            var user = mAuth.CurrentUser;
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
                WaitUtil.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Reauthentication");
                });
                return task;
            }
            else
            {
                var task = user.ReauthenticateAsync(cred);
                WaitUtil.WaitTask(task, () =>
                {
                    LogTaskCompletion(task, "Reauthentication");
                });
                return task;
            }
        }

        /// <summary>
        /// Unlink the email credential from the currently logged in user.
        /// </summary>
        public Task UnlinkEmailAsync(string pEmail, string pPassword)
        {
            if (mAuth.CurrentUser == null)
            {
                Debug.Log("Not signed in, unable to unlink");
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetException(new Exception("Not signed in"));
                return tcs.Task;
            }
            var task = mAuth.CurrentUser.UnlinkAsync(EmailAuthProvider.GetCredential(pEmail, pPassword).Provider);
            WaitUtil.WaitTask(task, () => { LogTaskCompletion(task, "Unlinking"); });
            return task;
        }


        //========

        public void ReloadUser()
        {
            var task = mAuth.CurrentUser.ReloadAsync();
            WaitUtil.WaitTask(task, () =>
            {
                LogTaskCompletion(task, "Reload");
                LogUserInfo();
            });
        }

        public void SignOut()
        {
            mAuth.SignOut();
        }

        public Task DeleteUserAsync()
        {
            if (mAuth.CurrentUser != null)
            {
                var task = mAuth.CurrentUser.DeleteAsync();
                WaitUtil.WaitTask(task, () =>
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

        public void GetUserToken()
        {
            if (mAuth.CurrentUser == null)
            {
                Debug.Log("FirebaseAuth::GetUserToken Not signed in, unable to get token.");
                return;
            }
            mFetchingToken = true;
            var task = mAuth.CurrentUser.TokenAsync(false);
            WaitUtil.WaitTask(task, () =>
            {
                mFetchingToken = false;
                LogTaskCompletion(task, "User token fetch");
            });
        }

        private bool LogTaskCompletion(Task task, string operation)
        {
            if (task.IsCanceled)
                Debug.Log("RFirebaseAuth::logTaskCompletion " + operation + " canceled.");

            else if (task.IsFaulted)
            {
                Debug.Log("RFirebaseAuth::logTaskCompletion " + operation + " encounted an error.");
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    string authErrorCode = "";
                    var firebaseEx = exception as global::Firebase.FirebaseException;
                    if (firebaseEx != null)
                        authErrorCode = string.Format("AuthError.{0}: ", ((AuthError)firebaseEx.ErrorCode).ToString());

                    Debug.Log("RFirebaseAuth::logTaskCompletion " + authErrorCode + exception.ToString());
                }
            }
            else if (task.IsCompleted)
                Debug.Log("FirebaseAuth::logTaskCompletion " + operation + " completed");

            return task.IsCompleted;
        }

        private void OnTokenChanged(object sender, EventArgs e)
        {
            FirebaseAuth senderAuth = sender as FirebaseAuth;
            if (senderAuth == mAuth && senderAuth.CurrentUser != null && !mFetchingToken)
            {
                var task = senderAuth.CurrentUser.TokenAsync(false);
                WaitUtil.WaitTask(task, () =>
                {
                    Debug.Log(string.Format("[RFirebaseAuth::OnTokenChanged] Token[0:8] = {0}", task.Result.Substring(0, 8)));
                });
            }
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            FirebaseAuth senderAuth = sender as FirebaseAuth;
            FirebaseUser user = null;
            if (senderAuth != null)
                mUserByAuth.TryGetValue(senderAuth.App.Name, out user);
            if (senderAuth == mAuth && senderAuth.CurrentUser != user)
            {
                bool signedIn = user != senderAuth.CurrentUser && senderAuth.CurrentUser != null;
                if (!signedIn && user != null)
                {
                    Debug.Log("RFirebaseAuth::OnStateChanged user" + user.UserId + " sign out!");
                }
                user = senderAuth.CurrentUser;
                mUserByAuth[senderAuth.App.Name] = user;
                if (signedIn)
                {
                    Debug.Log("RFirebaseAuth::OnStateChanged user" + user.UserId + " sign in!");
                }
            }
        }
#else
        public bool initialized { get; private set; }
        public bool logged { get { return false; } }
        public void Initialize() { }
        public void LogUserInfo() { }
        public void SignOut() { }
        public Task DeleteUserAsync() { return Task.FromResult(0); }
        public Task SigninAnonymouslyAsync() { return Task.FromResult(0); }
        public Task SignInWithCustomTokenAsync(string pToken) { return Task.FromResult(0); }
        public Task SigninWithEmailAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword) { return Task.FromResult(0); }
        public Task SigninWithEmailCredentialAsync(bool pSignInAndFetchProfile, string pEmail, string pPassword) { return Task.FromResult(0); }
        public Task LinkWithEmailCredentialAsync(bool signInAndFetchProfile, string email, string password) { return Task.FromResult(0); }
        public Task ReauthenticateAsync(bool signInAndFetchProfile, string email, string password) { return Task.FromResult(0); }
        public Task UnlinkEmailAsync(string pEmail, string pPassword) { return Task.FromResult(0); }
        public void ReloadUser() { }
        public void GetUserToken() { }
#endif
    }
}
