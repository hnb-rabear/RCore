/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RCore.Common
{
    [Serializable]
    public class KeyValue
    {
        public string key;
        public string value;

        public KeyValue(string pKey, string pValue)
        {
            key = pKey;
            value = pValue;
        }
    }

    public class WebRequest
    {
        #region Internal Class

        [Serializable]
        public class RequestResult
        {
            public string text;
            public string errorMessage;
            public bool error;
            /// <summary>
            /// 1: network error
            /// 2: decription error
            /// 3: json parse error
            /// 4: fatal error
            /// </summary>
            public BreakPoint breakPoint;
        }

        public enum BreakPoint
        {
            None = 0,
            NetworkError = 1,
            FaltalError = 2,
            DecriptionError = 3,
            JsonParseError = 4,
        }

        #endregion

        //==============================================================

        #region Members

        private IEncryption mEncryption;

        #endregion

        //==============================================================

        #region Public

        public WebRequest(IEncryption pDataEncryption)
        {
            mEncryption = pDataEncryption;
        }

        public void Post(string pUrl, Action<RequestResult> pOnResponsed, bool pEncryptOut = false)
        {
            Post(pUrl, null, pOnResponsed, false, pEncryptOut);
        }

        public void Post(string pUrl, List<KeyValue> pKeyValueList, Action<RequestResult> pOnResponsed, bool pEncryptIn = true, bool pEncryptOut = false)
        {
            string url = $"{pUrl}/{(pEncryptIn ? 1 : 0)}/{(pEncryptOut ? 1 : 0)}";

            var form = new WWWForm();

            if (pKeyValueList != null && pKeyValueList.Count > 0)
            {
                for (int i = 0; i < pKeyValueList.Count; i++)
                {
                    if (pEncryptIn)
                    {
                        pKeyValueList[i].key = mEncryption.Encrypt(pKeyValueList[i].key);
                        pKeyValueList[i].value = mEncryption.Encrypt(pKeyValueList[i].value);
                    }
                    form.AddField(pKeyValueList[i].key, pKeyValueList[i].value);
                }
            }

            var request = UnityWebRequest.Post(url, form);
            request.SendWebRequest();
            WaitUtil.Start(() => request.isDone, () =>
            {
                HandlePost(request, pOnResponsed, pEncryptOut);
            });
        }

        private void HandlePost(UnityWebRequest pRequest, Action<RequestResult> pOnResponsed, bool pEncryptOut)
        {
            var res = new RequestResult();

            try
            {
                if (pRequest.isNetworkError)
                {
                    res.breakPoint = BreakPoint.NetworkError;
                    res.error = true;
                    res.errorMessage = $"ERROR: {pRequest.responseCode}\n{pRequest.error}";
                    res.text = pRequest.downloadHandler.text.Replace("<meta charset=\"utf-8\">", "").Trim();
#if UNITY_EDITOR
                    Debug.LogError(res.errorMessage);
#endif
                }
                else
                {
                    res.text = pRequest.downloadHandler.text.Replace("<meta charset=\"utf-8\">", "").Trim();

                    if (pRequest.responseCode == 200)
                    {
                        if (res.text.Contains("Fatal error"))
                        {
                            res.breakPoint = BreakPoint.FaltalError;
                            res.error = true;
                            res.errorMessage = res.text;
                        }
                        else
                        {
                            if (pEncryptOut)
                            {
                                res.breakPoint = BreakPoint.DecriptionError;
                                res.text = mEncryption.Decrypt(res.text);
                            }

                            res.breakPoint = BreakPoint.JsonParseError;
                            var jsonParse = JSON.Parse(res.text);
                            if (jsonParse != null)
                            {
                                res.error = jsonParse.GetString("result") == "error";
                                res.errorMessage = jsonParse.GetString("message") + jsonParse[0].GetString("message");
                            }

                            res.breakPoint = BreakPoint.None;
                        }
                    }
                    else
                    {
                        res.breakPoint = BreakPoint.NetworkError;
                        res.error = true;
                        res.errorMessage = $"ERROR: {pRequest.responseCode}\n{pRequest.error}";
#if UNITY_EDITOR
                        Debug.LogError(res.errorMessage);
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                res.error = true;
                res.errorMessage = ex.ToString();
#if UNITY_EDITOR
                Debug.LogError(pRequest.url + "\n" + ex);
#endif
            }

            pOnResponsed?.Invoke(res);
        }

        #endregion

        //===============================================================
    }
}
