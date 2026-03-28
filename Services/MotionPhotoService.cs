using System;
using System.IO;
using System.Text;

namespace MotionPhotoWorkbench.Services;

public sealed class MotionPhotoService
{
    public bool TryExtractEmbeddedVideo(string inputPath, string outputMp4Path, out string message)
    {
        message = "";

        if (!File.Exists(inputPath))
        {
            message = "File not found.";
            return false;
        }

        byte[] data = File.ReadAllBytes(inputPath);

        // Cas le plus fréquent : MP4 appendu à la fin du JPEG.
        int mp4Start = FindMp4Start(data);
        if (mp4Start >= 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputMp4Path)!);
            File.WriteAllBytes(outputMp4Path, data.AsSpan(mp4Start).ToArray());
            message = "Embedded video detected and extracted.";
            return true;
        }

        message = "No embedded video detected in the file. If Windows can read it as a Motion Photo but this application cannot find anything, the copied file may have lost its video part or use a different format.";
        return false;
    }

    private static int FindMp4Start(byte[] data)
    {
        // Cherche une box ftyp : [size:4][ftyp:4][brand:4]...
        for (int i = 4; i <= data.Length - 12; i++)
        {
            if (data[i] == (byte)'f' && data[i + 1] == (byte)'t' && data[i + 2] == (byte)'y' && data[i + 3] == (byte)'p')
            {
                int start = i - 4;
                if (start >= 0)
                    return start;
            }
        }

        // Fallback simple : certains fichiers peuvent contenir la chaîne isom/mp42 près du header MP4.
        string ascii = Encoding.ASCII.GetString(data);
        foreach (string brand in new[] { "isom", "mp42", "mp41", "M4V ", "qt  " })
        {
            int idx = ascii.IndexOf(brand, StringComparison.Ordinal);
            if (idx >= 8)
            {
                int ftyp = idx - 4;
                if (ftyp >= 4 && ascii.AsSpan(ftyp, 4).SequenceEqual("ftyp"))
                    return ftyp - 4;
            }
        }

        return -1;
    }
}
