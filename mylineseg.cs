using System.Numerics;

namespace GarminRun
{
    class MyLineSegment
    {
        public Vector2 m_start { get; set; }
        public Vector2 m_end { get; set; }
        public MyFields m_fields { get; set; }

        public MyLineSegment(Vector2 start, Vector2 end, MyFields field)
        {
            this.m_start = start;
            this.m_end = end;
            this.m_fields = field;
        }
    }
}