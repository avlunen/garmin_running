using System;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Collections.Generic;
using static SharpKml.Engine.KmlFile;
using System.IO;
using System.Text;
using static System.Text.Encoding;

namespace GarminRun
{
    class ExportKML
    {
        readonly private static Dictionary<int, string> coldict = new()
        {
            {1, "#ff376800"},
            {2, "#ff4E9418"},
            {3, "#ff60B658"},
            {4, "#ff68D195"},
            {5, "#ff7FE7C6"},
            {6, "#ffA7F8EE"},
            {7, "#ffA7F1FF"},
            {8, "#ff7CCEFE"},
            {9, "#ff599CFB"},
            {10, "#ff3E62EF"},
            {11, "#ff272CD2"},
            {12, "#ff2600A5"}
        };

        private Document m_document = new Document();

        public ExportKML()
        {
            for (int i = 1; i < 13; i++)
            {
                SharpKml.Dom.LineStyle lineStyle = new()
                {
                    Color = Color32.Parse(coldict[i]),
                    Width = 6
                };

                Style runStyle = new()
                {
                    Id = "RunStyle"+i,
                    Line = lineStyle
                };

                // Add style to document
                m_document.AddStyle(runStyle);
            }
        }

        public void AddPoint(string label, double lat, double lon)
        {
            var plm = new Placemark
            {
                Name = label,
                Geometry = new SharpKml.Dom.Point
                {
                    Coordinate = new Vector(lat, lon)
                }
            };

            m_document.AddFeature(plm);
        }

        public void AddLine(string label, CoordinateCollection col)
        {
            LineString linestring = new()
            {
                Coordinates = col
            };

            Placemark placemark = new()
            {
                Name = label,
                Geometry = linestring
            };

            m_document.AddFeature(placemark);
        }

        public void AddLine(string label, LineString line)
        {
            Placemark placemark = new()
            {
                Name = label,
                Geometry = line
            };
            m_document.AddFeature(placemark);
        }

        public void AddLine(MyLineSegment line)
        {
            LineString linea = new();
            CoordinateCollection coordinates = [new Vector(line.m_start.Y, line.m_start.X), new Vector(line.m_end.Y, line.m_end.X)];
            linea.Coordinates = coordinates;

            Placemark placemark = new()
            {
                Name = line.m_fields.Mtimestamp.ToString(),
                Geometry = linea,
                // Specify style for your placemark by url
                StyleUrl = new Uri("#RunStyle"+MyColorRamp.MapValue(line.m_fields.Mhr), UriKind.Relative)
            };

            m_document.AddFeature(placemark);
        }

        public void Export(string fn)
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

