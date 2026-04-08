This is a simple commandline app written in C# to read running data from a Garmin Fit file.

It writes out an ESRI Shapefile (to be used with a GIS program, such as <a href="https://www.qgis.org">QGIS</a>) with the running track, a CSV file (can be opened in a spreadsheet app) with all the data points listed, and a CSV file with aggregated stats (such as average speed or heart rate).

To run: <tt>./garminrun &lt;path to folder with Fit files&gt;</tt>

The app does not connect to the Garmin device as such. Rather, it reads Fit files from a directory. Which means you can either download these files from your device, or hook your device up on a USB cable, which should make it available as drive (similar to a USB stick) in your OS, and then use the path to your Activities folder on your device as argument to get to the Fit files without downloading them. The app will create a sub-folder named "output" in the folder where it is run from in which it will write the output files.