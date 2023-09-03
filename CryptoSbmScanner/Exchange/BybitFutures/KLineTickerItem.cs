﻿using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Sockets;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.BybitFutures;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class KLineTickerItem : KLineTickerItemBase
{
    private BybitSocketClient socketClient;
    private UpdateSubscription _subscription;

    public KLineTickerItem(string apiExchangeName, CryptoQuoteData quoteData) : base(apiExchangeName, quoteData)
    {
    }

    private void ProcessCandle(string topic, BybitKlineUpdate kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        // De interval wordt geprefixed in de topic
        string symbolName = topic[2..];
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                TickerCount++;
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                Process1mCandle(symbol, kline.StartTime, kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume);

            }
        }

    }


    public override async Task StartAsync()
    {
        ConnectionLostCount = 0;

        if (Symbols.Count > 0)
        {
            socketClient = new BybitSocketClient();
            var subscriptionResult = await socketClient.V5LinearApi.SubscribeToKlineUpdatesAsync(
                Symbols, KlineInterval.OneMinute, data =>
            {
                //if (data.Data.Data.Confirm)
                {
                    //Er zit tot ongeveer 8 a 10 seconden vertraging is van de exchange tot hier, dat moet ansich genoeg zijn
                    //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.Symbol));

                    foreach (BybitKlineUpdate kline in data.Data)
                    {
                        if (kline.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                            Task.Run(() => { ProcessCandle(data.Topic, kline); });
                    }

                }
            });
            // .ConfigureAwait(false);

            // Subscribe to network-related stuff
            if (subscriptionResult.Success)
            {
                _subscription = subscriptionResult.Data;

                // Events
                _subscription.Exception += Exception;
                _subscription.ConnectionLost += ConnectionLost;
                _subscription.ConnectionRestored += ConnectionRestored;


                //    // TODO: Put a CancellationToken in order to stop it gracefully
                //    BinanceClient client = new();
                //    var keepAliveTask = Task.Run(async () =>
                //    {
                //        while (true)
                //        {
                //            await client.V5LinearApi.Account.KeepAliveUserStreamAsync(subscriptionResult.Data.); //???
                //            await Task.Delay(TimeSpan.FromMinutes(30));
                //        }
                //    });
                //GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m started candle stream {symbols.Count} symbols");
            }
            else
            {
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} 1m ERROR starting kline ticker {subscriptionResult.Error.Message}");
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} 1m ERROR starting kline ticker {string.Join(',', Symbols)}");
                
            }
        }
    }

    public override async Task StopAsync()
    {
        if (_subscription == null)
            return;

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);
        _subscription = null;

        socketClient?.Dispose();
        socketClient = null;
    }

    private void ConnectionLost()
    {
        ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} 1m kline ticker connection lost.");
        ScannerSession.ConnectionWasLost("");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} 1m kline ticker connection restored.");
        ScannerSession.ConnectionWasRestored("");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} 1m kline ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}