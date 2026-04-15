This is a simple commandline app written in C# to read running data from Garmin Fit files.

It writes out an ESRI Shapefile (to be used with a GIS program, such as <a href="https://www.qgis.org">QGIS</a>) with the running track, a CSV file (can be opened in a spreadsheet app) with all the data points listed, and a CSV file with aggregated stats (such as average speed or heart rate).

To run: <tt>./garminrun -h</tt> -- will give you hints on what parameters are accetable.

In a nutshell:
Mandatory:
<ul>
    <li><tt>--file &lt;filename&gt;</tt>: name of an individual *.fit file to decode</li>
    <li><tt>--file &lt;directory name&gt;</tt>: name of directory where a bunch of *.fit files reside, all to be decoded</li>
</ul>

Optional:
<ul>
    <li><tt>--s</tt>: export aggregated stats only</li>
</ul>

The app does not connect to the Garmin device as such. Rather, it reads Fit files from a directory. Which means you can either download these files from your device and point to the directory where you saved these files, or hook your device up on a USB cable, which should make it available as drive (similar to a USB stick, you may need to configure this on your Garmin watch) in your OS, and then use the path to your Activity folder on your device as argument to get to the Fit files without downloading them. The app will create a sub-folder named "output" in the folder where it is run from in which it will write the output files.

This will need some more testing (only have the Garmin G1) and fine tuning. Looking into how to access the data via Bluetooth.

Uses the Garmin Fit SDK (from NuGet) and <a href="https://github.com/rosspickard/ArcShapeFile/tree/master">ArcShapeFile</a> (included in this project).