using System.IO;
using System.Runtime.InteropServices;

namespace Jupiter.Runtime;

public sealed class NativeConsole
{
  const uint AttachParentProcess = 0xFFFFFFFF;
  static int consoleAttachAttempted;

  public void Log(string? message) => Write("log", message);
  public void Warn(string? message) => Write("warn", message);
  public void Error(string? message) => Write("error", message);

  static void Write(string level, string? message)
  {
    AttachToParentConsole();
    System.Console.WriteLine($"[Jupiter:{level}] {message ?? "null"}");
  }

  static void AttachToParentConsole()
  {
    if (Interlocked.Exchange(ref consoleAttachAttempted, 1) != 0) return;
    if (!AttachConsole(AttachParentProcess)) return;

    var output = new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true };
    var error = new StreamWriter(System.Console.OpenStandardError()) { AutoFlush = true };
    System.Console.SetOut(output);
    System.Console.SetError(error);
  }

  [DllImport("kernel32.dll", SetLastError = true)]
  static extern bool AttachConsole(uint processId);
}
