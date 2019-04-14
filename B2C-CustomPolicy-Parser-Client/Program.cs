using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AADB2C.CustomPolicy.Parser;

namespace AADB2C.CustomPolicy.Parser.Client
{
    public class Program
    {


        static void Main(string[] args)
        {

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                Console.WriteLine("pass an integer (default)0, 1 or 2 ");
                Console.WriteLine("New Tenant Scenario : 0");
                Console.WriteLine("Existing Tenant Scenario : 1");
                Console.WriteLine("Existing Tenant of 1P Scenario : 2");
                int choice = args != null && args.Length > 0 ? int.Parse(args[0]) : 0;

                B2CClientParser client = new B2CClientParser();
                switch (choice)
                {
                    case 0:
                        client.NewTenant();
                        break;
                    case 1:
                        client.ExistingTenant();
                        break;
                    case 2:
                        client.ExistingTenantWith1P();
                        break;
                    case 3:
                        client.TestAuth();
                        break;
                }

            }
            catch (Exception e)
            {

                Debug.WriteLine("\nError {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : "");
            }

        }






    }
}
