import App from "./App";
import { start } from "@jupiter/core";

start(App, {
  title: "Jupiter Native Desktop",
  width: 1366,
  height: 764,
  resizable: true,
  titleBar: true,
  alwaysOnTop: false,
  fullscreen: false,
  startPosition: { type: "manual", x: 0, y: 0 },
});
