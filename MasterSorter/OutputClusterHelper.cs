using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;

namespace MasterSorter
{
    public class OutputClusterHelper
    {
        public class Cluster
        {
            /// <summary>
            /// The masters for the current cluster. The output file must have these masters.
            /// </summary>
            public HashSet<ModKey> masters = new();

            /// <summary>
            /// The record for the current cluster, these would be contained in the resulting file
            /// </summary>
            public List<IMajorRecord> records = new();
        }

        private static HashSet<ModKey> GetAllMasters(IMajorRecord majorRecord, ModKey except)
        {
            var result = new HashSet<ModKey>();

            // what if majorRecord is an override?
            if (except != majorRecord.FormKey.ModKey)
            {
                result.Add(majorRecord.FormKey.ModKey);
            }

            var formLinks = majorRecord.EnumerateFormLinks();
            foreach (var formLink in formLinks)
            {
                // is formLink a major record itself? but, do we even care?
                // I think we don't have to follow it recursively
                //formLink.
                // result.UnionWith(GetAllMasters(formLink));
                result.Add(formLink.FormKey.ModKey);
            }

            return result;
        }

        public static List<Cluster> GenerateClusters(IMod inputMod, int limit)
        {
            var clusters = new List<Cluster>();

            var recs = inputMod.EnumerateMajorRecords();
            foreach (var rec in recs)
            {
                var masters = GetAllMasters(rec, inputMod.ModKey);
                var lastClusterIndex = -1;

                for (int i = 0; i < clusters.Count; i++)
                {
                    var curCluster = clusters[i];

                    var missingMasters = masters.Except(curCluster.masters);

                    if (curCluster.masters.Count + missingMasters.Count() <= limit)
                    {
                        // found an existing cluster where the current record fits
                        lastClusterIndex = i;
                        break;
                    }
                }

                if (lastClusterIndex < 0)
                {
                    // we didn't find any, create new
                    var newCluster = new Cluster
                    {
                        masters = masters
                    };
                    newCluster.records.Add(rec);

                    clusters.Add(newCluster);
                    continue;
                }

                // found existing, union it with current masters
                var existingCluster = clusters[lastClusterIndex];
                existingCluster.masters.UnionWith(masters);
                existingCluster.records.Add(rec);
            }

            return clusters;
        }

        public List<TMod> SplitOutputMod<TMod>(GameRelease release, TMod inputMod, int limit)
            where TMod : IModGetter, IMod//IModGetter
        {
            // edge case: if rec is supposed to recieve a certain FormID via the EditorID mechanism, it should also always go into the same filename
            // TODO think of a mechanism for this
            var result = new List<TMod>();
            var inputFileName = inputMod.ModKey.FileName;
            var baseName = inputFileName.NameWithoutExtension;
            var ext = inputFileName.Extension;// includes .

            var clusters = GenerateClusters(inputMod, limit);
            for (int i = 0; i < clusters.Count; i++)
            {
                var curCluster = clusters[i];
                string? curFileName;
                if (i == 0)
                {
                    curFileName = inputFileName;
                }
                else
                {
                    curFileName = baseName + "_" + (i + 1).ToString() + ext;
                }

                var newMod = ModInstantiator<TMod>.Activator(ModKey.FromFileName(curFileName), release);

                // var linkCache = newMod.ToUntypedMutableLinkCache();
                foreach (var rec in curCluster.records)
                {
                    // add the recs
                    addRecordToMod(rec, newMod);
                }
                result.Add(newMod);
            }

            return result;
        }

