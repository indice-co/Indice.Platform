import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectWidgetComponent } from './select-widget.component';

describe('SelectWidgetComponent', () => {
  let component: SelectWidgetComponent;
  let fixture: ComponentFixture<SelectWidgetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SelectWidgetComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SelectWidgetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
