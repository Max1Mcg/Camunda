using dotenv.net.Interfaces;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Impl.Builder;
using Zeebe.Client;
using fastJSON;
using Zeebe.Client.Api.Worker;

namespace Cloudstarter.Services
{
    public interface IZeebeService
    {
        public Task<IDeployResponse> Deploy(string modelFilename);
        public Task<String> StartWorkflowInstance(string bpmProcessId);
        public Task<ITopology> Status();
        public void StartWorkers();
    }

    public class MakeGreetingCustomHeadersDTO
    {
        public string greeting { get; set; }
    }

    public class MakeGreetingVariablesDTO
    {
        public string name { get; set; }
    }
    public class ZeebeService : IZeebeService
    {
        private readonly IZeebeClient _client;
        private readonly ILogger<ZeebeService> _logger;

        public ZeebeService(IEnvReader envReader, ILogger<ZeebeService> logger)
        {
            _logger = logger;
            var authServer = envReader.GetStringValue("ZEEBE_AUTHORIZATION_SERVER_URL");
            var clientId = envReader.GetStringValue("ZEEBE_CLIENT_ID");
            var clientSecret = envReader.GetStringValue("ZEEBE_CLIENT_SECRET");
            var zeebeUrl = envReader.GetStringValue("ZEEBE_ADDRESS");
            char[] port =
            {
                '4', '3', ':'
            };
            var audience = zeebeUrl?.TrimEnd(port);

            _client =
                ZeebeClient.Builder()
                    .UseGatewayAddress(zeebeUrl)
                    .UseTransportEncryption()
                    .UseAccessTokenSupplier(
                        CamundaCloudTokenProvider.Builder()
                            .UseAuthServer(authServer)
                            .UseClientId(clientId)
                            .UseClientSecret(clientSecret)
                            .UseAudience(audience)
                            .Build())
                    .Build();
        }
        public async Task<String> StartWorkflowInstance(string bpmProcessId)
        {
            var instance = await _client.NewCreateProcessInstanceCommand()
                    .BpmnProcessId(bpmProcessId)
                    .LatestVersion()
                    .Variables("{\"name\": \"Josh Wulf\"}")
                    .WithResult()
                    .Send();
            var jsonParams = new JSONParameters { ShowReadOnlyProperties = true };
            return JSON.ToJSON(instance, jsonParams);
        }
        public async Task<IDeployResponse> Deploy(string modelFilename)
        {
            var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "Resources", modelFilename);
            var deployment = await _client.NewDeployCommand().AddResourceFile(filename).Send();
            var res = deployment.Processes[0];
            _logger.LogInformation("Deployed BPMN Model: " + res?.BpmnProcessId +
                        " v." + res?.Version);
            return deployment;
        }

        public Task<ITopology> Status()
        {
            return _client.TopologyRequest().Send();
        }
        // ...
        private void _createWorker(String jobType, JobHandler handleJob)
        {
            _client.NewWorker()
                    .JobType(jobType)
                    .Handler(handleJob)
                    .MaxJobsActive(5)
                    .Name(jobType)
                    .PollInterval(TimeSpan.FromSeconds(50))
                    .PollingTimeout(TimeSpan.FromSeconds(50))
                    .Timeout(TimeSpan.FromSeconds(10))
                    .Open();
        }
        public void CreateGetTimeWorker()
        {
            _createWorker("get-time", async (client, job) =>
            {
                _logger.LogInformation("Received job: " + job);
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync("https://script.googleusercontent.com/macros/echo?user_content_key=fI6lz0X6crpf6TYvXnCYM6qBOdGJfteP2W4t2Ax3oZ7aQHf4sOgqt3p92yMBDg8rCbkkelw1Tc4VYNpw7yeAqyD-D2ldxecJm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnJ9GRkcRevgjTvo8Dc32iw_BLJPcPfRdVKhJT5HNzQuXEeN3QFwl2n0M6ZmO-h7C6eIqWsDnSrEd&lib=MwxUjRcLr2qLlnVOLh12wSNkqcO1Ikdrk"))
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();

                        await client.NewCompleteJobCommand(job.Key)
                            .Variables("{\"time\":" + apiResponse + "}")
                            .Send();
                    }
                }
            });
        }
        public void CreateMakeGreetingWorker()
        {

            _createWorker("make-greeting", async (client, job) =>
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                _logger.LogInformation("Make Greeting Received job: " + job);

                var headers = JSON.ToObject<MakeGreetingCustomHeadersDTO>(job.CustomHeaders);
                var variables = JSON.ToObject<MakeGreetingVariablesDTO>(job.Variables);
                string greeting = headers.greeting;
                string name = variables.name;
                Console.WriteLine(greeting);
                Console.WriteLine(name);
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                await client.NewCompleteJobCommand(job.Key)
                    .Variables("{\"say\": \"" + greeting + " " + name + "\"}")
                    .Send();
                _logger.LogInformation("Make Greeting Worker completed job");
            });
        }
        public void StartWorkers()
        {
            CreateGetTimeWorker();
            CreateMakeGreetingWorker();
        }
    }
}