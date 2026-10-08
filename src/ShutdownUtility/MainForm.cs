using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal sealed class MainForm : Form
{
    private readonly AppConfig Config;
    private NotifyIcon Tray;
    private ContextMenuStrip TrayMenu;
    private NumericUpDown CountdownBox;
    private NumericUpDown ScheduleMinutesBox;
    private ComboBox ScheduleActionBox;
    private CheckBox PlaySoundBox;
    private CheckBox WaitSoundBox;
    private CheckBox NotificationBox;
    private CheckBox HotkeyBox;
    private CheckBox TrayStartBox;
    private CheckBox StartWithWindowsBox;
    private CheckBox CloseToTrayBox;
    private CheckBox TestModeBox;
    private Label TestModeHintLabel;
    private Panel ModeBannerPanel;
    private TextBox SoundPathBox;
    private Label StatusLabel;
    private Label ModeLabel;
    private int HotkeyId = 9473;
    private bool Exiting;
    private const string DefaultStatus = "Ready  |  Choose an action to preview its countdown.";
    private static readonly Color SurfaceColor = Color.FromArgb(25, 29, 37);
    private static readonly Color BorderColor = Color.FromArgb(48, 55, 67);
    private static readonly Color AccentColor = Color.FromArgb(90, 170, 255);

    public MainForm(AppConfig config)
    {
        Config = config;
        InitializeUi();
        SetupTray();
        LoadWindowState();
        ApplyConfigToUi();
        RegisterHotkeyIfNeeded();
        FormClosing += OnFormClosing;
        Resize += OnResize;
    }

    private void InitializeUi()
    {
        Text = "Shutdown Utility Pro";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 760);
        ClientSize = new Size(1040, 800);
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.Sizable;
        Icon = AppResources.GetAppIcon();
        BackColor = Color.FromArgb(15, 18, 24);
        ForeColor = Color.FromArgb(235, 239, 245);

        Panel header = new Panel();
        header.Dock = DockStyle.Top;
        header.Height = 112;
        header.BackColor = Color.FromArgb(21, 25, 33);

        Label title = new Label();
        title.Text = "Shutdown Utility Pro";
        title.Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold);
        title.ForeColor = Color.FromArgb(245, 247, 250);
        title.Location = new Point(26, 10);
        title.AutoSize = true;
        header.Controls.Add(title);

        Label subtitle = new Label();
        ModeLabel = subtitle;
        UpdateModeIndicator();
        subtitle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        subtitle.Dock = DockStyle.Fill;
        subtitle.AutoSize = false;
        subtitle.TextAlign = ContentAlignment.MiddleLeft;
        subtitle.Padding = new Padding(8, 0, 8, 0);
        subtitle.AccessibleName = "Power action safety mode";
        subtitle.AccessibleRole = AccessibleRole.StatusBar;
        ModeBannerPanel = new Panel();
        ModeBannerPanel.Location = new Point(26, 65);
        ModeBannerPanel.Size = new Size(header.ClientSize.Width - 230, 30);
        ModeBannerPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        ModeBannerPanel.Controls.Add(subtitle);
        header.Controls.Add(ModeBannerPanel);
        UpdateModeIndicator();
        header.Resize += delegate
        {
            ModeBannerPanel.Width = Math.Max(220, header.ClientSize.Width - ModeBannerPanel.Left - 205);
        };

        Button hideButton = MakeButton("Hide to tray", ClientSize.Width - 166, 35, 140, 38);
        hideButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        hideButton.Click += delegate { HideToTray(true); };
        header.Controls.Add(hideButton);

        TableLayoutPanel root = new TableLayoutPanel();
        root.Dock = DockStyle.Fill;
        root.Padding = new Padding(18, 16, 18, 14);
        root.BackColor = BackColor;
        root.ColumnCount = 2;
        root.RowCount = 3;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49f));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 51f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 53f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 47f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        root.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
        Controls.Add(root);
        Controls.Add(header);

        Panel actionsBody;
        Panel actions = MakeCard("POWER ACTIONS", out actionsBody);
        TableLayoutPanel actionLayout = new TableLayoutPanel();
        actionLayout.Dock = DockStyle.Fill;
        actionLayout.BackColor = SurfaceColor;
        actionLayout.Padding = new Padding(2);
        actionLayout.ColumnCount = 3;
        actionLayout.RowCount = 3;
        for (int i = 0; i < 3; i++) actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        Label actionIntro = MakeHint("Every action opens a cancellable countdown.", true);
        actionLayout.Controls.Add(actionIntro, 0, 0);
        actionLayout.SetColumnSpan(actionIntro, 3);
        AddActionButton(actionLayout, "Shut down", PowerAction.Shutdown, 0, 1);
        AddActionButton(actionLayout, "Restart", PowerAction.Restart, 1, 1);
        AddActionButton(actionLayout, "Sleep", PowerAction.Sleep, 2, 1);
        AddActionButton(actionLayout, "Hibernate", PowerAction.Hibernate, 0, 2);
        AddActionButton(actionLayout, "Lock", PowerAction.Lock, 1, 2);
        actionsBody.Controls.Add(actionLayout);
        root.Controls.Add(actions, 0, 0);

        Panel settingsBody;
        Panel settings = MakeCard("COUNTDOWN & SOUND", out settingsBody);
        TableLayoutPanel st = new TableLayoutPanel();
        st.Dock = DockStyle.Fill;
        st.Padding = new Padding(4, 6, 4, 4);
        st.BackColor = SurfaceColor;
        st.ColumnCount = 2;
        st.RowCount = 6;
        st.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148f));
        st.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        st.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        AddLabel(st, "Countdown (seconds)", 0);
        CountdownBox = NewNumeric(1, 3600, 3);
        st.Controls.Add(CountdownBox, 1, 0);
        AddLabel(st, "Sound file", 1);
        TableLayoutPanel soundPanel = new TableLayoutPanel();
        soundPanel.Dock = DockStyle.Fill;
        soundPanel.ColumnCount = 3;
        soundPanel.RowCount = 1;
        soundPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        soundPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82f));
        soundPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
        soundPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        SoundPathBox = new TextBox(); SoundPathBox.Dock = DockStyle.Fill; StyleInput(SoundPathBox);
        Button browseSound = MakeButton("Browse", 0, 0, 78, 28); browseSound.Dock = DockStyle.Fill;
        Button testSound = MakeButton("Test", 0, 0, 56, 28); testSound.Dock = DockStyle.Fill;
        browseSound.Click += BrowseSound; testSound.Click += TestSound;
        soundPanel.Controls.Add(SoundPathBox, 0, 0);
        soundPanel.Controls.Add(browseSound, 1, 0);
        soundPanel.Controls.Add(testSound, 2, 0);
        st.Controls.Add(soundPanel, 1, 1);
        PlaySoundBox = NewCheck("Play sound", 2); st.Controls.Add(PlaySoundBox, 1, 2);
        WaitSoundBox = NewCheck("Wait for sound to finish", 3); st.Controls.Add(WaitSoundBox, 1, 3);
        NotificationBox = NewCheck("Show tray notifications", 4); st.Controls.Add(NotificationBox, 1, 4);
        Label hint = MakeHint("Action-specific WAV files are used automatically when present.", false);
        st.Controls.Add(hint, 1, 5);
        settingsBody.Controls.Add(st); root.Controls.Add(settings, 1, 0);

        Panel scheduleBody;
        Panel schedule = MakeCard("SCHEDULE", out scheduleBody);
        TableLayoutPanel sch = new TableLayoutPanel(); sch.Dock = DockStyle.Fill; sch.Padding = new Padding(4, 6, 4, 4); sch.BackColor = SurfaceColor; sch.ColumnCount = 3; sch.RowCount = 3;
        sch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92f)); sch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f)); sch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        sch.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f)); sch.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f)); sch.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Label al = MakeFieldLabel("Action"); sch.Controls.Add(al,0,0);
        ScheduleActionBox = new ComboBox(); ScheduleActionBox.DropDownStyle = ComboBoxStyle.DropDownList; ScheduleActionBox.Dock = DockStyle.Fill; ScheduleActionBox.Items.Add("Shut down"); ScheduleActionBox.Items.Add("Restart"); ScheduleActionBox.SelectedIndex = 0; StyleInput(ScheduleActionBox); sch.Controls.Add(ScheduleActionBox,1,0);
        Label dl = MakeFieldLabel("In minutes"); sch.Controls.Add(dl,0,1);
        ScheduleMinutesBox = NewNumeric(1, 10080, 10); ScheduleMinutesBox.Dock = DockStyle.Fill; StyleInput(ScheduleMinutesBox); sch.Controls.Add(ScheduleMinutesBox,1,1);
        Button scheduleButton = MakeButton("Schedule action", 0, 0, 140, 36); scheduleButton.Dock = DockStyle.Fill; scheduleButton.Click += ScheduleAction; StylePrimaryButton(scheduleButton); sch.Controls.Add(scheduleButton,2,0);
        Button cancelScheduleButton = MakeButton("Cancel schedule", 0, 0, 140, 36); cancelScheduleButton.Dock = DockStyle.Fill; cancelScheduleButton.Click += CancelScheduledAction; sch.Controls.Add(cancelScheduleButton,2,1);
        Label schHint = MakeHint("Uses the Windows timer. You can cancel it here or from the tray menu.", false); schHint.Dock = DockStyle.Top; sch.Controls.Add(schHint,0,2); sch.SetColumnSpan(schHint,3);
        scheduleBody.Controls.Add(sch); root.Controls.Add(schedule, 0, 1);

        Panel optionsBody;
        Panel options = MakeCard("SAFETY & CONVENIENCE", out optionsBody);
        TableLayoutPanel opt = new TableLayoutPanel(); opt.Dock = DockStyle.Fill; opt.Padding = new Padding(4, 2, 4, 2); opt.BackColor = SurfaceColor; opt.RowCount = 7; opt.ColumnCount = 1;
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
        TestModeBox = NewCheck("Test mode (simulate; no Windows actions)", 0);
        TestModeBox.AccessibleDescription = "When checked, power actions and schedules are simulated. Changes take effect after saving settings.";
        TestModeBox.CheckedChanged += UpdateTestModeHint;
        opt.Controls.Add(TestModeBox,0,0);
        TestModeHintLabel = new Label();
        TestModeHintLabel.Dock = DockStyle.Fill;
        TestModeHintLabel.AutoEllipsis = true;
        TestModeHintLabel.Padding = new Padding(24, 0, 4, 2);
        TestModeHintLabel.ForeColor = Color.FromArgb(255, 211, 110);
        opt.Controls.Add(TestModeHintLabel,0,1);
        HotkeyBox = NewCheck("Desktop-only hotkey (Ctrl+Alt+Shift+S)", 2); opt.Controls.Add(HotkeyBox,0,2);
        TrayStartBox = NewCheck("Start utility in the system tray", 3); opt.Controls.Add(TrayStartBox,0,3);
        CloseToTrayBox = NewCheck("Close button hides to tray instead of exiting", 4); opt.Controls.Add(CloseToTrayBox,0,4);
        StartWithWindowsBox = NewCheck("Start with Windows (tray mode)", 5); opt.Controls.Add(StartWithWindowsBox,0,5);
        FlowLayoutPanel settingsButtons = new FlowLayoutPanel();
        settingsButtons.Dock = DockStyle.Fill;
        settingsButtons.WrapContents = false;
        Button save = MakeButton("Save settings",0,0,122,32); save.Margin = new Padding(2); save.Click += SaveSettings; StylePrimaryButton(save); settingsButtons.Controls.Add(save);
        Button openFolder = MakeButton("Open data folder",0,0,132,32); openFolder.Margin = new Padding(2); openFolder.Click += delegate { OpenFolder(); }; settingsButtons.Controls.Add(openFolder);
        Button reset = MakeButton("Reset",0,0,82,32); reset.Margin = new Padding(2); reset.Click += ResetSettings; settingsButtons.Controls.Add(reset);
        opt.Controls.Add(settingsButtons,0,6);
        optionsBody.Controls.Add(opt); root.Controls.Add(options,1,1);

        StatusLabel = new Label();
        StatusLabel.Text = DefaultStatus;
        StatusLabel.Dock = DockStyle.Fill;
        StatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        StatusLabel.ForeColor = Color.FromArgb(183, 194, 209);
        StatusLabel.Padding = new Padding(12, 0, 12, 0);
        StatusLabel.BackColor = Color.FromArgb(21, 25, 33);
        root.Controls.Add(StatusLabel,0,2); root.SetColumnSpan(StatusLabel,2);
    }

    private void AddActionButton(TableLayoutPanel grid, string text, PowerAction action, int column, int row)
    {
        Button b = new Button();
        b.Text = text;
        b.Tag = action;
        b.Dock = DockStyle.Fill;
        b.Margin = new Padding(5);
        b.Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);
        b.AccessibleName = text + " action";
        b.AccessibleDescription = "Starts a cancellable countdown before the selected action.";
        if (action == PowerAction.Shutdown) StylePrimaryButton(b);
        else StyleButton(b);
        b.Click += delegate { BeginAction((PowerAction)b.Tag); };
        grid.Controls.Add(b, column, row);
    }

    private void BeginAction(PowerAction action)
    {
        int seconds = (int)CountdownBox.Value;
        Logger.Write("Requested action: " + action + ", countdown=" + seconds);
        using (CountdownForm f = new CountdownForm(Config, action, seconds))
        {
            f.ShowDialog(this);
        }
        StatusLabel.Text = "Ready. Last action: " + action + ".";
    }

    private void ScheduleAction(object sender, EventArgs e)
    {
        PowerAction action = ScheduleActionBox.SelectedIndex == 1 ? PowerAction.Restart : PowerAction.Shutdown;
        int minutes = (int)ScheduleMinutesBox.Value;
        int seconds = minutes * 60;
        if (Config.ConfirmScheduledActions)
        {
            DialogResult dr = MessageBox.Show(this,
                String.Format("Schedule {0} in {1} minute{2}?\r\n\r\nThis uses Windows' native timer and can be cancelled later.",
                    action == PowerAction.Shutdown ? "shutdown" : "restart", minutes, minutes == 1 ? "" : "s"),
                "Confirm schedule", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes) return;
        }
        bool simulated;
        string error;
        if (!PowerController.Schedule(action, seconds, Config.TestMode, out simulated, out error))
        {
            MessageBox.Show(this, error, "Schedule failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Logger.Write("Schedule failed: " + error); return;
        }
        if (simulated)
        {
            Logger.Write("TEST MODE prevented schedule: " + action + " in " + seconds + " seconds.");
            StatusLabel.Text = "TEST MODE: schedule was simulated; no schedule was created.";
            Notify("Shutdown Utility Pro", "Test mode is enabled; no schedule was created.");
            return;
        }
        Logger.Write("Scheduled " + action + " in " + seconds + " seconds.");
        StatusLabel.Text = String.Format("Scheduled {0} in {1} minute{2}.", action == PowerAction.Shutdown ? "shutdown" : "restart", minutes, minutes == 1 ? "" : "s");
        Notify("Shutdown Utility Pro", StatusLabel.Text);
    }

    private void CancelScheduledAction(object sender, EventArgs e)
    {
        string error;
        if (!PowerController.CancelScheduled(out error))
        {
            MessageBox.Show(this, "No scheduled shutdown/restart was cancelled, or Windows rejected the request.\r\n\r\n" + error,
                "Cancel schedule", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Logger.Write("Cancel schedule result: " + error); return;
        }
        Logger.Write("Cancelled native scheduled power action.");
        StatusLabel.Text = "Any pending Windows shutdown/restart timer was cancelled.";
        Notify("Shutdown Utility Pro", StatusLabel.Text);
    }

    private void BrowseSound(object sender, EventArgs e)
    {
        using (OpenFileDialog dlg = new OpenFileDialog())
        {
            dlg.Filter = "WAV audio (*.wav)|*.wav|All files (*.*)|*.*";
            dlg.Title = "Choose shutdown sound";
            if (dlg.ShowDialog(this) == DialogResult.OK) SoundPathBox.Text = dlg.FileName;
        }
    }

    private void TestSound(object sender, EventArgs e)
    {
        string path = SoundPathBox.Text.Trim();
        if (!Path.IsPathRooted(path)) path = Path.Combine(AppResources.BaseDir, path);
        if (!File.Exists(path)) { MessageBox.Show(this, "Sound file was not found:\r\n" + path, "Test sound", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        try { using (SoundPlayer p = new SoundPlayer(path)) { p.Load(); p.PlaySync(); } }
        catch (Exception ex) { MessageBox.Show(this, "Could not play the WAV file.\r\n\r\n" + ex.Message, "Test sound", MessageBoxButtons.OK, MessageBoxIcon.Error); Logger.Write("Test sound failed: " + ex); }
    }

    private void ApplyConfigToUi()
    {
        CountdownBox.Value = Math.Max(1, Math.Min(3600, Config.CountdownSeconds));
        SoundPathBox.Text = Config.SoundFile;
        PlaySoundBox.Checked = Config.PlaySound;
        WaitSoundBox.Checked = Config.WaitForSound;
        NotificationBox.Checked = Config.ShowNotification;
        HotkeyBox.Checked = Config.EnableDesktopOnlyHotkey;
        TestModeBox.Checked = Config.TestMode;
        TestModeBox.Enabled = !Config.ForceTestMode;
        TestModeBox.Text = Config.ForceTestMode
            ? "Test mode (forced for this run)"
            : "Test mode (simulate; no Windows actions)";
        UpdateTestModeHint(null, EventArgs.Empty);
        TrayStartBox.Checked = Config.StartInTray;
        CloseToTrayBox.Checked = Config.CloseToTray;
        StartWithWindowsBox.Checked = Config.StartWithWindows || StartupManager.IsEnabled();
        ScheduleMinutesBox.Value = Math.Max(1, Math.Min(10080, Config.ScheduledDefaultMinutes));
    }

    private void UpdateTestModeHint(object sender, EventArgs e)
    {
        if (TestModeHintLabel == null || TestModeBox == null) return;
        if (Config.ForceTestMode)
        {
            TestModeHintLabel.Text = "Safe simulation is forced for this run and cannot be turned off here.";
            TestModeHintLabel.ForeColor = Color.Gold;
        }
        else if (TestModeBox.Checked && !Config.TestMode)
        {
            TestModeHintLabel.Text = "Safe mode is not active yet. Click Save settings to apply it.";
            TestModeHintLabel.ForeColor = Color.Gold;
        }
        else if (!TestModeBox.Checked && Config.TestMode)
        {
            TestModeHintLabel.Text = "To enable real actions, click Save settings and confirm the warning.";
            TestModeHintLabel.ForeColor = Color.Orange;
        }
        else if (Config.TestMode)
        {
            TestModeHintLabel.Text = "Safe mode is active. Uncheck this and save to enable real actions.";
            TestModeHintLabel.ForeColor = Color.Gold;
        }
        else
        {
            TestModeHintLabel.Text = "Real actions are enabled. Check this and save to return to safe mode.";
            TestModeHintLabel.ForeColor = Color.LightSalmon;
        }
    }

    private void SaveSettings(object sender, EventArgs e)
    {
        if (!Config.ForceTestMode && Config.TestMode && !TestModeBox.Checked)
        {
            DialogResult confirmation = MessageBox.Show(this,
                "Turn off Test mode?\r\n\r\nFuture power actions and schedules will affect Windows. Continue?",
                "Enable real power actions", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmation != DialogResult.Yes)
            {
                TestModeBox.Checked = true;
                return;
            }
        }

        bool previousTestMode = Config.TestMode;
        bool settingsSaved = false;
        try
        {
            Config.CountdownSeconds = (int)CountdownBox.Value;
            Config.SoundFile = SoundPathBox.Text.Trim();
            Config.PlaySound = PlaySoundBox.Checked;
            Config.WaitForSound = WaitSoundBox.Checked;
            Config.ShowNotification = NotificationBox.Checked;
            Config.EnableDesktopOnlyHotkey = HotkeyBox.Checked;
            Config.StartInTray = TrayStartBox.Checked;
            Config.CloseToTray = CloseToTrayBox.Checked;
            Config.StartWithWindows = StartWithWindowsBox.Checked;
            Config.TestMode = Config.ForceTestMode || TestModeBox.Checked;
            Config.ScheduledDefaultMinutes = (int)ScheduleMinutesBox.Value;
            Config.Save(AppResources.ConfigPath);
            settingsSaved = true;
            SetStartup(Config.StartWithWindows);
            RegisterHotkeyIfNeeded(true);
            UpdateModeIndicator();
            UpdateTestModeHint(null, EventArgs.Empty);
            StatusLabel.Text = "Settings saved.";
            Notify("Shutdown Utility Pro", "Settings saved successfully.");
        }
        catch (Exception ex)
        {
            if (!settingsSaved)
            {
                Config.TestMode = previousTestMode;
                TestModeBox.Checked = previousTestMode;
                UpdateModeIndicator();
                UpdateTestModeHint(null, EventArgs.Empty);
            }
            Logger.Write("Save settings failed: " + ex);
            MessageBox.Show(this, "Could not save settings.\r\n\r\n" + ex.Message, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetSettings(object sender, EventArgs e)
    {
        DialogResult dr = MessageBox.Show(this,
            "Reset the dashboard settings to safe defaults?\r\n\r\nThis will not delete your sound or icon files.",
            "Reset settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (dr != DialogResult.Yes) return;

        AppConfig fresh = new AppConfig();
        Config.CountdownSeconds = fresh.CountdownSeconds;
        Config.SoundFile = fresh.SoundFile;
        Config.PlaySound = fresh.PlaySound;
        Config.WaitForSound = fresh.WaitForSound;
        Config.ShowNotification = fresh.ShowNotification;
        Config.EnableDesktopOnlyHotkey = fresh.EnableDesktopOnlyHotkey;
        Config.Hotkey = fresh.Hotkey;
        Config.StartInTray = fresh.StartInTray;
        Config.MinimizeToTray = fresh.MinimizeToTray;
        Config.CloseToTray = fresh.CloseToTray;
        Config.StartWithWindows = fresh.StartWithWindows;
        Config.ConfirmScheduledActions = fresh.ConfirmScheduledActions;
        Config.RememberWindowPosition = fresh.RememberWindowPosition;
        Config.DarkTheme = fresh.DarkTheme;
        Config.TestMode = fresh.TestMode;
        Config.ScheduledDefaultMinutes = fresh.ScheduledDefaultMinutes;
        ApplyConfigToUi();
        try { Config.Save(AppResources.ConfigPath); SetStartup(false); RegisterHotkeyIfNeeded(true); UpdateModeIndicator(); StatusLabel.Text = "Safe defaults restored."; }
        catch (Exception ex)
        {
            Logger.Write("Reset settings failed: " + ex);
            MessageBox.Show(this, "Could not restore safe defaults.\r\n\r\n" + ex.Message,
                "Reset settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStartup(bool enabled)
    {
        try
        {
            StartupManager.SetEnabled(enabled, Application.ExecutablePath);
            Logger.Write("Start with Windows=" + enabled);
        }
        catch (Exception ex)
        {
            Logger.Write("Startup setting failed: " + ex);
            MessageBox.Show(this, "Could not change Start with Windows.\r\n\r\n" + ex.Message, "Startup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetupTray()
    {
        Tray = new NotifyIcon();
        Tray.Icon = AppResources.GetAppIcon();
        Tray.Text = Config.TestMode ? "Shutdown Utility Pro - TEST MODE" : "Shutdown Utility Pro";
        Tray.Visible = true;
        Tray.DoubleClick += delegate { ShowFromTray(); };
        Tray.BalloonTipClicked += delegate { ShowFromTray(); };

        TrayMenu = new ContextMenuStrip();
        TrayMenu.Items.Add("Open dashboard", null, delegate { ShowFromTray(); });
        TrayMenu.Items.Add(new ToolStripSeparator());
        TrayMenu.Items.Add("Shut down…", null, delegate { RunFromTray(PowerAction.Shutdown); });
        TrayMenu.Items.Add("Restart…", null, delegate { RunFromTray(PowerAction.Restart); });
        TrayMenu.Items.Add("Sleep…", null, delegate { RunFromTray(PowerAction.Sleep); });
        TrayMenu.Items.Add("Hibernate…", null, delegate { RunFromTray(PowerAction.Hibernate); });
        TrayMenu.Items.Add("Lock…", null, delegate { RunFromTray(PowerAction.Lock); });
        TrayMenu.Items.Add(new ToolStripSeparator());
        TrayMenu.Items.Add("Schedule shutdown 30 sec", null, delegate { ScheduleQuick(PowerAction.Shutdown, 30); });
        TrayMenu.Items.Add("Schedule shutdown 5 min", null, delegate { ScheduleQuick(PowerAction.Shutdown, 300); });
        TrayMenu.Items.Add("Schedule shutdown 10 min", null, delegate { ScheduleQuick(PowerAction.Shutdown, 600); });
        TrayMenu.Items.Add("Schedule restart 10 min", null, delegate { ScheduleQuick(PowerAction.Restart, 600); });
        TrayMenu.Items.Add("Cancel scheduled power action", null, delegate { CancelScheduledAction(null, EventArgs.Empty); });
        TrayMenu.Items.Add(new ToolStripSeparator());
        TrayMenu.Items.Add("Test sound", null, delegate { TestSound(null, EventArgs.Empty); });
        TrayMenu.Items.Add("Open folder", null, delegate { OpenFolder(); });
        TrayMenu.Items.Add("View log", null, delegate { OpenLog(); });
        TrayMenu.Items.Add("About", null, delegate { ShowAbout(); });
        TrayMenu.Items.Add(new ToolStripSeparator());
        TrayMenu.Items.Add("Exit", null, delegate { Exiting = true; Application.Exit(); });
        Tray.ContextMenuStrip = TrayMenu;
    }

    private void RunFromTray(PowerAction action)
    {
        ShowFromTray();
        BeginAction(action);
    }

    private void ScheduleQuick(PowerAction action, int seconds)
    {
        bool simulated;
        string error;
        if (!PowerController.Schedule(action, seconds, Config.TestMode, out simulated, out error)) { Notify("Shutdown Utility Pro", error); return; }
        if (simulated) { Notify("Shutdown Utility Pro", "Test mode is enabled; no schedule was created."); Logger.Write("TEST MODE prevented tray schedule: " + action + " in " + seconds + " seconds."); return; }
        string message = String.Format("Scheduled {0} in {1} seconds.", action == PowerAction.Shutdown ? "shutdown" : "restart", seconds);
        Logger.Write(message); Notify("Shutdown Utility Pro", message);
    }

    private void RegisterHotkeyIfNeeded()
    {
        RegisterHotkeyIfNeeded(false);
    }

    private void RegisterHotkeyIfNeeded(bool forceRefresh)
    {
        if (forceRefresh && HotkeyId != 0) { try { NativeMethods.UnregisterHotKey(Handle, HotkeyId); } catch { } }
        if (!Config.EnableDesktopOnlyHotkey) return;
        uint mods, key;
        if (!HotkeyParser.TryParse(Config.Hotkey, out mods, out key))
        {
            Logger.Write("Invalid hotkey: " + Config.Hotkey);
            return;
        }
        if (!NativeMethods.RegisterHotKey(Handle, HotkeyId, mods, key))
        {
            int err = Marshal.GetLastWin32Error();
            Logger.Write("Could not register hotkey " + Config.Hotkey + ", error=" + err);
        }
        else Logger.Write("Registered desktop-only hotkey " + Config.Hotkey);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            if (NativeMethods.IsDesktopForeground()) BeginAction(PowerAction.Shutdown);
            else Logger.Write("Ignored hotkey because desktop was not foreground.");
        }
        base.WndProc(ref m);
    }

    private void OnFormClosing(object sender, FormClosingEventArgs e)
    {
        if (Exiting) return;
        if (Config.CloseToTray)
        {
            e.Cancel = true;
            HideToTray(true);
        }
        else CleanupTray();
    }

    private void HideToTray(bool notify)
    {
        Hide();
        if (notify) Notify("Shutdown Utility Pro", "Running in the system tray.");
    }

    private void ShowFromTray()
    {
        Show(); WindowState = FormWindowState.Normal; Activate(); BringToFront();
    }

    private void Notify(string title, string text)
    {
        if (Config.ShowNotification && Tray != null)
        {
            try { Tray.ShowBalloonTip(2500, title, text, ToolTipIcon.Info); } catch { }
        }
    }

    private void OnResize(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized && Config.MinimizeToTray)
        {
            BeginInvoke((MethodInvoker)delegate { HideToTray(false); });
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(this,
            "Shutdown Utility Pro 3.1.1\r\n\r\n" +
            (Config.TestMode ? "TEST MODE is enabled; no Windows power actions will run.\r\n" : "Windows power controls are enabled.\r\n") +
            "No forced application termination is used by default.\r\n\r\n" +
            "Folder:\r\n" + AppResources.BaseDir,
            "About Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void CleanupTray()
    {
        if (HotkeyId != 0) { try { NativeMethods.UnregisterHotKey(Handle, HotkeyId); } catch { } }
        if (Tray != null) { Tray.Visible = false; Tray.Dispose(); Tray = null; }
    }

    private void OpenFolder()
    {
        try { Process.Start("explorer.exe", "\"" + AppResources.BaseDir + "\""); }
        catch (Exception ex) { Logger.Write("Open folder failed: " + ex); }
    }

    private void OpenLog()
    {
        try
        {
            if (!File.Exists(Logger.PathName)) File.WriteAllText(Logger.PathName, "");
            Process.Start("notepad.exe", "\"" + Logger.PathName + "\"");
        }
        catch (Exception ex) { Logger.Write("Open log failed: " + ex); }
    }

    private void LoadWindowState()
    {
        if (!Config.RememberWindowPosition) return;
        string path = AppResources.WindowStatePath;
        try
        {
            if (!File.Exists(path)) return;
            string[] p = File.ReadAllText(path).Split(',');
            if (p.Length != 4) return;
            int x, y, w, h;
            if (!Int32.TryParse(p[0], out x) || !Int32.TryParse(p[1], out y) || !Int32.TryParse(p[2], out w) || !Int32.TryParse(p[3], out h)) return;
            Rectangle r = new Rectangle(x, y, Math.Max(MinimumSize.Width, w), Math.Max(MinimumSize.Height, h));
            foreach (Screen s in Screen.AllScreens) if (s.WorkingArea.IntersectsWith(r)) { Bounds = r; break; }
        }
        catch { }
    }

    private void SaveWindowState()
    {
        if (!Config.RememberWindowPosition) return;
        try { File.WriteAllText(AppResources.WindowStatePath, String.Join(",", new string[] { Left.ToString(), Top.ToString(), Width.ToString(), Height.ToString() })); }
        catch { }
    }

    private static Panel MakeCard(string title, out Panel body)
    {
        Panel card = new Panel();
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(7);
        card.Padding = new Padding(1);
        card.BackColor = BorderColor;

        TableLayoutPanel layout = new TableLayoutPanel();
        layout.Dock = DockStyle.Fill;
        layout.BackColor = SurfaceColor;
        layout.ColumnCount = 1;
        layout.RowCount = 2;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        Label heading = new Label();
        heading.Text = title;
        heading.Dock = DockStyle.Fill;
        heading.TextAlign = ContentAlignment.MiddleLeft;
        heading.Padding = new Padding(14, 0, 8, 0);
        heading.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        heading.ForeColor = Color.FromArgb(155, 176, 202);
        layout.Controls.Add(heading, 0, 0);

        body = new Panel();
        body.Dock = DockStyle.Fill;
        body.Padding = new Padding(12, 4, 12, 10);
        body.BackColor = SurfaceColor;
        layout.Controls.Add(body, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Label MakeHint(string text, bool prominent)
    {
        Label label = new Label();
        label.Text = text;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoEllipsis = true;
        label.ForeColor = prominent ? Color.FromArgb(196, 207, 222) : Color.FromArgb(150, 163, 181);
        label.Font = new Font("Segoe UI", prominent ? 9.5f : 9f, FontStyle.Regular);
        return label;
    }

    private static Label MakeFieldLabel(string text)
    {
        Label label = new Label();
        label.Text = text;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.ForeColor = Color.FromArgb(190, 200, 214);
        label.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        return label;
    }

    private static Button MakeButton(string text, int x, int y, int w, int h)
    {
        Button b = new Button();
        b.Text = text;
        b.Location = new Point(x,y);
        b.Size = new Size(w,h);
        StyleButton(b);
        return b;
    }

    private static void StyleButton(Button button)
    {
        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = BorderColor;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 52, 67);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(35, 69, 105);
        button.BackColor = Color.FromArgb(31, 37, 47);
        button.ForeColor = Color.FromArgb(231, 237, 245);
        button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(5, 2, 5, 2);
    }

    private static void StylePrimaryButton(Button button)
    {
        StyleButton(button);
        button.BackColor = Color.FromArgb(35, 103, 167);
        button.FlatAppearance.BorderColor = Color.FromArgb(58, 134, 205);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 122, 193);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(31, 88, 145);
        button.ForeColor = Color.White;
    }

    private static void StyleInput(Control control)
    {
        control.BackColor = Color.FromArgb(17, 21, 28);
        control.ForeColor = Color.FromArgb(235, 239, 245);
        control.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        if (control is ComboBox) ((ComboBox)control).FlatStyle = FlatStyle.Flat;
        if (control is TextBox) ((TextBox)control).BorderStyle = BorderStyle.FixedSingle;
    }

    private static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.BackColor = SurfaceColor;
        checkBox.ForeColor = Color.FromArgb(218, 225, 235);
        checkBox.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        checkBox.Cursor = Cursors.Hand;
    }

    private static NumericUpDown NewNumeric(int min, int max, int value)
    {
        NumericUpDown n = new NumericUpDown();
        n.Minimum = min;
        n.Maximum = max;
        n.Value = value;
        n.Dock = DockStyle.Left;
        n.Width = 140;
        n.ThousandsSeparator = true;
        StyleInput(n);
        return n;
    }

    private static CheckBox NewCheck(string text, int row)
    {
        CheckBox c = new CheckBox();
        c.Text = text;
        c.AutoSize = true;
        c.Margin = new Padding(3,3,3,1);
        StyleCheckBox(c);
        return c;
    }

    private static void AddLabel(TableLayoutPanel t, string text, int row)
    {
        Label l = MakeFieldLabel(text);
        t.Controls.Add(l, 0, row);
    }

    private void UpdateModeIndicator()
    {
        if (ModeLabel == null) return;
        ModeLabel.Text = Config.TestMode
            ? (Config.ForceTestMode ? "TEST MODE  |  Forced for this run; actions are simulated" : "TEST MODE ON  |  Actions are simulated; Windows is unchanged")
            : "REAL ACTIONS ON  |  Power controls can affect Windows";
        ModeLabel.ForeColor = Config.TestMode ? Color.FromArgb(255, 220, 120) : Color.FromArgb(255, 190, 170);
        if (ModeBannerPanel != null)
            ModeBannerPanel.BackColor = Config.TestMode ? Color.FromArgb(76, 61, 24) : Color.FromArgb(83, 41, 35);
        if (Tray != null) Tray.Text = Config.TestMode ? "Shutdown Utility Pro - TEST MODE" : "Shutdown Utility Pro";
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (Config.StartInTray) BeginInvoke((MethodInvoker)delegate { HideToTray(false); });
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SaveWindowState(); CleanupTray(); base.OnFormClosed(e);
    }
}
