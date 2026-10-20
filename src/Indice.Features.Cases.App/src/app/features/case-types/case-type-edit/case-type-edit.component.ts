import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { switchMap, tap } from 'rxjs';
import { CasesApiService, CaseType, ICaseTypeRequest } from 'src/app/core/services/cases-api.service';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';
import { CheckpointTypesComponent } from '../../checkpoint-types/checkpoint-types.component';
import { CaseTypeFormComponent } from '../case-type-form/case-type-form.component';
import { CaseTypeUpdateService } from '../case-type-update.service';

@Component({
  selector: 'app-case-type-edit',
  imports: [RouterLink, NgIcon, TranslatePipe, PageHeaderComponent, CaseTypeFormComponent, CheckpointTypesComponent],
  template: `
    <div class="page max-w-5xl">
      <app-page-header [title]="'caseTypes.edit.title' | translate" [subtitle]="data()?.title">
        <a eyebrow routerLink="/case-types" class="link link-hover mb-1 inline-flex items-center gap-1 text-[0.8125rem] text-quiet">
          <ng-icon name="lucideArrowLeft" size="0.9rem" /> {{ 'nav.caseTypes' | translate }}
        </a>
      </app-page-header>

      @if (data(); as caseType) {
        <app-case-type-form [data]="caseType" (submitted)="onSubmit($event)" />
      } @else {
        <div class="panel panel-body space-y-4" aria-busy="true">
          <div class="skeleton h-5 w-40"></div>
          <div class="grid gap-4 sm:grid-cols-2">
            <div class="skeleton h-10"></div>
            <div class="skeleton h-10"></div>
          </div>
        </div>
      }

      @if (caseTypeId()) {
        <section class="panel mt-8 overflow-hidden" aria-labelledby="ct-checkpoints">
          <div class="panel-head"><h2 id="ct-checkpoints">{{ 'checkpointTypes.title' | translate }}</h2></div>
          <app-checkpoint-types [caseTypeId]="caseTypeId()" [embedded]="true" />
        </section>
      }
    </div>
  `
})
export class CaseTypeEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(CasesApiService);
  private readonly caseTypesService = inject(CaseTypeUpdateService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly caseTypeId = signal('');
  protected readonly data = signal<CaseType | undefined>(undefined);

  public ngOnInit(): void {
    this.route.params
      .pipe(
        tap(params => this.caseTypeId.set(params['caseTypeId'])),
        switchMap(params => this.api.getCaseTypeById(params['caseTypeId'])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(caseType => this.data.set(caseType));
  }

  protected onSubmit(event: ICaseTypeRequest): void {
    this.caseTypesService.onEditSubmit(this.caseTypeId(), event);
  }
}
