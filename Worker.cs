using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using Serilog;
using Newtonsoft.Json;
using Microsoft.Graph;

using Schlauchboot.Teams.Monitoring.Scripts;
using Schlauchboot.Teams.Monitoring.Models.Meta;

namespace Schlauchboot.Teams.Monitoring
{
    public class Worker : BackgroundService
    {
        private bool _firstRun;
        private readonly ILogger _logger;
        private DateTime _applicationTimestamp;
        private readonly IConfiguration _config;
        private Stopwatch _graphServiceClientTimeout;
        private GraphServiceClient _graphServiceClient;
        private readonly Dictionary<int, string> _logDictionary = new LogDictionary().logDictionary;

        public Worker(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _firstRun = true;

            _applicationTimestamp = DateTime.Now;

            _graphServiceClientTimeout = new Stopwatch();
            _graphServiceClientTimeout.Start();

            _graphServiceClient = new GraphServiceClientManager(_config).GenerateGraphServiceClient();

            _logger.Information(_logDictionary[0]);

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Warning(_logDictionary[8]);

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_applicationTimestamp.Day != DateTime.Now.Day || _firstRun)
                {
                    _logger.Information(_logDictionary[1]);

                    //Disable Stopwatch
                    _graphServiceClientTimeout.Stop();

                    //Refresh OAuth Token
                    if (_graphServiceClientTimeout.Elapsed >= TimeSpan.FromMinutes(55))
                    {
                        _logger.Warning(_logDictionary[2]);
                    }

                    //Initialize Manager
                    var graphTeamsManager = new GraphTeamsManager(_config);

                    //Query Groups
                    var groupsCollection = await graphTeamsManager.QueryGroups(_graphServiceClient);

                    _logger.Information(_logDictionary[3]);

                    //Filter Groups from Teams
                    var teamsCollection = graphTeamsManager.FilterTeamsFromGroups(groupsCollection);

                    _logger.Information(string.Join(' ', teamsCollection.Count, _logDictionary[4]));

                    //Set Helper Value
                    var teamsStatusCollection = new List<TeamsStatus>();

                    //Get Teams Information
                    foreach (var teams in teamsCollection)
                    {
                        //Set Helper Value
                        teamsStatusCollection.Add(new TeamsStatus()
                        {
                            id = teams.Id,
                            displayname = teams.DisplayName
                        });

                        //Get Last File Edit
                        teamsStatusCollection.Last().lastFileEdit = await graphTeamsManager.GetLastFileEdit(_graphServiceClient, teams);

                        //Get Last Message Post
                        teamsStatusCollection.Last().lastMessagePost = await graphTeamsManager.GetLastMessagePost(_graphServiceClient, teams);

                        if (teamsStatusCollection.Count % 10 == 0 || teamsStatusCollection.Count == teamsCollection.Count)
                        {
                            _logger.Information(string.Join(' ', teamsStatusCollection.Count, _logDictionary[5]));
                        }
                    }

                    _logger.Information(_logDictionary[6]);

                    //Output to File
                    using (StreamWriter outputFile = System.IO.File
                        .CreateText($"{AppDomain.CurrentDomain.BaseDirectory}\\Reports\\TeamsReport_{DateTime.Now.ToString("MM_dd_yyyy_HH_mm")}.json"))
                    {
                        new JsonSerializer().Serialize(outputFile, teamsStatusCollection);
                    }

                    _logger.Information(_logDictionary[7]);

                    //Reset Timestamp
                    _applicationTimestamp = DateTime.Now;

                    //Reset Stopwatch
                    _graphServiceClientTimeout.Start();

                    //Disable _firstRun
                    if (_firstRun)
                    {
                        _firstRun = false;
                    }
                }

                await Task.Delay(30000, cancellationToken);
            }
        }
    }
}
