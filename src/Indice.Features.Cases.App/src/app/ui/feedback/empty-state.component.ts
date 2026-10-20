import { Component, input } from '@angular/core';
import { NgIcon } from '@ng-icons/core';

/** What to show when a list or panel has nothing in it: an icon, a plain statement and, optionally, the next step. */
@Component({
  selector: 'app-empty-state',
  imports: [NgIcon],
  template: `
    <span class="grid size-11 place-items-center rounded-full bg-base-200 text-quiet">
      <ng-icon [name]="icon()" size="1.35rem" />
    </span>
    <p class="mt-3 font-semibold">{{ title() }}</p>
    @if (message()) {
      <p class="mt-1 max-w-sm text-quiet">{{ message() }}</p>
    }
    <div class="mt-4 empty:hidden"><ng-content /></div>
  `,
  host: { class: 'flex flex-col items-center px-6 py-12 text-center' }
})
export class EmptyStateComponent {
  public readonly icon = input('lucideInbox');
  public readonly title = input.required<string>();
  public readonly message = input<string | null | undefined>();
}
