import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { JsonPipe } from '@angular/common';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { debounceTime, take } from 'rxjs';
import { CodeEditor } from '@acrodata/code-editor';
import { languages } from '@codemirror/language-data';
import { LucideBraces, LucideExternalLink, LucideEye, LucideImage } from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { Hyperlink, MessageContent, MessageSender } from 'src/app/core/services/messages-api.service';
import { ValidationService } from 'src/app/core/services/validation.service';
import { PartialTemplatesStore } from 'src/app/features/campaigns/create/steps/content/partial-templates-store.service';
import { SettingsStore } from 'src/app/features/settings/settings-store.service';
import { ThemeService } from 'src/app/layout/theme.service';
import { SafePipe } from 'src/app/shared/pipes/safe.pipe';
import { ChannelIconComponent, ChannelKind } from 'src/app/ui/badges/badges';
import { codeEditorDarkTheme, codeEditorLightTheme } from './code-editor-theme';

interface ChannelOption {
  /** Key of the channel in the template's `content` dictionary. */
  key: string;
  kind: ChannelKind;
  description: string;
}

const CHANNELS: ChannelOption[] = [
  { key: 'email', kind: 'Email', description: 'Campaigns.ChannelEmailDescription' },
  { key: 'sms', kind: 'SMS', description: 'Campaigns.ChannelSmsDescription' },
  { key: 'pushNotification', kind: 'PushNotification', description: 'Campaigns.ChannelPushNotificationDescription' },
  { key: 'inbox', kind: 'Inbox', description: 'Campaigns.ChannelInboxDescription' }
];

const DATA_TAB = '$data';

const SAMPLE_PAYLOAD = {
  contact: {
    id: 'FA24F7D6-332F-4E17-8E43-A111DB32E7CC',
    recipientId: '05B7B29D-8969-425A-A1FC-D5222553336F',
    salutation: 'Mr.',
    firstName: 'John',
    lastName: 'Doe',
    fullName: 'John Doe',
    email: 'email@john-doe.com',
    phoneNumber: '(212)-456-7890'
  }
};

// Email and inbox bodies are HTML; the preview is a light "paper" like the message the recipient gets.
const PREVIEW_STYLE = '<style>html{color-scheme:light}body{margin:16px;font-family:system-ui,sans-serif;font-size:14px;background:Canvas;color:CanvasText}</style>';

type ChannelForm = FormGroup<{
  channel: FormControl<string>;
  sender: FormControl<MessageSender | null>;
  subject: FormControl<string>;
  body: FormControl<string>;
}>;

/**
 * Per channel content editor of a template: channel picker, channel tabs with title / body (HTML code editor),
 * a live Handlebars preview rendered with a sample contact, and the template's JSON data.
 */
