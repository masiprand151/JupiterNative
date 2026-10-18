# Jupiter Documentation

This documentation site is a standalone HTML page using Bootstrap from a CDN and does not add dependencies to the root project.

Open [index.html](./index.html) directly in a browser, or serve the `docs/` folder with any static file server.

The documentation covers:

- Architecture and the JavaScript-to-WPF rendering flow.
- Window configuration passed to `start(...)`.
- JSX components and stable controlled inputs.
- `useState`, `useEffect`, and terminal console output.
- Navigation and route parameters.
- Native window, clipboard, file, URL, and message APIs.
- Slider synchronization and WPF logical-parent troubleshooting.
- Build commands, limitations, and current runtime behavior.

The guide is split into separate pages in [`pages/`](./pages/), while [`index.html`](./index.html) remains the landing page. All documentation assets are isolated in `docs/`.
