import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import Handlebars from 'handlebars';
import { Observable, map, shareReplay } from 'rxjs';
import { MESSAGES_API_BASE_URL, Template } from 'src/app/core/services/messages-api.service';

const CHANNELS = ['inbox', 'pushnotification', 'email', 'sms'] as const;
type Channel = typeof CHANNELS[number];

/**
 * On first injection, fetches every Partial/Layout template once and builds one
 * isolated Handlebars environment per channel with all partials pre-registered
 * (keyed by alias). The component pulls the env for the active channel and uses
 * it for both `registerPartial` lookup and `compile` — same instance, no
 * cross-channel leakage.
 */
@Injectable({ providedIn: 'root' })
export class PartialTemplatesStore {
  private readonly _baseUrl: string;
  private readonly _envs$: Observable<Record<Channel, typeof Handlebars>>;

  constructor(
    private _http: HttpClient,
    @Optional() @Inject(MESSAGES_API_BASE_URL) baseUrl?: string
  ) {
    this._baseUrl = baseUrl ?? '';
    this._envs$ = this._fetch().pipe(
      map(templates => this._buildEnvs(templates)),
      shareReplay({ bufferSize: 1, refCount: false })
    );
    // Eager prime: fire the HTTP call as soon as the service is created.
    this._envs$.subscribe();
  }

  /** Resolves to the Handlebars env for the requested channel. Unknown channel → a partial-free env. */
  public envFor(channel: string | undefined): Observable<typeof Handlebars> {
    const key = (channel || '').toLowerCase() as Channel;
    return this._envs$.pipe(map(envs => envs[key] ?? Handlebars.create()));
  }

  private _buildEnvs(templates: Template[]): Record<Channel, typeof Handlebars> {
    const envs = {} as Record<Channel, typeof Handlebars>;
    for (const channel of CHANNELS) {
      const env = Handlebars.create();
      for (const t of templates) {
        const body = this._bodyFor(t, channel);
        if (t.alias && body) {
          env.registerPartial(t.alias.toLowerCase(), body);
        }
      }
      envs[channel] = env;
    }
    return envs;
  }

  private _fetch(): Observable<Template[]> {
    const url = `${this._baseUrl}/templates/partials`;
    return this._http.get<{ count?: number; items?: any[] }>(url)
      .pipe(map(result => (result?.items ?? []).map(x => Template.fromJS(x))));
  }

  private _bodyFor(t: Template, channelLower: string): string | undefined {
    const content = t.content;
    if (!content) {
      return undefined;
    }
    for (const k of Object.keys(content)) {
      if (k.toLowerCase() === channelLower) {
        return content[k]?.body || undefined;
      }
    }
    return undefined;
  }
}
