using System.Windows;
namespace Jupiter.Runtime;

public static class Program
{
  [STAThread]
  public static void Main()
  {
    var app = new Application();
    var window = new AppWindow();
    app.Run(window);
  }
}
