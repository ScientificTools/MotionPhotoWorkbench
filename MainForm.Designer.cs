using System;
using System.Drawing;
using System.Windows.Forms;

namespace MotionPhotoWorkbench;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    private Button btnOpenInput = null!;
    private Button btnPrev = null!;
    private Button btnNext = null!;
    private Button btnZoomIn = null!;
    private Button btnZoomOut = null!;
    private Button btnToggleKeep = null!;
    private Button btnAutoAnchorOtherFrames = null!;
    private Button btnRenderAndExportGif = null!;
    private Button btnSaveProject = null!;
    private Button btnLoadProject = null!;
    private Button btnCleanCacheProject = null!;

    private ListBox listBoxFrames = null!;
    private PictureBox pictureBoxFrame = null!;

    private Label lblStatus = null!;
    private Label lblFrameInfo = null!;
    private Label lblFrameLegend = null!;
    private ContextMenuStrip contextMenuFrames = null!;
    private ToolStripMenuItem menuToggleKeep = null!;

    private TrackBar trackBrightness = null!;
    private TrackBar trackContrast = null!;
    private TrackBar trackSaturation = null!;
    private TrackBar trackTemperature = null!;
    private TrackBar trackSharpness = null!;
    private TrackBar trackHighlights = null!;
    private TrackBar trackShadows = null!;
    private TextBox txtBrightnessValue = null!;
    private TextBox txtContrastValue = null!;
    private TextBox txtSaturationValue = null!;
    private TextBox txtTemperatureValue = null!;
    private TextBox txtSharpnessValue = null!;
    private TextBox txtHighlightsValue = null!;
    private TextBox txtShadowsValue = null!;
    private Button btnResetAdjustments = null!;

    private TableLayoutPanel rootLayout = null!;
    private FlowLayoutPanel topBar = null!;
    private SplitContainer splitMain = null!;
    private SplitContainer splitRight = null!;
    private TableLayoutPanel statusLayout = null!;
    private TableLayoutPanel imagePanel = null!;
    private TableLayoutPanel settingsLayout = null!;
    private Panel imageHeaderPanel = null!;
    private GroupBox groupNavigation = null!;
    private GroupBox groupGif = null!;
    private GroupBox groupAdjustments = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _adjustmentPreviewTimer?.Stop();
            _adjustmentPreviewTimer?.Dispose();
            _framePreviewCts?.Cancel();
            _framePreviewCts?.Dispose();
            pictureBoxFrame.Image?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        btnOpenInput = new Button();
        btnPrev = new Button();
        btnNext = new Button();
        btnZoomIn = new Button();
        btnZoomOut = new Button();
        btnToggleKeep = new Button();
        btnAutoAnchorOtherFrames = new Button();
        btnRenderAndExportGif = new Button();
        btnSaveProject = new Button();
        btnLoadProject = new Button();
        btnCleanCacheProject = new Button();

        listBoxFrames = new ListBox();
        pictureBoxFrame = new PictureBox();

        lblStatus = new Label();
        lblFrameInfo = new Label();
        lblFrameLegend = new Label();
        contextMenuFrames = new ContextMenuStrip(components);
        menuToggleKeep = new ToolStripMenuItem();

        trackBrightness = new TrackBar();
        trackContrast = new TrackBar();
        trackSaturation = new TrackBar();
        trackTemperature = new TrackBar();
        trackSharpness = new TrackBar();
        trackHighlights = new TrackBar();
        trackShadows = new TrackBar();
        txtBrightnessValue = new TextBox();
        txtContrastValue = new TextBox();
        txtSaturationValue = new TextBox();
        txtTemperatureValue = new TextBox();
        txtSharpnessValue = new TextBox();
        txtHighlightsValue = new TextBox();
        txtShadowsValue = new TextBox();
        btnResetAdjustments = new Button();

        rootLayout = new TableLayoutPanel();
        topBar = new FlowLayoutPanel();
        splitMain = new SplitContainer();
        splitRight = new SplitContainer();
        statusLayout = new TableLayoutPanel();
        imagePanel = new TableLayoutPanel();
        settingsLayout = new TableLayoutPanel();
        imageHeaderPanel = new Panel();
        groupNavigation = new GroupBox();
        groupGif = new GroupBox();
        groupAdjustments = new GroupBox();
        ((System.ComponentModel.ISupportInitialize)pictureBoxFrame).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        splitRight.Panel1.SuspendLayout();
        splitRight.Panel2.SuspendLayout();
        splitRight.SuspendLayout();
        rootLayout.SuspendLayout();
        imagePanel.SuspendLayout();
        statusLayout.SuspendLayout();
        groupNavigation.SuspendLayout();
        groupGif.SuspendLayout();
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.Font;
        Text = "Motion Photo Workbench V1.2.1";
        MinimumSize = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1450;
        Height = 900;

        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.Dock = DockStyle.Fill;
        Controls.Add(rootLayout);

        Load += MainForm_Load;
        Shown += MainForm_Shown;
        Resize += MainForm_Resize;

        topBar.AutoSize = true;
        topBar.WrapContents = true;
        topBar.Dock = DockStyle.Fill;
        topBar.Padding = new Padding(8);
        topBar.Controls.Add(btnOpenInput);
        topBar.Controls.Add(btnSaveProject);
        topBar.Controls.Add(btnLoadProject);
        topBar.Controls.Add(btnCleanCacheProject);
        rootLayout.Controls.Add(topBar, 0, 0);

        btnOpenInput.Text = "Open";
        btnOpenInput.AutoSize = true;
        btnOpenInput.Click += btnOpenInput_Click;

        btnSaveProject.Text = "Save project";
        btnSaveProject.AutoSize = true;
        btnSaveProject.Click += btnSaveProject_Click;

        btnLoadProject.Text = "Load project";
        btnLoadProject.AutoSize = true;
        btnLoadProject.Click += btnLoadProject_Click;

        btnCleanCacheProject.Text = "Clean project cache";
        btnCleanCacheProject.AutoSize = true;
        btnCleanCacheProject.Visible = false;
        btnCleanCacheProject.Click += btnCleanCacheProject_Click;

        splitMain.Dock = DockStyle.Fill;
        splitMain.FixedPanel = FixedPanel.Panel1;
        splitMain.Panel1MinSize = 100;
        splitMain.Panel2MinSize = 100;
        rootLayout.Controls.Add(splitMain, 0, 1);

        var framesPanel = new TableLayoutPanel();
        framesPanel.ColumnCount = 1;
        framesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        framesPanel.RowCount = 4;
        framesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        framesPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        framesPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        framesPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        framesPanel.Dock = DockStyle.Fill;

        listBoxFrames.Dock = DockStyle.Fill;
        listBoxFrames.HorizontalScrollbar = true;
        listBoxFrames.IntegralHeight = false;
        listBoxFrames.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        listBoxFrames.SelectedIndexChanged += listBoxFrames_SelectedIndexChanged;
        listBoxFrames.MouseDown += listBoxFrames_MouseDown;
        listBoxFrames.KeyDown += listBoxFrames_KeyDown;
        splitMain.Panel1.Padding = new Padding(8);
        splitMain.Panel1.Controls.Add(framesPanel);
        framesPanel.Controls.Add(listBoxFrames, 0, 0);

        lblFrameLegend.Dock = DockStyle.Fill;
        lblFrameLegend.AutoSize = true;
        lblFrameLegend.Padding = new Padding(4, 8, 4, 0);
        lblFrameLegend.Text = "Black: anchor to place   |   Green: anchor placed   |   Red: discarded frame";
        framesPanel.Controls.Add(lblFrameLegend, 0, 1);

        lblStatus.AutoEllipsis = false;
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Text = "Prêt.";
        var helpLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false,
            Padding = new Padding(4, 8, 4, 0),
            Text = "Clic gauche = point fixe | molette = zoom | glisser = dÃ©placer"
        };
        framesPanel.Controls.Add(helpLabel, 0, 2);
        framesPanel.Controls.Add(lblStatus, 0, 3);

        menuToggleKeep.Text = "Discard / restore";
        menuToggleKeep.Click += menuToggleKeep_Click;
        contextMenuFrames.Items.Add(menuToggleKeep);
        listBoxFrames.ContextMenuStrip = contextMenuFrames;

        splitRight.Dock = DockStyle.Fill;
        splitRight.FixedPanel = FixedPanel.Panel2;
        splitRight.Panel1MinSize = 100;
        splitRight.Panel2MinSize = 100;
        splitMain.Panel2.Controls.Add(splitRight);

        imagePanel.ColumnCount = 1;
        imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        imagePanel.RowCount = 2;
        imagePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        imagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        imagePanel.Dock = DockStyle.Fill;
        imagePanel.Padding = new Padding(8);
        splitRight.Panel1.Controls.Add(imagePanel);

        imageHeaderPanel.Dock = DockStyle.Fill;
        imageHeaderPanel.Padding = new Padding(0, 0, 0, 8);
        imageHeaderPanel.Height = 42;
        imagePanel.Controls.Add(imageHeaderPanel, 0, 0);

        lblFrameInfo.Dock = DockStyle.Fill;
        lblFrameInfo.AutoEllipsis = true;
        lblFrameInfo.TextAlign = ContentAlignment.MiddleLeft;
        lblFrameInfo.Text = "Aucune frame sélectionnée";
        imageHeaderPanel.Controls.Add(lblFrameInfo);

        pictureBoxFrame.Dock = DockStyle.Fill;
        pictureBoxFrame.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxFrame.BackColor = Color.Black;
        pictureBoxFrame.TabStop = true;
        pictureBoxFrame.MouseDown += pictureBoxFrame_MouseDown;
        pictureBoxFrame.MouseMove += pictureBoxFrame_MouseMove;
        pictureBoxFrame.MouseUp += pictureBoxFrame_MouseUp;
        pictureBoxFrame.MouseWheel += pictureBoxFrame_MouseWheel;
        pictureBoxFrame.Paint += pictureBoxFrame_Paint;
        imagePanel.Controls.Add(pictureBoxFrame, 0, 1);

        splitRight.Panel2.Padding = new Padding(8);

        settingsLayout.ColumnCount = 1;
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsLayout.RowCount = 6;
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        settingsLayout.Dock = DockStyle.Fill;
        settingsLayout.AutoScroll = true;
        settingsLayout.Padding = new Padding(0, 0, 0, 8);
        splitRight.Panel2.Controls.Add(settingsLayout);

        groupNavigation.Text = "Navigation";
        groupNavigation.Dock = DockStyle.Top;
        groupNavigation.AutoSize = true;
        groupNavigation.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupNavigation.Padding = new Padding(10);
        settingsLayout.Controls.Add(groupNavigation, 0, 0);

        var navLayout = new TableLayoutPanel();
        navLayout.ColumnCount = 2;
        navLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        navLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        navLayout.RowCount = 4;
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        navLayout.Dock = DockStyle.Fill;
        navLayout.AutoSize = true;
        navLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        navLayout.Padding = new Padding(4);
        navLayout.Margin = new Padding(0);
        groupNavigation.Controls.Add(navLayout);

        ConfigureNavButton(btnPrev, "<");
        ConfigureNavButton(btnNext, ">");
        ConfigureNavButton(btnZoomIn, "Zoom in");
        ConfigureNavButton(btnZoomOut, "Zoom out");
        ConfigureNavButton(btnAutoAnchorOtherFrames, "Auto anchor other frames");
        ConfigureNavButton(btnToggleKeep, "Garder / écarter");

        btnPrev.Click += btnPrev_Click;
        btnNext.Click += btnNext_Click;
        btnZoomIn.Click += btnZoomIn_Click;
        btnZoomOut.Click += btnZoomOut_Click;
        btnAutoAnchorOtherFrames.Click += btnAutoAnchorOtherFrames_Click;
        btnToggleKeep.Click += btnToggleKeep_Click;

        navLayout.Controls.Add(btnPrev, 0, 0);
        navLayout.Controls.Add(btnNext, 1, 0);
        navLayout.Controls.Add(btnZoomIn, 0, 1);
        navLayout.Controls.Add(btnZoomOut, 1, 1);
        navLayout.Controls.Add(btnToggleKeep, 0, 2);
        navLayout.SetColumnSpan(btnToggleKeep, 2);
        navLayout.Controls.Add(btnAutoAnchorOtherFrames, 0, 3);
        navLayout.SetColumnSpan(btnAutoAnchorOtherFrames, 2);

        groupGif.Text = "Export";
        groupGif.Dock = DockStyle.Top;
        groupGif.AutoSize = true;
        groupGif.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupGif.Padding = new Padding(10);
        settingsLayout.Controls.Add(groupGif, 0, 1);

        var gifLayout = new TableLayoutPanel();
        gifLayout.ColumnCount = 1;
        gifLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gifLayout.RowCount = 1;
        gifLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        gifLayout.Dock = DockStyle.Top;
        gifLayout.AutoSize = true;
        gifLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupGif.Controls.Add(gifLayout);
        btnRenderAndExportGif.Text = "Apercu / export";
        btnRenderAndExportGif.Dock = DockStyle.Fill;
        btnRenderAndExportGif.Height = 34;
        btnRenderAndExportGif.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        btnRenderAndExportGif.BackColor = Color.Gainsboro;
        btnRenderAndExportGif.FlatStyle = FlatStyle.Flat;
        btnRenderAndExportGif.UseVisualStyleBackColor = false;
        btnRenderAndExportGif.Click += btnRenderAndExportGif_Click;
        gifLayout.Controls.Add(btnRenderAndExportGif, 0, 0);

        groupAdjustments.Text = "Image adjustments";
        groupAdjustments.Dock = DockStyle.Top;
        groupAdjustments.AutoSize = true;
        groupAdjustments.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupAdjustments.Padding = new Padding(10);
        settingsLayout.Controls.Add(groupAdjustments, 0, 2);

        var adjustmentsLayout = new TableLayoutPanel();
        adjustmentsLayout.ColumnCount = 3;
        adjustmentsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        adjustmentsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        adjustmentsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        adjustmentsLayout.RowCount = 8;
        for (int i = 0; i < 8; i++) adjustmentsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        adjustmentsLayout.Dock = DockStyle.Top;
        adjustmentsLayout.AutoSize = true;
        adjustmentsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupAdjustments.Controls.Add(adjustmentsLayout);

        ConfigureAdjustmentTrackBar(trackBrightness, -100, 100, 0);
        ConfigureAdjustmentTrackBar(trackContrast, -100, 100, 0);
        ConfigureAdjustmentTrackBar(trackSaturation, -100, 100, 0);
        ConfigureAdjustmentTrackBar(trackTemperature, -100, 100, 0);
        ConfigureAdjustmentTrackBar(trackSharpness, 0, 100, 0);
        ConfigureAdjustmentTrackBar(trackHighlights, -100, 100, 0);
        ConfigureAdjustmentTrackBar(trackShadows, -100, 100, 0);

        AddLabeledTrackBar(adjustmentsLayout, "Brightness", trackBrightness, txtBrightnessValue, 0);
        AddLabeledTrackBar(adjustmentsLayout, "Contrast", trackContrast, txtContrastValue, 1);
        AddLabeledTrackBar(adjustmentsLayout, "Saturation", trackSaturation, txtSaturationValue, 2);
        AddLabeledTrackBar(adjustmentsLayout, "Temperature", trackTemperature, txtTemperatureValue, 3);
        AddLabeledTrackBar(adjustmentsLayout, "Sharpness", trackSharpness, txtSharpnessValue, 4);
        AddLabeledTrackBar(adjustmentsLayout, "Highlights", trackHighlights, txtHighlightsValue, 5);
        AddLabeledTrackBar(adjustmentsLayout, "Shadows", trackShadows, txtShadowsValue, 6);

        btnResetAdjustments.Text = "Reset";
        btnResetAdjustments.Dock = DockStyle.Fill;
        btnResetAdjustments.AutoSize = true;
        btnResetAdjustments.Margin = new Padding(4, 8, 4, 4);
        adjustmentsLayout.Controls.Add(btnResetAdjustments, 0, 7);
        adjustmentsLayout.SetColumnSpan(btnResetAdjustments, 3);

        var filler = new Panel { Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(filler, 0, 3);

        statusLayout.ColumnCount = 1;
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        statusLayout.RowCount = 1;
        statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statusLayout.Dock = DockStyle.Fill;
        statusLayout.Padding = new Padding(8, 0, 8, 8);

        var helpLabelLegacy = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Text = "Clic gauche = point fixe | molette = zoom | glisser = déplacer"
        };
        statusLayout.Controls.Add(helpLabelLegacy, 0, 1);

        ((System.ComponentModel.ISupportInitialize)pictureBoxFrame).EndInit();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        splitMain.ResumeLayout(false);
        splitRight.Panel1.ResumeLayout(false);
        splitRight.Panel2.ResumeLayout(false);
        splitRight.ResumeLayout(false);
        groupNavigation.ResumeLayout(false);
        groupGif.ResumeLayout(false);
        groupAdjustments.ResumeLayout(false);
        imagePanel.ResumeLayout(false);
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        ResumeLayout(false);
    }

    private static void ConfigureAdjustmentTrackBar(TrackBar trackBar, int minimum, int maximum, int value)
    {
        trackBar.Minimum = minimum;
        trackBar.Maximum = maximum;
        trackBar.Value = value;
        trackBar.TickFrequency = 10;
        trackBar.SmallChange = 1;
        trackBar.LargeChange = 10;
        trackBar.AutoSize = false;
        trackBar.Height = 36;
        trackBar.Dock = DockStyle.Fill;
        trackBar.Margin = new Padding(4);
    }

    private static void AddLabeledTrackBar(TableLayoutPanel layout, string text, TrackBar trackBar, TextBox valueTextBox, int row)
    {
        valueTextBox.Anchor = AnchorStyles.Right;
        valueTextBox.Width = 64;
        valueTextBox.Margin = new Padding(4, 6, 4, 4);
        valueTextBox.TextAlign = HorizontalAlignment.Right;
        valueTextBox.Text = "0";

        layout.Controls.Add(new Label { Text = text, Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(4, 8, 8, 4) }, 0, row);
        layout.Controls.Add(trackBar, 1, row);
        layout.Controls.Add(valueTextBox, 2, row);
    }

    private static void ConfigureNavButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.AutoSize = false;
        button.MinimumSize = new Size(150, 48);
        button.Height = 48;
        button.Margin = new Padding(6);
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        button.UseVisualStyleBackColor = true;
    }
}



