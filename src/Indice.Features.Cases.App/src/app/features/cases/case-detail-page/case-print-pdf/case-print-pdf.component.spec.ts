import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CasePrintPdfComponent } from './case-print-pdf.component';

describe('CasePrintPdfComponent', () => {
  let component: CasePrintPdfComponent;
  let fixture: ComponentFixture<CasePrintPdfComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CasePrintPdfComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CasePrintPdfComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
