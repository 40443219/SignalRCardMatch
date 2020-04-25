using System;
using System.Collections.Generic;

namespace SignalRCardMatch.Models
{
    public class Card
    {
        public int value { get; set; }
        public Dictionary<String, Object> detail { get; set; }
    }
}