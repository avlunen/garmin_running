using System;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Collections.Generic;
using static SharpKml.Engine.KmlFile;
using System.IO;
using System.Text;
using static System.Text.Encoding;

namespace garminrun
{
    class ExportKML
    {
        private Document m_document = new Document();

        public void AddPoint(string label, double lat, double lon)
        {
            var plm = new Placemark();
            plm.Name = label;
            plm.Geometry = new SharpKml.Dom.Point
            {
                Coordinate = new Vector(lat, lon)
            };

            m_document.AddFeature(plm);
        }

        public void AddLine(string label, CoordinateCollection col)
        {
            LineString linestring = new LineString();

            linestring.Coordinates = col;

            Placemark placemark = new Placemark();
            placemark.Name = label;
            placemark.Geometry = linestring;
            m_document.AddFeature(placemark);
        }

        public void export(string fn)
        {
            var kml = new Kml();
            FileStream fStream = null;
            //StreamWriter w_kml = null;

            // add document to kml
            kml.Feature = m_document;

            // create xml based kml file
            KmlFile kmlFile = KmlFile.Create(kml, false);

            // save file to a file stream
            fStream = new FileStream(fn, FileMode.Create);
            //w_kml = new StreamWriter(fStream, Encoding.UTF8);

            kmlFile.Save(fStream);

            fStream?.Flush();
            fStream?.Close();
        }
    }
}

