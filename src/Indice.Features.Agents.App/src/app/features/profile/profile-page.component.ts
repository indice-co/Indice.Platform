import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '@indice/ng-auth';

import { DexApiService, Profile } from '../../core/services/dex-api.service';
import {
  UsageStats,
  formatCount,
  initialsOf,
  languageLabel,
  rolesFromClaims,
  styleLabel,
  toUsageStats,
} from './profile.models';

/**
 * Profile surface composed from three real sources:
 *  - OIDC identity (name / email / roles) from `AuthService`,
 *  - the app profile + preferences from `GET /api/my/profile` (`getMe`),
 *  - usage aggregated from the caller's chat sessions (`GET /api/my/chats`).
 * Each source degrades independently so a failure in one still renders the rest.
 */
@Component({
  selector: 'app-profile-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink],
  templateUrl: './profile-page.component.html',
})
export class ProfilePageComponent {
  private readonly auth = inject(AuthService);
  private readonly dex = inject(DexApiService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Identity (read once from the OIDC user) ───────────────────────────────
  protected readonly displayName = this.auth.getDisplayName() || 'You';
  protected readonly fullName = this.auth.getFullName();
  protected readonly userName = this.auth.getUserName();
  protected readonly email = this.auth.getEmail();
  protected readonly emailVerified = this.auth.hasVerifiedEmail() ?? false;
  protected readonly subjectId = this.auth.getSubjectId();
  protected readonly isAdmin = this.auth.isAdmin();
  protected readonly roles = rolesFromClaims(
    this.auth.getUserProfile() as Record<string, unknown> | undefined,
  );
  protected readonly initials = initialsOf(this.displayName);

  // ── App profile + preferences (GET /api/my/profile) ───────────────────────
  protected readonly profile = signal<Profile | null>(null);
  protected readonly profileLoading = signal(true);
  protected readonly profileFailed = signal(false);

  // ── Usage stats (aggregated from GET /api/my/chats) ───────────────────────
  protected readonly stats = signal<UsageStats | null>(null);
  protected readonly statsLoading = signal(true);
  protected readonly statsFailed = signal(false);

  // Prefer the profile's authoritative timestamps; fall back to session-derived values.
  protected readonly memberSince = computed(
    () => this.profile()?.createdAt ?? this.stats()?.activeSince ?? null,
  );
  protected readonly lastSeen = computed(
    () => this.profile()?.lastSeenAt ?? this.stats()?.lastActivity ?? null,
  );
  protected readonly reasoning7d = computed(() => this.profile()?.reasoningTokensLast7Days ?? null);

  protected readonly fmt = formatCount;
  protected readonly langLabel = languageLabel;
  protected readonly styleLbl = styleLabel;

  constructor() {
    this.loadProfile();
    this.loadStats();
  }

  protected reload(): void {
    this.loadProfile();
    this.loadStats();
  }

  private loadProfile(): void {
    this.profileLoading.set(true);
    this.profileFailed.set(false);
    this.dex
      .getMe()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.profileLoading.set(false);
        },
        error: () => {
          this.profileFailed.set(true);
          this.profileLoading.set(false);
        },
      });
  }

  private loadStats(): void {
    this.statsLoading.set(true);
    this.statsFailed.set(false);
    this.dex
      // Most-recent first, generous page so token sums cover the full demo history.
      .list(1, 100, 'lastActivityAt-', null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.stats.set(toUsageStats(result.items ?? [], result.count ?? 0));
          this.statsLoading.set(false);
        },
        error: () => {
          // Non-blocking: identity still renders even if usage can't be loaded (e.g. 401).
          this.statsFailed.set(true);
          this.statsLoading.set(false);
        },
      });
  }
}
