using System.Drawing;
using System.Windows.Forms;

namespace MotionPhotoWorkbench;

internal sealed class ExportCompletedForm : Form
{
    public ExportCompletedForm(string message)
    {
        Text = "Export";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(420, 140);

        var label = new Label
        {
            Text = message, AutoSize = false, Dock = DockStyle.Fill,
            Padding = new Padding(20), TextAlign = ContentAlignment.MiddleLeft
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 55,
            FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10)
        };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var view = new Button { Text = "Visualiser", DialogResult = DialogResult.Yes, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(view);
        Controls.Add(label);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = ok;
    }
}
