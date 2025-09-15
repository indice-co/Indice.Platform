import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { HeaderMetaItem, Icons, MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { CreateTemplateRequest, MessageContent, MessagesApiClient, MessageTypeResultSet } from 'src/app/core/services/messages-api.service';
import { CampaignContentComponent } from '../../campaigns/create/steps/content/campaign-content.component';
import { catchError, EMPTY } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-template-create',
  templateUrl: './template-create.component.html'
})
export class TemplateCreateComponent implements OnInit, AfterViewChecked {
  @ViewChild('templateContent', { static: true }) private _templateContent: CampaignContentComponent | undefined;

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient,
    @Inject(ToasterService) private _toaster: ToasterService,
    private _router: Router
  ) {

    this.template.messageTypeId = '';
  }
  public selectedOption = new MenuOption('Παρακαλώ επιλέξτε...', null, undefined, {});
  public messageTypes: MenuOption[] = [];
  
  public metaItems: HeaderMetaItem[] | null = [];

  public basicInfoData: any = {};
  public saveInProgress = false;
  public template = new CreateTemplateRequest();
  public content: { [key: string]: MessageContent; } | undefined = {
    'inbox': new MessageContent()
  };

  public ngOnInit(): void {
    this.metaItems = [
      { key: 'info', icon: Icons.Details, text: 'Ακολουθήστε τα παρακάτω βήματα για να δημιουργήσετε ένα νέο πρότυπο.' }
    ];
    this._loadMessageTypes();
  }

  public ngAfterViewChecked(): void {
    this._changeDetector.detectChanges();
  }

  public saveTemplate(): void {
    this.saveInProgress = true;
    const name = 'test-name';
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
        this._toaster.show(ToastType.Success, 'Επιτυχής ενημέρωση', `Το πρότυπο με όνομα '${name}' δημιουργήθηκε με επιτυχία.`);
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
