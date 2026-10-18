using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Jupiter.Runtime;

public sealed class NativeDesktop
{
  readonly AppWindow window;
  WindowStyle previousWindowStyle = WindowStyle.SingleBorderWindow;
  ResizeMode previousResizeMode = ResizeMode.CanResize;
  WindowState previousWindowState = WindowState.Normal;
  bool isFullscreen;

  public NativeDesktop(AppWindow window) => this.window = window;

  public void MinimizeWindow() => window.WindowState = WindowState.Minimized;
  public void ToggleMaximizeWindow() => window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
  public void RestoreWindow()
  {
    if (isFullscreen)
    {
      window.WindowStyle = previousWindowStyle;
      window.ResizeMode = previousResizeMode;
      isFullscreen = false;
      window.ClearConfiguredFullscreen();
    }

    window.WindowState = WindowState.Normal;
  }
  public void ToggleFullscreen()
  {
    if (!isFullscreen && window.ConfiguredFullscreen)
    {
      previousWindowStyle = window.FullscreenRestoreStyle;
      previousResizeMode = window.FullscreenRestoreResizeMode;
      previousWindowState = window.FullscreenRestoreState;
      isFullscreen = true;
    }
    if (isFullscreen)
    {
      window.WindowStyle = previousWindowStyle;
      window.ResizeMode = previousResizeMode;
      window.WindowState = previousWindowState;
      isFullscreen = false;
      window.ClearConfiguredFullscreen();
      return;
    }

    previousWindowStyle = window.WindowStyle;
    previousResizeMode = window.ResizeMode;
    previousWindowState = window.WindowState;
    window.WindowStyle = WindowStyle.None;
    window.ResizeMode = ResizeMode.NoResize;
    window.WindowState = WindowState.Maximized;
    isFullscreen = true;
  }

  public bool ToggleAlwaysOnTop()
  {
    window.Topmost = !window.Topmost;
    return window.Topmost;
  }
  public void CloseWindow() => window.Close();
  public void SetWindowTitle(string title) => window.Title = title;

  public void SetClipboardText(string? text)
  {
    if (text is not null) Clipboard.SetText(text);
  }

  public string GetClipboardText() => Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;

  public string OpenFile(string filter = "All files (*.*)|*.*")
  {
    var dialog = new OpenFileDialog { Filter = filter, Multiselect = false };
    return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
  }

  public string SaveFile(string defaultFileName = "", string filter = "All files (*.*)|*.*")
  {
    var dialog = new SaveFileDialog { FileName = defaultFileName, Filter = filter };
    return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
  }

  public string ReadTextFile(string path) => File.ReadAllText(path);
  public void WriteTextFile(string path, string content) => File.WriteAllText(path, content);

  public void OpenUrl(string url)
  {
    if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
    {
      Process.Start(new ProcessStartInfo { FileName = uri.ToString(), UseShellExecute = true });
      return;
    }

    var path = uri?.Scheme == Uri.UriSchemeFile ? uri.LocalPath : ResolvePath(url);
    if (path is null || (!File.Exists(path) && !Directory.Exists(path))) throw new FileNotFoundException("The requested local path was not found.", path);
    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
  }

  static string? ResolvePath(string path)
  {
    if (Path.IsPathRooted(path)) return path;
    var locations = new[] { new DirectoryInfo(AppContext.BaseDirectory), new DirectoryInfo(Directory.GetCurrentDirectory()) };
    foreach (var location in locations)
    {
      var current = location;
      while (current is not null)
      {
        var candidate = Path.Combine(current.FullName, path);
        if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
        current = current.Parent;
      }
    }
    return null;
  }

  public void ShowMessage(string message, string title = "Jupiter")
  {
    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
  }
}
