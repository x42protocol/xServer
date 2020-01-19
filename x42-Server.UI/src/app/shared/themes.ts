import { AppConfig } from '../../environments/environment';
import { Theme } from './theme';

export class Themes {
  themes: Theme[];

  constructor() {
    this.themes = this.getThemes();
  }

  getThemes(): Theme[] {
    var themeArray: Theme[] = [];
    var basePath: string = "../assets/themes/";
    var prodBasePath: string = "../../src/assets/themes/";
    var endPath: string = "/theme.css";

    if (AppConfig.production) {
      basePath = prodBasePath;
    }
    themeArray.push(new Theme("Rhea", basePath + "rhea" + endPath, "#666666", "#FCFCFC !important", "light"));
    themeArray.push(new Theme("Nova-Colored", basePath + "nova-colored" + endPath, "#666666", "#FCFCFC !important", "light"));
    themeArray.push(new Theme("Nova-Dark", basePath + "nova-dark" + endPath, "#666666", "#FCFCFC !important", "light"));
    themeArray.push(new Theme("Nova-light", basePath + "nova-light" + endPath, "#666666", "#FCFCFC !important", "light"));
    themeArray.push(new Theme("Luna-amber", basePath + "luna-amber" + endPath, "#dedede", "#3f3f3f !important", "dark"));
    themeArray.push(new Theme("Luna-blue", basePath + "luna-blue" + endPath, "#dedede", "#3f3f3f !important", "dark"));
    themeArray.push(new Theme("Luna-green", basePath + "luna-green" + endPath, "#dedede", "#3f3f3f !important", "dark"));
    themeArray.push(new Theme("Luna-pink", basePath + "luna-pink" + endPath, "#dedede", "#3f3f3f !important", "dark"));

    return themeArray;
  }
}
