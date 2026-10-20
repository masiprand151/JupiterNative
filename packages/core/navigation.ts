import {
  ComponentType,
  NavigationApi,
  NavigationParams,
  NavigationProps,
  NavigationScreenProps,
  NavigationRoute,
} from "./types";
import { useState } from "./state";

let navigator: Navigator | undefined;

class Navigator {
  readonly stack: NavigationRoute[];
  readonly navigation: NavigationApi;

  constructor(
    public screens: Record<string, ComponentType<NavigationScreenProps>>,
    initial: string,
    private update: () => void,
  ) {
    if (!screens[initial])
      throw new Error(`Jupiter route not found: ${initial}`);
    this.stack = [{ name: initial }];
    this.navigation = {
      navigate: (route, params) => this.navigate(route, params),
      goBack: () => this.back(),
      replace: (route, params) => this.replace(route, params),
    };
  }

  current() {
    return this.stack[this.stack.length - 1];
  }
  navigate(route: string, params?: NavigationParams) {
    this.assertRoute(route);
    this.stack.push({ name: route, params });
    this.update();
  }
  back() {
    if (this.stack.length > 1) {
      this.stack.pop();
      this.update();
    }
  }
  replace(route: string, params?: NavigationParams) {
    this.assertRoute(route);
    this.stack[this.stack.length - 1] = { name: route, params };
    this.update();
  }
  private assertRoute(route: string) {
    if (!this.screens[route])
      throw new Error(`Jupiter route not found: ${route}`);
  }
}

export function NavigationContainer(props: NavigationProps) {
  const [, rerender] = requireState();
  if (!navigator || navigator.screens !== props.screens)
    navigator = new Navigator(props.screens, props.initialRouteName, () =>
      rerender((value) => value + 1),
    );
  const route = navigator.current();
  const Screen = navigator.screens[route.name];
  return {
    type: Screen,
    props: { route, navigation: navigator.navigation },
    children: [],
  };
}

export function navigate(route: string, params?: NavigationParams) {
  if (!navigator) throw new Error("Jupiter navigation is not mounted.");
  navigator.navigate(route, params);
}
export function goBack() {
  navigator?.back();
}
export function replace(route: string, params?: NavigationParams) {
  if (!navigator) throw new Error("Jupiter navigation is not mounted.");
  navigator.replace(route, params);
}
function requireState(): [
  number,
  (value: number | ((previous: number) => number)) => void,
] {
  return useState(0);
}
