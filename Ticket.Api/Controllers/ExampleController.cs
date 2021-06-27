namespace Ticket.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Ticket.Core;

    [ApiController]
    [Route("[controller]")]
    public class ExampleController : ControllerBase
    {
        private readonly IUnitOfWork context;

        public ExampleController(IUnitOfWork _unitOfWork)
        {
            this.context = _unitOfWork;
        }

        // [HttpGet("{id}")]
        // public async Task<Example> Get(int id)
        // {
        //     var example = await context.ExampleRepository.GetAsync(id.ToString());
        //
        //     return example;
        // }
    }
}
