using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using IllusionPlugin;
using System.Reflection;
using BS_Utils.Gameplay;

namespace BeatSaberDataWrappers
{
    /// <summary>
    /// Main Plugin class that handles updates to Data.
    /// Large portions of this code taken from the BeatSaberHTTPStatus mod.
    /// Link: https://github.com/opl-/beatsaber-http-status
    /// </summary>
    public class Plugin : IPlugin
    {
        public string Name => Constants.Name;
        public string Version => Constants.Version;

        private bool headInObstacle = false;

        private GameplayCoreSceneSetupData gameplayCoreSceneSetupData;
        private GamePauseManager gamePauseManager;
        private ScoreController scoreController;
        private StandardLevelGameplayManager gameplayManager;
        private GameplayModifiersModelSO gameplayModifiersSO;
        private AudioTimeSyncController audioTimeSyncController;
        private BeatmapObjectCallbackController beatmapObjectCallbackController;
        private PlayerHeadAndObstacleInteraction playerHeadAndObstacleInteraction;
        private GameEnergyCounter gameEnergyCounter;
        private Dictionary<NoteCutInfo, NoteData> noteCutMapping = new Dictionary<NoteCutInfo, NoteData>();

        /// protected NoteCutInfo AfterCutScoreBuffer._noteCutInfo
		private FieldInfo noteCutInfoField = typeof(AfterCutScoreBuffer).GetField("_noteCutInfo", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// protected List<AfterCutScoreBuffer> ScoreController._afterCutScoreBuffers // contains a list of after cut buffers
        private FieldInfo afterCutScoreBuffersField = typeof(ScoreController).GetField("_afterCutScoreBuffers", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// private int AfterCutScoreBuffer#_afterCutScoreWithMultiplier
        private FieldInfo afterCutScoreWithMultiplierField = typeof(AfterCutScoreBuffer).GetField("_afterCutScoreWithMultiplier", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// private int AfterCutScoreBuffer#_multiplier
        private FieldInfo afterCutScoreBufferMultiplierField = typeof(AfterCutScoreBuffer).GetField("_multiplier", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// private static LevelCompletionResults.Rank LevelCompletionResults.GetRankForScore(int score, int maxPossibleScore)
        private MethodInfo getRankForScoreMethod = typeof(LevelCompletionResults).GetMethod("GetRankForScore", BindingFlags.NonPublic | BindingFlags.Static);

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            Data.scene = newScene.name;

            if (newScene.name == "MenuCore")
            {
                // Menu
                Data.scene = "Menu";

                Gamemode.Init();

                // TODO: get the current song, mode and mods while in menu
                Data.ResetMapInfo();

                Data.ResetPerformance();

                // Release references for AfterCutScoreBuffers that don't resolve due to player leaving the map before finishing.
                noteCutMapping.Clear();

                Data.StatusChange(ChangedProperties.AllButNoteCut, "menu");
            }
            else if (newScene.name == "GameCore")
            {
                // In game
                Data.scene = "Song";

                gamePauseManager = FindFirstOrDefault<GamePauseManager>();
                scoreController = FindFirstOrDefault<ScoreController>();
                gameplayManager = FindFirstOrDefault<StandardLevelGameplayManager>();
                beatmapObjectCallbackController = FindFirstOrDefault<BeatmapObjectCallbackController>();
                gameplayModifiersSO = FindFirstOrDefault<GameplayModifiersModelSO>();
                audioTimeSyncController = FindFirstOrDefault<AudioTimeSyncController>();
                playerHeadAndObstacleInteraction = FindFirstOrDefault<PlayerHeadAndObstacleInteraction>();
                gameEnergyCounter = FindFirstOrDefault<GameEnergyCounter>();

                gameplayCoreSceneSetupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;

                // Register event listeners
                // private GameEvent GamePauseManager#_gameDidPauseSignal
                AddSubscriber(gamePauseManager, "_gameDidPauseSignal", OnGamePause);
                // private GameEvent GamePauseManager#_gameDidResumeSignal
                AddSubscriber(gamePauseManager, "_gameDidResumeSignal", OnGameResume);
                // public ScoreController#noteWasCutEvent<NoteData, NoteCutInfo, int multiplier> // called after AfterCutScoreBuffer is created
                scoreController.noteWasCutEvent += OnNoteWasCut;
                // public ScoreController#noteWasMissedEvent<NoteData, int multiplier>
                scoreController.noteWasMissedEvent += OnNoteWasMissed;
                // public ScoreController#scoreDidChangeEvent<int> // score
                scoreController.scoreDidChangeEvent += OnScoreDidChange;
                // public ScoreController#comboDidChangeEvent<int> // combo
                scoreController.comboDidChangeEvent += OnComboDidChange;
                // public ScoreController#multiplierDidChangeEvent<int, float> // multiplier, progress [0..1]
                scoreController.multiplierDidChangeEvent += OnMultiplierDidChange;
                // private GameEvent GameplayManager#_levelFinishedSignal
                AddSubscriber(gameplayManager, "_levelFinishedSignal", OnLevelFinished);
                // private GameEvent GameplayManager#_levelFailedSignal
                AddSubscriber(gameplayManager, "_levelFailedSignal", OnLevelFailed);
                // public event Action<BeatmapEventData> BeatmapObjectCallbackController#beatmapEventDidTriggerEvent
                beatmapObjectCallbackController.beatmapEventDidTriggerEvent += OnBeatmapEventDidTrigger;

                IDifficultyBeatmap diff = gameplayCoreSceneSetupData.difficultyBeatmap;
                IBeatmapLevel level = diff.level;

                Data.partyMode = Gamemode.IsPartyActive;
                Data.mode = Gamemode.GameMode;

                GameplayModifiers gameplayModifiers = gameplayCoreSceneSetupData.gameplayModifiers;
                PlayerSpecificSettings playerSettings = gameplayCoreSceneSetupData.playerSpecificSettings;
                PracticeSettings practiceSettings = gameplayCoreSceneSetupData.practiceSettings;

                float songSpeedMul = gameplayModifiers.songSpeedMul;
                if (practiceSettings != null) songSpeedMul = practiceSettings.songSpeedMul;
                float modifierMultiplier = gameplayModifiersSO.GetTotalMultiplier(gameplayModifiers);

                Data.songName = level.songName;
                Data.songSubName = level.songSubName;
                Data.songAuthorName = level.songAuthorName;
                Data.levelAuthorName = level.levelAuthorName;
                Data.songBPM = level.beatsPerMinute;
                Data.noteJumpSpeed = diff.noteJumpMovementSpeed;
                Data.songHash = level.levelID.Substring(0, Math.Min(32, level.levelID.Length));
                Data.songTimeOffset = (long)(level.songTimeOffset * 1000f / songSpeedMul);
                Data.length = (long)(level.beatmapLevelData.audioClip.length * 1000f / songSpeedMul);
                Data.start = GetCurrentTime() - (long)(audioTimeSyncController.songTime * 1000f / songSpeedMul);
                if (practiceSettings != null) Data.start -= (long)(practiceSettings.startSongTime * 1000f / songSpeedMul);
                Data.paused = 0;
                Data.difficulty = diff.difficulty.Name();
                Data.notesCount = diff.beatmapData.notesCount;
                Data.bombsCount = diff.beatmapData.bombsCount;
                Data.obstaclesCount = diff.beatmapData.obstaclesCount;
                Data.environmentName = level.environmentSceneInfo.sceneName;
                Data.maxScore = ScoreController.GetScoreForGameplayModifiersScoreMultiplier(ScoreController.MaxScoreForNumberOfNotes(diff.beatmapData.notesCount), modifierMultiplier);
                Data.maxRank = RankModel.MaxRankForGameplayModifiers(gameplayModifiers, gameplayModifiersSO).ToString();

                try
                {
                    // From https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
                    var texture = level.coverImage.texture;
                    var active = RenderTexture.active;
                    var temporary = RenderTexture.GetTemporary(
                        texture.width,
                        texture.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear
                    );

                    Graphics.Blit(texture, temporary);
                    RenderTexture.active = temporary;

                    var cover = new Texture2D(texture.width, texture.height);
                    cover.ReadPixels(new Rect(0, 0, temporary.width, temporary.height), 0, 0);
                    cover.Apply();

                    RenderTexture.active = active;
                    RenderTexture.ReleaseTemporary(temporary);

                    Data.songCover = Convert.ToBase64String(
                        ImageConversion.EncodeToPNG(cover)
                    );
                }
                catch
                {
                    Data.songCover = null;
                }

                Data.ResetPerformance();

                Data.modifierMultiplier = modifierMultiplier;
                Data.songSpeedMultiplier = songSpeedMul;
                Data.batteryLives = gameEnergyCounter.batteryLives;

                Data.modObstacles = gameplayModifiers.enabledObstacleType.ToString();
                Data.modInstaFail = gameplayModifiers.instaFail;
                Data.modNoFail = gameplayModifiers.noFail;
                Data.modBatteryEnergy = gameplayModifiers.batteryEnergy;
                Data.modDisappearingArrows = gameplayModifiers.disappearingArrows;
                Data.modNoBombs = gameplayModifiers.noBombs;
                Data.modSongSpeed = gameplayModifiers.songSpeed.ToString();
                Data.modNoArrows = gameplayModifiers.noArrows;
                Data.modGhostNotes = gameplayModifiers.ghostNotes;
                Data.modFailOnSaberClash = gameplayModifiers.failOnSaberClash;
                Data.modStrictAngles = gameplayModifiers.strictAngles;
                Data.modFastNotes = gameplayModifiers.fastNotes;

                Data.staticLights = playerSettings.staticLights;
                Data.leftHanded = playerSettings.leftHanded;
                Data.swapColors = playerSettings.swapColors;
                Data.playerHeight = playerSettings.playerHeight;
                Data.disableSFX = playerSettings.disableSFX;
                Data.noHUD = playerSettings.noTextsAndHuds;
                Data.advancedHUD = playerSettings.advancedHud;

                Data.StatusChange(ChangedProperties.AllButNoteCut, "songStart");
            }
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;

            if (gamePauseManager != null)
            {
                RemoveSubscriber(gamePauseManager, "_gameDidPauseSignal", OnGamePause);
                RemoveSubscriber(gamePauseManager, "_gameDidResumeSignal", OnGameResume);
            }

            if (scoreController != null)
            {
                scoreController.noteWasCutEvent -= OnNoteWasCut;
                scoreController.noteWasMissedEvent -= OnNoteWasMissed;
                scoreController.scoreDidChangeEvent -= OnScoreDidChange;
                scoreController.comboDidChangeEvent -= OnComboDidChange;
                scoreController.multiplierDidChangeEvent -= OnMultiplierDidChange;
            }

            if (gameplayManager != null)
            {
                RemoveSubscriber(gameplayManager, "_levelFinishedSignal", OnLevelFinished);
                RemoveSubscriber(gameplayManager, "_levelFailedSignal", OnLevelFailed);
            }

            if (beatmapObjectCallbackController != null)
            {
                beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= OnBeatmapEventDidTrigger;
            }
        }

        private void OnGamePause()
        {
            Data.paused = GetCurrentTime();
            Data.StatusChange(ChangedProperties.Beatmap, "pause");
        }

        private void OnGameResume()
        {
            Data.start = GetCurrentTime() - (long)(audioTimeSyncController.songTime * 1000f / Data.songSpeedMultiplier);
            Data.paused = 0;
            Data.StatusChange(ChangedProperties.Beatmap, "resume");
        }

        private void OnNoteWasCut(NoteData data, NoteCutInfo info, int multiplier)
        {
            SetNoteCutStatus(data, info);

            int score, afterScore, cutDistanceScore;

            ScoreController.ScoreWithoutMultiplier(info, null, out score, out afterScore, out cutDistanceScore);

            Data.initialScore = score;
            Data.finalScore = -1;
            Data.cutMultiplier = multiplier;

            if (data.noteType == NoteType.Bomb)
            {
                Data.passedBombs++;
                Data.hitBombs++;

                Data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "bombCut");
            } else
            {
                Data.passedNotes++;

                if (info.allIsOK)
                {
                    Data.hitNotes++;

                    Data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "noteCut");
                } else
                {
                    Data.missedNotes++;

                    Data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "noteMissed");
                }
                Data.UpdateAccuracy();
            }

            List<AfterCutScoreBuffer> list = (List<AfterCutScoreBuffer>)afterCutScoreBuffersField.GetValue(scoreController);

            foreach (AfterCutScoreBuffer acsb in list)
            {
                if (noteCutInfoField.GetValue(acsb) == info)
                {
                    noteCutMapping.Add(info, data);

                    acsb.didFinishEvent += OnNoteWasFullyCut;
                    break;
                }
            }
        }

