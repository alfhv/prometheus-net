using Microsoft.AspNetCore.Mvc;

namespace tester
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class DummyController : ControllerBase
    {
        [HttpGet]
        [Route("{myparam}/{method}")]
        public string Get(string myparam)
        {
            return $"Hello tester : {myparam}";
        }
    }
}
