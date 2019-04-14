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
        
        static IEFParser iEFParser;
        static void Main(string[] args)
        {
            // validate parameters
            if (!CheckValidParameters(args))
                return;

            HttpRequestMessage request = null;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                // Login as global admin of the Azure AD B2C tenant
                string token = AuthenticationHelper.LoginAsAdmin();
                iEFParser = new IEFParser(Constants.Tenant, token);
                if (!iEFParser.SetUpParserForTenant())
                {
                    throw new InvalidProgramException("setup parser failed");
                }
                TrustFrameworkPolicy tfp = null;
                List<ClaimsProvider> claimsProviders = new List<ClaimsProvider>();
                List<UserJourney> userJourneys = new List<UserJourney>();

                while (args[0].ToUpper() != "EXIT")
                {


                    // Graph client does not yet support trustFrameworkPolicy, so using HttpClient to make rest calls
                    switch (args[0].ToUpper())
                    {
                        case "LIST":
                            // List all polcies using "GET /trustFrameworkPolicies"
                            var policies = iEFParser.PolicyIdsInTenant;
                            break;
                        case "GET":
                            // Get a specific policy using "GET /trustFrameworkPolicies/{id}"
                            var policy = iEFParser.GetPolicy(args[1]);
                            break;
                        case "CREATERP":
                            // Create a policy using "POST /trustFrameworkPolicies" with XML in the body
                            
                            var ret = iEFParser.CreatePolicy(tfp);
                            break;
                        case "UPDATE":
                            
                            iEFParser.UpdatePolicy(tfp);
                            break;
                        case "DELETE":
                            // Delete using "DELETE /trustFrameworkPolicies/{id}"
                            iEFParser.DeletePolicy(tfp);
                            break;
                        case "PARSE":
                            // Parse using "Get /trustFrameworkPolicies/{id} and then split the xml"
                            claimsProviders = iEFParser.GetClaimsProviders();
                            userJourneys = iEFParser.GetUserJourneys();

                            break;
                        case "PARSE-IDP":
                            claimsProviders = iEFParser.GetClaimsProviders();
                            break;
                        case "PARSE-USERFLOWS":
                            userJourneys = iEFParser.GetUserJourneys();
                            break;

                        default:
                            return;
                    }


                    
                }
            }
            catch (Exception e)
            {
                PrintRequest(request);
                Debug.WriteLine("\nError {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : "");
            }

        }


        public static bool CheckValidParameters(string[] args)
        {
            if (Constants.ClientIdForUserAuthn.Equals("ENTER_YOUR_CLIENT_ID") ||
                Constants.Tenant.Equals("ENTER_YOUR_TENANT_NAME"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Debug.WriteLine("1. Open 'Constants.cs'");
                Debug.WriteLine("2. Update 'ClientIdForUserAuthn'");
                Debug.WriteLine("3. Update 'Tenant'");
                Debug.WriteLine("");
                Debug.WriteLine("See README.md for detailed instructions.");
                Debug.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;
                Debug.WriteLine("[press any key to exit]");
                Console.ReadKey();
                return false;
            }

            if (args.Length <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Debug.WriteLine("Please enter a command as the first argument.");
                Console.ForegroundColor = ConsoleColor.White;
                PrintHelp(args);
                return false;
            }

            switch (args[0].ToUpper())
            {
                case "LIST":
                case "PARSE":
                case "PARSE-IDP":
                case "PARSE-USERFLOWS":
                    break;
                case "GET":
                    if (args.Length <= 1)
                    {
                        PrintHelp(args);
                        return false;
                    }
                    break;
                case "CREATERP":
                    if (args.Length <= 1)
                    {
                        PrintHelp(args);
                        return false;
                    }
                    break;
                case "UPDATE":
                    if (args.Length <= 2)
                    {
                        PrintHelp(args);
                        return false;
                    }
                    break;
                case "DELETE":
                    if (args.Length <= 1)
                    {
                        PrintHelp(args);
                        return false;
                    }
                    break;
                case "HELP":
                    PrintHelp(args);
                    return false;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Debug.WriteLine("Invalid command.");
                    Console.ForegroundColor = ConsoleColor.White;
                    PrintHelp(args);
                    return false;
            }
            return true;
        }

        public static void PrintRequest(HttpRequestMessage request)
        {
            if (request != null)
            {
                Debug.Write(request.Method + " ");
                Debug.WriteLine(request.RequestUri);
                Debug.WriteLine(request.RequestUri);
                Debug.WriteLine("");
            }
            else
            {
                Debug.WriteLine("null request");
            }
        }

        private static void PrintHelp(string[] args)
        {
            string appName = "B2C-Parse-TrustFrameworkPolicy";
            Console.ForegroundColor = ConsoleColor.White;
            Debug.WriteLine("- Square brackets indicate optional arguments");
            Debug.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Debug.WriteLine("Exit                                 : {0} To exit", appName);
            Debug.WriteLine("List                                 : {0} List", appName);
            Debug.WriteLine("Get                                  : {0} Get [PolicyID]", appName);
            Debug.WriteLine("                                     : {0} Get B2C_1A_PolicyName", appName);
            Debug.WriteLine("CreateRP                             : {0} CreateRP [SuSi, EditProfile, PasswordReset, ROPC]", appName);
            Debug.WriteLine("                                     : {0} CreateRP Susi", appName);
            Debug.WriteLine("Update                               : {0} Update [PolicyID] [RelativePathToXML]", appName);
            Debug.WriteLine("                                     : {0} Update B2C_1A_PolicyName updatepolicy.xml", appName);
            Debug.WriteLine("Delete                               : {0} Delete [PolicyID]", appName);
            Debug.WriteLine("                                     : {0} Delete B2C_1A_PolicyName", appName);
            Debug.WriteLine("Parse                                : {0} Parse ", appName);
            Debug.WriteLine("Parse-IDP                            : {0} Parse-IDP ", appName);
            Debug.WriteLine("Parse-UserFlows                      : {0} Parse-UserFlows", appName);
            Debug.WriteLine("Help                                 : {0} Help", appName);
            Console.ForegroundColor = ConsoleColor.White;
            Debug.WriteLine("");

            if (args.Length == 0)
            {
                Debug.WriteLine("[press any key to exit]");
                Console.ReadKey();
            }
        }


    }
}
