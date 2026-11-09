import { Injectable, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';

/**
 * State of the distribution list shown by the distribution list page. Provided by `DistributionListEditComponent`,
 * so the page and its tabs share one instance.
 */
@Injectable()
export class DistributionListEditStore {
  private readonly _api = inject(MessagesApiClient);
  private _request?: Subscription;

  public readonly distributionListId = signal('');
  public readonly distributionList = signal<DistributionList | undefined>(undefined);
  public readonly loading = signal(false);

  public load(distributionListId: string): void {
    if (distributionListId !== this.distributionListId()) {
      this.distributionList.set(undefined);
    }
    this.distributionListId.set(distributionListId);
    this.reload();
  }

  public reload(): void {
    const id = this.distributionListId();
    if (!id) {
      return;
    }
    this._request?.unsubscribe();
    this.loading.set(true);
    this._request = this._api.getDistributionListById(id).subscribe({
      next: list => {
        this.distributionList.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
