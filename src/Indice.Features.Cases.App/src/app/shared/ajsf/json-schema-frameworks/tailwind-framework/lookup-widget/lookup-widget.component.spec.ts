import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LookupWidgetComponent } from './lookup-widget.component';

describe('LookupWidgetComponent', () => {
  let component: LookupWidgetComponent;
  let fixture: ComponentFixture<LookupWidgetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LookupWidgetComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LookupWidgetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
