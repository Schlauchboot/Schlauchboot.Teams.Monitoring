using System;

namespace Schlauchboot.Teams.Monitoring.Models.Meta
{
    class TeamsStatus
    {
        public string id { get; set; }
        public string displayname { get; set; }
        public DateTime lastFileEdit { get; set; }
        public DateTime lastMessagePost { get; set; }
    }
}
