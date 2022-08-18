import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { AbstractControl, FormGroup, FormControl, Validators, FormBuilder, FormArray } from '@angular/forms';

import * as Handlebars from 'handlebars/dist/cjs/handlebars';
import { LibTabComponent } from '@indice/ng-components';
import { MessageChannelKind, MessageContent } from 'src/app/core/services/messages-api.service';
import { ValidationService } from 'src/app/core/services/validation.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';

@Component({
    selector: 'app-campaign-content',
    templateUrl: './campaign-content.component.html'
})
export class CampaignContentComponent implements OnInit, OnChanges {
    private _samplePayload: any = {
        contact: {
            id: 'FA24F7D6-332F-4E17-8E43-A111DB32E7CC',
            recipientId: '05B7B29D-8969-425A-A1FC-D5222553336F',
            salutation: 'Mr.',
            firstName: 'John',
            lastName: 'Doe',
            fullName: 'John Doe',
            email: 'email@john-doe.com',
            phoneNumber: '(212)-456-7890'
        }
    };

    private _currentValidDataObject: any = undefined;

    constructor(
        private _validationService: ValidationService,
        private _utilities: UtilitiesService,
        private _formBuilder: FormBuilder
    ) { }

    // Input & Output parameters
    @Input() public additionalData: any = {};
    @Input() public content: { [key: string]: MessageContent; } | undefined = undefined;
    // Form Controls
    public form: FormGroup = this._formBuilder.group({
        data: new FormControl(null, this._validationService.invalidJsonValidator()),
        content: this._formBuilder.array([])
    });

    public get data(): AbstractControl {
        return this.form.controls['data'];
    }

    public get channelsContent(): FormArray {
        return this.form.controls['content'] as FormArray;
    }

    public subjectPreview: string = '';
    public bodyPreview: string = '';
    public MessageChannelKind = MessageChannelKind;

    public get samplePayload(): any {
        return {
            ...this._samplePayload,
            data: this._currentValidDataObject ? { ...this._currentValidDataObject } : null,
            ...this.additionalData
        };
    }

    public ngOnInit(): void { }

    public ngOnChanges(changes: SimpleChanges): void {
        if (changes.content?.currentValue) {
            this.channelsContent.clear();
            const contentValue = changes.content.currentValue;
            let index = 0;
            for (const channel in contentValue) {
                if (Object.prototype.hasOwnProperty.call(contentValue, channel)) {
                    const content = <MessageContent>contentValue[channel];
                    const channelForm = this._formBuilder.group({
                        channel: channel,
                        subject: [content.title, Validators.required],
                        body: [content.body, Validators.required]
                    });
                    this.channelsContent.push(channelForm);
                    if (index === 0) {
                        this._setSubjectPreview(content.title);
                        this._setBodyPreview(content.body);
                    }
                }
                index++;
            }
        }
    }

    public onContentTabChanged(tab: LibTabComponent): void {
        const index = tab.index || 0;
        const formGroup = <FormGroup>this.channelsContent.controls[index];
        const subject = formGroup.controls['subject'].value;
        const body = formGroup.controls['body'].value;
        this._setSubjectPreview(subject);
        this._setBodyPreview(body);
    }

    public onCampaignMetadataInput(event: any): void {
        const value = event.target.value;
        if (this._utilities.isValidJson(value)) {
            this._currentValidDataObject = JSON.parse(value);
        }
    }

    public onSubjectInput(content: FormGroup): void {
        const subject = content.controls['subject'].value;
        this._setSubjectPreview(subject);
    }

    public onBodyInput(content: FormGroup): void {
        const subject = content.controls['body'].value;
        this._setBodyPreview(subject);
    }

    private _setSubjectPreview(value: string | undefined): void {
        if (!value) {
            this.subjectPreview = '';
        }
        try {
            const template = Handlebars.compile(value);
            this.subjectPreview = template(this.samplePayload);
        } catch (error) { }
    }

    private _setBodyPreview(value: string | undefined): void {
        if (!value) {
            this.bodyPreview = '';
        }
        try {
            const template = Handlebars.compile(value);
            this.bodyPreview = template(this.samplePayload);
        } catch (error) { }
    }
}
