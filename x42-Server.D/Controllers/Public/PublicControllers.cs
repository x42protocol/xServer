using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using X42.Controllers.Requests;
using X42.Feature.Database.Tables;
using X42.Server;
using X42.Server.Results;

namespace X42.Controllers.Public
{
    /// <inheritdoc />
    /// <summary>
    ///     Controller providing Public Methods for the server.
    /// </summary>
    [Route("")]
    public class PublicController : Controller
    {
        private readonly X42Server x42Server;

        public PublicController(X42Server x42Server)
        {
            this.x42Server = x42Server;
        }

        /// <summary>
        ///     Returns a web page to act as a dashboard
        /// </summary>
        /// <returns>text/html content</returns>
        [HttpGet]
        [Route("ping")]
        public IActionResult Ping()
        {
            string content = x42Server.Version.ToString();
            return Content(content);
        }

        /// <summary>
        ///     Registers a servernode to the network.
        /// </summary>
        /// <returns>A <see cref="RegisterResult" /> with registration result.</returns>
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
        {
            ServerNodeData serverNode = new ServerNodeData()
            {
                Name = registerRequest.Name,
                Ip = registerRequest.Ip,
                Port = registerRequest.Port,
                Signature = registerRequest.Signature,
                TxId = registerRequest.TxId,
                TxOut = registerRequest.TxOut
            };

            RegisterResult registerResult = await x42Server.Register(serverNode);

            return Json(registerResult);
        }
    }
}