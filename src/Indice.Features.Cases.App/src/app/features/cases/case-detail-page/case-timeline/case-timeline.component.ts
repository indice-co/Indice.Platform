import { Component, Input, OnInit } from '@angular/core';
import { tap } from 'rxjs/operators';
import { CasesApiService, TimelineEntry } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-timeline',
  templateUrl: './case-timeline.component.html'
})
export class CaseTimelineComponent implements OnInit {

  @Input() timelineEntries: TimelineEntry[] | undefined;

  constructor(private api: CasesApiService) { }

  ngOnInit(): void { }

  onAttachmentClick(attachmentId: string) {
    this.api.downloadAttachment(attachmentId)
      .pipe(
        tap(results => {
          // saveAs(results.data, results.fileName);
          const fileURL = window.URL.createObjectURL(results.data);
          window.open(fileURL, '_blank');
        }))
      .subscribe();
  }
}
