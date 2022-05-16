import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe, DOCUMENT } from '@angular/common';
import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MenuOption, ToasterService } from '@indice/ng-components';
import { map } from 'rxjs/operators';
import { CreateCampaignRequest, MessagesApiClient, MessageChannelKind, MessageTypeResultSet, Period, Hyperlink } from 'src/app/core/services/messages-api.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { LibStepperComponent } from 'src/app/shared/components/stepper/lib-stepper.component';

@Component({
    selector: 'app-campaign-create',
    templateUrl: './campaign-create.component.html'
})
export class CampaignCreateComponent implements OnInit, AfterViewInit {
    constructor(
        private _api: MessagesApiClient,
        private _router: Router,
        private _utilities: UtilitiesService,
        private _changeDetector: ChangeDetectorRef,
        private _datePipe: DatePipe,
        @Inject(ToasterService) private _toaster: ToasterService,
        @Inject(DOCUMENT) private _document: Document
    ) { }
    
    public now: Date = new Date();
    public basicDetailsForm!: FormGroup;
    public messageTypes: MenuOption[] = [];
    public MessageChannelKind = MessageChannelKind;
    public customDataValid = true;
    public showCustomDataValidation = false;
    public submitInProgress = false;
    public targetOptions: MenuOption[] = [new MenuOption('Όλους τους χρήστες', true), new MenuOption('Ομάδα χρηστών', false)];

    public get title(): AbstractControl {
        return this.basicDetailsForm.get('title')!;
    }

    public get from(): AbstractControl {
        return this.basicDetailsForm.get('from')!;
    }

    public get to(): AbstractControl {
        return this.basicDetailsForm.get('to')!;
    }

    public get actionLinkText(): AbstractControl {
        return this.basicDetailsForm.get('actionLinkText')!;
    }

    public get actionLinkHref(): AbstractControl {
        return this.basicDetailsForm.get('actionLinkHref')!;
    }

    public ngOnInit(): void {
        // Did not find any better way to do this.
        setTimeout(() => {
            const sidePane = this._document.getElementsByClassName('side-pane-box-size')[0] as HTMLElement;
            sidePane.style.maxWidth = '84rem';
        }, 0);
        this.loadMessageTypes();
        this.initBasicDetailsFormGroup();
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmit(): void {
        debugger;
        if (!this.basicDetailsForm.valid) {
            return;
        }
        this.submitInProgress = true;
        const data = new CreateCampaignRequest({
            actionLink: new Hyperlink({
                href: this.actionLinkHref.value,
                text: this.actionLinkText.value
            }),
            activePeriod: new Period({
                from: this.from.value,
                to: this.to.value
            }),
            isGlobal: true,
            messageChannelKind: [MessageChannelKind.Inbox],
            published: false,
            templateId: undefined,
            title: this.title.value
        });
        // this._api
        //     .createCampaign(this.model)
        //     .subscribe({
        //         next: (campaign: Campaign) => {
        //             this.submitInProgress = false;
        //             // This is to force reload campaigns page when a new campaign is successfully saved. 
        //             this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
        //             this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η καμπάνια με τίτλο '${campaign.title}' δημιουργήθηκε με επιτυχία.`);
        //         },
        //         error: (problemDetails: ValidationProblemDetails) => {
        //             this._toaster.show(ToastType.Error, 'Αποτυχής αποθήκευση', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
        //         }
        //     });
    }

    public onCampaignStartInput(event: any): void {
        this.from.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public onCampaignEndInput(event: any): void {
        this.to.setValue(this._datePipe.transform(event.target.value, 'yyyy-MM-ddThh:mm'));
    }

    public setIsGlobal(isGlobal: boolean): void {
        // this.model.isGlobal = isGlobal;
        // if (isGlobal) {
        //     delete this.model.recipientIds;
        // }
    }

    public toRecipientIdsArray(recipientIds: string | undefined): string[] {
        return recipientIds ? [...new Set(recipientIds.split('\n').filter(x => x !== ''))] : [];
    }

    public toRecipientIdsString(recipientIds: string[] | undefined): string {
        return recipientIds ? recipientIds.join('\n') : '';
    }

    public toggleMessageChannelKind(messageChannelKind: MessageChannelKind): void {
        // const index = this.model.messageChannelKind!.findIndex(channel => channel === messageChannelKind);
        // if (index > -1) {
        //     this.model.messageChannelKind!.splice(index, 1);
        // } else {
        //     this.model.messageChannelKind!.push(messageChannelKind);
        // }
    }

    public hasMessageChannelKind(): boolean {
        //return this.model.messageChannelKind!.length > 0;
        return true;
    }

    public containsMessageChannelKind(messageChannelKind: MessageChannelKind): boolean {
        //return this.model.messageChannelKind!.indexOf(messageChannelKind) > -1;
        return true;
    }

    public setCampaignCustomData(metadataJson: string): void {
        // if (!metadataJson || metadataJson === '') {
        //     if ('data' in this.model) {
        //         delete this.model.data;
        //     }
        //     return;
        // }
        // try {
        //     const data = JSON.parse(metadataJson);
        //     this.customDataValid = true;
        //     this.model.data = data;
        // } catch (error) {
        //     this.customDataValid = false;
        // }
    }

    public onCustomDataFocusOut(): void {
        this.showCustomDataValidation = true;
    }

    private loadMessageTypes(): void {
        this.messageTypes = [];
        this._api
            .getMessageTypes()
            .pipe(map((messageTypes: MessageTypeResultSet) => {
                if (messageTypes.items) {
                    this.messageTypes = messageTypes.items.map(type => new MenuOption(type.name || '', type.id));
                    this.messageTypes.unshift(new MenuOption('Παρακαλώ επιλέξτε...', null));
                }
            }))
            .subscribe();
    }

    private initBasicDetailsFormGroup(): void {
        this.basicDetailsForm = new FormGroup({
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
            ])
        });
    }
}
