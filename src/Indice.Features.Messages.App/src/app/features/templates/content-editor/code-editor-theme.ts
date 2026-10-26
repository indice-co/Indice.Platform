import { syntaxHighlighting } from '@codemirror/language';
import { oneDarkHighlightStyle } from '@codemirror/theme-one-dark';
import { EditorView } from '@codemirror/view';

/**
 * CodeMirror theme built from the daisyUI theme variables, so the editor follows the app's light / dark switch.
 * In dark mode the One Dark syntax colors replace CodeMirror's default (light) highlight style.
 */
const muted = 'color-mix(in oklab, var(--color-base-content) 45%, transparent)';
const selection = 'color-mix(in oklab, var(--color-primary) 25%, transparent)';

const themeSpec = {
  '&': { color: 'var(--color-base-content)', backgroundColor: 'transparent', height: '100%' },
  // the surrounding daisyUI field draws the focus ring
  '&.cm-focused': { outline: 'none' },
  '.cm-scroller': { fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace', lineHeight: '1.6' },
  '.cm-content': { caretColor: 'var(--color-base-content)' },
  '.cm-cursor, .cm-dropCursor': { borderLeftColor: 'var(--color-base-content)' },
  '&.cm-focused > .cm-scroller > .cm-selectionLayer .cm-selectionBackground, .cm-selectionBackground, .cm-content ::selection': { backgroundColor: selection },
  '.cm-activeLine': { backgroundColor: 'color-mix(in oklab, var(--color-base-content) 4%, transparent)' },
  '.cm-gutters': { backgroundColor: 'var(--color-base-200)', color: muted, borderRight: '1px solid var(--color-base-300)' },
  '.cm-activeLineGutter': { backgroundColor: 'var(--color-base-300)', color: 'var(--color-base-content)' },
  '&:not(.cm-focused) .cm-activeLine, &:not(.cm-focused) .cm-activeLineGutter': { backgroundColor: 'transparent', color: 'inherit' },
  '&.cm-focused .cm-matchingBracket': { backgroundColor: selection },
  '.cm-foldPlaceholder': { backgroundColor: 'var(--color-base-300)', border: 'none', color: muted },
  '.cm-placeholder': { color: muted },
  '.cm-tooltip': { backgroundColor: 'var(--color-base-100)', color: 'var(--color-base-content)', border: '1px solid var(--color-base-300)' },
  '.cm-panels': { backgroundColor: 'var(--color-base-200)', color: 'var(--color-base-content)' }
};

export const codeEditorLightTheme = EditorView.theme(themeSpec);
export const codeEditorDarkTheme = [EditorView.theme(themeSpec, { dark: true }), syntaxHighlighting(oneDarkHighlightStyle)];
