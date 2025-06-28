using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;

namespace TelephoneUpdates.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncomingCallController : ControllerBase
    {

        [HttpGet(Name = "Test")]
        public async Task<IActionResult> Test()
        {

            var dataRepository = new DataRepository();
            var result = await dataRepository.GetContactByPhoneNumber("0543201060");
            return Ok(result);
        }

        [HttpGet("HandleCall")]
        public async Task<IActionResult> HandleCall(
        [FromQuery] string ApiPhone,
        [FromQuery(Name = "ApiCallId")] string callId)
        {
            var attendance = Request.Query["Attendance"].ToString();

            // בדיקה אם הגיע מספר טלפון
            if (string.IsNullOrWhiteSpace(ApiPhone))
            {
                return BadRequest(new { error = "Missing phone number" });
            }

            var dataRepository = new DataRepository();

            // שליפת פרטי החונך לפי מספר הטלפון
            var result = await dataRepository.GetContactByPhoneNumber(ApiPhone);
            var parseResult = JObject.Parse(result);

            // בדיקת זיהוי המספר
            if (parseResult["data"] == null)
            {
                return Content("id_list_message=t-המספר שלך לא מזוהה במערכת, נא לפנות למשרד.");
            }

            // שמירת זיהוי המשתמש ב-Cache
            dataRepository.AddContactToCall(callId, parseResult);

            var fullName = parseResult["data"]?[0]?["id"]?.ToString();
            var studentName = "משה כהן";

            // ============================
            // שלב 1: אם זו קריאה שניה ויש Attendance → מתייחסים לתשובה
            // ============================
            if (!string.IsNullOrEmpty(attendance))
            {
                switch (attendance)
                {
                    case "1":
                        // עדכון במסד נתונים: השיעור התקיים
                        return Content("id_list_message=t-תודה רבה, נרשם שהשיעור התקיים.");
                    case "2":
                        return Content("id_list_message=t-נרשם שהשיעור לא התקיים.");
                    case "3":
                        return Content("id_list_message=t-נרשם שהשיעור התקיים באיחור.");
                    default:
                        return Content("id_list_message=t-בחירה לא תקינה, נא נסה שוב.");
                }
            }
     

                // ============================
                // שלב 2: אם זו קריאה ראשונה → מחזירים לימות שאלה עם read
                // ============================
                var message = $"שלום הרב {fullName}, האם התקיים היום שיעור עם {studentName}? השיעור התקיים הקש 1, השיעור לא התקיים הקש 2, השיעור התקיים באיחור הקש 3";
                var responseText = $"read=t-{message}=Attendance,,1,1,,No,,,,1.2.3,,,,yes";


            return Content(responseText, "text/plain", Encoding.UTF8);
        }
    }
}
             