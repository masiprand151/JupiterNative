import { beginHooks, endHooks, flushEffects, EffectTask } from './state'; import { Child, ComponentType, Fragment, VNode } from './types';
export interface Bridge { mount(tree: unknown): void; update(tree: unknown): void; }
type Instance = { hooks: { value?: unknown; deps?: unknown[]; cleanup?: () => void; kind?: 'state' | 'effect' }[]; rerender: () => void };
export function render(root: Child, bridge: Bridge) {
  const instances = new WeakMap<Function, Instance>();
  let tree: unknown;
  const update = () => { const effects: EffectTask[] = []; tree = resolve(root, bridge, update, instances, effects); bridge.update(tree); flushEffects(effects); };
  const effects: EffectTask[] = [];
  tree = resolve(root, bridge, update, instances, effects);
  bridge.mount(tree);
  flushEffects(effects);
  return { update };
}
function resolve(value: Child, bridge: Bridge, update: () => void, instances: WeakMap<Function, Instance>, effects: EffectTask[]): unknown { if (Array.isArray(value)) return value.map((x) => resolve(x, bridge, update, instances, effects)); if (typeof value !== 'object' || value === null) return value; const node = value as VNode; if (node.type === Fragment) return node.children.map((x) => resolve(x, bridge, update, instances, effects)); if (typeof node.type === 'function') { let instance = instances.get(node.type); if (!instance) { instance = { hooks: [], rerender: update }; instances.set(node.type, instance); } beginHooks(instance, effects); const result = (node.type as ComponentType)({ ...node.props, children: node.children }); endHooks(); return resolve(result, bridge, update, instances, effects); } return { type: node.type, props: node.props, children: node.children.map((x) => resolve(x, bridge, update, instances, effects)) }; }
