import { JsonPipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { FormArray, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CodeEditor } from '@acrodata/code-editor';
import { languages } from '@codemirror/language-data';
import { Highlight } from 'ngx-highlightjs';
import { LucideBraces, LucideEye, LucideLayers } from '@lucide/angular';
import * as app from 'src/app/core/models/settings';
import { MessageContent, MessageSender } from 'src/app/core/services/messages-api.service';
import { ValidationService } from 'src/app/core/services/validation.service';
import { SettingsStore } from 'src/app/features/settings/settings-store.service';
import { ThemeService } from 'src/app/layout/theme.service';
import { SafePipe } from 'src/app/shared/pipes/safe.pipe';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { ChannelIconComponent } from 'src/app/ui/badges/badges';
import { CONTENT_CHANNELS, ContentChannel, contentChannel } from '../../../campaign-utils';
import { PartialTemplatesStore } from './partial-templates-store.service';

export type ChannelContentForm = FormGroup<{
  channel: FormControl<string>;
  sender: FormControl<MessageSender | null | undefined>;
  subject: FormControl<string>;
  body: FormControl<string>;
}>;

/** Tab id of the shared JSON data (metadata) tab. */
const DATA_TAB = -1;

const SAMPLE_CONTACT = {
  id: 'FA24F7D6-332F-4E17-8E43-A111DB32E7CC',
  recipientId: '05B7B29D-8969-425A-A1FC-D5222553336F',
  salutation: 'Mr.',
  firstName: 'John',
  lastName: 'Doe',
  fullName: 'John Doe',
  email: 'email@john-doe.com',
  phoneNumber: '(212)-456-7890'
};

/**
 * Per channel content editor (subject + Handlebars body with live preview) plus the shared JSON data used by the
 * templates. Used by the campaign wizard, the campaign content page and the template pages.
 * Consumers read `form.controls.content` (`{ channel, sender, subject, body }[]`) and `form.controls.data` (JSON text).
 */
@Component({
  selector: 'app-campaign-content',
  imports: [ReactiveFormsModule, JsonPipe, TranslatePipe, CodeEditor, Highlight, SafePipe, ChannelIconComponent, LucideBraces, LucideEye, LucideLayers],
  templateUrl: './campaign-content.component.html'
})
export class CampaignContentComponent {
  private readonly _fb = inject(NonNullableFormBuilder);
  private readonly _utilities = inject(UtilitiesService);
  private readonly _settings = inject(SettingsStore);
  private readonly _partials = inject(PartialTemplatesStore);
  private readonly _translate = inject(TranslateService);
  protected readonly theme = inject(ThemeService);

  /** Extra sample payload fields (title, type, actionLink, mediaBaseHref) merged into the preview data. */
  public readonly additionalData = input<Record<string, any>>({});
  /** Initial content per channel; replaces the edited content whenever it changes. */
  public readonly content = input<{ [key: string]: MessageContent } | undefined>(undefined);
  /** Initial JSON data (object or JSON text). */
  public readonly data = input<any>(undefined);
  /** Preview only (e.g. a published campaign): no channel selection or editors, the first channel opens instead. */
  public readonly readonly = input(false);

  public readonly form = this._fb.group({
    data: new FormControl<string | null>(null, inject(ValidationService).invalidJsonValidator()),
    content: new FormArray<ChannelContentForm>([])
  });

  /** Base URL that media references in the content resolve against (part of the preview payload). */
  public readonly mediaBaseHref = signal<string | undefined>(undefined);

  protected readonly channels = CONTENT_CHANNELS;
  protected readonly languages = languages;
  /** 0 = channel selection, n = content of the (n - 1)th channel, {@link DATA_TAB} = the JSON data. */
  protected readonly activeTab = signal(0);
  protected readonly dataTab = DATA_TAB;
  protected readonly channelKeys = signal<string[]>([]);
  protected readonly subjectPreview = signal('');
  protected readonly bodyPreview = signal('');
  protected readonly senders = signal<MessageSender[]>([]);
  protected readonly selectedSenderId = signal<string>('');
  private readonly _dataText = signal<string | null>(null);
  private _explicitMediaBaseHref = false;

  protected readonly samplePayload = computed(() => {
    const text = this._dataText();
    const data = text && this._utilities.isValidJson(text) ? JSON.parse(text) : null;
    const extra = this.additionalData() ?? {};
    return {
      contact: SAMPLE_CONTACT,
      data,
      ...extra,
      title: extra['title'] || this._translate.instant('Campaigns.DefaultTitle'),
      actionLink: extra['actionLink'] ?? { text: this._translate.instant('Campaigns.ActionLinkDefaultText'), href: 'https://www.indice.gr' },
      mediaBaseHref: this.mediaBaseHref()
    };
  });

  /** The body preview as an iframe document, following the app theme unless the content sets its own colors. */
  protected readonly previewDocument = computed(() =>
    `<style>:root { color-scheme: ${this.theme.isDark() ? 'dark' : 'light'}; font-family: ui-sans-serif, system-ui, sans-serif; }</style>${this.bodyPreview()}`);

  public get channelsContent(): FormArray<ChannelContentForm> {
    return this.form.controls.content;
  }

  constructor() {
    this.form.controls.data.valueChanges.subscribe(value => {
      this._dataText.set(value);
      const active = this.channelsContent.at(this.activeTab() - 1);
      if (active) {
        this.renderPreview(active);
      }
    });
    effect(() => {
      const value = this.data();
      untracked(() => this.form.controls.data.setValue(value instanceof Object ? JSON.stringify(value, undefined, 2) : (value ?? null)));
    });
    effect(() => {
      const content = this.content();
      if (content) {
        untracked(() => this.loadContent(content));
      }
    });
    effect(() => {
      // there is no channel selection tab in preview mode
      if (this.readonly() && this.channelKeys().length > 0 && untracked(this.activeTab) === 0) {
        untracked(() => this.selectTab(1));
      }
    });
    effect(() => {
      const href = this.additionalData()?.['mediaBaseHref'];
      untracked(() => {
        if (href) {
          this._explicitMediaBaseHref = true;
          this.mediaBaseHref.set(href);
        } else {
          this.resolveDefaultMediaBaseHref();
        }
      });
    });
  }

  /** The edited content as the API's `content` dictionary. */
  public buildContent(): { [key: string]: MessageContent } {
    const content: { [key: string]: MessageContent } = {};
    for (const item of this.channelsContent.getRawValue()) {
      content[item.channel] = new MessageContent({ title: item.subject, sender: item.sender ?? undefined, body: item.body });
    }
    return content;
  }

  protected isChecked(channel: ContentChannel): boolean {
    return this.channelKeys().some(key => contentChannel(key) === channel);
  }

  protected channelOf(key: string): ContentChannel | undefined {
    return contentChannel(key);
  }

  protected toggleChannel(channel: ContentChannel, checked: boolean): void {
    if (checked) {
      this.channelsContent.push(this.createChannelForm(channel.key));
    } else {
      const index = this.channelsContent.controls.findIndex(c => contentChannel(c.controls.channel.value) === channel);
      if (index >= 0) {
        this.channelsContent.removeAt(index);
      }
    }
    this.syncChannelKeys();
    if (channel.key === 'email' && checked) {
      this.loadSenders();
    }
  }

  protected selectTab(index: number): void {
    this.activeTab.set(index);
    const form = index > 0 ? this.channelsContent.at(index - 1) : undefined;
    if (form) {
      this.renderPreview(form);
    }
  }

  protected renderPreview(form: ChannelContentForm): void {
    const { channel, subject, body } = form.getRawValue();
    this.render(subject, channel, rendered => this.subjectPreview.set(rendered));
    this.render(body, channel, rendered => {
      const isPlainText = !['email', 'inbox'].includes(channel?.toLowerCase());
      this.bodyPreview.set(isPlainText && rendered ? `<pre style="white-space: pre-wrap; font-family: ui-sans-serif, system-ui, sans-serif">${rendered}</pre>` : rendered);
    });
  }

  protected onSenderChanged(id: string, form: ChannelContentForm): void {
    if (!id) {
      return;
    }
    this.selectedSenderId.set(id);
    const sender = this.senders().find(s => s.id === id);
    // The default sender is not stored on the campaign: the server falls back to it.
    form.controls.sender.setValue(sender && !sender.isDefault
      ? new MessageSender({ id: sender.id, sender: sender.sender, displayName: sender.displayName })
      : undefined);
  }

  private loadContent(content: { [key: string]: MessageContent }): void {
    this.channelsContent.clear();
    for (const key of Object.keys(content)) {
      const item = content[key];
      this.channelsContent.push(this.createChannelForm(key, item));
    }
    this.syncChannelKeys();
    const index = Math.min(this.activeTab(), this.channelsContent.length);
    this.activeTab.set(index);
    const first = this.channelsContent.at(Math.max(index, 1) - 1);
    if (first) {
      this.renderPreview(first);
    }
    const emailKey = Object.keys(content).find(key => key.toLowerCase() === 'email');
    if (emailKey) {
      this.selectedSenderId.set(content[emailKey]?.sender?.id ?? '');
      this.loadSenders();
    }
  }

  private createChannelForm(channel: string, content?: MessageContent): ChannelContentForm {
    return this._fb.group({
      channel: [channel],
      sender: new FormControl<MessageSender | null | undefined>(content?.sender),
      subject: [content?.title ?? '', Validators.required],
      body: [content?.body ?? '', Validators.required]
    });
  }

  private syncChannelKeys(): void {
    this.channelKeys.set(this.channelsContent.controls.map(c => c.controls.channel.value));
  }

  private loadSenders(): void {
    this._settings.getMessageSenders().subscribe(result => {
      const senders = result.items ?? [];
      this.senders.set(senders);
      if (!this.selectedSenderId()) {
        this.selectedSenderId.set(senders.find(s => s.isDefault)?.id ?? '');
      }
    });
  }

  private resolveDefaultMediaBaseHref(): void {
    if (this.mediaBaseHref()) {
      return;
    }
    this.mediaBaseHref.set(`${app.settings.api_url}`);
    if (app.settings.enableMediaLibrary) {
      this._settings.getMediaSetting('Media CDN').subscribe(setting => {
        if (this._explicitMediaBaseHref) {
          return;
        }
        const value = setting?.value;
        this.mediaBaseHref.set(value ? (value.endsWith('/') ? value.substring(0, value.length - 1) : value) : `${app.settings.api_url}/media-root`);
      });
    }
  }

  private render(template: string | undefined, channel: string, done: (rendered: string) => void): void {
    if (!template) {
      done('');
      return;
    }
    this._partials.envFor(channel).subscribe(env => {
      try {
        done(env.compile(template)(this.samplePayload(), { data: { 'partial-block': () => '' } }));
      } catch {
        // keep the last good preview while the template is being typed
      }
    });
  }
}
