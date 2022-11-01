import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CaseCustomActionComponent } from './case-custom-action.component';

describe('CaseCustomActionComponent', () => {
  let component: CaseCustomActionComponent;
  let fixture: ComponentFixture<CaseCustomActionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CaseCustomActionComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CaseCustomActionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
