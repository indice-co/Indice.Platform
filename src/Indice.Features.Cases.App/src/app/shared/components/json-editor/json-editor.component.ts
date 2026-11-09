import { Component, OnInit, computed, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CodeEditor } from '@acrodata/code-editor';
import { EditorView } from 'codemirror';
import { json, jsonParseLinter } from '@codemirror/lang-json';
import { HighlightStyle, syntaxHighlighting } from '@codemirror/language';
import { linter } from '@codemirror/lint';
import { tags } from '@lezer/highlight';
import { ThemeService } from 'src/app/layout/theme.service';

/**
 * JSON code editor (CodeMirror) with syntax highlighting, bracket matching, folding and inline parse errors.
 * Colors come from the daisyUI theme variables, so it follows the app's light / dark switch.
 */
@Component({
  selector: 'app-json-editor',
  imports: [FormsModule, CodeEditor],
  template: `
    <div class="textarea textarea-sm w-full resize-y overflow-hidden p-0 font-mono" [class.textarea-error]="invalid()" [style.height.em]="rows() * 1.6 + 1">
      <!-- ngModel only writes values that did not come from the editor, so typing does not reset the cursor -->
      <code-editor class="h-full" setup="basic" indentUnit="  " [theme]="theme()" [extensions]="extensions"
        [ngModel]="value()" (ngModelChange)="valueChange.emit($event)" />
    </div>
  `
})
export class JsonEditorComponent implements OnInit {
  private readonly _theme = inject(ThemeService);

  public readonly value = input('');
  public readonly valueChange = output<string>();
  /** Visible height in lines; the user can resize it vertically. */
  public readonly rows = input(8);
  public readonly invalid = input(false);
  /** Id of the element that labels the editor. */
  public readonly labelledBy = input<string>();

  protected readonly theme = computed(() => this._theme.isDark() ? darkTheme : lightTheme);
  // set once: the wrapper resets its theme when the extensions change
  protected extensions: CodeEditor['extensions'] = [];

  public ngOnInit(): void {
    const labelledBy = this.labelledBy();
    this.extensions = [
      json(),
      jsonLinter,
      syntaxHighlighting(highlightStyle),
      labelledBy ? EditorView.contentAttributes.of({ 'aria-labelledby': labelledBy }) : []
    ];
  }
}

const parseJson = jsonParseLinter();
// empty documents are allowed (optional fields), so only lint when there is something to parse
const jsonLinter = linter(view => view.state.doc.toString().trim() ? parseJson(view) : []);

const highlightStyle = HighlightStyle.define([
  { tag: tags.propertyName, color: 'var(--color-primary)' },
  { tag: tags.string, color: 'var(--color-secondary)' },
  { tag: tags.number, color: 'color-mix(in oklab, var(--color-warning) 75%, var(--color-base-content))' },
  { tag: [tags.bool, tags.null], color: 'color-mix(in oklab, var(--color-error) 85%, var(--color-base-content))' },
  { tag: [tags.brace, tags.squareBracket, tags.separator], color: 'color-mix(in oklab, var(--color-base-content) 55%, transparent)' }
]);

const muted = 'color-mix(in oklab, var(--color-base-content) 45%, transparent)';
const selection = 'color-mix(in oklab, var(--color-primary) 25%, transparent)';

const themeSpec = {
  '&': { color: 'var(--color-base-content)', backgroundColor: 'transparent' },
  // the surrounding daisyUI field draws the focus ring
  '&.cm-focused': { outline: 'none' },
  '.cm-scroller': { fontFamily: 'inherit', lineHeight: '1.6' },
  '.cm-content': { caretColor: 'var(--color-base-content)' },
  '.cm-cursor, .cm-dropCursor': { borderLeftColor: 'var(--color-base-content)' },
  '&.cm-focused > .cm-scroller > .cm-selectionLayer .cm-selectionBackground, .cm-selectionBackground, .cm-content ::selection': { backgroundColor: selection },
  '.cm-activeLine': { backgroundColor: 'color-mix(in oklab, var(--color-base-content) 4%, transparent)' },
  '.cm-gutters': { backgroundColor: 'var(--color-base-200)', color: muted, borderRight: '1px solid var(--color-base-300)' },
  '.cm-activeLineGutter': { backgroundColor: 'var(--color-base-300)', color: 'var(--color-base-content)' },
  // a form shows several editors: only the focused one marks its cursor line
  '&:not(.cm-focused) .cm-activeLine, &:not(.cm-focused) .cm-activeLineGutter': { backgroundColor: 'transparent', color: 'inherit' },
  '&.cm-focused .cm-matchingBracket': { backgroundColor: selection },
  '.cm-foldPlaceholder': { backgroundColor: 'var(--color-base-300)', border: 'none', color: muted },
  '.cm-tooltip': { backgroundColor: 'var(--color-base-100)', color: 'var(--color-base-content)', border: '1px solid var(--color-base-300)' },
  '.cm-panels': { backgroundColor: 'var(--color-base-200)', color: 'var(--color-base-content)' }
};

// same rules, but the dark flag switches CodeMirror's own built-in pieces (search panel, buttons) to dark
const lightTheme = EditorView.theme(themeSpec);
const darkTheme = EditorView.theme(themeSpec, { dark: true });
