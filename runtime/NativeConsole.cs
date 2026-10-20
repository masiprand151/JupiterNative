
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Microsoft.ClearScript;

namespace Jupiter.Runtime;

public sealed class NativeConsole
{
  const uint AttachParentProcess = 0xFFFFFFFF;
  static int consoleAttachAttempted;

  public void Log(params object?[] messages)
      => Write("log", messages);

  public void Warn(params object?[] messages)
      => Write("warn", messages);

  public void Error(params object?[] messages)
      => Write("error", messages);

  static void Write(string level, object?[] messages)
  {
    AttachToParentConsole();

    var text = string.Join(" ", messages.Select(Format));

    if (level == "error")
      System.Console.Error.WriteLine($"[Jupiter:{level}] {text}");
    else
      System.Console.WriteLine($"[Jupiter:{level}] {text}");
  }

  static string Format(object? value)
  {
    if (value == null)
      return "null";

    if (value is string text)
      return text;

    try
    {
      return JsonSerializer.Serialize(value);
    }
    catch
    {
      return value.ToString() ?? "null";
    }
  }

  static void AttachToParentConsole()
  {
    if (Interlocked.Exchange(ref consoleAttachAttempted, 1) != 0)
      return;

    if (!OperatingSystem.IsWindows())
      return;

    if (!AttachConsole(AttachParentProcess))
      return;

    var output = new StreamWriter(
        System.Console.OpenStandardOutput())
    {
      AutoFlush = true
    };

    var error = new StreamWriter(
        System.Console.OpenStandardError())
    {
      AutoFlush = true
    };

    System.Console.SetOut(output);
    System.Console.SetError(error);
  }

  [DllImport("kernel32.dll", SetLastError = true)]
  static extern bool AttachConsole(uint processId);
}
