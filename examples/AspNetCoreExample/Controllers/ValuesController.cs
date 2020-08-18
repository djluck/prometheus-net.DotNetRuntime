using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private Random r = new Random();
        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get()
        {
            // assign some SOH memory
            var x = new byte[1024];
            
            // assign some LOH memory
            x = new byte[1024 * 100];
            
            // await a task (will result in a Task being scheduled on the thread pool) 
            await Task.Yield();

            var val = this.r.Next();
            CompileMe(() => val);

            try
            {
                var divide = 0;
                var result = 1 / divide;
            }
            catch { }

            return new string[] {"value1" + this.r.Next(), "value2"+ this.r.Next()};
        }

        private void CompileMe(Expression<Func<int>> func)
        {
            func.Compile()();
        }
    }
}