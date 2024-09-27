

using BepInEx;
using EntityStates;
using RoR2.Skills;
using R2API;
using UnityEngine;
using R2API.Utils;
using UnityEngine.AddressableAssets;
using System;
using RoR2;
using System.IO;
using System.Reflection;

namespace SweepBarrage
{
    [BepInDependency("com.bepis.r2api")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(ContentAddition))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2API.Utils.R2APISubmoduleDependency(nameof(ContentAddition), nameof(LanguageAPI))]

    [BepInPlugin("com.Forced_Reassembly.SweepingBarrage", "Sweeping Barrage", "1.0.0")]
    public class SweepBarragePlugin : BaseUnityPlugin
    {
        [Obsolete]
        private void Awake()
        {
            using (Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SweepingBarrage.sweepingbarragebundle"))
            {
                mainAssetBundle = AssetBundle.LoadFromStream(manifestResourceStream);
            }

            // Now we must create a SkillDef
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the EntityStates.Commando.CommandoWeapon.FireSweepBarrage class
            mySkillDef.activationState = new SerializableEntityStateType(typeof(EntityStates.FORCED_REASSEMBLY.Commando.FireSweepBarrage));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 7f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = true;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Skill;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            // For the skill icon, you will have to load a Sprite from your own AssetBundle
            mySkillDef.icon = mainAssetBundle.LoadAsset<Sprite>("SweepBarrageIconGoodVersion");
            mySkillDef.skillDescriptionToken = "Automatically fire at all enemies in front of you for <style=cIsDamage>380% damage</style> damage per shot.";
            mySkillDef.skillName = "COMMANDO_SPECIAL_SWEEPBARRAGE_NAME";
            mySkillDef.skillNameToken = "Sweeping Barrage";
            (mySkillDef as ScriptableObject).name = mySkillDef.skillName;
            SweepBarragePlugin.SweepBarrageSkill = mySkillDef;
            LanguageAPI.Add("COMMANDO_SPECIAL_SWEEPBARRAGE_NAME", "Sweeping Barrage");


            // This adds our skilldef. If you don't do this, the skill will not work.
            ContentAddition.AddSkillDef(mySkillDef);

            GameObject commandoBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/CommandoBody.prefab").WaitForCompletion();
            // Now we add our skill to one of the survivor's skill families
            // You can change component.primary to component.secondary, component.utility and component.special
            SkillLocator skillLocator = commandoBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.special.skillFamily;

            // If this is an alternate skill, use this code.
            // Here, we add our skill as a variant to the existing Skill Family.
            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant


            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

            
            ContentAddition.AddEntityState<EntityStates.FORCED_REASSEMBLY.Commando.FireSweepBarrage>(out bool wasAdded);
        }
        public static void DumpEntityStateConfig(EntityStateConfiguration esc)
        {

            for (int i = 0; i < esc.serializedFieldsCollection.serializedFields.Length; i++)
            {
                if (esc.serializedFieldsCollection.serializedFields[i].fieldValue.objectValue)
                {
                    Debug.Log(esc.serializedFieldsCollection.serializedFields[i].fieldName + " - " + esc.serializedFieldsCollection.serializedFields[i].fieldValue.objectValue);
                }
                else
                {
                    Debug.Log(esc.serializedFieldsCollection.serializedFields[i].fieldName + " - " + esc.serializedFieldsCollection.serializedFields[i].fieldValue.stringValue);
                }
            }
        }


      

        public static SkillDef SweepBarrageSkill;
        public static AssetBundle mainAssetBundle;

    }
}