﻿using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace CryptoSbmScanner.Excel;

public class ExcelPositionDump : ExcelBase
{
    CryptoPosition Position;
    readonly Dictionary<string, bool> OrderList = [];

    public void DumpParts()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Parts");
        ICell cell;

        // Er zijn 2 rijen met headers
        int row = 0;

        // Headers
        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "Order");
        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Create");
        WriteCell(sheet, columns++, row, "Close");
        WriteCell(sheet, columns++, row, "Type");
        WriteCell(sheet, columns++, row, "Status");
        WriteCell(sheet, columns++, row, "Trailing");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "Q.Filled");
        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Stop");
        WriteCell(sheet, columns++, row, "Limit");
        WriteCell(sheet, columns++, row, "Value");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "Asset");

        WriteCell(sheet, columns++, row, "");
        WriteCell(sheet, columns++, row, "");
        WriteCell(sheet, columns++, row, "Profit");
        WriteCell(sheet, columns++, row, "Percent");

        foreach (CryptoPositionPart part in Position.Parts.Values.ToList())
        {
            ++row;
            int column = 0;
            {
                cell = WriteCell(sheet, column++, row, part.Id);
                string text = part.Purpose + " " + part.PartNumber.ToString();
                if (part.ManualOrder)
                    text += " manual";
                cell = WriteCell(sheet, column++, row, text); // 0 = entry and >= 1 is dca
                cell = WriteCell(sheet, column++, row, part.Purpose.ToString());
                cell = WriteCell(sheet, column++, row, part.CreateTime.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                column++;
                if (part.CloseTime.HasValue)
                {
                    cell = WriteCell(sheet, column, row, (DateTime)part.CloseTime?.ToLocalTime());
                    cell.CellStyle = CellStyleDate;
                }

                //cell = WriteCell(sheet, column, row, part.Status.ToString());

                column = 8;
                if (part.Quantity != 0)
                {
                    cell = WriteCell(sheet, column, row, (double)part.Quantity);
                    cell.CellStyle = CellStyleDecimalNormal;
                }

                column = 14;
                if (part.Commission != 0)
                {
                    cell = WriteCell(sheet, column++, row, (double)part.Commission);
                    cell.CellStyle = CellStyleDecimalNormal;
                }
                else column++;


                if (part.Interval != null)
                    cell = WriteCell(sheet, column++, row, part.Interval.Name);
                else
                    cell = WriteCell(sheet, column++, row, Position.Interval.Name);

                if (part.EntryMethod == CryptoEntryOrProfitMethod.AfterNextSignal)
                    cell = WriteCell(sheet, column++, row, part.Strategy.ToString());
                else
                    cell = WriteCell(sheet, column++, row, "Fixed percentage");


                if (part.CloseTime.HasValue) // && part.Status == CryptoPositionStatus.Ready
                {
                    cell = WriteCell(sheet, column++, row, (double)part.Profit);
                    if (part.Profit >= 0)
                        cell.CellStyle = CellStyleDecimalGreen;
                    else
                        cell.CellStyle = CellStyleDecimalRed;


                    cell = WriteCell(sheet, column++, row, (double)part.Percentage);
                    if (part.Profit >= 0)
                        cell.CellStyle = CellStylePercentageGreen;
                    else
                        cell.CellStyle = CellStylePercentageRed;
                }

            }

            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                ++row;
                // Geannuleerde order of openstaande orders overslagen
                //if (step.Status == CryptoOrderStatus.Expired || step.Status == CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                //    continue;

                column = 0;

                if (step.OrderId != "")
                    OrderList.TryAdd(step.OrderId, false);

                cell = WriteCell(sheet, column++, row, step.Id);
                if (step.OrderId != "")
                    cell = WriteCell(sheet, column++, row, step.OrderId);
                else
                    cell = WriteCell(sheet, column++, row, "?");

                cell = WriteCell(sheet, column++, row, step.Side.ToString());
                if (step.Side == CryptoOrderSide.Buy)
                    cell.CellStyle = CellStyleStringGreen;
                else
                    cell.CellStyle = CellStyleStringRed;


                cell = WriteCell(sheet, column++, row, step.CreateTime.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                if (step.CloseTime.HasValue)
                {
                    cell = WriteCell(sheet, column++, row, (DateTime)step.CloseTime?.ToLocalTime());
                    cell.CellStyle = CellStyleDate;
                }
                else column++;

                cell = WriteCell(sheet, column++, row, step.OrderType.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, step.Status.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, step.Trailing.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)step.Quantity);
                cell.CellStyle = CellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)step.QuantityFilled);
                cell.CellStyle = CellStyleDecimalNormal;

                // wat is de werkelijke prijs (stopprice of normale price)?
                // Gekozen om dit ter plekke uit te rekenen (is tevens beter met market orders die over meerdere trades gaan)
                //cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled / (double)step.Quantity);
                cell = WriteCell(sheet, column++, row, (double)step.Price);
                cell.CellStyle = CellStyleDecimalNormal;

                if (step.StopPrice.HasValue)
                {
                    cell = WriteCell(sheet, column++, row, (double)step.StopPrice);
                    cell.CellStyle = CellStyleDecimalNormal;
                }
                else column++;

                if (step.StopLimitPrice.HasValue)
                {
                    cell = WriteCell(sheet, column++, row, (double)step.Quantity * (double)step.StopLimitPrice);
                    cell.CellStyle = CellStyleDecimalNormal;
                }
                else column++;


                if (step.QuoteQuantityFilled != 0)
                {
                    cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled);
                    cell.CellStyle = CellStyleDecimalNormal;
                }
                else column++;

                if (step.Commission != 0)
                {
                    cell = WriteCell(sheet, column++, row, (double)step.Commission);
                    cell.CellStyle = CellStyleDecimalNormal;
                }
                else column++;

                if (step.CommissionAsset != null)
                {
                    cell = WriteCell(sheet, column++, row, step.CommissionAsset);
                    cell.CellStyle = CellStyleDecimalNormal;
                }
                else column++;
            }

            ++row;
            ++row;
        }

        ++row;
        ++row;

        // Wat een zooi met die kolommen en rijen, denk dat het ondertussen niet meer overeenkomt met het originele ontwerp..
        //(continue kolommen toevoegen heeft zijn nadelen)

        int x = 16;
        WriteCell(sheet, x++, row, "BE");
        cell = WriteCell(sheet, x++, row, (double)Position.BreakEvenPrice);
        cell.CellStyle = CellStyleDecimalNormal;

        x = 16;
        ++row;
        WriteCell(sheet, x++, row, "LP");
        cell = WriteCell(sheet, x++, row, (double)Position.Symbol.LastPrice);
        cell.CellStyle = CellStyleDecimalNormal;

        ++row;
        ++row;

        if (Position.CloseTime.HasValue)
        {
            cell = WriteCell(sheet, 18, row, (double)Position.Profit);
            if (Position.Profit >= 0)
                cell.CellStyle = CellStyleStringGreen;
            else
                cell.CellStyle = CellStyleStringRed;

            cell = WriteCell(sheet, 19, row, (double)Position.Percentage);
            if (Position.Percentage >= 100)
                cell.CellStyle = CellStyleStringGreen;
            else
                cell.CellStyle = CellStyleStringRed;
        }


        columns = 19;
        AutoSize(sheet, columns);
    }


    public void DumpBreakEven()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("BE");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Create");
        WriteCell(sheet, columns++, row, "Closed");
        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "QuoteQuantity");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "BreakEven");
        WriteCell(sheet, columns++, row, "Percentage");

        decimal be;
        decimal firstValue = 0;
        decimal totalValue = 0;
        decimal totalQuantity = 0;
        decimal totalCommission = 0;
        foreach (CryptoPositionPart part in Position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                // Geannuleerde order of openstaande orders overslagen
                if (step.Status >= CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                    continue;

                ++row;
                int column = 0;
                int factor = 1;
                if (step.Side == CryptoOrderSide.Buy)
                    factor = -1;

                var cell = WriteCell(sheet, column++, row, step.Side.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, step.CreateTime.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                cell = WriteCell(sheet, column++, row, (DateTime)step.CloseTime?.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                cell = WriteCell(sheet, column++, row, (double)(step.QuoteQuantityFilled / step.Quantity)); // gem. prijs
                cell.CellStyle = CellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)step.Quantity);
                cell.CellStyle = CellStyleDecimalNormal;
                totalQuantity += factor * step.QuantityFilled;

                cell = WriteCell(sheet, column++, row, factor * (double)step.QuoteQuantityFilled);
                cell.CellStyle = CellStyleDecimalNormal;
                totalValue += factor * step.QuoteQuantityFilled;

                cell = WriteCell(sheet, column++, row, (double)step.Commission);
                cell.CellStyle = CellStyleDecimalNormal;

                totalCommission += step.Commission;

                be = 0;
                if (totalQuantity != 0)
                    be = (totalValue - totalCommission) / totalQuantity;
                cell = WriteCell(sheet, column++, row, (double)be);
                cell.CellStyle = CellStyleDecimalNormal;


                // Percentage
                if (firstValue == 0)
                    firstValue = be;
                decimal perc = (100 * be / firstValue) - 100;
                cell = WriteCell(sheet, column++, row, (double)perc);
                cell.CellStyle = CellStyleDecimalNormal;
            }
        }
        AutoSize(sheet, columns);
    }


    public void DumpTrades()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Trades");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "TradeId");
        WriteCell(sheet, columns++, row, "OrderId");

        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Time");

        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "QuoteQuantity");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "C. Asset");

        List<CryptoTrade> tradelist = Position.Symbol.TradeList.Values.ToList();
        tradelist.Sort((x, y) => x.TradeTime.CompareTo(y.TradeTime));
        foreach (CryptoTrade trade in tradelist)
        {
            if (!OrderList.ContainsKey(trade.OrderId))
                continue;

            ++row;
            int column = 0;

            var cell = WriteCell(sheet, column++, row, trade.Id);
            //cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, trade.TradeId);

            cell = WriteCell(sheet, column++, row, trade.OrderId);

            cell = WriteCell(sheet, column++, row, "?");

            cell = WriteCell(sheet, column++, row, trade.TradeTime.ToLocalTime());
            cell.CellStyle = CellStyleDate;

            cell = WriteCell(sheet, column++, row, (double)trade.Price);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)trade.Quantity);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)trade.QuoteQuantity);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)trade.Commission);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, trade.CommissionAsset);
            cell.CellStyle = CellStyleDecimalNormal;

            //if (OrderList.ContainsKey(trade.OrderId))
            //{
            //    cell = WriteCell(sheet, column++, row, "In orderlist");
            //    cell.CellStyle = CellStyleDecimalNormal;
            //}
        }


        AutoSize(sheet, columns);
    }

    private void DumpInformation()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Information");

        int row = 0;
        int column = 0;
        WriteCell(sheet, row, column++, "Positie");
        WriteCell(sheet, row, column++, "Exchange");
        WriteCell(sheet, row, column++, "Symbol");
        WriteCell(sheet, row, column++, "Price ticksize");
        WriteCell(sheet, row, column++, "Quantity ticksize");
        WriteCell(sheet, row, column++, "Direction");
        WriteCell(sheet, row, column++, "Geinvesteeerd");
        WriteCell(sheet, row, column++, "Geretourneerd");
        if (Position.Status == CryptoPositionStatus.Ready)
            WriteCell(sheet, row, column++, "Winst/Verlies");
        else
            WriteCell(sheet, row, column++, "Nu geinvesteerd");
        WriteCell(sheet, row, column++, "Totale commissie");
        WriteCell(sheet, row, column++, "Markt waarde");
        WriteCell(sheet, row, column++, "Markt percentage");
        WriteCell(sheet, row, column++, "Geopend");
        WriteCell(sheet, row, column++, "Gesloten");
        WriteCell(sheet, row, column++, "Status");
        WriteCell(sheet, row, column++, "Account");

        row++;
        column = 0;
        WriteCell(sheet, row, column++, Position.Id);
        WriteCell(sheet, row, column++, Position.Exchange.Name);

        WriteCell(sheet, row, column++, Position.Symbol.Name);
        var cell = WriteCell(sheet, row, column++, (double)Position.Symbol.PriceTickSize);
        cell.CellStyle = CellStyleDecimalNormal;

        cell = WriteCell(sheet, row, column++, (double)Position.Symbol.QuantityTickSize);
        cell.CellStyle = CellStyleDecimalNormal;

        WriteCell(sheet, row, column++, Position.SideText);
        
        cell = WriteCell(sheet, row, column++, (double)Position.Invested);
        cell.CellStyle = CellStyleDecimalNormal;
        
        cell = WriteCell(sheet, row, column++, (double)Position.Returned);
        cell.CellStyle = CellStyleDecimalNormal;

        decimal investedInTrades = Position.Invested - Position.Returned - Position.Commission;
        cell = WriteCell(sheet, row, column++, (double)investedInTrades);
        cell.CellStyle = CellStyleDecimalNormal;
        
        cell = WriteCell(sheet, row, column++, (double)(Position.Commission ));
        cell.CellStyle = CellStyleDecimalNormal;
        
        cell = WriteCell(sheet, row, column++, (double)Position.CurrentProfit());
        cell.CellStyle = CellStyleDecimalNormal;
        
        cell = WriteCell(sheet, row, column++, (double)Position.CurrentProfitPercentage());
        cell.CellStyle = CellStyleDecimalNormal;
        
        cell = WriteCell(sheet, row, column++, Position.CreateTime.ToLocalTime());
        cell.CellStyle = CellStyleDate;
        
        if (Position.CloseTime.HasValue)
        {
            cell =  WriteCell(sheet, row, column++, Position.CloseTime.Value.ToLocalTime());
            cell.CellStyle = CellStyleDate;
        }
        else WriteCell(sheet, row, column++, "");
        
        WriteCell(sheet, row, column++, Position.Status.ToString());
        WriteCell(sheet, row, column++, Position.TradeAccount.Name);

        AutoSize(sheet, 6);
    }


    //private void DumpSignals()
    //{
    //    HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Signals");

    //    int row = 0;

    //    // Headers
    //    int columns = 0;

    //    WriteCell(sheet, columns++, row, "Id");
    //    WriteCell(sheet, columns++, row, "Order");
    //    WriteCell(sheet, columns++, row, "Side");
    //    WriteCell(sheet, columns++, row, "Create");
    //    WriteCell(sheet, columns++, row, "Close");
    //    WriteCell(sheet, columns++, row, "Type");
    //    WriteCell(sheet, columns++, row, "Status");
    //    WriteCell(sheet, columns++, row, "Trailing");
    //    WriteCell(sheet, columns++, row, "Quantity");
    //    WriteCell(sheet, columns++, row, "Price");
    //    WriteCell(sheet, columns++, row, "StopLimit");
    //    WriteCell(sheet, columns++, row, "Value");
    //    WriteCell(sheet, columns++, row, "Commission");


    //    row++;
    //    WriteCell(sheet, 0, row, "TODO");

    //    //        // De signalen laden
    //    //#if SQLDATABASE
    //    //        string sql = "select top 50 * from signal order by id desc";
    //    //        //sql = string.Format("select top 50 * from signal where exchangeid={0} order by id desc", exchange.Id);
    //    //#else
    //    //        string sql = "select * from signal order by id desc limit 50";
    //    //        //sql = string.Format("select * from signal where exchangeid={0} order by id desc limit 50", exchange.Id);
    //    //#endif
    //    //        using var database = new CryptoDatabase();

    //    //        foreach (CryptoPosition position in databaseThread.Connection.Query<CryptoPosition>("select * from position " +
    //    //            "where CreateTime >= @fromDate and status=2", new { fromDate = DateTime.Today }))

    //    //            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql))
    //    //        {
    //    //            ?

    //    //            if (GlobalData.ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange exchange2))
    //    //            {
    //    //                signal.Exchange = exchange2;

    //    //                if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol symbol))
    //    //                {
    //    //                    signal.Symbol = symbol;

    //    //                    if (GlobalData.IntervalListId.TryGetValue((int)signal.IntervalId, out CryptoInterval interval))
    //    //                        signal.Interval = interval;

    //    //                    GlobalData.SignalQueue.Enqueue(signal);
    //    //                }
    //    //            }
    //    //        }
    //}

    public void ExportToExcel(CryptoPosition position)
    {
        Position = position;
        try
        {
            CreateBook(Position.Symbol.Name);
            CreateFormats();

            DumpParts();
            DumpBreakEven();
            DumpTrades();
            //DumpSignals();
            DumpInformation();

            StartExcell("Position", Position.Symbol.Name, Position.Exchange.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR postion dump " + error.ToString());
        }
    }
}
