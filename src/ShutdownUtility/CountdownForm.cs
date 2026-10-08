using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows.Forms;

internal sealed class CountdownForm : Form
{
    private readonly AppConfig Config;
    private readonly PowerAction Action;
    private readonly int Seconds;
    private Label CountLabel;
    private Label StatusLabel;
    private Button CancelActionButton;
    private System.Windows.Forms.Timer Timer;
    private int Remaining;
    private bool Cancelled;
    private bool Executed;
    private bool SoundFinished;
    private SoundPlayer SoundPlayer;
    private Thread SoundThread;

    public CountdownForm(AppConfig config, PowerAction action, int seconds)
    {
        Config = config;
        Action = action;
        Seconds = Math.Max(1, seconds);
        Remaining = Seconds;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = GetActionText(Action);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(500, 285);
        BackColor = Color.FromArgb(24, 24, 27);
        ForeColor = Color.White;
        KeyPreview = true;

        Label title = new Label();
        title.Text = GetActionText(Action);
        title.Font = new Font("Segoe UI Semibold", 19f, FontStyle.Bold);
        title.TextAlign = ContentAlignment.MiddleCenter;
        title.Bounds = new Rectangle(25, 18, 450, 38);
        Controls.Add(title);

        CountLabel = new Label();
        CountLabel.Font = new Font("Segoe UI", 50f, FontStyle.Bold);
        CountLabel.TextAlign = ContentAlignment.MiddleCenter;
        CountLabel.Bounds = new Rectangle(30, 55, 440, 100);
        CountLabel.ForeColor = Color.White;
        Controls.Add(CountLabel);

        StatusLabel = new Label();
        StatusLabel.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
        StatusLabel.TextAlign = ContentAlignment.MiddleCenter;
        StatusLabel.Bounds = new Rectangle(25, 155, 450, 45);
        StatusLabel.ForeColor = Color.Gainsboro;
        Controls.Add(StatusLabel);

        CancelActionButton = new Button();
        CancelActionButton.Text = "Cancel (Esc)";
        CancelActionButton.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
        CancelActionButton.Bounds = new Rectangle(160, 215, 180, 40);
        CancelActionButton.Click += delegate { Cancelled = true; Close(); };
        Controls.Add(CancelActionButton);
        CancelButton = CancelActionButton;
        CancelActionButton.Focus();

        Timer = new System.Windows.Forms.Timer();
        Timer.Interval = 1000;
        Timer.Tick += TimerTick;
        KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) { e.Handled = true; Cancelled = true; Close(); } };
        FormClosing += OnClosing;
        Load += OnLoad;
    }

    private void OnLoad(object sender, EventArgs e)
    {
        UpdateCountdownLabel();
        StartSoundHandling();
        if (Seconds == 0)
        {
            if (!Config.PlaySound || !Config.WaitForSound || SoundFinished) ExecuteAction();
            return;
        }
        if (Config.PlaySound && Config.WaitForSound && SoundPlayer != null && !SoundFinished)
        {
            StatusLabel.Text = "Playing shutdown sound…";
            return;
        }
        SoundFinished = true;
        StatusLabel.Text = "Click Cancel or press Esc to abort.";
        Timer.Start();
    }

    private void StartSoundHandling()
    {
        if (!Config.PlaySound) { SoundFinished = true; return; }
        string soundPath = AppResources.ResolveSound(Config, Action);
        if (!File.Exists(soundPath))
        {
            Logger.Write("Sound missing for " + Action + ": " + soundPath);
            StatusLabel.Text = "Sound file not found. Continuing without sound.";
            SoundFinished = true;
            Timer.Start();
            return;
        }
        try
        {
            SoundPlayer = new SoundPlayer(soundPath);
            if (Config.WaitForSound)
            {
                SoundThread = new Thread(delegate()
                {
                    try
                    {
                        SoundPlayer.Load();
                        SoundPlayer.PlaySync();
                    }
                    catch (Exception ex) { Logger.Write("Sound playback failed: " + ex); }
                    finally
                    {
                        if (!IsDisposed && !Cancelled)
                        {
                            try { BeginInvoke((MethodInvoker)delegate { SoundFinished = true; if (Seconds == 0) ExecuteAction(); else { StatusLabel.Text = "Click Cancel or press Esc to abort."; Timer.Start(); } }); }
                            catch { }
                        }
                    }
                });
                SoundThread.IsBackground = true;
                SoundThread.Start();
            }
            else
            {
                SoundFinished = true;
                SoundPlayer.LoadAsync();
                SoundPlayer.Play();
                Timer.Start();
                StatusLabel.Text = "Sound playing. Click Cancel or press Esc to abort.";
            }
        }
        catch (Exception ex)
        {
            Logger.Write("Sound setup failed: " + ex);
            SoundFinished = true;
            Timer.Start();
        }
    }

    private void TimerTick(object sender, EventArgs e)
    {
        if (Cancelled || Executed) return;
        Remaining--;
        UpdateCountdownLabel();
        if (Remaining <= 0) ExecuteAction();
    }

    private void UpdateCountdownLabel()
    {
        CountLabel.Text = Remaining > 0 ? Remaining.ToString() : "GO";
    }

    private void ExecuteAction()
    {
        if (Executed || Cancelled) return;
        Executed = true;
        Timer.Stop();
        string error;
        Logger.Write("Executing action: " + Action);
        bool simulated;
        if (!PowerController.Execute(Action, Config.TestMode, out simulated, out error))
        {
            Executed = false;
            MessageBox.Show(this, "Windows could not complete the requested action.\r\n\r\n" + error + "\r\n\r\nDetails are in Shutdown.log.", "Shutdown Utility", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Logger.Write("Action failed: " + Action + " - " + error);
            return;
        }
        if (simulated)
        {
            StatusLabel.Text = "TEST MODE: no power action was executed.";
            Logger.Write("TEST MODE prevented action: " + Action);
            MessageBox.Show(this, "Test mode is enabled. No shutdown/restart/sleep/hibernate/lock was performed.", "Shutdown Utility Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        StatusLabel.Text = "Windows action initiated.";
        Close();
    }

    private void OnClosing(object sender, FormClosingEventArgs e)
    {
        if (!Executed) Logger.Write("Countdown cancelled: " + Action);
        Timer.Stop();
        try { if (SoundPlayer != null) SoundPlayer.Stop(); } catch { }
    }

    private static string GetActionText(PowerAction action)
    {
        switch (action)
        {
            case PowerAction.Restart: return "Restarting Windows";
            case PowerAction.Sleep: return "Putting Windows to Sleep";
            case PowerAction.Hibernate: return "Hibernating Windows";
            case PowerAction.Lock: return "Locking Windows";
            default: return "Shutting Down Windows";
        }
    }
}
