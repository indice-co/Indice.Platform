import { Injectable } from '@angular/core';
import Handlebars from 'handlebars';
import { Observable, map, shareReplay } from 'rxjs';
import { MessagesApiClient, Template } from 'src/app/core/services/messages-api.service';

const CHANNELS = ['inbox', 'pushnotification', 'email', 'sms'] as const;
type Channel = typeof CHANNELS[number];

/**
 * On first injection, fetches every Partial/Layout template once via the
 * generated `getPartialTemplates` method (backed by `GET /api/templates/partials`)
 * and builds one isolated Handlebars environment per channel with all partials
 * pre-registered (keyed by lowercase alias). The component pulls the env for the
 * active channel and uses it for both `registerPartial` lookup and `compile`.
 */
@Injectable({ providedIn: 'root' })
export class PartialTemplatesStore {
  private readonly _envs$: Observable<Record<Channel, typeof Handlebars>>;

  constructor(private _api: MessagesApiClient) {
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

  /**
   * Lowercases the names inside `{{> name}}` / `{{#> name}}` partial references
   * (and their matching `{{/name}}` closers) so author casing doesn't have to
   * exactly match the DB alias. Helper blocks like `{{#if X}}...{{/X}}` are
   * untouched (their opener has no `>`).
   */
  public normalizePartialCasing(template: string): string {
    const blockNames = new Set<string>();
    template.replace(/\{\{#>\s*([\w.-]+)/g, (_, name) => { blockNames.add(name); return ''; });
    let out = template
      .replace(/(\{\{>\s*)([\w.-]+)/g, (_, prefix, name) => prefix + name.toLowerCase())
      .replace(/(\{\{#>\s*)([\w.-]+)/g, (_, prefix, name) => prefix + name.toLowerCase());
    out = out.replace(/(\{\{\/\s*)([\w.-]+)(\s*\}\})/g, (full, prefix, name, suffix) =>
      blockNames.has(name) ? prefix + name.toLowerCase() + suffix : full
    );
    return out;
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
    return this._api.getPartialTemplates()
      .pipe(map((result: any) => (result?.items ?? []) as Template[]));
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
