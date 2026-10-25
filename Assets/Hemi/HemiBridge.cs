using System;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Bridge between the game and the Hemi web layer (WebGLTemplates/Hemi/TemplateData/hemi.js).
// Builds its own overlay UI: the "save your run" prompt and the onchain leaderboard on the main menu.
public class HemiBridge : MonoBehaviour
{
    public static HemiBridge Instance { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int Hemi_IsAvailable();
    [DllImport("__Internal")] private static extern void Hemi_SubmitRun(int score, int escaped);
    [DllImport("__Internal")] private static extern void Hemi_FetchLeaderboard();
    private static bool Available => Hemi_IsAvailable() == 1;
#else
    private static bool Available => false;
    private static void Hemi_SubmitRun(int score, int escaped) { }
    private static void Hemi_FetchLeaderboard() { }
#endif

    [Serializable] private class StatusMsg { public string status; }
    [Serializable] private class SubmitResult { public bool ok; public string error; public string txHash; public bool badge; }
    [Serializable] private class LeaderboardEntry { public string player; public int score; }
    [Serializable] private class LeaderboardMsg { public bool ok; public LeaderboardEntry[] entries; public int totalRuns; public int totalEscapes; }

    private static readonly Color Gold = new Color(1f, 0.83f, 0.43f);
    private static readonly Color Lava = new Color(1f, 0.35f, 0.12f);

    private int pendingScore;
    private bool pendingEscaped;
    private bool hasPending;
    private bool submitting;
    private bool fontApplied;

    private GameObject submitPanel;
    private GameObject leaderboardPanel;
    private TextMeshProUGUI runText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI leaderboardText;
    private TextMeshProUGUI skipLabel;
    private Button submitButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject("HemiBridge"); // name must match the SendMessage target in hemi.js
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<HemiBridge>();
    }

    void Awake()
    {
        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem();
        if (!fontApplied) ApplySceneFont();
        bool menu = scene.name.Contains("MainMenu");
        leaderboardPanel.SetActive(menu);
        submitPanel.SetActive(false);
        if (menu)
        {
            RefreshLeaderboard();
            if (hasPending) ShowSubmitPanel();
        }
    }

    // --- Game API ---

    // Remember the finished run; it is offered for saving now (win) or on the main menu (game over)
    public void QueueRun(int score, bool escaped)
    {
        pendingScore = score;
        pendingEscaped = escaped;
        hasPending = true;
    }

    public void ShowSubmitPanel()
    {
        if (!hasPending) return;
        runText.text = "Score " + pendingScore.ToString("D6") + (pendingEscaped ? "   <color=#ffd36e>ESCAPED!</color>" : "");
        statusText.text = Available
            ? "Free: you only sign a message, our relayer pays the gas."
            : "Saving onchain only works in the web build.";
        submitButton.gameObject.SetActive(true);
        submitButton.interactable = Available;
        skipLabel.text = "Skip";
        submitPanel.SetActive(true);
    }

    private void OnSubmitClicked()
    {
        if (submitting || !hasPending) return;
        submitting = true;
        submitButton.interactable = false;
        statusText.text = "Opening wallet...";
        Hemi_SubmitRun(pendingScore, pendingEscaped ? 1 : 0);
    }

    private void OnSkipClicked()
    {
        if (submitting) return;
        hasPending = false;
        submitPanel.SetActive(false);
    }

    private void RefreshLeaderboard()
    {
        if (!Available)
        {
            leaderboardText.text = "<size=80%>The onchain leaderboard loads in the web build.</size>";
            return;
        }
        leaderboardText.text = "Loading...";
        Hemi_FetchLeaderboard();
    }

    // --- Called from hemi.js via SendMessage ---

    public void OnSubmitStatus(string json)
    {
        statusText.text = JsonUtility.FromJson<StatusMsg>(json).status;
    }

    public void OnSubmitResult(string json)
    {
        submitting = false;
        SubmitResult r = JsonUtility.FromJson<SubmitResult>(json);
        if (r.ok)
        {
            hasPending = false;
            statusText.text = "<color=#7dff9a>Saved on Hemi!</color>  tx " + Short(r.txHash)
                + (r.badge ? "\n<color=#ffd36e>Soulbound ESCAPED badge minted to your wallet!</color>" : "");
            submitButton.gameObject.SetActive(false);
            skipLabel.text = "Close";
            if (leaderboardPanel.activeSelf) RefreshLeaderboard();
        }
        else
        {
            statusText.text = "<color=#ff7d7d>" + r.error + "</color>";
            submitButton.interactable = true;
        }
    }

