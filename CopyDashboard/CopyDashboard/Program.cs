using System;
using System.Linq;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Dashboards.WebApi;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 7)
            {
                Console.WriteLine("Usage: copydashboard {orgUrl} {personalAccessToken} {projectName} {sourceTeamName} {sourceDashboardName} {targetTeamName} {targetDashboardName}");
                return;
            }
            Uri orgUrl = new Uri(args[0]);         // Organization URL, for example: https://dev.azure.com/fabrikam               
            string personalAccessToken = args[1];  // See https://docs.microsoft.com/azure/devops/integrate/get-started/authentication/pats
            string projectName = args[2];          // Project Name
            string sourceTeamName = args[3];       // Source Team Name
            string sourceDashboardName = args[4];  // Source Dashboard to copy
            string targetTeamName = args[5];       // Target Team Name
            string targetDashboardName = args[6];  // Target Dashboard name

            // Create a connection
            using VssConnection connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, personalAccessToken));
            try
            {
                // Make sure we can connect before running the copy
                connection.ConnectAsync().SyncResult();
            }
            catch (VssServiceResponseException)
            {
                Console.Error.WriteLine("ERROR: Could not connect to {0}", orgUrl);
                return;
            }

            // Get WebApi client
            using DashboardHttpClient dashboardClient = connection.GetClient<DashboardHttpClient>();

            // Set source team context
            TeamContext sourceTeamContext = new TeamContext(projectName, sourceTeamName);

            // Get dashboard entries
            DashboardGroup dashboards;
            try
            {
                dashboards = dashboardClient.GetDashboardsAsync(sourceTeamContext).SyncResult();
            }
            catch (VssUnauthorizedException)
            {
                Console.Error.WriteLine("ERROR: Not authorized. Invalid token or invalid scope");
                return;
            }
            catch (ProjectDoesNotExistWithNameException)
            {
                Console.Error.WriteLine("ERROR: Project not found: {0}", projectName);
                return;
            }
            catch (TeamNotFoundException)
            {
                Console.Error.WriteLine("ERROR: Team not found: {0}", sourceTeamName);
                return;
            }
            // Get dashboard by name
            Dashboard sourceDashboardEntry = dashboards.DashboardEntries.Single(d => d.Name == sourceDashboardName);
            if (sourceDashboardEntry == null)
            {
                Console.Error.WriteLine("ERROR: Dashboard not found: {0}", sourceDashboardName);
                return;
            }
            Dashboard sourceDashboard = dashboardClient.GetDashboardAsync(sourceTeamContext, (Guid)sourceDashboardEntry.Id).SyncResult();

            // get target team
            WebApiTeam targetTeam;
            using TeamHttpClient teamClient = connection.GetClient<TeamHttpClient>();
            try
            {
                targetTeam = teamClient.GetTeamAsync(projectName, targetTeamName).SyncResult();
            }
            catch (TeamNotFoundException)
            {
                Console.Error.WriteLine("ERROR: Team not found: {0}", targetTeamName);
                return;
            }

            // replace source team id with target team id  
            // for widgets like Burndown or Backlog where team id is a parameter
            foreach (Widget w in sourceDashboard.Widgets)
            {
                if (w.Settings != null)
                { 
                    w.Settings = w.Settings.Replace(sourceDashboard.OwnerId.ToString(), targetTeam.Id.ToString());
                }
            }

            // Set target team context
            TeamContext targetTeamContext = new TeamContext(projectName, targetTeamName);

            // Create target dashboard
            Dashboard targetObj = new Dashboard
            {
                Name = targetDashboardName,
                Description = sourceDashboard.Description,
                Widgets = sourceDashboard.Widgets
            };
            try
            {
                dashboardClient.CreateDashboardAsync(targetObj, targetTeamContext).SyncResult();
            }
            catch (DuplicateDashboardNameException)
            {
                Console.Error.WriteLine("ERROR: Dashboard {0} already exists in {1} team", targetDashboardName, targetTeamName);
                return;
            }
        }
    }
}