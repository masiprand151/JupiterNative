import { Child, ErrorBoundaryProps } from './types';

export function ErrorBoundary(props: ErrorBoundaryProps): Child {
  return props.children ?? null;
}
