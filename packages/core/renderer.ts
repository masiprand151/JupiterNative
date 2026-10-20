import {
  beginHooks,
  endHooks,
  flushEffects,
  disposeInstance,
  EffectTask,
} from "./state";
import { Child, ComponentType, Fragment, VNode } from "./types";
// import { ErrorBoundary } from "./errorBoundary";
export interface Bridge {
  mount(tree: unknown): void;
  update(tree: unknown): void;
}
type Instance = {
  hooks: {
    value?: unknown;
    deps?: unknown[];
    cleanup?: () => void;
    kind?: "state" | "effect";
  }[];
  rerender: () => void;
};
export function render(root: Child, bridge: Bridge) {
  const instances = new Map<string, Instance>();
  const boundaryErrors = new Map<string, Error>();
  let previousInstances = new Set<Instance>();
  let tree: unknown;
  const update = () => {
    const effects: EffectTask[] = [];
    const activeInstances = new Set<Instance>();
    tree = resolve(
      root,
      bridge,
      update,
      instances,
      effects,
      "root",
      activeInstances,
      boundaryErrors,
    );
    for (const instance of previousInstances)
      if (!activeInstances.has(instance)) disposeInstance(instance);
    bridge.update(tree);
    flushEffects(effects);
    previousInstances = activeInstances;
  };
  const effects: EffectTask[] = [];
  const activeInstances = new Set<Instance>();
  tree = resolve(
    root,
    bridge,
    update,
    instances,
    effects,
    "root",
    activeInstances,
    boundaryErrors,
  );
  bridge.mount(tree);
  flushEffects(effects);
  previousInstances = activeInstances;
  return { update };
}
function resolve(
  value: Child,
  bridge: Bridge,
  update: () => void,
  instances: Map<string, Instance>,
  effects: EffectTask[],
  path: string,
  activeInstances: Set<Instance>,
  boundaryErrors: Map<string, Error>,
): unknown {
  if (Array.isArray(value))
    return value.map((x, index) =>
      resolve(
        x,
        bridge,
        update,
        instances,
        effects,
        `${path}.${childPath(x, index)}`,
        activeInstances,
        boundaryErrors,
      ),
    );
  if (typeof value !== "object" || value === null) return value;
  const node = value as VNode;
  if (node.type === Fragment)
    return node.children.map((x, index) =>
      resolve(
        x,
        bridge,
        update,
        instances,
        effects,
        `${path}.${childPath(x, index)}`,
        activeInstances,
        boundaryErrors,
      ),
    );
  // if (node.type === ErrorBoundary)
  //   return resolveBoundary(
  //     node,
  //     bridge,
  //     update,
  //     instances,
  //     effects,
  //     path,
  //     activeInstances,
  //     boundaryErrors,
  //   );
  if (typeof node.type === "function") {
    const key = `${path}:${node.key ?? ""}`;
    let instance = instances.get(key);
    if (!instance) {
      instance = { hooks: [], rerender: update };
      instances.set(key, instance);
    }
    activeInstances.add(instance);
    beginHooks(instance, effects);
    let result: Child;
    try {
      result = (node.type as ComponentType)({
        ...node.props,
        children: node.children,
      });
    } finally {
      endHooks();
    }
    return resolve(
      result,
      bridge,
      update,
      instances,
      effects,
      `${path}.component`,
      activeInstances,
      boundaryErrors,
    );
  }
  return {
    type: node.type,
    props: node.props,
    children: node.children.map((x, index) =>
      resolve(
        x,
        bridge,
        update,
        instances,
        effects,
        `${path}.${childPath(x, index)}`,
        activeInstances,
        boundaryErrors,
      ),
    ),
  };
}

function resolveBoundary(
  node: VNode,
  bridge: Bridge,
  update: () => void,
  instances: Map<string, Instance>,
  effects: EffectTask[],
  path: string,
  activeInstances: Set<Instance>,
  boundaryErrors: Map<string, Error>,
): unknown {
  const props = node.props as {
    fallback: Child | ((error: Error, reset: () => void) => Child);
    onError?: (error: Error) => void;
  };
  const reset = () => {
    boundaryErrors.delete(path);
    update();
  };
  const fallback = (error: Error) =>
    typeof props.fallback === "function"
      ? props.fallback(error, reset)
      : props.fallback;
  const existingError = boundaryErrors.get(path);
  if (existingError)
    return resolve(
      fallback(existingError),
      bridge,
      update,
      instances,
      effects,
      `${path}.fallback`,
      activeInstances,
      boundaryErrors,
    );
  try {
    return resolve(
      node.children,
      bridge,
      update,
      instances,
      effects,
      `${path}.children`,
      activeInstances,
      boundaryErrors,
    );
  } catch (value) {
    const error = value instanceof Error ? value : new Error(String(value));
    boundaryErrors.set(path, error);
    try {
      props.onError?.(error);
    } catch {
      /* Error reporting must not replace the fallback UI. */
    }
    return resolve(
      fallback(error),
      bridge,
      update,
      instances,
      effects,
      `${path}.fallback`,
      activeInstances,
      boundaryErrors,
    );
  }
}

function childPath(value: Child, index: number) {
  if (typeof value === "object" && value !== null && !Array.isArray(value)) {
    const key = (value as VNode).key;
    if (key !== undefined) return `key:${String(key)}`;
  }
  return String(index);
}
