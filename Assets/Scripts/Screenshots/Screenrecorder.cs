using UnityEngine;
using System.IO;
using System.Diagnostics;
using System;
using System.Linq;

public static class Screenrecorder {

	private static Process process;
	private static StreamWriter writer;

	public static bool isClosed = true;


	public static void init(string filename) {
		isClosed = false;
		process = new Process ();

        String relativeOutFileLoc = @filename; 
        String ffmpegCommand = "-y -f image2pipe -i - -vf scale=trunc(iw/2)*2:trunc(ih/2)*2 -r 25 -c:v libx264 -pix_fmt yuv420p -crf 18 " + relativeOutFileLoc;

        // access ffmpeg from crowd:it directory
        String fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + 
                                       Path.DirectorySeparatorChar + "accu-rate" +
                                       Path.DirectorySeparatorChar + "crowd-it" +
                                       Path.DirectorySeparatorChar + "bin" +
                                       Path.DirectorySeparatorChar + "ffmpeg";

        DirectoryInfo dirInfo = new DirectoryInfo(fileName);
        // get latest file
        FileInfo ffmpegFile = (from f in dirInfo.GetFiles()
                      orderby f.LastWriteTime descending
                      select f).First();

        process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.FileName = ffmpegFile.FullName;
		process.StartInfo.Arguments = ffmpegCommand;

		process.Start ();
		writer = process.StandardInput;
		writer.AutoFlush = true;
	}

	public static void writeImg(byte[] img) {
		writer.BaseStream.Write(img, 0, img.Length);
	}

    private static String AddQuotesIfRequired(String path) {
        return 
            path.Contains(" ") && (!path.StartsWith(Path.DirectorySeparatorChar.ToString()) && !path.EndsWith(Path.DirectorySeparatorChar.ToString())) ?
                Path.DirectorySeparatorChar.ToString() + path + Path.DirectorySeparatorChar.ToString() : path;
    }

    public static void close() {
		writer.Close ();
		process.WaitForExit ();
		//process.Close (); // this would be a force-close, shouldn't be necessary
		isClosed = true;
	}

}
