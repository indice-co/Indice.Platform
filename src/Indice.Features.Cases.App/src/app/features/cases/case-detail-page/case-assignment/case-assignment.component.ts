import { Assignee, AuditMeta } from './../../../../core/services/cases-api.service';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Icons, MenuOption } from '@indice/ng-components';
import { tap } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-assignment',
  templateUrl: './case-assignment.component.html'
})
export class CaseAssignmentComponent implements OnInit {

  @Input() caseId: string | undefined;

  @Input() enabled: boolean | undefined;

  @Output() caseAssigned = new EventEmitter<void>();

  public assignees: MenuOption[] = [];
  public assignees1: Assignee[] = [];
  public selectedAuditMeta: AuditMeta | undefined;

  constructor(private api: CasesApiService) { }

  ngOnInit(): void {
    this.api.getAssignees(this.caseId)
      .subscribe(
        (assignees) => {
          this.assignees1 = assignees;
          assignees.forEach(element => {
            this.assignees.push({ text: `${element.displayName!} | ${element.mail}`, description: undefined, data: undefined, value: element.id, icon: Icons.Badges });
          })
        });
  }

  selectedAssigneeChanged(fieldValue: string) {
    let selectedAssignee = this.assignees1.find(x => x.id === fieldValue);
    this.selectedAuditMeta = new AuditMeta({ id: selectedAssignee?.id, name: selectedAssignee?.displayName, email: selectedAssignee?.mail })
  }

  onAssign(): void {
    // let auditMeta = new AuditMeta({ id: this.selectedAuditMeta })
    this.api
      .assignCase(this.caseId!, undefined, this.selectedAuditMeta)
      .pipe(
        tap(() => this.caseAssigned.emit())
      )
      .subscribe();
  }

}
