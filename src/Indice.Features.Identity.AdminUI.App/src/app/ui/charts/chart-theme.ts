/**
 * Resolves a CSS color expression (e.g. `var(--app-channel-email)` or `#fff`) against the document root, so canvas
 * based charts can use the active theme's tokens. Must be called after the theme attribute has been applied.
 */
export function resolveColor(value: string, element: Element = document.documentElement): string {
  const match = /^var\((--[\w-]+)\)$/.exec(value.trim());
  if (!match) {
    return value;
  }
  return getComputedStyle(element).getPropertyValue(match[1]).trim() || value;
}

/** A `#rgb` / `#rrggbb` color with the given alpha (0..1) as `rgba()`. Other formats are returned unchanged. */
export function withAlpha(color: string, alpha: number): string {
  const hex = /^#([\da-f]{3}|[\da-f]{6})$/i.exec(color.trim())?.[1];
  if (!hex) {
    return color;
  }
  const full = hex.length === 3 ? hex.split('').map(c => c + c).join('') : hex;
  const [r, g, b] = [0, 2, 4].map(i => parseInt(full.slice(i, i + 2), 16));
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

/** Shared visual defaults for chart.js text and grid lines. */
export function chartChrome(element: Element = document.documentElement) {
  return {
    text: resolveColor('var(--color-base-content)', element),
    grid: resolveColor('var(--app-chart-grid)', element),
    surface: resolveColor('var(--color-base-100)', element),
    font: getComputedStyle(element).fontFamily
  };
}