        private void OnNoteWasFullyCut(AfterCutScoreBuffer buffer)
        {
            int score, afterScore, cutDistanceScore;

            NoteCutInfo info = (NoteCutInfo)noteCutInfoField.GetValue(buffer);
            NoteData data = noteCutMapping[info];

            noteCutMapping.Remove(info);
            SetNoteCutStatus(data, info);

            ScoreController.ScoreWithoutMultiplier(info, null, out score, out afterScore, out cutDistanceScore);
            int multiplier = (int)afterCutScoreBufferMultiplierField.GetValue(buffer);
            afterScore = (int)afterCutScoreWithMultiplierField.GetValue(buffer) / multiplier;

            Data.initialScore = score;
            Data.finalScore = score + afterScore;
            Data.multiplier = multiplier;

            Data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "noteFullyCut");

            buffer.didFinishEvent -= OnNoteWasFullyCut;
        }

        private void OnNoteWasMissed(NoteData noteData, int multiplier)
        {
            Data.batteryEnergy = gameEnergyCounter.batteryEnergy;

            if (noteData.noteType == NoteType.Bomb)
            {
                Data.passedBombs++;

                Data.StatusChange(ChangedProperties.Performance, "bombMissed");
            }
            else
            {
                Data.passedNotes++;
                Data.missedNotes++;

                Data.StatusChange(ChangedProperties.Performance, "noteMissed");
            }
        }

