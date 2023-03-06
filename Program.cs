using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;


namespace Doxy_Combine
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var mc = new MainClass();

            // Get output directory
            var outPutDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            outPutDirectory = outPutDirectory.Substring(5);

            // Get log file path
            string logFilePath = Path.Combine(outPutDirectory, "LogFile.txt");
            string logFile = Path.Combine(Environment.CurrentDirectory, "LogFile.txt");

            // Clear the log file
            File.Create(logFile).Close();

            // Get config file path
            string configFilePath = Path.Combine(outPutDirectory, "config.xml");
            string configFile = Path.Combine(Environment.CurrentDirectory, "config.xml");

            // Config File Variables
            List<string> configValues = new List<string>();
            configValues = mc.GetConfigValues(configFile, logFile);

            if (configValues.Count < 6)
            {
                return;
            }

            // Config Variables
            string UnitySDKLoc = configValues[0];
            string UnityDocLoc = configValues[1];
            string AndroidSDKLoc = configValues[2];
            string AndroidDocLoc = configValues[3];
            string iOSSDKLoc = configValues[4];
            string iOSDocLoc = configValues[5];

            // Get SDK Versions from DoxyFile
            string UVersion = mc.GetSDKVersions(Path.Combine(UnitySDKLoc, "config", "Doxyfile"));
            string AVersion = mc.GetSDKVersions(Path.Combine(AndroidSDKLoc, "config", "Doxyfile")); ;
            string IVersion = mc.GetSDKVersions(Path.Combine(iOSSDKLoc, "config", "Doxyfile")); ;

            // Output Destinations
            string root = Path.Combine(outPutDirectory, "BFGSDK");
            string unityRoot = Path.Combine(root, "unity-docs");
            string androidRoot = Path.Combine(root, "android-docs");
            string iosRoot = Path.Combine(root, "ios-docs");

            // Create Merge Location
            mc.CreateFolders(outPutDirectory, UVersion, AVersion, IVersion);

            // Run Doxygen to get Outputs
            mc.RunDoxygen(UnitySDKLoc, UnityDocLoc, logFile);
            mc.RunDoxygen(AndroidSDKLoc, AndroidDocLoc, logFile);
            mc.RunDoxygen(iOSSDKLoc, iOSDocLoc, logFile);

            // Copy Files to Merge Location
            mc.CopyFiles(Path.Combine(UnitySDKLoc, UnityDocLoc), Path.Combine(unityRoot, UVersion), logFile);
            mc.CopyFiles(Path.Combine(AndroidSDKLoc, AndroidDocLoc), Path.Combine(androidRoot, AVersion), logFile);
            mc.CopyFiles(Path.Combine(iOSSDKLoc, iOSDocLoc), Path.Combine(iosRoot, IVersion), logFile);

        }

        public string GetSDKVersions(string path)
        {
            string line = "";
            StreamReader file = new StreamReader(path);
            while((line = file.ReadLine()) != null)
            {
                if (line.Contains("PROJECT_NUMBER"))
                {
                    line = line.Split('=')[1].ToString();
                    line = line.Trim();
                    line = line.Replace("\"", "");

                    return line;
                }
            }
            return "";
        }

        // Get the needed config values for running the script from the config file
        public List<string> GetConfigValues(string filePath, string logFile)
        {
            //string[] configValues = new string[5];
            List<string> configValues = new List<string>();
            XmlDocument xmldoc = new XmlDocument();

            try
            {
                xmldoc.Load(filePath);
                configValues.Add(xmldoc.SelectSingleNode("config/UnitySDK").InnerText);
                configValues.Add(xmldoc.SelectSingleNode("config/UnityDocs").InnerText);
                configValues.Add(xmldoc.SelectSingleNode("config/AndroidSDK").InnerText);
                configValues.Add(xmldoc.SelectSingleNode("config/AndroidDocs").InnerText);
                configValues.Add(xmldoc.SelectSingleNode("config/iOSSDK").InnerText);
                configValues.Add(xmldoc.SelectSingleNode("config/iOSDocs").InnerText);
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, "\n\n======================\n");
                File.AppendAllText(logFile, "CONFIGURATION ERROR\n\n");
                File.AppendAllText(logFile, "Config values not found in config file");
                File.AppendAllText(logFile, ex.ToString());
            }
            return configValues;
        }

        // Create temporary folder structure
        public void CreateFolders(string output, string uversion, string aversion, string iversion)
        {
            string root = Path.Combine(output, "BFGSDK");

            // Delete folder and all its contents to ensure clean start
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);

                string unityRoot = Path.Combine(root, "unity-docs");
                string unityVersion = Path.Combine(unityRoot, uversion);
                string androidRoot = Path.Combine(root, "android-docs");
                string androidVersion = Path.Combine(androidRoot, aversion);
                string iosRoot = Path.Combine(root, "ios-docs");
                string iosVersion = Path.Combine(iosRoot, iversion);

                Directory.CreateDirectory(unityRoot);
                Directory.CreateDirectory(unityVersion);
                Directory.CreateDirectory(Path.Combine(unityVersion, "search"));

                Directory.CreateDirectory(androidRoot);
                Directory.CreateDirectory(androidVersion);
                Directory.CreateDirectory(Path.Combine(androidVersion, "search"));

                Directory.CreateDirectory(iosRoot);
                Directory.CreateDirectory(iosVersion);
                Directory.CreateDirectory(Path.Combine(iosVersion, "search"));
            }
        }

        // Call the DoxyFiles of each project
        public void RunDoxygen(string docLoc, string exportLoc, string logFile)
        {
            // Clear out the output folders
            if (Directory.Exists(exportLoc))
            {
                Directory.Delete(exportLoc, true);
            }

            // Run the generate_docs.sh script
            string fileLoc = Path.Combine(docLoc, "generate_docs.sh");

            if (File.Exists(fileLoc))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = fileLoc,
                    WorkingDirectory = docLoc,
                    Arguments = "",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                Process proc = new Process()
                {
                    StartInfo = startInfo,
                };
                proc.Start();

                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    File.AppendAllText(logFile, line);
                    File.AppendAllText(logFile, "\n\n");
                }
            }
        }

        // Copy over files from projects
        public void CopyFiles(string docLoc, string destination, string logFile)
        {
            if (Directory.Exists(docLoc))
            {
                string[] allFiles = Directory.GetFiles(docLoc, "*.*", SearchOption.TopDirectoryOnly);
                string[] searchFiles = Directory.GetFiles(Path.Combine(docLoc, "search"), "*.*", SearchOption.TopDirectoryOnly);

                try
                {
                    foreach (string file in allFiles)
                    {
                        File.Copy(file, destination, true);
                    }
                    foreach (string file in searchFiles)
                    {
                        File.Copy(file, Path.Combine(destination, "search"), true);
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(logFile, "\n\n======================\n");
                    File.AppendAllText(logFile, "FILE COPY FAILED: " + docLoc + " : " + destination + "\n\n");
                    File.AppendAllText(logFile, ex.ToString());
                }
            }
            else
            {
                File.AppendAllText(logFile, "\n\n======================\n");
                File.AppendAllText(logFile, "NO FILES FOUND: " + docLoc + "\n\n");
            }
        }
    }
}

