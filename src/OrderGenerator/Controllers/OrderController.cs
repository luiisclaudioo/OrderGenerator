using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Models;
using OrderGenerator.Services.Interfaces;

namespace OrderGenerator.Controllers
{
    public class OrderController : Controller
    {
        private readonly IFixInitiator _fix;

        public OrderController(IFixInitiator fix)
        {
            _fix = fix;            
        }
        public IActionResult Index()
        {            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Symbol,Side,Quantity,Price")] Order model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //$"Ordem enviada: Símbolo: {model.Symbol}, Lado: {model.Side}, Quantidade: {model.Quantity}, Preço: {model.Price}";
                    TempData["Sucesso"] = await _fix.SendNewOrderAsync(model);
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch
            {
                return View();
            }
        }
    }
}
