## How to run

1. Create a personal access token ([steps](https://docs.microsoft.com/vsts/organizations/accounts/use-personal-access-tokens-to-authenticate?view=vsts))
2. Install [.NET SDK (2.0 or later)](https://microsoft.com/net/core)
3. Build `dotnet build`
4. Run `copydashboard {orgUrl} {personalAccessToken} {projectName} {sourceTeamName} {sourceDashboardName} {targetTeamName} {targetDashboardName}`
5. Example: ` https://dev.azure.com/contoso abcdefghijklmnopqrstuvwxyz01234567890abcdefghijklmno  "Project1" "Team1" Overview "Team2" Overview2`
6. **Warning**: if you try to copy to a dashboard that already exists, you will get an error.
