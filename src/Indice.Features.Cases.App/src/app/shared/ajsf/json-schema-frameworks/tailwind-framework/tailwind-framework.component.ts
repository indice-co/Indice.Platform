import {
  ChangeDetectorRef,
  Component,
  Input,
  OnChanges,
  OnInit
} from '@angular/core';

import { JsonSchemaFormService, addClasses, inArray } from '@ajsf-extended/core';
import { cloneDeep, map } from 'lodash';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';

/**
 * Tailwind framework for Angular JSON Schema Form.
 *
 */
@Component({
  selector: 'tailwind-framework',
  templateUrl: './tailwind-framework.component.html',
  styleUrls: ['./tailwind-framework.component.scss']
})
export class TailwindFrameworkComponent implements OnInit, OnChanges {
  frameworkInitialized = false;
  widgetOptions: any; // Options passed to child widget
  widgetLayoutNode: any; // layoutNode passed to child widget
  options: any; // Options used in this framework
  formControl: any = null;
  debugOutput: any = '';
  debug: any = '';
  parentArray: any = null;
  isOrderable = false;
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] = [];
  @Input() dataIndex: number[] = [];

  constructor(
    private _caseDetailsService: CaseDetailsService,
    public changeDetector: ChangeDetectorRef,
    public jsf: JsonSchemaFormService
  ) { }

  get showRemoveButton(): boolean {
    if (!this.options.removable || this.options.readonly ||
      this.layoutNode.type === '$ref'
    ) {
      return false;
    }
    if (this.layoutNode.recursiveReference) {
      return true;
    }
    if (!this.layoutNode.arrayItem || !this.parentArray) {
      return false;
    }
    // If array length <= minItems, don't allow removing any items
    return this.parentArray.items.length - 1 <= this.parentArray.options.minItems ? false :
      // For removable list items, allow removing any item
      this.layoutNode.arrayItemType === 'list' ? true :
        // For removable tuple items, only allow removing last item in list
        this.layoutIndex![this.layoutIndex!.length - 1] === this.parentArray.items.length - 2;
  }

  ngOnInit() {
    this.initializeFramework();
    if (this.layoutNode.arrayItem && this.layoutNode.type !== '$ref') {
      this.parentArray = this.jsf.getParentNode(this);
      if (this.parentArray) {
        this.isOrderable = this.layoutNode.arrayItemType === 'list' &&
          !this.options.readonly && this.parentArray.options.orderable;
      }
    }
  }

  ngOnChanges() {
    if (!this.frameworkInitialized) {
      this.initializeFramework();
    }
  }

  initializeFramework() {
    if (this.layoutNode) {
      // 'case-channel' option indicates whether the field is visible based on case's channel ('Agent' or 'Customer')
      if (this.layoutNode.options['case-channel'] && this.layoutNode.options['case-channel'] !== this._caseDetailsService.caseDetails?.channel) {
        this.layoutNode.options.condition = 'false';
      }
      this.options = cloneDeep(this.layoutNode.options);
      this.widgetLayoutNode = {
        ...this.layoutNode,
        options: cloneDeep(this.layoutNode.options)
      };
      this.widgetOptions = this.widgetLayoutNode.options;
      this.formControl = this.jsf.getFormControl(this);

      this.options.isInputWidget = inArray(this.layoutNode.type, [
        'button', 'checkbox', 'checkboxes-inline', 'checkboxes', 'color',
        'date', 'datetime-local', 'datetime', 'email', 'file', 'hidden',
        'image', 'integer', 'month', 'number', 'password', 'radio',
        'radiobuttons', 'radios-inline', 'radios', 'range', 'reset', 'search',
        'select', 'submit', 'tel', 'text', 'textarea', 'time', 'url', 'week'
      ]);

      this.options.title = this.setTitle();

      this.options.htmlClass =
        addClasses(this.options.htmlClass, 'schema-form-' + this.layoutNode.type);
      this.options.htmlClass =
        this.layoutNode.type === 'array' ?
          addClasses(this.options.htmlClass, 'list-group') :
          this.layoutNode.arrayItem && this.layoutNode.type !== '$ref' ?
            addClasses(this.options.htmlClass, 'list-group-item') :
            addClasses(this.options.htmlClass, 'form-group');
      this.widgetOptions.htmlClass = '';
      this.options.labelHtmlClass =
        addClasses(this.options.labelHtmlClass, 'form-label inline-block mb-1 text-gray-700');
      this.widgetOptions.activeClass =
        addClasses(this.widgetOptions.activeClass, 'active');
      this.options.fieldAddonLeft =
        this.options.fieldAddonLeft || this.options.prepend;
      this.options.fieldAddonRight =
        this.options.fieldAddonRight || this.options.append;

      // Add asterisk to titles if required
      if (this.options.title && this.layoutNode.type !== 'tab' &&
        !this.options.notitle && this.options.required &&
        !this.options.title.includes('*')
      ) {
        this.options.title += ' <strong class="text-red-500">*</strong>';
      }
      // Set miscellaneous styles and settings for each control type
      switch (this.layoutNode.type) {
        // Checkbox controls
        case 'checkbox':
        case 'checkboxes':
          this.widgetOptions.htmlClass = addClasses(
            this.widgetOptions.htmlClass, 'checkbox');
          break;
        case 'checkboxes-inline':
          this.widgetOptions.htmlClass = addClasses(
            this.widgetOptions.htmlClass, 'checkbox');
          this.widgetOptions.itemLabelHtmlClass = addClasses(
            this.widgetOptions.itemLabelHtmlClass, 'checkbox-inline');
          break;
        // Radio controls
        case 'radio':
        case 'radios':
          this.widgetOptions.htmlClass = addClasses(
            this.widgetOptions.htmlClass, 'radio');
          break;
        case 'radios-inline':
          this.widgetOptions.htmlClass = addClasses(
            this.widgetOptions.htmlClass, 'radio');
          this.widgetOptions.itemLabelHtmlClass = addClasses(
            this.widgetOptions.itemLabelHtmlClass, 'radio-inline');
          break;
        // Button sets - checkboxbuttons and radiobuttons
        case 'checkboxbuttons':
        case 'radiobuttons':
          this.widgetOptions.htmlClass = addClasses(
            this.widgetOptions.htmlClass, 'btn-group');
          this.widgetOptions.itemLabelHtmlClass = addClasses(
            this.widgetOptions.itemLabelHtmlClass, 'btn');
          this.widgetOptions.itemLabelHtmlClass = addClasses(
            this.widgetOptions.itemLabelHtmlClass, this.options.style || 'btn-default');
          this.widgetOptions.fieldHtmlClass = addClasses(
            this.widgetOptions.fieldHtmlClass, 'sr-only');
          break;
        // Single button controls
        case 'button':
        case 'submit':
          // if this is a submit button and it is readonly, hide it
          if (this.widgetOptions.readonly) {
            this.widgetOptions.fieldHtmlClass = addClasses(this.widgetOptions.fieldHtmlClass, "hidden");
          }
          break;
        // Containers - arrays and fieldsets
        case 'array':
          this.options.messageLocation = 'top';
          if (this.widgetOptions.readonly) {
            this.widgetOptions.addable = false;
            this.widgetOptions.orderable = false;
            this.widgetOptions.removable = false;
          }
          break;
        case 'fieldset':
        case 'section':
        case 'conditional':
        case 'advancedfieldset':
        case 'authfieldset':
        case 'selectfieldset':
        case 'optionfieldset':
          this.options.messageLocation = 'top';
          break;
        case 'tabarray':
        case 'tabs':
          this.widgetOptions.htmlClass = addClasses(
            this.widgetOptions.htmlClass, 'tab-content');
          this.widgetOptions.fieldHtmlClass = addClasses(
            this.widgetOptions.fieldHtmlClass, 'tab-pane');
          this.widgetOptions.labelHtmlClass = addClasses(
            this.widgetOptions.labelHtmlClass, 'nav nav-tabs');
          break;
        // 'Add' buttons - references
        case '$ref':
          // Apply max items on parent readonly array.
          // This will disable Add button from showing up.
          var parentNode = this.jsf.getParentNode(this);
          if (parentNode?.type == 'array' && parentNode?.options?.readonly) {
            this.widgetOptions.maxItems = parentNode.options.listItems;
            break;
          }
          this.widgetOptions.fieldHtmlClass = addClasses(
            this.widgetOptions.fieldHtmlClass, 'btn btn-info float-right m-2');
          this.widgetOptions.fieldHtmlClass = addClasses(
            this.widgetOptions.fieldHtmlClass, this.options.style || 'btn-default');
          this.options.icon = 'glyphicon glyphicon-plus';
          break;
        // Default - including regular inputs
        default:
          this.widgetOptions.fieldHtmlClass = addClasses(
            this.widgetOptions.fieldHtmlClass, 'form-control');
          if (this.widgetOptions.readonly) {
            this.widgetOptions.fieldHtmlClass = addClasses(this.widgetOptions.fieldHtmlClass, "read-only:bg-gray-100");
          }
      }

      if (this.formControl) {
        this.updateHelpBlock(this.formControl.status);
        this.formControl.statusChanges.subscribe((status: any) => this.updateHelpBlock(status));

        if (this.options.debug) {
          const vars: any[] = [];
          this.debugOutput = map(vars, (thisVar: any) => JSON.stringify(thisVar, null, 2)).join('\n');
        }
      }
      this.frameworkInitialized = true;
    }

  }

  updateHelpBlock(status: any) {
    this.options.helpBlock = status === 'INVALID' &&
      this.options.enableErrorState && this.formControl.errors &&
      (this.formControl.dirty || this.options.feedbackOnRender) ?
      this.jsf.formatErrors(this.formControl.errors, this.options.validationMessages) :
      this.options.description || this.options.help || null;
  }

  setTitle(): string | null {
    switch (this.layoutNode.type) {
      case 'button':
      case 'checkbox':
      case 'section':
      case 'help':
      case 'msg':
      case 'submit':
      case 'message':
      case 'tabarray':
      case 'tabs':
      case '$ref':
        return null;
      case 'advancedfieldset':
        this.widgetOptions.expandable = true;
        this.widgetOptions.title = 'Advanced options';
        return null;
      case 'authfieldset':
        this.widgetOptions.expandable = true;
        this.widgetOptions.title = 'Authentication settings';
        return null;
      case 'fieldset':
        this.widgetOptions.title = this.options.title;
        return null;
      case 'label-only':
        this.widgetOptions.title = this.options.title;
        this.widgetOptions.extraType = this.options.extraType;
        return null;
      default:
        this.widgetOptions.title = null;
        return this.jsf.setItemTitle(this);
    }
  }

  removeItem() {
    this.jsf.removeItem(this);
  }
}
