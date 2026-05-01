using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ArcShapeFile;
using Dynastream.Fit;

namespace GarminRun
{

    /**
    * Class to decode a Running Activity from a Garmin Fit file
    * Creates:
    *   1) an ESRI shapefile with the running tracks (as your Garmin device recorded it)
    *   2) a CSV file with the data from 1) tabulated
    *   3) a CSV file with aggregated statistics
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
*/
    public class GarminRunningDecode
    {
        private List<float> m_speeds = new List<float>();
        private List<byte> m_heartbeats = new List<byte>();
        private List<float> m_distances = new List<float>();
        private List<string> m_dates = new List<string>();
        private List<string> m_times = new List<string>();

        private Sport m_activity = new Sport();
        private SubSport m_subactivity = new SubSport();
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

        public void DecodeGarmin(string dir, string fn, bool statsOnly = false)
        {
            ShapeFile myShape = new ShapeFile();
            FileStream fitSource = null;
            FileStream fs_data = null;
            FileStream fs_stats = null;
            StreamWriter w_data = null;
            StreamWriter w_stats = null;
            bool ret = false;
            string fitfilename = dir;
            string fnstem = "";
            string subDir = "./output/";
            DirectoryInfo di;

            try
            {
                if (!dir.EndsWith("/") && dir != "") fitfilename += "/";
                fitfilename += fn;

                // Assumes that filenames end in ".fit"
                int pos = fn.LastIndexOf("/");
                if (pos == -1) pos = 0;
                fnstem = fn.Substring(pos + 1, fn.Length - (pos + 5));

                // Attempt to open .FIT file
                fitSource = new FileStream(fitfilename, FileMode.Open);
                //Console.WriteLine("Opening {0}", fn);

                Decode decoderGarmin = new Decode();

                // Use a FitListener to capture all decoded messages in a FitMessages object
                FitListener fitListener = new FitListener();
                decoderGarmin.MesgEvent += fitListener.OnMesg;

                //Console.WriteLine("Decoding...");
                decoderGarmin.Read(fitSource);

                FitMessages fitMessages = fitListener.FitMessages;

                // get device info
                getDeviceInfo(fitMessages.DeviceInfoMesgs);

                // decode sports
                foreach (SportMesg smesg in fitMessages.SportMesgs)
                    ret = checkSportMesg(smesg);

                if (ret == false)
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

                // create sub-directory, if it not exists
                if (!Directory.Exists(subDir))
                    di = Directory.CreateDirectory(subDir);

                // write data files
                fs_stats = new FileStream(subDir + "run-" + fnstem + "_stats.csv", FileMode.Create);
                w_stats = new StreamWriter(fs_stats, Encoding.UTF8);
                w_stats.WriteLine("Date_Start,Time_Start,Date_End,Time_End,Duration(mins),Distance(m),Avg_Heart_Rate(bpm),Avg_Speed(m/s)");

                if (statsOnly == false)
                {
                    fs_data = new FileStream(subDir + "run-" + fnstem + ".csv", FileMode.Create);
                    w_data = new StreamWriter(fs_data, Encoding.UTF8);
                    w_data.WriteLine("Date,Time,Lat,Lon,Alt,Distance,Heart_Rate,Speed");

                    if (m_subactivity.Equals(SubSport.Generic))
                    { // only write shapefile if outdoors
                      // Write shapefile
                        myShape.Open(subDir + "run-" + fnstem + ".shp", eShapeType.shpPoint);

                        myShape.Fields.Add("Date", eFieldType.shpDate);
                        myShape.Fields.Add("Time", eFieldType.shpText);
                        myShape.Fields.Add("altitude", eFieldType.shpFloat);
                        myShape.Fields.Add("heart_rate", eFieldType.shpNumeric, 3, 0);
                        myShape.Fields.Add("distance", eFieldType.shpFloat);
                        myShape.Fields.Add("speed", eFieldType.shpFloat);

                        myShape.WriteFieldDefs();

                        // write records
                        foreach (RecordMesg mesg in fitMessages.RecordMesgs)
                        {
                            decodeRecordMesg(mesg, myShape, w_data);
                        }

                    }
                    else
                    {
                        // write records
                        foreach (RecordMesg mesg in fitMessages.RecordMesgs)
                        {
                            decodeRecordMesg(mesg, null, w_data);
                        }
                    }
                }
                else
                {
                    foreach (RecordMesg mesg in fitMessages.RecordMesgs)
                    {
                        decodeRecordMesg(mesg, null, null);
                    }

                }
                // write Avgs
                var end = System.DateTime.Parse(m_dates.Max() + " " + m_times.Max());
                var start = System.DateTime.Parse(m_dates.Min() + " " + m_times.Min());
                TimeSpan mins = end.Subtract(start);
                w_stats.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", m_dates.Min(), m_times.Min(), m_dates.Max(), m_times.Max(),
                   mins.Minutes.ToString() + ":" + mins.Seconds.ToString(), m_distances.Max(), Math.Round(AvgHeartBeat(), 2), Math.Round(AvgSpeeds(), 2));

                // finished
                //Console.WriteLine("Decoded FIT file {0}", fn);
                //Console.WriteLine();
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
            }
            finally
            {
                fitSource?.Close();
                fs_data?.Close();
                fs_stats?.Close();
                myShape?.Close();
            }

        }

