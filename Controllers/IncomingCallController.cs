using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using TelephoneUpdates.API;
using TelephoneUpdates.API.Objects;
using static System.Collections.Specialized.BitVector32;

namespace TelephoneUpdates.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncomingCallController : ControllerBase
    {

        #region IdentifyContact
        [HttpGet("HandleCall")]
        public async Task<IActionResult> HandleCall(
       [FromQuery(Name = "ApiPhone")] string phoneNumber,
       [FromQuery(Name = "ApiCallId")] string callId)
        {

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return BadRequest(new { error = "Missing phone number" });
            }
            var dataRepository = new DataRepository();
            var tzTeacher = Request.Query["tzTeacher"].LastOrDefault()?.ToString();

            if (!string.IsNullOrEmpty(tzTeacher))
            {
                var session2 = await dataRepository.GetCallSession(callID: callId, phoneNumber: phoneNumber, Tzman: tzTeacher);
                if (session2.ContactIdentifiedStatus == CallSession.ContactIdentifiedStatusOptions.NotIdentifyed)
                {
                    var message3 = $"המספר שלך לא מזוהה במערכת אנא פנה לרכז הסניף";
                    var responseText3 = $"id_list_message=t-{message3}";
                    return Content(responseText3, "text/plain", Encoding.UTF8);
                }
                else
                {
                    var fullName1 = session2.Contact?.FullName ?? "";
                    //העברה לשלוחה שקוראת לפונקציית דיווח
                    var message1 = $"שלום הרב {fullName1},הינך מועבר לדיווח נוכחות";
                    var responseText1= $"id_list_message=t-{message1}.g-1";
                    return Content(responseText1, "text/plain", Encoding.UTF8);
                }

            }

            var session = await dataRepository.GetCallSession(callID: callId, phoneNumber: phoneNumber, Tzman:null);
            if (session.ContactIdentifiedStatus == CallSession.ContactIdentifiedStatusOptions.NotIdentifyed)
            {
                var comment = $"read=t-המספר שלך לא זוהה במערכת אנא הקש מספר תעודת זהות=tzTeacher,,9,8,,TeudatZehut,,,,,,,,yes";
                return Content(comment, "text/plain", Encoding.UTF8);

                // פה תיהיה פונקציה לבדיקה לפי תעודת זהות
            }
            var fullName = session.Contact?.FullName ?? "";


            //העברה לשלוחה שקוראת לפונקציית דיווח
            var message = $"שלום הרב {fullName},הינך מועבר לדיווח נוכחות";
            var responseText = $"id_list_message=t-{message}.g-1";
            return Content(responseText, "text/plain", Encoding.UTF8);
            // פונקציה לזיהוי לפי תעודת זהות + קוד אישי אולי לשאול את גלסנר

        }
        #endregion

        #region ReportAttendance

        [HttpGet("ReportAttendance")]
        public async Task<IActionResult> ReportAttendance(
            [FromQuery(Name = "ApiPhone")] string phoneNumber,
            [FromQuery(Name = "ApiCallId")] string callId)
        {
            var dataRepository = new DataRepository();
            DateTime dateInstance = DateTime.Today;
            var session = await dataRepository.GetCallSession(callID: callId, phoneNumber: phoneNumber,Tzman:null);
            var teacherId = session.Contact.Id;

            //שליפת מערך השיבוצים של החונך לפי המזהה במידה והמערך ריק מושמעת לו הודעה כי אין לו שיעורים לדיווח ביום זה
            var jArrayStudents = await dataRepository.GetPrivateLessonsInstances(teacherId, dateInstance);
            session.Students = jArrayStudents.ToObject<List<JObject>>();
 
            if (session.Students == null || session.Students.Count == 0)
            {
                var context = $"id_list_message=t-אין לך שיעורים לדיווח ליום זה. שלום ותודה!&go_to_folder=hangup";
                return Content(context, "text/plain", Encoding.UTF8);
            }

            var attendance = Request.Query["Attendance"].LastOrDefault()?.ToString();

            // אם יש תשובה - נשמור אותה ואז נתקדם
            if (!string.IsNullOrEmpty(attendance))
            {
                string comment = "";

                switch (attendance)
                {
                    case "1":
                        comment = "נרשם כי הישעור התקיים בזמן";
                        break;
                    case "2":
                        comment = "נרשם כי השיעור התקיים באיחור";
                        break;
                    case "3":
                        comment = "נרשם כי השיעור לא התקיים";
                        break;
                    case "4":
                        comment = "נרשם כי התלמיד לא הגיע";
                        break;
                    default:
                        return Content("id_list_message=t-בחירה לא תקינה, נא נסה שוב");
                }

                // כאן עדכון במסד נתונים :
                // dataRepository.UpdateAttendanceInDatabase(...);

                // מקדמים את האינדקס
                session.CurrentIndex++;
            }

            while (session.CurrentIndex < session.Students.Count)
            {
                var currentStudent = session.Students[session.CurrentIndex];
                bool isGroupLesson = false;

                var token = currentStudent["isAGroupLesson"];
                if (token?.Type == JTokenType.Boolean)
                    isGroupLesson = token.Value<bool>();
                /// במידה וזה שיעור קבוצתי ממשיכים לתלמיד הבא ומעדכנים בסשן כי קיים לחונך היום שיעור קבוצתי
                if (isGroupLesson)
                {
                 
                    session.HasGroupLesson = true;
                    session.CurrentIndex++;
                    continue;
                }

                // הגעת לתלמיד פרטני
                var studentName = currentStudent["studentName"];
                var message = $"האם התקיים היום שיעור עם {studentName}? השיעור התקיים הקש 1, לא התקיים הקש 2, התקיים באיחור הקש 3";
                var responseText = $"read=t-{message}=Attendance,,1,1,,No,,,,1.2.3,,,,yes";

                dataRepository.SaveCallSession(callId, session);
                return Content(responseText, "text/plain", Encoding.UTF8);
            }

            // אם סיימת את כל התלמידים
            if (session.HasGroupLesson)
            {
                // בסוף שאלה על הקבוצתי
                var message = $"?האם התקיים היום השיעור הקבוצתי";
                var responseText = $"read=t-{message}=Attendance,,1,1,,No,,,,1.2.3,,,,yes";

                session.HasGroupLesson = false; 
                dataRepository.SaveCallSession(callId, session);

                return Content(responseText, "text/plain", Encoding.UTF8);
            }

            return Content($"id_list_message=t-תודה רבה! סיימת את כל הדיווחים &go_to_folder=hangup", "text/plain", Encoding.UTF8);
        }
        #endregion

    }
}
