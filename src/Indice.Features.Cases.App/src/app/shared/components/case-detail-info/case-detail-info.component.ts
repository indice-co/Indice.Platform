import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePencil } from '@lucide/angular';

export interface InfoEntry {
  /** Translation key of the label. */
  key: string;
  value: string | null | undefined;
  /** Render the value with tabular figures (ids, reference numbers). */
  mono?: boolean;
  /** Show the entry even without a value (as a dash). */
  alwaysShow?: boolean;
  /** Router link commands of the entry's editor: adds a pen next to the value. */
  editLink?: any[];
}

/** A titled description list of case facts. Entries without a value are skipped unless `alwaysShow` is set. */
@Component({
  selector: 'app-case-detail-info',
  imports: [RouterLink, TranslatePipe, LucidePencil],
  host: { class: 'block' },
  template: `
    @if (visibleEntries().length) {
      <section class="panel p-5">
        <h2 class="mb-3 text-sm font-semibold">{{ title() }}</h2>
        <dl class="space-y-3 text-sm">
          @for (entry of visibleEntries(); track entry.key) {
            <div class="grid grid-cols-[minmax(0,2fr)_minmax(0,3fr)] gap-3">
              <dt class="text-base-content/65">{{ entry.key | translate }}</dt>
              <dd class="flex items-start justify-between gap-2">
                <span class="min-w-0 break-words font-medium" [class.docket]="entry.mono">{{ entry.value || '-' }}</span>
                @if (entry.editLink) {
                  <a class="btn btn-ghost btn-xs btn-square -my-0.5 shrink-0" [routerLink]="entry.editLink" queryParamsHandling="preserve"
                    [title]="'common.edit' | translate" [attr.aria-label]="('common.edit' | translate) + ': ' + (entry.key | translate)">
                    <svg lucidePencil class="size-3.5"></svg>
                  </a>
                }
              </dd>
            </div>
          }
        </dl>
      </section>
    }
  `
})
export class CaseDetailInfoComponent {
  public readonly title = input('');
  public readonly entries = input<InfoEntry[]>([]);
  protected readonly visibleEntries = computed(() => this.entries().filter(e => !!e.value || e.alwaysShow));
}
