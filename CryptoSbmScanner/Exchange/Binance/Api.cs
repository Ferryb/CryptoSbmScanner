﻿using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Binance;




//Mogelijke foutmeldingen bij het kopen (of verkopen?):

//Te laag bedrag
//Filter failure: MIN_NOTIONAL
//price* quantity is too low to be a valid order for the symbol.

//Problemen met het aantal decimalen (zowel price als amount)
//buyResult.Error = {-1111: Precision is over the maximum defined for this asset. }

//Er is te weinig geld om de order te plaatsen
//buyResult.Error = {-1013: Filter failure: PRICE_FILTER }

// buyResult.Error = { -1013: Filter failure: LOT_SIZE }

//The relationship of the prices for the orders is not correct". The prices set in the OCO is breaking the Price rules.
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price < Last Price < Stop Price
// mooit overzicht: https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md


// Vanwege "The relationship of the prices for the orders is not correct." The prices set in the OCO 
// is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)

//"The relationship of the prices for the orders is not correct." The prices set in the OCO is breaking the Price rules. (de prijs is dan waarschijnlijk al hoger dan de gekozen sell prijs!!!!)
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price<Last Price<Stop Price

//De prijs is dan ondertussen al onder de StopPrice beland?


//The relationship of the prices for the orders is not correct."	The prices set in the OCO is breaking the Price rules.
//The rules are:
//SELL Orders: Limit Price > Last Price > Stop Price
//BUY Orders: Limit Price < Last Price < Stop Price
// https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md

/*
 * 
https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-servertime
https://api-testnet.bybit.com/v2/public/time
{"ret_code":0,"ret_msg":"OK","result":{},"ext_code":"","ext_info":"","time_now":"1688116858.760925"}

https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-announcement
https://api-testnet.bybit.com/v2/public/announcement
{"ret_code":0,"ret_msg":"OK","result":[],"ext_code":"","ext_info":"","time_now":"1688116961.886013"}
(dat lijkt nogal op die eerste..)


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querykline
https://api-testnet.bybit.com/v2/public/kline/list
{"retCode":10001,"retMsg":"The requested symbol is invalid.","result":{},"retExtInfo":{},"time":1688117090806}
https://api-testnet.bybit.com/v2/public/kline/list?symbol=BTCUSDT&interval=1


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querysymbol
https://api-testnet.bybit.com/spot/v3/public/symbols
(denk om de versie verschillen)

 */

public class Api : ExchangeBase
{
    public static readonly string ExchangeName = "Binance";

    public Api() : base()
    {
    }

    public override void ExchangeDefaults()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} defaults");
        // Default opties voor deze exchange
        //

        ExchangeHelper.PriceTicker = new PriceTicker();
        ExchangeHelper.KLineTicker = new KLineTicker();
#if TRADEBOT
        ExchangeHelper.UserData = new UserData();
#endif

        //BinanceRestClient.SetDefaultOptions(options => { 
        //    options.ApiCredentials = new ApiCredentials(apiKey, apiSecret); 
        //});


        //var client = new BinanceRestClient(options => {
        //    options.OutputOriginalData = true;
        //    options.Environment = BinanceEnvironment.Testnet;
        //    // Other options
        //});

        // Waarom werkt dit niet meer? (In CryptoBot is het okay)
        //BinanceClientOptions options = new();
        //if (GlobalData.Settings.ApiKey != "")
        //  options.ApiCredentials = new BinanceApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        //BinanceClient.SetDefaultOptions(options);
        BinanceRestClient.SetDefaultOptions(options =>
        {
            if (GlobalData.Settings.ApiKey != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });

        {
            //BinanceSocketClientOptions options = new();
            //if (GlobalData.Settings.ApiKey != "")
            //    options.ApiCredentials = new BinanceApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
            //options.SpotStreamsOptions.AutoReconnect = true;
            //options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
        }
        BinanceSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            if (GlobalData.Settings.ApiKey != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });
    }

    public async override Task FetchSymbolsAsync()
    {
        await FetchSymbols.ExecuteAsync();
    }
    public async override Task FetchCandlesAsync()
    {
        await FetchCandles.ExecuteAsync();
    }

    public override string GetAltradyCode()
    {
        return "BINA";
    }

    public override string GetHyperTraderCode()
    {
        return "binance";
    }

    public override string GetTradingViewCode()
    {
        return "BINANCE";
    }

