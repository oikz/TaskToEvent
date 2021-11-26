# TaskToEvent
TaskToEvent takes a specified Microsoft To Do Task list and creates a Microsoft Calendar Event from each task to provide better visualisation of tasks across a given period of time.

Created to have an easy to run, self-managed solution to not being able to visualise To Do Tasks in a Calendar easily


# Usage
- Supply configuration parameters in config.txt (located in the User directory, tasktoevent folder)
  - e.g., `C:/Users/<username>/tasktoevent/config.txt` on Windows
- Execute the program and first-time login with Microsoft
    - Credentials are cached for subsequent executions
- The program will run with the specified parameters, create Calendar Tasks and then exit
  
# Configuration
An example configuration file is located in this repository, and can be used to create a config.txt file
The config.txt file can contain the following parameters:
- `List`: The name of the task list to pull tasks from
- `Calendar`: The Name of the calendar to create events in
- `LookBackPages`: The number of pages to look back in to find tasks at 10 tasks per page