using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Collections.Generic;
using System.Numerics;

namespace GarminRun
{
    class MyLine
    {
        private List<MyLineSegment> m_pts = [];

        public void AddSegment(Vector2 s, Vector2 e, MyFields f)
        {
            m_pts.Add(new MyLineSegment(s, e, f));
        }

        public List<MyLineSegment> GetSegs()
        {
            return m_pts;
        }

        public long Len()
        {
            return m_pts.Count;
        }
/*
        public LineString AsKMLLineString()
        {
            LineString linestring = new();
            CoordinateCollection coordinates = new CoordinateCollection();

            foreach (Vector2 pt in m_pts)
                coordinates.Add(new SharpKml.Base.Vector(pt.Y, pt.X));
            linestring.Coordinates = coordinates;

            return linestring;
        }
*/
    }
}