export class ProfileResult {
  constructor(name: string, keyAddress: string) {
    this.name = name;
    this.keyAddress = keyAddress;
  }

  public name: string;
  public keyAddress: string;
  public signature: string;
  public priceLockId: string;
  public profileFields: [ProfileField];
}

class ProfileField {
  constructor(value: string, signature: string) {
    this.value = value;
    this.signature = signature;
  }

  public value: string;
  public signature: string;
}
