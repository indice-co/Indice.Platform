import { tap, catchError } from 'rxjs/operators';
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { ToasterService, ToastType } from "@indice/ng-components";
import { CasesApiService, CaseTypeRequest } from "src/app/core/services/cases-api.service";
import { SubmitWidgetComponent } from "src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/submit-widget/submit-widget.component";
import { TailwindFrameworkComponent } from "src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/tailwind-framework.component";
import { EMPTY } from 'rxjs';
import { InputWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/input-widget/input-widget.component';
import { TextAreaWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/text-area-widget/text-area-widget.component';

@Injectable({
    providedIn: 'root'
})
export class CaseTypeUpdateService {

    constructor(
        private _api: CasesApiService,
        private toaster: ToasterService,
        private router: Router) { }

    public widgets = {
        "submit": SubmitWidgetComponent,
        "text": InputWidgetComponent,
        "number": InputWidgetComponent,
        "textarea": TextAreaWidgetComponent
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
            "order": {
                "type": "number"
            },
            "description": {
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
            },
            "tags": {
                "type": "string"
            },
            "config": {
                "type": "string"
            },
            "canCreateRoles": {
                "type": "string"
            },
            "isMenuItem": {
              "type": "boolean"
            },
            "gridFilterConfig": {
              "type": "string"
            },
            "gridColumnConfig": {
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
            "title": "Τύπος Υπόθεσης",
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
                            "key": "order",
                            "title": "Σειρά μέσα στην κατηγορία",
                            "htmlClass": "px-2 my-2"
                        },
                        {
                            "key": "description",
                            "title": "Περιγραφή",
                            "htmlClass": "px-2 my-2"
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
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "tags",
                            "title": "Ετικέτες",
                            "htmlClass": "px-2 my-2"
                        }
                    ]
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "config",
                            "title": "Case Type Configuration",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2"
                        }
                    ]
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "canCreateRoles",
                            "title": "Επιτρεπτοί Ρόλοι για Δημιουργία υπόθεσης (csv χωρίς κενά πχ Role1,Role2)",
                            "htmlClass": "px-2 my-2"
                        }
                    ]
                },
                {
                  "type": "flex",
                  "flex-flow": "row wrap",
                  "items": [
                    {
                      "key": "isMenuItem",
                      "title": "Is menu item",
                      "type": "checkbox",
                      "htmlClass": "px-2 my-2"
                    },
                    {
                      "key": "gridFilterConfig",
                      "title": "Grid filter config",
                      "type": "textarea",
                      "htmlClass": "px-2 my-2"
                    },
                    {
                      "key": "gridColumnConfig",
                      "title": "Grid column config",
                      "type": "textarea",
                      "htmlClass": "px-2 my-2"
                    }
                  ]
              },
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
            // TODO with recursion
            createLayout.forEach((element: any) => {
                activateElement(element);
                if (element.hasOwnProperty('items')) { // ajsf sections have items!
                    element.items.forEach((item: any) => {
                        activateElement(item);
                        if (item.hasOwnProperty('items')) { // ajsf flex containers have items!
                            item.items.forEach((i: any) => {
                                activateElement(i);
                                if (i.hasOwnProperty('items')) {
                                    i.items.forEach((j: any) => {
                                        activateElement(j);
                                        if (j.hasOwnProperty('items')) {
                                            j.items.forEach((k: any) => {
                                                activateElement(k);
                                            })
                                        }
                                    });
                                }
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
            order: event.order,
            description: event.description,
            code: event.code,
            dataSchema: event.dataSchema,
            layout: event?.layout,
            translations: event?.translations,
            layoutTranslations: event?.layoutTranslations,
            tags: event?.tags,
            config: event?.config,
            canCreateRoles: event?.canCreateRoles,
            isMenuItem: event?.isMenuItem,
            gridFilterConfig: event?.gridFilterConfig,
            gridColumnConfig: event?.gridColumnConfig
        });
        this._api.createCaseType(undefined, request).pipe(
            tap(_ => {
                this.toaster.show(ToastType.Success, "Επιτυχία", "Η δημιουργία τύπου υπόθεσης ήταν επιτυχής.")
                this.router.navigate(['/case-types']);
            }),
            catchError(err => {
                this.toaster.show(ToastType.Error, "Whoops!", err.detail)
                return EMPTY
            })
        ).subscribe();
    }

    public onEditSubmit(caseTypeId: string, event: any) {
        const request = new CaseTypeRequest({
            id: caseTypeId,
            title: event.title,
            order: event.order,
            description: event.description,
            code: event.code,
            dataSchema: event.dataSchema,
            layout: event?.layout,
            translations: event?.translations,
            layoutTranslations: event?.layoutTranslations,
            tags: event?.tags,
            config: event?.config,
            canCreateRoles: event?.canCreateRoles,
            isMenuItem: event?.isMenuItem,
            gridFilterConfig: event?.gridFilterConfig,
            gridColumnConfig: event?.gridColumnConfig
        })
        this._api.updateCaseType(caseTypeId, undefined, request).pipe(
            tap(_ => {
                this.toaster.show(ToastType.Success, "Επιτυχία!", "Η επεξεργασία του τύπου υπόθεσης ήταν επιτυχής")
            }),
            catchError(err => {
                this.toaster.show(ToastType.Error, "Whoops!", err.detail)
                return EMPTY
            })
        ).subscribe();
    }
}
