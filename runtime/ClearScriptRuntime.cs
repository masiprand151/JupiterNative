
using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace Jupiter.Runtime;

public sealed class ClearScriptRuntime : IDisposable
{
    readonly NativeBridge bridge;
    readonly AppWindow window;
    readonly Panel root;
    readonly V8ScriptEngine engine;

    FileSystemWatcher? watcher;
    Timer? reloadTimer;
    bool isReloading;
    bool disposed;

    readonly string appPath;

    public ClearScriptRuntime(
        AppWindow window,
        Panel root,
        string appPath)
    {
        this.window = window;
        this.root = root;
        this.appPath = Path.GetFullPath(appPath);

        // Aktifkan konversi Task .NET menjadi Promise JavaScript.
        engine = new V8ScriptEngine(
            V8ScriptEngineFlags.EnableTaskPromiseConversion
        );

        bridge = new NativeBridge(window, root, engine);

        engine.AddHostObject("NativeDesktop", new NativeDesktop(window));
        engine.AddHostObject("NativeBridge", bridge);
        engine.AddHostObject("JupiterConsole", new NativeConsole());
        engine.AddHostObject("NativeFetch", new NativeFetch());

        engine.Execute("""
            // ==========================================
            // CONSOLE
            // ==========================================

            function formatConsoleValue(value) {
                if (value === undefined) return "undefined";
                if (value === null) return "null";
                if (typeof value === "string") return value;

                if (typeof value === "object") {
                    try {
                        return JSON.stringify(value);
                    } catch {
                        return Object.prototype.toString.call(value);
                    }
                }

                return String(value);
            }

            globalThis.console = {
                log: (...values) =>
                    JupiterConsole.Log(
                        values.map(formatConsoleValue).join(" ")
                    ),

                warn: (...values) =>
                    JupiterConsole.Warn(
                        values.map(formatConsoleValue).join(" ")
                    ),

                error: (...values) =>
                    JupiterConsole.Error(
                        values.map(formatConsoleValue).join(" ")
                    )
            };


            // ==========================================
            // FETCH
            // ==========================================

            globalThis.fetch = async (input, options = {}) => {
                const url =
                    typeof input === "string" ? input : input.url;

                const method =
                    String(options.method || "GET").toUpperCase();

                const headers = {};
                const inputHeaders = options.headers || {};

                if (typeof inputHeaders.forEach === "function") {
                    inputHeaders.forEach((value, key) => {
                        headers[key] = String(value);
                    });
                } else {
                    for (const key of Object.keys(inputHeaders)) {
                        headers[key] = String(inputHeaders[key]);
                    }
                }

                let body = "";

                if (options.body != null) {
                    body = typeof options.body === "string"
                        ? options.body
                        : JSON.stringify(options.body);

                    if (
                        typeof options.body !== "string" &&
                        !Object.keys(headers).some(
                            key => key.toLowerCase() === "content-type"
                        )
                    ) {
                        headers["Content-Type"] = "application/json";
                    }
                }

                const timeout = Number(options.timeout) || 30000;

                let raw;

                try {
                    raw = await NativeFetch.Send(
                        String(url),
                        method,
                        JSON.stringify(headers),
                        body,
                        timeout
                    );
                } catch (error) {
                    throw new Error(
                        "NativeFetch.Send failed: " +
                        (error && error.message
                            ? error.message
                            : String(error))
                    );
                }

                let data;

                try {
                    data = typeof raw === "string"
                        ? JSON.parse(raw)
                        : raw;
                } catch {
                    throw new Error(
                        "NativeFetch returned invalid JSON: " +
                        String(raw).slice(0, 300)
                    );
                }

                if (data === null || typeof data !== "object") {
                    throw new Error(
                        "NativeFetch returned invalid response: " +
                        String(raw).slice(0, 300)
                    );
                }

                if (data.error) {
                    throw new Error("NativeFetch: " + data.error);
                }

                const responseBody =
                    typeof data.body === "string"
                        ? data.body
                        : data.body == null
                            ? ""
                            : JSON.stringify(data.body);

                const responseHeaders = data.headers || {};

                return {
                    ok: Boolean(data.ok),
                    status: Number(data.status || 0),
                    statusText: String(data.statusText || ""),
                    url: String(data.url || url),

                    headers: {
                        get(name) {
                            const key = Object.keys(responseHeaders).find(
                                k => k.toLowerCase() ===
                                    String(name).toLowerCase()
                            );

                            return key === undefined
                                ? null
                                : responseHeaders[key];
                        },

                        has(name) {
                            return Object.keys(responseHeaders).some(
                                k => k.toLowerCase() ===
                                    String(name).toLowerCase()
                            );
                        },

                        forEach(callback) {
                            for (const key of Object.keys(responseHeaders)) {
                                callback(responseHeaders[key], key);
                            }
                        }
                    },

                    text: async () => responseBody,

                    json: async () => {
                        if (!responseBody.trim()) {
                            throw new Error(
                                "HTTP " + data.status +
                                ": response body is empty"
                            );
                        }

                        try {
                            return JSON.parse(responseBody);
                        } catch {
                            throw new Error(
                                "Response body is not valid JSON: " +
                                responseBody.slice(0, 300)
                            );
                        }
                    }
                };
            };
            """);
    }

    public void Start()
    {
        LoadApp();
        EnableLiveReload();
    }

    private string LoadAppSource()
    {
#if DEBUG
        // Development: App.js eksternal agar live reload tetap berfungsi.
        return File.ReadAllText(appPath);
#else
    // Production: App.js dibaca dari embedded resource.
    using var stream =
        Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Jupiter.App.js")
        ?? throw new FileNotFoundException(
            "Embedded resource Jupiter.App.js tidak ditemukan."
        );

    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
#endif
    }

    void LoadApp()
    {
        if (disposed || isReloading)
            return;

        isReloading = true;

        try
        {
            root.Dispatcher.Invoke(() => root.Children.Clear());

            // var source = File.ReadAllText(appPath);
            var source = LoadAppSource();
            engine.Execute(appPath, source);

            Console.WriteLine(
                "Jupiter app loaded: " + DateTime.Now
            );
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