@Component({
  selector: 'app-template-content-editor',
  imports: [ReactiveFormsModule, RouterLink, JsonPipe, TranslatePipe, SafePipe, CodeEditor, ChannelIconComponent, LucideBraces, LucideExternalLink, LucideEye, LucideImage],
  template: `
    <div class="space-y-5">
      <fieldset class="fieldset">
        <legend class="fieldset-legend">{{ 'Campaigns.CommunicationChannelsLabelRequired' | translate }}</legend>
        <div class="grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
          @for (option of channelOptions; track option.key) {
            @let checked = selected().includes(option.key);
            @let locked = checked && selected().length <= 1;
            <label class="flex items-start gap-3 rounded-box border border-base-300 p-3 transition-colors has-checked:border-primary has-checked:bg-primary/5"
              [class.cursor-pointer]="!locked">
              <input type="checkbox" class="checkbox checkbox-sm checkbox-primary mt-0.5" [checked]="checked" [disabled]="locked"
                (change)="toggleChannel(option.key, $any($event.target).checked)" />
              <span class="min-w-0">
                <app-channel-icon [kind]="option.kind" [label]="true" />
                <span class="mt-1 block text-xs text-base-content/60">{{ option.description | translate }}</span>
              </span>
            </label>
          }
        </div>
      </fieldset>

      <div class="panel overflow-hidden">
        <div role="tablist" class="tabs tabs-border flex-nowrap overflow-x-auto border-b border-base-300 px-2">
          @for (key of selected(); track key) {
            @let option = optionFor(key);
            <button type="button" role="tab" class="tab gap-2 whitespace-nowrap" [class.tab-active]="active() === key" (click)="activate(key)">
              <app-channel-icon [kind]="option?.kind" [label]="true" />
              @if (groupFor(key)?.invalid && groupFor(key)?.touched) {
                <span class="status status-error"></span>
              }
            </button>
          }
          <button type="button" role="tab" class="tab ml-auto gap-2 whitespace-nowrap" [class.tab-active]="active() === dataTab" (click)="activate(dataTab)">
            <svg lucideBraces class="size-4"></svg>{{ 'Campaigns.MetadataLabel' | translate }}
            @if (form.controls.data.invalid) { <span class="status status-error"></span> }
          </button>
        </div>

        <div class="p-5">
          @let group = groupFor(active());
          @if (group) {
            <div class="grid gap-6 xl:grid-cols-2" [formGroup]="group">
              <div class="min-w-0 space-y-4">
                @if (group.controls.channel.value === 'email') {
                  <fieldset class="fieldset">
                    <legend class="fieldset-legend">{{ 'Campaigns.SenderLabel' | translate }}</legend>
                    <select class="select w-full" [value]="senderValue(group)" (change)="setSender(group, $any($event.target).value)">
                      @if (!defaultSender()) {
                        <option value="">{{ 'Campaigns.SelectSenderPlaceholder' | translate }}</option>
                      }
                      @for (sender of senders(); track sender.id) {
                        <option [value]="sender.id">{{ sender.displayName }} &lt;{{ sender.sender }}&gt;</option>
                      }
                    </select>
                    <p class="label whitespace-normal">{{ 'Campaigns.SenderInfo' | translate }}</p>
                  </fieldset>
                }

                <fieldset class="fieldset">
                  <legend class="fieldset-legend">{{ 'Campaigns.SubjectLabelRequired' | translate }}</legend>
                  <input class="input w-full" formControlName="subject" [placeholder]="'Campaigns.SubjectPlaceholder' | translate"
                    [class.input-error]="group.controls.subject.invalid && group.controls.subject.touched" />
                  @if (group.controls.subject.touched && group.controls.subject.hasError('required')) {
                    <p class="label text-error">{{ 'Campaigns.SubjectRequiredError' | translate }}</p>
                  }
                </fieldset>

                <fieldset class="fieldset">
                  <div class="flex items-center justify-between gap-2">
                    <legend class="fieldset-legend">{{ 'Campaigns.BodyLabelRequired' | translate }}</legend>
                    @if (enableMediaLibrary) {
                      <a class="btn btn-ghost btn-xs" routerLink="/media" target="_blank" [title]="'Campaigns.NavigateActionTitle' | translate">
                        <svg lucideImage class="size-4"></svg>{{ 'Campaigns.FileLibraryHeader' | translate }}
                        <svg lucideExternalLink class="size-3.5 opacity-60"></svg>
                      </a>
                    }
                  </div>
                  <code-editor class="code-editor-ritch h-[50vh]" formControlName="body" setup="basic" language="html" [languages]="languages"
                    [theme]="editorTheme()" [lineWrapping]="true" [indentWithTab]="false" [placeholder]="'Campaigns.BodyPlaceholder' | translate"
                    [class.border-error]="group.controls.body.invalid && group.controls.body.touched" (blur)="group.controls.body.markAsTouched()" />
                  @if (group.controls.body.touched && group.controls.body.hasError('required')) {
                    <p class="label text-error">{{ 'Campaigns.BodyRequiredError' | translate }}</p>
                  }
                </fieldset>
              </div>

              <div class="min-w-0">
                <p class="mb-2 flex items-center gap-2 text-sm font-semibold">
                  <svg lucideEye class="size-4 text-base-content/60"></svg>{{ 'Campaigns.PreviewLabel' | translate }}
                </p>
                <div class="overflow-hidden rounded-box border border-base-300">
                  <div class="border-b border-base-300 bg-base-200 px-4 py-2.5 text-sm">
                    @if (subjectPreview()) {
                      <span class="font-semibold">{{ subjectPreview() }}</span>
                    } @else {
                      <span class="text-base-content/40">{{ 'Campaigns.SubjectPlaceholder' | translate }}</span>
                    }
                  </div>
                  <iframe class="block h-[50vh] w-full" [title]="'Campaigns.PreviewLabel' | translate" [srcdoc]="bodyPreview() | safe: 'html'"></iframe>
                </div>
              </div>
            </div>
          } @else {
            <div class="grid gap-6 xl:grid-cols-2">
              <fieldset class="fieldset min-w-0">
                <legend class="fieldset-legend">{{ 'Campaigns.MetadataLabel' | translate }}</legend>
                <code-editor class="code-editor-ritch h-[40vh]" [formControl]="form.controls.data" setup="basic" language="json" [languages]="languages"
                  [theme]="editorTheme()" [lineWrapping]="true" [indentWithTab]="false" [placeholder]="'Campaigns.MetadataPlaceholder' | translate"
                  [class.border-error]="form.controls.data.invalid" />
                @if (form.controls.data.hasError('invalidJson')) {
                  <p class="label text-error">{{ 'Campaigns.MetadataInvalidJsonError' | translate }}</p>
                }
              </fieldset>
              <fieldset class="fieldset min-w-0">
                <legend class="fieldset-legend">{{ 'Campaigns.MessageDataLabel' | translate }}</legend>
                <pre class="h-[40vh] overflow-auto rounded-field border border-base-300 bg-base-200 p-3 font-mono text-xs leading-relaxed">{{ samplePayload() | json }}</pre>
              </fieldset>
            </div>
          }
        </div>
      </div>
    </div>
  `
})
export class TemplateContentEditorComponent {
  private readonly _fb = inject(NonNullableFormBuilder);
  private readonly _partials = inject(PartialTemplatesStore);
  private readonly _settings = inject(SettingsStore);
  private readonly _theme = inject(ThemeService);
  private readonly _translate = inject(TranslateService);
  private readonly _destroyRef = inject(DestroyRef);

