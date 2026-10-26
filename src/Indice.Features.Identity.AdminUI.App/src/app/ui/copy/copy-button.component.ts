import { Component, input, signal } from '@angular/core';
import { LucideCheck, LucideCopy } from '@lucide/angular';

/** Copies `value` to the clipboard and briefly shows a check mark. */
@Component({
  selector: 'app-copy-button',
  imports: [LucideCheck, LucideCopy],
  host: { class: 'inline-flex' },
  template: `
    <button type="button" class="btn btn-ghost btn-xs btn-square" [attr.aria-label]="'Copy ' + (label() || 'to clipboard')"
      [attr.title]="copied() ? 'Copied' : 'Copy'" (click)="copy()">
      @if (copied()) { <svg lucideCheck class="size-3.5 text-success"></svg> } @else { <svg lucideCopy class="size-3.5 opacity-70"></svg> }
    </button>
  `
})
export class CopyButtonComponent {
  public readonly value = input<string | null | undefined>('');
  public readonly label = input<string>();
  protected readonly copied = signal(false);

  protected copy(): void {
    const value = this.value();
    if (!value) {
      return;
    }
    navigator.clipboard?.writeText(value).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 1500);
    });
  }
}
