﻿using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Spot;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace CryptoSbmScanner.Exchange.Kucoin;


public class Api: ExchangeBase
{
    public static readonly string ExchangeName = "Kucoin";

    public Api() : base()
    {
    }

    public override void ExchangeDefaults()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} defaults");

        // Ik begrijp hier niet zoveel van.....
        //var logFactory = new LoggerFactory();
        //logFactory.AddProvider(new ConsoleLoggerProvider());
        //var binanceClient = new KucoinRestClient(new HttpClient(), logFactory, options => { });

        //var KucoinRestClient = new KucoinRestClient(null, factory, opts =>
        //{
        //    // set options
        //});



        // Default opties voor deze exchange
        KucoinRestClient.SetDefaultOptions(options =>
        {
            // type mismatch?
            //if (GlobalData.Settings.ApiKey != "")
            //    options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });

        KucoinSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            options.SocketSubscriptionsCombineTarget = 20;
            // type mismatch?
            //if (GlobalData.Settings.ApiKey != "")
            //    options.ApiCredentials = new ApiCredentials(GlobalData.Settings.ApiKey, GlobalData.Settings.ApiSecret);
        });

        ExchangeHelper.PriceTicker = new PriceTicker();
        ExchangeHelper.KLineTicker = new KLineTicker();
#if TRADEBOT
        //ExchangeHelper.UserData = new userData();
#endif
    }

    public async override Task FetchSymbolsAsync()
    {
        await FetchSymbols.ExecuteAsync();
    }

    public async override Task FetchCandlesAsync()
    {
        await FetchCandles.ExecuteAsync();
    }

#if TRADEBOT
    //// Converteer de orderstatus van Exchange naar "intern"
    //public static CryptoOrderType LocalOrderType(SpotOrderType orderType)
    //{
    //    CryptoOrderType localOrderType = orderType switch
    //    {
    //        SpotOrderType.Market => CryptoOrderType.Market,
    //        SpotOrderType.Limit => CryptoOrderType.Limit,
    //        SpotOrderType.StopLoss => CryptoOrderType.StopLimit,
    //        SpotOrderType.StopLossLimit => CryptoOrderType.Oco,
    //        _ => throw new Exception("Niet ondersteunde ordertype"),
    //    };

    //    return localOrderType;
    //}

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        CryptoOrderSide localOrderSide = orderSide switch
        {
            OrderSide.Buy => CryptoOrderSide.Buy,
            OrderSide.Sell => CryptoOrderSide.Sell,
            _ => throw new Exception("Niet ondersteunde orderside"),
        };

        return localOrderSide;
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.Active => CryptoOrderStatus.New,
            OrderStatus.Done => CryptoOrderStatus.Filled,
            //OrderStatus.New => CryptoOrderStatus.New,
            //OrderStatus.Filled => CryptoOrderStatus.Filled,
            //OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            //OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    public async Task<(bool result, TradeParams tradeParams)> BuyOrSell(CryptoDatabase database,
        CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit)
    {
        // Controleer de limiten van de maximum en minimum bedrag en de quantity
        if (!symbol.InsideBoundaries(quantity, price, out string text))
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} (debug={2} {3})", symbol.Name, text, price, quantity));
            return (false, null);
        }

        TradeParams tradeParams = new()
        {
            CreateTime = currentDate,
            OrderSide = orderSide,
            OrderType = orderType,
            Price = price,
            StopPrice = stop, // OCO - the price at which the limit order to sell is activated
            LimitPrice = limit, // OCO - the lowest price that the trader is willing to accept
            Quantity = quantity,
            QuoteQuantity = price * quantity,
            //OrderId = 0,
        };
        if (orderType == CryptoOrderType.StopLimit)
            tradeParams.QuoteQuantity = (decimal)tradeParams.StopPrice * tradeParams.Quantity;
        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
        {
            tradeParams.OrderId = database.CreateNewUniqueId();
            return (true, tradeParams);
        }


        OrderSide side;
        if (orderSide == CryptoOrderSide.Buy)
            side = OrderSide.Buy;
        else
            side = OrderSide.Sell;


        // Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        //BinanceWeights.WaitForFairBinanceWeight(1); flauwekul voor die ene tick (geen herhaling toch?)
        using KucoinRestClient client = new();

        WebCallResult<KucoinNewOrder> result;
        switch (orderType)
        {
            case CryptoOrderType.Market:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, side,
                        NewOrderType.Market, quantity);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.Id;
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.Limit:
                {
                    result = await client.SpotApi.Trading.PlaceOrderAsync(symbol.Name, side,
                    NewOrderType.Limit, quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                    if (!result.Success)
                    {
                        tradeParams.Error = result.Error;
                        tradeParams.ResponseStatusCode = result.ResponseStatusCode;
                    }
                    if (result.Success && result.Data != null)
                    {
                        tradeParams.CreateTime = currentDate;
                        tradeParams.OrderId = result.Data.Id;
                    }
                    return (result.Success, tradeParams);
                }
            case CryptoOrderType.StopLimit:
                {
                    // wordt het nu wel of niet ondersteund? Het zou ook een extra optie van de limit kunnen (zie wel een tp)
                    //result = await client.V5Api.Trading.PlaceOrderAsync(Category.Linear, symbol.Name, side, NewOrderType.Market,
                    //    quantity, price: price, timeInForce: TimeInForce.GoodTillCanceled);
                    throw new Exception("${orderType} not supported");
                }
            case CryptoOrderType.Oco:
                {
                    // Een OCO is afwijkend ten opzichte van een standaard buy or sell
                    //    Bij Binance was een OCO totaal afwijkend ten opzichte van een standaard buy or sell
                    //    het had ook andere parameters en results
                    //WebCallResult<BybitOrderOcoList> result;?????
                    //    throw new Exception("${orderType} not supported");
                    throw new Exception("${orderType} not supported");
                }
            default:
                throw new Exception("${orderType} not supported");
        }
    }

    public override async Task FetchTradesForSymbolAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        //await BinanceFetchTrades.FetchTradesForSymbol(tradeAccount, symbol);
    }


    public async override Task FetchAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        //    //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange exchange))
        //    {
        //        try
        //        {
        //            GlobalData.AddTextToLogTab("Reading asset information from Bybit");

        //            BybitWeights.WaitForFairWeight(1);

        //            using var client = new KucoinRestClient();
        //            {
        //                //https://openapi-sandbox.kucoin.com/api/v1/accounts

        //                var accountInfo = await client.SpotApi.Account.GetAccountAsync();

        //                if (!accountInfo.Success)
        //                {
        //                    GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
        //                }

        //                //Zo af en toe komt er geen data of is de Data niet gezet.
        //                //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
        //                if (accountInfo == null | accountInfo.Data == null)
        //                    throw new ExchangeException("Geen account data ontvangen");

        //                try
        //                {
        //                    PickupAssets(tradeAccount, accountInfo.Data.Assets);
        //                    GlobalData.AssetsHaveChanged("");
        //                }
        //                catch (Exception error)
        //                {
        //                    GlobalData.Logger.Error(error);
        //                    GlobalData.AddTextToLogTab(error.ToString());
        //                    throw;
        //                }
        //            }
        //        }
        //        catch (Exception error)
        //        {
        //            GlobalData.Logger.Error(error);
        //            GlobalData.AddTextToLogTab(error.ToString());
        //            GlobalData.AddTextToLogTab("");
        //        }

        //    }
    }

#endif

}
