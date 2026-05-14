using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GarminRun
{
    static class MyColorRamp
    {
        readonly private static Dictionary<int, string> dict = new()
        {
            {1, "#006837"},
            {2, "#18944E"},
            {3, "#58B660"},
            {4, "#95D168"},
            {5, "#C6E77F"},
            {6, "#EEF8A7"},
            {7, "#FFF1A7"},
            {8, "#FECE7C"},
            {9, "#FB9C59"},
            {10, "#EF623E"},
            {11, "#D22C27"},
            {12, "#A50026"}
        };

        public static string MapValue(int a0, int a1, int b0, int b1, int a)
        {
            return dict[b0 + ((b1 - b0) * ((a - a0) / (a1 - a0)))];
        }

        /**
        * Mapping from 60 to 180
        */
        public static int MapValue(int a)
        {
            int b = (int)(1 + (11 * ((a - 60.0) / 120.0)));

            return b;
        }

    }
}