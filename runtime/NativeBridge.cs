using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace Jupiter.Runtime;

public sealed class NativeBridge
{
  readonly Panel root;
  readonly V8ScriptEngine engine;
  readonly AppWindow window;

  public NativeBridge(AppWindow window, Panel root, V8ScriptEngine engine) { this.window = window; this.root = root; this.engine = engine; }

  public void ConfigureWindow(string optionsJson) => window.ApplyOptions(optionsJson);

  public void Mount(object? tree)
  {
    var previousTextBoxes = FindTextBoxes(root);
    var previousSliders = FindSliders(root);
    var focusedKey = previousTextBoxes.FirstOrDefault(textBox => textBox.IsKeyboardFocusWithin)?.Tag?.ToString();
    foreach (var textBox in previousTextBoxes) Detach(textBox);
    foreach (var slider in previousSliders) Detach(slider);
    root.Children.Clear();
    root.Children.Add(LayoutRenderer.Render(tree, engine));

    if (focusedKey is not null)
    {
      var textBox = FindTextBoxes(root).FirstOrDefault(candidate => candidate.Tag?.ToString() == focusedKey);
      if (textBox is not null)
      {
        root.Dispatcher.BeginInvoke(() =>
        {
          if (!textBox.IsKeyboardFocusWithin)
          {
            Keyboard.Focus(textBox);
            textBox.Focus();
          }
          textBox.Select(textBox.Text.Length, 0);
        }, System.Windows.Threading.DispatcherPriority.ContextIdle);
      }
    }
  }

  public void Update(object? tree) => Mount(tree);

  public void Schedule(ScriptObject callback)
  {
    root.Dispatcher.BeginInvoke(() =>
    {
      try { callback.Invoke(false); }
      catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter scheduled update error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }, System.Windows.Threading.DispatcherPriority.DataBind);
  }

  static List<TextBox> FindTextBoxes(DependencyObject parent)
  {
    var result = new List<TextBox>();
    CollectTextBoxes(parent, result);
    return result;
  }

  static List<Slider> FindSliders(DependencyObject parent)
  {
    var result = new List<Slider>();
    CollectSliders(parent, result);
    return result;
  }

  static void CollectSliders(DependencyObject parent, List<Slider> result)
  {
    if (parent is Slider slider) { result.Add(slider); return; }
    for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
    {
      CollectSliders(VisualTreeHelper.GetChild(parent, index), result);
    }
  }

  static void CollectTextBoxes(DependencyObject parent, List<TextBox> result)
  {
    if (parent is TextBox textBox) { result.Add(textBox); return; }
    for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
    {
      CollectTextBoxes(VisualTreeHelper.GetChild(parent, index), result);
    }
  }

  static void Detach(FrameworkElement element)
  {
    var parent = VisualTreeHelper.GetParent(element);
    if (parent is Panel panel) panel.Children.Remove(element);
    else if (parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, element)) contentControl.Content = null;
    else if (parent is Decorator decorator && ReferenceEquals(decorator.Child, element)) decorator.Child = null;
  }
}
