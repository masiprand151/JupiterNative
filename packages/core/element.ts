import { Child, Fragment, VNode, VNodeType } from './types';
export function createElement(type: VNodeType, props: Record<string, unknown> | null, ...children: Child[]): VNode {
  const input = props ?? {};
  const key = input.key as string | number | undefined;
  const clean = { ...input }; delete clean.key;
  return { type, props: clean, key, children: flatten(children) };
}
export function jsx(type: VNodeType, props: Record<string, unknown> | null, key?: string | number): VNode {
  const input = props ?? {}; const children = input.children as Child | undefined; const clean = { ...input }; delete clean.children;
  return { type, props: clean, key, children: flatten([children]) };
}
export const jsxs = jsx;
export function flatten(values: Child[]): Child[] { const output: Child[] = []; for (const value of values) Array.isArray(value) ? output.push(...flatten(value)) : value !== null && value !== undefined && value !== false && value !== true ? output.push(value) : undefined; return output; }
export { Fragment };