        private void addRecordToMod(IMajorRecord oldRec, IMod newMod)
        {
            // 
            var rec = oldRec.Duplicate(new FormKey(newMod.ModKey, newMod.NextFormID++));
            // this is a really really dirty hack, for the test only.
            // This should be replaced by a proper game-agnostic function down the line
            if (newMod is Fallout4Mod f4mod)
            {
                // regex-generated
                if (rec is GameSetting recGameSettings) { f4mod.GameSettings.Add(recGameSettings); }

                if (rec is Keyword recKeywords) { f4mod.Keywords.Add(recKeywords); }

                if (rec is LocationReferenceType recLocationReferenceTypes) { f4mod.LocationReferenceTypes.Add(recLocationReferenceTypes); }

                if (rec is Mutagen.Bethesda.Fallout4.ActionRecord recActions) { f4mod.Actions.Add(recActions); }

                if (rec is Transform recTransforms) { f4mod.Transforms.Add(recTransforms); }

                if (rec is Component recComponents) { f4mod.Components.Add(recComponents); }

                if (rec is TextureSet recTextureSets) { f4mod.TextureSets.Add(recTextureSets); }

                if (rec is Global recGlobals) { f4mod.Globals.Add(recGlobals); }

                if (rec is DamageType recDamageTypes) { f4mod.DamageTypes.Add(recDamageTypes); }

                if (rec is Class recClasses) { f4mod.Classes.Add(recClasses); }

                if (rec is Faction recFactions) { f4mod.Factions.Add(recFactions); }

                if (rec is HeadPart recHeadParts) { f4mod.HeadParts.Add(recHeadParts); }

                if (rec is Race recRaces) { f4mod.Races.Add(recRaces); }

                if (rec is SoundMarker recSoundMarkers) { f4mod.SoundMarkers.Add(recSoundMarkers); }

                if (rec is AcousticSpace recAcousticSpaces) { f4mod.AcousticSpaces.Add(recAcousticSpaces); }

                if (rec is MagicEffect recMagicEffects) { f4mod.MagicEffects.Add(recMagicEffects); }

                if (rec is LandscapeTexture recLandscapeTextures) { f4mod.LandscapeTextures.Add(recLandscapeTextures); }

                if (rec is ObjectEffect recObjectEffects) { f4mod.ObjectEffects.Add(recObjectEffects); }

                if (rec is Spell recSpells) { f4mod.Spells.Add(recSpells); }

                if (rec is Mutagen.Bethesda.Fallout4.Activator recActivators) { f4mod.Activators.Add(recActivators); }

                if (rec is TalkingActivator recTalkingActivators) { f4mod.TalkingActivators.Add(recTalkingActivators); }

                if (rec is Armor recArmors) { f4mod.Armors.Add(recArmors); }

                if (rec is Book recBooks) { f4mod.Books.Add(recBooks); }

                if (rec is Container recContainers) { f4mod.Containers.Add(recContainers); }

                if (rec is Door recDoors) { f4mod.Doors.Add(recDoors); }

                if (rec is Ingredient recIngredients) { f4mod.Ingredients.Add(recIngredients); }

                if (rec is Light recLights) { f4mod.Lights.Add(recLights); }

                if (rec is MiscItem recMiscItems) { f4mod.MiscItems.Add(recMiscItems); }

                if (rec is Static recStatics) { f4mod.Statics.Add(recStatics); }

                if (rec is StaticCollection recStaticCollections) { f4mod.StaticCollections.Add(recStaticCollections); }

                if (rec is MovableStatic recMovableStatics) { f4mod.MovableStatics.Add(recMovableStatics); }

                if (rec is Grass recGrasses) { f4mod.Grasses.Add(recGrasses); }

                if (rec is Tree recTrees) { f4mod.Trees.Add(recTrees); }

                if (rec is Flora recFlorae) { f4mod.Florae.Add(recFlorae); }

                if (rec is Furniture recFurniture) { f4mod.Furniture.Add(recFurniture); }

                if (rec is Weapon recWeapons) { f4mod.Weapons.Add(recWeapons); }

                if (rec is Ammunition recAmmunitions) { f4mod.Ammunitions.Add(recAmmunitions); }

                if (rec is Npc recNpcs) { f4mod.Npcs.Add(recNpcs); }

                if (rec is LeveledNpc recLeveledNpcs) { f4mod.LeveledNpcs.Add(recLeveledNpcs); }

                if (rec is Key recKeys) { f4mod.Keys.Add(recKeys); }

                if (rec is Ingestible recIngestibles) { f4mod.Ingestibles.Add(recIngestibles); }

                if (rec is IdleMarker recIdleMarkers) { f4mod.IdleMarkers.Add(recIdleMarkers); }

                if (rec is Holotape recHolotapes) { f4mod.Holotapes.Add(recHolotapes); }

                if (rec is Projectile recProjectiles) { f4mod.Projectiles.Add(recProjectiles); }

                if (rec is Hazard recHazards) { f4mod.Hazards.Add(recHazards); }

                if (rec is BendableSpline recBendableSplines) { f4mod.BendableSplines.Add(recBendableSplines); }

                if (rec is Terminal recTerminals) { f4mod.Terminals.Add(recTerminals); }

                if (rec is LeveledItem recLeveledItems) { f4mod.LeveledItems.Add(recLeveledItems); }

                if (rec is Weather recWeather) { f4mod.Weather.Add(recWeather); }

                if (rec is Climate recClimates) { f4mod.Climates.Add(recClimates); }

                if (rec is ShaderParticleGeometry recShaderParticleGeometries) { f4mod.ShaderParticleGeometries.Add(recShaderParticleGeometries); }

                if (rec is VisualEffect recVisualEffects) { f4mod.VisualEffects.Add(recVisualEffects); }

                if (rec is Region recRegions) { f4mod.Regions.Add(recRegions); }

                if (rec is NavigationMeshInfoMap recNavigationMeshInfoMaps) { f4mod.NavigationMeshInfoMaps.Add(recNavigationMeshInfoMaps); }

                if (rec is Worldspace recWorldspaces) { f4mod.Worldspaces.Add(recWorldspaces); }

                if (rec is Quest recQuests) { f4mod.Quests.Add(recQuests); }

                if (rec is IdleAnimation recIdleAnimations) { f4mod.IdleAnimations.Add(recIdleAnimations); }

                if (rec is Package recPackages) { f4mod.Packages.Add(recPackages); }

                if (rec is CombatStyle recCombatStyles) { f4mod.CombatStyles.Add(recCombatStyles); }

                if (rec is LoadScreen recLoadScreens) { f4mod.LoadScreens.Add(recLoadScreens); }

                if (rec is AnimatedObject recAnimatedObjects) { f4mod.AnimatedObjects.Add(recAnimatedObjects); }

                if (rec is Water recWaters) { f4mod.Waters.Add(recWaters); }

                if (rec is EffectShader recEffectShaders) { f4mod.EffectShaders.Add(recEffectShaders); }

                if (rec is Explosion recExplosions) { f4mod.Explosions.Add(recExplosions); }

                if (rec is Debris recDebris) { f4mod.Debris.Add(recDebris); }

                if (rec is ImageSpace recImageSpaces) { f4mod.ImageSpaces.Add(recImageSpaces); }

                if (rec is ImageSpaceAdapter recImageSpaceAdapters) { f4mod.ImageSpaceAdapters.Add(recImageSpaceAdapters); }

                if (rec is FormList recFormLists) { f4mod.FormLists.Add(recFormLists); }

                if (rec is Perk recPerks) { f4mod.Perks.Add(recPerks); }

                if (rec is BodyPartData recBodyParts) { f4mod.BodyParts.Add(recBodyParts); }

                if (rec is AddonNode recAddonNodes) { f4mod.AddonNodes.Add(recAddonNodes); }

                if (rec is ActorValueInformation recActorValueInformation) { f4mod.ActorValueInformation.Add(recActorValueInformation); }

                if (rec is CameraShot recCameraShots) { f4mod.CameraShots.Add(recCameraShots); }

                if (rec is CameraPath recCameraPaths) { f4mod.CameraPaths.Add(recCameraPaths); }

                if (rec is VoiceType recVoiceTypes) { f4mod.VoiceTypes.Add(recVoiceTypes); }

                if (rec is MaterialType recMaterialTypes) { f4mod.MaterialTypes.Add(recMaterialTypes); }

                if (rec is Impact recImpacts) { f4mod.Impacts.Add(recImpacts); }

                if (rec is ImpactDataSet recImpactDataSets) { f4mod.ImpactDataSets.Add(recImpactDataSets); }

                if (rec is ArmorAddon recArmorAddons) { f4mod.ArmorAddons.Add(recArmorAddons); }

                if (rec is EncounterZone recEncounterZones) { f4mod.EncounterZones.Add(recEncounterZones); }

                if (rec is Location recLocations) { f4mod.Locations.Add(recLocations); }

                if (rec is Message recMessages) { f4mod.Messages.Add(recMessages); }

                if (rec is DefaultObjectManager recDefaultObjectManagers) { f4mod.DefaultObjectManagers.Add(recDefaultObjectManagers); }

                if (rec is DefaultObject recDefaultObjects) { f4mod.DefaultObjects.Add(recDefaultObjects); }

                if (rec is LightingTemplate recLightingTemplates) { f4mod.LightingTemplates.Add(recLightingTemplates); }

                if (rec is MusicType recMusicTypes) { f4mod.MusicTypes.Add(recMusicTypes); }

                if (rec is Footstep recFootsteps) { f4mod.Footsteps.Add(recFootsteps); }

                if (rec is FootstepSet recFootstepSets) { f4mod.FootstepSets.Add(recFootstepSets); }

                if (rec is StoryManagerBranchNode recStoryManagerBranchNodes) { f4mod.StoryManagerBranchNodes.Add(recStoryManagerBranchNodes); }

                if (rec is StoryManagerQuestNode recStoryManagerQuestNodes) { f4mod.StoryManagerQuestNodes.Add(recStoryManagerQuestNodes); }

                if (rec is StoryManagerEventNode recStoryManagerEventNodes) { f4mod.StoryManagerEventNodes.Add(recStoryManagerEventNodes); }

                if (rec is MusicTrack recMusicTracks) { f4mod.MusicTracks.Add(recMusicTracks); }

                if (rec is DialogView recDialogViews) { f4mod.DialogViews.Add(recDialogViews); }

                if (rec is EquipType recEquipTypes) { f4mod.EquipTypes.Add(recEquipTypes); }

                if (rec is Relationship recRelationships) { f4mod.Relationships.Add(recRelationships); }

                if (rec is AssociationType recAssociationTypes) { f4mod.AssociationTypes.Add(recAssociationTypes); }

                if (rec is Outfit recOutfits) { f4mod.Outfits.Add(recOutfits); }

                if (rec is ArtObject recArtObjects) { f4mod.ArtObjects.Add(recArtObjects); }

                if (rec is MaterialObject recMaterialObjects) { f4mod.MaterialObjects.Add(recMaterialObjects); }

                if (rec is MovementType recMovementTypes) { f4mod.MovementTypes.Add(recMovementTypes); }

                if (rec is SoundDescriptor recSoundDescriptors) { f4mod.SoundDescriptors.Add(recSoundDescriptors); }

                if (rec is SoundCategory recSoundCategories) { f4mod.SoundCategories.Add(recSoundCategories); }

                if (rec is SoundOutputModel recSoundOutputModels) { f4mod.SoundOutputModels.Add(recSoundOutputModels); }

                if (rec is CollisionLayer recCollisionLayers) { f4mod.CollisionLayers.Add(recCollisionLayers); }

                if (rec is ColorRecord recColors) { f4mod.Colors.Add(recColors); }

                if (rec is ReverbParameters recReverbParameters) { f4mod.ReverbParameters.Add(recReverbParameters); }

                if (rec is PackIn recPackIns) { f4mod.PackIns.Add(recPackIns); }

                if (rec is ReferenceGroup recReferenceGroups) { f4mod.ReferenceGroups.Add(recReferenceGroups); }

                if (rec is AimModel recAimModels) { f4mod.AimModels.Add(recAimModels); }

                if (rec is Layer recLayers) { f4mod.Layers.Add(recLayers); }

                if (rec is ConstructibleObject recConstructibleObjects) { f4mod.ConstructibleObjects.Add(recConstructibleObjects); }

                if (rec is ObjectModification recObjectModifications) { f4mod.ObjectModifications.Add(recObjectModifications); }

                if (rec is MaterialSwap recMaterialSwaps) { f4mod.MaterialSwaps.Add(recMaterialSwaps); }

                if (rec is Zoom recZooms) { f4mod.Zooms.Add(recZooms); }

                if (rec is InstanceNamingRules recInstanceNamingRules) { f4mod.InstanceNamingRules.Add(recInstanceNamingRules); }

                if (rec is SoundKeywordMapping recSoundKeywordMappings) { f4mod.SoundKeywordMappings.Add(recSoundKeywordMappings); }

                if (rec is AudioEffectChain recAudioEffectChains) { f4mod.AudioEffectChains.Add(recAudioEffectChains); }

                if (rec is SceneCollection recSceneCollections) { f4mod.SceneCollections.Add(recSceneCollections); }

                if (rec is AttractionRule recAttractionRules) { f4mod.AttractionRules.Add(recAttractionRules); }

                if (rec is AudioCategorySnapshot recAudioCategorySnapshots) { f4mod.AudioCategorySnapshots.Add(recAudioCategorySnapshots); }

                if (rec is AnimationSoundTagSet recAnimationSoundTagSets) { f4mod.AnimationSoundTagSets.Add(recAnimationSoundTagSets); }

                if (rec is NavigationMeshObstacleManager recNavigationMeshObstacleManagers) { f4mod.NavigationMeshObstacleManagers.Add(recNavigationMeshObstacleManagers); }

                if (rec is LensFlare recLensFlares) { f4mod.LensFlares.Add(recLensFlares); }

                if (rec is GodRays recGodRays) { f4mod.GodRays.Add(recGodRays); }

                if (rec is ObjectVisibilityManager recObjectVisibilityManagers) { f4mod.ObjectVisibilityManagers.Add(recObjectVisibilityManagers); }
            }
        }
    }
}
