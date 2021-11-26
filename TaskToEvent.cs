﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace TaskToEvent {
    public class TaskToEvent {
        private const string AppId = "855929bc-bbb8-475b-84ff-7a93c0b91019"; //Spooky appID
        private static readonly string[] Scopes = { "User.Read", "Tasks.Read", "Calendars.ReadWrite" };
        private const string ListName = "Tasks";
        private const int LookBackPages = 50;
        private const string CalendarName = "University";

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

            var user = await graphClient.Me.Request().GetAsync();
            Console.WriteLine(user.DisplayName);

            var tasks = await GetTasks(graphClient);
            var calendar = await FindCalendar(graphClient);

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
                todoTask.IsReminderOn != null && todoTask.CompletedDateTime == null));

            // Look back a set number of pages for more tasks
            for (var i = 0; i < LookBackPages; i++) {
                if (todoTasks.NextPageRequest != null)
                    todoTasks = await todoTasks.NextPageRequest.GetAsync();

                tasks.AddRange(todoTasks.Where(todoTask =>
                    todoTask.IsReminderOn != null && todoTask.CompletedDateTime == null));
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
                if (taskList.DisplayName == ListName) {
                    return taskList;
                }
            }

            //Retry looking for the task list up to 5 times
            for (var i = 0; i < 5; i++) {
                if (taskLists.NextPageRequest != null)
                    taskLists = await taskLists.NextPageRequest.GetAsync();

                foreach (var taskList in taskLists) {
                    if (taskList.DisplayName == ListName) {
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
                if (calendar.Name == CalendarName) {
                    return calendar;
                }
            }

            //Retry looking for the task list up to 5 times
            for (var i = 0; i < 5; i++) {
                if (calendars.NextPageRequest != null)
                    calendars = await calendars.NextPageRequest.GetAsync();

                foreach (var calendar in calendars) {
                    if (calendar.Name == CalendarName) {
                        return calendar;
                    }
                }
            }

            Console.WriteLine("Could not find Calendar");
            Environment.Exit(0);

            return new Calendar();
        }
    }
}