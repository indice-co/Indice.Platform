import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { Approval, ApprovalRequest, CasesApiService } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-approval-buttons',
  templateUrl: './approval-buttons.component.html'
})
export class ApprovalButtonsComponent {

  @Input() caseId: string | undefined;
  @Input() enabled: boolean | undefined;
  public buttonsDisabled: boolean | undefined = false;
  public approveButtonDisabled: boolean | undefined = false;
  public comment: string | undefined;
  public rejectionOptions: MenuOption[] = [
    new MenuOption('Μη αποδεκτός τύπος αρχείου', RejectionReason.NotAcceptableFileType),
    new MenuOption('Μη αποδεκτή ποιότητα αρχείου', RejectionReason.NotAcceptableFileQuality),
    new MenuOption('Λαθεμένη καταχώρηση στοιχείων', RejectionReason.WrongData)
  ];

  constructor(
    private _toaster: ToasterService,
    private _api: CasesApiService,
    private router: Router
  ) { }

  public onToggleButtonChange() {
    this.approveButtonDisabled = !this.approveButtonDisabled;
    this.comment = this.approveButtonDisabled ? this.rejectionOptions[0].text : undefined;
  }

  public onOptionChange(value: any): void {
    this.comment = this.rejectionOptions.find(element => element.value === value)?.text;
  }

  public approveCase(): void {
    this.caseDecision(Approval.Approve);
  }

  public rejectCase(): void {
    this.caseDecision(Approval.Reject);
  }

  private caseDecision(action: Approval): void {
    this.buttonsDisabled = true;
    const approvalRequest = new ApprovalRequest({ action: action, comment: this.comment });
    this._api.submitApproval(this.caseId!, undefined, approvalRequest)
      .subscribe(_ => {
        this._toaster.show(ToastType.Success, 'Επιτυχής Επεξεργασία', `Η αίτηση επεξεργάστηκε επιτυχώς.`, 5000);
        this.router.navigate(['/cases']);
      }, _ => {
        this._toaster.show(ToastType.Error, 'Αποτυχία Επεξεργασίας', `Δεν κατέστη εφικτή η επεξεργασία της αίτησης.`, 5000);
        this.router.navigate(['/cases']);
      });
  }

}

export enum RejectionReason {
  NotAcceptableFileType = "Μη αποδεκτός τύπος αρχείου",
  NotAcceptableFileQuality = "Μη αποδεκτή ποιότητα αρχείου",
  WrongData = "Λαθεμένη καταχώρηση στοιχείων"
}