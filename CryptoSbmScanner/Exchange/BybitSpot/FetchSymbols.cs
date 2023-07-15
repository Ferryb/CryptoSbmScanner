﻿using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Exchange.BybitSpot;

public class FetchSymbols
{

    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading symbol information from {Api.ExchangeName}");
                LimitRates.WaitForFairWeight(1);

                using CryptoDatabase database = new();
                database.Open();

                //WebCallResult<BybitSpotResponse> exchangeInfo = null;
                using var client = new BybitRestClient();
                var exchangeInfo = await client.V5Api.ExchangeData.GetSpotSymbolsAsync();
                //var exchangeInfo = await client.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Spot);

                // Zo af en toe komt er geen data of is de Data niet gezet.
                // De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                if (exchangeInfo == null)
                    throw new ExchangeException("Geen exchange data ontvangen (1)");
                if (!exchangeInfo.Success)
                    GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error + "\r\n");
                if (exchangeInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");
                



                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = new();
                    try
                    {
                        //BybitSpotSymbol
                        //WebCallResult<BybitResponse<BybitSpotSymbol>> x;
                        foreach (var symbolData in exchangeInfo.Data.List)
                        {
                            //if (coin != "")
                            {
                                //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                // https://api.bybit.com/v5/market/instruments-info?category=spot
                                /*
                                  "Data": {
                                    "List": [
                                      {
                                        "Name": "BTCUSDT",
                                        "BaseAsset": "BTC",
                                        "QuoteAsset": "USDT",
                                        "Status": 1,
                                        "MarginTading": 0,
                                        "Innovation": false,
                                        "LotSizeFilter": {
                                          "BasePrecision": 0.000001,
                                          "QuotePrecision": 0.00000001,
                                          "MinOrderQuantity": 0.000048,
                                          "MaxOrderQuantity": 71.73956243,
                                          "MinOrderValue": 1,
                                          "MaxOrderValue": 2000000
                                        },
                                        "PriceFilter": {
                                          "TickSize": 0.01
                                        }
                                      },
                                enzovoort..
                                */
                                //Eventueel symbol toevoegen
                                if (!exchange.SymbolListName.TryGetValue(symbolData.Name, out CryptoSymbol symbol))
                                {
                                    symbol = new()
                                    {
                                        Exchange = exchange,
                                        ExchangeId = exchange.Id,
                                        Name = symbolData.Name,
                                        Base = symbolData.BaseAsset,
                                        Quote = symbolData.QuoteAsset,
                                        Status = 1,
                                    };
                                }

                                //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                                //De te gebruiken precisie in prijzen
                                //symbol.BaseAssetPrecision = binanceSymbol.LotSizeFilter.BasePrecision.ToString().Length - 2;
                                //if (symbol.BaseAssetPrecision <= 0)
                                //    symbol.BaseAssetPrecision = 8;
                                //symbol.QuoteAssetPrecision = binanceSymbol.LotSizeFilter.QuotePrecision.ToString().Length - 2;
                                //if (symbol.QuoteAssetPrecision <= 0)
                                //    symbol.QuoteAssetPrecision = 8;
                                //symbol.MinNotional = binanceSymbol.MinNotional; // ????

                                //Minimale en maximale amount voor een order (in base amount)
                                symbol.QuantityMinimum = symbolData.LotSizeFilter.MinOrderQuantity;
                                symbol.QuantityMaximum = symbolData.LotSizeFilter.MaxOrderQuantity;
                                // Dit klopt niet, deze heeft wederom effect op de Clamp routine!
                                symbol.QuantityTickSize = symbolData.LotSizeFilter.MinOrderQuantity;

                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = niet aanwezig! binanceSymbol.PriceFilter.min;
                                //symbol.PriceMaximum = niet aanwezig! binanceSymbol.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = symbolData.PriceFilter.TickSize; // ? binanceSymbol.PriceFilter.TickSize;

                                symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                if (symbolData.Status == SymbolStatus.Trading)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

                                if (symbol.Id == 0)
                                {
#if !SQLDATABASE
                                        database.Connection.Insert(symbol, transaction);
#endif
                                    cache.Add(symbol);
                                }
                                else
                                    database.Connection.Update(symbol, transaction);
                            }
                        }
#if SQLDATABASE
                        database.BulkInsertSymbol(cache, transaction);
#endif
                        transaction.Commit();


                        // Bewaren voor debug werkzaamheden
                        {
                            string filename = GlobalData.GetBaseDir();
                            filename += $@"\{Api.ExchangeName}\";
                            Directory.CreateDirectory(filename);
                            filename += "symbols.json";

                            string text = JsonSerializer.Serialize(exchangeInfo, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                            //var accountFile = new FileInfo(filename);
                            File.WriteAllText(filename, text);
                        }



                        // De nieuwe symbols toevoegen aan de lijst
                        // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                        foreach (CryptoSymbol symbol in cache)
                        {
                            GlobalData.AddSymbol(symbol);
                        }

                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString());
                        transaction.Rollback();
                        throw;
                    }
                }

                exchange.LastTimeFetched = DateTime.UtcNow;
                database.Connection.Update(exchange);

            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}