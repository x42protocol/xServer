export class Theme {
  name: string;
  path: string;
  contentColor: string;
  contentBackgroundColor: string;
  themeType: string;

  constructor(name: string, path: string, contentColor: string, contentBackgroundColor: string, themeType: string) {
    this.name = name;
    this.path = path;
    this.contentColor = contentColor;
    this.contentBackgroundColor = contentBackgroundColor;
    this.themeType = themeType;
  }
}
