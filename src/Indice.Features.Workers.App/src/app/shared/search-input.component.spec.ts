import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchInputComponent } from './search-input.component';

describe('SearchInputComponent', () => {
  let fixture: ComponentFixture<SearchInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchInputComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchInputComponent);
    fixture.componentRef.setInput('debounce', 10);
  });

  it('cancels a pending search when an external value is accepted', async () => {
    const search = jasmine.createSpy('search');
    fixture.componentInstance.search.subscribe(search);
    fixture.componentRef.setInput('value', 'applied');
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'stale';
    input.dispatchEvent(new Event('input'));

    fixture.componentRef.setInput('value', '');
    fixture.detectChanges();
    await new Promise((resolve) => setTimeout(resolve, 20));

    expect(search).not.toHaveBeenCalled();
  });
});
