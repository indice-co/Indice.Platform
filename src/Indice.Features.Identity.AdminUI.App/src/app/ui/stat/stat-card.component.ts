import { Component, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideDynamicIcon, LucideIcon } from '@lucide/angular';

export type StatTone = 'primary' | 'secondary' | 'accent' | 'success' | 'warning' | 'error' | 'info' | 'neutral';

/**
 * KPI tile: icon, label, big value and an optional hint line (`<span hint>…</span>`). With `link` the whole tile
 * navigates, e.g. to the list pre-filtered by the metric.
 */
@Component({
  selector: 'app-stat-card',
  imports: [NgTemplateOutlet, RouterLink, LucideDynamicIcon],
  host: { class: 'block' },
  template: `
    <ng-template #body>
      <div class="flex items-start justify-between gap-3">
        <p class="text-sm font-medium text-base-content/70">{{ label() }}</p>
        @if (icon()) {
          <span class="grid size-9 shrink-0 place-items-center rounded-field" [class]="toneClass()">
            <svg [lucideIcon]="icon()!" class="size-[1.125rem]"></svg>
          </span>
        }
      </div>
      <p class="mt-1 text-3xl font-semibold tracking-tight tabular">
        @if (loading()) { <span class="skeleton inline-block h-8 w-20 align-middle"></span> } @else { {{ value() ?? '—' }} }
      </p>
      <div class="mt-1 min-h-5 text-xs text-base-content/65"><ng-content select="[hint]" /></div>
    </ng-template>
    @if (link()) {
      <a [routerLink]="link()" [queryParams]="queryParams()"
        class="panel block h-full p-5 transition-colors hover:border-primary/40 focus-visible:outline-2 focus-visible:outline-primary">
        <ng-container [ngTemplateOutlet]="body" />
      </a>
    } @else {
      <div class="panel h-full p-5"><ng-container [ngTemplateOutlet]="body" /></div>
    }
  `
})
export class StatCardComponent {
  public readonly label = input.required<string>();
  public readonly value = input<string | number | null | undefined>();
  public readonly icon = input<LucideIcon>();
  public readonly tone = input<StatTone>('primary');
  public readonly loading = input(false);
  public readonly link = input<string | any[]>();
  public readonly queryParams = input<Record<string, string>>();

  protected toneClass(): string {
    return {
      primary: 'bg-primary/10 text-primary',
      secondary: 'bg-secondary/10 text-secondary',
      accent: 'bg-accent/25 text-accent-content dark:text-accent',
      success: 'bg-success/10 text-success',
      warning: 'bg-warning/15 text-warning',
      error: 'bg-error/10 text-error',
      info: 'bg-info/10 text-info',
      neutral: 'bg-base-200 text-base-content/70'
    }[this.tone()];
  }
}
