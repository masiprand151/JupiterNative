# Jupiter

Jupiter is an experimental Windows desktop framework that uses TypeScript/TSX for application code, esbuild for bundling, ClearScript V8 for JavaScript execution, and native WPF controls for rendering.

## Commands

```powershell
npm install
npm run doctor
npm run typecheck
npm run build
npm run dev
```

The publish output is `dist/Jupiter.exe`. The runtime is self-contained for Windows x64 and embeds `runtime/App.js` as a resource.

The current milestone includes the JSX runtime, typed primitive components, `useState`, basic navigation APIs, a ClearScript host, and native WPF rendering for the sample app. Reconciliation, full callback lifecycle management, TextInput, and production-grade native event/token handling remain follow-up work.
