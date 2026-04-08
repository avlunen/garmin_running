using System;
using System.IO;
using Dynastream.Fit;

namespace GarminRun
{
    class Program
    {
        static void Main(string[] args)
        {
            GarminRunningDecode dec = new GarminRunningDecode();

            Console.WriteLine("Garmin G1 Running Data Export - Protocol {0}.{0} Profile {0}.{0}", Fit.ProtocolMajorVersion.ToString(), Fit.ProtocolMinorVersion.ToString(), Fit.ProfileMajorVersion.ToString(), Fit.ProfileMinorVersion.ToString());

            if (args.Length != 1) {
               Console.WriteLine("Usage: garminrun <dirname>");
               return;
            }
            DirectoryInfo dir = new DirectoryInfo(args[0]);
            FileInfo[] files = dir.GetFiles("*.fit");

            foreach (FileInfo file in files) {
               Console.WriteLine("Name: " + file.Name);
               dec.DecodeGarmin(args[0], file.Name);    
            }
            //
        }
    }
}
