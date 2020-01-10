﻿using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NearClientUnity.Utilities
{
    public static class Web
    {
        public static async Task<dynamic> FetchJsonAsync(string url, string json = "")
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response;

                if (!string.IsNullOrEmpty(json))
                {
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    Console.WriteLine(url + " -> " + json);
                    response = client.PostAsync(url, content).Result;
                }
                else
                {
                    Console.WriteLine(url);
                    response = await client.GetAsync(url);
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(">>> " + jsonString);
                    dynamic result = JObject.Parse(jsonString);
                    return result;
                }
                else
                {                    
                    throw new HttpException((int) response.StatusCode, response.Content.ToString());
                }
            }
        }

        public static async Task<string> FetchJsonAsync(ConnectionInfo connection, string json = "")
        {
            var url = connection.Url;
            var result = await FetchJsonAsync(url, json);
            return result;
        }
    }
}