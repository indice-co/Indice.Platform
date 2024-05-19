import { AfterContentChecked, AfterContentInit, AfterViewChecked, AfterViewInit, Component, Input, OnChanges, OnInit, SimpleChanges, ViewChild } from '@angular/core';
import { AbstractControl, FormGroup, FormControl, Validators, FormBuilder, FormArray } from '@angular/forms';

import * as Handlebars from 'handlebars';
import * as app from 'src/app/core/models/settings';
import { LibTabComponent, LibTabGroupComponent, MenuOption, SidePaneComponent } from '@indice/ng-components';
import { Hyperlink, MessageChannelKind, MessageContent, MessageSender, MessageSenderResultSet } from 'src/app/core/services/messages-api.service';
import { ValidationService } from 'src/app/core/services/validation.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { ChannelState } from './channel-state';
import { map } from 'rxjs/operators';
import { SettingsStore } from 'src/app/features/settings/settings-store.service';
import { MediaFile } from 'src/app/core/services/media-api.service';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';
import { Editor } from 'tinymce';
import "tinymce";
import { settings } from 'src/app/core/models/settings';
declare var tinymce: any;
@Component({
    selector: 'app-campaign-content',
    templateUrl: './campaign-content.component.html',
    styleUrl: './campaign-content.component.scss'
})
export class CampaignContentComponent implements OnInit, OnChanges, AfterViewChecked {
    private _samplePayload: any = {
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
    private _contentForm?: FormGroup;

    private _currentValidDataObject: any = undefined;
    @ViewChild('tabGroup', { static: true }) private _tabGroup: LibTabGroupComponent | undefined;
    @ViewChild('rightPane', { static: false }) public rightPaneComponent!: SidePaneComponent;

    constructor(
        private _validationService: ValidationService,
        private _utilities: UtilitiesService,
        private _formBuilder: FormBuilder,
        private _store: SettingsStore,
        private _fileUtilitiesService: FileUtilitiesService
    ) { }

    // Input & Output parameters
    @Input() public additionalData: any = {};
    @Input() public content: { [key: string]: MessageContent; } | undefined = undefined;
    // Form Controls
    public form: FormGroup = this._formBuilder.group({
        data: new FormControl(null, this._validationService.invalidJsonValidator()),
        content: this._formBuilder.array([])
    });
    @Input() set data(value: any) {
        this.form.controls['data'].setValue(value instanceof Object ? JSON.stringify(value, undefined, 2) : value);
    }
    public get data(): AbstractControl {
        return this.form.controls['data'];
    }

    public get channelsContent(): FormArray {
        return this.form.controls['content'] as FormArray;
    }

    public subjectPreview: string = '';
    public bodyPreview: string = '';
    public MessageChannelKind = MessageChannelKind;
    public hideMetadata = true;
    public selectedSenderId: any;
    public messageSenders: MenuOption[] = [];
    public enableRichTextEditor = settings.enableRichTextEditor;

    public get samplePayload(): any {
        let data = null;
        if (this.data?.value && this._utilities.isValidJson(this.data.value)) {
            data = JSON.parse(this.data.value);
        }
        return {
            ...this._samplePayload,
            data: data,
            ...this.additionalData
        };
    }

    public get cannotRemoveChannel(): boolean {
        return this.channelsContent.value.length <= 1;
    }

    public channelsState: { [key: string]: ChannelState; } = {
        'inbox': { name: 'Inbox', description: 'Ειδοποίηση μέσω πρoσωπικού μήνυμα.', value: MessageChannelKind.Inbox, checked: false },
        'pushNotification': { name: 'Push Notification', description: 'Ειδοποίηση μέσω push notification στις εγγεγραμμένες συσκευές.', value: MessageChannelKind.PushNotification, checked: false },
        'email': { name: 'Email', description: 'Ειδοποίηση μέσω ηλεκτρονικού ταχυδρομείου', value: MessageChannelKind.Email, checked: false },
        'sms': { name: 'SMS', description: 'Ειδοποίηση μέσω σύντομου γραπτού μηνύματος.', value: MessageChannelKind.SMS, checked: false }
    };
    public tinyMceEditor?: Editor;
    public tinyMceOptions = {
        base_url: '/tinymce',
        suffix: '.min',
        selector: 'textarea.tinymce-editor',
        plugins: 'preview importcss searchreplace autolink directionality code visualblocks visualchars fullscreen image link codesample table charmap pagebreak nonbreaking anchor insertdatetime advlist lists wordcount help charmap quickbars emoticons',
        menubar: 'file edit view insert format tools table help',
        toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | align numlist bullist | link image media-library | table | lineheight outdent indent| forecolor backcolor removeformat | charmap emoticons codesample | code fullscreen preview',
        external_plugins: {},
        setup: (editor: Editor) => {
          this.tinyMceEditor = editor;
          editor.ui.registry.addButton('media-library', {
            text: 'Media Library',
            onAction: () => {
              this.openSidePane(this.form)
            }
          });
        },
        init_instance_callback: (editor: Editor) => {
            var initialInnerHtml: any;
            var self = this;
            editor.on('BeforeExecCommand', function (e) {
                if (e.command == "mcePreview") {
                    //store content prior to changing.
                    initialInnerHtml = editor.getContent();
                    const template = Handlebars.compile(initialInnerHtml);
                    self.bodyPreview = template(self.samplePayload);
                    editor.setContent(self.bodyPreview);
                }
            });
            editor.on("ExecCommand", function (e) {
                if (e.command == "mcePreview") {
                    //Restore initial content.
                    editor.setContent(initialInnerHtml);
                }
            });

            editor.on("change", function (e) {
                let currentIndex = self._tabGroup?.currentTab?.index ? self._tabGroup.currentTab.index - 1 : 0
                let form = self.channelsContent?.controls[currentIndex] as FormGroup;
                form.controls['body'].setValue(editor.getContent());
            });

        }
    };

    public ngOnInit(): void {
        if (!this.additionalData.actionLink) {
            this.additionalData.actionLink = new Hyperlink({
                text: "Click me!",
                href: "https://www.indice.gr"
            })
        }
        if (!this.additionalData.title) {
            this.additionalData.title = "Welcome"
        }
        if (!this.additionalData.mediaBaseHref) {
            this.additionalData.mediaBaseHref = `${app.settings.api_url}`;
            if (app.settings.enableMediaLibrary) {
                this._store.getMediaSetting('Media CDN')
                .subscribe(x => {
                    this.additionalData.mediaBaseHref = `${app.settings.api_url}/api/media`;
                    if (x?.value) {
                        this.additionalData.mediaBaseHref = x.value.endsWith('/') ? x.value.substring(0,  x.value.length - 1) : x.value;
                    }
                })
            }
        }
    }
    public ngAfterViewChecked(): void {
      if (this.enableRichTextEditor && (!tinymce.get("tinymce-editor-email") || !tinymce.get("tinymce-editor-inbox"))) {
        tinymce.init(this.tinyMceOptions);
      }
    }
    public ngOnChanges(changes: SimpleChanges): void {
        if (changes.content?.currentValue) {
            this.channelsContent.clear();
            const contentValue = changes.content.currentValue;
            let index = 0;
            for (const channel in contentValue) {
                if (Object.prototype.hasOwnProperty.call(contentValue, channel)) {
                    const content = <MessageContent>contentValue[channel];
                    const channelForm = this._formBuilder.group({
                        channel: channel,
                        sender: [content.sender],
                        subject: [content.title, Validators.required],
                        body: [content.body, Validators.required]
                    });
                    const channelState = this.getChannelState(channel);
                    if (channelState) {
                        channelState.checked = true;
                    }
                    this.channelsContent.push(channelForm);
                    if (index === 0) {
                        this._setSubjectPreview(content.title);
                        this._setBodyPreview(content.body);
                    }
                }
                index++;
            }
            if (contentValue?.email) {
                this._loadMessageSenders();
            }
        }
    }

    public senderSelectionChanged(selectedOption: MenuOption, content: FormGroup) {
        if (!selectedOption.value) {
            return;
        }
        this.selectedSenderId = selectedOption;
        let sender = selectedOption.data ? new MessageSender(selectedOption.data) : undefined;
        content.controls['sender'].setValue(sender);
    }

    public onChannelCheckboxChange(event: any): void {
        const channelsFormArray: FormArray = this.channelsContent as FormArray;
        const messageKind = event.target.value;
        const checkbox = this.channelsState[messageKind];
        if (event.target.checked) {
            const channelForm = this._formBuilder.group({
                channel: messageKind,
                sender: [null],
                subject: ['', Validators.required],
                body: ['', Validators.required]
            });
            channelsFormArray.push(channelForm);
            checkbox!.checked = true;
        } else {
            let i: number = 0;
            channelsFormArray.controls.forEach((control: AbstractControl) => {
                if (control.value.channel.toLowerCase() == messageKind.toLowerCase()) {
                    channelsFormArray.removeAt(i);
                    checkbox!.checked = false;
                    return;
                }
                i++;
            });
        }

        if (messageKind == 'email') {
            this._loadMessageSenders();
        }
    }

    public onContentTabChanged(tab: LibTabComponent): void {
        if (this.enableRichTextEditor && (tinymce.get("tinymce-editor-email") || tinymce.get("tinymce-editor-inbox"))) {
            tinymce.remove();
            tinymce.init(this.tinyMceOptions);
        }
        const index = tab.index || 0;
        this.hideMetadata = index === 0;
        const formGroup = <FormGroup>this.channelsContent.controls[index - 1];
        if (!formGroup) {
            return;
        }
        const subject = formGroup.controls['subject'].value;
        const body = formGroup.controls['body'].value;
        this._setSubjectPreview(subject);
        this._setBodyPreview(body);
    }

    public onCampaignMetadataInput(event: any): void {
        const value = event.target.value;
    }

    public onSubjectInput(content: FormGroup): void {
        const subject = content.controls['subject'].value;
        this._setSubjectPreview(subject);
    }

    public onBodyInput(content: FormGroup): void {
        const subject = content.controls['body'].value;
        this._setBodyPreview(subject);
    }

    public openMediaLibraryInNewTab() {
        let url = `${window.location.origin}/media`;
        window.open(url, '_blank');
    }

    public openSidePane(content: FormGroup) {
        this.rightPaneComponent.show();
        this._contentForm = content;
    }

    public closeSidePane() {
        this.rightPaneComponent.hide();
    }

    public async addToTextArea (file: MediaFile | undefined) {
        if (!file) {
            return;
        }
        let text = await this._fileUtilitiesService.getFileTemplate(file);
        this.tinyMceEditor?.insertContent(text)
        // if (!file) {
        //     return;
        // }
        // let text = await this._fileUtilitiesService.getFileTemplate(file);
        // let textarea = document.getElementById("body") as HTMLTextAreaElement;
        // let start_position = textarea.selectionStart;
        // let end_position = textarea.selectionEnd;

        // this._contentForm!.controls['body'].setValue(`${textarea.value.substring(0, start_position)}${text}${textarea.value.substring(end_position, textarea.value.length)}`);
        // this.onBodyInput(this._contentForm!);
        this.rightPaneComponent.hide();
    };

    public getChannelState(key: string) {
        let lowerKey = key.toLowerCase();
        switch (lowerKey) {
            case 'pushnotification':
                return this.channelsState['pushNotification'];
            default:
                return this.channelsState[lowerKey];
        }
    }

    private _setSubjectPreview(value: string | undefined): void {
        if (!value) {
            this.subjectPreview = '';
        }
        try {
            const template = Handlebars.compile(value);
            this.subjectPreview = template(this.samplePayload);
        } catch (error) { }
    }
    private _setBodyPreview(value: string | undefined): void {
        if (!value) {
            this.bodyPreview = '';
        }
        try {
            const template = Handlebars.compile(value);
            this.bodyPreview = template(this.samplePayload);
        } catch (error) { }
    }
    private _loadMessageSenders(): void {
        this._store
            .getMessageSenders()
            .pipe(map((messageSenders: MessageSenderResultSet) => {
                this.selectedSenderId = this.content?.email?.sender
                    ? new MenuOption(`${this.content.email.sender.displayName} <${this.content.email.sender.sender}>`, this.content.email.sender.id, undefined, this.content?.email?.sender)
                    : undefined;
                if (messageSenders.items) {
                    this.messageSenders = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
                    this.messageSenders.push(...messageSenders.items.map(s => {
                        let sender = {
                            id: s.id,
                            sender: s.sender,
                            displayName: s.displayName
                        }
                        if (s.isDefault) {
                            this.selectedSenderId ??= new MenuOption(`${s.displayName} <${s.sender}>`, s.id, undefined, undefined);
                            return new MenuOption(`${s.displayName} <${s.sender}>`, s.id, undefined, undefined)
                        }
                        return new MenuOption(`${s.displayName} <${s.sender}>`, s.id, undefined, sender)
                    }));
                }
            }))
            .subscribe();
    }
}
