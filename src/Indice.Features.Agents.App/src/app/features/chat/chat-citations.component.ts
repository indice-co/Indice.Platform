import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

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
            @for (favicon of sourceFavicons(); track favicon) {
              <img [src]="favicon" alt="" width="16" height="16" class="rounded-sm" />
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
              @if (citation.sourceUrl) {
                <a
                  [href]="citation.sourceUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="shrink-0 text-base-content/40 transition hover:text-accent"
                  [attr.aria-label]="'Open source ' + citation.number"
                  title="Open source"
                >
                  <svg viewBox="0 0 24 24" fill="none" class="size-3" aria-hidden="true">
                    <path
                      d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6M15 3h6v6M10 14L21 3"
                      stroke="currentColor"
                      stroke-width="2"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                    />
                  </svg>
                </a>
              }
            </span>
          }
        </div>
      }
    </div>
  `,
})
export class ChatCitationsComponent {
  readonly citations = input<ICitation[]>([]);

  /** Favicons of the distinct source origins, for the collapsed row; citations without a parseable URL are skipped. */
  protected readonly sourceFavicons = computed(() => {
    const origins = new Set<string>();
    for (const citation of this.citations()) {
      if (!citation.sourceUrl) {
        continue;
      }
      try {
        origins.add(new URL(citation.sourceUrl).origin);
      } catch {
        // Relative or malformed source URLs carry no origin to resolve a favicon from.
      }
    }
    return Array.from(origins, (origin) => this.getFaviconUrl(origin) || `${origin}/favicon.ico`);
  });

  /**
   * Get favicon URL from a domain using Google's favicon service
   * @param {string} domain - The domain name or full URL
   * @returns {string} - Direct favicon URL
   */
  getFaviconUrl(domain: string) : string | null {
    try {
      // Extract hostname if a full URL is provided
      const hostname = new URL(domain).hostname;
      return `https://www.google.com/s2/favicons?sz=64&domain=${hostname}`;
    } catch (error) {
      return null;
    }
  }

  /** Collapsed by default; the chips render only on demand. */
  protected readonly expanded = signal(false);
}
