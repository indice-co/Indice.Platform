import { AfterViewInit, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { forkJoin } from 'rxjs';
import { Contact, CreateDistributionListContactRequest, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ListContactCreateComponent } from 'src/app/shared/components/list-contact-create/list-contact-create.component';

@Component({
    selector: 'app-distribution-list-contact-create',
    templateUrl: './distribution-list-contact-create.component.html'
})
export class DistributionListContactCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('contactCreateComponent', { static: false }) public contactCreateComponent!: ListContactCreateComponent;
    
    private _distributionListId: string = '';

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router
    ) { }

    public submitInProgress = false;

    public ngOnInit(): void {
        this._distributionListId = this._router.url.split('/')[2];
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmit(contacts: Contact[]): void {
        if (!contacts) return;
        this.submitInProgress = true;
        var tasks = contacts.map((contact: Contact) => {
            const body = new CreateDistributionListContactRequest({
                email: contact.email,
                firstName: contact.firstName,
                fullName: contact.fullName,
                contactId: contact.id,
                lastName: contact.lastName,
                phoneNumber: contact.phoneNumber,
                recipientId: contact.recipientId,
                salutation: contact.salutation
            });
            return this._api.addContactToDistributionList(this._distributionListId, body);
        });
        forkJoin(tasks).subscribe().add(() => {
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId, 'distribution-list-contacts']));
        });
    }
}
