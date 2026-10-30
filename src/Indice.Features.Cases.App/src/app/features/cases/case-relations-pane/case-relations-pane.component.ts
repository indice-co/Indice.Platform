import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, firstValueFrom } from 'rxjs';
import { LucideLink, LucidePlus, LucideSearch, LucideTriangleAlert, LucideUnlink } from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import {
  AddRelatedCasesRequest, CasePartial, CasesApiService, UpdateCaseRelationKeyRequest
} from 'src/app/core/services/cases-api.service';
import { ConfirmDialogData } from 'src/app/ui/dialog/confirm-dialog.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { SidePaneLayoutComponent } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

/**
 * Side pane (administrators): the relation key of a case and the cases sharing it. Changing or clearing the key takes the
 * case out of its relation (the others stay related); added cases take the key, leaving any relation they had.
 */
@Component({
  selector: 'app-case-relations-pane',
  imports: [TranslatePipe, EmptyStateComponent, SidePaneLayoutComponent, LucideLink, LucidePlus, LucideSearch, LucideTriangleAlert, LucideUnlink],
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
              <button type="submit" class="btn btn-primary join-item" [disabled]="!keyChanged() || locked()">
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
                    <button type="button" class="btn btn-ghost btn-sm btn-square text-error" [disabled]="locked()" (click)="remove(relatedCase)"
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
                <button type="submit" class="btn btn-primary join-item" [disabled]="term().trim().length < 3 || results.isLoading()">
                  @if (results.isLoading()) { <span class="loading loading-spinner loading-xs"></span> }
                  {{ 'common.search' | translate }}
                </button>
              </form>

              @if (found(); as found) {
                @if (found.length) {
                  <ul class="mt-4 divide-y divide-base-300 overflow-hidden rounded-box border border-base-300">
                    @for (result of found; track result.id) {
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
                          <button type="button" class="btn btn-ghost btn-sm shrink-0" [disabled]="locked()" (click)="add(result)">
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
              } @else if (results.error()) {
                <p role="alert" class="mt-4 rounded-box bg-error/10 px-4 py-3 text-sm text-error">{{ 'cases.loadError.title' | translate }}</p>
              }
            } @else {
              <p class="rounded-box bg-base-200 px-4 py-3 text-sm text-base-content/70">{{ 'caseRelations.add.setKeyFirst' | translate }}</p>
            }
          </section>
        </div>
      } @else if (loadFailed()) {
        <app-empty-state [title]="'caseDetails.loadError.title' | translate" [message]="'caseDetails.loadError.message' | translate">
          <svg icon lucideTriangleAlert class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="reload()">{{ 'common.retry' | translate }}</button>
        </app-empty-state>
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

  private readonly _case = rxResource({
    params: () => this.caseId(),
    stream: ({ params }) => this._api.getCaseById(params)
  });
  private readonly _related = rxResource({
    params: () => this.caseId(),
    stream: ({ params }) => this._api.getRelatedCases(params)
  });
  /** The case, once it and its related cases are loaded. */
  protected readonly case = computed(() => this._case.hasValue() && this._related.hasValue() ? this._case.value() : undefined);
  protected readonly related = computed(() => this._related.hasValue() ? this._related.value() ?? [] : []);
  protected readonly loadFailed = computed(() => !!this._case.error() || !!this._related.error());
  /** The edited key; the stored one again whenever the case (re)loads. */
  protected readonly relationKey = linkedSignal(() => this.case()?.relationKey ?? '');
  protected readonly keyChanged = computed(() => !!this.case() && this.relationKey().trim() !== (this.case()!.relationKey ?? ''));

  protected readonly term = linkedSignal({ source: this.caseId, computation: () => '' });
  /** The submitted search: a new object on every submit, so searching the same term again reruns it. */
  private readonly _query = linkedSignal<string, { term: string } | undefined>({ source: this.caseId, computation: () => undefined });
  protected readonly results = rxResource({
    params: () => this._query(),
    stream: ({ params }) => this._api.getCases(1, 10, undefined, params.term)
  });
  protected readonly found = computed(() => this.results.hasValue()
    ? (this.results.value().items ?? []).filter(item => item.id !== this.caseId())
    : undefined);

  /** What is being saved: 'key', or the id of the case being added or removed. */
  protected readonly busy = signal<string | undefined>(undefined);
  /** No new change while one is saved or the pane reloads after it. */
  protected readonly locked = computed(() => !!this.busy() || this._case.isLoading() || this._related.isLoading());

  /** The case number, or the case id when it has none. */
  protected caseLabel(item: CasePartial): string {
    return item.referenceNumber?.toString() ?? item.id ?? '';
  }

  protected caseLine(item: CasePartial): string {
    return [item.caseType?.title ?? item.caseType?.code, item.ownerName].filter(Boolean).join(' · ');
  }

  protected reload(): void {
    this._case.reload();
    this._related.reload();
  }

  protected async saveKey(): Promise<void> {
    const caseId = this.caseId();
    const relationKey = this.relationKey().trim();
    const related = this.related().length;
    // changing or clearing the key takes the case out of its relation: confirm when there is one
    if (related && !await this.confirm({
      title: 'caseRelations.leaveConfirm.title',
      message: 'caseRelations.leaveConfirm.message',
      messageParams: { count: related },
      tone: 'danger'
    })) {
      return;
    }
    await this.save('key', caseId, this._api.updateCaseRelationKey(caseId, new UpdateCaseRelationKeyRequest({ relationKey: relationKey || undefined })));
  }

  protected async remove(relatedCase: CasePartial): Promise<void> {
    const caseId = this.caseId();
    if (!await this.confirm({
      title: 'caseRelations.remove',
      message: 'caseRelations.removeConfirm',
      messageParams: { referenceNumber: this.caseLabel(relatedCase) },
      tone: 'danger'
    })) {
      return;
    }
    await this.save(relatedCase.id!, caseId, this._api.removeRelatedCase(caseId, relatedCase.id!));
  }

  protected async add(result: CasePartial): Promise<void> {
    const caseId = this.caseId();
    // the case leaves the relation it is in: warn first
    if (result.relationKey && !await this.confirm({
      title: 'caseRelations.moveConfirm.title',
      message: 'caseRelations.moveConfirm.message',
      messageParams: { referenceNumber: this.caseLabel(result), relationKey: result.relationKey }
    })) {
      return;
    }
    await this.save(result.id!, caseId, this._api.addRelatedCases(caseId, new AddRelatedCasesRequest({ caseIds: [result.id!] })));
  }

  protected search(): void {
    this._query.set({ term: this.term().trim() });
  }

  private confirm(data: ConfirmDialogData): Promise<boolean> {
    return firstValueFrom(this._dialog.confirm(data), { defaultValue: false });
  }

  /** Saves a change, then reloads the pane (and the search results, which show each case's relation) and tells the details page. */
  private async save(what: string, caseId: string, change: Observable<void>): Promise<void> {
    this.busy.set(what);
    try {
      await firstValueFrom(change);
      this._toast.success('toasts.success.title', 'caseRelations.saved');
      this._caseDetails.notifyCaseChanged(caseId);
      this.reload();
      this.results.reload();
    } catch {
      // the problem details interceptor reports the failure
    } finally {
      this.busy.set(undefined);
    }
  }
}
