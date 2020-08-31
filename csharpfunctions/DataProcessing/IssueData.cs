using System;
using System.Collections.Generic;
using System.Text;

namespace csharpfunctions
{
    public class IssueData
    {
        public string id { get; set; }
        public string Source { get; set; }
        public string EntryType { get; set; }
        public DateTime Timestamp { get; set; }
        public int TotalOpenIssues { get; set; }
        public int MissingTags { get; set; }
        public Dictionary<string, int> CountByPriority { get; set; }
        public Dictionary<string, int> CountByService { get; set; }
    }
}
