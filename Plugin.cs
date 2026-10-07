// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 speakingOfLoom

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HeliAgilityCap
{
    [BepInPlugin("local.ravenfield.heliagilitycap", "Helicopter Agility Cap", "1.8.2")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        internal static ConfigEntry<float> MaxAngularVelocity;
        internal static ConfigEntry<string> Mode;
        internal static ConfigEntry<string> Names;

        // 穿越机翻滚速率曲线（Actual）
        internal static ConfigEntry<string> RateMode;
        internal static ConfigEntry<string> RateNames;
        internal static ConfigEntry<string> RollRates;
        internal static ConfigEntry<string> PitchRates;
        internal static ConfigEntry<string> YawRates;

        // 定距桨气动（杆量线性映射转速）
        internal static ConfigEntry<string> ThrustRpm;
        internal static ConfigEntry<string> ThrustRpmNames;
        internal static ConfigEntry<string> Vpitch;
        internal static ConfigEntry<string> Fmax;
        internal static ConfigEntry<string> RotorSpoolTime;
        internal static ConfigEntry<string> RotorForce;

        // 垂直杆量重映射
        internal static ConfigEntry<string> VerticalMode;

        // 原版物理字段覆盖
        internal static ConfigEntry<string> AerodynamicLift;
        internal static ConfigEntry<string> GroundEffectAcceleration;
        internal static ConfigEntry<string> Drag;

        // 按载具参数解析缓存
        internal static readonly FloatMapCache VpitchMap = new FloatMapCache("Vpitch");
        internal static readonly FloatMapCache FmaxMap = new FloatMapCache("Fmax");
        internal static readonly FloatMapCache SpoolMap = new FloatMapCache("RotorSpoolTime");
        internal static readonly FloatMapCache RotorForceMap = new FloatMapCache("RotorForce");
        internal static readonly FloatMapCache LiftMap = new FloatMapCache("AerodynamicLift");
        internal static readonly FloatMapCache GeaMap = new FloatMapCache("GroundEffectAcceleration");
        internal static readonly FloatMapCache DragMap = new FloatMapCache("Drag");

        private void Awake()
        {
            Log = Logger;

            MaxAngularVelocity = Config.Bind(
                "General", "MaxAngularVelocity", 10f,
                "直升机类载具角速度上限（rad/s）。原版=1.5（约 86°/s）    //给使用原版操控的载具用的");

            Mode = Config.Bind(
                "General", "Mode", "names",
                "生效范围：all=所有直升机；player=仅玩家驾驶的；names=载具名包含 Names 列表的。");

            Names = Config.Bind(
                "General", "Names", "UFO,FPV",
                "Mode=names 时生效，逗号分隔，不区分大小写。");

            RateMode = Config.Bind(
                "General", "RateMode", "on",
                "穿越机式姿态控制模式：off=不处理（杆量）；actual（或 on）=旋转改为\"杆量直接映射到目标角速度\"（Betaflight Actual 曲线），接管后此载具的 manouverability、m_AngularDrag和角速度 1.5 上限都不再起作用。");

            RateNames = Config.Bind(
                "General", "RateNames", "FPV",
                "RateMode 对哪些载具生效：载具名包含列表中任一词即可，逗号分隔、不区分大小写。留空 = 对所有直升机生效。");

            RollRates = Config.Bind(
                "General", "RollRates", "270,500,0.1",
                "翻滚轴 Actual 参数，格式：中心灵敏度(deg/s), 满杆速率(deg/s), expo(0~1)。");

            PitchRates = Config.Bind(
                "General", "PitchRates", "270,500,0.1",
                "俯仰轴 Actual 参数，格式同上。");

            YawRates = Config.Bind(
                "General", "YawRates", "270,490,0.0",
                "偏航轴 Actual 参数，格式同上。");

            ThrustRpm = Config.Bind(
                "General", "ThrustRpm", "on",
                "启用定距桨气动模型：off=不处理；on=杆量映射桨叶转速。");

            ThrustRpmNames = Config.Bind(
                "General", "ThrustRpmNames", "FPV",
                "定距桨气动模型对哪些载具生效：载具名包含列表中任一词即可，逗号分隔、不区分大小写。留空 = 对所有直升机生效。");

            Vpitch = Config.Bind(
                "General", "Vpitch", "FPV:80",
                "桨距速度（m/s，主要决定极速）：轴向速度达到该值时推力归零。格式：名称:值,名称:值,...——只有列出的载具单独设置；未列出（或写 -1）= 用默认 80。如 \"FPV:13,Mi-8:54\"。");

            Fmax = Config.Bind(
                "General", "Fmax", "FPV:1.25",
                "下降缓冲上限（未单独设置时用 1.25 = 下降时最多 +25% 推力）。格式同上。");

            RotorSpoolTime = Config.Bind(
                "General", "RotorSpoolTime", "FPV:0.05",
                "转速响应时间（秒；未单独设置时用 0.05 ≈ 打杆后 0.15 秒到 95%，0=瞬时）。格式同上。");

            RotorForce = Config.Bind(
                "General", "RotorForce", "FPV:100",
                "旋翼满转静态推力(覆盖原版载具属性)（m/s²，悬停转速=√(9.81/该值)，务必 > 9.81）：只有列出的载具生效（值必须 >0），如 \"FPV:125,Mi-8:150\"；未列出（或值 ≤0）= 用原版载具属性。");

            VerticalMode = Config.Bind(
                "General", "VerticalMode", "",
                "垂直杆量重映射（仅玩家、且当前载具在新气动名单内时生效）：留空（默认）=自动 full；off=不处理；clamp=负杆量按 0 计；full=整杆程映射为 0~1（杆到底=0）。填值即覆盖自动。");

            AerodynamicLift = Config.Bind(
                "General", "AerodynamicLift", "FPV:0.01",
                "aerodynamicLift(覆盖原版载具属性)（前飞升力系数：沿机身 up 施加 前飞速度×该值）。只有列出的载具生效，如 \"FPV:0.05,Mi-8:0.01\"（值 0 = 该车取消前飞升力）；未列出（或写 -1）= 所有载具都不动（用文件值）。");

            GroundEffectAcceleration = Config.Bind(
                "General", "GroundEffectAcceleration", "FPV:0",
                "groundEffectAcceleration(覆盖原版载具属性)（地效上限加速度，离地越近推力越强）。只有列出的载具生效，如 \"FPV:0\"（值 0 = 关地效）；未列出（或写 -1）= 所有载具都不动（用文件值）。");

            Drag = Config.Bind(
                "General", "Drag", "FPV:0.02",
                "线性空气阻力(覆盖原版载具属性)（线性空气阻力 1/s）。只有列出的载具生效，如 \"FPV:0.05,Mi-8:0.02\"；未列出（或写 -1）= 所有载具都不动（用文件值）。");

            // 立即写盘一次，把本版本的说明/默认值刷新到 cfg
            Config.Save();

            new Harmony("local.ravenfield.heliagilitycap").PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo("Helicopter Agility Cap 1.8.2 已加载。");
            Log.LogInfo("driverInput 反射: " + (DriverInputAccess.Ref != null ? "OK" : "失败（将使用兜底取值）"));
            Log.LogInfo("RateMode 配置: \"" + RateMode.Value + "\" → " + (RateModeEnabled() ? "启用" : "未启用"));
            Log.LogInfo("ThrustRpm 配置: \"" + ThrustRpm.Value + "\" → " + (ThrustRpmEnabled() ? "启用" : "未启用"));
            Log.LogInfo("VerticalMode 配置: \"" + VerticalMode.Value + "\" → " + (string.IsNullOrWhiteSpace(VerticalMode.Value) ? "自动（full）" : VerticalMode.Value.Trim()));
        }

        internal static bool ShouldApply(Helicopter heli)
        {
            switch (Mode.Value.Trim().ToLowerInvariant())
            {
                case "all":
                    return true;
                case "player":
                    return heli.HasDriver() && heli.HasPlayerDriver();
                case "names":
                    string vehicleName = heli.name ?? string.Empty;
                    foreach (string part in Names.Value.Split(','))
                    {
                        string token = part.Trim();
                        if (token.Length > 0 &&
                            vehicleName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        internal static bool RateModeEnabled()
        {
            string v = RateMode.Value.Trim().ToLowerInvariant();
            return v == "actual" || v == "on";
        }

        internal static bool RateModeAppliesTo(Vehicle vehicle)
        {
            return MatchesVehicleNameList(vehicle != null ? vehicle.name : null, RateNames.Value);
        }

        internal static bool ThrustRpmEnabled()
        {
            string v = ThrustRpm.Value.Trim().ToLowerInvariant();
            return v == "on" || v == "true";
        }

        internal static bool ThrustRpmAppliesTo(Vehicle vehicle)
        {
            return MatchesVehicleNameList(vehicle != null ? vehicle.name : null, ThrustRpmNames.Value);
        }

        // 名字包含列表中任一词即命中；列表留空 = 全部
        internal static bool MatchesVehicleNameList(string vehicleName, string list)
        {
            if (string.IsNullOrWhiteSpace(list))
                return true;

            vehicleName = vehicleName ?? string.Empty;
            foreach (string part in list.Split(','))
            {
                string token = part.Trim();
                if (token.Length > 0 &&
                    vehicleName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }

    // "名称:值,名称:值,..." 的按载具参数解析缓存：
    internal sealed class FloatMapCache
    {
        private readonly string keyName;
        private string raw;
        private string[] names = new string[0];
        private float[] values = new float[0];

        internal FloatMapCache(string keyName)
        {
            this.keyName = keyName;
        }

        internal float Resolve(string rawText, string vehicleName, float fallback)
        {
            if (rawText != raw)
                Rebuild(rawText);

            if (!string.IsNullOrEmpty(vehicleName))
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (vehicleName.IndexOf(names[i], StringComparison.OrdinalIgnoreCase) >= 0)
                        return values[i] < 0f ? fallback : values[i];
                }
            }
            return fallback;
        }

        private void Rebuild(string text)
        {
            raw = text;
            List<string> nameList = new List<string>();
            List<float> valueList = new List<float>();

            if (!string.IsNullOrEmpty(text))
            {
                foreach (string part in text.Split(','))
                {
                    string t = part.Trim();
                    if (t.Length == 0)
                        continue;

                    int colon = t.LastIndexOf(':');
                    if (colon > 0)
                    {
                        string nm = t.Substring(0, colon).Trim();
                        string vs = t.Substring(colon + 1).Trim();
                        if (nm.Length > 0 &&
                            float.TryParse(vs, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                        {
                            nameList.Add(nm);
                            valueList.Add(v);
                            continue;
                        }
                    }

                    Plugin.Log?.LogWarning(keyName + "：配置格式无法解析，已忽略该项：" + t);
                }
            }

            names = nameList.ToArray();
            values = valueList.ToArray();
        }
    }

    // driverInput 读取
    internal static class DriverInputAccess
    {
        internal static readonly AccessTools.FieldRef<Vehicle, Vector4> Ref = Create();

        private static AccessTools.FieldRef<Vehicle, Vector4> Create()
        {
            try
            {
                return AccessTools.FieldRefAccess<Vehicle, Vector4>("driverInput");
            }
            catch
            {
                return null;
            }
        }

        internal static Vector4 Get(Helicopter heli)
        {
            if (Ref != null)
            {
                try
                {
                    return Ref(heli);
                }
                catch
                {
                }
            }

            try
            {
                return Clamp(heli.Driver().controller.HelicopterInput()) * heli.engine.power;
            }
            catch
            {
                return Vector4.zero;
            }
        }

        private static Vector4 Clamp(Vector4 v)
        {
            return new Vector4(
                Mathf.Clamp(v.x, -1f, 1f),
                Mathf.Clamp(v.y, -1f, 1f),
                Mathf.Clamp(v.z, -1f, 1f),
                Mathf.Clamp(v.w, -1f, 1f));
        }
    }

    // 角速度上限：原逻辑 + 速率模式接管的车跳过（上限由速率补丁按曲线需求管理）
    [HarmonyPatch(typeof(Helicopter), "FixedUpdate")]
    internal static class HelicopterFixedUpdatePatch
    {
        private static void Postfix(Helicopter __instance)
        {
            if (__instance == null || __instance.rigidbody == null)
                return;
            if (Plugin.RateModeEnabled() && Plugin.RateModeAppliesTo(__instance))
                return;
            if (!Plugin.ShouldApply(__instance))
                return;

            __instance.rigidbody.maxAngularVelocity = Plugin.MaxAngularVelocity.Value;
        }
    }

    // 垂直杆量重映射（仅玩家、当前载具在新气动名单内）：
    [HarmonyPatch(typeof(FpsActorController), "HelicopterInput")]
    internal static class HelicopterInputVerticalPatch
    {
        private static void Postfix(FpsActorController __instance, ref Vector4 __result)
        {
            if (!Plugin.ThrustRpmEnabled())
                return;

            Vehicle vehicle = GetCurrentVehicle(__instance);
            if (vehicle == null || !Plugin.ThrustRpmAppliesTo(vehicle))
                return;

            string mode = Plugin.VerticalMode.Value.Trim().ToLowerInvariant();
            if (mode == "off")
                return;
            if (mode == "clamp")
            {
                __result.y = Mathf.Clamp(__result.y, 0f, 1f);
                return;
            }

            // 默认（留空/auto/full）
            float y = Mathf.Clamp(__result.y, -1f, 1f);
            __result.y = (y + 1f) * 0.5f;
        }

        private static Vehicle GetCurrentVehicle(FpsActorController controller)
        {
            Actor actor = controller == null ? null : controller.actor;
            if (actor == null || actor.seat == null)
                return null;
            return actor.seat.vehicle;
        }
    }

    // 物理字段覆盖（AerodynamicLift / GroundEffectAcceleration / Drag，均为"名称:值"列表、只有列出的载具生效）：
    [HarmonyPatch(typeof(Helicopter), "FixedUpdate")]
    internal static class HelicopterFieldOverridePatch
    {
        private static readonly HashSet<int> LiftLogged = new HashSet<int>();
        private static readonly HashSet<int> GeaLogged = new HashSet<int>();
        private static readonly HashSet<int> DragLogged = new HashSet<int>();

        private static void Prefix(Helicopter __instance)
        {
            if (__instance == null || __instance.rigidbody == null)
                return;
            string vn = __instance.name;

            float lift = Plugin.LiftMap.Resolve(Plugin.AerodynamicLift.Value, vn, -1f);
            if (lift >= 0f)
            {
                float old = __instance.aerodynamicLift;
                if (old != lift)
                    __instance.aerodynamicLift = lift;
                if (LiftLogged.Add(__instance.GetInstanceID()))
                    Plugin.Log?.LogInfo("aerodynamicLift 已覆盖：" + (vn ?? "?") + " = " + lift + "（文件值 " + old + "）");
            }

            float gea = Plugin.GeaMap.Resolve(Plugin.GroundEffectAcceleration.Value, vn, -1f);
            if (gea >= 0f)
            {
                float old = __instance.groundEffectAcceleration;
                if (old != gea)
                    __instance.groundEffectAcceleration = gea;
                if (GeaLogged.Add(__instance.GetInstanceID()))
                    Plugin.Log?.LogInfo("groundEffectAcceleration 已覆盖：" + (vn ?? "?") + " = " + gea + "（文件值 " + old + "）");
            }

            float drag = Plugin.DragMap.Resolve(Plugin.Drag.Value, vn, -1f);
            if (drag >= 0f)
            {
                float old = __instance.rigidbody.drag;
                if (old != drag)
                    __instance.rigidbody.drag = drag;
                if (DragLogged.Add(__instance.GetInstanceID()))
                    Plugin.Log?.LogInfo("Rigidbody.drag 已覆盖：" + (vn ?? "?") + " = " + drag + "（文件值 " + old + "）");
            }
        }
    }

    // 速率模式（穿越机式 Actual 曲线，逐轴参数）：
    [HarmonyPatch(typeof(Helicopter), "FixedUpdate")]
    internal static class HelicopterRateModePatch
    {
        // 每个载具只打一次"已生效"日志，便于排查
        private static readonly HashSet<int> AppliedLogged = new HashSet<int>();

        // 每个载具只打一次"名单不匹配"日志，便于排查
        private static readonly HashSet<int> NotMatchedLogged = new HashSet<int>();

        private static void Postfix(Helicopter __instance)
        {
            if (!Plugin.RateModeEnabled())
                return;
            if (__instance == null || __instance.rigidbody == null)
                return;
            if (__instance.dead || !__instance.HasDriver())
                return;
            if (!Plugin.RateModeAppliesTo(__instance))
            {
                if (NotMatchedLogged.Add(__instance.GetInstanceID()))
                    Plugin.Log?.LogInfo("RateMode 未生效（名单不匹配）：" + (__instance.name ?? "?") + "，RateNames=" + Plugin.RateNames.Value);
                return;
            }

            Vector4 input = DriverInputAccess.Get(__instance);

            ParseRateTriple(Plugin.RollRates.Value, 150f, 400f, 0.6f, out float rCs, out float rMr, out float rExpo);
            ParseRateTriple(Plugin.PitchRates.Value, 120f, 350f, 0.5f, out float pCs, out float pMr, out float pExpo);
            ParseRateTriple(Plugin.YawRates.Value, 100f, 300f, 0.3f, out float yCs, out float yMr, out float yExpo);

            float burnScale = __instance.burning ? __instance.controlWhenBurning : 1f;


            Vector3 localOmega = new Vector3(
                ActualRate(input.w, pCs * Mathf.Deg2Rad, pMr * Mathf.Deg2Rad, pExpo),
                ActualRate(input.x, yCs * Mathf.Deg2Rad, yMr * Mathf.Deg2Rad, yExpo),
                -ActualRate(input.z, rCs * Mathf.Deg2Rad, rMr * Mathf.Deg2Rad, rExpo)) * burnScale;

            float damp = 1f + __instance.rigidbody.angularDrag * Time.fixedDeltaTime;


            __instance.rigidbody.maxAngularVelocity = Mathf.Max(localOmega.magnitude * damp + 1f, 2f);

            __instance.manouverability = 0f;

            __instance.rigidbody.angularVelocity = __instance.transform.rotation * (localOmega * damp);

            if (AppliedLogged.Add(__instance.GetInstanceID()))
                Plugin.Log?.LogInfo("RateMode 已生效：" + (__instance.name ?? "?") +
                                    "（Roll=" + Plugin.RollRates.Value + " Pitch=" + Plugin.PitchRates.Value + " Yaw=" + Plugin.YawRates.Value + "）");
        }

        // "中心,满杆,expo" 三元组解析；格式不对的项回退默认值
        private static void ParseRateTriple(string text, float defaultCs, float defaultMr, float defaultExpo,
                                            out float cs, out float mr, out float expo)
        {
            cs = defaultCs;
            mr = defaultMr;
            expo = defaultExpo;

            if (string.IsNullOrWhiteSpace(text))
                return;

            string[] parts = text.Split(',');
            if (parts.Length < 3)
                return;

            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out cs))
                cs = defaultCs;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out mr))
                mr = defaultMr;
            if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out expo))
                expo = defaultExpo;
        }

        // Betaflight Actual 曲线：ω(x) = CS·x + (MR−CS)·h(x)，h = x·(x⁵·e + x·(1−e))
        // 输入 stick ∈ [-1,1]，cs/mr 单位为 rad/s，输出带符号角速度（rad/s）
        private static float ActualRate(float stick, float cs, float mr, float expo)
        {
            float x = Mathf.Abs(stick);
            if (x > 1f)
                x = 1f;

            float e = Mathf.Clamp01(expo);
            float h = x * (Mathf.Pow(x, 5f) * e + x * (1f - e));
            float rate = cs * x + (mr - cs) * h;
            return Mathf.Sign(stick) * rate;
        }
    }

    // 定距桨气动（杆量线性映射转速）：
    [HarmonyPatch(typeof(Helicopter), "FixedUpdate")]
    internal static class HelicopterThrustRpmPatch
    {
        private sealed class OmegaState
        {
            public float omega;
        }


        private static readonly ConditionalWeakTable<Helicopter, OmegaState> OmegaStates =
            new ConditionalWeakTable<Helicopter, OmegaState>();

        private static readonly HashSet<int> AppliedLogged = new HashSet<int>();
        private static readonly HashSet<int> NotMatchedLogged = new HashSet<int>();

        private static void Postfix(Helicopter __instance)
        {
            if (!Plugin.ThrustRpmEnabled())
                return;
            if (__instance == null || __instance.rigidbody == null)
                return;
            if (!Plugin.ThrustRpmAppliesTo(__instance))
            {
                if (NotMatchedLogged.Add(__instance.GetInstanceID()))
                    Plugin.Log?.LogInfo("ThrustRpm 未生效（名单不匹配）：" + (__instance.name ?? "?") + "，ThrustRpmNames=" + Plugin.ThrustRpmNames.Value);
                return;
            }

            OmegaState state = OmegaStates.GetOrCreateValue(__instance);

            if (__instance.dead || !__instance.HasDriver())
            {
                state.omega = 0f;
                return;
            }

            float dt = Time.fixedDeltaTime;
            Vector4 input = DriverInputAccess.Get(__instance);
            float yRaw = input.y;
            float yCmd = Mathf.Clamp01(yRaw);


            string vn = __instance.name;
            float vpitch = Plugin.VpitchMap.Resolve(Plugin.Vpitch.Value, vn, 80f);
            float fmax = Plugin.FmaxMap.Resolve(Plugin.Fmax.Value, vn, 1.25f);
            float spool = Plugin.SpoolMap.Resolve(Plugin.RotorSpoolTime.Value, vn, 0.05f);
            float rfFile = __instance.rotorForce;
            float rfCfg = Plugin.RotorForceMap.Resolve(Plugin.RotorForce.Value, vn, 0f);
            float rf = rfCfg > 0f ? rfCfg : rfFile;

            if (spool <= 0.0001f)
                state.omega = yCmd;
            else
                state.omega += (yCmd - state.omega) * Mathf.Min(1f, dt / spool);
            float omega = state.omega;

            Vector3 dir = (__instance.transform.up + __instance.transform.forward * 0.05f).normalized;
            Vector3 proj = Vector3.Project(dir, Vector3.up);
            dir = (dir - 0.05f * proj).normalized;

            float vAx = Vector3.Dot(__instance.rigidbody.velocity, dir);
            float f = Mathf.Clamp(1f - vAx / Mathf.Max(vpitch, 1f), 0f, fmax);


            Vector3 fGame = ComputeGameThrust(__instance, yRaw, dir);


            float burnScale = __instance.burning ? 0.3f : 1f;

            float geoEffect = ComputeGameGroundEffect(__instance, yRaw, dir);
            float tNew = rf * omega * omega * f * burnScale + geoEffect;
            Vector3 fNew = dir * tNew;

            // ---- 差值替换 ----
            __instance.rigidbody.AddForce(fNew - fGame, ForceMode.Acceleration);

            if (AppliedLogged.Add(__instance.GetInstanceID()))
                Plugin.Log?.LogInfo("ThrustRpm 已生效：" + (__instance.name ?? "?") +
                                    "（Vpitch=" + vpitch + " Fmax=" + fmax + " Spool=" + spool +
                                    " rotorForce=" + rf +
                                    (rfCfg > 0f ? "（配置；文件值 " + rfFile + " 已忽略）" : "（文件）") + "）");
        }


        private static Vector3 ComputeGameThrust(Helicopter heli, float yRaw, Vector3 dir)
        {
            float t = Mathf.Clamp01(-Vector3.Dot(dir, heli.rigidbody.velocity.normalized));
            float num = 1f + Mathf.Lerp(0f, heli.extraForceWhenStopping, t);
            float num2 = ComputeGameGroundEffect(heli, yRaw, dir) + yRaw * heli.rotorForce * num;
            float scale = heli.burning ? 0.3f : 1f;
            return dir * ((num2 - Physics.gravity.y - 0.5f) * scale);
        }


        private static float ComputeGameGroundEffect(Helicopter heli, float yRaw, Vector3 dir)
        {
            float g = Mathf.Clamp01(Mathf.Max(yRaw, 0f) * Vector3.Dot(dir, Vector3.up) * (20f - heli.altitude) * 0.1f);
            return g * heli.groundEffectAcceleration;
        }
    }
}
