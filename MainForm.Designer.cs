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
    private Button btnRenderAndExportGif = null!;
    private Button btnSaveProject = null!;
    private Button btnLoadProject = null!;

    private ListBox listBoxFrames = null!;
    private PictureBox pictureBoxFrame = null!;

    private Label lblStatus = null!;
    private Label lblFrameInfo = null!;
    private Label lblFrameLegend = null!;
    private ContextMenuStrip contextMenuFrames = null!;
    private ToolStripMenuItem menuToggleKeep = null!;

    private NumericUpDown numGifDelay = null!;
    private NumericUpDown numCropX = null!;
    private NumericUpDown numCropY = null!;
    private NumericUpDown numCropW = null!;
    private NumericUpDown numCropH = null!;
    private NumericUpDown numTargetX = null!;
    private NumericUpDown numTargetY = null!;

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
    private GroupBox groupCrop = null!;
    private GroupBox groupTarget = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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
        btnRenderAndExportGif = new Button();
        btnSaveProject = new Button();
        btnLoadProject = new Button();

        listBoxFrames = new ListBox();
        pictureBoxFrame = new PictureBox();

        lblStatus = new Label();
        lblFrameInfo = new Label();
        lblFrameLegend = new Label();
        contextMenuFrames = new ContextMenuStrip(components);
        menuToggleKeep = new ToolStripMenuItem();

        numGifDelay = new NumericUpDown();
        numCropX = new NumericUpDown();
        numCropY = new NumericUpDown();
        numCropW = new NumericUpDown();
        numCropH = new NumericUpDown();
        numTargetX = new NumericUpDown();
        numTargetY = new NumericUpDown();

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
        groupCrop = new GroupBox();
        groupTarget = new GroupBox();

        ((System.ComponentModel.ISupportInitialize)pictureBoxFrame).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numGifDelay).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numCropX).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numCropY).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numCropW).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numCropH).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numTargetX).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numTargetY).BeginInit();
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
        groupCrop.SuspendLayout();
        groupTarget.SuspendLayout();
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
        rootLayout.Controls.Add(topBar, 0, 0);

        btnOpenInput.Text = "Ouvrir";
        btnOpenInput.AutoSize = true;
        btnOpenInput.Click += btnOpenInput_Click;

        btnSaveProject.Text = "Sauver projet";
        btnSaveProject.AutoSize = true;
        btnSaveProject.Click += btnSaveProject_Click;

        btnLoadProject.Text = "Charger projet";
        btnLoadProject.AutoSize = true;
        btnLoadProject.Click += btnLoadProject_Click;

        splitMain.Dock = DockStyle.Fill;
        splitMain.FixedPanel = FixedPanel.Panel1;
        splitMain.Panel1MinSize = 100;
        splitMain.Panel2MinSize = 100;
        rootLayout.Controls.Add(splitMain, 0, 1);

        var framesPanel = new TableLayoutPanel();
        framesPanel.ColumnCount = 1;
        framesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        framesPanel.RowCount = 2;
        framesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
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
        lblFrameLegend.Text = "Noir: point a placer   |   Vert: point place   |   Rouge: frame ecartee";
        framesPanel.Controls.Add(lblFrameLegend, 0, 1);

        menuToggleKeep.Text = "Ecarter / Reintegrer";
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
        settingsLayout.RowCount = 5;
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
        navLayout.RowCount = 3;
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
        ConfigureNavButton(btnZoomIn, "Zoom +");
        ConfigureNavButton(btnZoomOut, "Zoom -");
        ConfigureNavButton(btnToggleKeep, "Garder / écarter");

        btnPrev.Click += btnPrev_Click;
        btnNext.Click += btnNext_Click;
        btnZoomIn.Click += btnZoomIn_Click;
        btnZoomOut.Click += btnZoomOut_Click;
        btnToggleKeep.Click += btnToggleKeep_Click;

        navLayout.Controls.Add(btnPrev, 0, 0);
        navLayout.Controls.Add(btnNext, 1, 0);
        navLayout.Controls.Add(btnZoomIn, 0, 1);
        navLayout.Controls.Add(btnZoomOut, 1, 1);
        navLayout.Controls.Add(btnToggleKeep, 0, 2);
        navLayout.SetColumnSpan(btnToggleKeep, 2);

        groupGif.Text = "GIF";
        groupGif.Dock = DockStyle.Top;
        groupGif.AutoSize = true;
        groupGif.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupGif.Padding = new Padding(10);
        settingsLayout.Controls.Add(groupGif, 0, 1);

        var gifLayout = new TableLayoutPanel();
        gifLayout.ColumnCount = 2;
        gifLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        gifLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        gifLayout.RowCount = 2;
        gifLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        gifLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        gifLayout.Dock = DockStyle.Top;
        gifLayout.AutoSize = true;
        gifLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupGif.Controls.Add(gifLayout);

        gifLayout.Controls.Add(new Label { Text = "Delay (centièmes)", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
        numGifDelay.Dock = DockStyle.Fill;
        numGifDelay.Minimum = 1;
        numGifDelay.Maximum = 500;
        numGifDelay.Value = 5;
        gifLayout.Controls.Add(numGifDelay, 1, 0);

        btnRenderAndExportGif.Text = "Tester le réalignement";
        btnRenderAndExportGif.Dock = DockStyle.Fill;
        btnRenderAndExportGif.Height = 34;
        btnRenderAndExportGif.Click += btnRenderAndExportGif_Click;
        gifLayout.SetColumnSpan(btnRenderAndExportGif, 2);
        gifLayout.Controls.Add(btnRenderAndExportGif, 0, 1);

        groupCrop.Text = "Crop";
        groupCrop.Dock = DockStyle.Top;
        groupCrop.AutoSize = true;
        groupCrop.Padding = new Padding(10);
        settingsLayout.Controls.Add(groupCrop, 0, 3);

        var cropLayout = new TableLayoutPanel();
        cropLayout.ColumnCount = 2;
        cropLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        cropLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        cropLayout.RowCount = 4;
        for (int i = 0; i < 4; i++) cropLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cropLayout.Dock = DockStyle.Top;
        cropLayout.AutoSize = true;
        groupCrop.Controls.Add(cropLayout);

        ConfigureNumeric(numCropX, 0, 10000, 0);
        ConfigureNumeric(numCropY, 0, 10000, 0);
        ConfigureNumeric(numCropW, 1, 10000, 300);
        ConfigureNumeric(numCropH, 1, 10000, 300);

        AddLabeledNumeric(cropLayout, "Crop X", numCropX, 0);
        AddLabeledNumeric(cropLayout, "Crop Y", numCropY, 1);
        AddLabeledNumeric(cropLayout, "Crop W", numCropW, 2);
        AddLabeledNumeric(cropLayout, "Crop H", numCropH, 3);

        groupTarget.Text = "Point cible";
        groupTarget.Dock = DockStyle.Top;
        groupTarget.AutoSize = true;
        groupTarget.Padding = new Padding(10);
        settingsLayout.Controls.Add(groupTarget, 0, 4);

        var targetLayout = new TableLayoutPanel();
        targetLayout.ColumnCount = 2;
        targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        targetLayout.RowCount = 2;
        targetLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        targetLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        targetLayout.Dock = DockStyle.Top;
        targetLayout.AutoSize = true;
        groupTarget.Controls.Add(targetLayout);

        ConfigureNumeric(numTargetX, 0, 10000, 150);
        ConfigureNumeric(numTargetY, 0, 10000, 150);

        AddLabeledNumeric(targetLayout, "Target X", numTargetX, 0);
        AddLabeledNumeric(targetLayout, "Target Y", numTargetY, 1);

        var filler = new Panel { Dock = DockStyle.Fill };
        settingsLayout.Controls.Add(filler, 0, 2);

        statusLayout.ColumnCount = 2;
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        statusLayout.Dock = DockStyle.Fill;
        statusLayout.Padding = new Padding(8, 0, 8, 8);
        rootLayout.Controls.Add(statusLayout, 0, 2);

        lblStatus.AutoEllipsis = true;
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Text = "Prêt.";
        statusLayout.Controls.Add(lblStatus, 0, 0);

        var helpLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Text = "Clic gauche = point fixe | molette = zoom | glisser = déplacer"
        };
        statusLayout.Controls.Add(helpLabel, 1, 0);

        ((System.ComponentModel.ISupportInitialize)pictureBoxFrame).EndInit();
        ((System.ComponentModel.ISupportInitialize)numGifDelay).EndInit();
        ((System.ComponentModel.ISupportInitialize)numCropX).EndInit();
        ((System.ComponentModel.ISupportInitialize)numCropY).EndInit();
        ((System.ComponentModel.ISupportInitialize)numCropW).EndInit();
        ((System.ComponentModel.ISupportInitialize)numCropH).EndInit();
        ((System.ComponentModel.ISupportInitialize)numTargetX).EndInit();
        ((System.ComponentModel.ISupportInitialize)numTargetY).EndInit();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        splitMain.ResumeLayout(false);
        splitRight.Panel1.ResumeLayout(false);
        splitRight.Panel2.ResumeLayout(false);
        splitRight.ResumeLayout(false);
        groupNavigation.ResumeLayout(false);
        groupGif.ResumeLayout(false);
        groupCrop.ResumeLayout(false);
        groupTarget.ResumeLayout(false);
        imagePanel.ResumeLayout(false);
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        ResumeLayout(false);
    }

    private static void ConfigureNumeric(NumericUpDown control, decimal min, decimal max, decimal value)
    {
        control.Minimum = min;
        control.Maximum = max;
        control.Value = value;
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(4);
    }

    private static void AddLabeledNumeric(TableLayoutPanel layout, string text, NumericUpDown numeric, int row)
    {
        layout.Controls.Add(new Label { Text = text, Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(4, 8, 4, 4) }, 0, row);
        layout.Controls.Add(numeric, 1, row);
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
