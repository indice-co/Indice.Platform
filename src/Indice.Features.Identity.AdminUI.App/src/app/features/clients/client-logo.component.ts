import { Component, computed, input, linkedSignal } from '@angular/core';

/** Client logo, falling back to the client's initials when there is no logo or it fails to load. */
@Component({
  selector: 'app-client-logo',
  host: { class: 'inline-flex shrink-0' },
  template: `
    <span class="grid place-items-center overflow-hidden rounded-field border border-base-300 bg-base-100 font-semibold text-primary"
      [style.width.px]="size()" [style.height.px]="size()" [style.font-size.px]="size() * 0.38">
      @if (showImage()) {
        <img [src]="src()" alt="" class="size-full object-contain p-1" (error)="broken.set(true)" />
      } @else {
        <span class="grid size-full place-items-center bg-primary/10">{{ initials() }}</span>
      }
    </span>
  `
})
export class ClientLogoComponent {
  public readonly src = input<string | null | undefined>();
  public readonly name = input<string | null | undefined>('');
  public readonly size = input(32);

  protected readonly broken = linkedSignal({ source: this.src, computation: () => false });
  protected readonly showImage = computed(() => !!this.src() && !this.broken());
  protected readonly initials = computed(() => {
    const words = (this.name() ?? '').replace(/[^\p{L}\p{N}\s]/gu, ' ').trim().split(/\s+/).filter(Boolean);
    return (words.length > 1 ? words[0][0] + words[1][0] : (words[0] ?? '?').slice(0, 2)).toUpperCase();
  });
}
