import { AngularElectronPage } from './app.po';
import { browser, element, by } from 'protractor';

describe('x42-Server App', () => {
  let page: AngularElectronPage;

  beforeEach(() => {
    page = new AngularElectronPage();
  });

  it('Page title should be x42 Server', () => {
    page.navigateTo('/');
    expect(page.getTitle()).toEqual('x42 Server');
  });
});
