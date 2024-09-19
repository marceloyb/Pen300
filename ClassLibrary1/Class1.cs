using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

public class TimedEventLogService : ServiceBase
{
    private Timer timer;
    private const string EventLogSource = "TimedEventLogServiceSource";
    private const string EventLogName = "Application";
    private const int TimerInterval = 180000; // 3 minutes in milliseconds

    public TimedEventLogService()
    {
        this.ServiceName = "TimedEventLogService";
    }

    protected override void OnStart(string[] args)
    {
        // Configure and start the timer
        timer = new Timer();
        timer.Interval = TimerInterval; // Set interval to 3 minutes
        timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
        timer.AutoReset = true; // Make sure the timer keeps running
        timer.Enabled = true; // Start the timer

        // Create event log source if it does not exist
        if (!EventLog.SourceExists(EventLogSource))
        {
            EventLog.CreateEventSource(EventLogSource, EventLogName);
        }
        EventLog.WriteEntry(EventLogSource, "Service started.");
    }

    protected override void OnStop()
    {
        // Stop the timer
        timer.Enabled = false;
        EventLog.WriteEntry(EventLogSource, "Service stopped.");
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Write an entry to the event log
        EventLog.WriteEntry(EventLogSource, "Service is running - Timer ticked.", EventLogEntryType.Information);
    }

    public static void Main()
    {
        ServiceBase.Run(new TimedEventLogService());
    }
}
