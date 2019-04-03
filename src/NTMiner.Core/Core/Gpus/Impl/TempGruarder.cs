﻿using NTMiner.Core.Profiles;
using NTMiner.MinerClient;
using System;
using System.Collections.Generic;

namespace NTMiner.Core.Gpus.Impl {
    public class TempGruarder {
        public static readonly TempGruarder Instance = new TempGruarder();

        private TempGruarder() { }

        private bool _isInited = false;
        private Dictionary<int, uint> _temperatureDic = new Dictionary<int, uint>();
        private Dictionary<int, DateTime> _guardOn = new Dictionary<int, DateTime>();
        private readonly int _fanSpeedDownSeconds = 20;
        private readonly uint _fanSpeedDownStep = 2;
        private static readonly int _guardTemp = 60;
        public void Init(INTMinerRoot root) {
            if (_isInited) {
                return;
            }
            _isInited = true;
            VirtualRoot.On<GpuStateChangedEvent>("当显卡温度变更时守卫温度防线", LogEnum.None,
                action: message => {
                    IGpu gpu = message.Source;
                    if (gpu.Index == NTMinerRoot.GpuAllId || root.MinerProfile.CoinId == Guid.Empty) {
                        return;
                    }
                    IGpuProfile gpuProfile = GpuProfileSet.Instance.GetGpuProfile(root.MinerProfile.CoinId, gpu.Index);
                    if (!gpuProfile.IsAutoFanSpeed) {
                        gpuProfile = GpuProfileSet.Instance.GetGpuProfile(root.MinerProfile.CoinId, NTMinerRoot.GpuAllId);
                        if (!gpuProfile.IsAutoFanSpeed) {
                            return;
                        }
                    }
                    // 警戒时间
                    DateTime guardOn;
                    if (!_guardOn.TryGetValue(gpu.Index, out guardOn)) {
                        guardOn = DateTime.Now;
                        _guardOn.Add(gpu.Index, guardOn);
                    }
                    if (gpu.FanSpeed == 100) {
                        Write.DevDebug($"GPU{gpu.Index} 温度{gpu.Temperature}大于防线温度{_guardTemp}，但风扇转速已达100%");
                    }
                    else if (gpu.Temperature < _guardTemp) {
                        if (!_temperatureDic.ContainsKey(gpu.Index)) {
                            _temperatureDic.Add(gpu.Index, 0);
                        }
                        if (gpu.Temperature > _temperatureDic[gpu.Index]) {
                            _guardOn[gpu.Index] = DateTime.Now;
                        }
                        _temperatureDic[gpu.Index] = gpu.Temperature;
                        // 连续?分钟GPU温度没有突破防线
                        if (guardOn.AddSeconds(_fanSpeedDownSeconds) < DateTime.Now) {
                            int cool = (int)(gpu.FanSpeed - _fanSpeedDownStep);
                            if (gpu.Temperature < 50) {
                                cool = gpu.CoolMin;
                            }
                            if (cool >= gpu.CoolMin) {
                                _guardOn[gpu.Index] = DateTime.Now;
                                root.GpuSet.OverClock.SetCool(gpu.Index, cool);
                                Write.UserInfo($"GPU{gpu.Index} 风扇转速由{gpu.FanSpeed}%自动降至{cool}%");
                            }
                        }
                    }
                    else if (gpu.Temperature > _guardTemp) {
                        Write.UserInfo($"GPU{gpu.Index} 温度{gpu.Temperature}大于防线温度{_guardTemp}，自动增加风扇转速");
                        uint cool;
                        uint len;
                        // 防线突破可能是由于小量降低风扇转速造成的
                        if (guardOn.AddSeconds(_fanSpeedDownSeconds) < DateTime.Now) {
                            _guardOn[gpu.Index] = DateTime.Now;
                            len = 100 - gpu.FanSpeed;
                        }
                        else {
                            len = _fanSpeedDownStep;
                        }
                        cool = gpu.FanSpeed + (uint)Math.Ceiling(len / 2.0);
                        if (cool > 100) {
                            cool = 100;
                        }
                        if (cool <= 100) {
                            root.GpuSet.OverClock.SetCool(gpu.Index, (int)cool);
                            Write.UserInfo($"GPU{gpu.Index} 风扇转速由{gpu.FanSpeed}%自动增加至{cool}%");
                        }
                    }
                });
        }
    }
}
