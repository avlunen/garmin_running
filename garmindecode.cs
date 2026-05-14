using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using Dynastream.Fit;
using ScottPlot;

namespace GarminRun
{

    /**
    * Class to decode a Running Activity from a Garmin Fit file
    * Creates:
    *   1) an ESRI shapefile with the running tracks (as your Garmin device recorded it)
    *   2) a CSV file with the data from 1) tabulated
    *   3) a CSV file with aggregated statistics
    *   4) a chart with the heart rate plotted over the excercise time
    *
    *   These files are written into a sub-folder 'output', which will be created if it
    *   does not exist.
    *
    *   @author Alexander von Lunen
    *   @since 07 Apr 2026
    *   @version 0.9
    *   @since 15 Apr 2026
    *   @version 0.91
    *   @since 19 Apr 2026
    *   @version 0.92
    *   @since 24 Apr 2026
    *   @version 0.95
    *   @since 29 Apr 2026
    *   @version 0.96
    *   @since 12 May 2026
    *   @version 1.0
    *   @since 13 May 2026
    *   @version 1.1
*/
    public class GarminRunningDecode
    {
        private List<float> m_speeds = [];
        private List<byte> m_heartbeats = [];
        private List<float> m_distances = [];
        private List<string> m_dates = [];
        private List<string> m_times = [];
        private List<System.DateTime> m_timestamps = [];

        private List<MyFields> m_fields = [];
        private List<Vector2> m_pts = [];

        private Sport m_activity = new();
        private SubSport m_subactivity = new();
        private UInt32 m_serial_no;
        private Single m_firmware;


        public Sport getActivity()
        {
            return m_activity;
        }

        public double AvgSpeeds()
        {
            return m_speeds.Aggregate(0.0, (sum, x) => sum + x) / m_speeds.Count;
        }

        public double AvgHeartBeat()
        {
            return m_heartbeats.Aggregate(0.0, (sum, x) => sum + x) / m_heartbeats.Count;
        }

        public double semicircles2degrees(int semic)
        {
            return semic * (double)(180.0 / 2147483648.0);
        }

        public int degrees2semicircles(double degrees)
        {
            return (int)(degrees * (2147483648.0 / 180.0));
        }

        private void CreateHRChart(string fn)
        {
            ScottPlot.TickGenerators.NumericManual ticks = new();
            ScottPlot.Plot myPlot = new();
            var dataY = m_heartbeats.ToArray();
            List<long> dataXL = new List<long>();
            var endDate = m_timestamps.Max();
            var startDate = m_timestamps.Min();
            System.TimeSpan span = endDate - startDate;

            foreach(System.DateTime dat in m_timestamps)
            {
                System.TimeSpan tmp = dat - startDate;
                long dats = (tmp.Minutes * 60) + tmp.Seconds;
                dataXL.Add(dats);

                if (dats % 60 == 0)
                {
                    ticks.AddMajor(dats, $"{dats/60}");
                }
                else if(dats % 30 == 0)
                {
                    ticks.AddMinor(dats);
                }
            }

            var sp = myPlot.Add.Scatter(dataXL.ToArray(), dataY);

            myPlot.Title("Heart Rate");
            myPlot.XLabel("Minutes");
            myPlot.YLabel("BPM");
            myPlot.Axes.SetLimitsX(0, (span.Minutes * 60) + span.Seconds + 10);
            myPlot.Axes.SetLimitsY(m_heartbeats.Min()-5, m_heartbeats.Max()+5);
            myPlot.Axes.Bottom.TickGenerator = ticks;
            sp.FillY = true;
            sp.FillYColor = sp.Color.WithAlpha(.2);

            myPlot.SavePng(fn, 2560, 1024);
        }

