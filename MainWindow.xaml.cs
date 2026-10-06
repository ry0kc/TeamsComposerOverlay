using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TeamsComposer;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(
        IntPtr hWnd,
        out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(
        uint dwFlags,
        uint dx,
        uint dy,
        uint dwData,
        UIntPtr dwExtraInfo);

    private const int SW_RESTORE = 9;

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private readonly DispatcherTimer positionTimer;

    public MainWindow()
    {
        InitializeComponent();

        MessageBox.PreviewKeyDown += MessageBox_PreviewKeyDown;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };

        positionTimer.Tick += PositionTimer_Tick;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionOverTeams();
        positionTimer.Start();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        positionTimer.Stop();
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        PositionOverTeams();
    }

    private void PositionOverTeams()
    {
        IntPtr teamsWindow = FindTeamsWindow();

        if (teamsWindow == IntPtr.Zero)
        {
            return;
        }

        if (!GetWindowRect(teamsWindow, out RECT rect))
        {
            return;
        }

        double teamsWidth = rect.Right - rect.Left;

        Width = 500;
        Height = 40;

        Left = rect.Left + (teamsWidth - Width) / 2 + 90;
        Top = rect.Bottom - Height - 35;
    }

    private async void MessageBox_PreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }

        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            e.Handled = true;

            string message = MessageBox.Text;

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            System.Windows.Clipboard.SetText(message);

            MessageBox.Clear();

            await SendToTeams();
        }
    }

    private async Task SendToTeams()
    {
        IntPtr teamsWindow = FindTeamsWindow();

        if (teamsWindow == IntPtr.Zero)
        {
            System.Windows.MessageBox.Show(
                "Microsoft Teams could not be found.",
                "Teams Composer",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        // Remember where the overlay is currently positioned.
        int clickX = (int)(Left + Width / 2);
        int clickY = (int)(Top + Height / 2);

        // Hide our overlay so the mouse click reaches Teams.
        Hide();

        await Task.Delay(100);

        ShowWindow(teamsWindow, SW_RESTORE);
        SetForegroundWindow(teamsWindow);

        await Task.Delay(200);

        // Click the Teams composer underneath our overlay.
        SetCursorPos(clickX, clickY);

        await Task.Delay(50);

        mouse_event(
            MOUSEEVENTF_LEFTDOWN,
            0,
            0,
            0,
            UIntPtr.Zero);

        mouse_event(
            MOUSEEVENTF_LEFTUP,
            0,
            0,
            0,
            UIntPtr.Zero);

        await Task.Delay(100);

        // Paste the complete message.
        System.Windows.Forms.SendKeys.SendWait("^v");

        await Task.Delay(100);

        // Send the message.
        System.Windows.Forms.SendKeys.SendWait("{ENTER}");

        await Task.Delay(200);

        // Put our overlay back.
        Show();

        PositionOverTeams();

        MessageBox.Focus();
    }

    private static IntPtr FindTeamsWindow()
    {
        IntPtr window = IntPtr.Zero;

        foreach (System.Diagnostics.Process process in
                 System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (process.ProcessName.Equals(
                    "ms-teams",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        window = process.MainWindowHandle;
                        break;
                    }
                }
            }
            catch
            {
                // Ignore processes that cannot be queried.
            }
        }

        return window;
    }

    [StructLayout(LayoutKind.Sequential)]
	private struct RECT { public int Left; public int Top; public int Right; public int Bottom; } }

