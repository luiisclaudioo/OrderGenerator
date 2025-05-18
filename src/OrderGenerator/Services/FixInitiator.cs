using OrderGenerator.Models;
using OrderGenerator.Services.Interfaces;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System.Collections.Concurrent;

namespace OrderGenerator.Services
{
    public class FixInitiator : MessageCracker, IApplication, IFixInitiator
    {
        private SessionID _sessionID;
        private ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingOrders = new();
        private IInitiator _initiator;
        private bool _isConnected = false;

        #region Config
        public void Start()
        {
            if (_initiator != null && _initiator.IsLoggedOn)
                return;

            var settings = new SessionSettings("initiator.cfg");
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new FileLogFactory(settings);
            //var logFactory = new ScreenLogFactory(settings);
            _initiator = new SocketInitiator(this, storeFactory, settings, logFactory);
            _initiator.Start();
        }

        public void Stop() => _initiator?.Stop();

        public bool IsConnected => _isConnected;
        #endregion

        #region Eventos
        public void FromAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void FromApp(QuickFix.Message message, SessionID sessionID) => Crack(message, sessionID);
        public void OnCreate(SessionID sessionID) => _sessionID = sessionID;
        public void OnLogon(SessionID sessionID)
        {
            _isConnected = true;
            Console.WriteLine($"Conectado ao servidor. {sessionID}");
        }
        public void OnLogout(SessionID sessionID)
        {
            _isConnected = false;
            Console.WriteLine($"Desconectado. {sessionID}");
        }
        public void ToAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void ToApp(QuickFix.Message message, SessionID sessionID) { }
        #endregion

        #region Eventos Mensagem Servidor
        public void OnMessage(ExecutionReport report, SessionID sessionID)
        {
            var clOrdID = report.ClOrdID.getValue();
            var status = report.ExecType.getValue() == ExecType.REJECTED ? "REJEITADA" : "ACEITA";
            var symbol = report.Symbol.getValue();
            var qty = report.GetDecimal(Tags.OrderQty);
            var price = report.GetDecimal(Tags.Price);
            var side = report.Side.getValue() == Side.BUY ? "BUY" : "SELL";

            var text = new Text();
            var motivo = string.Empty;
            if (report.IsSetField(text))
            {
                report.GetField(text);
                motivo = text.getValue();
            }                        

            if (_pendingOrders.TryRemove(clOrdID, out var tcs))
                tcs.TrySetResult($"Ordem: {status}" + (!string.IsNullOrEmpty(motivo) ? $" - {motivo}" : ""));
        }
        #endregion

        #region Send
        public void SendNewOrder(Order order)
        {
            if (_sessionID == null)
                throw new InvalidOperationException("Sessão FIX ainda não estabelecida.");

            var newOrder = new NewOrderSingle(
               new ClOrdID(Guid.NewGuid().ToString()),
               new Symbol(order.Symbol),
               new Side(order.Side == "BUY" ? Side.BUY : Side.SELL),
               new TransactTime(DateTime.UtcNow),
               new OrdType(OrdType.LIMIT)
            );

            newOrder.Set(new OrderQty(order.Quantity));
            newOrder.Set(new Price(order.Price));
            Session.SendToTarget(newOrder, _sessionID);
        }

        public async Task<string> SendNewOrderAsync(Order order)
        {
            if (_sessionID == null)
                throw new InvalidOperationException("Sessão FIX ainda não estabelecida.");

            string clOrdId = Guid.NewGuid().ToString();

            var newOrder = new NewOrderSingle(
               new ClOrdID(clOrdId),
               new Symbol(order.Symbol),
               new Side(order.Side == "BUY" ? Side.BUY : Side.SELL),
               new TransactTime(DateTime.UtcNow),
               new OrdType(OrdType.LIMIT)
            );

            newOrder.Set(new OrderQty(order.Quantity));
            newOrder.Set(new Price(order.Price));

            var tcs = new TaskCompletionSource<string>();
            _pendingOrders[clOrdId] = tcs;

            Session.SendToTarget(newOrder, _sessionID);

            var timeout = Task.Delay(5000);
            var completed = await Task.WhenAny(tcs.Task, timeout);

            return completed == tcs.Task
                ? await tcs.Task
                : "Aguardando resposta da ordem.";
        }
        #endregion
    }
}
