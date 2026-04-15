using System;
using System.IO;
using Dynastream.Fit;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace GarminRun
{
    class Program
    {
        static int Main(string[] args)
        {
            Option<string> fileOption = new("--file") {
                Description = "The file to read."
            };
            Option<bool> statsOption = new("--s") {
                Description = "Export stats only"
            };

            string root = "Garmin G1 Running Data Export -- Protocol "+Fit.ProtocolMajorVersion.ToString()
               +"."+Fit.ProtocolMinorVersion.ToString()+", Profile "+Fit.ProfileMajorVersion.ToString()
               +"."+Fit.ProfileMinorVersion.ToString();

            RootCommand rootCommand = new(root);
            rootCommand.Options.Add(fileOption);
            rootCommand.Options.Add(statsOption);

            rootCommand.SetAction(parseResult => {
                string parsedFile = parseResult.GetValue(fileOption);
                bool stats_only = parseResult.GetValue(statsOption);
                ReadFile(parsedFile, stats_only);
                return 0;
            });

            ParseResult parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }

        internal static void ReadFile(string fn, bool statsMode)
        {
            FileAttributes attr = System.IO.File.GetAttributes(fn);
            GarminRunningDecode dec = new GarminRunningDecode();

            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) { // Directory
                DirectoryInfo dir = new DirectoryInfo(fn);
                FileInfo[] files = dir.GetFiles("*.fit");

                foreach (FileInfo file in files) {
                    Console.WriteLine("Name: " + file.Name);
                    dec.DecodeGarmin(fn, file.Name, statsMode);    
                }                
            }
            else { // File
                Console.WriteLine("Name: " + fn);
                dec.DecodeGarmin("", fn, statsMode);
            }

        }
    }
}
