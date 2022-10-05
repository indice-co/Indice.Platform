import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DateWidgetComponent } from './date-widget.component';

describe('DateWidgetComponent', () => {
  let component: DateWidgetComponent;
  let fixture: ComponentFixture<DateWidgetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DateWidgetComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DateWidgetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
