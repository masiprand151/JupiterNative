import { render } from "./renderer";
import { Child, FlatListProps, StyleProps, WindowOptions } from "./types";
export * from "./types";
export { createElement } from "./element";
export { jsx, jsxs, Fragment } from "./element";
export { useState } from "./state";
export { useEffect } from "./state";
export { ErrorBoundary } from "./errorBoundary";
export { navigate, goBack, replace, NavigationContainer } from "./navigation";
export const desktop = {
  window: {
    minimize() {
      getNativeDesktop().MinimizeWindow();
    },
    toggleMaximize() {
      getNativeDesktop().ToggleMaximizeWindow();
    },
    restore() {
      getNativeDesktop().RestoreWindow();
    },
    toggleFullscreen() {
      getNativeDesktop().ToggleFullscreen();
    },
    toggleAlwaysOnTop(): boolean {
      return getNativeDesktop().ToggleAlwaysOnTop();
    },
    close() {
      getNativeDesktop().CloseWindow();
    },
    setTitle(title: string) {
      getNativeDesktop().SetWindowTitle(title);
    },
  },
  clipboard: {
    setText(text: string) {
      getNativeDesktop().SetClipboardText(text);
    },
    getText(): string {
      return getNativeDesktop().GetClipboardText();
    },
  },
  files: {
    openFile(filter = "All files (*.*)|*.*"): string {
      return getNativeDesktop().OpenFile(filter);
    },
    saveFile(defaultFileName = "", filter = "All files (*.*)|*.*"): string {
      return getNativeDesktop().SaveFile(defaultFileName, filter);
    },
    readText(path: string): string {
      return getNativeDesktop().ReadTextFile(path);
    },
    writeText(path: string, content: string) {
      getNativeDesktop().WriteTextFile(path, content);
    },
  },
  openUrl(url: string) {
    getNativeDesktop().OpenUrl(url);
  },
  showMessage(message: string, title = "Jupiter") {
    getNativeDesktop().ShowMessage(message, title);
  },
};

function getNativeDesktop() {
  const native = (
    globalThis as unknown as {
      NativeDesktop?: {
        MinimizeWindow(): void;
        ToggleMaximizeWindow(): void;
        RestoreWindow(): void;
        ToggleFullscreen(): void;
        ToggleAlwaysOnTop(): boolean;
        CloseWindow(): void;
        SetWindowTitle(title: string): void;
        SetClipboardText(text: string): void;
        GetClipboardText(): string;
        OpenFile(filter: string): string;
        SaveFile(defaultFileName: string, filter: string): string;
        ReadTextFile(path: string): string;
        WriteTextFile(path: string, content: string): void;
        OpenUrl(url: string): void;
        ShowMessage(message: string, title: string): void;
      };
    }
  ).NativeDesktop;
  if (!native) throw new Error("Jupiter NativeDesktop is unavailable.");
  return native;
}

export function start(app: () => Child, options?: WindowOptions) {
  const native = (
    globalThis as unknown as {
      NativeBridge?: {
        ConfigureWindow(optionsJson: string): void;
        Mount(value: unknown): void;
        Update(value: unknown): void;
      };
    }
  ).NativeBridge;
  if (!native) throw new Error("Jupiter NativeBridge is unavailable.");
  if (options) native.ConfigureWindow(JSON.stringify(options));
  render(app(), {
    mount: (value) => native.Mount(value),
    update: (value) => native.Update(value),
  });
}
export function FlatList<T>(props: FlatListProps<T>): Child {
  return props.data.map((item, index) => {
    const child = props.renderItem({ item, index });
    if (
      !props.keyExtractor ||
      typeof child !== "object" ||
      child === null ||
      Array.isArray(child)
    )
      return child;
    return {
      ...(child as object),
      key: props.keyExtractor(item, index),
    } as Child;
  });
}
export const StyleSheet = {
  create<T extends Record<string, StyleProps>>(styles: T): T {
    return styles;
  },
};
export const Window = "Window" as const;
export const ErrorBoundaryComponent = "ErrorBoundary" as const;
export const View = "View" as const;
export const SafeAreaView = "SafeAreaView" as const;
export const Text = "Text" as const;
export const Button = "Button" as const;
export const Pressable = "Pressable" as const;
export const TouchableOpacity = "TouchableOpacity" as const;
export const ActivityIndicator = "ActivityIndicator" as const;
export const Switch = "Switch" as const;
export const Modal = "Modal" as const;
export const Slider = "Slider" as const;
export const ProgressBar = "ProgressBar" as const;
export const CheckBox = "CheckBox" as const;
export const RadioButton = "RadioButton" as const;
export const DatePicker = "DatePicker" as const;
export const Image = "Image" as const;
export const TextInput = "TextInput" as const;
export const ScrollView = "ScrollView" as const;
export const Video = "Video" as const;
