import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { tap } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-self-assignment',
  templateUrl: './case-self-assignment.component.html'
})
export class CaseSelfAssignmentComponent implements OnInit {
  @Input() caseId: string | undefined;

  @Input() enabled: boolean | undefined;

  @Output() caseAssigned = new EventEmitter<void>();

  constructor(private api: CasesApiService) { }

  ngOnInit(): void { }

  onAssign(): void {
    this.api
      .assignCase(this.caseId!)
      .pipe(
        tap(() => this.caseAssigned.emit())
      )
      .subscribe();
  }

}
