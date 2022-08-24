using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using x42.Feature.XDocuments;

namespace x42.Controllers.Public
{
    [Route("api/[controller]")]
    public class XDocumentController : ControllerBase
    {
        private readonly XDocumentClient _mongoService;
        public XDocumentController(XDocumentClient mongoService)
        {
            _mongoService = mongoService;
        }

        [HttpGet]
        [Route("ping")]
        public IActionResult Ping()
        {
            return Ok(true);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentById(Guid id)
        {
            return Content((await _mongoService.GetDocumentById(id)).ToString(), "application/json");
        }
    }
}