        private bool checkSportMesg(SportMesg mesg)
        {
            Sport? sp = mesg.GetSport();
            SubSport? ssp = mesg.GetSubSport();

            if (sp != null) m_activity = (Sport)sp;
            if (ssp != null) m_subactivity = (SubSport)ssp;

            if (sp.Equals(Sport.Running) || sp.Equals(Sport.Cycling)) return true;

            return false;
        }

        private void decodeRecordMesg(RecordMesg mesg, ShapeFile shp, StreamWriter wo)
        {
            System.DateTime timestamp;
            string date;
            string time;
            double lat;
            double lon;
            byte heart_rate;
            float distance;
            float altitude;
            float speed;
            object o_ret;


            if (mesg.GetTimestamp() != null)
            {
                timestamp = System.DateTime.Parse(mesg.GetTimestamp().ToString());
                date = timestamp.ToShortDateString();
                time = timestamp.ToLongTimeString();
            }
            else return;

            // decode record fields, setting respective field to zero if no record found
            // (this can happen, for instance, if a GPS connection has not been established,
            // but the run was commenced anyway)
            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.HeartRate);
            if (o_ret != null) heart_rate = (byte)o_ret;
            else heart_rate = 0;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.Distance);
            if (o_ret != null) distance = (float)o_ret;
            else distance = 0.0f;

            // TODO altitude looks off, I think there is an offset to be added, need to check SDK docs
            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.EnhancedAltitude);
            if (o_ret != null) altitude = (float)o_ret;
            else altitude = 0.0f;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.EnhancedSpeed);
            if (o_ret != null) speed = (float)o_ret;
            else speed = 0.0f;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.PositionLat);
            if (o_ret != null) lat = semicircles2degrees((int)o_ret);
            else lat = 0;

            o_ret = decodeField(mesg, RecordMesg.FieldDefNum.PositionLong);
            if (o_ret != null) lon = semicircles2degrees((int)o_ret);
            else lon = 0;

            // write data to datafile
            wo?.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", date, time, lat, lon, altitude, distance, heart_rate, speed);

            // write data to shapefile
            if (shp != null)
            {
                shp.Vertices.Add(lon, lat);
                shp.Fields[0].Value = timestamp;
                shp.Fields[1].Value = time;
                shp.Fields[2].Value = altitude;
                shp.Fields[3].Value = heart_rate;
                shp.Fields[4].Value = distance;
                shp.Fields[5].Value = speed;
                shp.WriteShape();
            }
            // collect data for stats
            m_speeds.Add(speed);
            m_heartbeats.Add(heart_rate);
            m_distances.Add(distance);
            m_dates.Add(date);
            m_times.Add(time);
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