    public void OnLeaderboard(string json)
    {
        LeaderboardMsg m = JsonUtility.FromJson<LeaderboardMsg>(json);
        if (!m.ok)
        {
            leaderboardText.text = "<size=80%>Leaderboard unavailable right now.</size>";
            return;
        }
        StringBuilder sb = new StringBuilder();
        if (m.entries == null || m.entries.Length == 0) sb.Append("No runs yet.\nBe the first!\n");
        else
        {
            for (int i = 0; i < m.entries.Length; i++)
            {
                string row = (i + 1).ToString().PadLeft(2) + ".  " + Short(m.entries[i].player) + "   " + m.entries[i].score.ToString("D6");
                sb.Append(i == 0 ? "<color=#ffd36e>" + row + "</color>" : row).Append('\n');
            }
        }
        sb.Append("\n<size=70%>").Append(m.totalRuns).Append(" runs  |  ").Append(m.totalEscapes).Append(" escapes</size>");
        leaderboardText.text = sb.ToString();
    }

    // --- UI construction ---

    private static string Short(string hex)
    {
        return string.IsNullOrEmpty(hex) || hex.Length < 12 ? hex : hex.Substring(0, 6) + "..." + hex.Substring(hex.Length - 4);
    }

    // Reuse the game's pixel font (taken from the scene UI) so the overlay matches the art style
    private void ApplySceneFont()
    {
        foreach (TextMeshProUGUI t in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.font == null || t.GetComponentInParent<HemiBridge>() != null) continue;
            foreach (TextMeshProUGUI mine in GetComponentsInChildren<TextMeshProUGUI>(true)) mine.font = t.font;
            fontApplied = true;
            return;
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("HemiCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // above WinPanel and menus
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        submitPanel = Panel(canvasGo.transform, "SubmitPanel", new Vector2(0.5f, 0.5f), new Vector2(780, 420), Vector2.zero);
        Label(submitPanel.transform, "SAVE YOUR RUN ON HEMI", 44, new Vector2(0, 150), new Vector2(720, 60), Gold);
        runText = Label(submitPanel.transform, "", 36, new Vector2(0, 80), new Vector2(720, 50), Color.white);
        statusText = Label(submitPanel.transform, "", 26, new Vector2(0, -15), new Vector2(720, 120), new Color(0.85f, 0.85f, 0.85f));
        submitButton = MakeButton(submitPanel.transform, "Save onchain (free)", new Vector2(-180, -150), Lava, OnSubmitClicked, out _);
        MakeButton(submitPanel.transform, "Skip", new Vector2(190, -150), new Color(0.3f, 0.3f, 0.3f), OnSkipClicked, out skipLabel);
        submitPanel.SetActive(false);

        leaderboardPanel = Panel(canvasGo.transform, "LeaderboardPanel", new Vector2(1f, 0.5f), new Vector2(520, 640), new Vector2(-40, 0));
        Label(leaderboardPanel.transform, "HEMI LEADERBOARD", 38, new Vector2(0, 270), new Vector2(480, 60), Gold);
        leaderboardText = Label(leaderboardPanel.transform, "", 24, new Vector2(0, -40), new Vector2(480, 500), Color.white);
        leaderboardText.alignment = TextAlignmentOptions.Top;
        leaderboardText.textWrappingMode = TextWrappingModes.NoWrap;
        leaderboardPanel.SetActive(false);
    }

    private static GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        go.GetComponent<Image>().color = new Color(0.07f, 0.04f, 0.03f, 0.94f);
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = Lava;
        outline.effectDistance = new Vector2(3, -3);
        return go;
    }

    private static TextMeshProUGUI Label(Transform parent, string text, float fontSize, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeButton(Transform parent, string text, Vector2 position, Color color, UnityAction onClick, out TextMeshProUGUI label)
    {
        GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340, 70);
        rt.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        label = Label(go.transform, text, 22, Vector2.zero, rt.sizeDelta, Color.white);
        return button;
    }
}
