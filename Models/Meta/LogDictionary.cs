using System.Collections.Generic;

namespace Schlauchboot.Teams.Monitoring.Models.Meta
{
    class LogDictionary
    {
        public Dictionary<int, string> logDictionary = new Dictionary<int, string>()
        {
            { 0, "The Service has been started!" }, //Service has been started
            { 1, "Scheduled Check has been initialized!" }, //A new Day has been registered or this is the first run
            { 2, "The Graph-Service-Client needs to be refreshed!" }, //Graph Service Client needs to be refreshed
            { 3, "All Groups have been queried!" }, //All Groups have been queried
            { 4, "Teams have been filtered from Groups!" }, //X Ammount of Teams have been found
            { 5, "Teams have been evaluated!" }, //X Ammount of Teams have been evaluated
            { 6, "Evaluation has been finished!" }, //Evaluation has been finished
            { 7, "Output has been written to File!" }, //Output has been written to File
            { 8, "The Service has been requested to stop!" } //The Service has been stopped
        };
    }
}
