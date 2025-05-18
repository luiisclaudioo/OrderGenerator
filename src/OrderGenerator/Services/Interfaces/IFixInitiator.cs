using OrderGenerator.Models;

namespace OrderGenerator.Services.Interfaces
{
    public interface IFixInitiator
    {
        public void Start();
        public void Stop();
        public void SendNewOrder(Order order);
        public Task<string> SendNewOrderAsync(Order order);
    }
}
