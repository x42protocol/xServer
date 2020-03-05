import { Directive, ElementRef, OnInit, Renderer2 } from '@angular/core';

@Directive({
  selector: '[myAutoFocus]'
})
export class AutoFocusDirective implements OnInit {

  constructor(private renderer: Renderer2, private elementRef: ElementRef) { }

  ngOnInit() {
    this.elementRef.nativeElement.focus();
  }
}
