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
    public class SimulateController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get(
            bool simulateAlloc = true,
            bool simulateJit = true,
            bool simulateException = true,
            bool simulateBlocking = false)
        {
            var r = new Random();
            if (simulateAlloc)
            {
                // assign some SOH memory
                var x = new byte[r.Next(1024, 1024 * 64)];

                // assign some LOH memory
                x = new byte[r.Next(1024 * 90, 1024 * 100)];
            }

            // await a task (will result in a Task being scheduled on the thread pool) 
            await Task.Yield();

            if (simulateJit)
            {
                var val = r.Next();
                CompileMe(() => val);
            }

            if (simulateException)
            {
                try
                {
                    var divide = 0;
                    var result = 1 / divide;
                }
                catch
                {
                }
            }

            if (simulateBlocking)
            {
                Thread.Sleep(100);
            }

            return new string[] {"value1" + r.Next(), "value2"+ r.Next()};
        }

        private void CompileMe(Expression<Func<int>> func)
        {
            func.Compile()();
        }
    }
}