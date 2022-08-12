import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { ToasterService, ToastType } from "@indice/ng-components";
import { CasesApiService, CaseTypeRequest } from "src/app/core/services/cases-api.service";
import { TailwindSubmitWidgetComponent } from "src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/submit-widget/submit-widget.component";
import { TailwindFrameworkComponent } from "src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/tailwind-framework.component";

@Injectable({
    providedIn: 'root'
})
export class CaseTypesService {

    constructor(
        private _api: CasesApiService,
        private toaster: ToasterService,
        private router: Router) { }

    public widgets = {
        "submit": TailwindSubmitWidgetComponent
    };

    public framework = {
        framework: TailwindFrameworkComponent
    };

    public schema: any = {
        "type": "object",
        "properties": {
            "code": {
                "type": "string"
            },
            "title": {
                "type": "string"
            },
            "dataSchema": {
                "type": "string"
            },
            "layout": {
                "type": "string"
            },
            "translations": {
                "type": "string"
            },
            "layoutTranslations": {
                "type": "string"
            }
        },
        "additionalProperties": false,
        "required": [
            "code",
            "title",
            "dataSchema"
        ]
    }

    public layout: any = [
        {
            "type": "section",
            "title": "Νέος Τύπος Αίτησης",
            "labelHtmlClass": "px-2",
            "items": [
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "code",
                            "title": "Κωδικός",
                            "htmlClass": "px-2 my-2",
                            "readonly": "true",
                            "validationMessages": {
                                "required": "Υποχρεωτικό Πεδίο."
                            }
                        },
                        {
                            "key": "title",
                            "title": "Τίτλος",
                            "htmlClass": "px-2 my-2",
                            "validationMessages": {
                                "required": "Υποχρεωτικό Πεδίο."
                            }
                        }
                    ]
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "dataSchema",
                            "title": "Schema",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2",
                            "validationMessages": {
                                "required": "Υποχρεωτικό Πεδίο."
                            }
                        },
                        {
                            "key": "layout",
                            "title": "Layout",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2",
                        },
                    ]
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "translations",
                            "title": "Μετάφραση",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2"
                        },
                        {
                            "key": "layoutTranslations",
                            "title": "Μετάφραση Layout",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2"
                        }
                    ]
                }
            ]
        }
    ]

    public onLoadLayout(id?: string): any {
        if (id == null) {
            // TODO With structuredClone https://developer.mozilla.org/en-US/docs/Web/API/structuredClone
            const createLayout = JSON.parse(JSON.stringify(this.layout))
            const activateElement = (element: any) => {
                delete element.readonly;
            }
            createLayout.forEach((element: any) => {
                activateElement(element);
                if (element.hasOwnProperty('items')) { // ajsf sections have items!
                    element.items.forEach((item: any) => {
                        activateElement(item);
                        if (item.hasOwnProperty('items')) { // ajsf flex containers have items!
                            item.items.forEach((i: any) => {
                                activateElement(i);
                            });
                        }
                    });
                }
            });
            return createLayout;
        } else {
            return this.layout;
        }
    }

    public onCreateSubmit(event: any): void {
        const request = new CaseTypeRequest({
            title: event.title,
            code: event.code,
            dataSchema: event.dataSchema,
            layout: event?.layout,
            translations: event?.translations,
            layoutTranslations: event?.layoutTranslations
        })
        this._api.createCaseType(undefined, request).subscribe(_ => {
            this.toaster.show(ToastType.Success, "Επιτυχία", "Η δημιουργία τύπου αίτησης ήταν επιτυχής")
            this.router.navigate(['/case-types']);
        },
            (err) => {
                this.toaster.show(ToastType.Error, "Ουπς!", "Ο κωδικός τύπου αίτησης υπάρχει ήδη")
            }
        )
    }

    public onEditSubmit(caseTypeId: string, event: any) {
        const request = new CaseTypeRequest({
            id: caseTypeId,
            title: event.title,
            code: event.code,
            dataSchema: event.dataSchema,
            layout: event?.layout,
            translations: event?.translations,
            layoutTranslations: event?.layoutTranslations
        })

        this._api.updateCaseType(caseTypeId, undefined, request).subscribe(_ => {
            this.toaster.show(ToastType.Success, "Επιτυχία!", "Η επεξεργασία του τύπου αίτησης ήταν επιτυχής")
        },
            (err) => {
                this.toaster.show(ToastType.Error, "Ουπς!", "Κάτι πήγε στραβά")
            })
    }
}