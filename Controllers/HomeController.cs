using Microsoft.AspNetCore.Mvc;
using OvejaNegra.Context;
using OvejaNegra.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
namespace OvejaNegra.Controllers
{
    public class HomeController : Controller
    {
        private readonly MiContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, MiContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> Menu()
        {
            var categorias = await _context.Categorias
                .Include(c => c.Productos.Where(p => p.Disponible))
                .Where(c => c.Productos.Any(p => p.Disponible))
                .ToListAsync();

            return View(categorias);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
