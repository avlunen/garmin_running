This is a simple commandline app written in C# to read running data from Garmin Fit files; cycling should also be handled at the moment, but needs testing (as I don't cycle). Any running/cycling activity should be handled, such as outdoor run or treadmill.

Optionally, it can write out an ESRI Shapefile (to be used with a GIS program, such as <a href="https://www.qgis.org">QGIS</a>), a KML file (for Google Maps/Earth) or GPX (usable with various apps) &mdash; or all of them &mdash; with the running track unless it is an indoor run (e.g., treadmill); furthermore, a CSV file (can be opened in a spreadsheet app) with all the data points listed, and a CSV file with aggregated stats (such as average speed or heart rate). It will also create a PNG file with a chart of the recorded heart rate.

To run: <tt>./garminrun -h</tt>: will give you hints on what parameters are accetable.

In a nutshell:

Mandatory:
<ul>
    <li><tt>--file &lt;filename&gt;</tt>: name of an individual *.fit file to decode</li>
    <li><tt>--file &lt;directory name&gt;</tt>: name of directory where a bunch of *.fit files reside, all to be decoded</li>
</ul>

Optional:
<ul>
    <li><tt>--s</tt>: export aggregated stats only</li>
    <li><tt>--shp</tt>: export running track as Shape file</li>
    <li><tt>--kml</tt>: export running track as KML file</li>
    <li><tt>--gpx</tt>: export running track as GPX file</li>
    <li><tt>--hr</tt>: export heart rate chart</li>
</ul>

Per default, only the CSV files will be exported (aggregated stats and details). Geo-files and chart need to be specified. If <tt>--s</tt> is issued, only the aggregated stats will be exported, regardless of any other switches.

The app does not connect to the Garmin device as such. Rather, it reads Fit files from a directory. Which means you can either download these files from your device and point to the directory where you saved these files; or hook up your device via a USB cable, which should make it available as drive (similar to a USB stick, you may need to configure this on your Garmin watch) in your OS, and then use the path to your Activity folder on your device as argument to this app to get to the Fit files without downloading them. The app will create a sub-folder named <tt>output</tt> in the folder where it is run from in which it will write the output files.

This will need some more testing (only have the Garmin Descent G1) and fine tuning.

Uses the Garmin Fit SDK (from NuGet), System.CommandLine (also from NuGet), ScottPlot (also NuGet), SharpKML (NuGet), <a href="https://github.com/rosspickard/ArcShapeFile/tree/master">ArcShapeFile</a> and Daniel Wolf's <a href="https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54">Progress Bar</a> (the latter two are included in this project).
