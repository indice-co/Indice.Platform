import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LabelOnlyComponent } from './label-only.component';

describe('LabelOnlyComponent', () => {
  let component: LabelOnlyComponent;
  let fixture: ComponentFixture<LabelOnlyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LabelOnlyComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LabelOnlyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
