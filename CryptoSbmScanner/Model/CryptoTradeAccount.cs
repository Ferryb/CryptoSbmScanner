﻿using CryptoSbmScanner.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("TradeAccount")]
public class CryptoTradeAccount
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Short { get; set; }
    public CryptoTradeAccountType AccountType { get; set; }

    // Instellingen:
    // Eventueel aangepaste instellingen per accountType voor bijvoorbeeld backtest project?
    //[Computed]
    // public ConfigWhatever Config { get; set; } 

    // Assets + locking
    [Computed]
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    [Computed]
    public SortedList<string, CryptoAsset> AssetList { get; } = new();

    // Trades + locking
    [Computed]
    public SemaphoreSlim TradeListSemaphore { get; set; } = new(1);
    [Computed]
    public SortedList<string, SortedList<int, CryptoTrade>> TradeList { get; set; } = new();

    // Posities + locking
    [Computed]
    public SemaphoreSlim PositionListSemaphore { get; set; } = new(1);
    [Computed]
    public SortedList<string, SortedList<int, CryptoPosition>> PositionList { get; } = new();
}