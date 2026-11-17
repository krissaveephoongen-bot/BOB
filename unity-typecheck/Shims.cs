// Offline type-check shims for Unity packages that have no public NuGet reference assembly.
// They are only compiled by unity-typecheck (never by Unity), so two rules apply:
//   1. Mirror the API surface that the game scripts actually touch — nothing more.
//   2. Keep the members faithful enough that a typo or a wrong type still fails the build.
// Packages covered: TextMeshPro, AI Navigation (Unity.AI.Navigation), UnityEditor.Android,
// and the Unity Gaming Services bootstrap (Bob.Ugs), whose real folder this project leaves out.
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace TMPro
{
    public enum TextAlignmentOptions
    {
        TopLeft, Top, TopRight, TopJustified, TopFlush, TopGeoAligned,
        Left, Center, Right, Justified, Flush, CenterGeoAligned,
        BottomLeft, Bottom, BottomRight, BottomJustified, BottomFlush, BottomGeoAligned,
        BaselineLeft, Baseline, BaselineRight, BaselineJustified, BaselineFlush, BaselineGeoAligned,
        MidlineLeft, Midline, MidlineRight, MidlineJustified, MidlineFlush, MidlineGeoAligned,
        CaplineLeft, Capline, CaplineRight, CaplineJustified, CaplineFlush, CaplineGeoAligned
    }

    [System.Flags]
    public enum FontStyles
    {
        Normal = 0, Bold = 1, Italic = 2, Underline = 4, LowerCase = 8, UpperCase = 16,
        SmallCaps = 32, Strikethrough = 64, Superscript = 128, Subscript = 256, Highlight = 512
    }

    public enum TextWrappingModes { NoWrap, Normal, PreserveWhitespace, PreserveWhitespaceNoWrap }

    public enum TextOverflowModes { Overflow, Ellipsis, Masking, Truncate, ScrollRect, Page, Linked }

    public class TMP_FontAsset : Object { }

    /// <summary>Base of every TMP label. Derives from MaskableGraphic so GetComponent/AddComponent and the
    /// Graphic members (color, raycastTarget, rectTransform) behave like they do in Unity.</summary>
    public class TMP_Text : MaskableGraphic
    {
        public string text { get; set; }
        public TMP_FontAsset font { get; set; }
        public float fontSize { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public FontStyles fontStyle { get; set; }
        public TextWrappingModes textWrappingMode { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public bool richText { get; set; }
        public float lineSpacing { get; set; }
        public float characterSpacing { get; set; }
        public float outlineWidth { get; set; }
        public Color outlineColor { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public float alpha { get; set; }
        public Material fontMaterial { get { return null; } }
    }

    public class TextMeshProUGUI : TMP_Text { }
    public class TextMeshPro : TMP_Text { }
}

namespace Unity.AI.Navigation
{
    public enum CollectObjects { All, Volume, Children, MarkedWithModifier }

    public class NavMeshSurface : MonoBehaviour
    {
        public NavMeshData navMeshData { get; set; }
        public CollectObjects collectObjects { get; set; }
        public NavMeshCollectGeometry useGeometry { get; set; }
        public int agentTypeID { get; set; }
        public int defaultArea { get; set; }
        public void BuildNavMesh() { }
        public void RemoveData() { }
        public void AddData() { }
        public void UpdateNavMesh(bool asyncUpdate = false) { }
    }

    public class NavMeshLink : MonoBehaviour
    {
        public Vector3 startPoint { get; set; }
        public Vector3 endPoint { get; set; }
        public float width { get; set; }
        public bool bidirectional { get; set; }
        public float costModifier { get; set; }
        public bool activated { get; set; }
        public int area { get; set; }
        public int agentTypeID { get; set; }
        public bool autoUpdate { get; set; }
    }

    public class NavMeshModifier : MonoBehaviour
    {
        public bool ignoreFromBuild { get; set; }
        public bool overrideArea { get; set; }
        public int area { get; set; }
        public bool applyToChildren { get; set; }
        public bool overrideGenerateLinks { get; set; }
        public bool generateLinks { get; set; }
    }
}

namespace UnityEditor.Android
{
    /// <summary>Gradle post-processor interface (com.unity.mobile.android). The game only needs the one callback.</summary>
    public interface IPostGenerateGradleAndroidProject
    {
        int callbackOrder { get; }
        void OnPostGenerateGradleAndroidProject(string path);
    }
}

namespace UnityEngine
{
    /// <summary>Device vibration lives in UnityEngine.VibrationModule, which the public reference
    /// assemblies do not ship. Only the call the game makes is mirrored.</summary>
    public static class Handheld
    {
        public static void Vibrate() { }
    }
}

namespace Bob.Ugs
{
    /// <summary>Cloud Save mirror: the wardrobe stores the ids of the cosmetics it granted (real file left out here).</summary>
    public class PlayerProfileData
    {
        public int coins = 0;
        public int level = 1;
        public System.Collections.Generic.List<string> unlockedItems = new System.Collections.Generic.List<string>();
    }

    /// <summary>Mirror of the members the game screens touch; the real manager needs the UGS SDK.</summary>
    public class UgsCloudSaveManager : MonoBehaviour
    {
        public static UgsCloudSaveManager Instance { get; private set; }
        public PlayerProfileData Profile { get; private set; }
        public bool IsLoaded { get; private set; }
        public System.Threading.Tasks.Task<bool> UnlockItemAndSaveAsync(string itemId) { return null; }
    }
    /// <summary>The real bootstrap needs the Unity Gaming Services SDK (unity/Assets/Scripts/UGS is left out of this
    /// project). Game screens reference it for the S02 guest sign-in, so mirror the members they touch.</summary>
    public class UgsBootstrapManager : MonoBehaviour
    {
        public static UgsBootstrapManager Instance { get; private set; }
        public bool signInOnAwake = true;
        public bool IsReady { get; private set; }
        public string PlayerId { get; private set; }
        public string LastError { get; private set; }
        public System.Threading.Tasks.Task KickoffAsync() => null;
        public System.Threading.Tasks.Task EnsureReadyAsync() => null;
    }

    /// <summary>The board card (UGS/BobBoard.cs) is SDK code and stays out of this project; mirror the three plain types
    /// the HUD reads, so the card compiles without the Leaderboards assembly.</summary>
    public sealed class BobBoardRow
    {
        public int Rank;
        public string Name;
        public int Score;
        public bool Self;
    }

    public sealed class BobBoardSnapshot
    {
        public readonly System.Collections.Generic.List<BobBoardRow> Rows = new System.Collections.Generic.List<BobBoardRow>();
        public bool SignedIn;
        public int SelfRank = -1;
        public int SelfScore;
        public string Error;
    }

    public static class BobBoardService
    {
        public static System.Threading.Tasks.Task<BobBoardSnapshot> LoadAsync(int limit = 10) => null;
    }

    /// <summary>The real reporter (UGS/BobRaceScoreReporter.cs) posts a finished match's score to the leaderboard.
    /// The Task Race manager only needs Attach, so that is all this mirrors.</summary>
    public class BobRaceScoreReporter : MonoBehaviour
    {
        public static BobRaceScoreReporter Attach(Bob.TaskRace.TaskRaceModeManager owner) => null;
    }

    /// <summary>The real room (UGS/BobOnlineRoom.cs) needs the Lobby, Relay and Netcode assemblies. Mirror only what the
    /// screens and the Task Race scene read: the state they draw, the two events they listen to, and the calls they make.</summary>
    public sealed class BobOnlineRoom : MonoBehaviour
    {
        public const float SeriesRoundSeconds = 100f;
        public const int SeriesWinsToTake = 2;
        public static BobOnlineRoom Instance { get; private set; }
        public static BobOnlineRoom Ensure() => null;
        public bool InRoom { get; private set; }
        public bool IsHost { get; private set; }
        public bool Connected { get; private set; }
        public int LocalNetId { get; private set; }
        public int Players { get; private set; }
        public int MaxPlayers { get; private set; }
        public string Code { get; private set; }
        public string Status { get; private set; }
        public bool Starting { get; private set; }
        public int StartSeed { get; private set; }
        public string HeroId { get; private set; }
        public System.Collections.Generic.IReadOnlyList<string> Roster { get; private set; }
        public Bob.TaskRace.StorySeries Series { get; private set; }
        public event System.Action<int, Vector3, float> PoseReceived;
        public event System.Action<int, string, bool> TaskReceived;
        public event System.Action<Bob.TaskRace.SeriesRound> SeriesResultApplied;
        public event System.Action<int, int, float> SeriesRoundRequested;
        public event System.Action SeriesAbandoned;
        public System.Threading.Tasks.Task<bool> HostAsync(string heroId, int maxPlayers) => null;
        public System.Threading.Tasks.Task<bool> JoinAsync(string heroId, string code) => null;
        public System.Threading.Tasks.Task LeaveAsync() => null;
        public void StartMatch(int seed, int players) { }
        public void SendPose(Vector3 position, float yaw) { }
        public void SendTask(string spotId, bool completed) { }
        public void SendSeriesResult(Bob.TaskRace.SeriesRound line) { }
        public void SendSeriesRound(int round, int seed, float seconds) { }
        public void AbandonSeries() { }
    }
}
