using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace TaskToEvent {
    public class TaskToEvent {
        private const string AppId = "855929bc-bbb8-475b-84ff-7a93c0b91019"; //Spooky appID
        private static readonly string[] Scopes = { "User.Read", "Tasks.Read", "Calendars.ReadWrite" };
        private static string _listName = "";
        private static int _lookBackPages = -1;
        private static string _calendarName = "";

        /// <summary>
        /// Initialise the application and authenticate the user
        /// </summary>
        /// <param name="args">Command line arguments (Unused)</param>
        public static async Task Main(string[] args) {
            // Initialize the auth provider with values from fields above
            var authProvider = new DeviceCodeAuthProvider(AppId, Scopes);

            var graphClient = new GraphServiceClient(authProvider);

            //Create new folder for storing data if not already created
            System.IO.Directory.CreateDirectory(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\tasktoevent\\");

            await LoadConfig();
            var tasks = await GetTasks(graphClient);
            var calendar = await FindCalendar(graphClient);
            var events = await GetEvents(graphClient, calendar);

            await CreateEvents(graphClient, tasks, calendar, events);

            Console.WriteLine("Done!");
        }

        private static async Task LoadConfig() {
            var configPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                             "\\tasktoevent\\config.txt";

            //Load Config
            if (System.IO.File.Exists(configPath)) {
                await LoadConfigFile(configPath);
            } else {
                LoadDefaults();
            }
        }

        /// <summary>
        /// Load in the entire config file and read the values
        /// </summary>
        /// <param name="configPath">The file path of the configuration file</param>
        private static async Task LoadConfigFile(string configPath) {
            var lines = await System.IO.File.ReadAllLinesAsync(configPath);
            foreach (var line in lines) {
                switch (line) {
                    case { } when line.Contains("List="):
                        _listName = line.Split('=')[1];
                        break;
                    case { } when line.Contains("Calendar="):
                        _calendarName = line.Split('=')[1];
                        break;
                    case { } when line.Contains("LookBackPages="):
                        try {
                            _lookBackPages = Convert.ToInt32(line.Split('=')[1]);
                        } catch (FormatException) {
                            Console.WriteLine("LookBackPages must be an integer");
                        }

                        break;
                }
            }

            //If any of the values were not initialised correctly, display error and exit
            if (!_listName.Any() || !_calendarName.Any() || _lookBackPages == -1) {
                Console.WriteLine("Config file is missing values");
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Load a set of default values to be used with no provided configuration file
        /// </summary>
        private static void LoadDefaults() {
            //Load default config
            _listName = "Tasks";
            _calendarName = "Calendar";
            _lookBackPages = 50;
        }


        /// <summary>
        /// Get all of the tasks in the specified list
        /// </summary>
        /// <param name="graphClient">The GraphClient to send requests to</param>
        private static async Task<List<TodoTask>> GetTasks(GraphServiceClient graphClient) {
            // Find the user specified list
            var list = await FindList(graphClient);
            var tasks = new List<TodoTask>();

            // Add all tasks to the list
            var todoTasks = await graphClient.Me.Todo.Lists[list.Id].Tasks.Request().GetAsync();
            tasks.AddRange(todoTasks.Where(todoTask =>
                todoTask.IsReminderOn == true && todoTask.CompletedDateTime == null &&
                todoTask.ReminderDateTime != null));

            // Look back a set number of pages for more tasks
            for (var i = 0; i < _lookBackPages; i++) {
                if (todoTasks.NextPageRequest != null)
                    todoTasks = await todoTasks.NextPageRequest.GetAsync();

                tasks.AddRange(todoTasks.Where(todoTask =>
                    todoTask.IsReminderOn == true && todoTask.CompletedDateTime == null &&
                    todoTask.ReminderDateTime != null));
            }

            return tasks;
        }

        /// <summary>
        /// Find the Task List specified by the user
        /// </summary>
        /// <param name="graphClient">The GraphClient to send requests to</param>
        /// <returns>The Task List</returns>
        private static async Task<TodoTaskList> FindList(GraphServiceClient graphClient) {
            var taskLists = await graphClient.Me.Todo.Lists.Request().GetAsync();
            foreach (var taskList in taskLists) {
                if (taskList.DisplayName == _listName) {
                    return taskList;
                }
            }

            //Retry looking for the task list up to 5 times
            for (var i = 0; i < 5; i++) {
                if (taskLists.NextPageRequest != null)
                    taskLists = await taskLists.NextPageRequest.GetAsync();

                foreach (var taskList in taskLists) {
                    if (taskList.DisplayName == _listName) {
                        return taskList;
                    }
                }
            }

            Console.WriteLine("Could not find list");
            Environment.Exit(0);

            return new TodoTaskList();
        }

        /// <summary>
        /// Find the Calendar specified by the user
        /// </summary>
        /// <param name="graphClient">The GraphClient to send requests to</param>
        /// <returns>The Calendar</returns>
        private static async Task<Calendar> FindCalendar(GraphServiceClient graphClient) {
            var calendars = await graphClient.Me.Calendars.Request().GetAsync();

            foreach (var calendar in calendars) {
                if (calendar.Name == _calendarName) {
                    return calendar;
                }
            }

            //Retry looking for the calendar up to 5 times
            for (var i = 0; i < 5; i++) {
                if (calendars.NextPageRequest != null)
                    calendars = await calendars.NextPageRequest.GetAsync();

                foreach (var calendar in calendars) {
                    if (calendar.Name == _calendarName) {
                        return calendar;
                    }
                }
            }

            Console.WriteLine("Could not find Calendar");
            Environment.Exit(0);

            return new Calendar();
        }

        /// <summary>
        /// Get all of the events in the specified calendar
        /// </summary>
        /// <param name="graphClient">The GraphClient to send requests to</param>
        /// <param name="calendar">The Calendar to find events in</param>
        /// <returns>A list of events in the specified calendar</returns>
        private static async Task<List<Event>> GetEvents(GraphServiceClient graphClient, Calendar calendar) {
            var response = await graphClient.Me.Calendars[calendar.Id].Events.Request().GetAsync();
            var events = response.ToList();

            //Get more calendar events
            for (var i = 0; i < _lookBackPages; i++) {
                if (response.NextPageRequest != null)
                    response = await response.NextPageRequest.GetAsync();

                events.AddRange(response);
            }

            return events;
        }

        /// <summary>
        /// Create the Events for each task in the specified calendar
        /// </summary>
        /// <param name="graphClient">The GraphClient to send requests to</param>
        /// <param name="tasks">The list of tasks that need to have Events created for</param>
        /// <param name="calendar">The Calendar that the events should be created in</param>
        /// <param name="events">The list of Events that already exist to be used for duplicate checks</param>
        private static async Task CreateEvents(GraphServiceClient graphClient, List<TodoTask> tasks, Calendar calendar,
            List<Event> events) {
            foreach (var newEvent in tasks.Select(task => new Event {
                Subject = task.Title,
                Body = new ItemBody {
                    Content = "Microsoft To Do Reminder"
                },
                Start = task.ReminderDateTime,
                End = task.ReminderDateTime,
                IsReminderOn = false
            })) {
                var result = events.FirstOrDefault(e =>
                    e.Subject.Equals(newEvent.Subject) && e.Body.Content.Contains(newEvent.Body.Content));

                if (result != null) {
                    //Check if it has a different timestamp, overwrite with this one?
                    if (result.Start.DateTime.Equals(newEvent.Start.DateTime)) {
                        //Same time zone, same time, no need to update
                        continue;
                    }

                    // Else, replace that one with this one
                    await graphClient.Me.Events[result.Id].Request().UpdateAsync(newEvent);
                    continue;
                }

                await graphClient.Me.Calendars[calendar.Id].Events
                    .Request()
                    .AddAsync(newEvent);
            }
        }
    }
}