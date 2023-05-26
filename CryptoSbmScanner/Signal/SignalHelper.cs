﻿using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


// Een oude enumeratie "SignalStrategy" is vervallen en overgenomen door de combinatie
//  van Mode en SignalStrategy (zonder de oversold/overbought toevoegingen)
public class AlgorithmDefinition
{
    public string Name { get; set; }
    public SignalStrategy Strategy { get; set; }
    public Type AnalyzeLongType { get; set; }
    public Type AnalyzeShortType { get; set; }

    public SignalCreateBase InstantiateAnalyzeLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        return (SignalCreateBase)Activator.CreateInstance(AnalyzeLongType, new object[] { symbol, interval, candle });
    }

    public SignalCreateBase InstantiateAnalyzeShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        return (SignalCreateBase)Activator.CreateInstance(AnalyzeShortType, new object[] { symbol, interval, candle });
    }

    //public SignalAcceptBase InstantiateSignalAccept(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    //{
    //    return (SignalAcceptBase)Activator.CreateInstance(SignalAcceptType, new object[] { symbol, interval, candle });
    //}
}

public class SignalHelper
{
    public static SignalCreateBase GetSignalAlgorithm(TradeDirection mode, SignalStrategy strategy, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        if (TradingConfig.AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition definition))
        {
            if (mode == TradeDirection.Long && definition.AnalyzeLongType != null)
                return definition.InstantiateAnalyzeLong(symbol, interval, candle);
            if (mode == TradeDirection.Short && definition.AnalyzeShortType != null)
                return definition.InstantiateAnalyzeShort(symbol, interval, candle);
        }
        return null;
    }

    public static string GetSignalAlgorithmText(SignalStrategy strategy)
    {
        if (TradingConfig.AlgorithmDefinitionIndex.TryGetValue(strategy, out AlgorithmDefinition definition))
            return definition.Name;
        return "";
    }

    ///
    /// Een lijst met alle mogelijke strategieën (en attributen)
    /// 
    public static List<AlgorithmDefinition> GetListOfAlgorithms()
    {
        List<AlgorithmDefinition> list = new();

        list.Add(new AlgorithmDefinition()
        {
            Name = "jump",
            Strategy = SignalStrategy.Jump,
            AnalyzeLongType = typeof(SignalCandleJumpDown),
            AnalyzeShortType = typeof(SignalCandleJumpUp),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "sbm1",
            Strategy = SignalStrategy.Sbm1,
            AnalyzeLongType = typeof(SignalSbm1Oversold),
            AnalyzeShortType = typeof(SignalSbm1Overbought),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "sbm2",
            Strategy = SignalStrategy.Sbm2,
            AnalyzeLongType = typeof(SignalSbm2Oversold),
            AnalyzeShortType = typeof(SignalSbm2Overbought),
        });


        list.Add(new AlgorithmDefinition()
        {
            Name = "sbm3",
            Strategy = SignalStrategy.Sbm3,
            AnalyzeLongType = typeof(SignalSbm3Oversold),
            AnalyzeShortType = typeof(SignalSbm3Overbought),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "sbm4",
            Strategy = SignalStrategy.Sbm4,
            AnalyzeLongType = typeof(SignalSbm4Oversold),
            AnalyzeShortType = null,
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "stobb",
            Strategy = SignalStrategy.Stobb,
            AnalyzeLongType = typeof(SignalStobbOversold),
            AnalyzeShortType = typeof(SignalStobbOverbought),
        });

#if TRADEBOT
        // Experimenteel (kan wellicht weg)
        list.Add(new AlgorithmDefinition()
        {
            Name = "close>ema20",
            Strategy = SignalStrategy.PriceCrossedEma20,
            AnalyzeLongType = typeof(SignalPriceCrossedEma20),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "close>ema50",
            Strategy = SignalStrategy.PriceCrossedEma50,
            AnalyzeLongType = typeof(SignalPriceCrossedEma50),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "close>sma20",
            Strategy = SignalStrategy.PriceCrossedSma20,
            AnalyzeLongType = typeof(SignalPriceCrossedSma20),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "close>sma50",
            Strategy = SignalStrategy.PriceCrossedSma50,
            AnalyzeLongType = typeof(SignalPriceCrossedSma50),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "sma 50 slope",
            Strategy = SignalStrategy.SlopeSma50,
            AnalyzeLongType = typeof(SignalSlopeSma50TurningPositive),
            AnalyzeShortType = typeof(SignalSlopeSma50TurningNegative),
        });


        list.Add(new AlgorithmDefinition()
        {
            Name = "ema 50 slope",
            Strategy = SignalStrategy.SlopeEma50,
            AnalyzeLongType = typeof(SignalSlopeEma50TurningPositive),
            AnalyzeShortType = typeof(SignalSlopeEma50TurningNegative),
        });


        list.Add(new AlgorithmDefinition()
        {
            Name = "ema 20 slope",
            Strategy = SignalStrategy.SlopeEma20,
            AnalyzeLongType = typeof(SignalSlopeEma20TurningPositive),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "sma 20 slope",
            Strategy = SignalStrategy.SlopeSma20,
            AnalyzeLongType = typeof(SignalSlopeSma20TurningPositive),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "flux",
            Strategy = SignalStrategy.Flux,
            AnalyzeLongType = typeof(SignalFluxOversold),
        });

        list.Add(new AlgorithmDefinition()
        {
            Name = "Engulfing",
            Strategy = SignalStrategy.BullishEngulfing,
            AnalyzeLongType = typeof(SignalBullishEngulfing),
        });

        

#endif
        return list;
    }

}