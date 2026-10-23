import { Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { map } from 'rxjs';
import { LucideChevronRight } from '@lucide/angular';
import { CasesApiService, CaseTypePartial } from 'src/app/core/services/cases-api.service';

interface CaseTypeCategory {
  /** Category name, or undefined for case types without a category. */
  name?: string;
  caseTypes: CaseTypePartial[];
}

/** Lists the case types the user can create, grouped by category, and lets them pick one. */
@Component({
  selector: 'app-select-case-type',
  imports: [TranslatePipe, LucideChevronRight],
  template: `
    @if (selected(); as caseType) {
      <div class="flex items-center justify-between gap-3 rounded-box border border-primary/40 bg-primary/5 px-4 py-3">
        <div class="min-w-0">
          <p class="truncate font-medium">{{ caseType.title }}</p>
          @if (caseType.description) {
            <p class="truncate text-sm text-base-content/70">{{ caseType.description }}</p>
          }
        </div>
        <button type="button" class="btn btn-ghost btn-sm" (click)="select(undefined)">{{ 'cases.create.change' | translate }}</button>
      </div>
    } @else if (categories(); as categories) {
      @if (loading()) {
        <div class="space-y-2">
          @for (i of [1, 2, 3, 4]; track i) { <div class="skeleton h-11 w-full"></div> }
        </div>
      } @else if (!categories.length) {
        <p class="text-sm text-base-content/70">{{ 'cases.create.noCaseTypes' | translate }}</p>
      } @else {
        <div class="space-y-5">
          @for (category of categories; track category.name) {
            <section>
              <h3 class="mb-1.5 text-sm font-medium text-base-content/60">{{ category.name ?? ('cases.create.otherCategory' | translate) }}</h3>
              <ul class="divide-y divide-base-300 overflow-hidden rounded-box border border-base-300">
                @for (caseType of category.caseTypes; track caseType.id) {
                  <li>
                    <button type="button" class="flex w-full items-center gap-3 px-4 py-3 text-left hover:bg-base-200 focus-visible:bg-base-200 focus-visible:outline-none"
                      (click)="select(caseType)">
                      <span class="min-w-0 flex-1 truncate font-medium">{{ caseType.title }}</span>
                      <svg lucideChevronRight class="size-4 shrink-0 text-base-content/50"></svg>
                    </button>
                  </li>
                }
              </ul>
            </section>
          }
        </div>
      }
    }
  `
})
export class SelectCaseTypeComponent {
  private readonly _api = inject(CasesApiService);
  /** Code of a case type to select once the list has loaded (ignored when the user cannot create it). */
  public readonly preselectedCode = input<string>();
  public readonly selectedCaseType = output<CaseTypePartial | undefined>();

  private readonly _caseTypes = toSignal(this._api.getCaseTypesList(true).pipe(map(result => result.items ?? [])));
  protected readonly loading = computed(() => this._caseTypes() === undefined);
  protected readonly selected = signal<CaseTypePartial | undefined>(undefined);

  protected readonly categories = computed<CaseTypeCategory[]>(() => {
    const groups = new Map<string, CaseTypeCategory>();
    for (const caseType of this._caseTypes() ?? []) {
      const name = caseType.category?.name;
      const key = name ?? '';
      if (!groups.has(key)) {
        groups.set(key, { name, caseTypes: [] });
      }
      groups.get(key)!.caseTypes.push(caseType);
    }
    // uncategorised case types go last
    return [...groups.values()].sort((a, b) => (a.name === undefined ? 1 : 0) - (b.name === undefined ? 1 : 0));
  });

  constructor() {
    effect(() => {
      const code = this.preselectedCode();
      const caseType = code ? this._caseTypes()?.find(c => c.code === code) : undefined;
      if (caseType) {
        untracked(() => this.select(caseType));
      }
    });
  }

  protected select(caseType: CaseTypePartial | undefined): void {
    this.selected.set(caseType);
    this.selectedCaseType.emit(caseType);
  }
}
