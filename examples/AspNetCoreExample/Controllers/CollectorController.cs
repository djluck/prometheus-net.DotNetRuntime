using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectorController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        [Route("enable")]
        public async Task<JsonResult> Enable()
        {
            
            if (Startup.Collector != null)
                return new JsonResult(new { Status = "Failed - already enabled"}) { StatusCode = (int)HttpStatusCode.InternalServerError};

            Startup.Collector = Startup.CreateCollector();
            
            return new JsonResult(new { Status = "Ok- started and assigned collector"});
        }
        
        [HttpGet]
        [Route("disable")]
        public async Task<JsonResult> Disable()
        {
            if (Startup.Collector == null)
                return new JsonResult(new { Status = "Failed - already disable"}) { StatusCode = (int)HttpStatusCode.InternalServerError};

            Startup.Collector.Dispose();
            Startup.Collector = null;
            
            return new JsonResult(new { Status = "Ok- stopped the collector"});
        }
    }
}