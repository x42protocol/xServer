using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin;

namespace X42.MasterNode
{
    public class Collateral
    {
        /// <summary> The amount needed for masternode collateral </summary>
        public Money Amount { get; set; }
    }
}
