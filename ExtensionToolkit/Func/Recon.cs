using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.IdentityModel.Tokens;

namespace ExtensionToolkit.Func
{
    internal class DomainRecon
    {
        public static void Recon(string[] args)
        {
            var reconHelpMap = new Dictionary<string, string>
            {
                { "getuserspns", "no args" },
            };
            if (args.Length == 2 && args[1].Equals("help"))
            {

                Console.WriteLine("Available operations and expected arguments:");
                foreach (var entry in reconHelpMap)
                {
                    Console.WriteLine($"- {entry.Key}: {entry.Value}");
                }

                return;
            }
            else if (args.Length < 2)
            {
                Console.WriteLine("Mandatory args: operation");
                return;
            }

            string operation = args[1];

            var reconMap = new Dictionary<string, Action>
            {
                { "getuserspns", () => GetUserSPNs(args) },
            };

            if (reconMap.TryGetValue(operation, out Action action))
            {
                Console.WriteLine($"Executing operation: {operation}");
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Unknown operation: {operation}");
            }
        }

        public static void GetUserSPNs(string[] args)
        {
            // Lista para armazenar catálogos globais
            List<string> gcList = new List<string>();

            // Obter o catálogo global do domínio atual
            Forest currentForest = Forest.GetCurrentForest();
            var globalCatalogs = currentForest.FindAllGlobalCatalogs();
            foreach (GlobalCatalog gc in globalCatalogs)
            {
                gcList.Add(currentForest.ApplicationPartitions[0].SecurityReferenceDomain.ToString());
            }

            // Verifica se encontrou catálogos globais
            if (gcList.Count == 0)
            {
                Console.WriteLine("No Global Catalogs Found!");
                return;
            }

            // Processa cada catálogo global encontrado
            foreach (var globalCatalog in gcList)
            {
                using (DirectoryEntry entry = new DirectoryEntry("LDAP://" + globalCatalog))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(entry))
                    {
                        searcher.PageSize = 1000;
                        searcher.Filter = "(&(!objectClass=computer)(servicePrincipalName=*))";
                        searcher.PropertiesToLoad.Add("serviceprincipalname");
                        searcher.PropertiesToLoad.Add("name");
                        searcher.PropertiesToLoad.Add("samaccountname");
                        searcher.PropertiesToLoad.Add("memberof");
                        searcher.PropertiesToLoad.Add("pwdlastset");

                        // Executa a pesquisa no catálogo global
                        var results = searcher.FindAll();
                        HashSet<string> userAccounts = new HashSet<string>();

                        foreach (SearchResult result in results)
                        {
                            foreach (string serviceName in result.Properties["serviceprincipalname"])
                            {
                                string userName = result.Properties["name"][0]?.ToString();
                                string accountName = result.Properties["samaccountname"][0]?.ToString();
                                string groupMembership = result.Properties["memberof"][0]?.ToString();
                                DateTime pwdSetTime = DateTime.FromFileTime((long)result.Properties["pwdlastset"][0]);

                                // Imprime o resultado
                                PrintResult(serviceName, userName, accountName, groupMembership, pwdSetTime);

                                // Opcional: iniciando a autenticação Kerberos (remova se não for necessário)
                                InitiateKerberosRequest(serviceName);
                            }
                        }
                    }
                }
            }
        }


        static void PrintResult(string serviceName, string userName, string accountName, string groupMembership, DateTime pwdSetTime)
        {
            Console.WriteLine($"SPN: {serviceName}, UserName: {userName}, AccountName: {accountName}, GroupMembership: {groupMembership}, PwdSetTime: {pwdSetTime}");
        }

        static void InitiateKerberosRequest(string servicePrincipalName)
        {
            // This will initiate the Kerberos request as in the PowerShell script
            new KerberosRequestorSecurityToken(servicePrincipalName);
        }
    }
}
