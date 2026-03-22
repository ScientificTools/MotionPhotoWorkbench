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
            message = "Fichier introuvable.";
            return false;
        }

        byte[] data = File.ReadAllBytes(inputPath);

        // Cas le plus fréquent : MP4 appendu à la fin du JPEG.
        int mp4Start = FindMp4Start(data);
        if (mp4Start >= 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputMp4Path)!);
            File.WriteAllBytes(outputMp4Path, data.AsSpan(mp4Start).ToArray());
            message = "Vidéo embarquée détectée et extraite.";
            return true;
        }

        message = "Aucune vidéo embarquée détectée dans le fichier. Si Windows lit bien une Motion Photo mais que cette application ne trouve rien, il est possible que la copie du fichier ait perdu sa partie vidéo ou que le format diffère.";
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
                {
                    return start;
                }
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
                {
                    return ftyp - 4;
                }
            }
        }

        return -1;
    }
}
