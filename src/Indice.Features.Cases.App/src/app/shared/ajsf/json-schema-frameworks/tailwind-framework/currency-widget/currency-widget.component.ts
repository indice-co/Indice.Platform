import { JsonSchemaFormService } from '@ajsf-extended/core';
import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil, map, tap } from 'rxjs/operators';

@Component({
  selector: 'app-currency-widget',
  templateUrl: './currency-widget.component.html',
  styleUrls: ['./currency-widget.component.scss']
})
export class CurrencyWidgetComponent implements OnInit, OnDestroy {
  formControl: any;
  controlName: string | undefined;
  controlValue: string | undefined;
  controlDisabled = false;
  boundControl = false;
  options: any;
  autoCompleteList: string[] = [];
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] = [];
  @Input() dataIndex: number[] = [];

  thousandSeparator: string = ".";
  // This is the placeholder for the mask input. The actual control value is a hidden input
  displayValue = '';
  // Specify type parameter for better type safety
  private destroy$ = new Subject<void>();
  constructor(
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit() {
    this.options = this.layoutNode.options || {};
    this.jsf.initializeControl(this);
    // Initialize displayValue if necessary
    if (this.formControl.value) {
      this.displayValue = parseFloat(this.formControl.value).toLocaleString('el');
    }
    // Subscribe to formControl value changes in order to inform UI
    this.formControl.valueChanges.pipe(
      takeUntil(this.destroy$),
      map((value: string) =>
        parseFloat(value).toLocaleString('el')
      ),
      tap((value: string) => {
        this.displayValue = value;
      })
    ).subscribe();
  }

  ngOnDestroy() {
    // Emit undefined to ensure type safety
    this.destroy$.next(undefined);
    this.destroy$.complete();
  }

  updateValue(event: any) {
    // Force replace masked value input into global decimal format (eg 5.125.000,03 --> 5125000.03)
    const controlValue = parseFloat(event.target.value.replace(/[.]/g, '').replace(/[,]/g, '.'));
    this.displayValue = event.target.value;
    this.jsf.updateValue(this, controlValue);
  }
}
