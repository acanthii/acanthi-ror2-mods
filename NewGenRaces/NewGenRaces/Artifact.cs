using R2API;
using RoR2;
using UnityEngine;

namespace NewGenRaces {
    public class Artifact {

        public static string ArtifactName = "Artifact of Time";
        public static string ArtifactDescription = "Upon dying, revive and add a penalty to the timer.";
        public static ArtifactDef ArtifactDef;

        public static bool ArtifactEnabled => RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef);

        public static Sprite ArtifactEnabledIcon => NewGenRaces.Assets.LoadAsset<Sprite>("ArtifactOfTime.png");
        public static Sprite ArtifactDisabledIcon => NewGenRaces.Assets.LoadAsset<Sprite>("ArtifactOfTimeDisabled.png");

        public static void Init() {
            CreateLang();
            CreateArtifact();
        }

        protected static void CreateArtifact() {
            ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            ArtifactDef.cachedName = "ARTIFACT_TIME";
            ArtifactDef.nameToken = "ARTIFACT_TIME_NAME";
            ArtifactDef.descriptionToken = "ARTIFACT_TIME_DESCRIPTION";
            ArtifactDef.smallIconSelectedSprite = ArtifactEnabledIcon;
            ArtifactDef.smallIconDeselectedSprite = ArtifactDisabledIcon;

            ContentAddition.AddArtifactDef(ArtifactDef);
        }

        protected static void CreateLang() {
            LanguageAPI.Add("ARTIFACT_TIME_NAME", ArtifactName);
            LanguageAPI.Add("ARTIFACT_TIME_DESCRIPTION", ArtifactDescription);
        }
    }
}
