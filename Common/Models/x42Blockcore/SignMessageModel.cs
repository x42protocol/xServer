
namespace Common.Models.x42Blockcore
{
    public class SignMessageModel
    {

        public string WalletName { get; set; }
        public string AccountName { get; set; }
        public string Password { get; set; }
        public string ExternalAddress { get; set; }
        public string Message { get; set; }

        public SignMessageModel(string message)
        {
            WalletName = "WordpressPreview";
            Message = message;
            AccountName = "coldStakingColdAddresses";
            Password = "!Coco1nut";
            ExternalAddress = "XJ4Vnin64v9hqp5gD6UvAjx4dCdmHS6SXm";
        }
    }

}

