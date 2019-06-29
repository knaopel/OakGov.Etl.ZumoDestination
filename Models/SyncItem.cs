using System;

namespace OakGov.Etl
{
    public class SyncItem
    {
        public string id { get; set; }
        public string syncType { get; set; }
        public int updates { get; set; }
        public int adds { get; set; }
        public int deletes { get; set; }
        public DateTime syncTime { get; set; }
    }
}
