#!/usr/bin/env node
const fs = require("fs");
const path = require("path");
const cp = require("child_process");
const root = path.resolve(__dirname, "..");
const runtime = path.join(root, "runtime");
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
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status || 1);
}
function doctor() {
  for (const command of ["node", "npm.cmd", "dotnet"]) {
    try {
      const result = cp.spawnSync(command, ["--version"], {
        stdio: "pipe",
        encoding: "utf8",
      });
      console.log(
        `${command}: ${result.status === 0 ? result.stdout.trim() : "not available"}`,
      );
    } catch {
      console.log(`${command}: not available`);
    }
  }
}
function build() {
  const { appName } = projectConfig();
  if (!fs.existsSync(path.join(root, "node_modules")))
    run("npm.cmd", ["install"]);
  fs.mkdirSync(runtime, { recursive: true });
  run(process.execPath, [
    path.join(root, "node_modules", "esbuild", "bin", "esbuild"),
    "src/entry.ts",
    "--bundle",
    "--platform=node",
    "--format=iife",
    "--outfile=runtime/App.js",
    "--tsconfig=tsconfig.json",
  ]);
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
  for (const directory of ["assets", "docs"]) {
    const source = path.join(root, directory);
    const destination = path.join(root, "dist", directory);
    if (fs.existsSync(source))
      fs.cpSync(source, destination, { recursive: true, force: true });
  }
  const exe = path.join(root, "dist", `${appName}.exe`);
  if (!fs.existsSync(exe))
    throw new Error(`Build completed without dist/${appName}.exe`);
  console.log(`Built ${exe}`);
}
function dev() {
  build();
  console.log("Starting Jupiter development preview with dotnet exec...");
  const developmentAssembly = path.join(
    runtime,
    "bin",
    "Release",
    "net10.0-windows",
    "win-x64",
    "Jupiter.dll",
  );
  const child = cp.spawn("dotnet", ["exec", developmentAssembly], {
    cwd: root,
    stdio: "inherit",
    windowsHide: false,
  });
  child.on("error", (error) => {
    console.error(
      `Could not start Jupiter development preview: ${error.message}`,
    );
    process.exitCode = 1;
  });
  child.on("close", (code) => process.exit(code ?? 0));
}
function clean() {
  for (const target of [
    path.join(root, "dist"),
    path.join(runtime, "bin"),
    path.join(runtime, "obj"),
    path.join(runtime, "App.js"),
  ])
    if (fs.existsSync(target))
      fs.rmSync(target, { recursive: true, force: true });
}
const command = process.argv[2] || "build";
if (command === "doctor") doctor();
else if (command === "clean") clean();
else if (command === "build") build();
else if (command === "dev") dev();
else {
  console.error(`Unknown command: ${command}`);
  process.exit(1);
}
