﻿using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

namespace CryptoSbmScanner.Exchange.Kucoin;

/// <summary>
/// Fetch the candles from Binance
/// </summary>
public class FetchCandles
{
#if KUCOINDEBUG
    private static int tickerIndex = 0;
#endif
    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);

    private static KlineInterval GetExchangeInterval(CryptoInterval interval)
    {
        var binanceInterval = interval.IntervalPeriod switch
        {
            CryptoIntervalPeriod.interval1m => KlineInterval.OneMinute,
            CryptoIntervalPeriod.interval3m => KlineInterval.ThreeMinutes,
            CryptoIntervalPeriod.interval5m => KlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => KlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => KlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => KlineInterval.OneHour,
            CryptoIntervalPeriod.interval2h => KlineInterval.TwoHours,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHours,
            CryptoIntervalPeriod.interval6h => KlineInterval.SixHours,
            CryptoIntervalPeriod.interval8h => KlineInterval.EightHours,
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHours,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            //case IntervalPeriod.interval1w:
            //    binanceInterval = KlineInterval.OneWeek;
            //    break;
            _ => KlineInterval.OneWeek,// Ten teken dat het niet ondersteund wordt
        };
        return binanceInterval;
    }

    private static async Task<long> GetCandlesForInterval(KucoinRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval)
    {
        KlineInterval exchangeInterval = GetExchangeInterval(interval);
        if (exchangeInterval == KlineInterval.OneWeek)
            return 0;

        //KucoinWeights.WaitForFairWeight(1);
        string prefix = $"{Api.ExchangeName} {symbol.Name} {interval.Name}";

        // Er is een maximum van circa 1500 volgens de docs
        // bewaust 5 candles terug (omdat de API qua klines raar doet, hebben we in ieder geval 1 te pakken)
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized - 10 * interval.Duration);
        while (true)
        {
            long dateNow = CandleTools.GetUnixTime(DateTime.UtcNow, 0);


            DateTime dateEnd = dateStart.AddSeconds(1500 * interval.Duration);
            var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Base + '-' + symbol.Quote, exchangeInterval, dateStart, dateEnd);
            //GlobalData.AddTextToLogTab($"Debug: {symbol.Name} {interval.Name} volume={symbol.Volume} start={dateStart} end={dateEnd} url={result.RequestUrl}");
            if (!result.Success)
            {
                // We doen het gewoon over (dat is tenminste het advies)
                // 13-07-2023 14:08:00 AOA-BTC 30m error getting klines 429000: Too Many Requests
                if (result.Error.Code == 429000)
                {
                    GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: (because of rate limits)");
                    Thread.Sleep(10000);
                    continue;
                }

                // Might do something better than this
                GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
                return 0;
            }


            // Might have problems with no internet etc.
            if (result == null || result.Data == null || !result.Data.Any())
            {
                GlobalData.AddTextToLogTab($"{prefix} ophalen vanaf {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} geen candles ontvangen");
                return 0;
            }

            // Remember
            long startFetchDate = (long)symbolInterval.LastCandleSynchronized;

#if USELOCKS
            Monitor.Enter(symbol.CandleList);
#endif
            try
            {
                long last = long.MinValue;
                // Combine candles, calculating other interval's
                foreach (var kline in result.Data)
                {
                    // Quoted = volume * price (expressed in usdt/eth/btc etc), base is coins
                    CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, interval, kline.OpenTime,
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.QuoteVolume, false);

                    // Onthoud de laatste aangeleverde candle, t/m die datum is ten minste alles binnen gehaald
                    if (candle.OpenTime > last)
                        last = candle.OpenTime;
#if SQLDATABASE
                GlobalData.TaskSaveCandles.AddToQueue(candle);
#endif
                }

                // Voor de volgende GetCandlesForInterval() sessie
                if (last > long.MinValue)
                {
                    symbolInterval.IsChanged = true; // zie tevens setter (maar ach)
                    symbolInterval.LastCandleSynchronized = last;
                    // Alternatief (maar als er gaten in de candles zijn geeft dit problemen, endless loops)
                    //CandleTools.UpdateCandleFetched(symbol, interval);
                }

