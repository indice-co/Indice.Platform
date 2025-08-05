import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { EMPTY, Subscription, catchError, map } from 'rxjs';

import { MessageTypeResultSet, MessagesApiClient, Template } from 'src/app/core/services/messages-api.service';
import { TemplateEditStore } from '../../template-edit-store.service';
import { settings } from 'src/app/core/models/settings';

@Component({
  selector: 'app-campaign-details-edit-rightpane',
  templateUrl: './template-edit-details-rightpane.component.html'
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
    @Inject(ToasterService) private _toaster: ToasterService
  ) { }

  @ViewChild('editNameTemplate', { static: true }) public editNameTemplate!: TemplateRef<any>;
  @ViewChild('editUserPreferenceTemplate', { static: true }) public editUserPreferenceTemplate!: TemplateRef<any>;
  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  @ViewChild('editMessageType', { static: true }) public editMessageType!: TemplateRef<any>;
  
  public submitInProgress = false;
  public templateOutlet!: TemplateRef<any>;
  public model = new Template();
  public selectedOption: MenuOption | null = null;
  public messageTypes: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
  public action = 'editName';
  public ngOnInit(): void {
    this._templateId = this._router.url.split('/')[2];
    this._activatedRoute.queryParams.subscribe((queryParams: Params) => {
      this._selectTemplate(queryParams.action || 'editName');
    });
  }

  public ngAfterViewInit(): void {
    this._templateStore
      .getTemplate(this._templateId)
      .subscribe((template: Template) => {
        this.model = template;
        if (this.model?.messageType?.id) {
          this.selectedOption = new MenuOption(this.model.messageType.name || '', this.model.messageType.id);
        }
      });
    this._changeDetector.detectChanges();
  }

  public ngOnDestroy(): void {
    this._updateTemplateSubscription?.unsubscribe();
  }

  public onSubmit(): void {
    this.submitInProgress = true;
    if (this.action == 'editUserPreference') {
      this._updateTemplateSubscription = this._templateStore
        .updateUserPreference(this._templateId, this.model)
        .pipe(
          catchError((error: any) => {
            this.submitInProgress = false;
            return EMPTY;
          }))
        .subscribe({
          next: () => {
            this.submitInProgress = false;
            this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Το πρότυπο με όνομα '${this.model.name}' αποθηκεύτηκε με επιτυχία.`);
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates', this._templateId]));
          }
        });
    } if (this.action == 'editMessageType') {
      this._updateTemplateSubscription = this._templateStore
        .updateTemplateMessageType(this._templateId, this.selectedOption?.value ?? undefined)
        .pipe(
          catchError((error: any) => {
            this.submitInProgress = false;
            return EMPTY;
          }))
        .subscribe({
          next: () => {
            this.submitInProgress = false;
            this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Το πρότυπο με όνομα '${this.model.name}' αποθηκεύτηκε με επιτυχία.`);
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates', this._templateId]));
          }
        });
    } else {
      this._updateTemplateSubscription = this._templateStore
        .updateTemplate(this._templateId, this.model)
        .pipe(
          catchError((error: any) => {
            this.submitInProgress = false;
            return EMPTY;
          }))
        .subscribe({
          next: () => {
            this.submitInProgress = false;
            this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Το πρότυπο με όνομα '${this.model.name}' αποθηκεύτηκε με επιτυχία.`);
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
          this.messageTypes.push(...messageTypes.items.map(type => new MenuOption(type.name || '', type.id)));
        }
      }))
      .subscribe();
  }
  protected setType(event: MenuOption): void {
    this.selectedOption = event;
  }
}
