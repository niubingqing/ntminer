﻿using NTMiner.MinerServer;
using System.Collections.Generic;

namespace NTMiner.Core {
    public interface ICalcConfigSet : IEnumerable<CalcConfigData> {
        bool TryGetCalcConfig(ICoin coin, out ICalcConfig calcConfig);
        IncomePerDay GetIncomePerHashPerDay(string coinCode);
        void SaveCalcConfigs(List<CalcConfigData> data);
    }
}
