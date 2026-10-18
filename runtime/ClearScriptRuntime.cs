using System.Reflection;
using System.IO;
using System.Windows.Controls;
using Microsoft.ClearScript.V8;
namespace Jupiter.Runtime;

public sealed class ClearScriptRuntime : IDisposable
{
  readonly NativeBridge bridge;
  readonly V8ScriptEngine engine = new();
  public ClearScriptRuntime(AppWindow window, Panel root)
  {
    bridge = new NativeBridge(window, root, engine);
    engine.AddHostObject("JupiterConsole", new NativeConsole());
    engine.Execute("globalThis.console = { log: (...values) => JupiterConsole.Log(values.join(' ')), warn: (...values) => JupiterConsole.Warn(values.join(' ')), error: (...values) => JupiterConsole.Error(values.join(' ')) };");
    engine.AddHostObject("NativeDesktop", new NativeDesktop(window));
    engine.AddHostObject("NativeBridge", bridge);
  }
  public void Start()
  {
    try
    {
      using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Jupiter.App.js") ?? throw new InvalidOperationException("Embedded JavaScript bundle not found."); using var reader = new StreamReader(stream); engine.Execute("Jupiter.App.js", reader.ReadToEnd());
    }
    catch (Exception error)
    {
      System.Windows.MessageBox.Show(error.ToString(), "Jupiter runtime error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); throw;
    }
  }
  public void Dispose() => engine.Dispose();
}
