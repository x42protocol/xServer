using System.Collections.Generic;
using X42.Feature.X42Client.Models;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Utilities;

namespace X42.Feature.X42Client.Utils.Extensions
{
    public static class RestModelExtensions
    {
        /// <summary>
        ///     API Returns The Amount Without a Decimal Point (e.g. 10000000 should be 1.0000000)
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static decimal ParseAPIAmount(this long amount)
        {
            decimal returnValue = 0;
            string amountStr = $"{amount}";

            //are we dealing with non-whole numbers (e.g 0.10000000)
            if ($"{amount}".Length == 8)
            {
                if (!decimal.TryParse($"0.{amount}", out returnValue))
                {
                    return -1;
                }

                return returnValue;
            } //end of if($"{amount}".Length == 8)


            //2000000000

            string newAmountWhole = amountStr.Substring(0, amountStr.Length - 8);
            string newAmountRemainder = amountStr.Substring(amountStr.Length - 8);

            if (!decimal.TryParse($"{newAmountWhole}.{newAmountRemainder}", out returnValue))
            {
                return -1;
            }

            return returnValue;
        } //end of public static decimal ParseAPIAmount(this long amount)

        /// <summary>
        ///     Converts the API Peer List Data Structure To A More Friendly One
        /// </summary>
        public static BlockHeader ToBlockHeader(this GetBlockResponse block)
        {
            Guard.Null(block, nameof(block));


            return new BlockHeader
            {
                Height = block.height,
                Version = block.version,
                MerkleRoot = block.merkleroot,
                Nonce = block.nonce,
                PreviousBlockHash = block.previousblockhash,
                Time = block.time
            };
        } //end of public static Block ToBlock(this GetBlockResponse block)


        /// <summary>
        ///     Converts the API Peer List Data Structure To A More Friendly One
        /// </summary>
        public static List<Peer> ToPeersList(this List<GetPeerInfoResponse> data)
        {
            List<Peer> peers = new List<Peer>();


            foreach (GetPeerInfoResponse peer in data)
            {
                peers.Add(new Peer
                {
                    Address = peer.addr,
                    ProtocolVersion = peer.version,
                    Version = peer.subver,
                    TipHeight = peer.startingheight,
                    WillRelayTXs = peer.relaytxes,
                    BanScore = peer.banscore,
                    InboundConnection = peer.inbound,
                    Services = peer.services
                });
            } //end of foreach

            return peers;
        } //end of public static List<Peer> ToPeersList(this Outboundpeer[] data)
    } //end of public static class RestModelExtensions
}