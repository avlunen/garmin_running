using ArcShapeFile;
using System.Collections.Generic;

/**
*   Class to export line segments to Shape file
*
*   @author Alexander von L&uuml;nen
*   @version 1.0
*   @since 13 May 2026
*/
namespace GarminRun
{
    static class ExportShp
    {
        public static void Export(string fn, MyLine line)
        {
            ShapeFile myShape = new();
            // Write shapefile
            myShape.Open(fn, eShapeType.shpPolyLine);

            myShape.Fields.Add("Date", eFieldType.shpDate);
            myShape.Fields.Add("Time", eFieldType.shpText);
            myShape.Fields.Add("altitude", eFieldType.shpFloat);
            myShape.Fields.Add("heart_rate", eFieldType.shpNumeric, 3, 0);
            myShape.Fields.Add("distance", eFieldType.shpFloat);
            myShape.Fields.Add("speed", eFieldType.shpFloat);

            myShape.WriteFieldDefs();

            foreach (MyLineSegment ls in line.GetSegs())
            {
                myShape.Vertices.Add(ls.m_start.X, ls.m_start.Y);
                myShape.Vertices.Add(ls.m_end.X, ls.m_end.Y);
                myShape.Fields[0].Value = ls.m_fields.Mtimestamp;
                myShape.Fields[1].Value = ls.m_fields.Mtime;
                myShape.Fields[2].Value = ls.m_fields.Malt;
                myShape.Fields[3].Value = ls.m_fields.Mhr;
                myShape.Fields[4].Value = ls.m_fields.Mdist;
                myShape.Fields[5].Value = ls.m_fields.Mspeed;
                myShape.WriteShape();
            }

            myShape.Close();
        }
    }
}