#if TRADEBOT

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(SpotOrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            SpotOrderType.Market => CryptoOrderType.Market,
            SpotOrderType.Limit => CryptoOrderType.Limit,
            SpotOrderType.StopLoss => CryptoOrderType.StopLimit,
            SpotOrderType.StopLossLimit => CryptoOrderType.Oco, // negatief gevuld (denk ik)
            SpotOrderType.LimitMaker => CryptoOrderType.Oco, // postief gevuld
            _ => throw new Exception("Niet ondersteunde ordertype " + orderType.ToString()),
        };

        return localOrderType;
    }

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        CryptoOrderSide localOrderSide = orderSide switch
        {
            OrderSide.Buy => CryptoOrderSide.Buy,
            OrderSide.Sell => CryptoOrderSide.Sell,
            _ => throw new Exception("Niet ondersteunde orderside " + orderSide.ToString()),
        };

        return localOrderSide;
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.New => CryptoOrderStatus.New,
            OrderStatus.Filled => CryptoOrderStatus.Filled,
            OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus " + orderStatus.ToString()),
        };

        return localOrderStatus;
    }

    private void DumpOrder(CryptoSymbol symbol, TradeParams tradeParams, string extraText)
    {
        string text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} {4} PLACED price={5} stop={6} quantity={7} quotequantity={8}",
            symbol.Name, tradeParams.Side,
            tradeParams.OrderType.ToString(),
            tradeParams.OrderId,
            extraText,
            tradeParams.Price.ToString0(),
            tradeParams.StopPrice?.ToString0(),
            tradeParams.Quantity.ToString0(),
            tradeParams.QuoteQuantity.ToString0());
        GlobalData.AddTextToLogTab(text2);
        GlobalData.AddTextToTelegram(text2);
    }

    private void DumpError(CryptoSymbol symbol, CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, string extraText,
        string responseStatusCode, string error)
    {
        string text = string.Format("{0} ERROR {1} {2} order {3} {4} {5}\r\n", symbol.Name, orderType, orderSide, responseStatusCode, error, extraText);
        text += string.Format("quantity={0}\r\n", quantity.ToString0());
        text += string.Format("price={0}\r\n", price.ToString0());
        if (stop.HasValue)
            text += string.Format("stop={0}\r\n", stop?.ToString0());
        if (limit.HasValue)
            text += string.Format("limit={0}\r\n", limit?.ToString0());
        //text += string.Format("lastprice={0}\r\n", Symbol.LastPrice?.ToString0());
        //text += string.Format("trades={0}\r\n", Symbol.TradeList.Count);
        GlobalData.AddTextToLogTab(text);
        GlobalData.AddTextToTelegram(text);
    }

    public async Task<(bool result, TradeParams tradeParams)> BuyOrSell(
        CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, string extraText = "")
    {
        // Bij nader inzien is het gebruik van de TradeParams ovebodig, enkel de datums 
        // en de order id's die terugkomen zijn van belang, de trades vallen later...
        // Het kan dus best versimpeld worden (maar of het nu zoveel uitmaakt <nee>?)


        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, text, price, quantity));
            return (false, null);
        }

        TradeParams tradeParams = new()
        {
            CreateTime = currentDate,
            Side = orderSide,
            OrderType = orderType,
            Price = price, // the sell part (can also be a buy)
            StopPrice = stop, // OCO - the price at which the limit order to sell is activated
            LimitPrice = limit, // OCO - the lowest price that the trader is willing to accept
            Quantity = quantity,
            QuoteQuantity = price * quantity,
            //tradeParams.OrderId = 0;
        };

        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
        {
            DumpOrder(symbol, tradeParams, extraText);
            return (true, tradeParams);
        }


        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Plaats een order op Binance
        //BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)
        using BinanceRestClient client = new();

        // Een OCO is ietjes afwijkend ten opzichte van de standaard
        if (orderType == CryptoOrderType.Oco)
        {
            WebCallResult<BinanceOrderOcoList> result;
            result = await client.SpotApi.Trading.PlaceOcoOrderAsync(symbol.Name, side,
                quantity, price: price, (decimal)stop, limit, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);

            if (!result.Success)
            {
                DumpError(symbol, orderType, orderSide, quantity, price, stop, limit, extraText,
                    result.ResponseStatusCode.ToString(), result.Error.ToString());
            }
            else if (result.Data != null)
            {
                // https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
                // De 1e order is de stop loss (te herkennen aan de "type": "STOP_LOSS")
                // De 2e order is de normale sell (te herkennen aan de "type": "LIMIT_MAKER")
                // De ene order heeft een price/stopprice, de andere enkel een price (combi)
                BinancePlacedOcoOrder order1 = result.Data.OrderReports.First();
                BinancePlacedOcoOrder order2 = result.Data.OrderReports.Last();
                tradeParams.CreateTime = result.Data.TransactionTime; // order1.CreateTime;
                tradeParams.OrderId = order1.Id;
                tradeParams.Order2Id = order2.Id; // Een 2e ordernummer (welke eigenlijk?)
                return (true, tradeParams);
            }
        }
        else
        {
            // Elk ordertype heeft andere opties, uitschrijven is het duidelijkste

            WebCallResult<BinancePlacedOrder> result;
            if (orderType == CryptoOrderType.Market)
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, side,
                    SpotOrderType.Market, quantity);
            else if (orderType == CryptoOrderType.StopLimit)
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, side,
                    SpotOrderType.StopLossLimit, quantity, price: price, stopPrice: stop, timeInForce: TimeInForce.GoodTillCanceled);
            else
                result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, side,
                    SpotOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);

            if (!result.Success)
            {
                DumpError(symbol, orderType, orderSide, quantity, price, stop, limit, extraText,
                    result.ResponseStatusCode.ToString(), result.Error.ToString());
            } 
            else if (result.Data != null)
            {
                // Vraag: waarom zijn de price en quantity niet gevuld in de result bij een StopLimit? 
                tradeParams.CreateTime = result.Data.CreateTime;
                tradeParams.OrderId = result.Data.Id;
                DumpOrder(symbol, tradeParams, extraText);
                return (true, tradeParams);
            }
        }

        return (false, tradeParams);
    }


    public static async Task<WebCallResult<BinanceOrderBase>> Cancel(CryptoTradeAccount tradeAccount,
        CryptoSymbol symbol, long? orderId)
    {
        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
        {
            //TradeParams tradeParams = new();
            //tradeParams.CreateTime = CurrentDate;
            //tradeParams.IsBuy = false;
            //tradeParams.OrderId = 0; // result.Data.Id;
            //tradeParams.StopPrice = stop; // order2.StopPrice;
            //tradeParams.LimitPrice = limit; // order2.  Hey, waarom is deze er niet?
            //if (price == null)
            //    tradeParams.Price = (decimal)symbol.LastPrice;
            //else
            //    tradeParams.Price = (decimal)price;
            //tradeParams.Quantity = (decimal)quantity;
            //tradeParams.QuoteQuantity = tradeParams.Price * tradeParams.Quantity;
            //return (true, tradeParams);

            // todo, deze tekst ook verderop plaatsen!
            //string text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} CANCEL price={4} stop={5} quantity={6} quotequantity={7}",
            //    symbol.Name, 
            //    orderId);
            //GlobalData.AddTextToLogTab(text2);
            //GlobalData.AddTextToTelegram(text2);

            return null; // what?
        }

        // BinanceWeights.WaitForFairBinanceWeight(1); flauwekul

        // Annuleer een order 
        if (orderId.HasValue)
        {
            using var client = new BinanceRestClient();
            WebCallResult<BinanceOrderBase> result = await client.SpotApi.Trading.CancelOrderAsync(symbol.Name, orderId);
            if (!result.Success)
            {
                string text = string.Format("{0} ERROR cancel order {1} {2}", symbol.Name, result.ResponseStatusCode, result.Error);
                GlobalData.AddTextToLogTab(text);
                GlobalData.AddTextToTelegram(text);
            }
            return result;

        }
        return null;
    }

    static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BinanceBalance> balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            foreach (var assetInfo in balances)
            {
                if (assetInfo.Total > 0)
                {
                    if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                    {
                        asset = new CryptoAsset()
                        {
                            Quote = assetInfo.Asset,
                        };
                        tradeAccount.AssetList.Add(asset.Quote, asset);
                    }
                    asset.Free = assetInfo.Available;
                    asset.Total = assetInfo.Total;
                    asset.Locked = assetInfo.Locked;

                    if (asset.Total == 0)
                        tradeAccount.AssetList.Remove(asset.Quote);
                }
            }
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
    }

    static public void PickupAssets(CryptoTradeAccount tradeAccount, IEnumerable<BinanceStreamBalance> balances)
    {
        tradeAccount.AssetListSemaphore.Wait();
        {
            try
            {
                foreach (var assetInfo in balances)
                {
                    if (!tradeAccount.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset asset))
                    {
                        asset = new CryptoAsset()
                        {
                            Quote = assetInfo.Asset,
                        };
                        tradeAccount.AssetList.Add(asset.Quote, asset);
                    }
                    asset.Free = assetInfo.Available;
                    asset.Total = assetInfo.Total;
                    asset.Locked = assetInfo.Locked;

                    if (asset.Total == 0)
                        tradeAccount.AssetList.Remove(asset.Quote);
                }
            }
            finally
            {
                tradeAccount.AssetListSemaphore.Release();
            }
        }
    }


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceTrade item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.Id;
        trade.OrderId = item.OrderId;
        //trade.OrderListId = (long)item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#1trade QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;

        trade.TradeTime = item.Timestamp;

        if (item.IsBuyer)
            trade.Side = CryptoOrderSide.Buy;
        else
            trade.Side = CryptoOrderSide.Sell;
    }


    static public void PickupTrade(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceStreamOrderUpdate item)
    {
        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.TradeId;
        trade.OrderId = item.Id;
        //trade.OrderListId = item.OrderListId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        // enig debug werk, soms wordt het niet ingevuld!
        //if (item.QuoteQuantity == 0)
        //    GlobalData.AddTextToLogTab(string.Format("{0} PickupTrade#2stream QuoteQuantity is 0 for order TradeId={1}!", symbol.Name, trade.TradeId));

        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;

        trade.TradeTime = item.EventTime;

        if (item.Side == OrderSide.Buy)
            trade.Side = CryptoOrderSide.Buy;
        else
            trade.Side = CryptoOrderSide.Sell;
    }


    public override async Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        await BinanceFetchTrades.FetchTradesForSymbol(tradeAccount, symbol);
    }

    public async override Task FetchAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        {
            try
            {
                GlobalData.AddTextToLogTab("Reading asset information from Binance");

                LimitRates.WaitForFairWeight(1);

                using var client = new BinanceRestClient();
                {
                    WebCallResult<BinanceAccountInfo> accountInfo = await client.SpotApi.Account.GetAccountInfoAsync();

                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo == null | accountInfo.Data == null)
                        throw new ExchangeException("Geen account data ontvangen");

                    try
                    {
                        Api.PickupAssets(tradeAccount, accountInfo.Data.Balances);
                        GlobalData.AssetsHaveChanged("");
                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString());
                        throw;
                    }
                }
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }
    }


#endif

}