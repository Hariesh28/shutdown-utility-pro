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
    private TextBox SoundPathBox;
    private Label StatusLabel;
    private Label ModeLabel;
    private int HotkeyId = 9473;
    private bool Exiting;
    private const string DefaultStatus = "Ready. Choose an action to start a cancellable countdown.";

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
        MinimumSize = new Size(900, 650);
        ClientSize = new Size(920, 700);
        FormBorderStyle = FormBorderStyle.Sizable;
        Icon = AppResources.GetAppIcon();
        BackColor = Color.FromArgb(18, 18, 20);
        ForeColor = Color.White;

        Panel header = new Panel();
        header.Dock = DockStyle.Top;
        header.Height = 94;
        header.BackColor = Color.FromArgb(30, 30, 34);

        Label title = new Label();
        title.Text = "Shutdown Utility Pro";
        title.Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold);
        title.Location = new Point(24, 9);
        title.AutoSize = true;
        header.Controls.Add(title);

        Label subtitle = new Label();
        ModeLabel = subtitle;
        UpdateModeIndicator();
        subtitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        subtitle.Location = new Point(27, 55);
        subtitle.AutoSize = true;
        header.Controls.Add(subtitle);

        Button hideButton = MakeButton("Hide to tray", ClientSize.Width - 160, 26, 135, 36);
        hideButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        hideButton.Click += delegate { HideToTray(true); };
        header.Controls.Add(hideButton);

        TableLayoutPanel root = new TableLayoutPanel();
        root.Dock = DockStyle.Fill;
        root.Padding = new Padding(14);
        root.ColumnCount = 2;
        root.RowCount = 3;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
        Controls.Add(root);
        Controls.Add(header);

        GroupBox actions = MakeGroup("Power actions");
        FlowLayoutPanel actionFlow = new FlowLayoutPanel();
        actionFlow.Dock = DockStyle.Fill;
        actionFlow.Padding = new Padding(8);
        actionFlow.WrapContents = true;
        AddActionButton(actionFlow, "Shut down", PowerAction.Shutdown);
        AddActionButton(actionFlow, "Restart", PowerAction.Restart);
        AddActionButton(actionFlow, "Sleep", PowerAction.Sleep);
        AddActionButton(actionFlow, "Hibernate", PowerAction.Hibernate);
        AddActionButton(actionFlow, "Lock", PowerAction.Lock);
        actions.Controls.Add(actionFlow);
        root.Controls.Add(actions, 0, 0);

        GroupBox settings = MakeGroup("Countdown & sound");
        TableLayoutPanel st = new TableLayoutPanel();
        st.Dock = DockStyle.Fill;
        st.Padding = new Padding(10);
        st.ColumnCount = 2;
        st.RowCount = 6;
        st.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
        st.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        st.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
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
        SoundPathBox = new TextBox(); SoundPathBox.Dock = DockStyle.Fill;
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
        Label hint = new Label();
        hint.Text = "Action-specific WAV files are used automatically when present.";
        hint.ForeColor = Color.Silver;
        hint.Dock = DockStyle.Fill;
        st.Controls.Add(hint, 1, 5);
        settings.Controls.Add(st); root.Controls.Add(settings, 1, 0);

        GroupBox schedule = MakeGroup("Schedule shutdown / restart");
        TableLayoutPanel sch = new TableLayoutPanel(); sch.Dock = DockStyle.Fill; sch.Padding = new Padding(8); sch.ColumnCount = 3; sch.RowCount = 3;
        sch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f)); sch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125f)); sch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        sch.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); sch.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); sch.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        Label al = new Label(); al.Text = "Action"; al.AutoSize = true; al.Anchor = AnchorStyles.Left; sch.Controls.Add(al,0,0);
        ScheduleActionBox = new ComboBox(); ScheduleActionBox.DropDownStyle = ComboBoxStyle.DropDownList; ScheduleActionBox.Dock = DockStyle.Fill; ScheduleActionBox.Items.Add("Shut down"); ScheduleActionBox.Items.Add("Restart"); ScheduleActionBox.SelectedIndex = 0; sch.Controls.Add(ScheduleActionBox,1,0);
        Label dl = new Label(); dl.Text = "Minutes"; dl.AutoSize = true; dl.Anchor = AnchorStyles.Left; sch.Controls.Add(dl,0,1);
        ScheduleMinutesBox = NewNumeric(1, 10080, 10); ScheduleMinutesBox.Dock = DockStyle.Fill; sch.Controls.Add(ScheduleMinutesBox,1,1);
        Button scheduleButton = MakeButton("Schedule", 0, 0, 105, 30); scheduleButton.Dock = DockStyle.Fill; scheduleButton.Click += ScheduleAction; sch.Controls.Add(scheduleButton,2,0);
        Button cancelScheduleButton = MakeButton("Cancel schedule", 0, 0, 125, 30); cancelScheduleButton.Dock = DockStyle.Fill; cancelScheduleButton.Click += CancelScheduledAction; sch.Controls.Add(cancelScheduleButton,2,1);
        Label schHint = new Label(); schHint.Text = "Uses Windows' timer. Cancel it here or from the tray."; schHint.ForeColor = Color.Silver; schHint.Dock = DockStyle.Fill; sch.Controls.Add(schHint,0,2); sch.SetColumnSpan(schHint,3);
        schedule.Controls.Add(sch); root.Controls.Add(schedule, 0, 1); root.SetColumnSpan(schedule, 1);

        GroupBox options = MakeGroup("Safety & convenience");
        TableLayoutPanel opt = new TableLayoutPanel(); opt.Dock = DockStyle.Fill; opt.Padding = new Padding(8); opt.RowCount = 6; opt.ColumnCount = 1;
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 23f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 23f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 23f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 23f));
        opt.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        TestModeBox = NewCheck("Test mode (simulate; no Windows actions)", 0); opt.Controls.Add(TestModeBox,0,0);
        HotkeyBox = NewCheck("Desktop-only hotkey (Ctrl+Alt+Shift+S)", 1); opt.Controls.Add(HotkeyBox,0,1);
        TrayStartBox = NewCheck("Start utility in the system tray", 2); opt.Controls.Add(TrayStartBox,0,2);
        CloseToTrayBox = NewCheck("Close button hides to tray instead of exiting", 3); opt.Controls.Add(CloseToTrayBox,0,3);
        StartWithWindowsBox = NewCheck("Start with Windows (tray mode)", 4); opt.Controls.Add(StartWithWindowsBox,0,4);
        FlowLayoutPanel settingsButtons = new FlowLayoutPanel();
        settingsButtons.Dock = DockStyle.Fill;
        settingsButtons.WrapContents = false;
        Button save = MakeButton("Save",0,0,80,28); save.Margin = new Padding(2); save.Click += SaveSettings; settingsButtons.Controls.Add(save);
        Button openFolder = MakeButton("Open folder",0,0,95,28); openFolder.Margin = new Padding(2); openFolder.Click += delegate { OpenFolder(); }; settingsButtons.Controls.Add(openFolder);
        Button reset = MakeButton("Reset",0,0,80,28); reset.Margin = new Padding(2); reset.Click += ResetSettings; settingsButtons.Controls.Add(reset);
        opt.Controls.Add(settingsButtons,0,5);
        options.Controls.Add(opt); root.Controls.Add(options,1,1);

        StatusLabel = new Label();
        StatusLabel.Text = DefaultStatus;
        StatusLabel.Dock = DockStyle.Fill;
        StatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        StatusLabel.ForeColor = Color.Silver;
        StatusLabel.Padding = new Padding(14, 0, 14, 0);
        StatusLabel.BackColor = Color.FromArgb(30,30,34);
        root.Controls.Add(StatusLabel,0,2); root.SetColumnSpan(StatusLabel,2);
    }

    private void AddActionButton(FlowLayoutPanel flow, string text, PowerAction action)
    {
        Button b = new Button();
        b.Text = text;
        b.Tag = action;
        b.Width = 150; b.Height = 62;
        b.Margin = new Padding(4);
        b.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
        StyleButton(b);
        b.Click += delegate { BeginAction((PowerAction)b.Tag); };
        flow.Controls.Add(b);
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
        TrayStartBox.Checked = Config.StartInTray;
        CloseToTrayBox.Checked = Config.CloseToTray;
        StartWithWindowsBox.Checked = Config.StartWithWindows || StartupManager.IsEnabled();
        ScheduleMinutesBox.Value = Math.Max(1, Math.Min(10080, Config.ScheduledDefaultMinutes));
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
        catch (Exception ex) { Logger.Write("Reset settings failed: " + ex); }
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
            "Shutdown Utility Pro 3.1.0\r\n\r\n" +
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

    private static GroupBox MakeGroup(string text)
    {
        GroupBox g = new GroupBox();
        g.Text = text; g.Dock = DockStyle.Fill; g.ForeColor = Color.White; g.Padding = new Padding(12); return g;
    }
    private static Button MakeButton(string text, int x, int y, int w, int h)
    {
        Button b = new Button(); b.Text = text; b.Location = new Point(x,y); b.Size = new Size(w,h); StyleButton(b); return b;
    }
    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(72, 78, 90);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 55, 68);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(39, 73, 120);
        button.BackColor = Color.FromArgb(35, 39, 48);
        button.ForeColor = Color.White;
    }
    private static NumericUpDown NewNumeric(int min, int max, int value)
    {
        NumericUpDown n = new NumericUpDown(); n.Minimum = min; n.Maximum = max; n.Value = value; n.Dock = DockStyle.Left; n.Width = 140; return n;
    }
    private static CheckBox NewCheck(string text, int row)
    {
        CheckBox c = new CheckBox(); c.Text = text; c.AutoSize = true; c.Margin = new Padding(3,3,3,1); return c;
    }
    private static void AddLabel(TableLayoutPanel t, string text, int row)
    {
        Label l = new Label(); l.Text = text; l.AutoSize = true; l.Anchor = AnchorStyles.Left; t.Controls.Add(l, 0, row);
    }

    private void UpdateModeIndicator()
    {
        if (ModeLabel == null) return;
        ModeLabel.Text = Config.TestMode
            ? "TEST MODE - actions are simulated"
            : "Windows power actions are enabled";
        ModeLabel.ForeColor = Config.TestMode ? Color.Gold : Color.LightGreen;
        if (Tray != null) Tray.Text = Config.TestMode ? "Shutdown Utility Pro - TEST MODE" : "Shutdown Utility Pro";
    }

    private void ApplyDarkButtonStyling(Control root)
    {
        // Reserved for future theme support; native WinForms controls intentionally remain accessible.
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
