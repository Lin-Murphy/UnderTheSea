using System;
using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 体验流程的所有阶段。6 大场景 + 过渡 + 待机(AR 现实)。
    /// 顺序即默认流程顺序。
    /// </summary>
    public enum Stage
    {
        Standby = 0,          // 待机(现实/AR,尚未进入)
        Enter,                // 场景1 进入场景
        ApplyGel,             // 场景2 涂耦合剂
        Probe,                // 场景3 探头检查
        DuskTransition,       // 过渡:天黑 + 水母飘来
        BreathHold,           // 场景4 吸气 / 憋气(保持状态)
        ExhaleTransition,     // 过渡:呼气,变天亮,水母飘走留一只
        Finale,               // 场景5 结尾交互(握手/拥抱)
        Exit,                 // 场景6 退出场景
        Finished              // 已回到现实,停留几秒后自动回到 Standby(Start 按钮)
    }

    public readonly struct StageInfo
    {
        public readonly string Zh;
        public readonly string En;
        public readonly int MainIndex;        // 1-6 为大场景,0 为待机/过渡
        public readonly float AutoAdvance;    // >0 表示过渡,自动进入下一阶段(秒)

        public StageInfo(string zh, string en, int mainIndex, float autoAdvance = 0f)
        {
            Zh = zh; En = en; MainIndex = mainIndex; AutoAdvance = autoAdvance;
        }

        public bool IsTransition => AutoAdvance > 0f;
    }

    /// <summary>
    /// 场景状态机。只负责"当前在哪个阶段",不关心输入来源,也不关心表现。
    /// 输入(键盘/手柄)调用 Next/Previous/Reset;表现层监听 StageChanged。
    /// </summary>
    public class ExperienceController : MonoBehaviour
    {
        [Tooltip("过渡:天黑 + 水母飘来 的时长(秒)")]
        public float duskDuration = 4f;
        [Tooltip("过渡:呼气、天亮、水母飘走 的时长(秒)")]
        public float exhaleDuration = 5f;
        [Tooltip("回到现实后,多少秒自动回到 Start 按钮状态")]
        public float finishedHold = 3f;

        public Stage Current { get; private set; } = Stage.Standby;
        public float TimeInStage { get; private set; }
        /// <summary>仅供编辑器自检:不进 Play 模式时直接设定阶段内时间。</summary>
        public void ForceStageTimeForVerification(float seconds) => TimeInStage = seconds;

        // ---------- 场景4 呼吸(按住右手柄扳机 = 吸气/憋气,松开 = 呼气)----------
        [Tooltip("吸气到最大 / 呼气回到正常 各需多少秒")]
        public float breathSeconds = 3f;
        [NonSerialized] public bool xrBreathHeld, keyboardBreathHeld, scriptBreathHeld;   // 各输入源各自写,互不覆盖
        public bool BreathHeld => xrBreathHeld || keyboardBreathHeld || scriptBreathHeld;
        /// <summary>呼吸程度 0..1:场景4 中按住时 3 秒升到 1,松开 3 秒回到 0;其它阶段回到 0。可反复使用。</summary>
        public float Breath { get; private set; }
        /// <summary>仅供编辑器自检:直接设定呼吸值。</summary>
        public void ForceBreathForVerification(float b) => Breath = Mathf.Clamp01(b);

        /// <summary>(旧阶段, 新阶段)</summary>
        public event Action<Stage, Stage> StageChanged;

        public StageInfo Info => GetInfo(Current);

        public StageInfo GetInfo(Stage s)
        {
            switch (s)
            {
                case Stage.Standby:          return new StageInfo("待机(现实)- 等待 Start", "Standby (AR) - waiting for Start", 0);
                case Stage.Finished:         return new StageInfo("已回到现实", "Finished (back to reality)", 0, finishedHold);
                case Stage.Enter:            return new StageInfo("进入场景", "Enter", 1);
                case Stage.ApplyGel:         return new StageInfo("涂耦合剂(小鱼游入珊瑚)", "Apply Gel - fish hide in me", 2);
                case Stage.Probe:            return new StageInfo("探头检查(海草跟随探头)", "Probe Scan - seaweed follows probe", 3);
                case Stage.DuskTransition:   return new StageInfo("过渡:天黑+水母游来", "Transition: Dusk + Jellyfish", 0, duskDuration);
                case Stage.BreathHold:       return new StageInfo("呼吸憋气(按住扳机吸气)", "Breath Hold", 4);
                // 2026-11-05:场景4 与场景5 合并 —— 助手按 A 后天亮,水母游走;不自动前进,再按 A 退出
                case Stage.ExhaleTransition: return new StageInfo("场景4·5:天亮,水母游走", "Dawn - jellies swim away", 5);
                case Stage.Finale:           return new StageInfo("(已合并到场景4·5,跳过)", "Finale (skipped)", 0);
                case Stage.Exit:             return new StageInfo("退出场景", "Exit", 6);
                default:                     return new StageInfo(s.ToString(), s.ToString(), 0);
            }
        }

        void Update()
        {
            TimeInStage += Time.deltaTime;
            bool inhale = Current == Stage.BreathHold && BreathHeld;
            Breath = Mathf.MoveTowards(Breath, inhale ? 1f : 0f, Time.deltaTime / Mathf.Max(.01f, breathSeconds));
            var info = Info;
            if (info.IsTransition && TimeInStage >= info.AutoAdvance)
                Next();
        }

        /// <summary>进入下一阶段;Exit → Finished(3 秒后自动)→ Standby(Start 按钮)。</summary>
        public void Next()
        {
            int n = (int)Current + 1;
            if (n == (int)Stage.Finale) n++;   // 场景5 已合并到"天亮,水母游走",直接跳过
            if (n > (int)Stage.Finished) n = (int)Stage.Standby;
            GoTo((Stage)n);
        }

        /// <summary>回到上一阶段(调试用),Standby 处不动作。</summary>
        public void Previous()
        {
            if (Current == Stage.Standby) return;
            int p = (int)Current - 1;
            if (p == (int)Stage.Finale) p--;
            GoTo((Stage)p);
        }

        public void ResetToStandby() => GoTo(Stage.Standby);

        public void GoTo(Stage next)
        {
            var prev = Current;
            Current = next;
            TimeInStage = 0f;
            Debug.Log($"[Experience] {prev} -> {next}");
            StageChanged?.Invoke(prev, next);
        }
    }
}
