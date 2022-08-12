import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-case-discard-draft',
  templateUrl: './case-discard-draft.component.html',
  styleUrls: ['./case-discard-draft.component.scss']
})
export class CaseDiscardDraftComponent implements OnInit {

  @Input()
  caseId: string | undefined;

  @Input()
  enabled: boolean | undefined;

  @Output()
  caseDiscarded = new EventEmitter<void>();

  constructor(private api: CasesApiService) { }

  ngOnInit(): void { }

  onDiscard() {
    this.api
      .deleteDraftCase(this.caseId!)
      .pipe(
        tap(() => this.caseDiscarded.next())
      )
      .subscribe();
  }
}
