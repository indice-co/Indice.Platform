import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { APP_LANGUAGES, MenuOption, ToastType } from '@indice/ng-components';
import { EMPTY, Subscription, catchError, map, Subject, combineLatest, takeUntil } from 'rxjs';

import { MessageTypeResultSet, MessagesApiClient, Template } from 'src/app/core/services/messages-api.service';
import { TemplateEditStore } from '../../template-edit-store.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';

@Component({
    selector: 'app-campaign-details-edit-rightpane',
    templateUrl: './template-edit-details-rightpane.component.html',
    standalone: false
})
export class TemplateDetailsEditRightpaneComponent implements OnInit, AfterViewInit, OnDestroy {
  private _updateTemplateSubscription: Subscription | undefined;
  private _templateId = '';

  constructor(
    private _templateStore: TemplateEditStore,
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient,
    @Inject(APP_LANGUAGES) private _lang: AppLanguagesService,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster
  ) { }

  @ViewChild('editNameTemplate', { static: true }) public editNameTemplate!: TemplateRef<any>;
  @ViewChild('editUserPreferenceTemplate', { static: true }) public editUserPreferenceTemplate!: TemplateRef<any>;
  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  @ViewChild('editMessageType', { static: true }) public editMessageType!: TemplateRef<any>;

  public submitInProgress = false;
  public templateOutlet!: TemplateRef<any>;
  public model = new Template();
  public selectedOption: MenuOption | null = null;
  // Fallback placeholder uses translation key; will be replaced reactively.
  public messageTypes: MenuOption[] = [new MenuOption('Templates.SelectPlaceholder', null)];
  public action = 'editName';

  private _destroy$ = new Subject<void>();

  public ngOnInit(): void {
    this._templateId = this._router.url.split('/')[2];
    this._activatedRoute.queryParams.subscribe((queryParams: Params) => {
        this._selectTemplate(queryParams.action || 'editName');
      });

    // Reactive translation for the placeholder option
    combineLatest([this._lang.translateKey('Templates.SelectPlaceholder')])
      .pipe(takeUntil(this._destroy$))
      .subscribe(([placeholder]) => {
        if (this.messageTypes.length > 0) {
          this.messageTypes[0] = new MenuOption(placeholder || 'Templates.SelectPlaceholder', null);
        }
      });
  }

  public ngAfterViewInit(): void {
    this._templateStore
      .getTemplate(this._templateId)
      .pipe(takeUntil(this._destroy$))
      .subscribe((template: Template) => {
        this.model = template;
        if (this.model?.messageType?.id) {
          this.selectedOption = new MenuOption(this.model.messageType.name || '', this.model.messageType.id, undefined, null, `dot dot-${this.model.messageType.classification}`);
        }
      });
    this._changeDetector.detectChanges();
  }

  public ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
    this._updateTemplateSubscription?.unsubscribe();
  }

  public onSubmit(): void {
    this.submitInProgress = true;
    if (this.action == 'editUserPreference') {
      this._updateTemplateSubscription = this._templateStore
        .updateUserPreference(this._templateId, this.model)
        .pipe(
          catchError(() => {
            this.submitInProgress = false;
            return EMPTY;
          }))
        .subscribe({
          next: () => {
            this.submitInProgress = false;
            this._toaster.show(ToastType.Success, 'Templates.UpdateSuccessTitle', 'Templates.UpdateSuccessMessage', undefined, { name: this.model.name });
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates', this._templateId]));
          }
        });
    } if (this.action == 'editMessageType') {
      this._updateTemplateSubscription = this._templateStore
        .updateTemplateMessageType(this._templateId, this.selectedOption?.value ?? undefined)
        .pipe(
          catchError(() => {
            this.submitInProgress = false;
            return EMPTY;
          }))
        .subscribe({
          next: () => {
            this.submitInProgress = false;
            this._toaster.show(ToastType.Success, 'Templates.UpdateSuccessTitle', 'Templates.UpdateSuccessMessage', undefined, { name: this.model.name });
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates', this._templateId]));
          }
        });
    } else {
      this._updateTemplateSubscription = this._templateStore
        .updateTemplate(this._templateId, this.model)
        .pipe(
          catchError(() => {
            this.submitInProgress = false;
            return EMPTY;
          }))
        .subscribe({
          next: () => {
            this.submitInProgress = false;
            this._toaster.show(ToastType.Success, 'Templates.UpdateSuccessTitle', 'Templates.UpdateSuccessMessage', undefined, { name: this.model.name });
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates', this._templateId]));
          }
        });
    }
  }

  private _selectTemplate(action: string): void {
    switch (action) {
      case 'editName':
        this.templateOutlet = this.editNameTemplate;
        break;
      case 'editUserPreference':
        this.action = 'editUserPreference';
        this.templateOutlet = this.editUserPreferenceTemplate;
        break;
      case 'editMessageType':
        this.action = 'editMessageType';
        this.templateOutlet = this.editMessageType;
        this._loadMessageTypes();
        break;
    }
  }

  private _loadMessageTypes(): void {
    this._api
      .getMessageTypes()
      .pipe(map((messageTypes: MessageTypeResultSet) => {
          if (messageTypes.items) {
          this.messageTypes.push(...messageTypes.items.map(type => new MenuOption(type.name || '', type.id, undefined, type, `dot dot-${type.classification}`)));
          }
      }))
      .subscribe();
  }

  protected setType(event: MenuOption): void {
    this.selectedOption = event;
  }
}
