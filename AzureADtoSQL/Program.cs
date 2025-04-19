using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace AzureADtoSQL
{
    class Program
    {
        private static string clientId = "<Client ID>";
        private static string clientSecret = "<Client Secret>";
        private static string tenantId = "<Tenant ID>";
        private static string sqlConnectionString = "<SQL Connection String>";

        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {
            var authContext = new AuthenticationContext("https://login.windows.net/" + tenantId);
            var clientCredential = new ClientCredential(clientId, clientSecret);
            var result = await authContext.AcquireTokenAsync("https://graph.windows.net", clientCredential);

            var activeDirectoryClient = new ActiveDirectoryClient(
                new Uri("https://graph.windows.net/" + tenantId + "/"),
                async () => await Task.FromResult(result.AccessToken));

            var users = await activeDirectoryClient.Users
                .Where(u => u.UserPrincipalName.StartsWith("user"))
                .ExecuteAsync();

            using (var sqlConnection = new SqlConnection(sqlConnectionString))
            {
                sqlConnection.Open();

                foreach (var user in users.CurrentPage)
                {
                    var command = new SqlCommand("INSERT INTO [dbo].[Users] ([UserPrincipalName], [DisplayName], [GivenName], [Surname]) VALUES (@UserPrincipalName, @DisplayName, @GivenName, @Surname)", sqlConnection);
                    command.Parameters.AddWithValue("@UserPrincipalName", user.UserPrincipalName);
                    command.Parameters.AddWithValue("@DisplayName", user.DisplayName);
                    command.Parameters.AddWithValue("@GivenName", user.GivenName);
                    command.Parameters.AddWithValue("@Surname", user.Surname);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

