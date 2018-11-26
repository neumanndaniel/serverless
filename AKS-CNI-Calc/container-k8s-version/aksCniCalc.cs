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

            string nodes = req.Query["nodes"];
            string pods = req.Query["pods"];
            string scale = req.Query["scale"];
            string ilbs = req.Query["ilbs"];
            int node;
            int pod;
            int nodescale;
            int ilb;
            int ipaddresses;
            dynamic output = new ExpandoObject();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            nodes = nodes ?? data?.nodes;
            pods = pods ?? data?.pods;
            scale = scale ?? data?.scale;
            ilbs = ilbs ?? data?.ilbs;
            node = Int32.Parse(nodes ?? "0");
            pod = Int32.Parse(pods ?? "30");
            nodescale = Int32.Parse(scale ?? "0");
            ilb = Int32.Parse(ilbs ?? "0");

            if (nodes != null && nodes != "0")
            {
                if (node + nodescale <= 100)
                {
                    if (pod <= 110)
                    {
                        ipaddresses = ((node + 1 + nodescale) + ((node + 1 + nodescale) * pod) + ilb);
                        output.nodes = node;
                        output.pods = pod;
                        output.scale = nodescale;
                        output.ilbs = ilb;
                        output.ipaddresses = ipaddresses;
                        string result = JsonConvert.SerializeObject(output, Formatting.Indented);
                        DateTime utcDateReturnSuccess = DateTime.UtcNow;
                        Console.WriteLine($"{utcDateReturnSuccess.ToString()} [INF]: Processed AKS cluster CNI IP address calculation for '{node}' node(s), '{pod}' pod(s) with a scaling option of '{nodescale}' node(s) and '{ilb}' Azure Internal Load Balancer(s) successfully. Result: '{ipaddresses}' IP addresses required.");
                        log.LogInformation($"Processed AKS cluster CNI IP address calculation for '{node}' node(s), '{pod}' pod(s) with a scaling option of '{nodescale}' node(s) and '{ilb}' Azure Internal Load Balancer(s) successfully. Result: '{ipaddresses}' IP addresses required.");
                        return (ActionResult)new OkObjectResult(result);
                    }
                    else
                    {
                        DateTime utcDateReturnFailure = DateTime.UtcNow;
                        Console.WriteLine($"{utcDateReturnFailure.ToString()} [ERR]: Pod number is higher than the supported limit of 110 pods per node.");
                        log.LogError("Pod number is higher than the supported limit of 110 per node.");
                        return new BadRequestObjectResult("Pod number is higher than the supported limit of 110 per node.");
                    }
                }
                else
                {
                    DateTime utcDateReturnFailure = DateTime.UtcNow;
                    Console.WriteLine($"{utcDateReturnFailure.ToString()} [ERR]: Node number is higher than the supported limit of 100 nodes per cluster.");
                    log.LogError("Node number is higher than the supported limit of 100 nodes per cluster.");
                    return new BadRequestObjectResult("Node number is higher than the supported limit of 100 nodes per cluster.");
                }
            }
            else
            {
                DateTime utcDateReturnFailure = DateTime.UtcNow;
                Console.WriteLine($"{utcDateReturnFailure.ToString()} [ERR]: Processed input was null or did not match the required input type.");
                log.LogError("Processed input was null or did not match the required input type.");
                return new BadRequestObjectResult("Please pass 'nodes' on the query string or in the request body at least with a number greater than 0.");
            }
        }
    }
}
