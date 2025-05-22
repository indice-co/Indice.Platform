import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, viewChild, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { CreateDistributionListRequest, MessagesApiClient, MessageType } from 'src/app/core/services/messages-api.service';
import { IAttachment } from '../../../shared/components/file-upload/file-upload.component';
import { AbstractControl, UntypedFormControl, UntypedFormGroup } from '@angular/forms';
import { FileParameter } from '../../../core/services/messages-api.service';
import { catchError, EMPTY, finalize, map, of, switchMap } from 'rxjs';

@Component({
    selector: 'app-distribution-list-create',
    templateUrl: './distribution-list-create.component.html'
})
export class DistributionListCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public form!: UntypedFormGroup;
    public get attachment(): AbstractControl { return this.form.get('attachment')!; }

    public submitInProgress = false;
    public model = new CreateDistributionListRequest({ name: '' });

    public ngOnInit(): void {
        this.form = new UntypedFormGroup({
          attachment: new UntypedFormControl(false)
        });
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onFileChange(file: IAttachment | undefined) {
        if (!file) {
            this.attachment.setValue(null)
            return;
        }
        this.attachment.setValue(<FileParameter>{
            fileName: file.title,
            data: file.data
        });
    }

    public onSubmit(): void {
        // first, try to create the distribution list
        // if creation succeeds, proceed with optionally importing the contacts from CSV
        this.submitInProgress = true;
        this._api
            .createDistributionList(this.model)
            .pipe(
                catchError(err => {
                    this._toaster.show(
                        ToastType.Error,
                        'Αποτυχία δημιουργίας λίστας',
                        'Αποτυχία κατά τη δημιουργία της λίστας επαφών.'
                    );
                    console.error('Failed to create distribution list:', err);
                    this.submitInProgress = false;
                    // abort chained calls early
                    return EMPTY;
                }),
                switchMap((messageType: MessageType) => {
                    const fileAttachment: FileParameter = this.attachment.value as FileParameter;
                    if (!fileAttachment) {
                        return of(messageType)
                    }
                    return this._api
                        .bulkImportContactsToDistributionList(messageType.id as string, fileAttachment)
                        .pipe(
                            catchError(err => {
                                this._toaster.show(
                                    ToastType.Warning,
                                    'Αποτυχία εισαγωγής επαφών',
                                    'Η λίστα δημιουργήθηκε, αλλά απέτυχε η εισαγωγή επαφών.'
                                );
                                console.error('Import contacts error:', err);
                                return of(messageType);
                            }),
                            map(() => messageType)
                        )
                }),
                finalize(() => {
                    this.submitInProgress = false;
                })
            )
            .subscribe((messageType: MessageType) => {
                this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η λίστα με όνομα '${messageType.name}' δημιουργήθηκε με επιτυχία.`);
                this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
            });
    }
}
