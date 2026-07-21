import '@testing-library/jest-dom';

// Ant Design v6 uses window.matchMedia via its Grid/responsive observer.
// jsdom does not implement matchMedia, so we provide a minimal mock.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});