        private void OnScoreDidChange(int scoreBeforeMultiplier)
        {
            Data.score = ScoreController.GetScoreForGameplayModifiersScoreMultiplier(scoreBeforeMultiplier, Data.modifierMultiplier);

            int currentMaxScoreBeforeMultiplier = ScoreController.MaxScoreForNumberOfNotes(Data.passedNotes);
            Data.currentMaxScore = ScoreController.GetScoreForGameplayModifiersScoreMultiplier(currentMaxScoreBeforeMultiplier, Data.modifierMultiplier);

            RankModel.Rank rank = RankModel.GetRankForScore(scoreBeforeMultiplier, Data.score, currentMaxScoreBeforeMultiplier, Data.currentMaxScore);
            Data.rank = RankModel.GetRankName(rank);

            Data.StatusChange(ChangedProperties.Performance, "scoreChanged");
        }

        private void OnComboDidChange(int combo)
        {
            Data.combo = combo;
            Data.maxCombo = scoreController.maxCombo;

            Data.StatusChange(ChangedProperties.Performance, "comboChange");
        }

        private void OnMultiplierDidChange(int multiplier, float multiplierProgress)
        {
            Data.multiplier = multiplier;
            Data.multiplierProgress = multiplierProgress;

            Data.StatusChange(ChangedProperties.Performance, "multiplierChange");
        }

