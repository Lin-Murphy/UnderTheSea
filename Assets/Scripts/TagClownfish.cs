using UnityEngine;

namespace UnderTheSea
{
    /// <summary>
    /// 场景3 探头检查:用小丑鱼(Assets/Models/Clownfish)代替原来表示 tag 位置的小黄点,
    /// 看起来像一条鱼在水草和珊瑚之间游动、拨动了水草(海草把它当作"交互者"来跟随 / 被按压)。
    /// - tag 被识别且在海草控制区域内:小鱼紧跟 tag(临界阻尼跟随,约 0.3 秒追上,保证 0.5 秒内跟上)。
    /// - tag 丢失或离开控制区域:小鱼在海草和珊瑚周围自然地随机游动(随机目标点 + 有限转向)。
    /// - 只在探头检查阶段出现,进出时 0.5 秒缩放淡入淡出;不会钻进主角珊瑚身体里。
    /// - 游动动画(Swim_Loop)的播放速度随游速变化。
    /// </summary>
    public sealed class TagClownfish : MonoBehaviour
    {
        public ExperienceController controller;
        public SoftSeaweedStrands seaweed;
        public Animator animator;
        public Stage activeStage = Stage.Probe;

        [Header("跟随 tag")]
        [Tooltip("跟随的平滑时间(秒);0.12 时约 0.3 秒追上,满足 0.5 秒内跟上")] public float followSmoothTime = .12f;
        public float followMaxSpeed = 6f;

        [Header("自然游动")]
        public float wanderSpeed = .4f;
        public float turnRate = 2.5f;       // 弧度/秒
        public Vector2 wanderRadius = new Vector2(.12f, .55f);   // 在身体半径之外再加的距离
        public Vector2 wanderHeight = new Vector2(.2f, .95f);    // 身体高度的比例

        Vector3 vel, smoothVel, wanderTarget, baseScale;
        float vis, retarget;
        bool wasFollowing, placed, rendered = true;
        System.Random rng = new System.Random(4242);

        /// <summary>自检用:当前是否在跟随 tag。</summary>
        public bool Following { get; private set; }
        public float Speed => vel.magnitude;

        void Awake()
        {
            baseScale = transform.localScale;
            transform.localScale = Vector3.zero;
            SetVisible(false);
        }

        void SetVisible(bool on)
        {
            rendered = on;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            if (animator) animator.enabled = on;
        }

        float R() => (float)rng.NextDouble();

        void PickWanderTarget()
        {
            float a = R() * Mathf.PI * 2f;
            float r = seaweed.RegionRadius + Mathf.Lerp(wanderRadius.x, wanderRadius.y, R());
            float h = seaweed.RegionHeight * Mathf.Lerp(wanderHeight.x, wanderHeight.y, R());
            Vector3 c = seaweed.RegionCenter;
            wanderTarget = new Vector3(c.x + Mathf.Cos(a) * r, c.y + h, c.z + Mathf.Sin(a) * r);
            retarget = Mathf.Lerp(2.5f, 5f, R());
        }

        void Update() => Step(Time.deltaTime);

        /// <summary>推进一步(Update 调用;编辑器自检也可直接调用)。</summary>
        public void Step(float dt)
        {
            if (!controller || !seaweed) return;

            bool active = controller.Current == activeStage && seaweed.RegionHeight > 0f;
            vis = Mathf.MoveTowards(vis, active ? 1f : 0f, dt / .5f);
            bool show = vis > .01f;
            if (show != rendered) SetVisible(show);
            transform.localScale = baseScale * Mathf.SmoothStep(0f, 1f, vis);
            if (!show) { placed = false; return; }

            if (!placed)
            {
                PickWanderTarget();
                transform.position = wanderTarget;
                vel = Vector3.zero; smoothVel = Vector3.zero; placed = true;
            }

            Following = seaweed.TagInRegion;
            Vector3 pos = transform.position;
            if (Following)
            {
                // tag 回来:临界阻尼跟随,约 0.3 秒追上
                if (!wasFollowing) smoothVel = vel;
                pos = Vector3.SmoothDamp(pos, seaweed.TagRegionPoint, ref smoothVel, followSmoothTime, followMaxSpeed, dt);
                vel = smoothVel;
            }
            else
            {
                // 自然随机游动:朝随机目标点游,有限转向,到达或超时就换一个目标
                if (wasFollowing) PickWanderTarget();
                retarget -= dt;
                Vector3 to = wanderTarget - pos;
                if (to.magnitude < .12f || retarget <= 0f) { PickWanderTarget(); to = wanderTarget - pos; }
                float speed = wanderSpeed * (.85f + .3f * Mathf.Sin(Time.time * 1.3f));
                Vector3 desired = to.normalized * speed;
                if (vel.sqrMagnitude < 1e-4f) vel = desired * .2f;
                vel = Vector3.RotateTowards(vel, desired, turnRate * dt, 1.5f * dt);
                pos += vel * dt;
            }
            wasFollowing = Following;

            // 不钻进主角珊瑚身体
            Vector3 c = seaweed.RegionCenter;
            float y = Mathf.Clamp(pos.y, c.y, c.y + seaweed.RegionHeight);
            Vector3 axis = new Vector3(c.x, y, c.z);
            Vector3 d = pos - axis;
            float minR = seaweed.RegionRadius + .05f;
            if (d.magnitude < minR) pos = axis + (d.sqrMagnitude < 1e-6f ? Vector3.forward : d.normalized) * minR;
            transform.position = pos;

            if (vel.sqrMagnitude > .0004f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(vel), 1f - Mathf.Exp(-dt * 10f));
            if (animator) animator.speed = Mathf.Lerp(.7f, 2.2f, Mathf.Clamp01(vel.magnitude / .8f));
        }
    }
}
