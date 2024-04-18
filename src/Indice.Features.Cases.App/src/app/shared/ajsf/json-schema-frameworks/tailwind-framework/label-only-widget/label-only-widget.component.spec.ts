import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LabelOnlyWidgetComponent } from './label-only-widget.component';

describe('LabelOnlyWidgetComponent', () => {
  let component: LabelOnlyWidgetComponent;
  let fixture: ComponentFixture<LabelOnlyWidgetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LabelOnlyWidgetComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LabelOnlyWidgetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
