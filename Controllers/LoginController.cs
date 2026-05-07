using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OvejaNegra.Context;
using OvejaNegra.Models;

namespace OvejaNegra.Controllers
{
    public class LoginController : Controller
    {
        private MiContext _context;

        private static Dictionary<string, int> _intentos = new();
        private static Dictionary<string, DateTime> _bloqueos = new();

        public LoginController(MiContext context)
        {
            _context = context;
        }

        private string ObtenerIdMaquina()
        {
            const string cookieName = "machine_id";

            if (Request.Cookies.ContainsKey(cookieName))
                return Request.Cookies[cookieName];

            string nuevoId = Guid.NewGuid().ToString();
            Response.Cookies.Append(cookieName, nuevoId, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(1),
                HttpOnly = true,
                IsEssential = true
            });

            return nuevoId;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            string maquina = ObtenerIdMaquina();
            if (_bloqueos.ContainsKey(maquina) && _bloqueos[maquina] > DateTime.Now)
            {
                var minutosRestantes = (int)(_bloqueos[maquina] - DateTime.Now).TotalMinutes + 1;
                TempData["LoginError"] = $"Acceso bloqueado. Intente en {minutosRestantes} minuto(s)";
                return RedirectToAction("Index");
            }

            var usuario = await _context.Usuarios
                .Where(x => x.username == username)
                .FirstOrDefaultAsync();

            bool contrasenaCorrecta = usuario != null &&
                BCrypt.Net.BCrypt.Verify(password, usuario.password);

            if (usuario == null || !contrasenaCorrecta)
            {
                if (!_intentos.ContainsKey(maquina))
                    _intentos[maquina] = 0;

                _intentos[maquina]++;

                if (_intentos[maquina] >= 3)
                {
                    _bloqueos[maquina] = DateTime.Now.AddMinutes(5);
                    _intentos[maquina] = 0;
                    TempData["LoginError"] = "Demasiados intentos fallidos. Acceso bloqueado por 5 minutos";
                }
                else
                {
                    int restantes = 3 - _intentos[maquina];
                    TempData["LoginError"] = $"Usuario o contrasena incorrectos. Intentos restantes: {restantes}";
                }

                return RedirectToAction("Index");
            }
            _intentos.Remove(maquina);
            _bloqueos.Remove(maquina);

            await SetUserCookie(usuario);
            return RedirectToAction("Index", "Home");
        }

        private async Task SetUserCookie(Usuario usuario)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, usuario!.username!),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
            };
            var claimsIdentify = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentify));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }
        public async Task<IActionResult> HashearContrasenas()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            foreach (var u in usuarios)
            {
                if (!u.password.StartsWith("$2"))
                {
                    u.password = BCrypt.Net.BCrypt.HashPassword(u.password);
                    _context.Update(u);
                }
            }
            await _context.SaveChangesAsync();
            return Content("Contrasenas hasheadas correctamente");
        }
    }
}