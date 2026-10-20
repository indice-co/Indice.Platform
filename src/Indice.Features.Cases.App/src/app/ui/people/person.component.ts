import { Component, computed, input } from '@angular/core';

/** One of six pale tints, picked from the name, so a person wears the same one on every screen. */
export function personTint(name: string): string {
  let hash = 0;
  for (const char of name) {
    hash = (hash * 31 + char.codePointAt(0)!) >>> 0;
  }
  return `var(--cases-person-${(hash % 6) + 1})`;
}

/** Up to two initials, from the first two words of the name. */
export function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map(part => [...part][0].toUpperCase())
    .join('');
}

/**
 * A person as their initials on their own pale tint, with the name beside it unless `compact`. The tint is identity, not
 * data: it never appears in charts or legends.
 */
@Component({
  selector: 'app-person',
  template: `
    <span class="person-mark" [class.is-lg]="size() === 'lg'" [style.--tint]="tint()" aria-hidden="true">{{ initials() }}</span>
    @if (!compact()) {
      <span class="min-w-0 truncate">{{ name() }}</span>
    }
  `,
  host: {
    class: 'inline-flex max-w-full min-w-0 items-center gap-2 align-middle',
    '[attr.title]': 'compact() ? name() : null'
  }
})
export class PersonComponent {
  public readonly name = input.required<string>();
  /** Initials only; the name becomes the tooltip. */
  public readonly compact = input(false);
  public readonly size = input<'md' | 'lg'>('md');
  protected readonly tint = computed(() => personTint(this.name()));
  protected readonly initials = computed(() => initialsOf(this.name()));
}
