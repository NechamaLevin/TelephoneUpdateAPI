using Newtonsoft.Json.Linq;

namespace TelephoneUpdates.API.Objects
{
    /// <summary>
    /// כל שיחת טלפון יוצרת אובייקט כזה שנשמר בקאש
    /// אובייקט זה מכיל את איש הקשר איתו אנחנו משוחחים כעת
    /// את רשימת התלמידים (??) 
    /// את רשימת השיבוצים לעדכון נוכחות
    /// </summary>
    public class CallSession
    {
        public enum ContactIdentifiedStatusOptions
        {
            NotIdentifyed,
            Identifyed
        }
        public string CallID { get; set; }
        public string ContactPhoneNumber { get; set; }
        public ContactIdentifiedStatusOptions ContactIdentifiedStatus { get; set; }

        private Contact _contact;
        public Contact Contact
        {
            get { return _contact; }
            set
            {
                _contact = value;
                if (value is null)
                {
                    this.ContactIdentifiedStatus = ContactIdentifiedStatusOptions.NotIdentifyed;
                }
                else
                {
                    this.ContactIdentifiedStatus = ContactIdentifiedStatusOptions.Identifyed;
                }
            }
        }
        public List<JObject> Students { get; set; }

        /// <summary>
        /// כאשר אנחנו במצב של הזנת נוכחות אזי בהתאם להקשות של הטלפון
        /// אנחנו מעדכנים נוכחות של תלמידים ועוברים לשיבוץ הבא בכל פעם בהתאם להקשה
        /// כאן אנחנו מחזיקים את האינדקס הנוכחי שבו החונך נמצא
        /// יש לשקול את הארכיטקטורה ולקחת בחשבון תרחישים שבהם השיחה מתנתקת באמצע עדכון
        /// ייתכן שיידרשו גם ביטולי עדכונים 
        /// ואולי יש לבקש בסופו של דבר שמירה סופית של הנתונים וכל עוד לא נשמרו החונך יכול לחזור בו
        /// אולי לומר לשמיר הקש 1 וכדומה...
        /// </summary>
        public int CurrentIndex { get; set; } = 0;
        /// <summary>
        /// ברגע שבמערך השיבוצים של החונך מזוהה כי יש לו תלמיד עם שיעור קבוצתי
        /// true  הופך להיות  HasGroupLesson  ערך
        /// והחונך אינו נשאל על תלמיד זה 
        /// כשמגיע לאינדקס האחרון בודק האם הערך שווה לtrue 
        /// אם כן הוא שואל אותו האם קיימת את השיעור הקבוצתי היום?   
        /// </summary>
        public bool HasGroupLesson { get; set; }= false;
    }
}
