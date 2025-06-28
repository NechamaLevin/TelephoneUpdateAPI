using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Data;
using System.Text;
using System.Runtime.Caching;
using System.Text.Json;

namespace TelephoneUpdates.API
{

    public class FilterUnit
    {
        public string Key { get; set; }
        public dynamic Value { get; set; }
        public string CommandType { get; set; } = "RestrictList";
        public string OperatorType { get; set; } = "Equal";
        public FilterUnit(string key, dynamic value)
        {
            Key = key;
            Value = value;
        }
    }

    public class DataRepository
    {

        const string Cache_CallIDToContactPrefix = "call_id_to_contact_";
        /// <summary>
        /// שומר מזהה שיחה לצד איש קשר
        /// כך שבמהלך השיחה אנחנו יודעים תמיד עם מי אנחנו מדברים
        /// הזיהוי הראשוני מתבצע עם תחילת השיחה
        /// באמצעות מספר הטלפון הנכנס או תעודת זהות שהמתקשר מקיש
        /// </summary>
        /// <param name="callID">מזהה השיחה</param>
        /// <param name="contact">אובייקט של איש קשר</param>
        public void AddContactToCall(string callID,JObject contact) {
            System.Runtime.Caching.MemoryCache.Default.Set($"{Cache_CallIDToContactPrefix}{callID}",
                contact,DateTime.Now.AddHours(2));
        }


        public JObject GetContactByCall(string callID) {
            var result = (JObject)System.Runtime.Caching.MemoryCache.Default.Get($"{Cache_CallIDToContactPrefix}{callID}");
            return result;
        }


        public async Task<dynamic> GetContactByPhoneNumber(string phoneNumber)
        {
            var viewStructureJson = await GetViewStructure(contextKey:"GlobalOptimum_Contacts_All");
            // עריכת הג'ייסון כך שיכיל סינון מתאים
            var viewStructure = JObject.Parse(viewStructureJson);

            
            viewStructure["pullSpecificColumns"] = new JArray() { "id", "fullName","firstName","lastName" };
            viewStructure["clientID"] = "b60e84fd-024b-4679-8eb7-d3a05c1345f9";
            // נוסיף סינון של מספר טלפון
            var filterByPhone = new FilterUnit(key: "phone", value: phoneNumber);
            var filterToken = new JArray();
            DefaultContractResolver contractResolver = new()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            filterToken.Add(JObject.FromObject(filterByPhone, new Newtonsoft.Json.JsonSerializer { ContractResolver = contractResolver }));
            viewStructure.Add("userSelectedListBuilderCommands", filterToken);
            var totalResult = await GetViaPostByViewStructureAsJson(viewStructure.ToString());
            return totalResult;
        }


        #region Internal Helpers


        static string baseUrlForKinyanApi = @"https://tests.matarah.com/api/";

        private static Dictionary<string, dynamic> ViewStructureCache = new();

        private async Task<string> GetViewStructure(string contextKey)
        {

            if (ViewStructureCache.ContainsKey(contextKey))
            {
                return ViewStructureCache[contextKey];
            }

            var http = new HttpClient();
            await SetHttp(http);

            var url = $@"ViewStructure/{contextKey}";
            var json = await http.GetStringAsync(url);
            ViewStructureCache.TryAdd(contextKey, json);
            
            return json;
        }
     
        private async Task<string> GetViaPostByViewStructureAsJson(string viewStructure)
        {
            var http = new HttpClient();
            await SetHttp(http);
            var url = @"Repository/GetViaPostByViewStructure";
            var jObj = JObject.Parse(viewStructure);
            jObj["columns"] = null;
            var stringContent = new StringContent(jObj.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await http.PostAsync(url, stringContent);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        private async Task SetHttp(HttpClient http) {
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            var token = await GetTokenForApiRequests();
            http.DefaultRequestHeaders.Add("Authorization", token);
            http.DefaultRequestHeaders.Add("Client-id", "b60e84fd-024b-4679-8eb7-d3a05c1345f9");
            http.BaseAddress = new Uri(baseUrlForKinyanApi);

        }

        private async Task<string> GetTokenForApiRequests()
        {
            return @"Bearer eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJFbWFpbCI6Im4wNTQ4NTAxODAyQGdtYWlsLmNvbSIsIlVzZXJOYW1lIjoibjA1NDg1MDE4MDJAZ21haWwuY29tIiwiVXNlclB1YmxpY05hbWUiOiLXoNeX157XmSDXnNeV15nXnyIsIm5iZiI6MTc1MDE0MjA0NCwiZXhwIjoxNzUwMTc4MDQ0LCJpYXQiOjE3NTAxNDIwNDR9.0xI4ODcSl6RsvHFea72j412iKmX2dpQGzU_PCcfKTdnPQFEpONQrqUtNGDLjuJ_694Nka2szSE_NCRExJhUzQA"
    ;
        }

        #endregion


    }
}