        private void OnLevelFinished()
        {
            Data.StatusChange(ChangedProperties.Performance, "finished");
        }

        private void OnLevelFailed()
        {
            Data.StatusChange(ChangedProperties.Performance, "failed");
        }

        private void OnBeatmapEventDidTrigger(BeatmapEventData beatmapEventData)
        {
            Data.beatmapEventType = beatmapEventData.type;
            Data.beatmapEventValue = beatmapEventData.value;

            Data.StatusChange(ChangedProperties.BeatmapEvent, "beatmapEvent");
        }

        private static T FindFirstOrDefault<T>() where T : UnityEngine.Object
        {
            T obj = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
            if (obj == null)
            {
                Log("Couldn't find " + typeof(T).FullName);
                throw new InvalidOperationException("Couldn't find " + typeof(T).FullName);
            }
            return obj;
        }

        private void AddSubscriber(object obj, string field, Action action)
        {
            Type t = obj.GetType();
            FieldInfo gameEventField = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (gameEventField == null)
            {
                Log("Can't subscribe to " + t.Name + "." + field);
                return;
            }

            MethodInfo methodInfo = gameEventField.FieldType.GetMethod("Subscribe");
            methodInfo.Invoke(gameEventField.GetValue(obj), new object[] { action });
        }

        private void RemoveSubscriber(object obj, string field, Action action)
        {
            Type t = obj.GetType();
            FieldInfo gameEventField = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (gameEventField == null)
            {
                Log("Can't unsubscribe from " + t.Name + "." + field);
                return;
            }

            MethodInfo methodInfo = gameEventField.FieldType.GetMethod("Unsubscribe");
            methodInfo.Invoke(gameEventField.GetValue(obj), new object[] { action });
        }

