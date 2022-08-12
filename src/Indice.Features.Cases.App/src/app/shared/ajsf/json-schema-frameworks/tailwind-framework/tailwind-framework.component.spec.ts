import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TailwindFrameworkComponent } from './tailwind-framework.component';

describe('TailwindFrameworkComponent', () => {
  let component: TailwindFrameworkComponent;
  let fixture: ComponentFixture<TailwindFrameworkComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ TailwindFrameworkComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(TailwindFrameworkComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
