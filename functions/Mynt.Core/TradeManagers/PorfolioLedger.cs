﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mynt.Core.Api;
using Mynt.Core.Models;

namespace Mynt.Core
{
    public class PorfolioLedger
    {
        private readonly IExchangeApi api;

        private IEnumerable<CreditPosition> creditPositions;

        private IList<(string, string)> openOrders = new List<(string, string)>();

        public PorfolioLedger(IExchangeApi api, IEnumerable<string> tradableSymbols, double initialBudget):
            this(api, tradableSymbols.Select(_=>(_,initialBudget)))
        {
        }

        public PorfolioLedger(IExchangeApi api, IEnumerable<(string, double)> entries)
        {
            this.api = api;
            var task = CreateCreditPositions(api, entries);
            task.Wait();
            creditPositions = task.Result;
        }

        public CreditPosition GetCreditPosition(string symbol)
        {
            return creditPositions.SingleOrDefault(_ => _.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<CreditPosition> GetCreditPositions()
        {
            return creditPositions;
        }

        public void AddOpenOrder(string orderId, string market)
        {
            openOrders.Add((orderId, market));
        }

        public async void Update()
        {
            for (int index = openOrders.Count - 1; index >=0; index--)
            {
                var item = openOrders[index];
                var order = await api.GetOrder(item.Item1, item.Item2);

                if (order == null)
                {
                    openOrders.Remove(item);
                    break;
                }

                switch (order.Status)
                {
                    case Mynt.Core.Enums.OrderStatus.Canceled:
                    case Mynt.Core.Enums.OrderStatus.Expired:
                    case Mynt.Core.Enums.OrderStatus.PendingCancel:
                    case Mynt.Core.Enums.OrderStatus.Rejected:
                        // Just remove the order.
                        openOrders.Remove(item);
                        break;
                    case Mynt.Core.Enums.OrderStatus.Filled:
                        // Update the credit position and remove the order.
                        UpdateCreditPosition(order);
                        openOrders.Remove(item);
                        break;
                    default:
                        // Do nothing.
                        break;
                }
            }
        }

        private void UpdateCreditPosition(Mynt.Core.Models.Order order)
        {
            var creditPosition = creditPositions.SingleOrDefault(_ => _.Symbol == order.Symbol);
            if (creditPosition != null)
            {
                if (order.Side == Mynt.Core.Enums.OrderSide.Buy)
                {
                    creditPosition.RegisterBuy(order.ExecutedQuantity, order.Price);
                }
                else
                {
                    creditPosition.RegisterSell(order.ExecutedQuantity, order.Price);
                }
            }
        }

        private async static Task<IEnumerable<CreditPosition>> CreateCreditPositions(IExchangeApi api, IEnumerable<(string, double)> entries)
        {
            IList<CreditPosition> creditPositions = new List<CreditPosition>();
            foreach ((string,double) entry in entries)
            {
                var ticker = await api.GetTicker(entry.Item1);
                var balance = await api.GetBalance(entry.Item1);
                var btcCredit = entry.Item2 - balance.Balance * ticker.Last;
                var creditPosition = new CreditPosition(entry.Item1, 0.0005, btcCredit); // TODO: Make fee configurable.

                creditPositions.Add(creditPosition);
            }

            return creditPositions;
        }
    }
}