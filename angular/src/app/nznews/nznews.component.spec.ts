import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NZNewsComponent } from './nznews.component';

describe('NZNewsComponent', () => {
  let component: NZNewsComponent;
  let fixture: ComponentFixture<NZNewsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NZNewsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(NZNewsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
