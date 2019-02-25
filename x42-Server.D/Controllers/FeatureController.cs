using System;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using X42.Configuration;
using X42.MasterNode;
using X42.Server;

namespace X42.Controllers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionDescription : Attribute
    {
        public ActionDescription(string description)
        {
            Description = description;
        }

        public string Description { get; }
    }

    public abstract class FeatureController : Controller
    {
        private FeatureController(
            IX42Server x42Server = null,
            ServerSettings nodeSettings = null,
            MasterNodeBase network = null)
        {
            X42Server = x42Server;
            Settings = nodeSettings;
            Network = network;
        }

        protected IX42Server X42Server { get; set; }

        protected ServerSettings Settings { get; set; }

        protected MasterNodeBase Network { get; set; }
    }
}