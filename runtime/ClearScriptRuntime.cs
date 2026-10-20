using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.ClearScript.V8;

namespace Jupiter.Runtime;

public sealed class ClearScriptRuntime : IDisposable
{
  readonly NativeBridge bridge;
  readonly AppWindow window;
  readonly Panel root;
  readonly V8ScriptEngine engine = new();

  FileSystemWatcher? watcher;
  Timer? reloadTimer;
  bool isReloading;
  bool disposed;

  readonly string appPath;

  public ClearScriptRuntime(AppWindow window, Panel root, string appPath)
  {
    this.window = window;
    this.root = root;
    this.appPath = Path.GetFullPath(appPath);

    bridge = new NativeBridge(window, root, engine);

    engine.AddHostObject("JupiterConsole", new NativeConsole());

    engine.Execute("""
            globalThis.console = {
                log: (...values) => JupiterConsole.Log(values.join(' ')),
                warn: (...values) => JupiterConsole.Warn(values.join(' ')),
                error: (...values) => JupiterConsole.Error(values.join(' '))
            };
        """);

    engine.AddHostObject("NativeDesktop", new NativeDesktop(window));
    engine.AddHostObject("NativeBridge", bridge);
  }

  public void Start()
  {
    LoadApp();
    EnableLiveReload();

  }

  void LoadApp()
  {
    if (disposed || isReloading)
      return;

    isReloading = true;

    try
    {
      // Hapus UI lama sebelum merender aplikasi kembali.
      root.Dispatcher.Invoke(() => root.Children.Clear());

      // Jalankan bundle terbaru pada engine yang sama.
      var source = File.ReadAllText(appPath);
      engine.Execute(appPath, source);

      Console.WriteLine("Jupiter app loaded: " + DateTime.Now);
    }
    catch (Exception error)
    {
      MessageBox.Show(
          error.ToString(),
          "Jupiter runtime error",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );
    }
    finally
    {
      isReloading = false;
    }
  }

  public void EnableLiveReload()
  {
    if (watcher != null)
      return;

    var directory = Path.GetDirectoryName(appPath)!;
    var filename = Path.GetFileName(appPath);


    watcher = new FileSystemWatcher(directory, filename)
    {
      NotifyFilter =
            NotifyFilters.LastWrite |
            NotifyFilters.Size |
            NotifyFilters.FileName,
      EnableRaisingEvents = true
    };

    watcher.Changed += OnFileChanged;
    watcher.Created += OnFileChanged;
    watcher.Renamed += OnFileRenamed;
  }

  void OnFileChanged(object sender, FileSystemEventArgs e)
  {
    // Debounce: bundler biasanya menulis file beberapa kali.
    reloadTimer?.Dispose();

    reloadTimer = new Timer(
        _ =>
        {
          if (disposed)
            return;

          try
          {
            window.Dispatcher.BeginInvoke(() =>
                {
                  if (!disposed)
                    LoadApp();
                });
          }
          catch (ObjectDisposedException)
          {
            // Window sudah ditutup.
          }
        },
        null,
        300,
        Timeout.Infinite
    );
  }

  void OnFileRenamed(object sender, RenamedEventArgs e)
  {
    OnFileChanged(sender, e);
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;

    watcher?.Dispose();
    reloadTimer?.Dispose();
    engine.Dispose();
  }
}
