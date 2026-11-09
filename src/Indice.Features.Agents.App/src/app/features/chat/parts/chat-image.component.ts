import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { ImageReference } from './part-contracts';

/**
 * Renders an image part as a figure: the image, capped so a large asset cannot swamp the thread, plus an optional
 * caption underneath. The URL was already scheme-checked by `parseImage`.
 *
 * The caption is the image's text in both senses — the visible `<figcaption>` and the `<img>`'s `alt` — so a producer
 * supplies one string and it describes the picture wherever that description is needed. An image with no caption is
 * treated as decorative and gets an empty `alt`.
 *
 * A dead URL renders nothing rather than a broken-image glyph — the URL usually originates from a model or an external
 * document, so a 404 is an ordinary outcome, not an exceptional one.
 */
@Component({
  selector: 'app-chat-image',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible(); as figure) {
      <figure class="max-w-full">
        <img
          [src]="figure.uri"
          [alt]="figure.caption ?? ''"
          loading="lazy"
          class="max-h-96 max-w-full rounded-box border border-base-300 object-contain shadow-sm"
          (error)="failed.set(true)"
        />
        @if (figure.caption) {
          <figcaption class="mt-1.5 text-xs text-base-content/55">{{ figure.caption }}</figcaption>
        }
      </figure>
    }
  `,
})
export class ChatImageComponent {
  /** The image to render, or null when the part carried nothing renderable. */
  readonly image = input<ImageReference | null>(null);

  /** Set once the browser reports the image could not be loaded. */
  protected readonly failed = signal(false);

  protected readonly visible = computed(() => (this.failed() ? null : this.image()));
}