#if KUCOINDEBUG
                // Debug, wat je al niet moet doen voor een exchange...
                tickerIndex++;
                long unix = CandleTools.GetUnixTime(DateTime.UtcNow, 0);
                string filename = GlobalData.GetBaseDir() + $@"\Kucoin\Candles-{symbol.Name}-{interval.Name}-{unix}-#{tickerIndex}.json";
                string text = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true});
                File.WriteAllText(filename, text);
#endif
            }
            finally
            {
#if USELOCKS
                Monitor.Exit(symbol.CandleList);
#endif
            }


            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
            string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " ophalen vanaf " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " UTC tot " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime() + " UTC";
            GlobalData.AddTextToLogTab(s + " opgehaald: " + result.Data.Count() + " totaal: " + candles.Count.ToString());
            return result.Data.Count();
        }
    }


    private static async Task FetchCandlesInternal(KucoinRestClient client, CryptoSymbol symbol, long fetchEndUnix)
    {
        DateTime[] fetchFrom = new DateTime[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];

        DateTime utcNow = DateTime.UtcNow;
        foreach (CryptoInterval interval in GlobalData.IntervalList)
            fetchFrom[(int)interval.IntervalPeriod] = utcNow;

        // Determine the (maximum) startdate (without knowing what we already have)
        // If the exchange does not have this interval than make the lower timeframe
        // a bit bigger so we can calculate the candles ourselves
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, utcNow);

            CryptoInterval loopInterval = interval;
            while (true)
            {
                DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                if (fetchFrom[(int)loopInterval.IntervalPeriod] > startFetchUnixDate)
                    fetchFrom[(int)loopInterval.IntervalPeriod] = startFetchUnixDate;

                // Is this timeframe supported?
                if (GetExchangeInterval(loopInterval) != KlineInterval.OneWeek)
                    break;
                else
                    loopInterval = loopInterval.ConstructFrom;
            }
        }

        // Debug
        //foreach (CryptoInterval interval in GlobalData.IntervalList)
        //  GlobalData.AddTextToLogTab("Fetching " + symbol.Name + " " + interval.Name + " " + fetchFrom[(int)interval.IntervalPeriod].ToLocalTime());


        // Correct the start date with what we already have
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            DateTime startFetchUnixDate = fetchFrom[(int)interval.IntervalPeriod];
            long startFetchUnix = CandleTools.GetUnixTime(startFetchUnixDate, 60);

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.LastCandleSynchronized == null || startFetchUnix > symbolInterval.LastCandleSynchronized)
                symbolInterval.LastCandleSynchronized = startFetchUnix;
        }


        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            // Fetch the candles
            while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
            {
                long lastDate = (long)symbolInterval.LastCandleSynchronized;
                //DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                //GlobalData.AddTextToLogTab("Debug: Fetching " + symbol.Name + " " + interval.Name + " " + dateStart.ToLocalTime());


                if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                    break;

                // Nothing more? (we have coins stopping, beaware for endless loops)
                long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval);
                if (symbolInterval.LastCandleSynchronized == lastDate || candleCount == 0)
                    break;
            }

#if USELOCKS
            Monitor.Enter(symbol.CandleList);