        public void DecodeGarmin(string dir, string fn, bool statsOnly = false, bool shpexp = false, bool kmlexp = false, bool hrexp = false)
        {
            MyLine line = new();
            FileStream fitSource = null;
            FileStream fs_data = null;
            FileStream fs_stats = null;
            StreamWriter w_data = null;
            StreamWriter w_stats = null;
            bool ret = false;
            string fitfilename = dir;
            string fnstem = "";
            const string subDir = "./output/";
            DirectoryInfo di;

            try
            {
                if (!dir.EndsWith('/') && dir != "") fitfilename += "/";
                fitfilename += fn;

                // Assumes that filenames end in ".fit"
                int pos = fn.LastIndexOf('/');
                if (pos == -1) pos = 0;
                fnstem = fn.Substring(pos + 1, fn.Length - (pos + 5));

                // Attempt to open .FIT file
                fitSource = new FileStream(fitfilename, FileMode.Open);
                //Console.WriteLine("Opening {0}", fn);

                Decode decoderGarmin = new();

                // Use a FitListener to capture all decoded messages in a FitMessages object
                FitListener fitListener = new();
                decoderGarmin.MesgEvent += fitListener.OnMesg;

                //Console.WriteLine("Decoding...");
                decoderGarmin.Read(fitSource);

                FitMessages fitMessages = fitListener.FitMessages;

                // get device info
                getDeviceInfo(fitMessages.DeviceInfoMesgs);

                // decode sports
                foreach (SportMesg smesg in fitMessages.SportMesgs)
                    ret = checkSportMesg(smesg);

                if (!ret)
                {
                    Console.WriteLine("\tFile {0} is not a recognized activity!\n", fn);
                    return;
                }

                // reset members
                m_distances.Clear();
                m_heartbeats.Clear();
                m_speeds.Clear();
                m_dates.Clear();
                m_times.Clear();
                m_timestamps.Clear();

                // create sub-directory, if it not exists
                if (!Directory.Exists(subDir))
                    di = Directory.CreateDirectory(subDir);

                // write avg. data file
                fs_stats = new FileStream(subDir + "run-" + fnstem + "_stats.csv", FileMode.Create);
                w_stats = new StreamWriter(fs_stats, Encoding.UTF8);
                w_stats.WriteLine("Date_Start,Time_Start,Date_End,Time_End,Duration(mins),Distance(m),Avg_Heart_Rate(bpm),Avg_Speed(m/s)");

                // decode Garmin data
                foreach (RecordMesg mesg in fitMessages.RecordMesgs)
                {
                    decodeRecordMesg(mesg);
                }

                // write Avgs
                var end = System.DateTime.Parse(m_dates.Max() + " " + m_times.Max());
                var start = System.DateTime.Parse(m_dates.Min() + " " + m_times.Min());
                TimeSpan mins = end.Subtract(start);
                w_stats.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", m_dates.Min(), m_times.Min(), m_dates.Max(), m_times.Max(),
                    mins.Minutes.ToString() + ":" + mins.Seconds.ToString(), m_distances.Max(), Math.Round(AvgHeartBeat(), 2), Math.Round(AvgSpeeds(), 2));

                // write data
                if (!statsOnly)
                {
                    // write raw data
                    fs_data = new FileStream(subDir + "run-" + fnstem + ".csv", FileMode.Create);
                    w_data = new StreamWriter(fs_data, Encoding.UTF8);
                    w_data.WriteLine("Date,Time,Lat,Lon,Alt,Distance,Heart_Rate,Speed");

                    var ptsAndfieldfs = m_pts.Zip(m_fields, (n, w) => new { pos = n, fld = w });
                    foreach (var nw in ptsAndfieldfs)
                    {
                        w_data.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", nw.fld.Mdate, nw.fld.Mtime, nw.pos.Y, nw.pos.X,
                            nw.fld.Malt, nw.fld.Mdist, nw.fld.Mhr, nw.fld.Mspeed);
                    }

                    // create HR chart
                    if(hrexp)
                        CreateHRChart(subDir + "run-" + fnstem + ".png");

                    // write geo files    
                    if (m_subactivity.Equals(SubSport.Generic)) // only write shapefile if outdoors; TODO needs better check
                    {
                        // create line segments
                        Vector2 prev_pt = Vector2.Zero;
                        MyFields prev_fld = new();

                        foreach (var nw in ptsAndfieldfs)
                        {
                            if (prev_pt != Vector2.Zero)
                            {
                                MyFields fl = new()
                                {
                                    Malt = (prev_fld.Malt + nw.fld.Malt) / 2,
                                    Mtimestamp = nw.fld.Mtimestamp,
                                    Mdate = nw.fld.Mdate,
                                    Mtime = nw.fld.Mtime,
                                    Mhr = (byte)((prev_fld.Mhr + nw.fld.Mhr) / 2),
                                    Mspeed = (prev_fld.Mspeed + nw.fld.Mspeed) / 2,
                                    Mdist = nw.fld.Mdist - prev_fld.Mdist
                                };
                                line.AddSegment(prev_pt, nw.pos, fl);
                            }
                            prev_pt = nw.pos;
                            prev_fld = nw.fld;
                        }

                        if (shpexp)
                            ExportShp.Export(subDir + "run-" + fnstem + ".shp", line);

                        if (kmlexp)
                        {
                            ExportKML myKML = new();

                            foreach (MyLineSegment mls in line.GetSegs())
                                myKML.AddLine(mls);

                            myKML.Export(subDir + "run-" + fnstem + ".kml");
                        }
                    }
                }
                // finished
                w_data?.Flush();
                w_stats?.Flush();
            }
            catch (FitException ex)
            {
                Console.WriteLine("A FitException occurred when trying to decode the FIT file. Message: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("The caller does not have the required permission to create `{0}`; message: {1}", subDir, ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred when trying to decode the FIT file. Message: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                fitSource?.Close();
                fs_data?.Close();
                fs_stats?.Close();
            }
        }

