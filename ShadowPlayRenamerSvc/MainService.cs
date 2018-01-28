// Copyright (c) 2018 Ádám Juhász
// This file is part of ShadowPlayRenamerSvc.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using JAL.ShadowPlayRenamer.Service.Extension;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace JAL.ShadowPlayRenamer.Service
{
    public partial class MainService : StatusReportingServiceBase
    {
        private enum EventID : int
        {
            ServiceInitializing = 0,
            ServiceStarted,
            ServiceStopped,
            ServicePaused,
            ServiceResumed,
            ServiceFaulted,
            ServiceConfigurationError,
            RenameStarted = 0x10,
            RenameFinished,
            RenameSuspended,
            RenameResumed,
            RenameCancelled,
        }

        private readonly string monitorPath;
        private readonly int    retryDelay;

        private ServiceStatus           serviceStatus;
        private FileSystemWatcher       watcher;
        private Renamer                 renamer;
        private CancellationTokenSource tokenSource;

        private HashSet<Task<string>> renamerTasks;
        private Task                  taskMaid;

        private EventLog        eventLog;
        private BinaryFormatter formatter   = new BinaryFormatter();
        private TraceSource     traceSource = new TraceSource("ShadowPlayRenamerSvc",
#if DEBUG
            SourceLevels.All
#else
            SourceLevels.Critical | SourceLevels.ActivityTracing
#endif
            );

        public override EventLog EventLog => eventLog;

        public MainService()
        {
            traceSource.TraceEvent(TraceEventType.Information, (int)EventID.ServiceInitializing, "Initializing");

            monitorPath = ConfigurationManager.AppSettings["MonitorPath"];
            if (!int.TryParse(ConfigurationManager.AppSettings["RetryDelay"], out retryDelay))
            {
                retryDelay = -1;
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Preparing for event logging.");

            eventLog = new EventLog();

            EventLog.Source = "SPRSVC";
            EventLog.Log    = "Application";

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Initializing components.");

            InitializeComponent();

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Initializing task set.");

            renamerTasks = new HashSet<Task<string>>();

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Initializing model.");

            renamer = new Renamer(new RenamerAppConfig());
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            traceSource.TraceData(e.IsTerminating ? TraceEventType.Critical : TraceEventType.Error,
                (int)EventID.ServiceFaulted, e.IsTerminating, e.ExceptionObject);
        }

        protected override void OnStart(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            traceSource.TraceInformation("Service starting.");

            if (!Directory.Exists(monitorPath))
            {
                traceSource.TraceEvent( TraceEventType.Error, (int)EventID.ServiceConfigurationError,
                    "The configured path cannot be found. Check if the application configuration is configured correctly.", monitorPath);
                EventLog.WriteEntry($"The configured path {monitorPath} cannot be found.",
                    EventLogEntryType.Error, (int)EventID.ServiceConfigurationError);
                Stop();
                return;
            }

            if (retryDelay == -1)
            {
                traceSource.TraceEvent(TraceEventType.Error, (int)EventID.ServiceConfigurationError,
                    "The configured timeout for retrying cannot be parsed. Check if the application configuration is configured correctly.", ConfigurationManager.AppSettings["RetryDelay"]);
                EventLog.WriteEntry($"The configured timeout for retrying ({ConfigurationManager.AppSettings["RetryDelay"]}) cannot be parsed.",
                    EventLogEntryType.Error, (int)EventID.ServiceConfigurationError);
                Stop();
                return;
            }

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Setting service status to start-pending.");

            serviceStatus = new ServiceStatus()
            {
                dwCurrentState = ServiceState.ServiceStartPending,
                dwWaitHint = 100_000,
            };

            SetServiceStatus(ref serviceStatus);

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Checking for stuck tasks.");

            if (renamerTasks.Any())
            {
                EventLog.WriteEntry("There are running tasks.", EventLogEntryType.Error, (int)EventID.ServiceFaulted);
                Stop();
            }

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Preparing cancellation token source.");

            if (tokenSource != null)
            {
                tokenSource.Dispose();
            }

            tokenSource = new CancellationTokenSource();

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Preparing task cleaning task.");

            taskMaid = Task.Run(() => ClearTaskSet(tokenSource.Token));

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceInitializing, "Configuring system watcher.");

            // TODO: Configuration variables:
            watcher = new FileSystemWatcher(monitorPath, "*.mp4");
            watcher.BeginInit();
            watcher.EnableRaisingEvents   = false;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter          = NotifyFilters.FileName;
            watcher.Created += Watcher_Created;
            watcher.Error   += Watcher_Error;
            watcher.EndInit();
            watcher.EnableRaisingEvents   = true;

            traceSource.TraceEvent(TraceEventType.Start, (int)EventID.ServiceStarted, "Service started.");
            EventLog.WriteEntry("Service started.", EventLogEntryType.Information, (int)EventID.ServiceStarted);

            ProcessExistingFiles();

            serviceStatus.dwCurrentState = ServiceState.ServiceRunning;

            SetServiceStatus(ref serviceStatus);
        }

        private void ProcessExistingFiles()
        {
            traceSource.TraceEvent(TraceEventType.Resume, (int)EventID.ServiceStarted, "Process files that have not yet been processed.");

            foreach (string filePath in Directory.GetFiles(watcher.Path, watcher.Filter, SearchOption.AllDirectories))
            {
                renamerTasks.Add(RenameFile(filePath, tokenSource.Token));
            }
        }

        protected override void OnStop() => StopProcessing();

        protected override void OnPause() => StopProcessing();

        protected override void OnContinue()
        {
            traceSource.TraceEvent(TraceEventType.Resume, (int)EventID.ServiceResumed, "Resume operation.");
            EventLog.WriteEntry("Resume operation.", EventLogEntryType.Information);

            tokenSource.Dispose();
            tokenSource = new CancellationTokenSource();
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            byte[] rawData;

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, e.GetException());
                rawData = stream.ToArray();
            }

            EventLog.WriteEntry("File system watcher failed.", EventLogEntryType.Error, (int)EventID.ServiceFaulted, 0, rawData);
            traceSource.TraceData(TraceEventType.Error, (int)EventID.ServiceFaulted, e.GetException());

            ExitCode = 1;
            Stop();
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceFaulted, "New file created. Trying to rename it.");
            renamerTasks.Add(RenameFile(e.FullPath, tokenSource.Token).ContinueWith(ClearTask));
        }

        private void StopProcessing()
        {
            traceSource.TraceInformation("Service stopping.");

            if (watcher != null)
            {
                traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceStopped, "Disabling file system watcher.");

                watcher.EnableRaisingEvents = false;
            }

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceStopped, "Canceling running tasks.");

            tokenSource?.Cancel();

            if ((!taskMaid?.IsCompleted ?? true) || (renamerTasks?.Any(task => !task.IsCompleted) ?? false))
            {
                traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceStopped,
                    "TaskMaid Completed: {0}, Task Count: {1}", taskMaid?.IsCompleted ?? (object)"N/A", renamerTasks?.Count ?? (object)"N/A");
                foreach (Task task in renamerTasks ?? (IEnumerable<Task<string>>)new Task<string>[0])
                {
                    traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceStopped,
                        "  Task Completed: {0}", taskMaid.IsCompleted, renamerTasks.Count);
                }
                traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceStopped, "Waiting for tasks to finish for about a minute.");

                RequestAdditionalTime(60_100);

                IEnumerable<Task> taskCollection = renamerTasks;

                if (taskMaid != null)
                {
                    taskCollection = taskCollection.Concat(new[] { taskMaid });
                }

                if (!Task.WaitAll(taskCollection.ToArray(), 60_000))
                {
                    renamerTasks.RemoveWhere(task => task.IsCompleted);

                    traceSource.TraceEvent(TraceEventType.Error, (int)EventID.ServiceStopped, "Failed to stop tasks.");
                    EventLog.WriteEntry("Failed to stop tasks.", EventLogEntryType.Error, (int)EventID.ServiceStopped);

                    ExitCode = 1;
                    Task.WhenAll(renamerTasks.ToArray()).ContinueWith(t => renamerTasks.RemoveWhere(task => task.IsCompleted));
                }
            }
            else
            {
                traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.ServiceStopped, "No tasks are running.");

                renamerTasks?.Clear();
            }

            traceSource.TraceEvent(TraceEventType.Stop, (int)EventID.ServiceStopped, "Service stopped.");
            EventLog.WriteEntry("Service stopped.", EventLogEntryType.Information, (int)EventID.ServiceStopped);
        }

        private string ClearTask(Task<string> completedTask)
        {
            renamerTasks.Remove(completedTask);

            return completedTask.IsCompleted && !completedTask.IsFaulted && !completedTask.IsCanceled ? completedTask.Result : null;
        }

        private async Task<string> RenameFile(string filePath, CancellationToken cancellationToken)
        {
            string result = null;

            traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.RenameStarted, "Waiting before renaming file: {0}", filePath);

            // Make sure we are not grabbing away the resource from ShadowPlay.
            await Task.Delay(500);

            traceSource.TraceEvent(TraceEventType.Start,
                (int)EventID.RenameStarted, "Renaming file: {0}", filePath);

            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    traceSource.TraceEvent(TraceEventType.Stop,
                        (int)EventID.RenameCancelled, "Cancellation requested. Terminating renaming attempts of {0}.", filePath);
                    return null;
                }

                traceSource.TraceEvent(TraceEventType.Verbose, (int)EventID.RenameStarted, "Renaming file. ({0})", filePath);

                try
                {
                    result = renamer.RenameFile(filePath);
                }
                catch (FileNotFoundException)
                {
                    traceSource.TraceEvent(TraceEventType.Stop,
                        (int)EventID.RenameFinished, "The file doesn't exist: {0}", filePath, result);

                    return null;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    byte[] rawData;

                    using (MemoryStream stream = new MemoryStream())
                    {
                        formatter.Serialize(stream, ex);
                        rawData = stream.ToArray();
                    }

                    traceSource.TraceEvent(TraceEventType.Error,
                        (int)EventID.ServiceFaulted, "Exception occurred during renaming of {1} file.{0}{2}", Environment.NewLine,
                        filePath, ex);
                    traceSource.TraceEvent(TraceEventType.Suspend,
                        (int)EventID.RenameSuspended, "Failed to rename {0}. Waiting {1:N}s", filePath, retryDelay / 1000d);

                    await Task.Delay(retryDelay, cancellationToken);

                    traceSource.TraceEvent(TraceEventType.Resume,
                        (int)EventID.RenameResumed, "Renaming file: {0}", filePath);
                }
                catch (Exception ex)
                {
                    byte[] rawData;

                    using (MemoryStream stream = new MemoryStream())
                    {
                        formatter.Serialize(stream, ex);
                        rawData = stream.ToArray();
                    }

                    traceSource.TraceData(TraceEventType.Critical, (int)EventID.ServiceFaulted, ex);

                    throw;
                }
            } while (result == null);

            traceSource.TraceEvent(TraceEventType.Stop,
                (int)EventID.RenameFinished, "File rename was successful: {0} => {1}", filePath, result);
            EventLog.WriteEntry($"File rename was successful: {filePath} => {result}",
                EventLogEntryType.Information, (int)EventID.RenameFinished);

            return result;
        }

        private async Task ClearTaskSet(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                renamerTasks.RemoveWhere(task => task.IsCompleted);

                try
                {
                    await Task.Delay(60_000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }
}
