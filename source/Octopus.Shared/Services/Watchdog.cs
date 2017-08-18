using System;
using System.Linq;
using System.Reflection;
using Microsoft.Win32.TaskScheduler;
using Octopus.Diagnostics;
using Octopus.Shared.Configuration;

namespace Octopus.Shared.Services
{
    public class Watchdog : IWatchdog
    {
        readonly ILog log;
        readonly string taskName;
        readonly string argsPrefix = "checkservices --instances ";

        public Watchdog(ApplicationName applicationName, ILog log)
        {
            taskName = "Octopus Watchdog " + applicationName;
            this.log = log;
        }

        public WatchdogConfiguration GetConfiguration()
        {
            var enabled = false;
            var interval = 0;
            var instances = "*";

#if WINDOWS_SERVICE
            using (var taskService = new TaskService())
            {
                var taskDefinition = taskService.FindAllTasks(t => t.Name == taskName).SingleOrDefault()?.Definition;

                if (taskDefinition != null)
                {
                    enabled = true;
                    var trigger = taskDefinition.Triggers.FirstOrDefault(x => x is TimeTrigger);
                    if (trigger?.Repetition != null)
                    {
                        interval = (int)trigger.Repetition.Interval.TotalMinutes;
                    }
                    var action = taskDefinition.Actions.FirstOrDefault(x => x is ExecAction);
                    if (action != null)
                    {
                        instances = ((ExecAction) action).Arguments.Replace(argsPrefix, "");
                    }
                }
            }
#endif
            return new WatchdogConfiguration(enabled, interval, instances);
        }

        public void Delete()
        {
#if WINDOWS_SERVICE
            using (var taskService = new TaskService())
            {
                if (taskService.FindAllTasks(t => t.Name == taskName).SingleOrDefault() == null)
                {
                    log.Info($"Scheduled task {taskName} not found. Nothing to do.");
                }
                else
                {
                    taskService.RootFolder.DeleteTask(taskName);
                    log.Info($"Deleted scheduled task {taskName}");
                }
            }
#else
            throw NotSupportedException("The watchdog is not supported on this platform");
#endif
        }

        public void Create(string instanceNames, int interval)
        {
#if WINDOWS_SERVICE
            using (var taskService = new TaskService())
            {
                var taskDefinition = taskService.FindAllTasks(t => t.Name == taskName).SingleOrDefault()? .Definition;
                if (taskDefinition == null)
                {
                    taskDefinition = taskService.NewTask();

                    taskDefinition.Principal.UserId = "SYSTEM";
                    taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;
                    log.Info($"Creating scheduled task {taskName}");
                }
                else
                {
                    log.Info($"Updating scheduled task {taskName}");
                }

                
                taskDefinition.Actions.Clear();
                taskDefinition.Actions.Add(new ExecAction(Assembly.GetEntryAssembly().Location, argsPrefix + instanceNames));

                taskDefinition.Triggers.Clear();
                taskDefinition.Triggers.Add(new TimeTrigger
                {
                    Repetition = new RepetitionPattern(TimeSpan.FromMinutes(interval), TimeSpan.Zero)
                });

                var task = taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);
                task.Enabled = true;
            }
#else
            throw NotSupportedException("The watchdog is not supported on this platform");
#endif
        }
    }
}
