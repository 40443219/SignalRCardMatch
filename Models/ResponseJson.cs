using System.Collections.Generic;

namespace SignalRCardMatch.Models
{
    public class ResponseJson
    {
        public string Event { get; set; }
        public string Target { get; set; }
        public Dictionary<string, object> Options { get; set; }
    }
}