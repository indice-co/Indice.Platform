import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PayloadDialogComponent } from './payload-dialog.component';

describe('PayloadDialogComponent', () => {
  let fixture: ComponentFixture<PayloadDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PayloadDialogComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(PayloadDialogComponent);
  });

  it('preserves precise decimal tokens when editing and submitting stored JSON', () => {
    const payload = '{"amount":0.1234567890123456789012345678}';
    const component = fixture.componentInstance;
    const el = fixture.nativeElement as HTMLElement;
    let submitted: string | undefined;
    component.submitted.subscribe((result) => (submitted = result.payload));

    component.open(payload);
    fixture.detectChanges();
    expect(el.querySelector<HTMLTextAreaElement>('textarea')!.value).toBe(payload);

    el.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    expect(submitted).toBe(payload);
  });
});