        private bool checkSportMesg(SportMesg mesg)
        {
            Sport? sp = mesg.GetSport();
            SubSport? ssp = mesg.GetSubSport();

            if (sp != null) m_activity = (Sport)sp;
            if (ssp != null) m_subactivity = (SubSport)ssp;

            return sp.Equals(Sport.Running) || sp.Equals(Sport.Cycling);
        }

        private void decodeRecordMesg(RecordMesg mesg)
        {
            MyFields field = new();
            double lat;
            double lon;
            object o_ret;


            if (mesg.GetTimestamp() != null)
            {
                field.Mtimestamp = System.DateTime.Parse(mesg.GetTimestamp().ToString());
                field.Mdate = field.Mtimestamp.ToShortDateString();
                field.Mtime = field.Mtimestamp.ToLongTimeString();
            }
            else
            {
                return;
            }
            // decode record fields, setting respective field to zero if no record found
            // (this can happen, for instance, if a GPS connection has not been established,
            // but the run was commenced anyway)
            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.HeartRate);
            if (o_ret != null) field.Mhr = (byte)o_ret;
            else field.Mhr = 0;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.Distance);
            if (o_ret != null) field.Mdist = (float)o_ret;
            else field.Mdist = 0.0f;

            // TODO altitude looks off, I think there is an offset to be added, need to check SDK docs
            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.EnhancedAltitude);
            if (o_ret != null) field.Malt = (float)o_ret;
            else field.Malt = 0.0f;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.EnhancedSpeed);
            if (o_ret != null) field.Mspeed = (float)o_ret;
            else field.Mspeed = 0.0f;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.PositionLat);
            if (o_ret != null) lat = semicircles2degrees((int)o_ret);
            else lat = 0;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.PositionLong);
            if (o_ret != null) lon = semicircles2degrees((int)o_ret);
            else lon = 0;

            m_fields.Add(field);
            m_pts.Add(new Vector2((float)lon, (float)lat));

            // collect data for stats
            m_speeds.Add(field.Mspeed);
            m_heartbeats.Add(field.Mhr);
            m_distances.Add(field.Mdist);
            m_dates.Add(field.Mdate);
            m_times.Add(field.Mtime);
            m_timestamps.Add(field.Mtimestamp);
        }

        private object decodeField(Mesg mesg, byte fieldNumber)
        {
            Dynastream.Fit.Field profileField = Profile.GetField(mesg.Num, fieldNumber);

            if (profileField == null) return null;

            IEnumerable<FieldBase> fields = mesg.GetOverrideField(fieldNumber);

            foreach (FieldBase field in fields) return field.GetValue();

            return null;
        }

        private void getDeviceInfo(ReadOnlyCollection<DeviceInfoMesg> mesgs, bool print = true)
        {
            object o_ret;

            foreach (DeviceInfoMesg msg in mesgs)
            {
                o_ret = decodeField(msg, DeviceInfoMesg.FieldDefNum.SerialNumber);
                if (o_ret != null)
                {
                    m_serial_no = (UInt32)o_ret;

                    if (print)
                        Console.WriteLine("Serial No. {0}", m_serial_no);

                    o_ret = decodeField(msg, DeviceInfoMesg.FieldDefNum.SoftwareVersion);
                    if (o_ret != null)
                    {
                        m_firmware = (Single)o_ret;

                        if (print)
                            Console.WriteLine("Firmware: {0}", m_firmware);
                    }
                    break;
                }
            }
        }
    }
}