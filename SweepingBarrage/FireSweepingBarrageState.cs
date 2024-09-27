using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;


namespace EntityStates.FORCED_REASSEMBLY.Commando
{
     public class FireSweepBarrage : BaseState
     { 

            public override void OnEnter()
            {
                base.OnEnter();
                this.totalDuration = FireSweepBarrage.baseTotalDuration / this.attackSpeedStat;
                this.firingDuration = FireSweepBarrage.baseFiringDuration / this.attackSpeedStat;
                base.characterBody.SetAimTimer(3f);
                base.PlayAnimation("Gesture, Additive", "FireBarrage", "FireBarrage.playbackRate", this.totalDuration);
                base.PlayAnimation("Gesture, Override", "FireBarrage", "FireBarrage.playbackRate", this.totalDuration);
                Util.PlaySound(FireSweepBarrage.enterSound, base.gameObject);
                Ray aimRay = base.GetAimRay();
                BullseyeSearch bullseyeSearch = new BullseyeSearch();
                bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(base.GetTeam());
                bullseyeSearch.maxAngleFilter = FireSweepBarrage.fieldOfView * 0.5f;
                bullseyeSearch.maxDistanceFilter = FireSweepBarrage.maxDistance;
                bullseyeSearch.searchOrigin = aimRay.origin;
                bullseyeSearch.searchDirection = aimRay.direction;
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
                bullseyeSearch.filterByLoS = true;
                bullseyeSearch.RefreshCandidates();
                this.targetHurtboxes = bullseyeSearch.GetResults().Where(new Func<HurtBox, bool>(Util.IsValid)).Distinct(default(HurtBox.EntityEqualityComparer)).ToList<HurtBox>();
                this.totalBulletsToFire = Mathf.Max(this.targetHurtboxes.Count, FireSweepBarrage.minimumFireCount);
                this.timeBetweenBullets = this.firingDuration / (float)this.totalBulletsToFire;
                this.childLocator = base.GetModelTransform().GetComponent<ChildLocator>();
                this.muzzleIndex = this.childLocator.FindChildIndex(FireSweepBarrage.muzzle);
                this.muzzleTransform = this.childLocator.FindChild(this.muzzleIndex);
            }
            private void Fire()
            {
                if (this.totalBulletsFired < this.totalBulletsToFire)
                {
                    if (!string.IsNullOrEmpty(FireSweepBarrage.muzzle))
                    {
                        EffectManager.SimpleMuzzleFlash(FireSweepBarrage.muzzleEffectPrefab, base.gameObject, FireSweepBarrage.muzzle, false);
                    }
                    Util.PlaySound(FireSweepBarrage.fireSoundString, base.gameObject);
                    this.PlayAnimation("Gesture Additive, Right", "FirePistol, Right");
                    if (NetworkServer.active && this.targetHurtboxes.Count > 0)
                    {
                        DamageInfo damageInfo = new DamageInfo();
                        damageInfo.damage = this.damageStat * FireSweepBarrage.damageCoefficient;
                        damageInfo.attacker = base.gameObject;
                        damageInfo.procCoefficient = FireSweepBarrage.procCoefficient;
                        damageInfo.crit = Util.CheckRoll(this.critStat, base.characterBody.master);
                        if (this.targetHurtboxIndex >= this.targetHurtboxes.Count)
                        {
                            this.targetHurtboxIndex = 0;
                        }
                        HurtBox hurtBox = this.targetHurtboxes[this.targetHurtboxIndex];
                        if (hurtBox)
                        {
                            HealthComponent healthComponent = hurtBox.healthComponent;
                            if (healthComponent)
                            {
                                this.targetHurtboxIndex++;
                                Vector3 normalized = (hurtBox.transform.position - base.characterBody.corePosition).normalized;
                                damageInfo.force = FireSweepBarrage.force * normalized;
                                damageInfo.position = hurtBox.transform.position;
                                EffectManager.SimpleImpactEffect(FireSweepBarrage.impactEffectPrefab, hurtBox.transform.position, normalized, true);
                                healthComponent.TakeDamage(damageInfo);
                                GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                            }
                            if (FireSweepBarrage.tracerEffectPrefab && this.childLocator)
                            {
                                int childIndex = this.childLocator.FindChildIndex(FireSweepBarrage.muzzle);
                                this.childLocator.FindChild(childIndex);
                                EffectData effectData = new EffectData
                                {
                                    origin = hurtBox.transform.position,
                                    start = this.muzzleTransform.position
                                };
                                effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
                                EffectManager.SpawnEffect(FireSweepBarrage.tracerEffectPrefab, effectData, true);
                            }
                        }
                    }
                    this.totalBulletsFired++;
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                this.fireTimer -= Time.fixedDeltaTime;
                if (this.fireTimer <= 0f)
                {
                    this.Fire();
                    this.fireTimer += this.timeBetweenBullets;
                }
                if (base. isAuthority && base.fixedAge >= this.totalDuration)
                {
                    this.outer.SetNextStateToMain();
                    return;
                }
            }

            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.PrioritySkill;
            }

            public static string enterSound; 
            public static string muzzle = "MuzzleRight";
            public static string fireSoundString = "Play_commando_M1";
            public static GameObject muzzleEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/MuzzleflashBarrage.prefab").WaitForCompletion();
            public static GameObject tracerEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/TracerToolbotRebar.prefab").WaitForCompletion();
            public static float baseTotalDuration = 1f;
            public static float baseFiringDuration = 0.6f;
            public static float fieldOfView = 120f;
            public static float maxDistance = 120f;
            public static float damageCoefficient = 3.8f;
            public static float procCoefficient = 0.7f;
            public static float force = 300f;
            public static int minimumFireCount = 3;
            public static GameObject impactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFX.prefab").WaitForCompletion();

        private float totalDuration;
            private float firingDuration;
            private int totalBulletsToFire;
            private int totalBulletsFired;
            private int targetHurtboxIndex;
            private float timeBetweenBullets;
            private List<HurtBox> targetHurtboxes = new List<HurtBox>();
            private float fireTimer;
            private ChildLocator childLocator;
            private int muzzleIndex;
            private Transform muzzleTransform;

        public override void OnExit()
        {
            //This runs on everyone's machine because the firing sfx/vfx is client-side while the actual damage dealing is server-side.
            int remainingBullets = this.totalBulletsToFire - totalBulletsFired; //Explicitly get the amount of remaining bullets to fire in case anything weird happens.
            for (int i = 0; i < remainingBullets; i++)
            {
                Fire();
            }
            base.OnExit();
        }
    }

}
    
