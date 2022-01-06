using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleFunctionAppDemo.Models;
using System.Collections.Generic;
using System.Linq;

namespace SimpleFunctionAppDemo
{
    public static class GetPeople
    {
        [FunctionName("GetPeople")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting People");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            var people = GetThePeople(name);

            return new OkObjectResult(people);
        }

        private static string GetThePeople(string name)
        {
            var allPeople = GetAllPeople();
            var returnedPeople = allPeople;
            if (!string.IsNullOrWhiteSpace(name))
            {
                returnedPeople = allPeople.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return JsonConvert.SerializeObject(returnedPeople);
        }

        private static List<Person> GetAllPeople()
        {
            var people = new List<Person>() {
                new Person { FirstName="Clark", LastName="Kent", Age=32},
                new Person { FirstName="Bruce", LastName="Wayne", Age=37},
                new Person { FirstName="Carol", LastName="Danvers", Age=32},
                new Person { FirstName="Janet", LastName="Van Dyne", Age=35},
                new Person { FirstName="Jane", LastName="Smith", Age=43},
                new Person { FirstName="John", LastName="Smith", Age=47},
                new Person { FirstName="Sally", LastName="Johnson", Age=42},
                new Person { FirstName="Larry", LastName="Johnson", Age=51}
            };

            return people;
        }
    }
}
