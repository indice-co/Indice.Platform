import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CanvasTileComponent } from './canvas-tile.component';

describe('CanvasTileComponent', () => {
  let component: CanvasTileComponent;
  let fixture: ComponentFixture<CanvasTileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CanvasTileComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CanvasTileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
