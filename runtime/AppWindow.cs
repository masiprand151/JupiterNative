using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Jupiter.Runtime;

public sealed class AppWindow : Window
{
  readonly Grid root = new(); readonly ClearScriptRuntime runtime;
  public bool ConfiguredFullscreen { get; private set; }
  public WindowStyle FullscreenRestoreStyle { get; private set; } = WindowStyle.SingleBorderWindow;
  public ResizeMode FullscreenRestoreResizeMode { get; private set; } = ResizeMode.CanResize;
  public WindowState FullscreenRestoreState { get; private set; } = WindowState.Normal;
  public void ClearConfiguredFullscreen() => ConfiguredFullscreen = false;
  public AppWindow()
  {
    Title = "Jupiter";
    Width = 0;
    Height = 0;
    Content = root;
    Console.Write(AppContext.BaseDirectory);
    runtime = new ClearScriptRuntime(this, root, Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "App.js"));
    Loaded += (_, _) => runtime.Start();
    Closed += (_, _) => runtime.Dispose();
  }

  public void ApplyOptions(string optionsJson)
  {
    var options = JsonSerializer.Deserialize<WindowOptions>(optionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    if (options is null) return;

    if (options.Title is not null) Title = options.Title;
    if (options.Icon is not null) Icon = LoadIcon(options.Icon);
    if (options.Width is not null) Width = options.Width.Value;
    if (options.Height is not null) Height = options.Height.Value;
    if (options.MinWidth is not null) MinWidth = options.MinWidth.Value;
    if (options.MinHeight is not null) MinHeight = options.MinHeight.Value;
    if (options.MaxWidth is not null) MaxWidth = options.MaxWidth.Value;
    if (options.MaxHeight is not null) MaxHeight = options.MaxHeight.Value;
    if (options.Resizable is not null) ResizeMode = options.Resizable.Value ? ResizeMode.CanResize : ResizeMode.NoResize;
    if (options.AlwaysOnTop is not null) Topmost = options.AlwaysOnTop.Value;

    if (options.TitleBar is not null)
    {
      WindowStyle = options.TitleBar.Value ? WindowStyle.SingleBorderWindow : WindowStyle.None;
    }

    ConfiguredFullscreen = options.Fullscreen is true;
    if (ConfiguredFullscreen)
    {
      FullscreenRestoreStyle = WindowStyle;
      FullscreenRestoreResizeMode = ResizeMode;
      FullscreenRestoreState = WindowState;
      WindowStyle = WindowStyle.None;
      ResizeMode = ResizeMode.NoResize;
      WindowState = WindowState.Maximized;
    }

    if (options.StartPosition is not null)
    {
      if (options.StartPosition.Type.Equals("center", StringComparison.OrdinalIgnoreCase))
      {
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
      }
      else if (options.StartPosition.Type.Equals("manual", StringComparison.OrdinalIgnoreCase))
      {
        WindowStartupLocation = WindowStartupLocation.Manual;
        if (options.StartPosition.X is not null) Left = options.StartPosition.X.Value;
        if (options.StartPosition.Y is not null) Top = options.StartPosition.Y.Value;
      }
    }
  }

  sealed class WindowOptions
  {
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double? MinWidth { get; set; }
    public double? MinHeight { get; set; }
    public double? MaxWidth { get; set; }
    public double? MaxHeight { get; set; }
    public bool? Resizable { get; set; }
    public bool? TitleBar { get; set; }
    public bool? AlwaysOnTop { get; set; }
    public bool? Fullscreen { get; set; }
    public StartPosition? StartPosition { get; set; }
  }

  sealed class StartPosition
  {
    public string Type { get; set; } = "center";
    public double? X { get; set; }
    public double? Y { get; set; }
  }

  ImageSource CreateDefaultIcon()
  {
    var group = new DrawingGroup();
    using (var drawing = group.Open())
    {
      drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(30, 64, 175)), null, new Rect(0, 0, 64, 64), 14, 14);
      var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
      var text = new FormattedText("JN", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 25, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
      drawing.DrawText(text, new Point((64 - text.Width) / 2, (64 - text.Height) / 2));
    }

    group.Freeze();
    var icon = new DrawingImage(group);
    icon.Freeze();
    return icon;
  }

  ImageSource LoadIcon(string iconPath)
  {
    if (iconPath.Equals("default", StringComparison.OrdinalIgnoreCase)) return CreateDefaultIcon();
    if (Path.GetExtension(iconPath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
    {
      MessageBox.Show("SVG icons are not supported by WPF directly. Use PNG, JPG, or ICO instead.", "Jupiter icon warning", MessageBoxButton.OK, MessageBoxImage.Warning);
      return CreateDefaultIcon();
    }

    var path = ResolveIconPath(iconPath);
    if (path is null && iconPath.Replace('\\', '/').Equals("assets/jn.png", StringComparison.OrdinalIgnoreCase))
    {
      path = Path.GetFullPath(iconPath);
      GenerateDefaultPngIcon(path);
    }
    if (path is null) throw new FileNotFoundException("The configured icon file was not found.", iconPath);
    var image = new BitmapImage();
    image.BeginInit();
    image.UriSource = new Uri(path, UriKind.Absolute);
    image.CacheOption = BitmapCacheOption.OnLoad;
    image.EndInit();
    image.Freeze();
    return image;
  }

  void GenerateDefaultPngIcon(string path)
  {
    var directory = Path.GetDirectoryName(path);
    if (directory is not null) Directory.CreateDirectory(directory);
    if (File.Exists(path)) return;

    var visual = new DrawingVisual();
    using (var drawing = visual.RenderOpen())
    {
      drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(30, 64, 175)), null, new Rect(0, 0, 64, 64), 14, 14);
      var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
      var text = new FormattedText("JN", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 25, Brushes.White, 1.0);
      drawing.DrawText(text, new Point((64 - text.Width) / 2, (64 - text.Height) / 2));
    }

    var bitmap = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var stream = File.Create(path);
    encoder.Save(stream);
  }

  static string? ResolveIconPath(string iconPath)
  {
    if (Uri.TryCreate(iconPath, UriKind.Absolute, out var uri) && uri.IsFile) return File.Exists(uri.LocalPath) ? uri.LocalPath : null;
    if (Path.IsPathRooted(iconPath)) return File.Exists(iconPath) ? iconPath : null;
    var locations = new[] { new DirectoryInfo(AppContext.BaseDirectory), new DirectoryInfo(Directory.GetCurrentDirectory()) };
    foreach (var location in locations)
    {
      var current = location;
      while (current is not null)
      {
        var candidate = Path.Combine(current.FullName, iconPath);
        if (File.Exists(candidate)) return candidate;
        current = current.Parent;
      }
    }
    return null;
  }
}
