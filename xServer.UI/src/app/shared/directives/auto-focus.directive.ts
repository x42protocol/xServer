import { Directive, ElementRef, OnInit } from '@angular/core';

@Directive({
  selector: '[appMyAutoFocus]'
})
export class AutoFocusDirective implements OnInit {

  constructor(
    private elementRef: ElementRef,
  ) { }

  ngOnInit() {
    this.elementRef.nativeElement.focus();
  }
}
