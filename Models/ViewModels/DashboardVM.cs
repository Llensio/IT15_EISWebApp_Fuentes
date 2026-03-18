using Microsoft.AspNetCore.Mvc;

namespace Executive_Fuentes.Models.ViewModels
{
    public class DashboardVM : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
