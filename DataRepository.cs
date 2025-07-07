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
using TelephoneUpdates.API.Objects;

namespace TelephoneUpdates.API
{

    public class DataRepository
    {

        const string Cache_CallIDToContactPrefix = "call_id_to_contact_";

        #region AddContactToCall
        /// <summary>
        /// שומר מזהה שיחה לצד איש קשר
        /// כך שבמהלך השיחה אנחנו יודעים תמיד עם מי אנחנו מדברים
        /// הזיהוי הראשוני מתבצע עם תחילת השיחה
        /// באמצעות מספר הטלפון הנכנס או תעודת זהות שהמתקשר מקיש
        /// </summary>
        /// <param name="callID">מזהה השיחה</param>
        /// <param name="contact">אובייקט של איש קשר</param>
        public void AddContactToCall(string callID, JObject contact)
        {
            System.Runtime.Caching.MemoryCache.Default.Set($"{Cache_CallIDToContactPrefix}{callID}",
                contact, DateTime.Now.AddHours(2));
        }
        #endregion

        #region SaveCallSession
        public void SaveCallSession(string callID, CallSession session)
        {
            System.Runtime.Caching.MemoryCache.Default.Set(
                $"{Cache_CallIDToContactPrefix}{callID}",
                session,
                DateTimeOffset.Now.AddHours(2)
            );
        }
        #endregion

        #region GetContactByCall
        public JObject GetContactByCall(string callID)
        {
            var result = (JObject)System.Runtime.Caching.MemoryCache.Default.Get($"{Cache_CallIDToContactPrefix}{callID}");
            return result;
        }
        public async Task<CallSession> GetCallSession(string callID, string phoneNumber)
        {
            var session = (CallSession)System.Runtime.Caching.MemoryCache.Default
                .Get($"{Cache_CallIDToContactPrefix}{callID}");
            if (session == null)
            {
                session = await CreateNewCallSession(callID:callID,phoneNumber:phoneNumber);
            }
            return session;
        }
        #endregion

        #region CreateNewCallSession
        private async Task<CallSession> CreateNewCallSession(string callID, string phoneNumber)
        {
            var session = new CallSession
            {
                CallID = callID,
                ContactPhoneNumber = phoneNumber
            };
            var contact = await this.GetContactByPhoneNumber(phoneNumber);
                session.Contact = contact;
            return session;

        }
        #endregion

        #region GetViewStructure
        //פונקציה להחזרת מבנה תצוגה-ViewStructure  
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

        #endregion

        #region GettingData


        /// <summary>
        /// מחזיר איש קשר - חונך בדרך כלל
        /// על פי מספר טלפון
        /// במידה ולא נמצא איש קשר יוחזר 
        /// null
        /// </summary>
        /// <param name="phoneNumber">מספר הטלפון על פיו מחפשים את איש הקשר</param>
        /// <returns></returns>
        /// <remarks>
        /// כרגע אנחנו מגבילים בכוונה את הסינון לפי קטגוריה חונכים בלבד!
        /// ייתכן שבהמשך נידרש להענות גם לאנשים שאינם חונכים ונצטרך לבצע ריפקטורינג לפונקציה הזו
        /// </remarks>
        public async Task<Contact> GetContactByPhoneNumber(string phoneNumber)
        {
            var viewStructureJson = await GetViewStructure(contextKey: "GlobalOptimum_Contacts_All");
            // עריכת הג'ייסון כך שיכיל סינון מתאים
            var viewStructure = JObject.Parse(viewStructureJson);


            viewStructure["pullSpecificColumns"] = new JArray() { "id", "fullName", "firstName", "lastName" };
            viewStructure["clientID"] = "b60e84fd-024b-4679-8eb7-d3a05c1345f9";
            // נוסיף סינון של מספר טלפון
            var filterByPhone = new FilterUnit(key: "phone", value: phoneNumber);
            var filterByCategory = new FilterUnit(key: "extensionColumn_Contacts_ExtensionColumnsParameterIDCategory_KeyWord", value: "Contacts_ExtensionColumnsCategoryTutor");
            var filterToken = new JArray();
            DefaultContractResolver contractResolver = new()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            filterToken.Add(JObject.FromObject(filterByPhone, new Newtonsoft.Json.JsonSerializer { ContractResolver = contractResolver }));
            filterToken.Add(JObject.FromObject(filterByCategory, new Newtonsoft.Json.JsonSerializer { ContractResolver = contractResolver }));
            viewStructure.Add("userSelectedListBuilderCommands", filterToken);
            var serverResult = await GetViaPostByViewStructureAsJson(viewStructure.ToString());
            var parsedServerResut = JObject.Parse(serverResult);
            var data = JArray.Parse(parsedServerResut["data"].ToString());
            // אובייקט קונטקט מפורסר
            if (data.Count == 1)
            {
                var contact = data.First().ToObject<Contact>();
                return contact;
            }

            return null;
        }

        /// <summary>
        /// מחזיר איש קשר - חונך בדרך כלל
        /// על פי מספר טלפון
        /// במידה ולא נמצא איש קשר יוחזר 
        /// null
        /// </summary>
        /// <param name="phoneNumber">מספר הטלפון על פיו מחפשים את איש הקשר</param>
        /// <returns></returns>
        /// <remarks>
        /// כרגע אנחנו מגבילים בכוונה את הסינון לפי קטגוריה חונכים בלבד!
        /// ייתכן שבהמשך נידרש להענות גם לאנשים שאינם חונכים ונצטרך לבצע ריפקטורינג לפונקציה הזו
        /// </remarks>
        public async Task<dynamic> GetPrivateLessonsInstances(int teacherID, DateTime dateInstance)
        {
            var viewStructureJson = await GetViewStructure(contextKey: "GlobalOptimumSchoolPrivateLessons_PrivateLessonsInstances");
            // עריכת הג'ייסון כך שיכיל סינון מתאים
            var viewStructure = JObject.Parse(viewStructureJson);


            viewStructure["pullSpecificColumns"] = new JArray() { "*" };
            viewStructure["clientID"] = "b60e84fd-024b-4679-8eb7-d3a05c1345f9";
            // נוסיף סינון של מספר טלפון
            var filterByteacherID = new FilterUnit(key: "teacherID", value: teacherID);
            var filterBydateInstance = new FilterUnit(key: "dateInstance", value: dateInstance);
            var filterToken = new JArray();
            DefaultContractResolver contractResolver = new()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            filterToken.Add(JObject.FromObject(filterByteacherID, new Newtonsoft.Json.JsonSerializer { ContractResolver = contractResolver }));
            filterToken.Add(JObject.FromObject(filterBydateInstance, new Newtonsoft.Json.JsonSerializer { ContractResolver = contractResolver }));
            viewStructure.Add("userSelectedListBuilderCommands", filterToken);
            var serverResult = await GetViaPostByViewStructureAsJson(viewStructure.ToString());
            var parsedServerResut = JObject.Parse(serverResult);
            var data = JArray.Parse(parsedServerResut["data"]?.ToString());
            
            return data;
         ;
        }



        #endregion

        #region Internal Helpers


        static string baseUrlForKinyanApi = @"https://tests.matarah.com/api/";

        private static Dictionary<string, dynamic> ViewStructureCache = new();



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

        private async Task SetHttp(HttpClient http)
        {
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
