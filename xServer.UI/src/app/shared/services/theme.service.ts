import { Component, OnInit, Injectable } from '@angular/core';
import { Http, Response, Headers } from '@angular/http';
import { Router } from '@angular/router';

import { Theme } from '../theme';
import { Themes } from '../themes';
import { Observable, Subject } from 'rxjs';

@Injectable()
export class ThemeService {
  themes: Themes = new Themes();
  selectedTheme: string;
  private theme$: Subject<Theme>;

  constructor(private router: Router) {
    this.theme$ = <Subject<Theme>>new Subject();
  }

  getThemes() {
    return this.themes.getThemes();
  }

  getLogo() {
    let logoFileName: string;
    var theme = this.findTheme(this.selectedTheme);
    if (theme.themeType == "dark") {
      logoFileName = "logo_white.png";
    } else {
      logoFileName = "logo_black.png";
    }
    if (theme.name == "Luna-pink") {
      logoFileName = "logo_pink.png";
    }
    return logoFileName;
  }

  getCurrentTheme() {
    return this.findTheme(localStorage.getItem('theme'));
  }

  setTheme(theme: string = null) {
    if (theme === undefined || theme === null || theme == "") {
      theme = localStorage.getItem('theme');
      theme = (theme === null || theme === undefined || theme === "" ? "Rhea" : theme);
    }

    this.selectedTheme = theme;
    var ft = this.findTheme(this.selectedTheme);
    var d = document.getElementById('themeStyleSheet')
    d.setAttribute('href', this.getThemePath(theme));
    this.setNewTheme(ft);

    var b = document.getElementById('htmlStyle')
    b.setAttribute('style', this.buildStyle(this.getAppPageHeaderDivStyle()));

    var b = document.getElementById('pageBody')
    b.setAttribute('style', this.buildStyle(this.getAppPageHeaderDivStyle()));

    localStorage.setItem('theme', theme);
  }

  buildStyle(obj: Object) {
    var str = '';
    for (var p in obj) {
      if (obj.hasOwnProperty(p)) {
        str += p + ':' + obj[p] + ';';
      }
    }
    return str;
  }

  findTheme(themeName: string): Theme {
    var theme: Theme;
    for (let t of this.themes.getThemes()) {
      if (t.name === themeName) {
        theme = t;
        break;
      }
    }

    return theme;
  }

  getThemePath(theme: string): string {
    var path = "";
    for (let t of this.themes.getThemes()) {
      if (t.name === theme) {
        path = t.path;
        break;
      }
    }

    return path;
  }

  getAppPageHeaderDivStyle(): Object {
    var style = { "background-color": "", "padding": "", "color": "" };
    this.selectedTheme = (this.selectedTheme === null || this.selectedTheme === undefined || this.selectedTheme === "" ? "Rhea" : this.selectedTheme);
    var theme = this.findTheme(this.selectedTheme);
    style["background-color"] = theme.contentBackgroundColor;
    style["color"] = theme.contentColor;
    return style;
  }

  setNewTheme(theme: Theme): void {
    this.theme$.next(theme);
  }

  getNewTheme(): Observable<Theme> {
    return this.theme$.asObservable();
  }

}
