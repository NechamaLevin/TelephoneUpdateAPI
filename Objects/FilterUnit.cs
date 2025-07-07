namespace TelephoneUpdates.API.Objects
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
}
