using System;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.Text;
using System.IO;
using System.Threading;

using Genetec.Sdk;
using Genetec.Sdk.Entities;

using File = System.IO.File;

namespace GenetecTools
{
    class Program
    {
        private static bool initialize_sdk = false;
        private static Engine genetec_engine = new Engine();

        static void Main(string[] args)
        {

            string genetec_server, user_name, password, file_path;

            if (args.Length < 3)
            {
                // Display the 'Help' for this utility.
                Console.WriteLine("\nGenetec Camera Guid Query Tools Usage\n");
                Console.WriteLine("GenetecTools.exe server=<IP> user=<USER> pw=<PW> path=<PATH>\n");
                Console.WriteLine("Usage:");
                Console.WriteLine("<SERVER>=192.168.0.1");
                Console.WriteLine("<USER>=admin");
                Console.WriteLine("<PW>=pw1234");
                Console.WriteLine("<PATH>=C:\\GenetecQuery\\result.csv");
                return;
            }
            else
            {
                InstallContext context = new InstallContext(null, args);
                StringDictionary cli_args = context.Parameters;

                genetec_server = cli_args["server"];
                Console.WriteLine(string.Format("server = {0}", genetec_server));
                user_name = cli_args["user"];
                Console.WriteLine(string.Format("user = {0}", user_name));
                password = cli_args["pw"];
                Console.WriteLine(string.Format("pw = {0}", "**********"));
                file_path = cli_args["path"];
                Console.WriteLine(string.Format("path = {0}", file_path));
                
                if ((file_path == null) || (file_path == ""))
                {
                    Console.WriteLine("[INFO] Default Save File Path is \"C:\\GenetecQuery\\result.csv");
                    file_path = "C:\\GenetecQuery\\result.csv";
                }

                string directory = Path.GetDirectoryName(file_path);
                Directory.CreateDirectory(directory);

                if (Path.GetExtension(file_path) != ".csv")
                {
                    Console.WriteLine("[WARN] Change file extension to csv");
                    Path.ChangeExtension(file_path, ".csv");
                }
                
                if (File.Exists(file_path))
                {
                    Console.WriteLine("[INFO] Delete existed file. And create new one.");
                    File.Delete(file_path);
                }

                // 1. Login
                if (!initialize_sdk)
                {
                    InitializeSDK(genetec_server, user_name, password);
                    Thread.Sleep(5000);
                }

                // 2. Get Genetec Server Information
                Entity entity = genetec_engine.GetEntity(SdkGuids.SystemConfiguration);
                if (entity == null)
                {
                    Console.WriteLine("[ERROR] No System Entity");
                    return;
                }

                Console.WriteLine("[INFO] Start Genetec Tools");

                var csv = new StringBuilder();
                string csv_header = string.Format("CameraID, CameraGUID");
                csv.AppendLine(csv_header);

                // 3. Get Camera Name and Guid.
                foreach(Guid guid in entity.HierarchicalChildren)
                {
                    Entity child_entity = genetec_engine.GetEntity(guid);
                    if ((child_entity != null) && (child_entity.EntityType == EntityType.Camera))
                    {
                        Console.WriteLine(string.Format("{0}: {1}", child_entity.Name.ToString(), child_entity.Guid.ToString()));
                        string csv_row = string.Format("{0},{1}", child_entity.Name.ToString(), child_entity.Guid.ToString());
                        csv.AppendLine(csv_row);
                    }
                }
                File.WriteAllText(file_path, csv.ToString());
            }
        }

        private static void InitializeSDK(string address, string user, string password)
        {
            genetec_engine.BeginLogOn(address, user, password);
            genetec_engine.LoggedOn += OnEngineLoggedOn;
            genetec_engine.LoggedOff += OnEngineLoggedOff;
            genetec_engine.LogonFailed += OnEngineLogonFailed;
        }

        private static void OnEngineLoggedOn(object sender, LoggedOnEventArgs e)
        {
            initialize_sdk = genetec_engine.IsConnected;
            Console.WriteLine("[INFO] Success to Login Genetec server");
        }

        private static void OnEngineLoggedOff(object sender, LoggedOffEventArgs e)
        {
            Console.WriteLine("[INFO] Success to Logoff Genetec server");
        }

        private static void OnEngineLogonFailed(object sender, LogonFailedEventArgs e)
        {
            Console.WriteLine(e.FormattedErrorMessage);
            return;
        }
    }
}
