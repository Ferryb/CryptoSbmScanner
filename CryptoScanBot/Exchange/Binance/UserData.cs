﻿using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange.Binance;

#if TRADEBOT
internal class UserData : UserDataBase
{
    static private UserDataStream TaskStreamUserData { get; set; }

    public override async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} user ticker starting");
        TaskStreamUserData = new UserDataStream();
        await Task.Run(async () => { await TaskStreamUserData.ExecuteAsync(); });
        ScannerLog.Logger.Trace($"{Api.ExchangeName} user ticker started");
    }


    public override async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} user ticker stopping");
        if (TaskStreamUserData != null)
            await TaskStreamUserData?.StopAsync();
        TaskStreamUserData = null;
        ScannerLog.Logger.Trace($"{Api.ExchangeName} user ticker stopped");
    }

    //public override void Reset()
    //{
    //    // empty
    //}


    //public override int Count()
    //{
    //    // empty
    //    return 0;
    //}

}
#endif
