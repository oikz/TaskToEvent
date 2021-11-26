# TaskToEvent
TaskToEvent takes a specified Microsoft To Do Task list and creates a Microsoft Calendar Event from each task to provide better visualisation of tasks across a given period of time.

Created to have an easy to run, self-managed solution to not being able to visualise To Do Tasks in a Calendar easily


# Usage
- Supply configuration parameters in config.txt (in the same folder as the executable)
- Execute the program and first-time login with Microsoft
    - Credentials are cached for subsequent executions
- The program will run with the specified parameters, creating Calendar Tasks and then exit
  
# Configuration
The config.txt file can contain the following parameters:
- `ListName`: The name of the task list to pull tasks from
- `Calendar`: The Name of the calendar to create events in
- `LookBackPages`: The number of pages to look back in to find tasks at 10 tasks per page