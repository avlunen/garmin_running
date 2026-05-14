using System;
using System.IO;
using Dynastream.Fit;
using System.CommandLine;


namespace GarminRun
{
    static class Program
    {
        static int Main(string[] args)
        {
            Option<string> fileOption = new("--file") {
                Description = "The file to read."
            };
            Option<bool> statsOption = new("--s") {
                Description = "Export stats only"
            };
            Option<bool> shpOption = new("--shp") {
                Description = "Export track as shape file"
            };
            Option<bool> kmlOption = new("--kml") {
                Description = "Export track as KML file"
            };

            string root = "Garmin G1 Running Data Export v 1.1 -- Protocol "+Fit.ProtocolMajorVersion.ToString()
               +"."+Fit.ProtocolMinorVersion.ToString()+", Profile "+Fit.ProfileMajorVersion.ToString()
               +"."+Fit.ProfileMinorVersion.ToString();

            RootCommand rootCommand = new(root);
            rootCommand.Options.Add(fileOption);
            rootCommand.Options.Add(statsOption);
            rootCommand.Options.Add(shpOption);
            rootCommand.Options.Add(kmlOption);

            rootCommand.SetAction(parseResult => {
                string parsedFile = parseResult.GetValue(fileOption);
                bool stats_only = parseResult.GetValue(statsOption);
                bool shp_exp = parseResult.GetValue(shpOption);
                bool kml_exp = parseResult.GetValue(kmlOption);
                ReadFile(parsedFile, stats_only, shp_exp, kml_exp);
                return 0;
            });

            ParseResult parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }

        internal static void ReadFile(string fn, bool statsMode, bool shp, bool kml)
        {
            FileAttributes attr = System.IO.File.GetAttributes(fn);
            GarminRunningDecode dec = new GarminRunningDecode();

            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) { // Directory
                DirectoryInfo dir = new DirectoryInfo(fn);
                FileInfo[] files = dir.GetFiles("*.fit");

                var progress = new ProgressBar();
                int i = 1;

                foreach (FileInfo file in files) {
                    //Console.WriteLine("Name: " + file.Name);
                    dec.DecodeGarmin(fn, file.Name, statsMode, shp, kml);
                    double rate = (double) i++ / files.Length;
                    progress.Report(rate);
                    //Console.WriteLine("File {0} out of {1}, ({2}%)", i-1, files.Length, rate);
                }
                progress.Dispose();
            }
            else { // File
                //Console.WriteLine("Name: " + fn);
                dec.DecodeGarmin("", fn, statsMode, shp, kml);
            }
            Console.WriteLine("Finished!");
        }
    }
}
