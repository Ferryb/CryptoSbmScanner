﻿using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Sockets;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using NPOI.Util;

namespace CryptoSbmScanner.Exchange.BybitFutures;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class KLineTickerStream : KLineTickerItem
{
    private BybitSocketClient socketClient;
    private UpdateSubscription _subscription;

    public KLineTickerStream(CryptoQuoteData quoteData) : base(quoteData)
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

        // De interval wordt geprefixed in de topic (rare jongens bij Bybit, extra veld?)
        string symbolName = topic[2..];


        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                TickerCount++;
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));

                CryptoCandle candle = null;
                Monitor.Enter(symbol.CandleList);
                try
                {
                    // Dit is de laatste bekende prijs (de priceticker vult aan)
                    symbol.LastPrice = kline.ClosePrice;

                    // Process the single 1m candle
                    candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], kline.StartTime,
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, false);
#if SQLDATABASE
                    GlobalData.TaskSaveCandles.AddToQueue(candle);
#endif

                    // Calculate the higher timeframes
                    foreach (CryptoInterval interval in GlobalData.IntervalList)
                    {
                        // Deze doen een call naar de TaskSaveCandles en doet de UpdateCandleFetched (wellicht overlappend?)
                        if (interval.ConstructFrom != null)
                            CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, candle.OpenTime);
                        CandleTools.UpdateCandleFetched(symbol, interval);
                    }
                }
                finally
                {
                    Monitor.Exit(symbol.CandleList);
                }


                // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
                if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running && candle != null)
                {
                    // Aanbieden voor analyse
                    GlobalData.ThreadMonitorCandle.AddToQueue(symbol, candle);
                }
            }
        }

    }


    public override async Task StartAsync()
    {
        ConnectionLostCount = 0;

        if (symbols.Count > 0)
        {
            socketClient = new BybitSocketClient();
            var subscriptionResult = await socketClient.V5LinearApi.SubscribeToKlineUpdatesAsync(
                symbols, KlineInterval.OneMinute, data =>
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
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteDataName} 1m ERROR starting candle stream {subscriptionResult.Error.Message}");
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteDataName} 1m ERROR starting candle stream {string.Join(',', symbols)}");
                
            }
        }
    }

    public override async Task StopAsync()
    {
        if (_subscription != null)
            return; // Task.CompletedTask;

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);
    }

    private void ConnectionLost()
    {
        ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteDataName} 1m candle ticker connection lost.");
        ScannerSession.ConnectionWasLost("");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteDataName} 1m candle ticker connection restored.");
        ScannerSession.ConnectionWasRestored("");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} 1m candle ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
