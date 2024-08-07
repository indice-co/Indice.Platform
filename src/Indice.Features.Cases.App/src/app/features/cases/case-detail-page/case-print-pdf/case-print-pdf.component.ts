import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { tap } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-print-pdf',
  templateUrl: './case-print-pdf.component.html',
  styleUrls: ['./case-print-pdf.component.scss']
})
export class CasePrintPdfComponent implements OnInit {

  @Input()
  caseId: string | undefined;

  /**
   * Component enabled
   */
  @Input()
  enabled: boolean | undefined;

  @Input()
  buttonDisabled: boolean | undefined;
  /** emit event when the pdf is printed */
  @Output() pdfButtonClicked: EventEmitter<boolean> = new EventEmitter(false);
  constructor(private api: CasesApiService) { }

  ngOnInit(): void {
  }

  onDownload() {
    this.api.downloadCasePdf(this.caseId!)
      .pipe(
        tap(results => {
          const fileURL = window.URL.createObjectURL(results.data);
          this.pdfButtonClicked.emit(true);
          window.open(fileURL, '_blank');
        }))
      .subscribe();
  }

}
