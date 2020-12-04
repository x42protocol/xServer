import { Injectable, Inject, InjectionToken, Optional } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, map, mergeMap } from 'rxjs/operators';
import { DOCUMENT } from '@angular/common';
import { BehaviorSubject, Observable } from 'rxjs';

export const APP_TITLE = new InjectionToken<string>('App Title Postfix');

@Injectable({
  providedIn: 'root'
})
export class TitleService {

  static singletonInstance: TitleService;

  constructor(
    private readonly router: Router,
    @Inject(DOCUMENT) private readonly document: any,
    @Optional() @Inject(APP_TITLE) private readonly appTitle: any) {

    if (!TitleService.singletonInstance) {
      this.initialize();
      TitleService.singletonInstance = this;
    }

    return TitleService.singletonInstance;
  }

  get $title(): Observable<string> {
    return this.title.asObservable();
  }

  private readonly title = new BehaviorSubject<string>(this.document.title);

  initialize() {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map(() => this.router.routerState.root),
      map(route => {
        while (route.firstChild) {
          route = route.firstChild;
        }
        return route;
      }),
      filter(route => route.outlet === 'primary'),
      mergeMap(route => route.data),
      // tslint:disable-next-line: no-string-literal
      map(data => ({ title: data['title'], prefix: data['prefix'] })),
    ).subscribe(title => {

      let formattedTitle = title.title != null ? title.title : '';

      if (title.prefix != null && formattedTitle !== '') {
        formattedTitle = title.prefix + ' - ' + formattedTitle;
      } else if (title.prefix != null) {
        formattedTitle = title.prefix;
      }

      // For the document title, we'll append the app title.
      if (this.appTitle != null && formattedTitle !== '') {
        this.document.title = this.appTitle + ' - ' + formattedTitle;
      } else if (title.prefix != null) {
        this.document.title = this.appTitle;
      }

      this.title.next(formattedTitle);
    });
  }
}
