﻿using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;
using System.Drawing;
using Microsoft.IdentityModel.Tokens;

namespace CryptoSbmScanner;

public partial class FrmSettings : Form
{
    private SettingsBasic settings;

    private readonly List<SettingsQuoteCoin> BaseCoinList = new();

    private readonly Dictionary<Control, CryptoInterval> AnalyzeInterval = new();
    private readonly Dictionary<Control, AlgorithmDefinition> AnalyzeDefinitionIndexLong = new();
    private readonly Dictionary<Control, AlgorithmDefinition> AnalyzeDefinitionIndexShort = new();

    private readonly Dictionary<Control, CryptoInterval> MonitorInterval = new();
    private readonly Dictionary<Control, AlgorithmDefinition> MonitorStrategyLong = new();
    private readonly Dictionary<Control, AlgorithmDefinition> MonitorStrategyShort = new();

    public FrmSettings()
    {
        InitializeComponent();

        toolTip1.SetToolTip(EditAnalyzeStobbOverbought, "Dit type signaal is een dubbele indicatie dat een munt overbought is en die bestaat uit:" +
            "\n-een candle die opent of sluit boven de bovenste bollingerband\n" +
            "-zowel de %d als %k van de stochastic zijn boven de 80\n" +
            "(dit kan een instapmoment zijn voor een short positie)");
        toolTip1.SetToolTip(EditAnalyzeStobbOversold, "Dit type signaal is een dubbele indicatie dat een munt oversold is en bestaat uit:\n" +
            "-een candle die opent of sluit onder de onderste bollingerbands\n" +
            "-zowel de % d als % k van de stochastic zijn onder de 20\n" +
            "(dit kan een instapmoment zijn voor een long positie).");

        toolTip1.SetToolTip(EditAnalyzeSbmOverbought, "Dit is een variatie op de stobb overbought signaal en bestaat uit:\n" +
            "-een stobb overbought signaal\n" +
            "-de ma200 onder de ma50 is\n" +
            "-de ma50 onder de ma20 is\n" +
            "-de psar op of boven de ma20\n" +
            "(dit kan een instapmoment zijn voor een short positie)");
        toolTip1.SetToolTip(EditAnalyzeSbmOversold, "Dit is een variatie op de stobb oversold signaal en bestaat uit:\n" +
            "-een stobb oversold signaal\n" +
            "-de ma200 boven de ma50 is\n" +
            "-de ma50 boven de ma20 is\n" +
            "-de psar op of onder de ma20\n" +
            "(dit kan een instapmoment zijn voor een long positie)");

        toolTip1.SetToolTip(EditAnalyzeCandleJumpUp, "Een signaal dat een munt een bepaald percentage naar boven \"spingt\" (info)");
        toolTip1.SetToolTip(EditAnalyzeCandleJumpDown, "Een signaal dat een munt een bepaald percentage naar beneden \"spingt\"(info)");
        toolTip1.SetToolTip(EditAnalysisCandleJumpPercentage, "Percentage dat de munt naar boven of beneden moet bewegen");


        // Stupid designer removes events (after moving, sick of it...)
        EditPlaySoundSbmSignal.Click += SetGrayed;
        EditPlaySoundStobbSignal.Click += SetGrayed;
        EditPlaySoundCandleJumpSignal.Click += SetGrayed;

        buttonReset.Click += ButtonReset_Click;
        buttonTestSpeech.Click += ButtonTestSpeech_Click;
        buttonFontDialog.Click += ButtonFontDialog_Click;


        buttonColorStobb.Click += ButtonColorStobb_Click;
        buttonColorSbm.Click += ButtonColorSbm_Click;
        buttonColorJump.Click += ButtonColorJump_Click;

        buttonSelectSoundStobbOverbought.Click += ButtonSelectSoundStobbOverbought_Click;
        buttonSelectSoundStobbOversold.Click += ButtonSelectSoundStobbOversold_Click;
        buttonSelectSoundSbmOverbought.Click += ButtonSelectSoundSbmOverbought_Click;
        buttonSelectSoundSbmOversold.Click += ButtonSelectSoundSbmOversold_Click;
        buttonSelectSoundCandleJumpUp.Click += ButtonSelectSoundCandleJumpUp_Click;
        buttonSelectSoundCandleJumpDown.Click += ButtonSelectSoundCandleJumpDown_Click;

        buttonPlaySoundStobbOverbought.Click += ButtonPlaySoundStobbOverbought_Click;
        buttonPlaySoundStobbOversold.Click += ButtonPlaySoundStobbOversold_Click;
        buttonPlaySoundSbmOverbought.Click += ButtonPlaySoundSbmOverbought_Click;
        buttonPlaySoundSbmOversold.Click += ButtonPlaySoundSbmOversold_Click;
        buttonPlaySoundCandleJumpUp.Click += ButtonPlaySoundCandleJumpUp_Click;
        buttonPlaySoundCandleJumpDown.Click += ButtonPlaySoundCandleJumpDown_Click;


        // analyze interval
        AnalyzeInterval.Add(EditAnalyzeInterval1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m]);
        AnalyzeInterval.Add(EditAnalyzeInterval2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m]);
        AnalyzeInterval.Add(EditAnalyzeInterval3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m]);
        AnalyzeInterval.Add(EditAnalyzeInterval5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]);
        AnalyzeInterval.Add(EditAnalyzeInterval10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m]);
        AnalyzeInterval.Add(EditAnalyzeInterval15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]);
        AnalyzeInterval.Add(EditAnalyzeInterval30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m]);
        AnalyzeInterval.Add(EditAnalyzeInterval1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h]);
        AnalyzeInterval.Add(EditAnalyzeInterval2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h]);
        AnalyzeInterval.Add(EditAnalyzeInterval4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h]);
        AnalyzeInterval.Add(EditAnalyzeInterval6h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval6h]);
        AnalyzeInterval.Add(EditAnalyzeInterval8h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval8h]);
        AnalyzeInterval.Add(EditAnalyzeInterval12h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval12h]);
        AnalyzeInterval.Add(EditAnalyzeInterval1d, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d]);

        // analyze long
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeCandleJumpUp, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Jump]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSbmOversold, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm1]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSbm2Oversold, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm2]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSbm3Oversold, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm3]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSbm4Oversold, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm4]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeStobbOversold, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Stobb]);

        AnalyzeDefinitionIndexLong.Add(EditAnalyzePriceCrossedSma20, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedSma20]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzePriceCrossedSma50, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedSma50]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzePriceCrossedEma20, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedEma20]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzePriceCrossedEma50, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedEma50]);

        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSlopeEma20TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeEma20]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSlopeEma50TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeEma50]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSlopeSma20TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeSma20]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSlopeSma50TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeSma50]);

        AnalyzeDefinitionIndexLong.Add(EditAnalyzeFluxOversold, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Flux]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeBullishEngulfing, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.BullishEngulfing]);

        // analyze short
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeCandleJumpDown, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Jump]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeSbmOverbought, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm1]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeSbm2Overbought, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm2]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeSbm3Overbought, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm3]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeStobbOverbought, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Stobb]);


        // monitor interval
        MonitorInterval.Add(EditMonitorInterval1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m]);
        MonitorInterval.Add(EditMonitorInterval2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m]);
        MonitorInterval.Add(EditMonitorInterval3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m]);
        MonitorInterval.Add(EditMonitorInterval5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]);
        MonitorInterval.Add(EditMonitorInterval10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m]);
        MonitorInterval.Add(EditMonitorInterval15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]);
        MonitorInterval.Add(EditMonitorInterval30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m]);
        MonitorInterval.Add(EditMonitorInterval1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h]);
        MonitorInterval.Add(EditMonitorInterval2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h]);
        MonitorInterval.Add(EditMonitorInterval4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h]);

        // monitor strategy long
        MonitorStrategyLong.Add(EditMonitorSbm1, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm1]);
        MonitorStrategyLong.Add(EditMonitorSbm2, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm2]);
        MonitorStrategyLong.Add(EditMonitorSbm3, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm3]);
        MonitorStrategyLong.Add(EditMonitorSbm4, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Sbm4]);
        MonitorStrategyLong.Add(EditMonitorStobb, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Stobb]);
        MonitorStrategyLong.Add(EditMonitorJump, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Jump]);

        MonitorStrategyLong.Add(EditBotPriceCrossingEma20, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedEma20]);
        MonitorStrategyLong.Add(EditBotPriceCrossingEma50, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedEma50]);
        MonitorStrategyLong.Add(EditBotPriceCrossingSma20, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedSma20]);
        MonitorStrategyLong.Add(EditBotPriceCrossingSma50, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.PriceCrossedSma50]);

        MonitorStrategyLong.Add(EditBotSlopeEma20TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeEma20]);
        MonitorStrategyLong.Add(EditBotSlopeEma50TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeEma50]);
        MonitorStrategyLong.Add(EditBotSlopeSma20TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeSma20]);
        MonitorStrategyLong.Add(EditBotSlopeSma50TurningPositive, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.SlopeSma50]);

        MonitorStrategyLong.Add(EditMonitorFluxOversold, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.Flux]);
        MonitorStrategyLong.Add(EditMonitorBullishEngulfing, TradingConfig.AlgorithmDefinitionIndex[SignalStrategy.BullishEngulfing]);

        // monitor strategy short
        // Geen?
        MonitorStrategyShort.Clear();
    }

    private static void SetCheckBoxFrom(Control control, object obj, List<string> text)
    {
        // probleem met de 5m en 15m, daarom geprefixed
        if (control is CheckBox checkBox)
        {
            if (obj is CryptoInterval interval)
                checkBox.Checked = text.Contains(interval.Name);
            if (obj is AlgorithmDefinition definition)
                checkBox.Checked = text.Contains(definition.Name);
        }
    }

    private static void GetValueFromCheckBox(Control control, object obj, List<string> text)
    {
        // probleem met de 5m en 15m, daarom geprefixed
        if (control is CheckBox checkBox && checkBox.Checked)
        {
            if (obj is CryptoInterval interval)
                text.Add(interval.Name);
            if (obj is AlgorithmDefinition definition)
                text.Add(definition.Name);
        }
    }

    private void SetGrayed(object sender, EventArgs e)
    {
        // Stobb
        EditSoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonSelectSoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;

        EditSoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonSelectSoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;

        // Sbm
        EditSoundFileSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonSelectSoundSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;

        EditSoundFileSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonSelectSoundSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;

        // Candle jump UP
        EditSoundFileCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonSelectSoundCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;

        // Candle jump Down
        EditSoundFileCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonSelectSoundCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
    }


    public void InitSettings(SettingsBasic settings)
    {
        this.settings = settings;

#if !TRADEBOT
        // Oppassen: Een tabPage.Visible=x doet helemaal niets
        // (dat was weer een onaangename WinForms verrassing)
        tabExtra.Parent = null;
        tabExtra2.Parent = null;
        settings.Bot.Active = false;
#endif

        // Deze worden na de overgang naar .net 7 regelmatig gereset naar 0
        // Benieuwd waarom dit gebeurd (het zijn er gelukkig niet zo veel)
        EditGlobalBuyVarying.Minimum = -0.5m;
        EditBarometer1hMinimal.Minimum = -100;
        EditBarometer15mBotMinimal.Minimum = -100;
        EditBarometer30mBotMinimal.Minimum = -100;
        EditBarometer01hBotMinimal.Minimum = -100;
        EditBarometer04hBotMinimal.Minimum = -100;
        EditBarometer24hBotMinimal.Minimum = -100;
        EditAnalysisMinChangePercentage.Minimum = -100;
        EditAnalysisMinEffectivePercentage.Minimum = -100;

        //EditSoundTradeNotification.Checked = settings.General.SoundTradeNotification;

        // ------------------------------------------------------------------------------
        // General
        // ------------------------------------------------------------------------------
        EditBlackTheming.Checked = settings.General.BlackTheming;
        EditTradingApp.SelectedIndex = (int)settings.General.TradingApp;
        EditDoubleClickAction.SelectedIndex = (int)settings.General.DoubleClickAction;
        EditTrendCalculationMethod.SelectedIndex = (int)settings.General.TrendCalculationMethod;
        EditSoundHeartBeatMinutes.Value = settings.General.SoundHeartBeatMinutes;
        EditGetCandleInterval.Value = settings.General.GetCandleInterval;

        EditShowFluxIndicator5m.Checked = settings.General.ShowFluxIndicator5m;
        EditHideTechnicalStuffSignals.Checked = settings.General.HideTechnicalStuffSignals;
        EditGlobalDataRemoveSignalAfterxCandles.Value = settings.General.RemoveSignalAfterxCandles;

        EditTelegramToken.Text = settings.Telegram.Token;
        EditTelegramChatId.Text = settings.Telegram.ChatId;
        EditSendSignalsToTelegram.Checked = settings.Telegram.SendSignalsToTelegram;


        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------

        int yPos = 40;
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.SymbolList.Count > 5)
                BaseCoinList.Add(new SettingsQuoteCoin(quoteData, yPos += 26, tabBasismunten.Controls));
            else
            {
                quoteData.FetchCandles = false;
                quoteData.CreateSignals = false;
            }
        }

        foreach (SettingsQuoteCoin x in BaseCoinList)
            x.SetControlValues();
        BaseCoinList[0].AddHeaderLabels(40, tabBasismunten.Controls);

        // ------------------------------------------------------------------------------
        // Signals
        // ------------------------------------------------------------------------------
        EditAnalysisMinChangePercentage.Value = (decimal)settings.Signal.AnalysisMinChangePercentage;
        EditAnalysisMaxChangePercentage.Value = (decimal)settings.Signal.AnalysisMaxChangePercentage;
        EditLogAnalysisMinMaxChangePercentage.Checked = settings.Signal.LogAnalysisMinMaxChangePercentage;

        EditAnalysisMinEffectivePercentage.Value = (decimal)settings.Signal.AnalysisMinEffectivePercentage;
        EditAnalysisMaxEffectivePercentage.Value = (decimal)settings.Signal.AnalysisMaxEffectivePercentage;
        EditLogAnalysisMinMaxEffectivePercentage.Checked = settings.Signal.LogAnalysisMinMaxEffectivePercentage;

        EditBarometer1hMinimal.Value = settings.Signal.Barometer1hMinimal;
        EditLogBarometerToLow.Checked = settings.Signal.LogBarometerToLow;

        EditSymbolMustExistsDays.Value = settings.Signal.SymbolMustExistsDays;
        EditLogSymbolMustExistsDays.Checked = settings.Signal.LogSymbolMustExistsDays;

        EditMinimumTickPercentage.Value = settings.Signal.MinimumTickPercentage;
        EditLogMinimumTickPercentage.Checked = settings.Signal.LogMinimumTickPercentage;
        EditShowInvalidSignals.Checked = settings.Signal.ShowInvalidSignals;

        foreach (var item in AnalyzeInterval)
            SetCheckBoxFrom(item.Key, item.Value, settings.Signal.Analyze.Interval);

        // ------------------------------------------------------------------------------
        // Signal types
        // ------------------------------------------------------------------------------

        foreach (var item in AnalyzeDefinitionIndexLong)
            SetCheckBoxFrom(item.Key, item.Value, settings.Signal.Analyze.Strategy[TradeDirection.Long]);
        foreach (var item in AnalyzeDefinitionIndexShort)
            SetCheckBoxFrom(item.Key, item.Value, settings.Signal.Analyze.Strategy[TradeDirection.Short]);

        // STOBB
        EditStobbBBMinPercentage.Value = (decimal)settings.Signal.StobbBBMinPercentage;
        EditStobbBBMaxPercentage.Value = (decimal)settings.Signal.StobbBBMaxPercentage;
        EditStobbUseLowHigh.Checked = settings.Signal.StobbUseLowHigh;
        EditPlaySoundStobbSignal.Checked = settings.Signal.PlaySoundStobbSignal;
        EditPlaySpeechStobbSignal.Checked = settings.Signal.PlaySpeechStobbSignal;
        EditSoundStobbOverbought.Text = settings.Signal.SoundStobbOverbought;
        EditSoundStobbOversold.Text = settings.Signal.SoundStobbOversold;
        EditStobIncludeRsi.Checked = settings.Signal.StobIncludeRsi;
        EditStobIncludeSbmMaLines.Checked = settings.Signal.StobIncludeSoftSbm;
        EditStobIncludeSbmPercAndCrossing.Checked = settings.Signal.StobIncludeSbmPercAndCrossing;
        panelColorStobb.BackColor = settings.Signal.ColorStobb;

        // SBM 1
        EditSbmBBMinPercentage.Value = (decimal)settings.Signal.SbmBBMinPercentage;
        EditSbmBBMaxPercentage.Value = (decimal)settings.Signal.SbmBBMaxPercentage;
        EditSbmUseLowHigh.Checked = settings.Signal.SbmUseLowHigh;
        EditPlaySoundSbmSignal.Checked = settings.Signal.PlaySoundSbmSignal;
        EditPlaySpeechSbmSignal.Checked = settings.Signal.PlaySpeechSbmSignal;
        EditSoundFileSbmOverbought.Text = settings.Signal.SoundSbmOverbought;
        EditSoundFileSbmOversold.Text = settings.Signal.SoundSbmOversold;
        panelColorSbm.BackColor = settings.Signal.ColorSbm;
        EditSbm1CandlesLookbackCount.Value = settings.Signal.Sbm1CandlesLookbackCount;

        // JUMP
        EditPlaySoundCandleJumpSignal.Checked = settings.Signal.PlaySoundCandleJumpSignal;
        EditPlaySpeechCandleJumpSignal.Checked = settings.Signal.PlaySpeechCandleJumpSignal;
        EditSoundFileCandleJumpDown.Text = settings.Signal.SoundCandleJumpDown;
        EditSoundFileCandleJumpUp.Text = settings.Signal.SoundCandleJumpUp;
        EditAnalysisCandleJumpPercentage.Value = settings.Signal.AnalysisCandleJumpPercentage;
        EditJumpCandlesLookbackCount.Value = settings.Signal.JumpCandlesLookbackCount;
        EditJumpUseLowHighCalculation.Checked = settings.Signal.JumpUseLowHighCalculation;
        panelColorJump.BackColor = settings.Signal.ColorJump;

        // SBM 2
        EditSbm2CandlesLookbackCount.Value = settings.Signal.Sbm2CandlesLookbackCount;
        EditSbm2BbPercentage.Value = settings.Signal.Sbm2BbPercentage;
        EditSbm2UseLowHigh.Checked = settings.Signal.Sbm2UseLowHigh;


        // SBM 3
        EditSbm3CandlesForBBRecovery.Value = settings.Signal.Sbm3CandlesLookbackCount;
        EditSbm3CandlesForBBRecoveryPercentage.Value = settings.Signal.Sbm3CandlesBbRecoveryPercentage;

        // SBM 4
        //EditSbm4Overbought.Checked = settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm4Overbought];
        //EditSbm4TemaPercent.Value = settings.Signal.Sbm4Percentage;
        //EditSbm4CandlesLookbackCount.Value = settings.Signal.Sbm4CandlesLookback;
        //EditSbm4BbPercent.Value = settings.Signal.Sbm4BbPercent;

        // SBM 5
        //EditSbm5Oversold.Checked = settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm5Oversold];
        //EditSbm5Overbought.Checked = settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm5Overbought];

        // SBM aanvullend
        EditSbmCandlesForMacdRecovery.Value = settings.Signal.SbmCandlesForMacdRecovery;

        EditSbmMa200AndMa50Percentage.Value = settings.Signal.SbmMa200AndMa50Percentage;
        EditSbmMa50AndMa20Percentage.Value = settings.Signal.SbmMa50AndMa20Percentage;
        EditSbmMa200AndMa20Percentage.Value = settings.Signal.SbmMa200AndMa20Percentage;

        EditSbmMa200AndMa20Crossing.Checked = settings.Signal.SbmMa200AndMa20Crossing;
        EditSbmMa200AndMa20Lookback.Value = settings.Signal.SbmMa200AndMa20Lookback;
        EditSbmMa200AndMa50Crossing.Checked = settings.Signal.SbmMa200AndMa50Crossing;
        EditSbmMa200AndMa50Lookback.Value = settings.Signal.SbmMa200AndMa50Lookback;
        EditSbmMa50AndMa20Crossing.Checked = settings.Signal.SbmMa50AndMa20Crossing;
        EditSbmMa50AndMa20Lookback.Value = settings.Signal.SbmMa50AndMa20Lookback;


        // --------------------------------------------------------------------------------
        // Extra instap condities
        // --------------------------------------------------------------------------------

        EditMinimumAboveBollingerBandsSma.Value = settings.Signal.AboveBollingerBandsSma;
        EditMinimumAboveBollingerBandsSmaCheck.Checked = settings.Signal.AboveBollingerBandsSmaCheck;

        EditMinimumAboveBollingerBandsUpper.Value = settings.Signal.AboveBollingerBandsUpper;
        EditMinimumAboveBollingerBandsUpperCheck.Checked = settings.Signal.AboveBollingerBandsUpperCheck;

        EditCandlesWithZeroVolume.Value = settings.Signal.CandlesWithZeroVolume;
        EditCandlesWithZeroVolumeCheck.Checked = settings.Signal.CandlesWithZeroVolumeCheck;

        EditCandlesWithFlatPrice.Value = settings.Signal.CandlesWithFlatPrice;
        EditCandlesWithFlatPriceCheck.Checked = settings.Signal.CandlesWithFlatPriceCheck;

        // ------------------------------------------------------------------------------
        // Trading
        // ------------------------------------------------------------------------------

        // slots
        EditSlotsMaximalExchange.Value = settings.Trading.SlotsMaximalExchange;
        EditSlotsMaximalSymbol.Value = settings.Trading.SlotsMaximalSymbol;
        EditSlotsMaximalBase.Value = settings.Trading.SlotsMaximalBase;

        // barometer
        EditBarometer15mBotMinimal.Value = settings.Trading.Barometer15mBotMinimal;
        EditBarometer30mBotMinimal.Value = settings.Trading.Barometer30mBotMinimal;
        EditBarometer01hBotMinimal.Value = settings.Trading.Barometer01hBotMinimal;
        EditBarometer04hBotMinimal.Value = settings.Trading.Barometer04hBotMinimal;
        EditBarometer24hBotMinimal.Value = settings.Trading.Barometer24hBotMinimal;

        // Buy
        EditBuyMethod.SelectedIndex = (int)settings.Trading.BuyMethod;
        EditGlobalBuyRemoveTime.Value = settings.Trading.GlobalBuyRemoveTime;
        EditGlobalBuyVarying.Value = settings.Trading.GlobalBuyVarying;

        // DCA
        EditDcaMethod.SelectedIndex = (int)settings.Trading.DcaMethod;
        EditDcaPercentage.Value = Math.Abs(settings.Trading.DcaPercentage);
        EditDcaFactor.Value = settings.Trading.DcaFactor;
        EditDcaCount.Value = settings.Trading.DcaCount;
        EditGlobalBuyCooldownTime.Value = settings.Trading.GlobalBuyCooldownTime;

        // take profit
        EditSellMethod.SelectedIndex = (int)settings.Trading.SellMethod;
        EditProfitPercentage.Value = settings.Trading.ProfitPercentage;
        EditLockProfits.Checked = settings.Trading.LockProfits;
        EditDynamicTp.Checked = settings.Trading.DynamicTp;
        EditDynamicTpPercentage.Value = settings.Trading.DynamicTpPercentage;

        // Stop loss
        EditGlobalStopPercentage.Value = Math.Abs(settings.Trading.GlobalStopPercentage);
        EditGlobalStopLimitPercentage.Value = Math.Abs(settings.Trading.GlobalStopLimitPercentage);


        foreach (var item in MonitorInterval)
            SetCheckBoxFrom(item.Key, item.Value, settings.Trading.Monitor.Interval);

        foreach (var item in MonitorStrategyLong)
            SetCheckBoxFrom(item.Key, item.Value, settings.Trading.Monitor.Strategy[TradeDirection.Long]);
        foreach (var item in MonitorStrategyShort)
            SetCheckBoxFrom(item.Key, item.Value, settings.Trading.Monitor.Strategy[TradeDirection.Short]);


        // Hoe gaan we traden
        EditDoNotEnterTrade.Checked = settings.Trading.DoNotEnterTrade;
        EditTradeViaBinance.Checked = settings.Trading.TradeViaBinance;
        EditTradeViaPaperTrading.Checked = settings.Trading.TradeViaPaperTrading;
        EditTradeViaAltradyWebhook.Checked = settings.Trading.TradeViaAltradyWebhook;


        // --------------------------------------------------------------------------------
        // Black & White list
        // --------------------------------------------------------------------------------
        textBoxBlackListOversold.Text = string.Join(Environment.NewLine, settings.BlackListOversold);
        textBoxWhiteListOversold.Text = string.Join(Environment.NewLine, settings.WhiteListOversold);
        textBoxBlackListOverbought.Text = string.Join(Environment.NewLine, settings.BlackListOverbought);
        textBoxWhiteListOverbought.Text = string.Join(Environment.NewLine, settings.WhiteListOverbought);


        // --------------------------------------------------------------------------------
        // Trade bot
        // --------------------------------------------------------------------------------

        //yPos = 40;
        //// todo, hoe en wat met de SignalStrategy en SignalMode
        //for (int i = 0; i < GlobalData.Settings.Bot.TradeOnStrategy.Length; i++)
        //{
        //    SignalLongStrategy strategy = (SignalLongStrategy)i;
        //    SignalUIList.Add(new SettingsSignalStrategy(strategy, yPos += 26, tabExtra2.Controls));
        //}

        //foreach (SettingsSignalStrategy x in SignalUIList)
        //    x.SetControlValues();
        //SignalUIList[0].AddHeaderLabels(40, tabExtra2.Controls);


        // ------------------------------------------------------------------------------
        // Balance bot
        // ------------------------------------------------------------------------------
        //EditBlanceBotActive.Checked = settings.BalanceBot.Active;
        //numericStartAmount.Value = settings.BalanceBot.StartAmount;
        //EditShowAdviceOnly.Checked = settings.BalanceBot.ShowAdviceOnly;
        //EditIntervalPeriod.Value = settings.BalanceBot.IntervalPeriod;
        //EditMinimalBuyBarometer.Value = settings.BalanceBot.MinimalBuyBarometer;
        //EditMinimalSellBarometer.Value = settings.BalanceBot.MinimalSellBarometer;
        //EditBuyBalanceThreshold.Value = settings.BalanceBot.BuyThresholdPercentage;
        //EditSellBalanceThreshold.Value = settings.BalanceBot.SellThresholdPercentage;


        // --------------------------------------------------------------------------------
        // Font (pas op het einde zodat de dynamisch gegenereerde controls netjes meesizen)
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.General.FontSize != Font.Size || GlobalData.Settings.General.FontName.Equals(Font.Name))
        {
            Font = new Font(GlobalData.Settings.General.FontName, GlobalData.Settings.General.FontSize,
                FontStyle.Regular, GraphicsUnit.Point, 0);
        }

        SetGrayed(null, null);
    }


    private void ButtonCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }


    private void ButtonOk_Click(object sender, EventArgs e)
    {
        //settings.General.SoundTradeNotification = EditSoundTradeNotification.Checked;

        // ------------------------------------------------------------------------------
        // General
        // ------------------------------------------------------------------------------
        settings.General.BlackTheming = EditBlackTheming.Checked;
        settings.General.TradingApp = (TradingApp)EditTradingApp.SelectedIndex;
        settings.General.DoubleClickAction = (DoubleClickAction)EditDoubleClickAction.SelectedIndex;
        settings.General.TrendCalculationMethod = (TrendCalculationMethod)EditTrendCalculationMethod.SelectedIndex;
        settings.General.SoundHeartBeatMinutes = (int)EditSoundHeartBeatMinutes.Value;
        settings.General.GetCandleInterval = (int)EditGetCandleInterval.Value;
        settings.General.FontName = Font.Name;
        settings.General.FontSize = Font.Size;

        settings.General.ShowFluxIndicator5m = EditShowFluxIndicator5m.Checked;
        settings.General.HideTechnicalStuffSignals = EditHideTechnicalStuffSignals.Checked;
        settings.General.RemoveSignalAfterxCandles = (int)EditGlobalDataRemoveSignalAfterxCandles.Value;


        settings.Telegram.Token = EditTelegramToken.Text.Trim();
        settings.Telegram.ChatId = EditTelegramChatId.Text.Trim();
        settings.Telegram.SendSignalsToTelegram = EditSendSignalsToTelegram.Checked;


        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------
        foreach (SettingsQuoteCoin x in BaseCoinList)
            x.GetControlValues();


        // ------------------------------------------------------------------------------
        // Signals
        // ------------------------------------------------------------------------------

        settings.Signal.AnalysisMinChangePercentage = (double)EditAnalysisMinChangePercentage.Value;
        settings.Signal.AnalysisMaxChangePercentage = (double)EditAnalysisMaxChangePercentage.Value;
        settings.Signal.LogAnalysisMinMaxChangePercentage = EditLogAnalysisMinMaxChangePercentage.Checked;

        settings.Signal.AnalysisMinEffectivePercentage = (double)EditAnalysisMinEffectivePercentage.Value;
        settings.Signal.AnalysisMaxEffectivePercentage = (double)EditAnalysisMaxEffectivePercentage.Value;
        settings.Signal.LogAnalysisMinMaxEffectivePercentage = EditLogAnalysisMinMaxEffectivePercentage.Checked;

        settings.Signal.Barometer1hMinimal = EditBarometer1hMinimal.Value;
        settings.Signal.LogBarometerToLow = EditLogBarometerToLow.Checked;

        settings.Signal.SymbolMustExistsDays = EditSymbolMustExistsDays.Value;
        settings.Signal.LogSymbolMustExistsDays = EditLogSymbolMustExistsDays.Checked;

        settings.Signal.MinimumTickPercentage = EditMinimumTickPercentage.Value;
        settings.Signal.LogMinimumTickPercentage = EditLogMinimumTickPercentage.Checked;
        settings.Signal.ShowInvalidSignals = EditShowInvalidSignals.Checked;

        settings.Signal.Analyze.Interval.Clear();
        foreach (var item in AnalyzeInterval)
            GetValueFromCheckBox(item.Key, item.Value, settings.Signal.Analyze.Interval);


        // ------------------------------------------------------------------------------
        // Signal types
        // ------------------------------------------------------------------------------

        // STOBB
        settings.Signal.StobbBBMinPercentage = (double)EditStobbBBMinPercentage.Value;
        settings.Signal.StobbBBMaxPercentage = (double)EditStobbBBMaxPercentage.Value;
        settings.Signal.PlaySoundStobbSignal = EditPlaySoundStobbSignal.Checked;
        settings.Signal.PlaySpeechStobbSignal = EditPlaySpeechStobbSignal.Checked;
        settings.Signal.SoundStobbOverbought = EditSoundStobbOverbought.Text;
        settings.Signal.SoundStobbOversold = EditSoundStobbOversold.Text;
        settings.Signal.StobIncludeRsi = EditStobIncludeRsi.Checked;
        settings.Signal.StobIncludeSoftSbm = EditStobIncludeSbmMaLines.Checked;
        settings.Signal.StobIncludeSbmPercAndCrossing = EditStobIncludeSbmPercAndCrossing.Checked;
        settings.Signal.ColorStobb = panelColorStobb.BackColor;

        // SBM x
        settings.Signal.SbmBBMinPercentage = (double)EditSbmBBMinPercentage.Value;
        settings.Signal.SbmBBMaxPercentage = (double)EditSbmBBMaxPercentage.Value;
        settings.Signal.SbmUseLowHigh = EditStobbUseLowHigh.Checked;

        // SBM 1
        settings.Signal.PlaySoundSbmSignal = EditPlaySoundSbmSignal.Checked;
        settings.Signal.PlaySpeechSbmSignal = EditPlaySpeechSbmSignal.Checked;
        settings.Signal.SoundSbmOverbought = EditSoundFileSbmOverbought.Text;
        settings.Signal.SoundSbmOversold = EditSoundFileSbmOversold.Text;
        settings.Signal.ColorSbm = panelColorSbm.BackColor;
        settings.Signal.Sbm1CandlesLookbackCount = (int)EditSbm1CandlesLookbackCount.Value;

        // SBM2
        settings.Signal.Sbm2CandlesLookbackCount = (int)EditSbm2CandlesLookbackCount.Value;
        settings.Signal.Sbm2BbPercentage = EditSbm2BbPercentage.Value;
        settings.Signal.Sbm2UseLowHigh = EditSbm2UseLowHigh.Checked;

        // SBM3
        settings.Signal.Sbm3CandlesLookbackCount = (int)EditSbm3CandlesForBBRecovery.Value;
        settings.Signal.Sbm3CandlesBbRecoveryPercentage = EditSbm3CandlesForBBRecoveryPercentage.Value;

        // SBM4
        //settings.Signal.AnalysisSbm4Overbought = EditSbm4Overbought.Checked;
        //settings.Signal.Sbm4Percentage = EditSbm4TemaPercent.Value;
        //settings.Signal.Sbm4CandlesLookback = (int)EditSbm4CandlesLookbackCount.Value;
        //settings.Signal.Sbm4BbPercent = EditSbm4BbPercent.Value;
        //settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm4Overbought] = false;
        //settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm4Oversold] = false;

        // SBM5
        //settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm5Overbought] = false;
        //settings.Signal.AnalyseStrategy[(int)SignalStrategy.sbm5Oversold] = false;

        // SBM aanvullend
        settings.Signal.SbmCandlesForMacdRecovery = (int)EditSbmCandlesForMacdRecovery.Value;

        settings.Signal.SbmMa200AndMa50Percentage = EditSbmMa200AndMa50Percentage.Value;
        settings.Signal.SbmMa50AndMa20Percentage = EditSbmMa50AndMa20Percentage.Value;
        settings.Signal.SbmMa200AndMa20Percentage = EditSbmMa200AndMa20Percentage.Value;

        settings.Signal.SbmMa200AndMa20Crossing = EditSbmMa200AndMa20Crossing.Checked;
        settings.Signal.SbmMa200AndMa20Lookback = (int)EditSbmMa200AndMa20Lookback.Value;
        settings.Signal.SbmMa200AndMa50Crossing = EditSbmMa200AndMa50Crossing.Checked;
        settings.Signal.SbmMa200AndMa50Lookback = (int)EditSbmMa200AndMa50Lookback.Value;
        settings.Signal.SbmMa50AndMa20Crossing = EditSbmMa50AndMa20Crossing.Checked;
        settings.Signal.SbmMa50AndMa20Lookback = (int)EditSbmMa50AndMa20Lookback.Value;

        // JUMP
        settings.Signal.PlaySoundCandleJumpSignal = EditPlaySoundCandleJumpSignal.Checked;
        settings.Signal.PlaySpeechCandleJumpSignal = EditPlaySpeechCandleJumpSignal.Checked;
        settings.Signal.SoundCandleJumpDown = EditSoundFileCandleJumpDown.Text;
        settings.Signal.SoundCandleJumpUp = EditSoundFileCandleJumpUp.Text;
        settings.Signal.AnalysisCandleJumpPercentage = EditAnalysisCandleJumpPercentage.Value;
        settings.Signal.JumpCandlesLookbackCount = (int)EditJumpCandlesLookbackCount.Value;
        settings.Signal.JumpUseLowHighCalculation = EditJumpUseLowHighCalculation.Checked;
        settings.Signal.ColorJump = panelColorJump.BackColor;


        settings.Signal.Analyze.Strategy[TradeDirection.Long].Clear();
        foreach (var item in AnalyzeDefinitionIndexLong)
            GetValueFromCheckBox(item.Key, item.Value, settings.Signal.Analyze.Strategy[TradeDirection.Long]);

        settings.Signal.Analyze.Strategy[TradeDirection.Short].Clear();
        foreach (var item in AnalyzeDefinitionIndexShort)
            GetValueFromCheckBox(item.Key, item.Value, settings.Signal.Analyze.Strategy[TradeDirection.Short]);


        // --------------------------------------------------------------------------------
        // Extra instap condities
        // --------------------------------------------------------------------------------
        settings.Signal.AboveBollingerBandsSma = (int)EditMinimumAboveBollingerBandsSma.Value;
        settings.Signal.AboveBollingerBandsSmaCheck = EditMinimumAboveBollingerBandsSmaCheck.Checked;

        settings.Signal.AboveBollingerBandsUpper = (int)EditMinimumAboveBollingerBandsUpper.Value;
        settings.Signal.AboveBollingerBandsUpperCheck = EditMinimumAboveBollingerBandsUpperCheck.Checked;

        settings.Signal.CandlesWithZeroVolume = (int)EditCandlesWithZeroVolume.Value;
        settings.Signal.CandlesWithZeroVolumeCheck = EditCandlesWithZeroVolumeCheck.Checked;

        settings.Signal.CandlesWithFlatPrice = (int)EditCandlesWithFlatPrice.Value;
        settings.Signal.CandlesWithFlatPriceCheck = EditCandlesWithFlatPriceCheck.Checked;


        // --------------------------------------------------------------------------------
        // Black & White list
        // --------------------------------------------------------------------------------

        settings.BlackListOversold = textBoxBlackListOversold.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOversold.Sort();

        settings.WhiteListOversold = textBoxWhiteListOversold.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOversold.Sort();

        settings.BlackListOverbought = textBoxBlackListOverbought.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOverbought.Sort();

        settings.WhiteListOverbought = textBoxWhiteListOverbought.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOverbought.Sort();

        // --------------------------------------------------------------------------------
        // Trade bot
        // --------------------------------------------------------------------------------

        // slots
        settings.Trading.SlotsMaximalExchange = (int)EditSlotsMaximalExchange.Value;
        settings.Trading.SlotsMaximalSymbol = (int)EditSlotsMaximalSymbol.Value;
        settings.Trading.SlotsMaximalBase = (int)EditSlotsMaximalBase.Value;

        // barometer
        settings.Trading.Barometer15mBotMinimal = EditBarometer15mBotMinimal.Value;
        settings.Trading.Barometer30mBotMinimal = EditBarometer30mBotMinimal.Value;
        settings.Trading.Barometer01hBotMinimal = EditBarometer01hBotMinimal.Value;
        settings.Trading.Barometer04hBotMinimal = EditBarometer04hBotMinimal.Value;
        settings.Trading.Barometer24hBotMinimal = EditBarometer24hBotMinimal.Value;

        // buy
        settings.Trading.BuyMethod = (BuyPriceMethod)EditBuyMethod.SelectedIndex;
        settings.Trading.GlobalBuyRemoveTime = (int)EditGlobalBuyRemoveTime.Value;
        settings.Trading.GlobalBuyVarying = EditGlobalBuyVarying.Value;

        // dca
        settings.Trading.DcaPercentage = EditDcaPercentage.Value;
        settings.Trading.DcaFactor = EditDcaFactor.Value;
        settings.Trading.DcaCount = (int)EditDcaCount.Value;
        settings.Trading.GlobalBuyCooldownTime = (int)EditGlobalBuyCooldownTime.Value;

        // take profit
        settings.Trading.ProfitPercentage = EditProfitPercentage.Value;
        settings.Trading.SellMethod = (BuyPriceMethod)EditSellMethod.SelectedIndex;
        settings.Trading.LockProfits = EditLockProfits.Checked;
        settings.Trading.DynamicTp = EditDynamicTp.Checked;
        settings.Trading.DynamicTpPercentage = EditDynamicTpPercentage.Value;

        // Stop loss
        settings.Trading.GlobalStopPercentage = EditGlobalStopPercentage.Value;
        settings.Trading.GlobalStopLimitPercentage = EditGlobalStopLimitPercentage.Value;



        settings.Trading.Monitor.Interval.Clear();
        foreach (var item in MonitorInterval)
            GetValueFromCheckBox(item.Key, item.Value, settings.Trading.Monitor.Interval);

        settings.Trading.Monitor.Strategy[TradeDirection.Long].Clear();
        foreach (var item in MonitorStrategyLong)
            GetValueFromCheckBox(item.Key, item.Value, settings.Trading.Monitor.Strategy[TradeDirection.Long]);

        settings.Trading.Monitor.Strategy[TradeDirection.Short].Clear();
        foreach (var item in MonitorStrategyShort)
            GetValueFromCheckBox(item.Key, item.Value, settings.Trading.Monitor.Strategy[TradeDirection.Short]);


        settings.Trading.DoNotEnterTrade = EditDoNotEnterTrade.Checked;
        settings.Trading.TradeViaBinance = EditTradeViaBinance.Checked;
        settings.Trading.TradeViaPaperTrading = EditTradeViaPaperTrading.Checked;
        settings.Trading.TradeViaAltradyWebhook = EditTradeViaAltradyWebhook.Checked;


        // ------------------------------------------------------------------------------
        // Balance bot
        // ------------------------------------------------------------------------------
        //settings.BalanceBot.Active = EditBlanceBotActive.Checked;
        //settings.BalanceBot.StartAmount = numericStartAmount.Value;
        //settings.BalanceBot.IntervalPeriod = (int)EditIntervalPeriod.Value;
        //settings.BalanceBot.ShowAdviceOnly = EditShowAdviceOnly.Checked;
        //settings.BalanceBot.MinimalBuyBarometer = EditMinimalBuyBarometer.Value;
        //settings.BalanceBot.MinimalSellBarometer = EditMinimalSellBarometer.Value;
        //settings.BalanceBot.BuyThresholdPercentage = EditBuyBalanceThreshold.Value;
        //settings.BalanceBot.SellThresholdPercentage = EditSellBalanceThreshold.Value;

        DialogResult = DialogResult.OK;
    }

    private void ButtonTestSpeech_Click(object sender, EventArgs e) => GlobalData.PlaySomeSpeech("Found a signal for BTC/BUSD interval 1m (it is going to the moon)", true);

    private static void BrowseForWavFile(ref TextBox textBox)
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "wav bestanden|*.wav"
        };
        if (!textBox.Text.IsNullOrEmpty())
            openFileDialog.FileName = Path.GetFileName(textBox.Text);
        if (!textBox.Text.IsNullOrEmpty())
            openFileDialog.InitialDirectory = Path.GetDirectoryName(textBox.Text);

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string fileName = openFileDialog.FileName;
            if (File.Exists(fileName))
            {
                textBox.Text = fileName;
            }
            else
            {
                MessageBox.Show("Selected file doesn't exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ButtonSelectSoundStobbOverbought_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundStobbOverbought);

    private void ButtonSelectSoundStobbOversold_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundStobbOversold);

    private void ButtonSelectSoundSbmOverbought_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileSbmOverbought);

    private void ButtonSelectSoundSbmOversold_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileSbmOversold);

    private void ButtonSelectSoundCandleJumpUp_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileCandleJumpUp);

    private void ButtonSelectSoundCandleJumpDown_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileCandleJumpDown);

    private void ButtonPlaySoundStobbOverbought_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundStobbOverbought.Text, true);

    private void ButtonPlaySoundStobbOversold_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundStobbOversold.Text, true);

    private void ButtonPlaySoundSbmOverbought_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileSbmOverbought.Text, true);

    private void ButtonPlaySoundSbmOversold_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileSbmOversold.Text, true);

    private void ButtonPlaySoundCandleJumpUp_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileCandleJumpUp.Text, true);

    private void ButtonPlaySoundCandleJumpDown_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileCandleJumpDown.Text, true);

    private static void PickColor(ref Panel panel)
    {
        ColorDialog dlg = new()
        {
            Color = panel.BackColor
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            panel.BackColor = dlg.Color;
        }
    }

    private void ButtonColorStobb_Click(object sender, EventArgs e)
        => PickColor(ref panelColorStobb);

    private void ButtonColorSbm_Click(object sender, EventArgs e)
        => PickColor(ref panelColorSbm);

    private void ButtonColorJump_Click(object sender, EventArgs e)
        => PickColor(ref panelColorJump);

    private void ButtonReset_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show("Alle instellingen resetten?", "Attentie!", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            GlobalData.Settings = new();
            GlobalData.DefaultSettings();
            InitSettings(GlobalData.Settings);
        }
    }

    private void ButtonFontDialog_Click(object sender, EventArgs e)
    {
        FontDialog dialog = new()
        {
            Font = Font
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            Font = dialog.Font;
        }

    }

    private void ButtonTestTelegram_Click(object sender, EventArgs e)
    {
        GlobalData.AddTextToTelegram(string.Format("{0} dit is een test Token='{1}' ChatId='{2}'",
            DateTime.Now.ToString(), GlobalData.Settings.Telegram.Token, GlobalData.Settings.Telegram.ChatId));
    }
}