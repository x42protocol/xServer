using Microsoft.AspNetCore.Mvc;
using X42.Configuration;
using X42.ServerNode;
using X42.Server;

namespace X42.Controllers
{
    public abstract class FeatureController : Controller
    {
        private FeatureController(
            IX42Server x42Server = null,
            ServerSettings nodeSettings = null,
            ServerNodeBase network = null)
        {
            X42Server = x42Server;
            Settings = nodeSettings;
            Network = network;
        }

        protected IX42Server X42Server { get; set; }

        protected ServerSettings Settings { get; set; }

        protected ServerNodeBase Network { get; set; }
    }
}