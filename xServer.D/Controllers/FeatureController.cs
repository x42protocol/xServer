using Microsoft.AspNetCore.Mvc;
using X42.Configuration;
using X42.ServerNode;
using X42.Server;

namespace X42.Controllers
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