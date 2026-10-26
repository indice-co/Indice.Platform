import { Component, ElementRef, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService, ImgUserPictureDirective } from '@indice/ng-auth';
import { LucideGlobe, LucideLock, LucideSendHorizontal } from '@lucide/angular';
import { CasesApiService, SendCommentRequest } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';

/**
 * Adds a comment to a case. Starts as a single quiet line and unfolds into a full composer (visibility, post button)
 * once focused. Ctrl / ⌘ + Enter posts, Escape on an empty composer folds it back.
 */
@Component({
  selector: 'app-case-comment-composer',
  imports: [TranslatePipe, ImgUserPictureDirective, LucideGlobe, LucideLock, LucideSendHorizontal],
  host: { class: 'block' },
  template: `
    <form #form class="rounded-box border bg-base-100 transition-[border-color,box-shadow] duration-200" [class]="frameClass()"
      (focusin)="expanded.set(true)" (focusout)="onFocusOut($event)" (submit)="$event.preventDefault(); submit()">
      <div class="flex items-start gap-3 p-3">
        <span class="mt-0.5 size-7 shrink-0 overflow-hidden rounded-full bg-base-300 ring-2 ring-base-100">
          @if (userId()) {
            <img [userPicture]="userId()!" [displayName]="userName()" [size]="64" alt="" />
          }
        </span>
        <textarea #textarea name="comment" rows="1" maxlength="4000"
          class="field-sizing-content mt-1 max-h-60 min-h-6 w-full resize-none bg-transparent text-sm leading-relaxed outline-none placeholder:text-base-content/45"
          [class.min-h-16]="expanded()" [placeholder]="'caseDetails.comments.placeholder' | translate"
          [attr.aria-label]="'common.comment' | translate" [value]="text()" [disabled]="submitting()"
          (input)="text.set($any($event.target).value)" (keydown)="onKeydown($event)"></textarea>
      </div>

      @if (expanded()) {
        <div animate.enter="animate-rise" class="flex flex-wrap items-center justify-between gap-2 border-t border-base-300/70 px-3 py-2">
          <div class="inline-flex rounded-field bg-base-200 p-0.5 text-xs font-medium" role="radiogroup"
            [attr.aria-label]="'caseDetails.comments.visibility' | translate">
            <button type="button" role="radio" class="inline-flex items-center gap-1 rounded-selector px-2 py-1 transition-colors"
              [class]="isPrivate() ? 'bg-base-100 text-base-content shadow-sm' : 'text-base-content/60 hover:text-base-content'"
              [attr.aria-checked]="isPrivate()" [title]="'caseDetails.comments.privateHint' | translate" (click)="isPrivate.set(true)">
              <svg lucideLock class="size-3.5"></svg>
              {{ 'caseDetails.comments.private' | translate }}
            </button>
            <button type="button" role="radio" class="inline-flex items-center gap-1 rounded-selector px-2 py-1 transition-colors"
              [class]="!isPrivate() ? 'bg-base-100 text-info shadow-sm' : 'text-base-content/60 hover:text-base-content'"
              [attr.aria-checked]="!isPrivate()" [title]="'caseDetails.comments.publicHint' | translate" (click)="isPrivate.set(false)">
              <svg lucideGlobe class="size-3.5"></svg>
              {{ 'caseDetails.comments.public' | translate }}
            </button>
          </div>

          <button type="submit" class="btn btn-primary btn-sm" [disabled]="!canSubmit()"
            [title]="'caseDetails.comments.shortcut' | translate">
            @if (submitting()) {
              <span class="loading loading-spinner loading-xs"></span>
            } @else {
              <svg lucideSendHorizontal class="size-4"></svg>
            }
            {{ 'caseDetails.comments.post' | translate }}
          </button>
        </div>
        <p class="px-3 pb-2 text-[0.6875rem] text-base-content/55" [class.text-info]="!isPrivate()">
          {{ (isPrivate() ? 'caseDetails.comments.privateHint' : 'caseDetails.comments.publicHint') | translate }}
          <span class="hidden sm:inline">· <kbd class="kbd kbd-xs">Ctrl</kbd> + <kbd class="kbd kbd-xs">Enter</kbd></span>
        </p>
      }
    </form>
  `
})
export class CaseCommentComposerComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _toaster = inject(TranslatedToasterService);
  private readonly _auth = inject(AuthService);
  private readonly _form = viewChild.required<ElementRef<HTMLFormElement>>('form');
  private readonly _textarea = viewChild.required<ElementRef<HTMLTextAreaElement>>('textarea');
  private readonly _user = toSignal(this._auth.user$, { initialValue: this._auth.getCurrentUser() });

  public readonly caseId = input.required<string>();
  public readonly commentAdded = output<void>();

  protected readonly text = signal('');
  /** Comments are private (back-office only) unless explicitly made public. */
  protected readonly isPrivate = signal(true);
  protected readonly expanded = signal(false);
  protected readonly submitting = signal(false);

  protected readonly userId = computed(() => this._user()?.profile?.sub);
  protected readonly userName = computed(() => this._user()?.profile?.name ?? this._user()?.profile?.email ?? '');
  protected readonly canSubmit = computed(() => !this.submitting() && this.text().trim().length > 0);
  protected readonly frameClass = computed(() => {
    if (!this.expanded()) {
      return 'border-base-300 hover:border-base-content/25';
    }
    return this.isPrivate()
      ? 'border-primary/50 shadow-[0_0_0_3px_color-mix(in_oklab,var(--color-primary)_14%,transparent)]'
      : 'border-info/60 shadow-[0_0_0_3px_color-mix(in_oklab,var(--color-info)_16%,transparent)]';
  });

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.submit();
    } else if (event.key === 'Escape' && !this.text().trim()) {
      this._textarea().nativeElement.blur();
      this.expanded.set(false);
    }
  }

  /** Folds back when focus leaves the composer and nothing has been written. */
  protected onFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (!this._form().nativeElement.contains(next) && !this.text().trim() && !this.submitting()) {
      this.expanded.set(false);
      this.isPrivate.set(true);
    }
  }

  protected submit(): void {
    const comment = this.text().trim();
    if (!comment || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this._api.adminAddComment(this.caseId(), new SendCommentRequest({ comment, privateComment: this.isPrivate() })).subscribe({
      next: () => {
        this.submitting.set(false);
        this.text.set('');
        this.isPrivate.set(true);
        this.expanded.set(false);
        this._toaster.show(ToastType.Success, undefined, 'toasts.commentAdded.body', 4000);
        this.commentAdded.emit();
      },
      // the problem details interceptor reports the failure; keep the text so nothing is lost
      error: () => {
        this.submitting.set(false);
        queueMicrotask(() => this._textarea().nativeElement.focus());
      }
    });
  }
}
