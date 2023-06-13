using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

namespace NearClientUnity.Utilities
{
    public static class Web
    {
        public static async Task<T> FetchJsonAsync<T>(string url, string json = "")
        {
            try
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                if (!string.IsNullOrEmpty(json))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                    request.method = "POST";
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    //request.downloadHandler = new DownloadHandlerBuffer();
                }

                var asyncOperation = request.SendWebRequest();
                while (!asyncOperation.isDone)
                {
                    await Task.Yield();
                }

                if (!request.isNetworkError || !request.isHttpError)
                {
                    string jsonString = request.downloadHandler.text;
                    var rawResult = JObject.Parse(jsonString);

                    if (rawResult["error"] != null && rawResult["error"]["data"] != null)
                    {
                        var error = rawResult["error"];
                        var errorData = error["data"];
                        throw new Exception($"[{error["code"]}]: {errorData}");
                    }
                                                            
                    return rawResult["result"].ToObject<T>();
                }
                else
                {
                    throw new Exception(request.error);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            
        }

        public static async Task<T> FetchJsonAsync<T>(ConnectionInfo connection, string json = "")
        {
            var url = connection.Url;
            var result = await FetchJsonAsync<T>(url, json);
            return result;
        }
    }
}