        private void SetNoteCutStatus(NoteData noteData, NoteCutInfo noteCutInfo)
        {
            Data.noteID = noteData.id;
            Data.noteType = noteData.noteType.ToString();
            Data.noteCutDirection = noteData.cutDirection.ToString();
            Data.noteLine = noteData.lineIndex;
            Data.noteLayer = (int)noteData.noteLineLayer;
            Data.speedOK = noteCutInfo.speedOK;
            Data.directionOK = noteCutInfo.directionOK;
            Data.saberTypeOK = noteCutInfo.saberTypeOK;
            Data.wasCutTooSoon = noteCutInfo.wasCutTooSoon;
            Data.saberSpeed = noteCutInfo.saberSpeed;
            Data.saberDirX = noteCutInfo.saberDir[0];
            Data.saberDirY = noteCutInfo.saberDir[1];
            Data.saberDirZ = noteCutInfo.saberDir[2];
            Data.saberType = noteCutInfo.saberType.ToString();
            Data.swingRating = noteCutInfo.swingRating;
            Data.timeDeviation = noteCutInfo.timeDeviation;
            Data.cutDirectionDeviation = noteCutInfo.cutDirDeviation;
            Data.cutPointX = noteCutInfo.cutPoint[0];
            Data.cutPointY = noteCutInfo.cutPoint[1];
            Data.cutPointZ = noteCutInfo.cutPoint[2];
            Data.cutNormalX = noteCutInfo.cutNormal[0];
            Data.cutNormalY = noteCutInfo.cutNormal[1];
            Data.cutNormalZ = noteCutInfo.cutNormal[2];
            Data.cutDistanceToCenter = noteCutInfo.cutDistanceToCenter;
        }

        private long GetCurrentTime()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerMillisecond);
        }

        private static void Log(string msg)
        {
            BS_Utils.Utilities.Logger.Log(Constants.Name, msg);
        }

        public void OnUpdate()
        {
            bool currentHeadInObstacle = false;

            if (playerHeadAndObstacleInteraction != null)
            {
                currentHeadInObstacle = playerHeadAndObstacleInteraction.intersectingObstacles.Count > 0;
            }

            if (!headInObstacle && currentHeadInObstacle)
            {
                headInObstacle = true;

                Data.StatusChange(ChangedProperties.Performance, "obstacleEnter");
            }
            else if (headInObstacle && !currentHeadInObstacle)
            {
                headInObstacle = false;

                Data.StatusChange(ChangedProperties.Performance, "obstacleExit");
            }
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
