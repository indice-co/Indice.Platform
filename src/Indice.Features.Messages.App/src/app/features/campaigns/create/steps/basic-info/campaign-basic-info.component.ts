import { DatePipe } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { AbstractControl, FormGroup, FormArray, FormControl, Validators } from '@angular/forms';

import { MenuOption } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { MessageChannelKind, MessagesApiClient, MessageTypeResultSet, TemplateListItemResultSet } from 'src/app/core/services/messages-api.service';
import { ChannelState } from '../content/channel-state';

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
    @Output() public templateSelected: EventEmitter<string | undefined> = new EventEmitter<string | undefined>();
    // Form Controls
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
    public templates: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
    public now: Date = new Date();

    public channelsState: ChannelState[] = [
        { name: 'Inbox', description: 'Ειδοποίηση μέσω πρoσωπικού μήνυμα.', value: MessageChannelKind.Inbox, checked: true },
        { name: 'Push Notification', description: 'Ειδοποίηση μέσω push notification στις εγγεγραμμένες συσκευές.', value: MessageChannelKind.PushNotification, checked: false },
        { name: 'Email', description: 'Ειδοποίηση μέσω ηλεκτρονικού ταχυδρομείου', value: MessageChannelKind.Email, checked: false },
        { name: 'SMS', description: 'Ειδοποίηση μέσω σύντομου γραπτού μηνύματος.', value: MessageChannelKind.SMS, checked: false }
    ];

    public ngOnInit(): void {
        this._initForm();
        this._loadMessageTypes();
    }

    public onCampaignStartInput(event: any): void {
        this.from.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onCampaignEndInput(event: any): void {
        this.to.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onNeedsTemplateChanged(event: any): void {
        const value = event.target.value;
        if (value === 'yes') {
            if (this.templates.length <= 1) {
                this._loadTemplates();
            }
            this.template.setValidators(Validators.required);
        } else {
            this.template.removeValidators(Validators.required);
            this.template.setValue(null);
        }
        this.template.updateValueAndValidity();
        this.needsTemplate.setValue(value);
    }

    public onTemplateSelectionChanged(event: any): void {
        if (event.value) {
            this.template.setValue(event);
            const channelsFormArray: FormArray = this.channels as FormArray;
            channelsFormArray.clear();
            event.data.forEach((channel: string) => channelsFormArray.push(new FormControl(channel)));
            this.channelsState.forEach((channel: any) => channel.checked = this.channels.value.indexOf(channel.value) > -1);
        } else {
            this.template.setValue(null);
        }
        this.templateSelected.emit(event.value);
    }

    public onChannelCheckboxChange(event: any): void {
        const channelsFormArray: FormArray = this.channels as FormArray;
        const value = event.target.value;
        const checkbox = this.channelsState.find(x => x.value === value);
        if (event.target.checked) {
            channelsFormArray.push(new FormControl(value));
            checkbox!.checked = true;
        } else {
            let i: number = 0;
            channelsFormArray.controls.forEach((control: AbstractControl) => {
                if (control.value == value) {
                    channelsFormArray.removeAt(i);
                    checkbox!.checked = false;
                    return;
                }
                i++;
            });
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

    private _loadTemplates(): void {
        this._api
            .getTemplates()
            .pipe(map((templates: TemplateListItemResultSet) => {
                if (templates.items) {
                    this.templates.push(...templates.items.map(template => new MenuOption(template.name || '', template.id, undefined, template.channels)))
                }
            }))
            .subscribe();
    }

    private _initForm(): void {
        this.form = new FormGroup({
            title: new FormControl(undefined, [
                Validators.required,
                Validators.maxLength(128)
            ]),
            from: new FormControl(this._datePipe.transform(this.now, 'yyyy-MM-ddThh:mm')),
            to: new FormControl(),
            actionLinkText: new FormControl(undefined, [Validators.maxLength(128)]),
            actionLinkHref: new FormControl(undefined, [
                Validators.pattern(/^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:?#[\]@!\$&'\(\)\*\+,;=.]+$/),
                Validators.maxLength(2048)
            ]),
            type: new FormControl(),
            template: new FormControl(),
            needsTemplate: new FormControl('no'),
            channels: new FormArray([new FormControl('Inbox')], [Validators.required])
        });
    }
}
