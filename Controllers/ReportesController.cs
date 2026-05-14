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
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, string tipoReporte = "productos")
        {
            desde ??= DateTime.Now.AddMonths(-1);
            hasta ??= DateTime.Now;
            var hastaFin = hasta.Value.Date.AddDays(1).AddTicks(-1);
            var desdeInicio = desde.Value.Date;

            ViewBag.Desde = desdeInicio.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

            ViewBag.TotalComandas = await _context.Ventas
                .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                .CountAsync();

            ViewBag.TotalRecaudado = await _context.Ventas
                .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                .SumAsync(v => v.Total);

            ViewBag.ProductosMasVendidos = await _context.DetalleComandas
                .Include(d => d.Comanda).ThenInclude(c => c.Venta)
                .Include(d => d.Producto)
                .Where(d => d.Comanda.Venta != null &&
                            d.Comanda.Venta.Fecha >= desdeInicio &&
                            d.Comanda.Venta.Fecha <= hastaFin)
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
                .Include(d => d.Comanda).ThenInclude(c => c.Venta)
                .Include(d => d.Producto).ThenInclude(p => p.Categoria)
                .Where(d => d.Comanda.Venta != null &&
                            d.Comanda.Venta.Fecha >= desdeInicio &&
                            d.Comanda.Venta.Fecha <= hastaFin)
                .GroupBy(d => new { d.Producto.CategoriaId, d.Producto.Categoria.Nombre })
                .Select(g => new
                {
                    Categoria = g.Key.Nombre,
                    Total = g.Sum(d => d.Cantidad * d.Precio_unitario),
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(c => c.Total)
                .ToListAsync();

            var todasVentas = await _context.Ventas
                .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                .ToListAsync();

            ViewBag.HorasPico = todasVentas
                .GroupBy(v => v.Fecha.Hour)
                .Select(g => new { Hora = g.Key, TotalComandas = g.Count() })
                .OrderByDescending(h => h.TotalComandas)
                .ToList();

            ViewBag.DiasSemana = todasVentas
                .GroupBy(v => v.Fecha.DayOfWeek)
                .Select(g => new { Dia = g.Key, TotalComandas = g.Count() })
                .OrderByDescending(d => d.TotalComandas)
                .ToList();

            ViewBag.Comandas = await _context.Ventas
                .Include(v => v.Comanda).ThenInclude(c => c.Usuario)
                .Include(v => v.Comanda).ThenInclude(c => c.DetallesComanda).ThenInclude(d => d.Producto)
                .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                .OrderByDescending(v => v.Fecha)
                .Select(v => v.Comanda)
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
                    .Include(d => d.Comanda).ThenInclude(c => c.Venta)
                    .Include(d => d.Producto)
                    .Where(d => d.Comanda.Venta != null &&
                                d.Comanda.Venta.Fecha >= desdeInicio &&
                                d.Comanda.Venta.Fecha <= hastaFin)
                    .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
                    .Select(g => new
                    {
                        Nombre = g.Key.Nombre,
                        Cantidad = g.Sum(d => d.Cantidad),
                        Total = g.Sum(d => d.Cantidad * d.Precio_unitario)
                    })
                    .OrderByDescending(p => p.Cantidad)
                    .ToListAsync();

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
                    .Include(d => d.Comanda).ThenInclude(c => c.Venta)
                    .Include(d => d.Producto).ThenInclude(p => p.Categoria)
                    .Where(d => d.Comanda.Venta != null &&
                                d.Comanda.Venta.Fecha >= desdeInicio &&
                                d.Comanda.Venta.Fecha <= hastaFin)
                    .GroupBy(d => new { d.Producto.CategoriaId, d.Producto.Categoria.Nombre })
                    .Select(g => new
                    {
                        Categoria = g.Key.Nombre,
                        Cantidad = g.Sum(d => d.Cantidad),
                        Total = g.Sum(d => d.Cantidad * d.Precio_unitario)
                    })
                    .OrderByDescending(c => c.Total)
                    .ToListAsync();

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
                var ventas = await _context.Ventas
                    .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                    .ToListAsync();

                var hoja = workbook.Worksheets.Add("Horas Pico");
                hoja.Cell(1, 1).Value = "Hora";
                hoja.Cell(1, 2).Value = "Total Ventas";
                hoja.Row(1).Style.Font.Bold = true;
                var horasPico = ventas.GroupBy(v => v.Fecha.Hour)
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
                var ventas = await _context.Ventas
                    .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                    .ToListAsync();

                var hoja = workbook.Worksheets.Add("Dias con mas movimiento");
                hoja.Cell(1, 1).Value = "Dia";
                hoja.Cell(1, 2).Value = "Total Ventas";
                hoja.Row(1).Style.Font.Bold = true;
                var dias = new[] { "Domingo", "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado" };
                var diasSemana = ventas.GroupBy(v => v.Fecha.DayOfWeek)
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
                var ventas = await _context.Ventas
                    .Include(v => v.Cliente)
                    .Include(v => v.Comanda).ThenInclude(c => c.Usuario)
                    .Include(v => v.Comanda).ThenInclude(c => c.DetallesComanda).ThenInclude(d => d.Producto)
                    .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                    .OrderByDescending(v => v.Fecha)
                    .ToListAsync();

                var hoja = workbook.Worksheets.Add("Ventas del periodo");
                hoja.Cell(1, 1).Value = "Fecha";
                hoja.Cell(1, 2).Value = "Mesa";
                hoja.Cell(1, 3).Value = "Mesero";
                hoja.Cell(1, 4).Value = "Cliente";
                hoja.Cell(1, 5).Value = "NIT/CI";
                hoja.Cell(1, 6).Value = "Metodo de Pago";
                hoja.Cell(1, 7).Value = "Productos";
                hoja.Cell(1, 8).Value = "Total (Bs)";
                hoja.Row(1).Style.Font.Bold = true;

                for (int i = 0; i < ventas.Count; i++)
                {
                    var v = ventas[i];
                    var prods = string.Join(", ", v.Comanda.DetallesComanda.Select(d => $"{d.Producto.Nombre} x{d.Cantidad}"));
                    hoja.Cell(i + 2, 1).Value = v.Fecha.ToString("dd/MM/yyyy HH:mm");
                    hoja.Cell(i + 2, 2).Value = $"Mesa {v.Comanda.Nro_Mesa}";
                    hoja.Cell(i + 2, 3).Value = $"{v.Comanda.Usuario.Nombre} {v.Comanda.Usuario.Apellido}";
                    hoja.Cell(i + 2, 4).Value = v.Cliente != null ? v.Cliente.Nombre : "Sin cliente";
                    hoja.Cell(i + 2, 5).Value = v.Cliente != null ? v.Cliente.NITCI : "-";
                    hoja.Cell(i + 2, 6).Value = v.MetodoPago.ToString();
                    hoja.Cell(i + 2, 7).Value = prods;
                    hoja.Cell(i + 2, 8).Value = v.Total;
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

            var totalRecaudado = await _context.Ventas
                .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                .SumAsync(v => v.Total);

            string cuerpoHtml = "";
            string tituloReporte = "";

            if (tipoReporte == "productos")
            {
                tituloReporte = "Productos mas vendidos";
                var productos = await _context.DetalleComandas
                    .Include(d => d.Comanda).ThenInclude(c => c.Venta)
                    .Include(d => d.Producto)
                    .Where(d => d.Comanda.Venta != null &&
                                d.Comanda.Venta.Fecha >= desdeInicio &&
                                d.Comanda.Venta.Fecha <= hastaFin)
                    .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
                    .Select(g => new
                    {
                        Nombre = g.Key.Nombre,
                        Cantidad = g.Sum(d => d.Cantidad),
                        Total = g.Sum(d => d.Cantidad * d.Precio_unitario)
                    })
                    .OrderByDescending(p => p.Cantidad)
                    .ToListAsync();

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
                    .Include(d => d.Comanda).ThenInclude(c => c.Venta)
                    .Include(d => d.Producto).ThenInclude(p => p.Categoria)
                    .Where(d => d.Comanda.Venta != null &&
                                d.Comanda.Venta.Fecha >= desdeInicio &&
                                d.Comanda.Venta.Fecha <= hastaFin)
                    .GroupBy(d => new { d.Producto.CategoriaId, d.Producto.Categoria.Nombre })
                    .Select(g => new
                    {
                        Categoria = g.Key.Nombre,
                        Cantidad = g.Sum(d => d.Cantidad),
                        Total = g.Sum(d => d.Cantidad * d.Precio_unitario)
                    })
                    .OrderByDescending(c => c.Total)
                    .ToListAsync();

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
                var ventas = await _context.Ventas
                    .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                    .ToListAsync();
                var horasPico = ventas.GroupBy(v => v.Fecha.Hour)
                    .Select(g => new { Hora = g.Key, Total = g.Count() })
                    .OrderByDescending(h => h.Total).ToList();

                cuerpoHtml = $@"
        <table border='1' cellpadding='6' cellspacing='0' width='100%'>
            <thead style='background:#333; color:white'>
                <tr><th>Hora</th><th>Total Ventas</th></tr>
            </thead>
            <tbody>
                {string.Join("", horasPico.Select(h => $"<tr><td>{h.Hora}:00 - {h.Hora + 1}:00</td><td>{h.Total}</td></tr>"))}
            </tbody>
        </table>";
            }
            else if (tipoReporte == "dias")
            {
                tituloReporte = "Dias con mas movimiento";
                var ventas = await _context.Ventas
                    .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                    .ToListAsync();
                var dias = new[] { "Domingo", "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado" };
                var diasSemana = ventas.GroupBy(v => v.Fecha.DayOfWeek)
                    .Select(g => new { Dia = dias[(int)g.Key], Total = g.Count() })
                    .OrderByDescending(d => d.Total).ToList();

                cuerpoHtml = $@"
        <table border='1' cellpadding='6' cellspacing='0' width='100%'>
            <thead style='background:#333; color:white'>
                <tr><th>Dia</th><th>Total Ventas</th></tr>
            </thead>
            <tbody>
                {string.Join("", diasSemana.Select(d => $"<tr><td>{d.Dia}</td><td>{d.Total}</td></tr>"))}
            </tbody>
        </table>";
            }
            else if (tipoReporte == "comandas")
            {
                tituloReporte = "Ventas del periodo";
                var ventas = await _context.Ventas
                    .Include(v => v.Cliente)
                    .Include(v => v.Comanda).ThenInclude(c => c.Usuario)
                    .Include(v => v.Comanda).ThenInclude(c => c.DetallesComanda).ThenInclude(d => d.Producto)
                    .Where(v => v.Fecha >= desdeInicio && v.Fecha <= hastaFin)
                    .OrderByDescending(v => v.Fecha)
                    .ToListAsync();

                cuerpoHtml = $@"
        <table border='1' cellpadding='6' cellspacing='0' width='100%'>
            <thead style='background:#333; color:white'>
                <tr>
                    <th>Fecha</th><th>Mesa</th><th>Mesero</th>
                    <th>Cliente</th><th>NIT/CI</th>
                    <th>Metodo Pago</th><th>Productos</th><th>Total</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", ventas.Select(v => $@"
                <tr>
                    <td>{v.Fecha:dd/MM/yyyy HH:mm}</td>
                    <td>Mesa {v.Comanda.Nro_Mesa}</td>
                    <td>{v.Comanda.Usuario.Nombre} {v.Comanda.Usuario.Apellido}</td>
                    <td>{(v.Cliente != null ? v.Cliente.Nombre : "Sin cliente")}</td>
                    <td>{(v.Cliente != null ? v.Cliente.NITCI : "-")}</td>
                    <td>{v.MetodoPago}</td>
                    <td>{string.Join(", ", v.Comanda.DetallesComanda.Select(d => $"{d.Producto.Nombre} x{d.Cantidad}"))}</td>
                    <td>Bs {v.Total:0.00}</td>
                </tr>"))}
            </tbody>
        </table>";
            }

            var html = $@"
    <html><body style='font-family:Arial; padding:20px'>
        <h1 style='color:#333'>Reporte - {tituloReporte}</h1>
        <p>Periodo: <strong>{desdeInicio:dd/MM/yyyy}</strong> al <strong>{hasta:dd/MM/yyyy}</strong></p>
        <p>Total recaudado: <strong>Bs {totalRecaudado:0.00}</strong></p>
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
