import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ShowMnemonicComponent } from './show-mnemonic.component';

describe('ShowMnemonicComponent', () => {
  let component: ShowMnemonicComponent;
  let fixture: ComponentFixture<ShowMnemonicComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ShowMnemonicComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ShowMnemonicComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
