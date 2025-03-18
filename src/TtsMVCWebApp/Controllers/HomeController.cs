using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TtsMVCWebApp.Models;

namespace TtsMVCWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AvatarDemo()
        {
            return View();
        }

        public IActionResult AvatarChatDemo()
        {
            return View();
        }

        public IActionResult AvatarChatDemoSK()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> ViewTickets()
        {
            HttpClient client=new HttpClient();

            //client.BaseAddress = new Uri("https://localhost:7058/");
            client.BaseAddress = new Uri(Settings.GetSettings().DemoKBApiEndpoint);

            var tickets = await client.GetFromJsonAsync<List<Ticket>>("Chat/GetTickets");

            return View(tickets);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class Ticket
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
