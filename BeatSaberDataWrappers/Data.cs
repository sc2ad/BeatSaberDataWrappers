using System;

namespace BeatSaberDataWrappers
{
    public class Data
    {
        public static string updateCause;

        public static string scene = "Menu";
        public static bool partyMode = false;
        public static string mode = null;

        // Beatmap
        public static string songName = null;
        public static string songSubName = null;
        public static string songAuthorName = null;
        public static string levelAuthorName = null;
        public static string songCover = null;
        public static string songHash = null;
        public static float songBPM;
        public static float noteJumpSpeed;
        public static long songTimeOffset = 0;
        public static long length = 0;
        public static long start = 0;
        public static long paused = 0;
        public static string difficulty = null;
        public static int notesCount = 0;
        public static int bombsCount = 0;
        public static int obstaclesCount = 0;
        public static int maxScore = 0;
        public static string maxRank = "E";
        public static string environmentName = null;

        // Performance
        public static int score = 0;
        public static int currentMaxScore = 0;
        public static string rank = "E";
        public static float accuracy = 100.0f;
        public static int passedNotes = 0;
        public static int hitNotes = 0;
        public static int missedNotes = 0;
        public static int lastNoteScore = 0;
        public static int passedBombs = 0;
        public static int hitBombs = 0;
        public static int combo = 0;
        public static int maxCombo = 0;
        public static int multiplier = 0;
        public static float multiplierProgress = 0;
        public static int batteryEnergy = 1;

        // Note cut
        public static int noteID = -1;
        public static string noteType = null;
        public static string noteCutDirection = null;
        public static int noteLine = 0;
        public static int noteLayer = 0;
        public static bool speedOK = false;
        public static bool directionOK = false;
        public static bool saberTypeOK = false;
        public static bool wasCutTooSoon = false;
        public static int initialScore = -1;
        public static int finalScore = -1;
        public static int cutMultiplier = 0;
        public static float saberSpeed = 0;
        public static float saberDirX = 0;
        public static float saberDirY = 0;
        public static float saberDirZ = 0;
        public static string saberType = null;
        public static float swingRating = 0;
        public static float timeDeviation = 0;
        public static float cutDirectionDeviation = 0;
        public static float cutPointX = 0;
        public static float cutPointY = 0;
        public static float cutPointZ = 0;
        public static float cutNormalX = 0;
        public static float cutNormalY = 0;
        public static float cutNormalZ = 0;
        public static float cutDistanceToCenter = 0;

        // Mods
        public static float modifierMultiplier = 1f;
        public static string modObstacles = "All";
        public static bool modInstaFail = false;
        public static bool modNoFail = false;
        public static bool modBatteryEnergy = false;
        public static int batteryLives = 1;
        public static bool modDisappearingArrows = false;
        public static bool modNoBombs = false;
        public static string modSongSpeed = "Normal";
        public static float songSpeedMultiplier = 1f;
        public static bool modNoArrows = false;
        public static bool modGhostNotes = false;
        public static bool modFailOnSaberClash = false;
        public static bool modStrictAngles = false;
        public static bool modFastNotes = false;

        // Player settings
        public static bool staticLights = false;
        public static bool leftHanded = false;
        public static bool swapColors = false;
        public static float playerHeight = 17f;
        public static bool disableSFX = false;
        public static bool reduceDebris = false;
        public static bool noHUD = false;
        public static bool advancedHUD = false;

        // Beatmap event
        public static BeatmapEventType beatmapEventType;
        public static int beatmapEventValue = 0;

        public static void UpdateAccuracy()
        {
            accuracy = score / (float)maxScore;
        }

        public static void ResetMapInfo()
        {
            songName = null;
            songSubName = null;
            songAuthorName = null;
            levelAuthorName = null;
            songCover = null;
            songHash = null;
            songBPM = 0f;
            noteJumpSpeed = 0f;
            songTimeOffset = 0;
            length = 0;
            start = 0;
            paused = 0;
            difficulty = null;
            notesCount = 0;
            obstaclesCount = 0;
            maxScore = 0;
            maxRank = "E";
            environmentName = null;
        }

        public static void ResetPerformance()
        {
            score = 0;
            currentMaxScore = 0;
            rank = "E";
            accuracy = 0;
            passedNotes = 0;
            hitNotes = 0;
            missedNotes = 0;
            lastNoteScore = 0;
            passedBombs = 0;
            hitBombs = 0;
            combo = 0;
            maxCombo = 0;
            multiplier = 0;
            multiplierProgress = 0;
            batteryEnergy = 1;
        }

        public static void ResetNoteCut()
        {
            noteID = -1;
            noteType = null;
            noteCutDirection = null;
            speedOK = false;
            directionOK = false;
            saberTypeOK = false;
            wasCutTooSoon = false;
            initialScore = -1;
            finalScore = -1;
            cutMultiplier = 0;
            saberSpeed = 0;
            saberDirX = 0;
            saberDirY = 0;
            saberDirZ = 0;
            saberType = null;
            swingRating = 0;
            timeDeviation = 0;
            cutDirectionDeviation = 0;
            cutPointX = 0;
            cutPointY = 0;
            cutPointZ = 0;
            cutNormalX = 0;
            cutNormalY = 0;
            cutNormalZ = 0;
            cutDistanceToCenter = 0;
        }

        public static event Action<ChangedProperties, string> statusChange;

        public static void StatusChange(ChangedProperties properties, string cause)
        {
            statusChange?.Invoke(properties, cause);
        }
    }
}
