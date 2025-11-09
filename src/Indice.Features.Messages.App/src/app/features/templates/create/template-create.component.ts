import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { HeaderMetaItem, Icons, MenuOption, ToastType } from '@indice/ng-components';
import { CreateTemplateRequest, MessageContent, MessagesApiClient, MessageTypeResultSet } from 'src/app/core/services/messages-api.service';
import { CampaignContentComponent } from '../../campaigns/create/steps/content/campaign-content.component';
import { catchError, EMPTY, Subject, combineLatest } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';

@Component({
  selector: 'app-template-create',
  templateUrl: './template-create.component.html'
})
export class TemplateCreateComponent implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('templateContent', { static: true }) private _templateContent: CampaignContentComponent | undefined;

  private _destroy$ = new Subject<void>();
  private _templateId: string = '';

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _router: Router,
    private _languages: AppLanguagesService
  ) {
    this.template.messageTypeId = '';
  }

  // Fallback initialization uses translation keys as labels until translated.
  public selectedOption = new MenuOption('Templates.SelectPlaceholder', null, undefined, {});
  public messageTypes: MenuOption[] = [];
  public metaItems: HeaderMetaItem[] | null = [];
  public basicInfoData: any = {};
  public saveInProgress = false;
  public template = new CreateTemplateRequest();
  public content: { [key: string]: MessageContent; } | undefined = {
    'inbox': new MessageContent()
  };

  public ngOnInit(): void {
    // Wizard intro meta item initialized with translation key as fallback.
    this.metaItems = [
      { key: 'info', icon: Icons.Details, text: 'Templates.CreateWizardIntro' }
    ];

    // Reactive translation for meta item text & placeholder option.
    const keysToTranslate = [
      this.metaItems[0].text,
      this.selectedOption.text
    ];
    combineLatest(keysToTranslate.map(k => this._languages.translateKey(k!)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.metaItems = [
          { key: 'info', icon: Icons.Details, text: translated[0] || this.metaItems![0].text }
        ];
        this.selectedOption = new MenuOption(translated[1] || this.selectedOption.text, null, undefined, {});
        // Ensure first element of messageTypes remains the (possibly translated) placeholder.
        if (this.messageTypes.length > 0) {
          this.messageTypes[0] = this.selectedOption;
        }
      });

    this._loadMessageTypes();
  }

  public ngAfterViewChecked(): void {
    this._changeDetector.detectChanges();
  }

  public ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public saveTemplate(): void {
    this.saveInProgress = true;
    const name = 'test-name'; // existing placeholder preserved
    const formContents = this._templateContent?.form.controls.content.value;
    const dataContents = this._templateContent?.form.controls.data.value;
    let content: { [key: string]: MessageContent; } = {};
    for (const item of formContents) {
      content[item.channel] = new MessageContent({
        title: item.subject,
        sender: item.sender,
        body: item.body
      })
    }
    this.template.content = content;
    this.template.data = JSON.parse(dataContents ?? "{}");
    this.template.messageTypeId = this.selectedOption.value;
    this._api
      .createTemplate(new CreateTemplateRequest(this.template))
      .pipe(
        catchError((error: any) => {
          this.saveInProgress = false;
          return EMPTY;
        }))
      .subscribe(_ => {
        this.saveInProgress = false;
        this._toaster.show(ToastType.Success, 'Templates.CreateSuccessTitle', 'Templates.CreateSuccessMessage', undefined, { title: name });
        this._router.navigate(['templates']);
      });
  }

  private _loadMessageTypes(): void {
    this.messageTypes.push(this.selectedOption);
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
