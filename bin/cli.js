#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const cp = require("child_process");

const root = path.resolve(__dirname, "..");
const runtime = path.join(root, "runtime");
const esbuild = path.join(root, "node_modules", "esbuild", "bin", "esbuild");

const npm = process.platform === "win32" ? "npm.cmd" : "npm";

let devProcesses = [];
let shuttingDown = false;

function projectConfig() {
  const packageJson = JSON.parse(
    fs.readFileSync(path.join(root, "package.json"), "utf8"),
  );

  const configPath = path.join(root, "jupiter.config.json");
  const config = fs.existsSync(configPath)
    ? JSON.parse(fs.readFileSync(configPath, "utf8"))
    : {};

  const configuredName = config.appName || packageJson.name || "jupiter";

  const appName = configuredName
    .replace(/^@[^/]+\//, "")
    .replace(/[^a-zA-Z0-9._-]+/g, "-");

  return { appName: appName || "jupiter" };
}

function run(command, args, cwd = root) {
  const result = cp.spawnSync(command, args, {
    cwd,
    stdio: "inherit",
    shell: false,
  });

  if (result.error) {
    throw result.error;
  }

  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}

function ensureDependencies() {
  if (!fs.existsSync(path.join(root, "node_modules"))) {
    run(npm, ["install"]);
  }

  if (!fs.existsSync(esbuild)) {
    throw new Error(
      "esbuild tidak ditemukan. Pastikan esbuild terpasang di devDependencies.",
    );
  }

  fs.mkdirSync(runtime, { recursive: true });
}

function bundleArgs(watch = false) {
  const args = [
    esbuild,
    path.join(root, "src", "entry.ts"),
    "--bundle",
    "--platform=node",
    "--format=iife",
    `--outfile=${path.join(runtime, "App.js")}`,
    `--tsconfig=${path.join(root, "tsconfig.json")}`,
  ];

  if (watch) {
    args.push("--watch");
  }

  return args;
}

function copyRuntimeAssets() {
  for (const directory of ["assets", "docs"]) {
    const source = path.join(root, directory);
    const destination = path.join(root, "dist", directory);

    if (fs.existsSync(source)) {
      fs.cpSync(source, destination, {
        recursive: true,
        force: true,
      });
    }
  }
}

function build() {
  const { appName } = projectConfig();

  ensureDependencies();

  // Build JavaScript bundle.
  run(process.execPath, bundleArgs());

  // Publish the production application.
  run(
    "dotnet",
    [
      "publish",
      "App.csproj",
      "-c",
      "Release",
      "-r",
      "win-x64",
      "--self-contained",
      "true",
      "-p:PublishSingleFile=true",
      "-p:IncludeNativeLibrariesForSelfExtract=true",
      `-p:AssemblyName=${appName}`,
      "-o",
      path.join(root, "dist"),
    ],
    runtime,
  );

  copyRuntimeAssets();

  const exe = path.join(root, "dist", `${appName}.exe`);

  if (!fs.existsSync(exe)) {
    throw new Error(`Build completed without dist/${appName}.exe`);
  }

  console.log(`Built ${exe}`);
}

function startChild(command, args, options = {}) {
  const child = cp.spawn(command, args, {
    cwd: options.cwd || root,
    stdio: "inherit",
    windowsHide: false,
    env: options.env || process.env,
    shell: false,
  });

  devProcesses.push(child);

  child.on("error", (error) => {
    console.error(`Failed to start ${command}: ${error.message}`);

    if (!shuttingDown) {
      shutdown(1);
    }
  });

  return child;
}

function shutdown(code = 0) {
  if (shuttingDown) return;

  shuttingDown = true;

  for (const child of devProcesses) {
    if (child && child.exitCode === null && !child.killed) {
      child.kill();
    }
  }

  process.exitCode = code;
}

function doctor() {
  for (const command of [process.execPath, npm, "dotnet"]) {
    const args = command === process.execPath ? ["--version"] : ["--version"];

    try {
      const result = cp.spawnSync(command, args, {
        cwd: root,
        stdio: "pipe",
        encoding: "utf8",
        shell: false,
      });

      console.log(
        `${command}: ${
          result.status === 0 ? result.stdout.trim() : "not available"
        }`,
      );
    } catch {
      console.log(`${command}: not available`);
    }
  }
}

function dev() {
  ensureDependencies();

  // 1. Buat bundle pertama sebelum aplikasi dijalankan.
  run(process.execPath, bundleArgs());

  console.log("");
  console.log("Jupiter Development Mode");
  console.log("JavaScript live rebuild: enabled");
  console.log("Application window reload: requires runtime FileSystemWatcher");
  console.log("");

  // 2. Pantau perubahan source JavaScript/TypeScript.
  const watcher = startChild(process.execPath, bundleArgs(true));

  watcher.on("close", (code) => {
    if (!shuttingDown) {
      console.error(`esbuild watcher stopped (code ${code}).`);
      shutdown(code ?? 1);
    }
  });

  // 3. Jalankan aplikasi WPF dalam mode development.
  // Runtime harus membaca file runtime/App.js, bukan hanya embedded resource.
  const child = startChild(
    "dotnet",
    [
      "run",
      "--project",
      "App.csproj",
      "--configuration",
      "Release",
      "--runtime",
      "win-x64",
    ],
    {
      cwd: runtime,
      env: {
        ...process.env,
        JUPITER_DEV: "1",
        JUPITER_APP_PATH: path.join(runtime, "App.js"),
      },
    },
  );

  child.on("close", (code) => {
    if (!shuttingDown) {
      shutdown(code ?? 0);
    }
  });
}

function clean() {
  for (const target of [
    path.join(root, "dist"),
    path.join(runtime, "bin"),
    path.join(runtime, "obj"),
    path.join(runtime, "App.js"),
  ]) {
    if (fs.existsSync(target)) {
      fs.rmSync(target, {
        recursive: true,
        force: true,
      });
    }
  }

  console.log("Jupiter build files cleaned.");
}

process.on("SIGINT", () => shutdown(0));
process.on("SIGTERM", () => shutdown(0));

const command = process.argv[2] || "build";

try {
  if (command === "doctor") {
    doctor();
  } else if (command === "clean") {
    clean();
  } else if (command === "build") {
    build();
  } else if (command === "dev") {
    dev();
  } else {
    console.error(`Unknown command: ${command}`);
    console.error("Available commands: build, dev, clean, doctor");
    process.exitCode = 1;
  }
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
}
