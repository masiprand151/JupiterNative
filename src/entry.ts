import App from "./App";
import { start } from "@jupiter/core";

start(App, {
  title: "Jupiter Native Desktop",
  icon: "assets/jn.png",
  width: 1366,
  height: 764,
  minWidth: 800,
  minHeight: 500,
  maxWidth: 1600,
  maxHeight: 1000,
  resizable: true,
  titleBar: true,
  alwaysOnTop: false,
  fullscreen: false,
  startPosition: { type: "manual", x: 0, y: 0 },
});
