import { Component, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideFileDown } from '@lucide/angular';
import { CasesApiService } from 'src/app/core/services/cases-api.service';

/** Downloads the case PDF and opens it in a new tab. Disabled while the form is invalid or has unsaved changes. */
@Component({
  selector: 'app-case-print-pdf',
  imports: [TranslatePipe, LucideFileDown],
  template: `
    <button type="button" class="btn" [disabled]="buttonDisabled() || downloading()" (click)="onDownload()"
      [title]="(buttonDisabled() ? 'caseDetails.print.info' : 'caseDetails.print.title') | translate">
      @if (downloading()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideFileDown class="size-4"></svg> }
      {{ 'caseDetails.print.button' | translate }}
    </button>
  `
})
export class CasePrintPdfComponent {
  private readonly _api = inject(CasesApiService);

  public readonly caseId = input<string | undefined>();
  public readonly buttonDisabled = input<boolean | undefined>(false);
  /** Emits `true` once the PDF has been downloaded. */
  public readonly pdfButtonClicked = output<boolean>();
  protected readonly downloading = signal(false);

  protected onDownload(): void {
    this.downloading.set(true);
    this._api.downloadCasePdf(this.caseId()!).subscribe({
      next: result => {
        this.downloading.set(false);
        const fileURL = window.URL.createObjectURL(result.data);
        this.pdfButtonClicked.emit(true);
        window.open(fileURL, '_blank');
      },
      error: () => this.downloading.set(false)
    });
  }
}
