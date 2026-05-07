using ClosedXML.Excel;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OvejaNegra.Context;

namespace OvejaNegra.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        private readonly MiContext _context;
        private readonly IConverter _converter;

        public ReportesController(MiContext context, IConverter converter)
        {
            _context = context;
            _converter = converter;
        }
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            desde ??= DateTime.Now.AddMonths(-1);
            hasta ??= DateTime.Now;
            var hastaFin = hasta.Value.Date.AddDays(1).AddTicks(-1);
            var desdeInicio = desde.Value.Date;

            ViewBag.Desde = desdeInicio.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

            ViewBag.TotalComandas = await _context.Comandas
                .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin)
                .CountAsync();

            ViewBag.TotalRecaudado = await _context.DetalleComandas
                .Include(d => d.Comanda)
                .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                .SumAsync(d => d.Cantidad * d.Precio_unitario);

            ViewBag.ProductosMasVendidos = await _context.DetalleComandas
                .Include(d => d.Comanda)
                .Include(d => d.Producto)
                .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
                .Select(g => new
                {
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(d => d.Cantidad),
                    Total = g.Sum(d => d.Cantidad * d.Precio_unitario)
                })
                .OrderByDescending(p => p.Cantidad)
                .Take(10)
                .ToListAsync();

            ViewBag.VentasPorCategoria = await _context.DetalleComandas
                .Include(d => d.Comanda)
                .Include(d => d.Producto).ThenInclude(p => p.Categoria)
                .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                .GroupBy(d => new { d.Producto.CategoriaId, d.Producto.Categoria.Nombre })
                .Select(g => new
                {
                    Categoria = g.Key.Nombre,
                    Total = g.Sum(d => d.Cantidad * d.Precio_unitario),
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(c => c.Total)
                .ToListAsync();

            var todasComandas = await _context.Comandas
                .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin)
                .ToListAsync();

            ViewBag.HorasPico = todasComandas
                .GroupBy(c => c.Fecha.Hour)
                .Select(g => new { Hora = g.Key, TotalComandas = g.Count() })
                .OrderByDescending(h => h.TotalComandas)
                .ToList();

            ViewBag.DiasSemana = todasComandas
                .GroupBy(c => c.Fecha.DayOfWeek)
                .Select(g => new { Dia = g.Key, TotalComandas = g.Count() })
                .OrderByDescending(d => d.TotalComandas)
                .ToList();
            ViewBag.Comandas = await _context.Comandas
                .Include(c => c.Usuario)
                .Include(c => c.DetallesComanda).ThenInclude(d => d.Producto)
                .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            return View();
        }
        public async Task<IActionResult> ExportarExcel(DateTime desde, DateTime hasta, string tipoReporte = "productos")
        {
            var desdeInicio = desde.Date;
            var hastaFin = hasta.Date.AddDays(1).AddTicks(-1);

            using var workbook = new XLWorkbook();

            if (tipoReporte == "productos")
            {
                var productos = await _context.DetalleComandas
                    .Include(d => d.Comanda).Include(d => d.Producto)
                    .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                    .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
                    .Select(g => new { Nombre = g.Key.Nombre, Cantidad = g.Sum(d => d.Cantidad), Total = g.Sum(d => d.Cantidad * d.Precio_unitario) })
                    .OrderByDescending(p => p.Cantidad).ToListAsync();

                var hoja = workbook.Worksheets.Add("Productos mas vendidos");
                hoja.Cell(1, 1).Value = "Producto";
                hoja.Cell(1, 2).Value = "Cantidad";
                hoja.Cell(1, 3).Value = "Total (Bs)";
                hoja.Row(1).Style.Font.Bold = true;
                for (int i = 0; i < productos.Count; i++)
                {
                    hoja.Cell(i + 2, 1).Value = productos[i].Nombre;
                    hoja.Cell(i + 2, 2).Value = productos[i].Cantidad;
                    hoja.Cell(i + 2, 3).Value = productos[i].Total;
                }
                hoja.Columns().AdjustToContents();
            }
            else if (tipoReporte == "categorias")
            {
                var categorias = await _context.DetalleComandas
                    .Include(d => d.Comanda).Include(d => d.Producto).ThenInclude(p => p.Categoria)
                    .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                    .GroupBy(d => new { d.Producto.CategoriaId, d.Producto.Categoria.Nombre })
                    .Select(g => new { Categoria = g.Key.Nombre, Cantidad = g.Sum(d => d.Cantidad), Total = g.Sum(d => d.Cantidad * d.Precio_unitario) })
                    .OrderByDescending(c => c.Total).ToListAsync();

                var hoja = workbook.Worksheets.Add("Ventas por Categoria");
                hoja.Cell(1, 1).Value = "Categoria";
                hoja.Cell(1, 2).Value = "Cantidad";
                hoja.Cell(1, 3).Value = "Total (Bs)";
                hoja.Row(1).Style.Font.Bold = true;
                for (int i = 0; i < categorias.Count; i++)
                {
                    hoja.Cell(i + 2, 1).Value = categorias[i].Categoria;
                    hoja.Cell(i + 2, 2).Value = categorias[i].Cantidad;
                    hoja.Cell(i + 2, 3).Value = categorias[i].Total;
                }
                hoja.Columns().AdjustToContents();
            }
            else if (tipoReporte == "horas")
            {
                var comandas = await _context.Comandas
                    .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin).ToListAsync();

                var hoja = workbook.Worksheets.Add("Horas Pico");
                hoja.Cell(1, 1).Value = "Hora";
                hoja.Cell(1, 2).Value = "Total Comandas";
                hoja.Row(1).Style.Font.Bold = true;
                var horasPico = comandas.GroupBy(c => c.Fecha.Hour)
                    .Select(g => new { Hora = g.Key, Total = g.Count() })
                    .OrderByDescending(h => h.Total).ToList();
                for (int i = 0; i < horasPico.Count; i++)
                {
                    hoja.Cell(i + 2, 1).Value = $"{horasPico[i].Hora}:00 - {horasPico[i].Hora + 1}:00";
                    hoja.Cell(i + 2, 2).Value = horasPico[i].Total;
                }
                hoja.Columns().AdjustToContents();
            }
            else if (tipoReporte == "dias")
            {
                var comandas = await _context.Comandas
                    .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin).ToListAsync();

                var hoja = workbook.Worksheets.Add("Dias con mas movimiento");
                hoja.Cell(1, 1).Value = "Dia";
                hoja.Cell(1, 2).Value = "Total Comandas";
                hoja.Row(1).Style.Font.Bold = true;
                var dias = new[] { "Domingo", "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado" };
                var diasSemana = comandas.GroupBy(c => c.Fecha.DayOfWeek)
                    .Select(g => new { Dia = dias[(int)g.Key], Total = g.Count() })
                    .OrderByDescending(d => d.Total).ToList();
                for (int i = 0; i < diasSemana.Count; i++)
                {
                    hoja.Cell(i + 2, 1).Value = diasSemana[i].Dia;
                    hoja.Cell(i + 2, 2).Value = diasSemana[i].Total;
                }
                hoja.Columns().AdjustToContents();
            }
            else if (tipoReporte == "comandas")
            {
                var comandas = await _context.Comandas
                    .Include(c => c.Usuario)
                    .Include(c => c.DetallesComanda).ThenInclude(d => d.Producto)
                    .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin)
                    .OrderByDescending(c => c.Fecha).ToListAsync();

                var hoja = workbook.Worksheets.Add("Comandas del periodo");
                hoja.Cell(1, 1).Value = "Fecha";
                hoja.Cell(1, 2).Value = "Mesa";
                hoja.Cell(1, 3).Value = "Mesero";
                hoja.Cell(1, 4).Value = "Estado";
                hoja.Cell(1, 5).Value = "Productos";
                hoja.Cell(1, 6).Value = "Total (Bs)";
                hoja.Row(1).Style.Font.Bold = true;
                for (int i = 0; i < comandas.Count; i++)
                {
                    var c = comandas[i];
                    var prods = string.Join(", ", c.DetallesComanda.Select(d => $"{d.Producto.Nombre} x{d.Cantidad}"));
                    var total = c.DetallesComanda.Sum(d => d.Cantidad * d.Precio_unitario);
                    hoja.Cell(i + 2, 1).Value = c.Fecha.ToString("dd/MM/yyyy HH:mm");
                    hoja.Cell(i + 2, 2).Value = $"Mesa {c.Nro_Mesa}";
                    hoja.Cell(i + 2, 3).Value = $"{c.Usuario.Nombre} {c.Usuario.Apellido}";
                    hoja.Cell(i + 2, 4).Value = c.Estado.ToString();
                    hoja.Cell(i + 2, 5).Value = prods;
                    hoja.Cell(i + 2, 6).Value = total;
                }
                hoja.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reporte_{tipoReporte}_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportarPDF(DateTime desde, DateTime hasta, string tipoReporte = "productos")
        {
            var desdeInicio = desde.Date;
            var hastaFin = hasta.Date.AddDays(1).AddTicks(-1);

            var totalRecaudado = await _context.DetalleComandas
                .Include(d => d.Comanda)
                .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                .SumAsync(d => d.Cantidad * d.Precio_unitario);

            string cuerpoHtml = "";
            string tituloReporte = "";

            if (tipoReporte == "productos")
            {
                tituloReporte = "Productos mas vendidos";
                var productos = await _context.DetalleComandas
                    .Include(d => d.Comanda).Include(d => d.Producto)
                    .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                    .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
                    .Select(g => new { Nombre = g.Key.Nombre, Cantidad = g.Sum(d => d.Cantidad), Total = g.Sum(d => d.Cantidad * d.Precio_unitario) })
                    .OrderByDescending(p => p.Cantidad).ToListAsync();

                cuerpoHtml = $@"
            <table border='1' cellpadding='6' cellspacing='0' width='100%'>
                <thead style='background:#333; color:white'>
                    <tr><th>Producto</th><th>Cantidad</th><th>Total</th></tr>
                </thead>
                <tbody>
                    {string.Join("", productos.Select(p => $"<tr><td>{p.Nombre}</td><td>{p.Cantidad}</td><td>Bs {p.Total:0.00}</td></tr>"))}
                </tbody>
            </table>";
            }
            else if (tipoReporte == "categorias")
            {
                tituloReporte = "Ventas por Categoria";
                var categorias = await _context.DetalleComandas
                    .Include(d => d.Comanda).Include(d => d.Producto).ThenInclude(p => p.Categoria)
                    .Where(d => d.Comanda.Fecha >= desdeInicio && d.Comanda.Fecha <= hastaFin)
                    .GroupBy(d => new { d.Producto.CategoriaId, d.Producto.Categoria.Nombre })
                    .Select(g => new { Categoria = g.Key.Nombre, Cantidad = g.Sum(d => d.Cantidad), Total = g.Sum(d => d.Cantidad * d.Precio_unitario) })
                    .OrderByDescending(c => c.Total).ToListAsync();

                cuerpoHtml = $@"
            <table border='1' cellpadding='6' cellspacing='0' width='100%'>
                <thead style='background:#333; color:white'>
                    <tr><th>Categoria</th><th>Cantidad</th><th>Total</th></tr>
                </thead>
                <tbody>
                    {string.Join("", categorias.Select(c => $"<tr><td>{c.Categoria}</td><td>{c.Cantidad}</td><td>Bs {c.Total:0.00}</td></tr>"))}
                </tbody>
            </table>";
            }
            else if (tipoReporte == "horas")
            {
                tituloReporte = "Horas con mas pedidos";
                var comandas = await _context.Comandas
                    .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin).ToListAsync();
                var horasPico = comandas.GroupBy(c => c.Fecha.Hour)
                    .Select(g => new { Hora = g.Key, Total = g.Count() })
                    .OrderByDescending(h => h.Total).ToList();

                cuerpoHtml = $@"
            <table border='1' cellpadding='6' cellspacing='0' width='100%'>
                <thead style='background:#333; color:white'>
                    <tr><th>Hora</th><th>Total Comandas</th></tr>
                </thead>
                <tbody>
                    {string.Join("", horasPico.Select(h => $"<tr><td>{h.Hora}:00 - {h.Hora + 1}:00</td><td>{h.Total}</td></tr>"))}
                </tbody>
            </table>";
            }
            else if (tipoReporte == "dias")
            {
                tituloReporte = "Dias con mas movimiento";
                var comandas = await _context.Comandas
                    .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin).ToListAsync();
                var dias = new[] { "Domingo", "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado" };
                var diasSemana = comandas.GroupBy(c => c.Fecha.DayOfWeek)
                    .Select(g => new { Dia = dias[(int)g.Key], Total = g.Count() })
                    .OrderByDescending(d => d.Total).ToList();

                cuerpoHtml = $@"
            <table border='1' cellpadding='6' cellspacing='0' width='100%'>
                <thead style='background:#333; color:white'>
                    <tr><th>Dia</th><th>Total Comandas</th></tr>
                </thead>
                <tbody>
                    {string.Join("", diasSemana.Select(d => $"<tr><td>{d.Dia}</td><td>{d.Total}</td></tr>"))}
                </tbody>
            </table>";
            }
            else if (tipoReporte == "comandas")
            {
                tituloReporte = "Comandas del periodo";
                var comandas = await _context.Comandas
                    .Include(c => c.Usuario)
                    .Include(c => c.DetallesComanda).ThenInclude(d => d.Producto)
                    .Where(c => c.Fecha >= desdeInicio && c.Fecha <= hastaFin)
                    .OrderByDescending(c => c.Fecha).ToListAsync();

                cuerpoHtml = $@"
            <table border='1' cellpadding='6' cellspacing='0' width='100%'>
                <thead style='background:#333; color:white'>
                    <tr><th>Fecha</th><th>Mesa</th><th>Mesero</th><th>Estado</th><th>Productos</th><th>Total</th></tr>
                </thead>
                <tbody>
                    {string.Join("", comandas.Select(c => $@"
                    <tr>
                        <td>{c.Fecha:dd/MM/yyyy HH:mm}</td>
                        <td>Mesa {c.Nro_Mesa}</td>
                        <td>{c.Usuario.Nombre} {c.Usuario.Apellido}</td>
                        <td>{c.Estado}</td>
                        <td>{string.Join(", ", c.DetallesComanda.Select(d => $"{d.Producto.Nombre} x{d.Cantidad}"))}</td>
                        <td>Bs {c.DetallesComanda.Sum(d => d.Cantidad * d.Precio_unitario):0.00}</td>
                    </tr>"))}
                </tbody>
            </table>";
            }

            var html = $@"
        <html><body style='font-family:Arial; padding:20px'>
        <h1 style='color:#333'>Reporte - {tituloReporte}</h1>
        <p>Periodo: <strong>{desdeInicio:dd/MM/yyyy}</strong> al <strong>{hasta:dd/MM/yyyy}</strong></p>
        <p>Total recaudado en el periodo: <strong>Bs {totalRecaudado:0.00}</strong></p>
        <hr/>
        <h2>{tituloReporte}</h2>
        {cuerpoHtml}
        </body></html>";

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = { PaperSize = PaperKind.A4, Orientation = Orientation.Portrait },
                Objects = { new ObjectSettings { HtmlContent = html } }
            };

            var pdf = _converter.Convert(doc);
            return File(pdf, "application/pdf", $"Reporte_{tipoReporte}_{desde:yyyyMMdd}.pdf");
        }
    }
}
