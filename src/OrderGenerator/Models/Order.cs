using System.ComponentModel.DataAnnotations;

namespace OrderGenerator.Models
{
    public class Order
    {
        [Required(ErrorMessage = "O símbolo é obrigatório")]
        [Display(Name = "Símbolo")]
        [RegularExpression("PETR4|VALE3|VIIA4", ErrorMessage = "Símbolo inválido.")]
        public string Symbol { get; set; }

        [Required(ErrorMessage = "A lado é obrigatória")]
        [Display(Name = "Lado")]
        [RegularExpression("BUY|SELL", ErrorMessage = "Lado deve ser 'Compra' ou 'Venda'.")]
        public string Side { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória.")]
        [Range(1, 99999, ErrorMessage = "A quantidade deve ser um valor positivo menor que 100.000.")]
        [Display(Name = "Quantidade")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório.")]
        [Range(0.01, 999.99, ErrorMessage = "O preço deve ser um valor positivo, múltiplo de 0.01 e menor que 1k")]
        [Display(Name = "Preço")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "O preço deve ser múltiplo de 0.01.")]
        public decimal Price { get; set; }

    }
}
