using System;
using System.IO;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace aksCniCalc
{
    public static class aksCniCalc
    {
        [FunctionName("aksCniCalc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            DateTime utcDateStart = DateTime.UtcNow;
            Console.WriteLine($"{utcDateStart.ToString()} [INF]: C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            int nodes = Convert.ToInt32((data?.nodes ?? req.Query["nodes"]) ?? 0);
            int pods = Convert.ToInt32((data?.pods ?? req.Query["pods"]) ?? 30);
            int scale = Convert.ToInt32((data?.scale ?? req.Query["scale"]) ?? 0);
            int ilbs = Convert.ToInt32((data?.ilbs ?? req.Query["ilbs"]) ?? 0);

            int ipaddresses;
            dynamic output = new ExpandoObject();

            if (nodes == 0)
            {
                DateTime utcDateReturnFailure = DateTime.UtcNow;
                Console.Error.WriteLine($"{utcDateReturnFailure.ToString()} [ERR]: Processed input was null or did not match the required input type.");
                log.LogError("Processed input was null or did not match the required input type.");
                return new BadRequestObjectResult("Please pass 'nodes' on the query string or in the request body at least with a number greater than 0.");
            }
            if (nodes + scale > 1000)
            {
                DateTime utcDateReturnFailure = DateTime.UtcNow;
                Console.Error.WriteLine($"{utcDateReturnFailure.ToString()} [ERR]: Node number is higher than the supported limit of 1000 nodes per cluster.");
                log.LogError("Node number is higher than the supported limit of 1000 nodes per cluster.");
                return new BadRequestObjectResult("Node number is higher than the supported limit of 1000 nodes per cluster.");
            }
            if (pods > 250)
            {
                DateTime utcDateReturnFailure = DateTime.UtcNow;
                Console.Error.WriteLine($"{utcDateReturnFailure.ToString()} [ERR]: Pod number is higher than the supported limit of 250 pods per node.");
                log.LogError("Pod number is higher than the supported limit of 250 per node.");
                return new BadRequestObjectResult("Pod number is higher than the supported limit of 250 per node.");
            }
            if (pods < 30)
            {
                pods = 30;
            }
            ipaddresses = ((nodes + 1 + scale) + ((nodes + 1 + scale) * pods) + ilbs);
            output.nodes = nodes;
            output.pods = pods;
            output.scale = scale;
            output.ilbs = ilbs;
            output.ipaddresses = ipaddresses;
            string result = JsonConvert.SerializeObject(output, Formatting.Indented);
            DateTime utcDateReturnSuccess = DateTime.UtcNow;
            Console.WriteLine($"{utcDateReturnSuccess.ToString()} [INF]: Processed AKS cluster CNI IP address calculation for '{nodes}' node(s), '{pods}' pod(s) with a scaling option of '{scale}' node(s) and '{ilbs}' Azure Internal Load Balancer(s) successfully. Result: '{ipaddresses}' IP addresses required.");
            log.LogInformation($"Processed AKS cluster CNI IP address calculation for '{nodes}' node(s), '{pods}' pod(s) with a scaling option of '{scale}' node(s) and '{ilbs}' Azure Internal Load Balancer(s) successfully. Result: '{ipaddresses}' IP addresses required.");
            return (ActionResult)new OkObjectResult(result);
        }
    }
}