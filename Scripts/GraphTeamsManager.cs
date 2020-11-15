extern alias GraphBeta;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Microsoft.Graph;
using Beta = GraphBeta.Microsoft.Graph;

namespace Schlauchboot.Teams.Monitoring.Scripts
{
    class GraphTeamsManager
    {
        private readonly IConfiguration _config;

        public GraphTeamsManager(IConfiguration config)
        {
            _config = config;
        }

        private List<Group> AddGroupToCollection(List<Group> groupsCollection, IGraphServiceGroupsCollectionPage groupsCollectionPage)
        {
            foreach (var group in groupsCollectionPage)
            {
                groupsCollection.Add(group);
            }
            return groupsCollection;
        }

        public async Task<List<Group>> QueryGroups(GraphServiceClient graphServiceClient)
        {
            var groupsCollection = new List<Group>();
            var groupsCollectionPage = await graphServiceClient
                .Groups
                .Request()
                .Select("displayName,id,resourceProvisioningOptions")
                .GetAsync();
            groupsCollection = AddGroupToCollection(groupsCollection, groupsCollectionPage);
            while (groupsCollectionPage.AdditionalData.ContainsKey("@odata.nextLink") &&
                groupsCollectionPage.AdditionalData.TryGetValue("@odata.nextLink", out _))
            {
                groupsCollectionPage = await groupsCollectionPage.NextPageRequest.GetAsync();
                AddGroupToCollection(groupsCollection, groupsCollectionPage);
            }
            return groupsCollection;
        }

        public List<Group> FilterTeamsFromGroups(List<Group> groupsCollection)
        {
            List<Group> teamsCollection = new List<Group>();
            foreach (var group in groupsCollection)
            {
                var resourceProvisioningOptions = group.AdditionalData["resourceProvisioningOptions"].ToString();
                if (resourceProvisioningOptions.Contains("Team"))
                {
                    teamsCollection.Add(group);
                }
            }
            return teamsCollection;
        }

        public async Task<List<Channel>> GetTeamsChannels(GraphServiceClient graphServiceClient, Group teams)
        {
            var teamsChannelCollectionPage = await graphServiceClient
                .Teams[teams.Id]
                .Channels
                .Request()
                .GetAsync();
            var teamsChannelCollection = new List<Channel>();
            foreach (var teamsChannel in teamsChannelCollectionPage)
            {
                teamsChannelCollection.Add(teamsChannel);
            }
            return teamsChannelCollection;
        }

        public async Task<List> GetSharepointSiteList(GraphServiceClient graphServiceClient, Group teams)
        {
            var sharepointSiteListCollection = new List<List>();
            var sharepointSiteListCollectionPage = await graphServiceClient
                .Groups[teams.Id]
                .Sites["root"]
                .Lists
                .Request()
                .GetAsync();
            foreach (var sharepointSiteList in sharepointSiteListCollectionPage)
            {
                sharepointSiteListCollection.Add(sharepointSiteList);
            }
            return sharepointSiteListCollection.First();
        }

        public async Task<List<ListItem>> GetSharepointSiteListItems(GraphServiceClient graphServiceClient, Group teams, List sharepointSiteList)
        {
            var sharepointSiteListItemCollection = new List<ListItem>();
            var sharepointSiteListCollectionItemPage = await graphServiceClient
                .Groups[teams.Id]
                .Sites["root"]
                .Lists[sharepointSiteList.Id]
                .Items
                .Request()
                .Expand("fields")
                .GetAsync();
            foreach (var sharepointSiteListItem in sharepointSiteListCollectionItemPage)
            {
                sharepointSiteListItemCollection.Add(sharepointSiteListItem);
            }
            return sharepointSiteListItemCollection;
        }

        public async Task<List<Beta.ChatMessage>> GetTeamsChannelMessages(Beta.GraphServiceClient graphServiceClient, string teamsId, string teamsChannelId)
        {
            var teamsChannelMessageCollection = new List<Beta.ChatMessage>();
            try
            {
                var teamsChannelMessageCollectionPage = await graphServiceClient
                    .Teams[teamsId]
                    .Channels[teamsChannelId]
                    .Messages
                    .Request() //Top does not work reliably!
                    .GetAsync();
                foreach (var teamsChannelMessage in teamsChannelMessageCollectionPage)
                {
                    teamsChannelMessageCollection.Add(teamsChannelMessage);
                }
            }
            catch (Exception)
            {
                //Not implemented
            }
            return teamsChannelMessageCollection;
        }

        public async Task<DateTime> GetLastFileEdit(GraphServiceClient graphServiceClient, Group teams)
        {
            var teamsSharepointSiteList = await GetSharepointSiteList(graphServiceClient, teams);
            var teamsSharepointSiteListItems = await GetSharepointSiteListItems(graphServiceClient, teams, teamsSharepointSiteList);
            var lastFileEdit = DateTime.MinValue;
            if (teamsSharepointSiteListItems.Count != 0)
            {
                lastFileEdit = GetNewestDate(teamsSharepointSiteListItems);
            }
            return lastFileEdit;
        }

        public async Task<DateTime> GetLastMessagePost(GraphServiceClient graphServiceClient, Group teams)
        {
            var betaGraphServiceClient = new GraphServiceClientManager(_config).GenerateBetaGraphServiceClient();
            var teamsChannelCollection = await GetTeamsChannels(graphServiceClient, teams);
            var lastMessagePost = DateTime.MinValue;
            foreach (var teamsChannel in teamsChannelCollection)
            {
                var teamsChannelMessageCollection = await GetTeamsChannelMessages(
                    betaGraphServiceClient, teams.Id, teamsChannel.Id);
                if (teamsChannelMessageCollection.Count != 0)
                {
                    lastMessagePost = GetNewestDate(teamsChannelMessageCollection);
                }
            }
            return lastMessagePost;
        }

        private DateTime GetNewestDate(List<Beta.ChatMessage> chatMessageCollection)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                        chatMessageCollection.OrderByDescending(x => x.LastModifiedDateTime).First().LastModifiedDateTime.Value.DateTime,
                        TimeZoneInfo.Local);
        }

        private DateTime GetNewestDate(List<ListItem> sharepointItemsCollection)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                    sharepointItemsCollection.OrderByDescending(x => x.LastModifiedDateTime).First().LastModifiedDateTime.Value.DateTime,
                    TimeZoneInfo.Local);
        }
    }
}