#endif
            try
            {
                // Fill missing candles (the only place we know fore it can be done)
                // We hebben de candles opgevraagd van x tot y, dat betekend dat we alle candles hebben,
                // eventueel ontbrekende candles in deze reeks mogen we opvullen met een "zero" candle
                if (symbolInterval.CandleList.Any())
                {
                    CryptoCandle stickOld = symbolInterval.CandleList.Values.First();
                    //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
                    long unixTime = stickOld.OpenTime;
                    while (unixTime < (long)symbolInterval.LastCandleSynchronized)
                    {
                        if (!symbolInterval.CandleList.TryGetValue(unixTime, out CryptoCandle candle))
                        {
                            candle = new()
                            {
#if SQLDATABASE
                                ExchangeId = symbol.Exchange.Id,
                                SymbolId = symbol.Id,
                                IntervalId = interval.Id,
#endif
                                //Symbol = symbol,
                                //Interval = interval,
                                OpenTime = unixTime,
                                Open = stickOld.Close,
                                High = stickOld.Close,
                                Low = stickOld.Close,
                                Close = stickOld.Close,
                                Volume = 0
                            };
                            symbolInterval.CandleList.Add(candle.OpenTime, candle);
                            //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Added missing candle " + CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
                        }
                        stickOld = candle;
                        unixTime += interval.Duration;
                    }
                }

                // Calculate higher interval candles from the lower interval (if available)
                for (int j = i + 1; j < GlobalData.IntervalList.Count; j++)
                {
                    CryptoInterval intervalCalc = GlobalData.IntervalList[j];
                    if (intervalCalc.IntervalPeriod > interval.IntervalPeriod)
                    {
                        // Naar het lagere tijd interval om de eerste en laatste candle te achterhalen
                        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(intervalCalc.ConstructFrom.IntervalPeriod);
                        SortedList<long, CryptoCandle> candlesLowerInterval = symbolPeriod.CandleList;
                        if (candlesLowerInterval.Values.Any())
                        {
                            long unixFirst = candlesLowerInterval.Values.First().OpenTime;
                            unixFirst -= unixFirst % intervalCalc.Duration; // too much?
                            //DateTime dateFirstDebug = CandleTools.GetUnixDate(unixFirst);

                            long unixLast = candlesLowerInterval.Values.Last().OpenTime;
                            unixLast -= unixLast % intervalCalc.Duration; // too much? ++ ?
                            //DateTime dateLastDebug = CandleTools.GetUnixDate(unixLast);

                            // Bulk calculation (shared code with the 1m stream)
                            long unixLoop = unixFirst;
                            while (unixLoop <= unixLast)
                            {
                                CandleTools.CalculateCandleForInterval(intervalCalc, intervalCalc.ConstructFrom, symbol, unixLoop);
                                unixLoop += intervalCalc.Duration;
                            }
                            CandleTools.UpdateCandleFetched(symbol, intervalCalc);
                        }
                    }
                }

            }
            finally
            {
#if USELOCKS
                Monitor.Exit(symbol.CandleList);
#endif
            }
        }
    }

    public static async Task FetchCandlesAsync(long fetchEndUnix, Queue<CryptoSymbol> queue)
    {
        try
        {
            // Reuse the socket in this thread, because:
            // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
            using KucoinRestClient client = new();

            while (true)
            {
                CryptoSymbol symbol;

                Monitor.Enter(queue);
                try
                {
                    if (queue.Count > 0)
                        symbol = queue.Dequeue();
                    else
                        break;
                }
                finally
                {
                    Monitor.Exit(queue);
                }

                // Er is niet geswicthed van exchange (omdat het ophalen zo lang duurt)
                if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId)
                    await FetchCandlesInternal(client, symbol, fetchEndUnix);
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " + 
        }
    }


    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(string.Format("Fetching {0} information", exchange.Name));
            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    GlobalData.SetCandleTimerEnable(false);
                    //GlobalData.AddTextToLogTab("");
                    //GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                    // Bij het opstarten is deze (vanuit de LoadData) reeds uitgevoerd
                    if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Initializing)
                        await Task.Run(FetchSymbols.ExecuteAsync);

                    GlobalData.AddTextToLogTab("Aantal symbols = " + exchange.SymbolListName.Values.Count.ToString());


                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol())
                            continue;

                        if (symbol.QuoteData.FetchCandles)
                        {
                            if (symbol.QuoteData.MinimalVolume > 0 && symbol.Volume > 0.1m * symbol.QuoteData.MinimalVolume)
                                //if (symbol.Name.Equals("BTCUSDT") || symbol.Name.Equals("ETHUSDT") || symbol.Name.Equals("ADABTC") || symbol.Name.Equals("LEVERBTC"))
                                queue.Enqueue(symbol);
                        }
                    }


                    // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                    // De datum en tijd tot na het activeren van beide streams (overlap)
                    DateTime fetchEndUnixDate = DateTimeOffset.UtcNow.UtcDateTime;
                    long fetchEndUnix = CandleTools.GetUnixTime(fetchEndUnixDate, 60);


                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = new();
                    while (taskList.Count < 5)
                    {
                        Task task = Task.Run(async () => { await FetchCandlesAsync(fetchEndUnix, queue); });
                        taskList.Add(task);
                    }
                    Task t = Task.WhenAll(taskList);
                    t.Wait();

                    GlobalData.AddTextToLogTab("Candles ophalen klaar", true);
                }
                finally
                {
                    // Enabled analysing
                    GlobalData.SetCandleTimerEnable(true);

                    Semaphore.Release();
                }
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("error get prices " + error.ToString() + "\r\n");
            }
        }
    }


}
