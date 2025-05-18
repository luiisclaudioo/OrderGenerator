using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Models;
using OrderGenerator.Services;

namespace OrderGenerator.Controllers
{
    public class OrderController : Controller
    {
        private readonly FixInitiator _fix;

        public OrderController(FixInitiator fix)
        {
            _fix = fix;
            _fix.Start();
        }
        public IActionResult Index()
        {
            ViewBag.IsConnected = _fix.IsConnected;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Symbol,Side,Quantity,Price")] Order model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.IsConnected = _fix.IsConnected;
                    return View(model);
                }

                try
                {
                    var retorno = await _fix.SendNewOrderAsync(model);
                    if (!string.IsNullOrEmpty(retorno) && retorno.Contains("ACEITA"))
                        TempData["Sucesso"] = retorno;
                    else
                        TempData["Erro"] = retorno;
                }
                catch (Exception ex)
                {
                    TempData["Erro"] = $"Erro ao enviar ordem: {ex.Message}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
