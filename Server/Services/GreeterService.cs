using Grpc.Core;
using Server;

namespace Server.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            var response = new HelloReply();

            response.Message = "Hello " + request.Name;

            return Task.FromResult(response);
            
        }
    }
}