  /** Content per channel key (`email`, `sms`, `pushNotification`, `inbox`). */
  public readonly content = input<{ [key: string]: MessageContent } | undefined>();
  /** The template's data object. */
  public readonly data = input<any>();

  public readonly form = this._fb.group({
    data: this._fb.control<string>('', inject(ValidationService).invalidJsonValidator()),
    content: this._fb.array<ChannelForm>([])
  });

  protected readonly channelOptions = CHANNELS;
  protected readonly dataTab = DATA_TAB;
  protected readonly languages = languages;
  protected readonly enableMediaLibrary = settings.enableMediaLibrary;
  protected readonly editorTheme = computed(() => this._theme.isDark() ? codeEditorDarkTheme : codeEditorLightTheme);

  protected readonly selected = signal<string[]>([]);
  protected readonly active = signal<string>(DATA_TAB);
  protected readonly senders = signal<MessageSender[]>([]);
  protected readonly defaultSender = computed(() => this.senders().find(s => s.isDefault));
  protected readonly subjectPreview = signal('');
  protected readonly bodyPreview = signal('');
  private readonly _dataText = signal('');
  private readonly _mediaBaseHref = signal(`${settings.api_url}`);
  private _sendersLoaded = false;

  protected readonly samplePayload = computed(() => {
    let data = null;
    try {
      data = this._dataText() ? JSON.parse(this._dataText()) : null;
    } catch { /* invalid JSON is reported by the validator */ }
    return {
      ...SAMPLE_PAYLOAD,
      data,
      actionLink: new Hyperlink({ text: this._translate.instant('Campaigns.ActionLinkDefaultText'), href: 'https://www.indice.gr' }),
      title: this._translate.instant('Campaigns.DefaultTitle'),
      mediaBaseHref: this._mediaBaseHref()
    };
  });

  constructor() {
    effect(() => {
      const content = this.content();
      const data = this.data();
      untracked(() => this.reset(content, data));
    });
    this.form.valueChanges.pipe(debounceTime(250), takeUntilDestroyed()).subscribe(() => {
      this._dataText.set(this.form.controls.data.value ?? '');
      this.renderPreview();
    });
    if (settings.enableMediaLibrary) {
      this._settings.getMediaSetting('Media CDN').subscribe(setting => {
        const value = setting?.value;
        this._mediaBaseHref.set(value ? value.replace(/\/$/, '') : `${settings.api_url}/media-root`);
      });
    }
  }

