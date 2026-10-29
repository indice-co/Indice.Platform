import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, filter, forkJoin, of, switchMap } from 'rxjs';
import { LucideLink, LucidePlus, LucideSearch, LucideUnlink } from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import {
  AddRelatedCasesRequest, Case, CasePartial, CasesApiService, UpdateCaseRelationKeyRequest
} from 'src/app/core/services/cases-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { SidePaneLayoutComponent } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

/**
 * Side pane (administrators): the relation key of a case and the cases sharing it. Changing or clearing the key takes the
 * case out of its relation (the others stay related); added cases take the key, leaving any relation they had.
 */
@Component({
  selector: 'app-case-relations-pane',
  imports: [TranslatePipe, SidePaneLayoutComponent, LucideLink, LucidePlus, LucideSearch, LucideUnlink],
  template: `
    <app-side-pane-layout [title]="'caseDetails.related.title' | translate" [subtitle]="'caseRelations.subtitle' | translate">
      @if (case(); as caseModel) {
        <div class="space-y-8">
          <section>
            <h3 class="mb-1 font-semibold">{{ 'caseDetails.relationKey' | translate }}</h3>
            <p class="mb-3 text-sm text-base-content/70">{{ 'caseRelations.keyHint' | translate }}</p>
            <form class="join w-full" (submit)="$event.preventDefault(); saveKey()">
              <label class="input join-item w-full">
                <svg lucideLink class="size-4 opacity-60"></svg>
                <input type="text" class="docket grow" name="relationKey" maxlength="256" [value]="relationKey()"
                  (input)="relationKey.set($any($event.target).value)" [attr.aria-label]="'caseDetails.relationKey' | translate" />
              </label>
              <button type="submit" class="btn btn-primary join-item" [disabled]="!keyChanged() || busy()">
                @if (busy() === 'key') { <span class="loading loading-spinner loading-xs"></span> }
                {{ 'common.save' | translate }}
              </button>
            </form>
          </section>

          <section>
            <h3 class="mb-3 flex items-center gap-2 font-semibold">
              {{ 'caseDetails.related.title' | translate }}
              <span class="badge badge-sm badge-ghost">{{ related().length }}</span>
            </h3>
            @if (related().length) {
              <ul class="divide-y divide-base-300 overflow-hidden rounded-box border border-base-300">
                @for (relatedCase of related(); track relatedCase.id) {
                  <li class="flex items-center gap-3 py-2.5 pl-4 pr-2">
                    <span class="min-w-0 flex-1">
                      <span class="docket block font-semibold">{{ caseLabel(relatedCase) }}</span>
                      <span class="block truncate text-sm text-base-content/70">{{ caseLine(relatedCase) }}</span>
                    </span>
                    <button type="button" class="btn btn-ghost btn-sm btn-square text-error" [disabled]="!!busy()" (click)="remove(relatedCase)"
                      [title]="'caseRelations.remove' | translate" [attr.aria-label]="('caseRelations.remove' | translate) + ' ' + caseLabel(relatedCase)">
                      @if (busy() === relatedCase.id) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideUnlink class="size-4"></svg> }
                    </button>
                  </li>
                }
              </ul>
            } @else {
              <p class="rounded-box bg-base-200 px-4 py-3 text-sm text-base-content/70">{{ 'caseRelations.noRelated' | translate }}</p>
            }
          </section>

          <section>
            <h3 class="mb-1 font-semibold">{{ 'caseRelations.add.title' | translate }}</h3>
            @if (caseModel.relationKey) {
              <p class="mb-3 text-sm text-base-content/70">{{ 'caseRelations.add.hint' | translate }}</p>
              <form class="join w-full" (submit)="$event.preventDefault(); search()">
                <label class="input join-item w-full">
                  <svg lucideSearch class="size-4 opacity-60"></svg>
                  <input type="search" class="grow" name="term" minlength="3" [placeholder]="'caseRelations.add.placeholder' | translate"
                    [value]="term()" (input)="term.set($any($event.target).value)" />
                </label>
                <button type="submit" class="btn btn-primary join-item" [disabled]="term().trim().length < 3 || searching()">
                  @if (searching()) { <span class="loading loading-spinner loading-xs"></span> }
                  {{ 'common.search' | translate }}
                </button>
              </form>

              @if (results(); as results) {
                @if (results.length) {
                  <ul class="mt-4 divide-y divide-base-300 overflow-hidden rounded-box border border-base-300">
                    @for (result of results; track result.id) {
                      @let alreadyRelated = result.relationKey === caseModel.relationKey;
                      <li class="flex items-center gap-3 py-2.5 pl-4 pr-2">
                        <span class="min-w-0 flex-1">
                          <span class="docket block font-semibold">{{ caseLabel(result) }}</span>
                          <span class="block truncate text-sm text-base-content/70">{{ caseLine(result) }}</span>
                          @if (result.relationKey && !alreadyRelated) {
                            <span class="badge badge-sm badge-warning badge-soft mt-1 max-w-full">
                              <span class="truncate">{{ 'caseRelations.add.otherRelation' | translate: { relationKey: result.relationKey } }}</span>
                            </span>
                          }
                        </span>
                        @if (alreadyRelated) {
                          <span class="badge badge-sm badge-ghost shrink-0">{{ 'caseRelations.add.alreadyRelated' | translate }}</span>
                        } @else {
                          <button type="button" class="btn btn-ghost btn-sm shrink-0" [disabled]="!!busy()" (click)="add(result)">
                            @if (busy() === result.id) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucidePlus class="size-4"></svg> }
                            {{ 'caseRelations.add.action' | translate }}
                          </button>
                        }
                      </li>
                    }
                  </ul>
                } @else {
                  <p class="mt-4 rounded-box bg-base-200 px-4 py-3 text-sm text-base-content/70">{{ 'caseRelations.add.noResults' | translate }}</p>
                }
              }
            } @else {
              <p class="rounded-box bg-base-200 px-4 py-3 text-sm text-base-content/70">{{ 'caseRelations.add.setKeyFirst' | translate }}</p>
            }
          </section>
        </div>
      } @else {
        <div class="space-y-3">
          @for (i of [1, 2, 3]; track i) { <div class="skeleton h-11 w-full"></div> }
        </div>
      }
    </app-side-pane-layout>
  `
})
export class CaseRelationsPaneComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _caseDetails = inject(CaseDetailsService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);

  /** The case whose relations are edited (`case-relations;caseId=X`). */
  public readonly caseId = input.required<string>();

  protected readonly case = signal<Case | undefined>(undefined);
  protected readonly related = signal<CasePartial[]>([]);
  protected readonly relationKey = signal('');
  protected readonly keyChanged = computed(() => this.relationKey().trim() !== (this.case()?.relationKey ?? ''));
  protected readonly term = signal('');
  protected readonly searching = signal(false);
  protected readonly results = signal<CasePartial[] | undefined>(undefined);
  /** What is being saved: 'key', or the id of the case being added or removed. */
  protected readonly busy = signal<string | undefined>(undefined);

  constructor() {
    effect(() => {
      const caseId = this.caseId();
      untracked(() => {
        this.case.set(undefined);
        this.results.set(undefined);
        this.load(caseId);
      });
    });
  }

  /** The case number, or the case id when it has none. */
  protected caseLabel(item: CasePartial): string {
    return item.referenceNumber?.toString() ?? item.id ?? '';
  }

  protected caseLine(item: CasePartial): string {
    return [item.caseType?.title ?? item.caseType?.code, item.ownerName].filter(Boolean).join(' · ');
  }

  protected saveKey(): void {
    const relationKey = this.relationKey().trim();
    // changing or clearing the key takes the case out of its relation: confirm when there is one
    const confirmed$: Observable<boolean> = this.related().length
      ? this._dialog.confirm({
        title: 'caseRelations.leaveConfirm.title',
        message: 'caseRelations.leaveConfirm.message',
        messageParams: { count: this.related().length },
        tone: 'danger'
      })
      : of(true);
    this.save('key', confirmed$.pipe(
      filter(Boolean),
      switchMap(() => this._api.updateCaseRelationKey(this.caseId(), new UpdateCaseRelationKeyRequest({ relationKey: relationKey || undefined })))
    ));
  }

  protected remove(relatedCase: CasePartial): void {
    this.save(relatedCase.id!, this._dialog.confirm({
      title: 'caseRelations.remove',
      message: 'caseRelations.removeConfirm',
      messageParams: { referenceNumber: this.caseLabel(relatedCase) },
      tone: 'danger'
    }).pipe(
      filter(Boolean),
      switchMap(() => this._api.removeRelatedCase(this.caseId(), relatedCase.id!))
    ));
  }

  protected add(result: CasePartial): void {
    // the case leaves the relation it is in: warn first
    const confirmed$: Observable<boolean> = result.relationKey
      ? this._dialog.confirm({
        title: 'caseRelations.moveConfirm.title',
        message: 'caseRelations.moveConfirm.message',
        messageParams: { referenceNumber: this.caseLabel(result), relationKey: result.relationKey }
      })
      : of(true);
    this.save(result.id!, confirmed$.pipe(
      filter(Boolean),
      switchMap(() => this._api.addRelatedCases(this.caseId(), new AddRelatedCasesRequest({ caseIds: [result.id!] })))
    ));
  }

  protected search(): void {
    this.searching.set(true);
    this._api.getCases(1, 10, undefined, this.term().trim()).subscribe({
      next: page => {
        this.results.set((page.items ?? []).filter(item => item.id !== this.caseId()));
        this.searching.set(false);
      },
      error: () => {
        this.results.set([]);
        this.searching.set(false);
      }
    });
  }

  /** Runs a change; once it is saved, reloads the pane and tells the details page. A cancelled dialog completes without a value. */
  private save(what: string, change$: Observable<void>): void {
    this.busy.set(what);
    change$.subscribe({
      next: () => {
        this._toast.success('toasts.success.title', 'caseRelations.saved');
        this._caseDetails.notifyCaseChanged(this.caseId());
        this.load(this.caseId(), true);
      },
      error: () => this.busy.set(undefined),
      complete: () => this.busy.set(undefined)
    });
  }

  private load(caseId: string, refreshResults = false): void {
    forkJoin([this._api.getCaseById(caseId), this._api.getRelatedCases(caseId)]).subscribe(([caseModel, related]) => {
      this.case.set(caseModel);
      this.related.set(related ?? []);
      this.relationKey.set(caseModel.relationKey ?? '');
      // the search results show the relation each case is in: keep them current
      if (refreshResults && this.results()) {
        this.search();
      }
    });
  }
}
