using Microsoft.VisualBasic;
using OrderGenerator.Models;
using OrderGenerator.Services.Interfaces;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime;

namespace OrderGenerator.Services
{
    public class FixInitiator : MessageCracker, IApplication, IFixInitiator
    {
        private SessionID? _sessionID;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingOrders = new();
        private readonly SocketInitiator _initiator;

        public FixInitiator()
        {
            var settings = new SessionSettings("initiator.cfg");
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new FileLogFactory(settings);
            _initiator = new SocketInitiator(this, storeFactory, settings, logFactory);
            _initiator.Start();
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void FromApp(QuickFix.Message message, SessionID sessionID) => Crack(message, sessionID);
        public void OnCreate(SessionID sessionID) => _sessionID = sessionID;
        public void OnLogon(SessionID sessionID) => Console.WriteLine($"Conectado ao servidor. {sessionID}");
        public void OnLogout(SessionID sessionID) => Console.WriteLine($"Desconectado. {sessionID}");
        public void ToAdmin(QuickFix.Message message, SessionID sessionID) { }
        public void ToApp(QuickFix.Message message, SessionID sessionID) { }
        public void OnMessage(ExecutionReport report, SessionID sessionID)
        {
            var clOrdID = report.ClOrdID.getValue();
            var status = report.ExecType.Equals(ExecType.REJECTED) ? "REJEITADA" : "ACEITA";

            if (_pendingOrders.TryRemove(clOrdID, out var tcs))
            {
                tcs.TrySetResult($"Ordem {status}: {report}");
            }
        }

        public void SendNewOrder(Order order)
        {
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

    }
}
