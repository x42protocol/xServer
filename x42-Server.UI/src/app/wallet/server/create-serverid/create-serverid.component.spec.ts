import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateServerIDComponent } from './create-serverid.component';

describe('CreateServerIDComponent', () => {
  let component: CreateServerIDComponent;
  let fixture: ComponentFixture<CreateServerIDComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [CreateServerIDComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateServerIDComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
