export class ServerIDResponse {
  public address: string;
  public serverId: string = '';

  public setServerId(address: string) {
    this.serverId = "SID" + this.Base64EncodeUrl(btoa(address));
  }

  public getAddressFromServerId() {
    return this.Base64DecodeUrl(atob(this.serverId.substring(3)));
  }

  /**
  * use this to make a Base64 encoded string URL friendly,
  * i.e. '+' and '/' are replaced with '-' and '_' also any trailing '='
  * characters are removed
  *
  * @param {String} str the encoded string
  * @returns {String} the URL friendly encoded String
  */
  private Base64EncodeUrl(str) {
    return str.replace(/\+/g, '-').replace(/\//g, '_').replace(/\=+$/, '');
  }

  /**
   * Use this to recreate a Base64 encoded string that was made URL friendly 
   * using Base64EncodeurlFriendly.
   * '-' and '_' are replaced with '+' and '/' and also it is padded with '+'
   *
   * @param {String} str the encoded string
   * @returns {String} the URL friendly encoded String
   */
  private Base64DecodeUrl(str) {
    str = (str + '===').slice(0, str.length + (str.length % 4));
    return str.replace(/-/g, '+').replace(/_/g, '/');
  }
}
