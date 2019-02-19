using System;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using X42.Server;
using X42.Configuration;
using X42.MasterNode;

namespace X42.Controllers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionDescription : Attribute
    {
        public string Description { get; private set; }

        public ActionDescription(string description)
        {
            this.Description = description;
        }
    }

    public abstract class FeatureController : Controller
    {
        protected IX42Server X42Server { get; set; }

        protected ServerSettings Settings { get; set; }

        protected MasterNodeBase Network { get; set; }

        protected ChainBase Chain { get; set; }

        public FeatureController(
            IX42Server x42Server = null,
            ServerSettings nodeSettings = null,
            MasterNodeBase network = null)
        {
            this.X42Server = x42Server;
            this.Settings = nodeSettings;
            this.Network = network;
        }
    }
}