  /** The edited content per channel and the parsed data object, as the API expects them. */
  public getValue(): { content: { [key: string]: MessageContent }; data: any } {
    const content: { [key: string]: MessageContent } = {};
    for (const group of this.form.controls.content.controls) {
      const value = group.getRawValue();
      content[value.channel] = new MessageContent({ title: value.subject, sender: value.sender ?? undefined, body: value.body });
    }
    const dataText = this.form.controls.data.value;
    return { content, data: JSON.parse(dataText || '{}') };
  }

  public markAllAsTouched(): void {
    this.form.markAllAsTouched();
  }

  protected optionFor(key: string): ChannelOption | undefined {
    return CHANNELS.find(c => c.key.toLowerCase() === key.toLowerCase());
  }

  protected groupFor(key: string): ChannelForm | undefined {
    return this.form.controls.content.controls.find(g => g.controls.channel.value === key);
  }

  protected activate(key: string): void {
    this.active.set(key);
    this.renderPreview();
  }

  protected toggleChannel(key: string, checked: boolean): void {
    const array = this.form.controls.content;
    if (checked) {
      array.push(this.channelGroup(key));
      this.activate(key);
    } else {
      const index = array.controls.findIndex(g => g.controls.channel.value === key);
      if (index >= 0) {
        array.removeAt(index);
      }
      if (this.active() === key) {
        this.activate(array.controls[0]?.controls.channel.value ?? DATA_TAB);
      }
    }
    this.syncSelected();
  }

  protected senderValue(group: ChannelForm): string {
    return group.controls.sender.value?.id ?? this.defaultSender()?.id ?? '';
  }

  /** Picking the default sender clears the explicit sender, so the server falls back to its default. */
  protected setSender(group: ChannelForm, id: string): void {
    const sender = this.senders().find(s => s.id === id);
    group.controls.sender.setValue(!sender || sender.isDefault ? null : new MessageSender({ id: sender.id, sender: sender.sender, displayName: sender.displayName }));
    group.markAsDirty();
  }

  private reset(content: { [key: string]: MessageContent } | undefined, data: any): void {
    const array = this.form.controls.content;
    array.clear({ emitEvent: false });
    for (const key of Object.keys(content ?? {})) {
      array.push(this.channelGroup(key, content![key]), { emitEvent: false });
    }
    const dataText = data instanceof Object ? JSON.stringify(data, undefined, 2) : (data ?? '');
    this.form.controls.data.setValue(dataText, { emitEvent: false });
    this._dataText.set(dataText);
    this.syncSelected();
    this.form.markAsPristine();
    this.activate(array.controls[0]?.controls.channel.value ?? DATA_TAB);
  }

  private channelGroup(key: string, content?: MessageContent): ChannelForm {
    if (key.toLowerCase() === 'email') {
      this.loadSenders();
    }
    return this._fb.group({
      channel: this._fb.control(key),
      sender: this._fb.control<MessageSender | null>(content?.sender ?? null),
      subject: this._fb.control(content?.title ?? '', Validators.required),
      body: this._fb.control(content?.body ?? '', Validators.required)
    });
  }

  private syncSelected(): void {
    this.selected.set(this.form.controls.content.controls.map(g => g.controls.channel.value));
  }

  private loadSenders(): void {
    if (this._sendersLoaded) {
      return;
    }
    this._sendersLoaded = true;
    this._settings.getMessageSenders().subscribe(result => this.senders.set(result.items ?? []));
  }

  private renderPreview(): void {
    const group = this.groupFor(this.active());
    if (!group) {
      return;
    }
    const { channel, subject, body } = group.getRawValue();
    const payload = this.samplePayload();
    this._partials.envFor(channel).pipe(take(1), takeUntilDestroyed(this._destroyRef)).subscribe(env => {
      const render = (text: string): string | undefined => {
        try {
          return env.compile(text)(payload, { data: { 'partial-block': () => '' } });
        } catch {
          return undefined; // keep the last good preview while the template is incomplete
        }
      };
      const renderedSubject = subject ? render(subject) : '';
      if (renderedSubject !== undefined) {
        this.subjectPreview.set(renderedSubject);
      }
      const renderedBody = body ? render(body) : '';
      if (renderedBody !== undefined) {
        const isHtml = ['email', 'inbox'].includes(channel.toLowerCase());
        this.bodyPreview.set(PREVIEW_STYLE + (isHtml ? renderedBody : `<pre style="white-space:pre-wrap;font-family:inherit;margin:0">${renderedBody}</pre>`));
      }
    });
  }
}
