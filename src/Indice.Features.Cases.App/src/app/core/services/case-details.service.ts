import { Injectable, signal } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import { Case } from './cases-api.service';

@Injectable({
    providedIn: 'root'
})
export class CaseDetailsService {
    private _caseDetails: ReplaySubject<Case> = new ReplaySubject(1);
    private readonly _revisions = signal<ReadonlyMap<string, number>>(new Map());
    public caseDetails$ = this._caseDetails.asObservable();
    public caseDetails: Case | undefined;

    constructor() { }

    public setCaseDetails(caseDetails: Case) {
        this.caseDetails = caseDetails;
        this._caseDetails.next(caseDetails);
    }

    /** How many times a case was changed outside the details page (e.g. from a side pane); the page refreshes it whenever this grows. */
    public revision(caseId: string | undefined): number {
        return caseId ? this._revisions().get(caseId) ?? 0 : 0;
    }

    public notifyCaseChanged(caseId: string) {
        this._revisions.update(revisions => new Map(revisions).set(caseId, (revisions.get(caseId) ?? 0) + 1));
    }
}
