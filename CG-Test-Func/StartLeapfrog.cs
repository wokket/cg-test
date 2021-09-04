using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CG_Test_Func
{
    public static class StartLeapfrog
    {
        [FunctionName("StartLeapfrog")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Start Leapfrog function processed a request.");

            try
            {
                var azure = GetAzureConnection(log);
                var cg = await GetContainerGroup(azure, "wva", "cg-test");

                if (cg == null)
                {
                    return new NotFoundObjectResult("Unable to find Container Group");
                }

                await azure.ContainerGroups.StartAsync(cg.ResourceGroupName, cg.Name);


                log.LogInformation($"State: {cg.State}");

                return new OkObjectResult("");
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return new ObjectResult(ex.ToString()) { StatusCode = 500 };

            }
        }

        private static IAzure GetAzureConnection(ILogger log)
        {
            var azure_name = Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(azure_name))
            {
                // running locally
                log.LogInformation("Running on local authentication...");
                return Azure.Authenticate(@"D:\dev\my.azureauth").WithDefaultSubscription();
            }
            else
            {
                // Managed Service Identity in Azure
                log.LogInformation("Using Managed Service Identity");
                var factory = new AzureCredentialsFactory();
                var msi = new MSILoginInformation(MSIResourceType.AppService);
                var msiCred = factory.FromMSI(msi, AzureEnvironment.AzureGlobalCloud);
                return Azure.Authenticate(msiCred).WithSubscription("4571eef2-e755-4d80-b36d-134d33376e74"); // VS Premium

                // TODO: MSI Currently has 'contributer' RBAC - Get it running with least privelege.
            }

        }

        /// <summary>
        /// Prints the container groups in the specified resource group.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the container group(s).</param>
        /// From https://docs.microsoft.com/en-us/dotnet/api/overview/azure/containerinstance
        private static async Task<IContainerGroup?> GetContainerGroup(IAzure azure, string resourceGroupName, string containerGroupName)
        {

            var cg = await azure.ContainerGroups.GetByResourceGroupAsync(resourceGroupName, containerGroupName);
            return cg;
        }
    }


}
