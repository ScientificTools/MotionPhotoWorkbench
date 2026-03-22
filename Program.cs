using System;
using System.Windows.Forms;

namespace MotionPhotoWorkbench;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
