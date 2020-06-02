using Microsoft.AspNetCore.Mvc;
using x42.Configuration;
using x42.ServerNode;
using x42.Server;

namespace x42.Controllers
{
    public abstract class FeatureController : Controller
    {
        private FeatureController(
            IxServer xServer = null,
            ServerSettings nodeSettings = null,
            ServerNodeBase network = null)
        {
            this.xServer = xServer;
            Settings = nodeSettings;
            Network = network;
        }

        protected IxServer xServer { get; set; }

        protected ServerSettings Settings { get; set; }

        protected ServerNodeBase Network { get; set; }
    }
}