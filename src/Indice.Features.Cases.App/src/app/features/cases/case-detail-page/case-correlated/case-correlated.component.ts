import { Component, Input, OnInit } from '@angular/core';
import { tap } from 'rxjs/operators';
import { CasesApiService, TimelineEntry } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-correlated',
  templateUrl: './case-correlated.component.html'
})
export class CaseCorrelatedComponent implements OnInit {

  @Input() correlatedCasesIds: string[] | undefined;

  constructor(private api: CasesApiService) { }

  ngOnInit(): void { }

  // onAttachmentClick(attachmentId: string) {
  //   this.api.downloadAttachment(attachmentId)
  //     .pipe(
  //       tap(results => {
  //         // saveAs(results.data, results.fileName);
  //         const fileURL = window.URL.createObjectURL(results.data);
  //         window.open(fileURL, '_blank');
  //       }))
  //     .subscribe();
  // }
}
