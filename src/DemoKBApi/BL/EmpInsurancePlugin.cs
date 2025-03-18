using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DemoKBApi.BL
{
    public class EmpInsurancePlugin
    {
   //     private readonly List<InsurancePlan> plans = new()
   //{
   //   new InsurancePlan{Id=1, Name="Northwind Standand Plan", Description="Plan which includes basic health coverage"},
   //   new InsurancePlan{Id=2, Name="Northwind Premium Plan", Description="Plan which includes full health coverage"}
   //};
        

        //[KernelFunction("get_insurace_plans")]
        //[Description("Returns all insurance plans")]
        //[return: Description("Returns list of insurance plans which includes insurance title and price")]
        //public async Task<List<InsurancePlan>> GetAllInsurancePlans()
        //{
        //    return plans;
        //}

        [KernelFunction("purchase_insurace")]
        [Description("Creates purchase infromation for insurance requested")]
        [return: Description("Creates purchase request for insurance requested by user and returns the refernce number for future reference")]
        public async Task<Ticket> PurchaseInsurance(string insuranceName)
        {

            var ticket= new Ticket
            {
                Id = string.Format("REF-{0:D6}", new Random(2000).Next(1000, 5000)),
                Title = "New Purchase for " + insuranceName,
                Description = "User wants to purchase " + insuranceName
            };

            TicketRepo.Instance.Tickets.Add(ticket);

            return ticket;
        }

        [KernelFunction("get_current_insurace")]
        [Description("Gets current insurance plan of the user")]
        [return: Description("Returns name of current insurance plan which user is registered for")]
        public string GetPurchaseInsurance()
        {
            return "Northwind Standand Plan";
        }

    }
}