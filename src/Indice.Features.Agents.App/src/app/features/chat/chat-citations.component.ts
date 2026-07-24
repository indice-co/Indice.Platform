import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

import { ICitation } from '../../core/services/dex-api.service';

/**
 * Meta row under an assistant answer: a collapsible "N sources" arrow toggle with room for
 * projected actions (e.g. like buttons) on the same row; the citation chips expand below it.
 */
@Component({
  selector: 'app-chat-citations',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mt-1">
      <div class="flex items-center gap-2">
        @if (citations().length > 0) {
          <button
            type="button"
            class="inline-flex items-center gap-1 font-mono text-[0.7rem] text-base-content/50
                   transition hover:text-base-content/80"
            (click)="expanded.set(!expanded())"
            [attr.aria-expanded]="expanded()"
            aria-label="Toggle sources"
          >
            <svg
              viewBox="0 0 24 24"
              fill="none"
              class="size-3 transition-transform"
              [class.rotate-180]="expanded()"
              aria-hidden="true"
            >
              <path d="M6 9l6 6 6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            {{ citations().length }} {{ citations().length === 1 ? 'source' : 'sources' }}
            @for (favico of sourceFavicons; track $index){
              <img src="{{favico}}" width="16" height="16">
              }
          </button>
        }
        <div class="ml-auto">
          <ng-content />
        </div>
      </div>
      @if (expanded()) {
        <div class="mt-1.5 flex flex-wrap gap-1.5">
          @for (citation of citations(); track citation.chunkId) {
            <span
              class="inline-flex max-w-full items-center gap-1.5 rounded-selector
                     border border-base-300 bg-base-100 py-1 pl-2 pr-2.5 font-mono
                     text-[0.7rem] text-base-content/70"
              [title]="citation.title || citation.headingPath || ''"
            >
              <span class="footnote text-accent">{{ citation.number }}.</span>
              <span class="truncate">
                {{ citation.headingPath || citation.title || 'Source' }}
              </span>
            </span>
          }
        </div>
      }
    </div>
  `,
})
export class ChatCitationsComponent {
  readonly citations = input<ICitation[]>([]);
  // Convert Map to array of distinct values
  get sources(): string[] { return Array.from(new Set(this.citations().map(x => x.sourceUrl!))); }
  get sourceFavicons() { return this.sources.map(x => `${new URL(x).origin}/favicon.ico`) }
  // new URL("https://www.google.gr/test/test2?v=3")
  /** Collapsed by default; the chips render only on demand. */
  protected readonly expanded = signal(false);
}
