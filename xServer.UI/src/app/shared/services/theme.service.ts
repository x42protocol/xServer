import { Injectable } from '@angular/core';
import { Theme } from '../theme';
import { Themes } from '../themes';
import { Observable, Subject, BehaviorSubject } from 'rxjs';

@Injectable()
export class ThemeService {
  public logoFileName = new BehaviorSubject<string>('logo_black.png');
  themes: Themes = new Themes();
  selectedTheme: string;
  private theme$: Subject<Theme>;

  constructor() {
    this.theme$ = new Subject();
  }

  getThemes() {
    return this.themes.getThemes();
  }

  getLogo() {
    let logoFileName: string;
    const theme = this.findTheme(this.selectedTheme);
    if (theme.themeType === 'dark') {
      logoFileName = 'logo_white.png';
    } else {
      logoFileName = 'logo_black.png';
    }
    if (theme.name === 'Luna-pink') {
      logoFileName = 'logo_pink.png';
    }
    return logoFileName;
  }

  getCurrentTheme() {
    return this.findTheme(localStorage.getItem('theme'));
  }

  setTheme(theme: string = null) {
    if (theme === undefined || theme === null || theme === '') {
      theme = localStorage.getItem('theme');
      theme = (theme === null || theme === undefined || theme === '' ? 'Rhea' : theme);
    }

    this.selectedTheme = theme;
    const ft = this.findTheme(this.selectedTheme);
    const themeStyleSheet = document.getElementById('themeStyleSheet');
    themeStyleSheet.setAttribute('href', this.getThemePath(theme));
    this.setNewTheme(ft);

    const hStyle = document.getElementById('htmlStyle');
    hStyle.setAttribute('style', this.buildStyle(this.getAppPageHeaderDivStyle()));

    const pBody = document.getElementById('pageBody');
    pBody.setAttribute('style', this.buildStyle(this.getAppPageHeaderDivStyle()));

    this.logoFileName.next(this.getLogo());

    localStorage.setItem('theme', theme);
  }

  buildStyle(obj: object) {
    let str = '';
    for (const p in obj) {
      if (obj.hasOwnProperty(p)) {
        str += p + ':' + obj[p] + ';';
      }
    }
    return str;
  }

  findTheme(themeName: string): Theme {
    let themeResult: Theme;
    for (const theme of this.themes.getThemes()) {
      if (theme.name === themeName) {
        themeResult = theme;
        break;
      }
    }

    return themeResult;
  }

  getThemePath(themeName: string): string {
    let pathResult = '';
    for (const theme of this.themes.getThemes()) {
      if (theme.name === themeName) {
        pathResult = theme.path;
        break;
      }
    }

    return pathResult;
  }

  getAppPageHeaderDivStyle(): object {
    const style = { 'background-color': '', padding: '', color: '' };
    this.selectedTheme = (this.selectedTheme === null || this.selectedTheme === undefined || this.selectedTheme === '' ? 'Rhea' : this.selectedTheme);
    const theme = this.findTheme(this.selectedTheme);
    style['background-color'] = theme.contentBackgroundColor;
    const colorIndex = 'color';
    style[colorIndex] = theme.contentColor;
    return style;
  }

  setNewTheme(theme: Theme): void {
    this.theme$.next(theme);
  }

  getNewTheme(): Observable<Theme> {
    return this.theme$.asObservable();
  }

}
