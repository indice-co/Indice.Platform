import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { AbstractControl, FormGroup, FormArray, FormControl, Validators } from '@angular/forms';

import { MenuOption } from '@indice/ng-components';
import { map, finalize } from 'rxjs/operators';
import { MessagesApiClient, MessageTypeResultSet, TemplateListItemResultSet } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-campaign-basic-info',
  templateUrl: './campaign-basic-info.component.html'
})
export class CampaignBasicInfoComponent implements OnInit {
  constructor(
    private _api: MessagesApiClient,
    private _datePipe: DatePipe
  ) { }

  // Input & Output parameters
  @Input() public hasTemplateLoaded: any = {};
  @Output() public templateSelected: EventEmitter<string | undefined> = new EventEmitter<string | undefined>();
  // Form Controls
  @ViewChild('typeCombobox') typeCombobox: any;
  @ViewChild('templateCombobox') templateCombobox: any;

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
  public templates: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
  public templatesForCombobox: any[] = [];
  public now: Date = new Date();
  public messageType?: string;
  public templatesLoading: boolean = false;

  public equalityPredicate = (x: any, y: any) => x && y && x.value === y.value;

  public ngOnInit(): void {
    this._initForm();
    this._loadMessageTypes();
  }

  public onCampaignStartInput(event: any): void {
    this.from.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddTHH:mm'));
  }

  public onCampaignEndInput(event: any): void {
    this.to.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddTHH:mm'));
  }

  public onMessageTypeSearch(searchTerm: string | undefined): void {
    if (!this.messageTypesForCombobox.length) {
      this._loadMessageTypes();
    }
  }

  public onMessageTypeSelectionChanged(event: any): void {
    if (event && event.value) {
      this.messageType = event.value;
      this.type.setValue(event);

      // Reset templates when message type changes
      this.templatesForCombobox = [];
      if (this.templateCombobox) {
        this.templateCombobox.selectedItems = [];
        this.templateCombobox.value = undefined;
      }
      this._loadTemplates();
    } else {
      this.messageType = undefined;
      this.type.setValue(null);
    }
  }

  public onTemplateSearch(searchTerm: string | undefined): void {
    if (this.needsTemplate.value === 'yes' && this.templatesForCombobox.length === 0) {
      this._loadTemplates();
    }
  }

  public onTemplateSelectionChanged(event: any): void {
    if (event && event.value) {
      this.template.setValue(event);

      const channelsFormArray: FormArray = this.channels as FormArray;
      channelsFormArray.clear();

      if (!this.actionLinkText.value) {
        this.actionLinkText.setValue("Click me!");
      }

      if (!this.actionLinkHref.value) {
        this.actionLinkHref.setValue("https://www.indice.gr");
      }

      if (event.data) {
        event.data.forEach((channel: string) => channelsFormArray.push(new FormControl(channel)));
      }

      this.templateSelected.emit(event.value);
    } else {
      this.template.setValue(null);
      this.templateSelected.emit(undefined);
    }
  }

  public onNeedsTemplateChanged(event: any): void {
    const value = event.target.value;

    if (value === 'yes') {
      if (this.templatesForCombobox.length === 0) {
        this._loadTemplates();
      }
      this.template.setValidators(Validators.required);
    } else {
      this.template.removeValidators(Validators.required);
      this.template.setValue(null);

      // Clear the combobox selection if it exists
      if (this.templateCombobox) {
        this.templateCombobox.selectedItems = [];
        this.templateCombobox.value = undefined;
      }
    }

    this.template.updateValueAndValidity();
    this.needsTemplate.setValue(value);
  }

  private _loadMessageTypes(): void {
    this._api
      .getMessageTypes()
      .pipe(map((messageTypes: MessageTypeResultSet) => {
        if (messageTypes.items) {
          // Keep the original menu options for backward compatibility
          this.messageTypes = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
          this.messageTypes.push(...messageTypes.items.map(type => new MenuOption(type.name || '', type.id)));

          // Transform for enhanced combobox
          this.messageTypesForCombobox = messageTypes.items.map(type => ({
            label: type.name || '',
            value: type.id,
            toString: function () { return this.label; } // Add toString method for proper string representation
          }));
        }
      }))
      .subscribe();
  }

  private _loadTemplates(): void {
    this.templatesLoading = true;

    this._api
      .getTemplates(undefined, undefined, undefined, undefined, this.messageType, true)
      .pipe(
        map((templates: TemplateListItemResultSet) => {
          if (templates.items) {
            // Keep the original menu options for backward compatibility
            this.templates = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
            this.templates.push(...templates.items.map(template =>
              new MenuOption(template.name || '', template.id, undefined, template.channels)
            ));

            // Transform for enhanced combobox
            this.templatesForCombobox = templates.items.map(template => ({
              label: template.name || '',
              value: template.id,
              data: template.channels,
              toString: function () { return this.label; } // Add toString method for proper string representation
            }));
          }
        }),
        finalize(() => {
          this.templatesLoading = false;
        })
      )
      .subscribe();
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
