import { AngularElectronPage } from './app.po';
import { browser, element, by } from 'protractor';

describe('xServer App', () => {
  let page: AngularElectronPage;

  beforeEach(() => {
    page = new AngularElectronPage();
  });

  it('Page title should be xServer', () => {
    page.navigateTo('/');
    expect(page.getTitle()).toEqual('xServer');
  });
});
