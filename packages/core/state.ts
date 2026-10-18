import { StateSetter } from './types';

export type EffectCleanup = () => void;
type Hook = { value?: unknown; deps?: unknown[]; cleanup?: EffectCleanup; kind?: 'state' | 'effect' };
export type EffectTask = { hook: Hook; effect: () => void | EffectCleanup };
let current: { hooks: Hook[]; index: number; rerender: () => void; effects: EffectTask[] } | undefined;

export function beginHooks(instance: { hooks: Hook[]; rerender: () => void }, effects: EffectTask[]) {
  current = { hooks: instance.hooks, index: 0, rerender: instance.rerender, effects };
}
export function endHooks() { current = undefined; }

export function useState<T>(initial: T | (() => T)): [T, StateSetter<T>] {
  if (!current) throw new Error('Jupiter hooks must be called inside a function component.');
  const slot = current.hooks[current.index] ?? (current.hooks[current.index] = { kind: 'state', value: typeof initial === 'function' ? (initial as () => T)() : initial });
  const index = current.index++; const rerender = current.rerender;
  return [slot.value as T, (next) => { const value = typeof next === 'function' ? (next as (v: T) => T)(slot.value as T) : next; if (!Object.is(value, slot.value)) { slot.value = value; const bridge = (globalThis as unknown as { NativeBridge?: { Schedule?: (callback: () => void) => void } }).NativeBridge; if (bridge?.Schedule) bridge.Schedule(rerender); else rerender(); } }];
}

export function useEffect(effect: () => void | EffectCleanup, dependencies?: unknown[]) {
  if (!current) throw new Error('Jupiter hooks must be called inside a function component.');
  const slot = current.hooks[current.index] ?? (current.hooks[current.index] = { kind: 'effect' });
  const previous = slot.deps;
  const changed = dependencies === undefined || previous === undefined || dependencies.length !== previous.length || dependencies.some((value, index) => !Object.is(value, previous[index]));
  slot.deps = dependencies;
  current.index++;
  if (changed) current.effects.push({ hook: slot, effect });
}

export function flushEffects(effects: EffectTask[]) {
  for (const task of effects) {
    task.hook.cleanup?.();
    const cleanup = task.effect();
    task.hook.cleanup = typeof cleanup === 'function' ? cleanup : undefined;
  }
}

export function disposeInstance(instance: { hooks: Hook[] }) {
  for (const hook of instance.hooks) {
    hook.cleanup?.();
    hook.cleanup = undefined;
  }
}
