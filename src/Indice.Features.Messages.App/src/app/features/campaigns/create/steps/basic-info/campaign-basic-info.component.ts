import { DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { AbstractControl, FormGroup, FormArray, FormControl, Validators } from '@angular/forms';

import { MenuOption } from '@indice/ng-components';
import { lastValueFrom } from 'rxjs';
import { MessagesApiClient, MessageTypeResultSet, TemplateListItemResultSet } from 'src/app/core/services/messages-api.service';
import { EnhancedComboboxComponent } from '@indice/ng-components';

@Component({
    selector: 'app-campaign-basic-info',
    templateUrl: './campaign-basic-info.component.html',
    standalone: false
})
export class CampaignBasicInfoComponent implements OnInit {
  constructor(
    private _api: MessagesApiClient,
    private _datePipe: DatePipe,
    private _changeDetector: ChangeDetectorRef
  ) { }

  // Input & Output parameters
  @Input() public hasTemplateLoaded: any = {};
  @Output() public templateSelected: EventEmitter<string | undefined> = new EventEmitter<string | undefined>();

  // Form Controls
  @ViewChild('typeCombobox') typeCombobox!: EnhancedComboboxComponent;
  @ViewChild('templateCombobox') templateCombobox!: EnhancedComboboxComponent;

  public get title(): AbstractControl { return this.form.get('title')!; }
  public get from(): AbstractControl { return this.form.get('from')!; }
  public get to(): AbstractControl { return this.form.get('to')!; }
  public get actionLinkText(): AbstractControl { return this.form.get('actionLinkText')!; }
  public get actionLinkHref(): AbstractControl { return this.form.get('actionLinkHref')!; }
  public get type(): AbstractControl { return this.form.get('type')!; }
  public get template(): AbstractControl { return this.form.get('template')!; }
  public get needsTemplate(): AbstractControl { return this.form.get('needsTemplate')!; }
  public get channels(): AbstractControl { return this.form.get('channels')!; }

  // Properties
  public form!: FormGroup;
  public messageTypes: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
  public messageTypesForCombobox: any[] = [];
  public messageTypesLoading: boolean = false;
  public displayMessageTypesShowMoreOption: boolean = false;

  public templates: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
  public templatesForCombobox: any[] = [];
  public templatesLoading: boolean = false;
  public displayTemplatesShowMoreOption: boolean = false;

  public now: Date = new Date();
  public messageType?: string;

  // Paging properties
  private _messageTypesPage: number = 1;
  private _templatesPage: number = 1;
  private _pageSize: number = 10;
  private _lastMessageTypeSearchTerm: string | undefined = undefined;
  private _lastTemplateSearchTerm: string | undefined = undefined;

  public equalityPredicate = (x: any, y: any) => x && y && x.id === y.id;

  public messageTypesFilter = (item: any) => {
    if (!this.typeCombobox || !this.typeCombobox.selectedItems) return true;
    const selectedItem = this.typeCombobox.selectedItems.find((x: any) => this.equalityPredicate(x, item));
    return selectedItem == null;
  };

  public templatesFilter = (item: any) => {
    if (!this.templateCombobox || !this.templateCombobox.selectedItems) return true;
    const selectedItem = this.templateCombobox.selectedItems.find((x: any) => this.equalityPredicate(x, item));
    return selectedItem == null;
  };

  public ngOnInit(): void {
    this._initForm();
  }

  public ngAfterViewInit(): void {
    this._changeDetector.detectChanges();
  }

  public onCampaignStartInput(event: any): void {
    this.from.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddTHH:mm'));
  }

  public onCampaignEndInput(event: any): void {
    this.to.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddTHH:mm'));
  }

  public async onMessageTypeSearch(searchTerm: string | undefined): Promise<void> {
    this._messageTypesPage = 1;
    this._lastMessageTypeSearchTerm = searchTerm;
    this.messageTypesLoading = true;

    try {
      const fetchedMessageTypes = await this._fetchMessageTypes(this._lastMessageTypeSearchTerm);

      if (fetchedMessageTypes.items) {
        this.messageTypesForCombobox = fetchedMessageTypes.items.map(type => ({
          name: type.name || '',
          id: type.id,
          icon: type.classification,
          toString: function () { return this.name; }
        }));

        this.displayMessageTypesShowMoreOption = fetchedMessageTypes.items.length === this._pageSize;
      }
    } catch (error) {
      console.error('Error fetching message types:', error);
    } finally {
      this.messageTypesLoading = false;
    }
  }

  public async onMessageTypesShowMore(): Promise<void> {
    this._messageTypesPage++;
    this.messageTypesLoading = true;

    try {
      const fetchedMessageTypes = await this._fetchMessageTypes(this._lastMessageTypeSearchTerm);

      if (fetchedMessageTypes.items) {
        const newItems = fetchedMessageTypes.items.map(type => ({
          name: type.name || '',
          id: type.id,
          icon: type.classification,
          toString: function () { return this.name; }
        }));

        this.messageTypesForCombobox = [...this.messageTypesForCombobox, ...newItems];

        this.displayMessageTypesShowMoreOption = fetchedMessageTypes.items.length === this._pageSize;
      }
    } catch (error) {
      console.error('Error fetching more message types:', error);
    } finally {
      this.messageTypesLoading = false;
    }
  }

  public onMessageTypeSelectionChanged(event: any): void {
    if (event && event.id) {
      this.messageType = event.id;
      this.type.setValue(event);
    } else {
      this.messageType = undefined;
      this.type.setValue(null);
    }

    if (this.needsTemplate.value === 'yes') {
      this.onTemplateSearch('');
    }
  }

  public async onTemplateSearch(searchTerm: string | undefined): Promise<void> {
    this._templatesPage = 1;
    this._lastTemplateSearchTerm = searchTerm;
    this.templatesLoading = true;

    try {
      const fetchedTemplates = await this._fetchTemplates(this._lastTemplateSearchTerm);

      if (fetchedTemplates.items) {
        this.templatesForCombobox = fetchedTemplates.items.map(template => ({
          name: template.name || '',
          id: template.id,
          channels: template.channels,
          toString: function () { return this.name; }
        }));

        this.displayTemplatesShowMoreOption = fetchedTemplates.items.length === this._pageSize;
      }
    } catch (error) {
      console.error('Error fetching templates:', error);
    } finally {
      this.templatesLoading = false;
    }
  }

  public async onTemplatesShowMore(): Promise<void> {
    this._templatesPage++;
    this.templatesLoading = true;

    try {
      const fetchedTemplates = await this._fetchTemplates(this._lastTemplateSearchTerm);

      if (fetchedTemplates.items) {
        const newItems = fetchedTemplates.items.map(template => ({
          name: template.name || '',
          id: template.id,
          channels: template.channels,
          toString: function () { return this.name; }
        }));

        this.templatesForCombobox = [...this.templatesForCombobox, ...newItems];
        this.displayTemplatesShowMoreOption = fetchedTemplates.items.length === this._pageSize;
      }
    } catch (error) {
      console.error('Error fetching more templates:', error);
    } finally {
      this.templatesLoading = false;
    }
  }

  public onTemplateSelectionChanged(event: any): void {
    if (event && event.id) {
      this.template.setValue(event);

      const channelsFormArray: FormArray = this.channels as FormArray;
      channelsFormArray.clear();

      if (!this.actionLinkText.value) {
        this.actionLinkText.setValue("Click me!");
      }

      if (!this.actionLinkHref.value) {
        this.actionLinkHref.setValue("https://www.indice.gr");
      }

      if (event.channels) {
        event.channels.forEach((channel: string) => channelsFormArray.push(new FormControl(channel)));
      }

      this.templateSelected.emit(event.id);
    } else {
      this.template.setValue(null);
      this.templateSelected.emit(undefined);
    }
  }

  public onNeedsTemplateChanged(event: any): void {
    const value = event.target.value;

    if (value === 'yes') {
      this.template.setValidators(Validators.required);
      this.template.setValue(null);
      this.template.markAsTouched();
      this.onTemplateSearch('');
    } else {
      this.template.removeValidators(Validators.required);
      this.template.setValue(null);
    }

    this.templateCombobox.selectedItems = [];
    this.templateCombobox.value = undefined;
    this.template.updateValueAndValidity();
    this.needsTemplate.setValue(value);
  }

  private async _fetchMessageTypes(searchTerm: string | undefined): Promise<MessageTypeResultSet> {
    return lastValueFrom(
      this._api.getMessageTypes(
        this._messageTypesPage,
        this._pageSize,
        'name+',
        searchTerm || ''
      )
    );
  }

  private async _fetchTemplates(searchTerm: string | undefined): Promise<TemplateListItemResultSet> {
    try {

      return lastValueFrom(
        this._api.getTemplates(
          this._templatesPage,
          this._pageSize,
          'name+',
          searchTerm || '',
          this.messageType,
          true
        )
      );
    } catch (error) {
      console.error('Error in _fetchTemplates:', error);
      throw error;
    }
  }

  private _initForm(): void {
    this.form = new FormGroup({
      title: new FormControl(undefined, [
        Validators.required,
        Validators.maxLength(128)
      ]),
      from: new FormControl(this._datePipe.transform(this.now, 'yyyy-MM-ddTHH:mm')),
      to: new FormControl(),
      actionLinkText: new FormControl(undefined, [Validators.maxLength(128)]),
      actionLinkHref: new FormControl(undefined, [
        Validators.pattern(/https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)(\/?)*$/),
        Validators.maxLength(2048)
      ]),
      type: new FormControl(),
      template: new FormControl(),
      needsTemplate: new FormControl('no'),
      channels: new FormArray([new FormControl('Inbox')], [Validators.required])
    });
  }
}
