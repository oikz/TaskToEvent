using System;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace TaskToEvent {
    public class TaskToEvent {
        private static readonly string appID = "855929bc-bbb8-475b-84ff-7a93c0b91019"; //Spooky appID
        private static readonly string[] scopes = {"User.Read", "Tasks.Read", "Calendars.ReadWrite"};
        
        public static async Task Main(string[] args) {
            // Initialize the auth provider with values from fields above
            var authProvider = new DeviceCodeAuthProvider(appID, scopes);
            
            var graphClient = new GraphServiceClient(authProvider);

            //Create new folder for storing data if not already created
            System.IO.Directory.CreateDirectory(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\tasktoevent\\");

            await GetTasks(graphClient);
        }

        private static async Task GetTasks(GraphServiceClient graphClient) {
            //Get all tasks
            var user = await graphClient.Me.Request().GetAsync();
            Console.WriteLine(user.DisplayName);
            
            var calendars = await graphClient.Me.Calendars.Request().GetAsync();
            foreach (var calendar in calendars) {
                Console.WriteLine(calendar.Name);
            }
        }
    }
}