using Mysqlx.Crud;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace BOTBUDDY_CYBERSECURITY_CHATBOT
{
    // ================= ACTIVITY LOG CLASS =================
    public class ActivityLog
    {
        private List<ActivityEntry> _entries = new List<ActivityEntry>();
        private readonly int _maxEntries = 100;

        public void Log(string action, string details = "")
        {
            var entry = new ActivityEntry
            {
                Timestamp = DateTime.Now,
                Action = action,
                Details = details
            };
            _entries.Insert(0, entry);

            if (_entries.Count > _maxEntries)
            {
                _entries = _entries.Take(_maxEntries).ToList();
            }
        }

        public List<ActivityEntry> GetEntries(int count = 10)
        {
            return _entries.Take(count).ToList();
        }

        public int EntryCount => _entries.Count;

        public string GetSummary(int count = 10)
        {
            if (_entries.Count == 0)
                return "📋 No activity logged yet.";

            var recent = _entries.Take(count).ToList();
            var result = new List<string>();
            result.Add($"📋 **RECENT ACTIVITY LOG** (Last {recent.Count} actions)");
            result.Add("");

            for (int i = 0; i < recent.Count; i++)
            {
                var entry = recent[i];
                string timeStr = entry.Timestamp.ToString("HH:mm:ss");
                string detailsStr = string.IsNullOrEmpty(entry.Details) ? "" : $" - {entry.Details}";
                result.Add($"  {i + 1}. [{timeStr}] {entry.Action}{detailsStr}");
            }

            if (_entries.Count > count)
            {
                result.Add("");
                result.Add($"  ... and {_entries.Count - count} more actions. Say 'Show full log' to see all.");
            }

            return string.Join("\n", result);
        }

        public string GetFullLog()
        {
            if (_entries.Count == 0)
                return "📋 No activity logged yet.";

            var result = new List<string>();
            result.Add($"📋 **FULL ACTIVITY LOG** ({_entries.Count} total actions)");
            result.Add("");

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                string timeStr = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                string detailsStr = string.IsNullOrEmpty(entry.Details) ? "" : $" - {entry.Details}";
                result.Add($"  {i + 1}. [{timeStr}] {entry.Action}{detailsStr}");
            }

            return string.Join("\n", result);
        }

        public void Clear()
        {
            _entries.Clear();
            Log("Log Cleared", "All activity entries were cleared");
        }

        public class ActivityEntry
        {
            public DateTime Timestamp { get; set; }
            public string Action { get; set; } = string.Empty;
            public string Details { get; set; } = string.Empty;
        }
    }

    // ================= RECYCLE BIN ITEM CLASS =================
    public class RecycleBinItem
    {
        public string Type { get; set; } = string.Empty; // "Conversation", "Task", "Reminder"
        public string Content { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
        public string OriginalContext { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();

        public RecycleBinItem(string type, string content, string originalContext = "")
        {
            Type = type;
            Content = content;
            DeletedAt = DateTime.Now;
            OriginalContext = originalContext;
            IsSelected = false;
        }
}
    // ================= RECYCLE BIN CLASS =================
    public class RecycleBin
    {
        private List<RecycleBinItem> _items = new List<RecycleBinItem>();
        private readonly int _maxItems = 1000;

        public event EventHandler? ItemsChanged;

        public void AddItem(RecycleBinItem item)
        {
            _items.Insert(0, item);
            if (_items.Count > _maxItems)
                _items.RemoveAt(_items.Count - 1);
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddConversation(string content)
        {
            AddItem(new RecycleBinItem("Conversation", content));
        }

        public void AddTask(string content)
        {
            AddItem(new RecycleBinItem("Task", content));
        }

        public void AddReminder(string content)
        {
            AddItem(new RecycleBinItem("Reminder", content));
        }

        public List<RecycleBinItem> GetItems()
        {
            return _items.ToList();
        }

        public List<RecycleBinItem> GetSelectedItems()
        {
            return _items.Where(i => i.IsSelected).ToList();
        }

        public void ToggleSelection(string uniqueId)
        {
            var item = _items.FirstOrDefault(i => i.UniqueId == uniqueId);
            if (item != null)
            {
                item.IsSelected = !item.IsSelected;
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SelectAll()
        {
            foreach (var item in _items)
                item.IsSelected = true;
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void DeselectAll()
        {
            foreach (var item in _items)
                item.IsSelected = false;
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public int RestoreSelected(Action<string, string> restoreAction)
        {
            var selected = _items.Where(i => i.IsSelected).ToList();
            int count = selected.Count;

            foreach (var item in selected)
            {
                restoreAction(item.Type, item.Content);
                _items.Remove(item);
            }

            ItemsChanged?.Invoke(this, EventArgs.Empty);
            return count;
        }

        public int RestoreAll(Action<string, string> restoreAction)
        {
            int count = _items.Count;
            var allItems = _items.ToList();

            foreach (var item in allItems)
            {
                restoreAction(item.Type, item.Content);
                _items.Remove(item);
            }

            ItemsChanged?.Invoke(this, EventArgs.Empty);
            return count;
        }

        public int EmptyBin(Action<string>? onDeletePermanent = null)
        {
            int count = _items.Count;
            var allItems = _items.ToList();

            foreach (var item in allItems)
            {
                onDeletePermanent?.Invoke(item.Content);
                _items.Remove(item);
            }

            ItemsChanged?.Invoke(this, EventArgs.Empty);
            return count;
        }

        public int DeleteSelected(Action<string>? onDeletePermanent = null)
        {
            var selected = _items.Where(i => i.IsSelected).ToList();
            int count = selected.Count;

            foreach (var item in selected)
            {
                // Call the callback to remove from main lists
                onDeletePermanent?.Invoke(item.Content);
                _items.Remove(item);
            }

            ItemsChanged?.Invoke(this, EventArgs.Empty);
            return count;
        }

        public int Count => _items.Count;

        public void RemoveItem(string uniqueId)
        {
            var item = _items.FirstOrDefault(i => i.UniqueId == uniqueId);
            if (item != null)
            {
                _items.Remove(item);
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    // ================= USER MEMORY CLASS =================
    public class UserMemory
    {
        public string UserName { get; set; } = string.Empty;
        public Dictionary<string, int> TopicInterestCount { get; set; }
        public List<string> FavoriteTopics { get; set; }
        public List<string> RecentlyDiscussedTopics { get; set; }
        public string LastSentiment { get; set; }
        public HashSet<string> CoveredTopics { get; set; }
        public Dictionary<string, int> FavoriteTopicMessageIndices { get; set; }
        public string LastDiscussedCyberKeyword { get; set; }
        public HashSet<string> TipsUsed { get; set; }
        public HashSet<string> ExamplesUsed { get; set; }
        public HashSet<string> MoreDetailsUsed { get; set; }
        public Dictionary<string, int> TopicRequestCount { get; set; }
        public string CurrentEmotion { get; set; } = "neutral";
        public List<string> EmotionHistory { get; set; } = new List<string>();
        public Dictionary<string, int> EmotionTriggers { get; set; } = new Dictionary<string, int>();

        // NEW: Track individual tip indices per topic
        private Dictionary<string, List<int>> _usedTipIndices = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        public UserMemory()
        {
            UserName = string.Empty;
            TopicInterestCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            FavoriteTopics = new List<string>();
            RecentlyDiscussedTopics = new List<string>();
            LastSentiment = "neutral";
            CoveredTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            FavoriteTopicMessageIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            LastDiscussedCyberKeyword = string.Empty;
            TipsUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ExamplesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            MoreDetailsUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TopicRequestCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            CurrentEmotion = "neutral";
            EmotionHistory = new List<string>();
            EmotionTriggers = new Dictionary<string, int>();
            _usedTipIndices = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetName(string name)
        {
            if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(UserName))
                UserName = name;
        }

        public void IncrementTopicInterest(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return;
            if (TopicInterestCount.ContainsKey(topic))
                TopicInterestCount[topic]++;
            else
                TopicInterestCount[topic] = 1;
            if (TopicInterestCount[topic] >= 3 && !FavoriteTopics.Contains(topic, StringComparer.OrdinalIgnoreCase))
                FavoriteTopics.Add(topic);
            RecentlyDiscussedTopics.Insert(0, topic);
            if (RecentlyDiscussedTopics.Count > 5)
                RecentlyDiscussedTopics.RemoveAt(RecentlyDiscussedTopics.Count - 1);
            LastDiscussedCyberKeyword = topic;
        }

        public void IncrementTopicRequest(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return;
            if (TopicRequestCount.ContainsKey(topic))
                TopicRequestCount[topic]++;
            else
                TopicRequestCount[topic] = 1;

            bool allThreeUsed = IsTipUsed(topic) && IsExampleUsed(topic) && IsMoreUsed(topic);
            bool requestedTwice = TopicRequestCount[topic] >= 4;

            if ((allThreeUsed || requestedTwice) && !FavoriteTopics.Contains(topic, StringComparer.OrdinalIgnoreCase))
            {
                FavoriteTopics.Add(topic);
            }
        }

        public bool IsFavoriteTopic(string topic) => FavoriteTopics.Any(f => f.Equals(topic, StringComparison.OrdinalIgnoreCase));

        public string GetPersonalizedGreeting()
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                if (FavoriteTopics.Count > 0)
                    return $"Welcome back {UserName}! I see you're interested in {string.Join(", ", FavoriteTopics.Take(2))}. Ready to learn more?";
                return $"Welcome back {UserName}! Ready to explore cybersecurity?";
            }
            return string.Empty;
        }

        public void MarkTopicCovered(string topic) { if (!string.IsNullOrEmpty(topic)) { CoveredTopics.Add(topic); LastDiscussedCyberKeyword = topic; } }
        public bool IsTopicCovered(string topic) => CoveredTopics.Contains(topic);

        // Legacy Tip methods (kept for compatibility)
        public void MarkTipUsed(string topic) { if (!string.IsNullOrEmpty(topic)) TipsUsed.Add(topic); }
        public bool IsTipUsed(string topic) => TipsUsed.Contains(topic);

        public void MarkExampleUsed(string topic) { if (!string.IsNullOrEmpty(topic)) ExamplesUsed.Add(topic); }
        public bool IsExampleUsed(string topic) => ExamplesUsed.Contains(topic);

        public void MarkMoreUsed(string topic) { if (!string.IsNullOrEmpty(topic)) MoreDetailsUsed.Add(topic); }
        public bool IsMoreUsed(string topic) => MoreDetailsUsed.Contains(topic);

        public void RecordFavoriteMessageIndex(string topic, int messageIndex)
        { if (!FavoriteTopicMessageIndices.ContainsKey(topic)) FavoriteTopicMessageIndices[topic] = messageIndex; }

        public int? GetFavoriteMessageIndex(string topic) => FavoriteTopicMessageIndices.TryGetValue(topic, out int index) ? index : (int?)null;

        public string GetRandomFavoriteRecall()
        {
            if (FavoriteTopics.Count == 0) return string.Empty;
            var random = new Random();
            string randomFavorite = FavoriteTopics[random.Next(FavoriteTopics.Count)];
            bool isCovered = CoveredTopics.Contains(randomFavorite);
            string coverageStatus = isCovered ? "You've already learned the definition!" : "You haven't covered the definition yet.";
            string[] recallMessages = { $"🌟 Speaking of which, I remembered you said you like {randomFavorite}! {coverageStatus}", $"💭 By the way, {randomFavorite} is one of your favorites! {coverageStatus}", $"🎯 Oh! And you're interested in {randomFavorite}. {coverageStatus}", $"📚 Just remembered - {randomFavorite} is on your favorites list! {coverageStatus}" };
            return recallMessages[random.Next(recallMessages.Length)];
        }

        public string GetCoverageStatusText(string topic) => IsTopicCovered(topic) ? "✅ COVERED" : "❌ NOT COVERED";

        // ================= NEW TIP TRACKING METHODS =================

        /// <summary>
        /// Marks a specific tip index as used for a topic
        /// </summary>
        public void MarkTipUsed(string topic, int tipIndex)
        {
            if (string.IsNullOrEmpty(topic)) return;

            if (!_usedTipIndices.ContainsKey(topic))
                _usedTipIndices[topic] = new List<int>();

            if (!_usedTipIndices[topic].Contains(tipIndex))
                _usedTipIndices[topic].Add(tipIndex);

            // Also mark in the legacy TipsUsed for compatibility
            MarkTipUsed(topic);
        }

        /// <summary>
        /// Checks if a specific tip index has been used for a topic
        /// </summary>
        public bool IsTipUsed(string topic, int tipIndex)
        {
            if (string.IsNullOrEmpty(topic)) return false;
            return _usedTipIndices.ContainsKey(topic) && _usedTipIndices[topic].Contains(tipIndex);
        }

        /// <summary>
        /// Gets the next unused tip index in order (0-based)
        /// Returns -1 if all tips have been used
        /// </summary>
        public int GetNextTipIndex(string topic, CybersecurityKnowledgeBase knowledgeBase)
        {
            if (string.IsNullOrEmpty(topic)) return -1;

            var allTips = knowledgeBase.GetAllTipsForTopic(topic);
            if (allTips == null || allTips.Length == 0) return -1;

            if (!_usedTipIndices.ContainsKey(topic))
                return 0;

            var usedIndices = _usedTipIndices[topic];

            if (usedIndices.Count >= allTips.Length)
                return -1; // All tips used

            // Find the next unused tip in order (0, 1, 2, 3, 4...)
            for (int i = 0; i < allTips.Length; i++)
            {
                if (!usedIndices.Contains(i))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Gets the count of used tips for a topic
        /// </summary>
        public int GetTipCount(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return 0;
            if (!_usedTipIndices.ContainsKey(topic))
                return 0;
            return _usedTipIndices[topic].Count;
        }

        /// <summary>
        /// Gets the total number of tips available for a topic
        /// </summary>
        public int GetTotalTipCount(string topic, CybersecurityKnowledgeBase knowledgeBase)
        {
            if (string.IsNullOrEmpty(topic)) return 0;
            var allTips = knowledgeBase.GetAllTipsForTopic(topic);
            return allTips?.Length ?? 0;
        }

        /// <summary>
        /// Checks if all tips for a topic have been used
        /// </summary>
        public bool AllTipsUsed(string topic, CybersecurityKnowledgeBase knowledgeBase)
        {
            if (string.IsNullOrEmpty(topic)) return false;

            var allTips = knowledgeBase.GetAllTipsForTopic(topic);
            if (allTips == null || allTips.Length == 0) return false;

            if (!_usedTipIndices.ContainsKey(topic))
                return false;

            return _usedTipIndices[topic].Count >= allTips.Length;
        }

        /// <summary>
        /// Resets tip tracking for a specific topic
        /// </summary>
        public void ResetTipTracking(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return;
            if (_usedTipIndices.ContainsKey(topic))
                _usedTipIndices[topic].Clear();
        }

        /// <summary>
        /// Resets all tip tracking
        /// </summary>
        public void ResetAllTipTracking()
        {
            _usedTipIndices.Clear();
        }
    }

    // ================= CONVERSATION STATE TRACKER =================
    public class ConversationStateTracker
    {
        public string CurrentTopic { get; set; }
        public int DefinitionPart { get; set; }
        public List<string> TopicsExplored { get; set; }
        public string LastBotMessage { get; set; }
        public bool AwaitingFollowUp { get; set; }
        public string FollowUpType { get; set; }
        public bool HasGreeted { get; set; }
        public bool HasAskedHowAreYou { get; set; }
        public ConversationStateTracker()
        {
            CurrentTopic = string.Empty;
            DefinitionPart = 0;
            TopicsExplored = new List<string>();
            LastBotMessage = string.Empty;
            AwaitingFollowUp = false;
            FollowUpType = string.Empty;
            HasGreeted = false;
            HasAskedHowAreYou = false;
        }
        public void Reset() { CurrentTopic = string.Empty; DefinitionPart = 0; AwaitingFollowUp = false; FollowUpType = string.Empty; }
        public void FullReset()
        {
            CurrentTopic = string.Empty;
            DefinitionPart = 0;
            TopicsExplored.Clear();
            LastBotMessage = string.Empty;
            AwaitingFollowUp = false;
            FollowUpType = string.Empty;
            HasGreeted = false;
            HasAskedHowAreYou = false;
        }
    }

    // ================= KNOWLEDGE BASE CLASS =================
    public class CybersecurityKnowledgeBase
    {
        private readonly Dictionary<string, (string Part1, string Part2, string Part3)> _definitions;
        private readonly Dictionary<string, string[]> _tips;
        private readonly Random _random = new Random();
        public CybersecurityKnowledgeBase() { _definitions = InitializeDefinitions(); _tips = InitializeTips(); }
        public string[] GetAllTipsForTopic(string topic) => _tips.TryGetValue(topic, out var tips) ? tips : null;
        public bool TryGetDefinition(string term, out (string Part1, string Part2, string Part3) definition) => _definitions.TryGetValue(term, out definition);
        public bool HasTips(string term) => _tips.ContainsKey(term);
        public string? GetRandomTip(string term) => _tips.TryGetValue(term, out var tips) && tips.Length > 0 ? tips[_random.Next(tips.Length)] : null;
        public IEnumerable<string> GetAllTerms() => _definitions.Keys;
        public List<string> GetRandomTerms(int count) => _definitions.Keys.OrderBy(_ => _random.Next()).Take(count).ToList();
        public List<string> GetSuggestedTopics(string currentTopic, int count = 3)
        {
            var all = GetAllTerms().ToList();
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { currentTopic };
            return all.Where(t => !excluded.Contains(t)).OrderBy(_ => _random.Next()).Take(count).ToList();
        }
        public List<string> GetDynamicHelpTopics()
        {
            var allTopics = GetAllTerms().ToList();
            return allTopics.OrderBy(_ => _random.Next()).Take(6).ToList();
        }

        private Dictionary<string, (string Part1, string Part2, string Part3)> InitializeDefinitions()
        {
            return new Dictionary<string, (string Part1, string Part2, string Part3)>(StringComparer.OrdinalIgnoreCase)
    {
        { "phishing", ( "Phishing is a cyberattack where criminals impersonate trusted entities to trick you into revealing sensitive information such as passwords or banking details.", "For example, you might receive an email that looks like it's from your bank, urging you to click a link and log in; in reality, the link leads to a fake site designed to steal your credentials.", "Common variants include email-based attacks, highly targeted attempts against specific individuals, and SMS or message-based impersonation used to steal data." ) },
        { "spear phishing", ( "Spear phishing is a targeted form of deception where attackers customize their messages to a specific individual or organization.", "For example, an attacker might send an email to a company executive that references a real recent project to appear legitimate.", "Variants include targeted email attacks and business email compromise schemes." ) },
        { "smishing", ( "Smishing is a type of deception conducted through SMS text messages.", "For example, you might receive a text claiming your account is locked, accompanied by a link to 'verify' your details.", "Clicking the link either installs malware on your device or leads to a fake login page." ) },
        { "vishing", ( "Vishing, or voice phishing, occurs when attackers use phone calls to trick victims into revealing sensitive information.", "For example, a caller may pretend to be from your bank's fraud department, claiming suspicious activity on your account.", "Variants include automated robocalls and live impersonation calls." ) },
        { "password", ( "A password is a secret string of characters used to verify the identity of a user and protect access to accounts and systems.", "For example, using 'P@ssw0rd123!' to log into your email account is far more secure than a simple guessable word like 'password'.", "Passwords can include passphrases, OTPs, and complex combinations to prevent brute-force attacks." ) },
        { "passwords", ( "Passwords are secret strings of characters used to verify identity and protect access to accounts.", "For example, using 'P@ssw0rd123!' to log into your email account is far more secure than a simple guessable word.", "Best practices include using unique passwords for each account and enabling multi-factor authentication." ) },
        { "malware", ( "Malware is malicious software designed to damage, disrupt, or gain unauthorized access to systems.", "For example, downloading a free program from an untrusted site may install spyware or a trojan.", "Types include viruses, worms, ransomware, spyware, and trojans." ) },
        { "ransomware", ( "Ransomware is malware that encrypts files and demands payment for decryption.", "For example, opening a malicious attachment can lock all your documents instantly.", "A famous example is WannaCry, which spread globally and encrypted systems for ransom." ) },
        { "spyware", ( "Spyware is software that secretly monitors your activity and collects personal information without your consent.", "For example, a free browser toolbar might log your passwords and browsing history in the background.", "Variants include keyloggers, adware, and tracking cookies." ) },
        { "trojan horse", ( "A Trojan horse is malware that disguises itself as legitimate, useful software.", "For example, downloading what appears to be a free utility tool may secretly install harmful code.", "Variants include backdoor trojans and banking trojans." ) },
        { "worm", ( "A computer worm is a self-replicating type of malware that spreads across networks without user interaction.", "For example, the Conficker worm spread rapidly by exploiting Windows vulnerabilities.", "Variants include email worms, internet worms, and file-sharing worms." ) },
        { "virus", ( "A computer virus attaches itself to legitimate files or programs and spreads when those files are shared.", "For example, opening an infected email attachment can unleash the virus onto your system.", "Variants include file infectors, macro viruses, and boot sector viruses." ) },
        { "botnet", ( "A botnet is a network of infected computers controlled remotely by attackers, often without the owners' knowledge.", "For example, the Mirai botnet infected IoT devices and disrupted major websites.", "Variants include spam-sending botnets, DDoS botnets, and cryptomining botnets." ) },
        { "denial of service", ( "A Denial of Service (DoS) attack floods a system with excessive traffic to overwhelm its resources.", "For example, attackers might send thousands of requests per second to a website until it crashes.", "Variants include application-layer DoS and protocol-based DoS." ) },
        { "distributed denial of service", ( "A Distributed Denial of Service (DDoS) attack uses multiple compromised systems to flood a target with traffic.", "For example, thousands of infected devices might send requests at the same time, overwhelming the server.", "Variants include volumetric attacks, protocol attacks, and application-layer attacks." ) },
        { "brute force attack", ( "A brute force attack involves systematically trying every possible password combination until the correct one is found.", "For example, attackers use automated tools that rapidly guess login credentials.", "Variants include simple brute force, dictionary attacks, and hybrid brute force." ) },
        { "sql injection", ( "SQL injection is a web attack where malicious SQL code is inserted into input fields to manipulate a database.", "For example, entering `' OR 1=1` into a login field may trick the database into bypassing authentication.", "Variants include classic SQL injection, blind SQL injection, and time-based SQL injection." ) },
        { "cross-site scripting", ( "Cross-Site Scripting (XSS) allows attackers to inject malicious scripts into trusted websites.", "For example, a comment field on a blog that does not properly filter input could accept JavaScript code.", "Variants include stored XSS, reflected XSS, and DOM-based XSS." ) },
        { "man-in-the-middle attack", ( "A Man-in-the-Middle (MITM) attack occurs when an attacker intercepts communication between two parties.", "For example, on unsecured public Wi-Fi, an attacker can capture login credentials as they travel between a user and a website.", "Variants include session hijacking, SSL stripping, and packet sniffing." ) },
        { "zero-day exploit", ( "A zero-day exploit targets a software vulnerability unknown to the vendor with no patch available.", "For example, a previously unknown flaw in a web browser could allow remote code execution.", "Variants include application zero-days, operating system zero-days, and browser zero-days." ) },
        { "patch management", ( "Patch management is the process of regularly updating software to fix known vulnerabilities.", "For example, applying security patches to a Windows server prevents exploitation of recently discovered flaws.", "Variants include automated patching and manual patching." ) },
        { "intrusion detection system", ( "An Intrusion Detection System (IDS) monitors network traffic for suspicious behavior and alerts administrators.", "For example, it might detect repeated failed login attempts that indicate a brute force attack.", "Variants include network-based IDS and host-based IDS." ) },
        { "intrusion prevention system", ( "An Intrusion Prevention System (IPS) detects and actively blocks suspicious activity in real time.", "For example, it might automatically ban traffic from a malicious IP address.", "Variants include signature-based IPS and anomaly-based IPS." ) },
        { "antivirus", ( "Antivirus software detects, quarantines, and removes malicious software from computers.", "For example, it scans files for known virus signatures and quarantines infected files.", "Variants include signature-based antivirus and heuristic antivirus." ) },
        { "endpoint security", ( "Endpoint security protects individual devices such as laptops, desktops, and smartphones from cyber threats.", "For example, a comprehensive endpoint solution includes antivirus, personal firewalls, and device management.", "Variants include Endpoint Detection and Response (EDR) and Mobile Device Management (MDM)." ) },
        { "password hygiene", ( "Password hygiene refers to best practices for creating, managing, and storing passwords securely.", "For example, using a unique, complex password for each account prevents a breach from compromising multiple services.", "Practices include using strong passwords, enabling MFA, and using a password manager." ) },
        { "password manager", ( "A password manager securely stores, generates, and fills in complex passwords for all your accounts.", "For example, it can create a random 16-character password for each site and remember it for you.", "Variants include cloud-based managers and local managers." ) },
        { "encryption", ( "Encryption converts readable data into a coded format that can only be deciphered with the correct key.", "For example, WhatsApp messages are encrypted end-to-end so only the intended recipient can read them.", "Variants include symmetric encryption and asymmetric encryption." ) },
        { "tls ssl", ( "TLS and SSL are cryptographic protocols that secure communication over the internet.", "For example, when you see a padlock icon and HTTPS in your browser, TLS is protecting your data.", "Variants include TLS 1.2 and TLS 1.3." ) },
        { "https", ( "HTTPS uses TLS/SSL encryption to ensure communication between your browser and a website remains private.", "For example, logging into your bank's website with HTTPS protects your username and password.", "Always look for HTTPS before entering sensitive information online." ) },
        { "digital certificate", ( "A digital certificate verifies the identity of a website, organization, or individual online.", "For example, your bank's digital certificate proves that the site is authentic.", "Variants include SSL/TLS certificates and code-signing certificates." ) },
        { "public key infrastructure", ( "PKI is the system that manages digital certificates and encryption keys.", "For example, PKI allows secure email communication by verifying the sender's identity.", "Components include Certificate Authorities and key management systems." ) },
        { "social engineering", ( "Social engineering manipulates people into divulging confidential information or performing actions.", "For example, an attacker may pose as IT support and ask for your password over the phone.", "Variants include pretexting, baiting, and quid pro quo attacks." ) },
        { "pretexting", ( "Pretexting is a social engineering technique where attackers create a false scenario to obtain information.", "For example, pretending to be a bank employee to verify account details.", "Variants include phone-based pretexting and email-based pretexting." ) },
        { "baiting", ( "Baiting lures victims with a promise of something desirable to deliver malware.", "For example, leaving a USB drive labeled 'Confidential' in a company parking lot.", "Variants include physical baiting and digital baiting." ) },
        { "tailgating", ( "Tailgating is a physical attack where an attacker follows someone with legitimate access through a secure door.", "For example, walking through a secure door right after an employee swipes their badge.", "Also known as piggybacking." ) },
        { "insider threat", ( "An insider threat occurs when someone within an organization misuses their authorized access to cause harm.", "For example, a disgruntled employee stealing customer data before leaving the company.", "Variants include malicious insiders and negligent insiders." ) },
        { "cyber hygiene", ( "Cyber hygiene refers to routine practices that keep computer systems and data secure.", "For example, regularly updating software, using strong passwords, and avoiding suspicious links.", "Includes personal and organizational cyber hygiene practices." ) },
        { "safe browsing", ( "Safe browsing means adopting habits that reduce your risk of encountering malicious websites or scams.", "For example, always double-check URLs and only use trusted websites for sensitive activities.", "Includes browser security settings and safe search filters." ) },
        { "identity theft", ( "Identity theft occurs when an attacker steals personal information to impersonate someone.", "For example, using your Social Security number to open credit accounts in your name.", "Variants include financial identity theft and medical identity theft." ) },
        { "data breach", ( "A data breach is an incident where sensitive information is accessed, copied, or stolen without authorization.", "For example, hackers breaking into a retailer's database and stealing millions of customer records.", "Variants include accidental breaches and malicious breaches." ) },
        { "cloud security", ( "Cloud security protects data, applications, and infrastructure hosted in cloud environments.", "For example, encrypting files stored in cloud services and enforcing access controls.", "Includes data encryption, identity management, and continuous monitoring." ) },
        { "multi-factor authentication", ( "Multi-Factor Authentication requires two or more verification factors to gain access to an account.", "For example, a password plus a fingerprint scan or a one-time code sent via SMS.", "Variants include biometrics, hardware tokens, and software-based codes." ) },
        { "biometrics", ( "Biometrics uses unique physical or behavioral characteristics to authenticate identity.", "For example, unlocking your phone with a fingerprint or your face.", "Variants include fingerprint, facial, iris, and voice recognition." ) },
        { "cloud misconfiguration", ( "Cloud misconfiguration occurs when cloud services are set up incorrectly, exposing data or resources.", "For example, leaving an S3 storage bucket publicly readable with sensitive customer information.", "Variants include access misconfiguration and encryption misconfiguration." ) },
        { "cyber espionage", ( "Cyber espionage uses cyberattacks to steal confidential information for political or economic advantage.", "For example, state-sponsored hackers targeting government agencies for diplomatic secrets.", "Variants include political espionage and corporate espionage." ) },
        { "cyber warfare", ( "Cyber warfare refers to politically motivated cyberattacks to disrupt or destroy critical infrastructure.", "For example, disabling a power grid through malware during a time of conflict.", "Variants include infrastructure attacks and propaganda campaigns." ) },
        { "advanced persistent threat", ( "An APT is a prolonged, stealthy cyberattack where an intruder gains access and remains undetected for extended periods.", "For example, attackers infiltrating a defense contractor's network and exfiltrating documents over months.", "Typically involves state-sponsored groups." ) },
        { "keylogger", ( "A keylogger is spyware that records every keystroke made on a device.", "For example, a malicious program running in the background logs everything you type.", "Variants include hardware keyloggers and software keyloggers." ) },
        { "rootkit", ( "A rootkit hides its presence and provides privileged access to an attacker.", "For example, it might conceal other malware so antivirus cannot detect them.", "Variants include kernel-mode rootkits and application-level rootkits." ) },
        { "cryptojacking", ( "Cryptojacking is the unauthorized use of someone's computer processing power to mine cryptocurrency.", "For example, visiting a compromised website that runs a JavaScript miner in your browser.", "Variants include browser-based and malware-based cryptojacking." ) },
        { "supply chain attack", ( "A supply chain attack targets vulnerabilities in third-party vendors or software components.", "For example, attackers inserting malicious code into a software update distributed to thousands of customers.", "A well-known example is the SolarWinds attack." ) },
        { "watering hole attack", ( "A watering hole attack compromises a website frequently visited by a specific target group.", "For example, infecting a professional forum that defense contractors often use.", "Visitors to the compromised site unknowingly download malware." ) },
        { "drive-by download", ( "A drive-by download occurs when a user unintentionally downloads malware by visiting a compromised website.", "For example, clicking on a malicious advertisement can trigger an exploit that installs spyware.", "Variants include browser-based and plugin-based drive-by downloads." ) },
        { "session hijacking", ( "Session hijacking occurs when an attacker steals a user's session token to impersonate them.", "For example, stealing cookies from a browser to gain access to online accounts.", "Variants include cookie hijacking and TCP session hijacking." ) },
        { "cyber forensics", ( "Cyber forensics involves collecting, preserving, and analyzing digital evidence for investigating cybercrimes.", "For example, tracing the origin of a malware attack by examining log files.", "Variants include network forensics, computer forensics, and mobile device forensics." ) },
        { "cybersecurity", ( "Cybersecurity protects systems, networks, and data from digital attacks.", "For example, a company deploys firewalls, antivirus, and employee training to defend against hackers.", "Branches include network, application, information, and operational security." ) },
        { "vpn", ( "A VPN (Virtual Private Network) creates a secure, encrypted connection over the internet.", "For example, using a VPN on public Wi-Fi prevents others from seeing your online activity.", "VPNs protect your privacy by hiding your IP address and encrypting your data." ) },
        { "privacy", ( "Privacy in cybersecurity refers to protecting personal information from unauthorized access.", "For example, adjusting social media settings to control who sees your posts.", "Good privacy practices include using VPNs, encrypted messaging, and being careful what you share online." ) },
        { "scam", ( "A scam is a fraudulent scheme designed to trick people into giving money or information.", "For example, fake lottery winnings or tech support calls claiming your computer is infected.", "Always verify unexpected requests through official channels before responding." ) }
    };
        }

        private Dictionary<string, string[]> InitializeTips()
        {
            return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "phishing", new string[] { "🚨 Never click links in suspicious emails - verify the sender directly.", "🔍 Check the sender's email address carefully for misspellings.", "📞 If in doubt, contact the organization using their official website or phone number." } },
        { "spear phishing", new string[] { "🎯 Be extra cautious with emails that reference personal or company-specific information.", "🔍 Verify unexpected requests through a different communication channel.", "📧 Never share sensitive information via email without verifying the recipient." } },
        { "smishing", new string[] { "📱 Never click links in text messages from unknown numbers.", "🚫 Don't reply to suspicious texts with personal information.", "📞 Call the organization directly using their official number if concerned." } },
        { "vishing", new string[] { "📞 Never give out personal information over the phone to unsolicited callers.", "🔍 Hang up and call back using official numbers from the organization's website.", "⚠️ Be suspicious of urgent calls demanding immediate action." } },
        { "password", new string[] { "🔐 Use strong unique passwords for each account (minimum 12 characters).", "🤫 Never share your passwords with anyone.", "🗄️ Use a password manager to generate and store secure passwords." } },
        { "passwords", new string[] { "🔐 Create unique, complex passwords for every account.", "🔄 Change passwords immediately if you suspect a breach.", "🗄️ Use a password manager to track all your passwords securely." } },
        { "malware", new string[] { "🛡️ Keep your antivirus software updated at all times.", "📥 Only download from trusted sources.", "💾 Keep backups of important files in case of infection." } },
        { "ransomware", new string[] { "💾 Maintain regular offline backups of all important data.", "🛡️ Use reputable antivirus and anti-malware software.", "📧 Never open suspicious email attachments or links." } },
        { "spyware", new string[] { "🛡️ Run regular antivirus and anti-spyware scans.", "📥 Only download software from official sources.", "🔍 Check browser extensions and remove unknown ones." } },
        { "trojan horse", new string[] { "📥 Only download software from trusted official sources.", "🛡️ Use antivirus software that detects trojans.", "⚠️ Be wary of 'free' software that seems too good to be true." } },
        { "worm", new string[] { "🛡️ Keep all software and operating systems patched and updated.", "🔒 Use firewalls to prevent unauthorized network access.", "📥 Avoid opening suspicious email attachments." } },
        { "virus", new string[] { "🛡️ Install and update antivirus software regularly.", "📥 Scan all downloads and email attachments before opening.", "💾 Keep backups of important files." } },
        { "botnet", new string[] { "🛡️ Use strong passwords on all devices to prevent compromise.", "🔒 Keep devices updated with the latest security patches.", "📱 Don't ignore unusual device behavior (slow performance, overheating)." } },
        { "denial of service", new string[] { "🛡️ Use DDoS protection services for business websites.", "🔍 Monitor network traffic for unusual patterns.", "📋 Have an incident response plan ready for attacks." } },
        { "distributed denial of service", new string[] { "🌐 Use CDN and DDoS mitigation services.", "🔍 Implement rate limiting and traffic filtering.", "📋 Have a response plan to communicate with users during attacks." } },
        { "brute force attack", new string[] { "🔐 Use long, complex passwords (12+ characters).", "🔒 Implement account lockout after multiple failed attempts.", "📱 Enable multi-factor authentication everywhere possible." } },
        { "sql injection", new string[] { "🛡️ Use parameterized queries and input validation.", "🔒 Sanitize all user inputs on your web applications.", "🔍 Regularly test your web applications for vulnerabilities." } },
        { "cross-site scripting", new string[] { "🛡️ Encode user input and output in web applications.", "🔒 Use Content Security Policy (CSP) headers.", "🔍 Regularly test for XSS vulnerabilities." } },
        { "man-in-the-middle attack", new string[] { "🌐 Never use public Wi-Fi for sensitive transactions.", "🔒 Always look for HTTPS before entering personal information.", "📱 Use a VPN on all public networks." } },
        { "zero-day exploit", new string[] { "🔄 Keep all software updated with the latest patches.", "🛡️ Use layered security with antivirus and firewalls.", "📋 Have an incident response plan for zero-day attacks." } },
        { "patch management", new string[] { "🔄 Enable automatic updates where possible.", "📅 Regularly review and apply security patches.", "🔍 Prioritize critical patches for known exploits." } },
        { "intrusion detection system", new string[] { "🛡️ Monitor IDS alerts regularly for suspicious activity.", "🔍 Tune IDS to reduce false positives.", "📋 Have a response plan for detected intrusions." } },
        { "intrusion prevention system", new string[] { "🛡️ Configure IPS to block known malicious traffic.", "🔍 Review IPS logs to improve security rules.", "📋 Regularly update IPS signatures." } },
        { "antivirus", new string[] { "🛡️ Keep antivirus definitions updated daily.", "📥 Run regular full-system scans.", "🔍 Use real-time protection features." } },
        { "endpoint security", new string[] { "🛡️ Use comprehensive endpoint protection on all devices.", "🔒 Enforce device encryption and access controls.", "📱 Keep all endpoint software updated." } },
        { "password hygiene", new string[] { "🔐 Use unique passwords for every account.", "🔄 Change passwords if you suspect a breach.", "🗄️ Never write passwords down or share them." } },
        { "password manager", new string[] { "🔑 Use a reputable password manager with strong encryption.", "🔄 Enable MFA on your password manager account.", "📱 Use the password manager's built-in password generator." } },
        { "encryption", new string[] { "🔐 Encrypt sensitive files on all devices.", "📱 Use encrypted messaging apps for private communication.", "💾 Ensure backups are encrypted too." } },
        { "tls ssl", new string[] { "🔒 Always check for HTTPS and padlock icon in browsers.", "🔄 Keep browsers updated for latest security protocols.", "🌐 Use only websites with valid SSL/TLS certificates." } },
        { "https", new string[] { "🔒 Always ensure websites use HTTPS before entering data.", "🔄 Look for the padlock icon in the address bar.", "⚠️ Avoid entering personal info on non-HTTPS sites." } },
        { "digital certificate", new string[] { "🔍 Verify digital certificates before trusting websites.", "⚠️ Don't ignore browser warnings about invalid certificates.", "🔄 Keep certificate revocation lists updated." } },
        { "public key infrastructure", new string[] { "🔒 Use PKI for secure communication and identity verification.", "🔍 Regularly audit certificate authorities.", "📋 Maintain proper certificate lifecycle management." } },
        { "social engineering", new string[] { "🤔 Always verify unexpected requests through different channels.", "📞 Hang up and call back using official numbers.", "🔒 Never give passwords or verification codes over the phone." } },
        { "pretexting", new string[] { "🔍 Always verify the identity of anyone requesting information.", "📞 Hang up and call back through official channels.", "⚠️ Be cautious of unsolicited requests for personal data." } },
        { "baiting", new string[] { "⚠️ Never plug unknown USB drives into your computer.", "📥 Don't download free software from unknown sources.", "🔍 Verify the source of any 'free' offers." } },
        { "tailgating", new string[] { "🚪 Always close doors behind you in secure areas.", "🔒 Politely challenge strangers attempting to follow you.", "📋 Report tailgating incidents to security personnel." } },
        { "insider threat", new string[] { "🔒 Limit access to the minimum necessary for each role.", "🔍 Monitor for unusual access patterns and behavior.", "📋 Conduct regular security training for all employees." } },
        { "cyber hygiene", new string[] { "🔄 Regular updates and patches for all software.", "🔐 Use strong, unique passwords and MFA.", "📥 Practice safe browsing and email habits." } },
        { "safe browsing", new string[] { "🔍 Always check URLs for typos or suspicious domains.", "🔄 Keep browsers and extensions updated.", "⚠️ Use ad-blockers and avoid clicking pop-up ads." } },
        { "identity theft", new string[] { "🔒 Monitor your credit reports and bank statements regularly.", "🔐 Use strong, unique passwords for all financial accounts.", "📞 Report any suspicious activity to authorities immediately." } },
        { "data breach", new string[] { "🔄 Change passwords immediately if you're affected by a breach.", "🔒 Enable MFA on all accounts after a breach.", "📋 Monitor your accounts for suspicious activity." } },
        { "cloud security", new string[] { "🔒 Encrypt sensitive data before uploading to the cloud.", "🔍 Regularly audit cloud permissions and access controls.", "🔄 Enable MFA for all cloud admin accounts." } },
        { "multi-factor authentication", new string[] { "🔑 Enable MFA on all accounts that support it.", "📱 Use authenticator apps instead of SMS when possible.", "🔒 Use hardware security keys for critical accounts." } },
        { "biometrics", new string[] { "🔒 Use biometrics as one factor, not the only factor.", "📱 Enable biometrics for device security where available.", "🔍 Be aware that biometrics can't be changed if compromised." } },
        { "cloud misconfiguration", new string[] { "🔍 Regularly audit cloud configuration settings.", "📋 Use automated tools to detect misconfigurations.", "🔒 Follow the principle of least privilege for cloud access." } },
        { "cyber espionage", new string[] { "🔒 Use encryption for sensitive communications.", "🔍 Monitor for unusual network activity.", "📋 Implement strict access controls for sensitive data." } },
        { "cyber warfare", new string[] { "🛡️ Protect critical infrastructure with robust security measures.", "📋 Have a national-level incident response plan.", "🔒 Use air-gapped systems for the most sensitive assets." } },
        { "advanced persistent threat", new string[] { "🔍 Continuous monitoring for unusual activity.", "🔒 Segment networks to limit lateral movement.", "📋 Regularly review and update security policies." } },
        { "keylogger", new string[] { "🛡️ Use reputable antivirus that detects keyloggers.", "🔍 Monitor for unknown programs running on your device.", "📱 Use on-screen keyboards for sensitive entries." } },
        { "rootkit", new string[] { "🛡️ Use robust antivirus with rootkit detection.", "🔍 Regularly scan for hidden processes and files.", "🔒 Use secure boot and trusted platform modules." } },
        { "cryptojacking", new string[] { "🛡️ Use ad-blockers to prevent browser-based mining.", "🔍 Monitor system performance for unexpected slowdowns.", "🔄 Keep browsers and plugins updated." } },
        { "supply chain attack", new string[] { "🔍 Vet all third-party vendors and their security practices.", "🔄 Regularly audit software updates for integrity.", "📋 Use software composition analysis tools." } },
        { "watering hole attack", new string[] { "🛡️ Keep browsers and plugins updated.", "🔍 Use DNS filtering to block malicious domains.", "🔄 Regularly scan for malware even on trusted sites." } },
        { "drive-by download", new string[] { "🔄 Keep browsers and plugins updated.", "🛡️ Use browser security features and ad-blockers.", "🔍 Avoid visiting suspicious or untrusted websites." } },
        { "session hijacking", new string[] { "🔒 Use HTTPS for all web transactions.", "🔐 Logout of accounts when finished, especially on shared devices.", "📱 Use MFA to prevent unauthorized access even with stolen tokens." } },
        { "cyber forensics", new string[] { "📋 Preserve evidence by creating disk images, not modifying original data.", "🔍 Document the chain of custody for all evidence.", "📱 Use specialized forensic tools for analysis." } },
        { "cybersecurity", new string[] { "🛡️ Stay informed about current threats and vulnerabilities.", "🔒 Practice good security habits in your daily life.", "📋 Regular security training and awareness." } },
        { "vpn", new string[] { "🌐 Always use a VPN on public Wi-Fi networks.", "🔒 Choose a VPN that doesn't keep logs.", "📱 Install VPN apps on all your devices." } },
        { "privacy", new string[] { "🔒 Use encrypted messaging for private conversations.", "🌐 Adjust social media privacy settings.", "📱 Be careful what personal information you share online." } },
        { "scam", new string[] { "🔍 Always verify unexpected requests for money or information.", "📞 Contact the organization directly using official channels.", "⚠️ Be suspicious of urgent requests and too-good-to-be-true offers." } }
    };
        }
    }

    // ================= KEYWORD MATCHER CLASS =================
    public class KeywordMatcher
    {
        private readonly HashSet<string> _greetings, _howAreYouKeywords, _positiveKeywords, _sadKeywords, _worriedKeywords, _angryKeywords, _exampleKeywords, _moreDetailsKeywords, _helpKeywords, _otherTopicsKeywords, _nameSetKeywords, _likeLoveKeywords, _interestedKeywords, _userPositiveStatements, _positiveNoHowAreYou, _anxiousKeywords, _frustratedKeywords, _curiousKeywords;
        public KeywordMatcher()
        {
            _greetings = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hi", "hello", "hey", "morning", "what's up", "good morning", "good afternoon", "good evening" };
            _howAreYouKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "how are you", "how are you doing", "how's it going", "how is it going", "how's life", "how do you feel", "how are you today", "and you", "what about you", "how about you", "you?" };
            _positiveKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "good", "great", "fine", "awesome", "wonderful", "fantastic", "excellent", "amazing" };
            _sadKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sad", "depressed", "unhappy", "down", "upset", "miserable", "terrible", "awful" };
            _worriedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "worried", "concerned", "anxious", "nervous", "afraid", "scared", "stressed", "overwhelmed" };
            _angryKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "angry", "furious", "mad", "annoyed", "irritated", "frustrated", "outraged" };
            _exampleKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "example", "examples", "show me an example", "give me an example", "what's an example" };
            _moreDetailsKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "more", "more details", "more information", "tell me more", "elaborate", "keep going", "what else" };
            _helpKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "help", "what can you help with", "what topics can you explain", "suggest", "recommend", "what can you do", "guide me" };
            _otherTopicsKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "other", "other topics", "something else", "different topic", "new topic", "another topic" };
            _nameSetKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "my name is", "call me", "you can call me", "i prefer you call me", "please call me" };
            _likeLoveKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "i like", "i love", "enjoy", "fascinated by", "passionate about", "my favorite topic is", "my favourite topic is" };
            _interestedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "interested in", "want to learn", "would like to know", "curious about", "tell me about" };
            _userPositiveStatements = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "i am good", "i'm good", "i am fine", "i'm fine", "i am doing good", "i'm doing good", "i am doing well", "i'm doing well", "doing good", "doing well", "i am great", "i'm great", "i am okay", "i'm okay", "feeling good", "feeling fine", "all good", "pretty good" };
            _positiveNoHowAreYou = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "good", "fine", "okay", "great", "alright", "not bad", "doing well", "i'm good", "i am good", "pretty good", "very good", "ok", "yep", "yeah", "awesome", "cool", "super", "perfect", "excellent", "fantastic", "wonderful", "amazing" };
            _anxiousKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "anxious", "anxiety", "panicking", "panic", "uneasy" };
            _frustratedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "frustrated", "frustrating", "annoying", "tired of", "sick of", "fed up" };
            _curiousKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "curious", "curiosity", "wondering", "i wonder", "what happens if", "how does that work", "why does", "interesting", "fascinating" };
        }
        public bool IsGreeting(string input) => _greetings.Any(g => input.Contains(g, StringComparison.OrdinalIgnoreCase));
        public bool IsHowAreYouQuestion(string input) => _howAreYouKeywords.Any(h => input.Contains(h, StringComparison.OrdinalIgnoreCase));
        public bool IsPositive(string input) => _positiveKeywords.Any(p => input.Contains(p, StringComparison.OrdinalIgnoreCase));
        public bool IsSad(string input) => _sadKeywords.Any(s => input.Contains(s, StringComparison.OrdinalIgnoreCase));
        public bool IsWorried(string input) => _worriedKeywords.Any(w => input.Contains(w, StringComparison.OrdinalIgnoreCase));
        public bool IsAngry(string input) => _angryKeywords.Any(a => input.Contains(a, StringComparison.OrdinalIgnoreCase));
        public bool IsAnxious(string input) => _anxiousKeywords.Any(a => input.Contains(a, StringComparison.OrdinalIgnoreCase));
        public bool IsFrustrated(string input) => _frustratedKeywords.Any(f => input.Contains(f, StringComparison.OrdinalIgnoreCase));
        public bool IsCurious(string input) => _curiousKeywords.Any(c => input.Contains(c, StringComparison.OrdinalIgnoreCase));
        public bool IsExampleRequest(string input) => _exampleKeywords.Any(e => input.Contains(e, StringComparison.OrdinalIgnoreCase));
        public bool IsMoreDetailsRequest(string input) => _moreDetailsKeywords.Any(m => input.Contains(m, StringComparison.OrdinalIgnoreCase));
        public bool IsHelpRequest(string input) => _helpKeywords.Any(h => input.Contains(h, StringComparison.OrdinalIgnoreCase));
        public bool HasWhatIs(string input)
        {
            string lower = input.ToLowerInvariant();
            return lower.Contains("what is") || lower.Contains("explanation of") || lower.Contains("define") || lower.Contains("definition of") || lower.Contains("describe") || lower.Contains("explain") || lower.Contains("tell me about");
        }
        public bool IsNameSetting(string input)
        {
            string lowerInput = input.ToLowerInvariant();
            return lowerInput.Contains("my name is") || lowerInput.Contains("call me") || lowerInput.Contains("you can call me") || lowerInput.Contains("i prefer you call me") || lowerInput.Contains("please call me");
        }
        public bool IsLikeLoveStatement(string input) => _likeLoveKeywords.Any(l => input.Contains(l, StringComparison.OrdinalIgnoreCase));
        public bool IsInterestedInTopic(string input) => _interestedKeywords.Any(i => input.Contains(i, StringComparison.OrdinalIgnoreCase));
        public bool IsUserPositive(string input) => _userPositiveStatements.Any(p => input.Contains(p, StringComparison.OrdinalIgnoreCase));
        public bool IsShortPositive(string input) => _positiveNoHowAreYou.Any(p => input.Contains(p, StringComparison.OrdinalIgnoreCase));
        public string? ExtractName(string input)
        {
            string lowerInput = input.ToLowerInvariant();
            if (lowerInput.Contains("my name is"))
            {
                int index = lowerInput.IndexOf("my name is") + 10;
                if (index < input.Length)
                {
                    string afterKeyword = input.Substring(index).Trim();
                    string name = afterKeyword.Split(new[] { ' ', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                    if (!string.IsNullOrEmpty(name) && name.Length >= 2 && char.IsLetter(name[0])) return name;
                }
            }
            if (lowerInput.Contains("call me"))
            {
                int index = lowerInput.IndexOf("call me") + 7;
                if (index < input.Length)
                {
                    string afterKeyword = input.Substring(index).Trim();
                    string name = afterKeyword.Split(new[] { ' ', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                    if (!string.IsNullOrEmpty(name) && name.Length >= 2 && char.IsLetter(name[0])) return name;
                }
            }
            if (lowerInput.Contains("you can call me"))
            {
                int index = lowerInput.IndexOf("you can call me") + 15;
                if (index < input.Length)
                {
                    string afterKeyword = input.Substring(index).Trim();
                    string name = afterKeyword.Split(new[] { ' ', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                    if (!string.IsNullOrEmpty(name) && name.Length >= 2 && char.IsLetter(name[0])) return name;
                }
            }
            return null;
        }
        public string ExtractAfterEmotion(string input, string emotion)
        {
            int emotionIndex = input.IndexOf(emotion, StringComparison.OrdinalIgnoreCase);
            if (emotionIndex >= 0)
            {
                string after = input.Substring(emotionIndex + emotion.Length);
                if (after.ToLowerInvariant().Contains("about") || after.ToLowerInvariant().Contains("because") || after.ToLowerInvariant().Contains("of")) return after.Trim();
            }
            return string.Empty;
        }
    }

    // ================= CONVERSATION CONTEXT CLASS =================
    public class ConversationContext
    {
        public string UserDisplayName { get; set; }
        public List<(string Sender, string Message)> Messages { get; set; }
        public UserMemory Memory { get; set; }
        public ConversationContext() { UserDisplayName = "User"; Messages = new List<(string, string)>(); Memory = new UserMemory(); }
        public void Reset() { Messages.Clear(); }
    }

    // ================= OPTIMISED FOLLOW-UP HANDLER =================
    public class FollowUpHandler
    {
        private readonly CybersecurityKnowledgeBase _knowledgeBase;
        private readonly ConversationStateTracker _stateTracker;
        private readonly Random _random = new();
        private static readonly HashSet<string> MorePatterns = new(StringComparer.OrdinalIgnoreCase) { "tell me more", "more about", "elaborate", "continue", "keep going", "go on", "what else", "anything else", "tell me more about", "explain more", "i want to learn more", "teach me more", "more information", "more details" };
        private static readonly HashSet<string> TipPatterns = new(StringComparer.OrdinalIgnoreCase) { "another tip", "more tips", "other tips", "give me another tip", "another advice", "more advice", "another suggestion", "any other tips", "give me a tip", "share a tip", "another" };
        private static readonly HashSet<string> ExamplePatterns = new(StringComparer.OrdinalIgnoreCase) { "another example", "more examples", "other example", "give me another example", "another instance", "show me another", "give me an example" };
        private static readonly HashSet<string> ExplainPatterns = new(StringComparer.OrdinalIgnoreCase) { "explain more", "explain further", "further explain", "more explanation", "in more detail", "deeper explanation", "break it down more" };

        // Track shown tips per topic (legacy)
        private Dictionary<string, HashSet<int>> _shownTipIndices = new(StringComparer.OrdinalIgnoreCase);

        public FollowUpHandler(CybersecurityKnowledgeBase knowledgeBase, ConversationStateTracker stateTracker)
        {
            _knowledgeBase = knowledgeBase;
            _stateTracker = stateTracker;
        }

        public string? Handle(string input, UserMemory memory)
        {
            string lower = input.ToLowerInvariant();
            string currentTopic = _stateTracker.CurrentTopic ?? memory.LastDiscussedCyberKeyword;
            if (MorePatterns.Any(p => lower.Contains(p))) return HandleMore(currentTopic, memory);
            if (TipPatterns.Any(p => lower.Contains(p))) return HandleTip(currentTopic, memory);
            if (ExamplePatterns.Any(p => lower.Contains(p))) return HandleExample(currentTopic, memory);
            if (ExplainPatterns.Any(p => lower.Contains(p))) return HandleExplain(currentTopic, memory);
            return null;
        }

        private string HandleMore(string topic, UserMemory memory)
        {
            if (string.IsNullOrEmpty(topic)) return "I'd love to tell you more! What cybersecurity topic interests you? 🔐";
            _stateTracker.CurrentTopic = topic;
            if (_knowledgeBase.TryGetDefinition(topic, out var definition))
            {
                memory.IncrementTopicRequest(topic);

                // ALWAYS show More details (Part 3) when "More" is clicked
                memory.MarkMoreUsed(topic);
                return $"{definition.Part3}\n\n💡 Want a practical tip? Just say 'give me a tip'!";
            }
            return "Try asking about phishing, passwords, malware, or 2FA! 🔐";
        }

        private string HandleTip(string topic, UserMemory memory)
        {
            if (string.IsNullOrEmpty(topic)) return "What topic would you like a tip about? Try 'tip about phishing'! 🔐";
            _stateTracker.CurrentTopic = topic;

            // Get the next tip index in order
            int nextTipIndex = memory.GetNextTipIndex(topic, _knowledgeBase);

            if (nextTipIndex == -1)
            {
                // All tips used - show 0 tips remaining message
                return $"0 tips remaining for '{topic}'. Upgrade to BotBuddy Premium for more tips or explore other cybersecurity topics";
            }

            var allTips = _knowledgeBase.GetAllTipsForTopic(topic);
            if (allTips == null || allTips.Length == 0)
            {
                return $"💡 Use strong unique passwords and enable 2FA everywhere!";
            }

            string tip = allTips[nextTipIndex];

            memory.MarkTipUsed(topic, nextTipIndex);
            memory.IncrementTopicRequest(topic);

            int usedTips = memory.GetTipCount(topic);
            int totalTips = allTips.Length;

            // Check if this was the last tip
            if (usedTips >= totalTips)
            {
                return $"0 tips remaining for '{topic}'. Upgrade to BotBuddy Premium for more tips or explore other cybersecurity topics";
            }

            // Return only the tip content - NO progress text
            return tip;
        }

        private string HandleExample(string topic, UserMemory memory)
        {
            if (string.IsNullOrEmpty(topic)) return "What topic would you like an example of? Ask 'example of phishing'! 📚";
            if (_knowledgeBase.TryGetDefinition(topic, out var definition))
            {
                _stateTracker.CurrentTopic = topic;
                memory.MarkExampleUsed(topic);
                memory.IncrementTopicRequest(topic);
                return $"{definition.Part2}\n\n🔍 Want more details? Just say 'tell me more' or 'more details'!";
            }
            return "I couldn't find an example for that. Try a different topic!";
        }

        private string HandleExplain(string topic, UserMemory memory)
        {
            if (string.IsNullOrEmpty(topic)) return "Which cybersecurity concept would you like me to elaborate on? 🎓";
            if (_knowledgeBase.TryGetDefinition(topic, out var definition))
            {
                _stateTracker.CurrentTopic = topic;
                memory.MarkMoreUsed(topic);
                memory.IncrementTopicRequest(topic);
                return $"🔍 Let me explain {topic.ToUpper()} in more detail:\n{definition.Part3}\n\n💡 Want a practical tip as well? Say 'give me a tip'!";
            }
            return "I couldn't find more info on that. Want to try a different topic?";
        }

    }
    // ================= BOT RESPONSE GENERATOR =================
    public class BotResponseGenerator
    {
        private readonly CybersecurityKnowledgeBase _knowledgeBase;
        private readonly KeywordMatcher _keywordMatcher;
        private readonly Random _random = new();
        private readonly ConversationStateTracker _stateTracker;
        private readonly FollowUpHandler _followUpHandler;
        private List<string> _lastOfferedKeywords = new();
        private bool _hasAskedHowAreYou = false;
        private bool _waitingForYesResponse = false;
        private string _pendingTopicForYes = string.Empty;
        private Dictionary<string, HashSet<int>> _shownTipIndices = new(StringComparer.OrdinalIgnoreCase);
        public Action<string>? ScrollToFirstKeywordConversation { get; set; }
        public ActivityLog? ActivityLog { get; set; }

        public BotResponseGenerator(CybersecurityKnowledgeBase knowledgeBase, KeywordMatcher keywordMatcher) { _knowledgeBase = knowledgeBase; _keywordMatcher = keywordMatcher; _stateTracker = new ConversationStateTracker(); _followUpHandler = new FollowUpHandler(knowledgeBase, _stateTracker); }

        public string GenerateReply(string input, ConversationContext context)
        {
            string lowerInput = input.ToLowerInvariant().Trim();
            string? followUpResponse = _followUpHandler.Handle(lowerInput, context.Memory);
            if (followUpResponse != null) return followUpResponse;
            if (_keywordMatcher.IsNameSetting(lowerInput))
            {
                string? extractedName = _keywordMatcher.ExtractName(input);
                if (!string.IsNullOrEmpty(extractedName)) { context.Memory.SetName(extractedName); context.UserDisplayName = extractedName; _hasAskedHowAreYou = false; return $"Thanks for letting me know! I'll call you {extractedName} from now on."; }
            }
            if (_keywordMatcher.IsLikeLoveStatement(lowerInput) || _keywordMatcher.IsInterestedInTopic(lowerInput))
            {
                List<string> matchedTermsLocal = GetMatchedCyberTerms(lowerInput);
                if (matchedTermsLocal.Count > 0) { string topic = matchedTermsLocal.First(); context.Memory.IncrementTopicInterest(topic); context.Memory.LastDiscussedCyberKeyword = topic; ScrollToFirstKeywordConversation?.Invoke(topic); if (!context.Memory.FavoriteTopics.Contains(topic)) context.Memory.FavoriteTopics.Add(topic); _hasAskedHowAreYou = false; return $"❤️ Great! I'll remember that you're interested in {topic}. It's a crucial part of staying safe online.\n\nWant to dive deeper into {topic}? Just say 'tell me more about {topic}' or ask for a tip!"; }
            }
            if (_waitingForYesResponse && !string.IsNullOrEmpty(_pendingTopicForYes))
            {
                string emotion = _pendingTopicForYes;
                string userResponse = lowerInput;
                List<string> userTopicMatches = GetMatchedCyberTerms(userResponse);
                if (userTopicMatches.Count > 0) { string topic = userTopicMatches.First(); context.Memory.CurrentEmotion = emotion; context.Memory.EmotionHistory.Add($"{emotion}_about_{topic}|{DateTime.Now}"); context.Memory.LastSentiment = emotion; context.Memory.LastDiscussedCyberKeyword = topic; context.Memory.IncrementTopicInterest(topic); _stateTracker.CurrentTopic = topic; ScrollToFirstKeywordConversation?.Invoke(topic); string comfortMessage = GetRandomEmpatheticOpening(emotion, topic); string tip = GetNextTipForTopic(topic); _waitingForYesResponse = false; _pendingTopicForYes = string.Empty; return $"{comfortMessage}\n\n💡 Let me share some tips to help you stay safe with {topic}:\n{tip}"; }
                else { _waitingForYesResponse = false; _pendingTopicForYes = string.Empty; return $"I'm sorry, I can only help with cybersecurity topics. Please try asking about things like phishing, malware, passwords, scams, or privacy. I'm glad to help with those!"; }
            }
            string detectedEmotion = DetectEmotion(lowerInput);
            if (!string.IsNullOrEmpty(detectedEmotion))
            {
                List<string> sentimentTopicMatches = GetMatchedCyberTerms(lowerInput);
                if (sentimentTopicMatches.Count > 0) { string topic = sentimentTopicMatches.First(); context.Memory.CurrentEmotion = detectedEmotion; context.Memory.EmotionHistory.Add($"{detectedEmotion}_about_{topic}|{DateTime.Now}"); context.Memory.LastSentiment = detectedEmotion; context.Memory.LastDiscussedCyberKeyword = topic; context.Memory.IncrementTopicInterest(topic); _stateTracker.CurrentTopic = topic; _hasAskedHowAreYou = false; _waitingForYesResponse = false; _pendingTopicForYes = string.Empty; ScrollToFirstKeywordConversation?.Invoke(topic); string comfortMessage = GetRandomEmpatheticOpening(detectedEmotion, topic); string tip = GetNextTipForTopic(topic); return $"{comfortMessage}\n\n💡 Let me share some tips to help you stay safe with {topic}:\n{tip}"; }
                string afterEmotion = ExtractAfterEmotion(lowerInput, detectedEmotion);
                if (!string.IsNullOrEmpty(afterEmotion) && sentimentTopicMatches.Count == 0) return $"I'm sorry, I can only help with cybersecurity topics. Please try asking about things like phishing, malware, passwords, scams, or privacy. I'm glad to help with those!";
                if (sentimentTopicMatches.Count == 0 && string.IsNullOrEmpty(afterEmotion)) { context.Memory.CurrentEmotion = detectedEmotion; context.Memory.EmotionHistory.Add($"{detectedEmotion}|{DateTime.Now}"); context.Memory.LastSentiment = detectedEmotion; _hasAskedHowAreYou = false; _waitingForYesResponse = true; _pendingTopicForYes = detectedEmotion; return $"I understand you're feeling {detectedEmotion}. I'd like to help, but I need to know what's concerning you.\n\nPlease name a specific cybersecurity topic (like scams, phishing, malware, passwords, or privacy) so I can give you the right advice!"; }
            }
            if (IsTipRequest(lowerInput))
            {
                List<string> tipMatchedTerms = GetMatchedCyberTerms(lowerInput);
                string? extractedTopic = ExtractTopicFromTipRequest(lowerInput, tipMatchedTerms);
                string topic;
                if (!string.IsNullOrEmpty(extractedTopic)) { topic = extractedTopic; context.Memory.IncrementTopicInterest(topic); context.Memory.LastDiscussedCyberKeyword = topic; _stateTracker.CurrentTopic = topic; ScrollToFirstKeywordConversation?.Invoke(topic); }
                else if (tipMatchedTerms.Count > 0) { topic = tipMatchedTerms.First(); context.Memory.LastDiscussedCyberKeyword = topic; _stateTracker.CurrentTopic = topic; ScrollToFirstKeywordConversation?.Invoke(topic); }
                else if (!string.IsNullOrEmpty(context.Memory.LastDiscussedCyberKeyword)) { topic = context.Memory.LastDiscussedCyberKeyword; }
                else { _hasAskedHowAreYou = false; return GetGeneralTip() + "\n\n" + GenerateRandomConceptPrompt(); }

                // Get the next tip index
                int nextIndex = context.Memory.GetNextTipIndex(topic, _knowledgeBase);

                if (nextIndex == -1)
                {
                    // All tips used
                    return $"0 tips remaining for '{topic}'. Upgrade to BotBuddy Premium for more tips or explore other cybersecurity topic instead";
                }

                var allTips = _knowledgeBase.GetAllTipsForTopic(topic);
                if (allTips == null || allTips.Length == 0)
                {
                    return GetGeneralTip();
                }

                string tip = allTips[nextIndex];
                context.Memory.MarkTipUsed(topic, nextIndex);
                context.Memory.IncrementTopicRequest(topic);

                int usedTips = context.Memory.GetTipCount(topic);
                int totalTips = allTips.Length;

                // Check if this was the last tip
                if (usedTips >= totalTips)
                {
                    return $"0 tips remaining for '{topic}'. Upgrade to BotBuddy Premium for more tips or explore other cybersecurity topic instead";
                }

                // Personalization if favorite
                string personalization = "";
                if (context.Memory.FavoriteTopics.Contains(topic))
                {
                    string[] personalizations = { $"Since you're interested in {topic}, here's a tip just for you!\n\n", $"As someone who likes {topic}, you'll find this tip valuable!\n\n", $"I remember you're interested in {topic}. Here's a helpful tip!\n\n" };
                    personalization = personalizations[_random.Next(personalizations.Length)];
                }

                _stateTracker.DefinitionPart = 0;
                _hasAskedHowAreYou = false;

                // Log tip request
                if (ActivityLog != null) ActivityLog.Log("Tip Requested", $"Topic: {topic}");

                // Return only the tip - NO progress text in bot message
                return $"{personalization}🔐 {topic.ToUpper()} TIP:\n{tip}";
            }
            string directExampleTopic = ExtractTopicWithKeyword(lowerInput, "example");
            if (_keywordMatcher.IsExampleRequest(lowerInput) || !string.IsNullOrEmpty(directExampleTopic))
            {
                string topic = !string.IsNullOrEmpty(directExampleTopic) ? directExampleTopic : (!string.IsNullOrEmpty(_stateTracker.CurrentTopic) ? _stateTracker.CurrentTopic : context.Memory.LastDiscussedCyberKeyword);
                if (!string.IsNullOrEmpty(topic))
                {
                    context.Memory.IncrementTopicInterest(topic);
                    context.Memory.MarkExampleUsed(topic);
                    context.Memory.LastDiscussedCyberKeyword = topic;
                    _stateTracker.CurrentTopic = topic;
                    _hasAskedHowAreYou = false;
                    ScrollToFirstKeywordConversation?.Invoke(topic);

                    // Log example request HERE
                    if (ActivityLog != null) ActivityLog.Log("Example Requested", $"Topic: {topic}");

                    return HandleExampleRequest(topic);
                }
                _hasAskedHowAreYou = false;
                return "What topic would you like an example of? Just ask me about a specific cybersecurity term!";
            }
            string directMoreTopic = ExtractTopicWithKeyword(lowerInput, "more");
            if (_keywordMatcher.IsMoreDetailsRequest(lowerInput) || !string.IsNullOrEmpty(directMoreTopic))
            {
                string topic = !string.IsNullOrEmpty(directMoreTopic) ? directMoreTopic : (!string.IsNullOrEmpty(_stateTracker.CurrentTopic) ? _stateTracker.CurrentTopic : context.Memory.LastDiscussedCyberKeyword);
                if (!string.IsNullOrEmpty(topic))
                {
                    context.Memory.IncrementTopicInterest(topic);
                    context.Memory.MarkMoreUsed(topic);
                    context.Memory.LastDiscussedCyberKeyword = topic;
                    _stateTracker.CurrentTopic = topic;
                    _hasAskedHowAreYou = false;
                    ScrollToFirstKeywordConversation?.Invoke(topic);

                    // Log more details request HERE
                    if (ActivityLog != null) ActivityLog.Log("More Details Requested", $"Topic: {topic}");

                    return HandleMoreDetails(topic);
                }
                _hasAskedHowAreYou = false;
                return "What topic would you like more details about? Just ask me about a specific cybersecurity term!";
            }
            List<string> definitionMatchedTerms = GetMatchedCyberTerms(lowerInput);
            bool hasCyberTerm = definitionMatchedTerms.Count > 0;
            if ((_keywordMatcher.HasWhatIs(lowerInput) || hasCyberTerm) && hasCyberTerm && !IsTipRequest(lowerInput))
            {
                _waitingForYesResponse = false;
                _pendingTopicForYes = string.Empty;
                string topic = definitionMatchedTerms.First();
                ScrollToFirstKeywordConversation?.Invoke(topic);

                // Log definition request
                if (ActivityLog != null) ActivityLog.Log("Definition Requested", $"Topic: {topic}");

                string recallMessage = "";
                if (context.Memory.FavoriteTopics.Count > 0 && !context.Memory.FavoriteTopics.Contains(topic)) { string[] recalls = { $"\n\n💭 As someone interested in {context.Memory.FavoriteTopics.First()}, understanding {topic} will give you a more complete security picture!\n\n", $"\n\n🎯 I notice you like {context.Memory.FavoriteTopics.First()}. Learning about {topic} will complement that knowledge!\n\n", $"\n\n🔐 Since you're interested in {context.Memory.FavoriteTopics.First()}, here's another important topic to know!\n\n" }; recallMessage = recalls[_random.Next(recalls.Length)]; }
                string defResponse = HandleWhatIsQuestion(definitionMatchedTerms, context);
                _hasAskedHowAreYou = false;
                return recallMessage + defResponse;
            }
            if (_keywordMatcher.IsHelpRequest(lowerInput))
            {
                _hasAskedHowAreYou = false;

                // Log help menu view
                if (ActivityLog != null) ActivityLog.Log("Help Menu Viewed", "");

                string helpResponse = GetHelpResponse(context);
                if (context.Memory.FavoriteTopics.Count > 0) { string favTopic = context.Memory.FavoriteTopics.First(); helpResponse += $"\n\n💡 Since you're interested in {favTopic}, would you like to learn more about that specifically? Just say 'tell me more about {favTopic}'!"; }
                return helpResponse;
            }
            bool asksHowAreYou = _keywordMatcher.IsHowAreYouQuestion(lowerInput);
            bool isSimpleGreeting = _keywordMatcher.IsGreeting(lowerInput);
            bool isUserPositive = _keywordMatcher.IsUserPositive(lowerInput);
            bool isShortPositive = _keywordMatcher.IsShortPositive(lowerInput);
            bool isAskingBack = lowerInput.Contains("and you") || lowerInput.Contains("how about you") || lowerInput.Contains("what about you") || lowerInput == "you?";
            bool isPositiveAndAskingBack = (isUserPositive || isShortPositive) && isAskingBack;
            string userName = context.Memory.UserName;
            if (isPositiveAndAskingBack) { _hasAskedHowAreYou = false; string response = "That's great to hear! I'm doing well too! " + GenerateRandomConceptPrompt(); if (!string.IsNullOrEmpty(userName)) return $"{userName}, " + response.ToLower(); return response; }
            if (asksHowAreYou && !isUserPositive && !isShortPositive) { _hasAskedHowAreYou = true; if (!string.IsNullOrEmpty(userName)) return $"I'm doing great, {userName}! How are you?"; return "I'm doing great! How are you?"; }
            if (isAskingBack && !isUserPositive && !isShortPositive) { _hasAskedHowAreYou = false; string response = "I'm doing well, thanks for asking! " + GenerateRandomConceptPrompt(); if (!string.IsNullOrEmpty(userName)) return $"{userName}, " + response.ToLower(); return response; }
            if ((isUserPositive || isShortPositive) && _hasAskedHowAreYou && !isAskingBack) { _hasAskedHowAreYou = false; string response = "Awesome! I'm glad you're doing well. " + GenerateRandomConceptPrompt(); if (!string.IsNullOrEmpty(userName)) return $"{userName}, " + response.ToLower(); return response; }
            if (isSimpleGreeting && !_hasAskedHowAreYou && !asksHowAreYou) { _hasAskedHowAreYou = true; if (!string.IsNullOrEmpty(userName)) return $"Hi {userName}, how are you?"; return "Hi, how are you?"; }
            bool isYesResponse = lowerInput == "yes" || lowerInput == "yeah" || lowerInput == "yep" || lowerInput == "sure" || lowerInput == "okay" || lowerInput == "ok";
            if (isYesResponse && _lastOfferedKeywords.Count > 0)
            {
                List<string> responseParts = new List<string>();
                foreach (string term in _lastOfferedKeywords.Take(1)) { if (_knowledgeBase.TryGetDefinition(term, out var definition)) { responseParts.Add(definition.Part1); responseParts.Add("\n" + definition.Part2); context.Memory.MarkTopicCovered(term); context.Memory.LastDiscussedCyberKeyword = term; _stateTracker.CurrentTopic = term; } }
                ScrollToFirstKeywordConversation?.Invoke(_lastOfferedKeywords.First());
                _hasAskedHowAreYou = false;
                return string.Join("", responseParts) + "\n\n" + GenerateRandomConceptPrompt();
            }
            if (lowerInput == "no" || lowerInput == "nah" || lowerInput == "nope" || lowerInput == "not now") { _waitingForYesResponse = false; _pendingTopicForYes = string.Empty; _stateTracker.DefinitionPart = 0; _hasAskedHowAreYou = false; string[] noResponses = { "No worries! Let me know when you're ready to chat about cybersecurity.", "That's fine! I'll be here when you want to learn more.", "Okay! Just say 'help' whenever you want to see what I can teach you." }; return noResponses[_random.Next(noResponses.Length)]; }
            if (hasCyberTerm && definitionMatchedTerms.Count > 0) { context.Memory.LastDiscussedCyberKeyword = definitionMatchedTerms.First(); _stateTracker.CurrentTopic = definitionMatchedTerms.First(); }
            if (!string.IsNullOrEmpty(userName)) return $"{userName}, I didn't quite catch that. Could you rephrase? Or type 'help' to see what I can help with!";
            return "I didn't quite catch that. Could you rephrase? Or type 'help' to see what I can help with!";
        }
        private string GetNextTipForTopic(string topic)
        {
            string[] allTips = _knowledgeBase.GetAllTipsForTopic(topic);
            if (allTips == null || allTips.Length == 0) return GetGeneralTip();
            if (!_shownTipIndices.ContainsKey(topic)) _shownTipIndices[topic] = new HashSet<int>();
            if (_shownTipIndices[topic].Count >= allTips.Length) _shownTipIndices[topic].Clear();
            List<int> availableIndices = new List<int>();
            for (int i = 0; i < allTips.Length; i++) if (!_shownTipIndices[topic].Contains(i)) availableIndices.Add(i);
            if (availableIndices.Count == 0) { _shownTipIndices[topic].Clear(); for (int i = 0; i < allTips.Length; i++) availableIndices.Add(i); }
            int randomIndex = availableIndices[_random.Next(availableIndices.Count)];
            _shownTipIndices[topic].Add(randomIndex);
            return allTips[randomIndex];
        }
        private string GetRandomEmpatheticOpening(string emotion, string topic)
        {
            var empatheticOpenings = new Dictionary<string, string[]>
            {
                { "worried", new string[] { "I understand you are worried about {topic}. It's completely normal to feel concerned about cybersecurity threats.", "Feeling worried about {topic} is understandable - the digital world can be intimidating sometimes.", "I hear your worry about {topic}. Many people feel the same way, but knowledge is power." } },
                { "anxious", new string[] { "I understand you are anxious about {topic}. It's natural to feel this way about security risks.", "Feeling anxious about {topic} is completely normal. Let's break it down together.", "Your anxiety about {topic} is valid. Many people share this concern." } },
                { "frustrated", new string[] { "I understand you are frustrated with {topic}. Technology can be annoying sometimes.", "Feeling frustrated about {topic} is totally valid. Let me help you find a better way.", "Your frustration with {topic} makes sense. Let's solve this together." } },
                { "scared", new string[] { "I understand you are scared about {topic}. That's a very natural feeling about security threats.", "Feeling scared about {topic} is completely normal. Let me help you understand it better.", "Your fear about {topic} is valid. Knowledge is the best way to overcome fear." } },
                { "sad", new string[] { "I understand you are sad about {topic}. I'm sorry this is affecting you.", "Feeling sad about {topic} is understandable. Let me help you feel better.", "Your sadness about {topic} is valid. Let me give you hope and solutions." } },
                { "angry", new string[] { "I understand you are angry about {topic}. That's a completely valid reaction.", "Feeling angry about {topic} is understandable. Let's channel that into action.", "Your anger about {topic} is valid. Let me help you fight back effectively." } }
            };
            string normalizedEmotion = emotion.ToLowerInvariant();
            if (!empatheticOpenings.ContainsKey(normalizedEmotion)) normalizedEmotion = "worried";
            var openings = empatheticOpenings[normalizedEmotion];
            string opening = openings[_random.Next(openings.Length)];
            return opening.Replace("{topic}", topic);
        }
        private string ExtractAfterEmotion(string input, string emotion)
        {
            string lowerInput = input.ToLowerInvariant();
            int emotionIndex = lowerInput.IndexOf(emotion);
            if (emotionIndex >= 0) { string after = input.Substring(emotionIndex + emotion.Length).Trim(); if (!string.IsNullOrEmpty(after) && after.Length > 0) return after; }
            return string.Empty;
        }
        private string ExtractTopicWithKeyword(string input, string keywordType)
        {
            string lowerInput = input.ToLowerInvariant();
            foreach (var term in _knowledgeBase.GetAllTerms()) if (lowerInput.Contains(term.ToLowerInvariant())) { if (keywordType == "example" && (lowerInput.Contains("example") || lowerInput.Contains("illustrate"))) return term; if (keywordType == "more" && (lowerInput.Contains("more about") || lowerInput.Contains("elaborate") || lowerInput.Contains("explain further") || lowerInput.Contains("tell me more about"))) return term; }
            return string.Empty;
        }
        private string DetectEmotion(string input)
        {
            if (_keywordMatcher.IsWorried(input)) return "worried";
            if (_keywordMatcher.IsAnxious(input)) return "anxious";
            if (_keywordMatcher.IsFrustrated(input)) return "frustrated";
            if (_keywordMatcher.IsSad(input)) return "sad";
            if (_keywordMatcher.IsAngry(input)) return "angry";
            if (input.Contains("scared") || input.Contains("fear") || input.Contains("terrified")) return "scared";
            if (input.Contains("stressed") || input.Contains("stress")) return "stressed";
            return string.Empty;
        }
        private bool IsTipRequest(string input)
        {
            string lowerInput = input.ToLowerInvariant();
            string[] tipPatterns = { "tip", "tips", "give me a tip", "give me tips", "share a tip", "share tips", "advice", "advise me", "advice on", "advice about", "give me advice", "how to avoid", "how to prevent", "how to protect", "how to stay safe", "recommendation", "suggestions", "best practices", "another tip", "more tips" };
            return tipPatterns.Any(p => lowerInput.Contains(p));
        }
        private string? ExtractTopicFromTipRequest(string input, List<string> matchedTerms)
        {
            string lowerInput = input.ToLowerInvariant();
            if (matchedTerms.Count > 0) return matchedTerms.First();
            foreach (var term in _knowledgeBase.GetAllTerms()) if (lowerInput.Contains(term.ToLowerInvariant())) return term;
            return null;
        }
        private string HandleWhatIsQuestion(List<string> terms, ConversationContext context)
        {
            if (terms.Count == 0) return "Hmm, I don't have info on that. Try asking about phishing, malware, or passwords!";
            string topic = terms.First();
            _stateTracker.CurrentTopic = topic;
            context.Memory.IncrementTopicInterest(topic);
            context.Memory.MarkTopicCovered(topic);
            context.Memory.LastDiscussedCyberKeyword = topic;
            context.Memory.IncrementTopicRequest(topic);
            if (_knowledgeBase.TryGetDefinition(topic, out var definition)) { _stateTracker.DefinitionPart = 1; bool hasTip = _knowledgeBase.HasTips(topic); string tipPrompt = hasTip ? $"\n\n💡 Want a practical tip about {topic}? Just ask \"Give me a tip about {topic}\"!" : ""; return definition.Part1 + $"\n\nWant to see an example? Just say 'example' or 'more details'!{tipPrompt}"; }
            return "I couldn't find good information on that. Want to try something else?";
        }
        private string HandleExampleRequest(string topic) { if (_knowledgeBase.TryGetDefinition(topic, out var definition)) { _stateTracker.DefinitionPart = 2; return $"{definition.Part2}\n\nWant to learn more? Just say 'more details'!"; } return "Sorry, I couldn't find an example for that."; }
        private string HandleMoreDetails(string topic) { if (_knowledgeBase.TryGetDefinition(topic, out var definition)) { _stateTracker.DefinitionPart = 3; return $"{definition.Part3}\n\nWant to know even more? Ask for 'another tip' or a specific question about {topic}!"; } return "I couldn't find more info on that. Want to try a different topic?"; }
        private string GetHelpResponse(ConversationContext context)
        {
            var dynamicTopics = _knowledgeBase.GetDynamicHelpTopics();
            string topicsList = string.Join(", ", dynamicTopics.Take(6));
            string lastTopic = !string.IsNullOrEmpty(context.Memory.LastDiscussedCyberKeyword) ? $"\n\n💬 Last discussed: {context.Memory.LastDiscussedCyberKeyword.ToUpper()}" : "";
            return $"🔐 Here's what I can teach you:\n\n{topicsList}\n\nTry asking:\n• \"What is {dynamicTopics[0]}?\"\n• \"Tell me about {dynamicTopics[1]}\"\n• \"Give me a tip about {dynamicTopics[2]}\"\n• \"How to avoid {dynamicTopics[2]}\"\n• \"Advice on {dynamicTopics[0]}\"\n• \"{dynamicTopics[2]} tips\"\n• \"Topics Covered\" - See everything you've learned!" + lastTopic + $"\n\n💡 You can also say \"I like {dynamicTopics[0]}\" and I'll remember your favorites!";
        }
        private string GenerateRandomConceptPrompt()
        {
            var keys = _knowledgeBase.GetRandomTerms(_random.Next(2, 4));
            _lastOfferedKeywords = keys;
            if (keys.Count == 2) return $"Would you like to learn about {keys[0]} or {keys[1]}?";
            else if (keys.Count == 3) return $"Would you like to learn about {keys[0]}, {keys[1]}, or {keys[2]}?";
            else return $"Interested in learning about {keys[0]}?";
        }
        private string GetGeneralTip()
        {
            string[] generalTips = { "💡 Use strong unique passwords for every account!", "💡 Turn on two-factor authentication - it blocks 99.9% of automated attacks!", "💡 Never click links in suspicious emails!", "💡 Keep your software updated - those updates fix security holes!", "💡 Use a VPN on public Wi-Fi to keep your data private!" };
            return generalTips[_random.Next(generalTips.Length)];
        }
        private List<string> GetMatchedCyberTerms(string lowerInput)
        {
            HashSet<string> matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string term in _knowledgeBase.GetAllTerms()) if (!string.IsNullOrWhiteSpace(term) && lowerInput.Contains(term.ToLowerInvariant())) matches.Add(term);
            return matches.ToList();
        }
    }

    // ================= QUIZ CLASS =================
    public class QuizQuestion
    {
        public string Question { get; set; }
        public string[] Options { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public string Explanation { get; set; }
        public bool IsTrueFalse { get; set; }
        public string Category { get; set; }

        public QuizQuestion(string question, string[] options, int correctIndex, string explanation, bool isTrueFalse = false, string category = "General")
        {
            Question = question;
            Options = options;
            CorrectAnswerIndex = correctIndex;
            Explanation = explanation;
            IsTrueFalse = isTrueFalse;
            Category = category;
        }
    }

    public class CybersecurityQuiz
    {
        private List<QuizQuestion> _allQuestions = new List<QuizQuestion>();
        private List<QuizQuestion>? _currentSessionQuestions;
        private Queue<int> _usedQuestionHistory = new Queue<int>();
        private int _currentQuestionIndex;
        private int _score;
        private bool _quizActive;
        private string _currentDifficulty = "";
        private Random _random = new Random();
        private int _sessionLength;
        public ActivityLog? ActivityLog { get; set; }

        public bool IsQuizActive => _quizActive;
        public int CurrentQuestionNumber => _currentQuestionIndex + 1;
        public int TotalQuestions => _sessionLength;
        public int CurrentScore => _score;
        public string CurrentDifficulty => _currentDifficulty;

        public QuizQuestion? CurrentQuestion => _quizActive && _currentSessionQuestions != null && _currentQuestionIndex < _currentSessionQuestions.Count ? _currentSessionQuestions[_currentQuestionIndex] : null;

        public CybersecurityQuiz()
        {
            InitializeAllQuestions();
            _usedQuestionHistory = new Queue<int>();
            _currentQuestionIndex = 0;
            _score = 0;
            _quizActive = false;
            _sessionLength = 0;
        }

        private void InitializeAllQuestions()
        {
            _allQuestions = new List<QuizQuestion>
            {
                // Phishing & Email Security
                new QuizQuestion("What should you do if you receive an email asking for your password?",
                    new string[] { "Reply with your password", "Delete the email", "Report the email as phishing", "Ignore it" }, 2,
                    "Reporting phishing emails helps prevent scams. Legitimate companies never ask for passwords via email.", false, "Phishing"),

                new QuizQuestion("What is a common sign of a phishing email?",
                    new string[] { "Personalized greeting with your name", "Urgent language demanding immediate action", "Professional signature", "Correct spelling and grammar" }, 1,
                    "Urgent language like 'act now' or 'your account will be closed' is a red flag for phishing attempts.", false, "Phishing"),

                new QuizQuestion("True or False: Spear phishing targets random individuals without any prior research.",
                    new string[] { "True", "False" }, 1,
                    "Spear phishing is highly targeted, using personal information to make the attack more convincing.", true, "Phishing"),

                new QuizQuestion("What is 'smishing'?",
                    new string[] { "A type of computer virus", "Phishing conducted via SMS text messages", "Secure messaging app", "Email encryption method" }, 1,
                    "Smishing is phishing carried out through SMS text messages, tricking victims into clicking malicious links.", false, "Phishing"),

                new QuizQuestion("What should you check in an email to verify its legitimacy?",
                    new string[] { "The sender's email address for misspellings", "The email logo", "The font size", "The email length" }, 0,
                    "Always check the sender's email address carefully - attackers often use addresses that look similar to real ones.", false, "Phishing"),

                new QuizQuestion("True or False: Vishing attacks are carried out through email.",
                    new string[] { "True", "False" }, 1,
                    "Vishing is voice phishing - attacks carried out through phone calls, not email.", true, "Phishing"),

                new QuizQuestion("What is a 'watering hole' attack?",
                    new string[] { "An attack on water treatment facilities", "Compromising websites frequently visited by the target", "A type of physical security breach", "Email-based phishing" }, 1,
                    "Watering hole attacks infect websites that the target group frequently visits.", false, "Phishing"),

                new QuizQuestion("What should you do if you accidentally clicked a phishing link?",
                    new string[] { "Nothing, it's probably fine", "Close the browser and forget about it", "Disconnect from the internet, run antivirus, change passwords", "Send the link to friends to warn them" }, 2,
                    "Immediately disconnect from the internet, run a security scan, change your passwords, and monitor your accounts.", false, "Phishing"),
                
                // Password Security
                new QuizQuestion("Which of the following is the STRONGEST password?",
                    new string[] { "password123", "P@ssw0rd", "MyBirthday1990", "C0mpl3x!P@55w0rd" }, 3,
                    "'C0mpl3x!P@55w0rd' uses uppercase, lowercase, numbers, symbols, and is at least 12 characters long.", false, "Passwords"),

                new QuizQuestion("True or False: Using the same password for multiple accounts is safe as long as the password is strong.",
                    new string[] { "True", "False" }, 1,
                    "You should NEVER reuse passwords across accounts. If one account gets breached, all your accounts become vulnerable.", true, "Passwords"),

                new QuizQuestion("What is a password manager?",
                    new string[] { "A person who manages passwords", "Software that securely stores and generates passwords", "A physical notebook for passwords", "A type of antivirus" }, 1,
                    "Password managers securely store your passwords and can generate strong, unique passwords for each account.", false, "Passwords"),

                new QuizQuestion("How long should a secure password ideally be?",
                    new string[] { "4-6 characters", "8-10 characters", "12+ characters", "Any length is fine" }, 2,
                    "Security experts recommend passwords of at least 12 characters for optimal security.", false, "Passwords"),

                new QuizQuestion("What is multi-factor authentication (MFA)?",
                    new string[] { "Using multiple passwords", "Requiring multiple verification methods to log in", "Having multiple accounts", "Multiple people approving logins" }, 1,
                    "MFA requires two or more verification methods, making accounts much harder to compromise.", false, "Passwords"),

                new QuizQuestion("True or False: Writing down your passwords on a sticky note under your keyboard is a secure practice.",
                    new string[] { "True", "False" }, 1,
                    "Writing down passwords is insecure. Use a password manager instead to store them securely.", true, "Passwords"),

                new QuizQuestion("What is a brute force attack?",
                    new string[] { "A physical attack on servers", "Trying every possible password combination", "Social engineering attack", "Email phishing attack" }, 1,
                    "Brute force attacks use automated tools to try millions of password combinations until they find the correct one.", false, "Passwords"),

                new QuizQuestion("What makes a password weak?",
                    new string[] { "Using personal information like birthdays", "Using a mix of character types", "Long length", "Using a passphrase" }, 0,
                    "Personal information like birthdays, names, or common words can be easily guessed or found online.", false, "Passwords"),
                
                // Malware & Viruses
                new QuizQuestion("What is ransomware?",
                    new string[] { "Software that holds your files hostage until you pay", "A type of antivirus program", "Software that speeds up your computer", "A password manager tool" }, 0,
                    "Ransomware is malware that encrypts your files and demands payment for decryption.", false, "Malware"),

                new QuizQuestion("What is a Trojan horse in cybersecurity?",
                    new string[] { "A type of computer virus", "Malware disguised as legitimate software", "A hardware device", "A security protocol" }, 1,
                    "A Trojan horse appears legitimate but contains malicious code that can damage or steal data.", false, "Malware"),

                new QuizQuestion("What is spyware?",
                    new string[] { "Software that monitors user activity without consent", "Software that protects from spies", "A type of encryption", "A firewall" }, 0,
                    "Spyware secretly monitors your activity, capturing keystrokes, passwords, and browsing habits.", false, "Malware"),

                new QuizQuestion("How does a computer virus typically spread?",
                    new string[] { "Through the air", "By attaching to legitimate files and programs", "Only through email", "Through power cables" }, 1,
                    "Viruses attach to legitimate files and spread when those files are shared or executed.", false, "Malware"),

                new QuizQuestion("What is a keylogger?",
                    new string[] { "A device that logs key presses", "A password manager", "An antivirus tool", "A firewall setting" }, 0,
                    "Keyloggers record every keystroke made on a device, capturing passwords, messages, and other sensitive data.", false, "Malware"),

                new QuizQuestion("What is a botnet?",
                    new string[] { "A network of robots", "A network of infected devices controlled by attackers", "A type of antivirus", "A security protocol" }, 1,
                    "Botnets are networks of compromised devices used to launch large-scale attacks like DDoS.", false, "Malware"),

                new QuizQuestion("True or False: Antivirus software can protect against all types of malware 100% of the time.",
                    new string[] { "True", "False" }, 1,
                    "No antivirus is 100% effective. Good security practices and updates are also essential.", true, "Malware"),

                new QuizQuestion("What is a rootkit?",
                    new string[] { "A gardening tool", "Malware that hides deep in the operating system", "A type of firewall", "An encryption method" }, 1,
                    "Rootkits hide deep within the operating system, making them very difficult to detect and remove.", false, "Malware"),
                
                // Safe Browsing & Web Security
                new QuizQuestion("What should you look for to verify a website is secure?",
                    new string[] { "A padlock icon in the address bar", "A '100% Safe' badge", "The word 'secure' in the domain", "A pop-up confirmation" }, 0,
                    "The padlock icon and 'https://' indicate the connection is encrypted and secure.", false, "Safe Browsing"),

                new QuizQuestion("What does HTTPS stand for?",
                    new string[] { "HyperText Transfer Protocol Secure", "High Transfer Text Protocol System", "Hyper Transfer Text Protection System", "High-Tech Transfer Protocol Secure" }, 0,
                    "HTTPS is the secure version of HTTP, encrypting data between your browser and the website.", false, "Safe Browsing"),

                new QuizQuestion("True or False: Public Wi-Fi networks are safe to use for online banking without additional protection.",
                    new string[] { "True", "False" }, 1,
                    "Public Wi-Fi is often unencrypted. Always use a VPN for sensitive activities.", true, "Safe Browsing"),

                new QuizQuestion("What is a VPN used for?",
                    new string[] { "Speeding up internet", "Encrypting internet traffic and hiding IP address", "Blocking all ads", "Backing up files" }, 1,
                    "A VPN encrypts your traffic and hides your IP address, protecting your privacy.", false, "Safe Browsing"),

                new QuizQuestion("What is a man-in-the-middle attack?",
                    new string[] { "An attack on social media", "When an attacker intercepts communication between two parties", "A physical security breach", "A type of spam email" }, 1,
                    "In MITM attacks, the attacker secretly intercepts communication between two parties who believe they're directly communicating.", false, "Safe Browsing"),

                new QuizQuestion("What does 'pharming' refer to?",
                    new string[] { "Farming simulation game", "Redirecting website traffic to fake sites without user knowledge", "Secure farming technology", "Email encryption method" }, 1,
                    "Pharming redirects users from legitimate websites to fake ones, often without any indication.", false, "Safe Browsing"),

                new QuizQuestion("What should you do before entering credit card information on a website?",
                    new string[] { "Check for HTTPS and padlock icon", "Ask a friend if they've used the site", "Check the website's color scheme", "Nothing, all sites are safe" }, 0,
                    "Always verify the connection is secure (HTTPS) before entering sensitive information.", false, "Safe Browsing"),

                new QuizQuestion("True or False: Software updates are optional and don't affect security.",
                    new string[] { "True", "False" }, 1,
                    "Software updates include critical security patches. Delaying updates leaves your system vulnerable.", true, "Safe Browsing"),
                
                // Social Engineering
                new QuizQuestion("What is social engineering?",
                    new string[] { "A type of computer virus", "Manipulating people into revealing confidential information", "Building social media networks", "Engineering social platforms" }, 1,
                    "Social engineering manipulates people into revealing information, relying on human psychology rather than technical hacking.", false, "Social Engineering"),

                new QuizQuestion("What is pretexting?",
                    new string[] { "A written excuse", "Creating a fake scenario to trick victims", "A type of encryption", "A security protocol" }, 1,
                    "Pretexting involves creating a fabricated scenario to convince victims to reveal information.", false, "Social Engineering"),

                new QuizQuestion("What is baiting in cybersecurity?",
                    new string[] { "Fishing with bait", "Leaving infected devices like USB drives to tempt victims", "A type of password attack", "Email filtering" }, 1,
                    "Baiting leaves physical media like USB drives in public places, hoping someone will use them and infect their system.", false, "Social Engineering"),

                new QuizQuestion("What should you do if someone claiming to be IT support asks for your password?",
                    new string[] { "Give them the password", "Ask for their ID and give password", "Never give passwords to anyone, even IT support", "Send password via email" }, 2,
                    "Legitimate IT support will never ask for your password. This is almost always a scam.", false, "Social Engineering"),

                new QuizQuestion("What is an insider threat?",
                    new string[] { "A threat from inside a building", "A current or former employee misusing access", "A type of computer virus", "A hardware failure" }, 1,
                    "Insider threats come from people within an organization who have legitimate access but misuse it.", false, "Social Engineering"),
                
                // General Cybersecurity
                new QuizQuestion("What is a DDoS attack?",
                    new string[] { "Data Distribution System", "Distributed Denial of Service - overwhelming a system with traffic", "Digital Data Security", "Direct Denial System" }, 1,
                    "DDoS attacks flood targets with traffic from multiple sources, making services unavailable.", false, "General"),

                new QuizQuestion("What is zero-day vulnerability?",
                    new string[] { "A vulnerability that was discovered 0 days ago", "A software flaw unknown to the vendor", "A type of antivirus", "A security update" }, 1,
                    "Zero-day vulnerabilities are unknown to the software vendor, meaning no patch exists yet.", false, "General"),

                new QuizQuestion("What is data encryption?",
                    new string[] { "Sending data faster", "Converting data into unreadable format to protect it", "Deleting old data", "Backing up data" }, 1,
                    "Encryption scrambles data so only authorized parties with the decryption key can read it.", false, "General"),

                new QuizQuestion("What is the purpose of a firewall?",
                    new string[] { "Stop physical fires", "Monitor and control network traffic", "Speed up internet", "Store passwords" }, 1,
                    "Firewalls monitor incoming and outgoing network traffic based on security rules.", false, "General"),

                new QuizQuestion("True or False: Cybercriminals only target large companies.",
                    new string[] { "True", "False" }, 1,
                    "Cybercriminals target everyone - individuals, small businesses, and large corporations.", true, "General"),

                new QuizQuestion("What is the first step after a data breach?",
                    new string[] { "Ignore it", "Change passwords and notify affected parties", "Delete everything", "Wait and see" }, 1,
                    "Immediately change passwords, enable 2FA, and notify anyone who might be affected.", false, "General"),

                new QuizQuestion("What is patch management?",
                    new string[] { "Managing clothing patches", "Regularly updating software to fix vulnerabilities", "A type of antivirus", "Password management" }, 1,
                    "Patch management is the process of regularly applying software updates to fix security vulnerabilities.", false, "General"),

                new QuizQuestion("What is the best defense against ransomware?",
                    new string[] { "Paying the ransom quickly", "Regular offline backups", "Disconnecting from the internet", "Using a VPN" }, 1,
                    "Regular offline backups ensure you can restore files without paying the ransom.", false, "General"),

                new QuizQuestion("What is a security audit?",
                    new string[] { "Listening to security sounds", "Systematic evaluation of security measures", "A type of antivirus", "Password checking" }, 1,
                    "Security audits systematically assess an organization's security posture and identify vulnerabilities.", false, "General"),

                new QuizQuestion("What is the principle of least privilege?",
                    new string[] { "Everyone gets admin access", "Users get minimum necessary access to do their job", "Only executives have access", "No one has access" }, 1,
                    "Least privilege means giving users only the access they absolutely need, reducing potential damage from breaches.", false, "General")
            };
        }

        private List<QuizQuestion> GetRandomQuestions(int count, List<string> excludeIndices)
        {
            var available = new List<QuizQuestion>();
            for (int i = 0; i < _allQuestions.Count; i++)
            {
                if (!excludeIndices.Contains(i.ToString()))
                    available.Add(_allQuestions[i]);
            }

            return available.OrderBy(x => _random.Next()).Take(count).ToList();
        }

        public string StartQuiz(string difficulty)
        {
            _currentDifficulty = difficulty;
            _usedQuestionHistory = new Queue<int>();

            switch (difficulty.ToLower())
            {
                case "quick":
                    _sessionLength = 5;
                    break;
                case "balanced":
                    _sessionLength = 15;
                    break;
                case "deep":
                    _sessionLength = 30;
                    break;
                default:
                    _sessionLength = 15;
                    break;
            }

            var usedIndices = new List<string>();
            foreach (var idx in _usedQuestionHistory)
            {
                usedIndices.Add(idx.ToString());
            }

            _currentSessionQuestions = GetRandomQuestions(_sessionLength, usedIndices);

            foreach (var q in _currentSessionQuestions)
            {
                int idx = _allQuestions.IndexOf(q);
                _usedQuestionHistory.Enqueue(idx);
                if (_usedQuestionHistory.Count > 50) _usedQuestionHistory.Dequeue();
            }

            _currentQuestionIndex = 0;
            _score = 0;
            _quizActive = true;

            // Log quiz start
            if (ActivityLog != null) ActivityLog.Log("Quiz Started", $"Mode: {difficulty} ({_sessionLength} questions)");

            return GetGameModeMessage();
        }

        private string GetGameModeMessage()
        {
            string modeDesc = "";
            switch (_currentDifficulty.ToLower())
            {
                case "quick":
                    modeDesc = "⚡ QUICK CHALLENGE MODE ⚡\n• 5 questions\n• Estimated time: 2-3 minutes";
                    break;
                case "balanced":
                    modeDesc = "📚 BALANCED EXPERIENCE MODE 📚\n• 15 questions\n• Estimated time: 5-8 minutes";
                    break;
                case "deep":
                    modeDesc = "🎓 DEEP LEARNING MODE 🎓\n• 30 questions\n• Estimated time: 10-15 minutes";
                    break;
            }

            return $"🎮 GAME MODE ACTIVATED!\n\n{modeDesc}";
        }

        public (bool isCorrect, string response) SubmitAnswer(int selectedIndex)
        {
            if (!_quizActive || _currentSessionQuestions == null || _currentQuestionIndex >= _currentSessionQuestions.Count)
                return (false, "Quiz is not active.");

            var q = _currentSessionQuestions[_currentQuestionIndex];
            bool isCorrect = (selectedIndex == q.CorrectAnswerIndex);
            string response = "";

            if (isCorrect)
            {
                _score++;
                response = $"{q.Explanation}";
            }
            else
            {
                response = $"{q.Explanation}";
            }

            _currentQuestionIndex++;

            if (_currentQuestionIndex >= _sessionLength)
            {
                _quizActive = false;
            }

            return (isCorrect, response);
        }
        public void Reset()
        {
            _quizActive = false;
            _currentQuestionIndex = 0;
            _score = 0;
            _currentSessionQuestions = null;
        }

        public void LogQuizCompletion()
        {
            if (ActivityLog != null)
            {
                int total = _sessionLength;
                int correct = _score;
                ActivityLog.Log("Quiz Completed", $"{correct}/{total} correct ({correct * 100 / total:F0}%)");
            }
        }
    }

    // ================= TASK 4: REMINDER & TASK MANAGEMENT =================
    public class ReminderItem
    {
        public string Description { get; set; } = string.Empty;
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DbId { get; set; } = 0;
        public string Category { get; set; } = "Task"; // "Task" or "Reminder"

        public ReminderItem(string description, DateTime? reminderDate = null, string category = "Task")
        {
            Description = description;
            ReminderDate = reminderDate;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
            DbId = 0;
            Category = category;
        }
    }

    public class TaskManager
    {
        private List<ReminderItem> _reminders = new List<ReminderItem>();
        private List<ReminderItem> _tasks = new List<ReminderItem>();
        private List<ReminderItem> _completedTasks = new List<ReminderItem>();
        private List<ReminderItem> _completedReminders = new List<ReminderItem>();
        private TaskRepository _dbRepo = new TaskRepository();
        private bool _dbAvailable = true;
        private Timer? _reminderCheckTimer;
        public ActivityLog? ActivityLog { get; set; }

        public List<ReminderItem> GetActiveTasks()
        {
            return _tasks.ToList();
        }

        public List<ReminderItem> GetActiveReminders()
        {
            return _reminders.ToList();
        }

        public List<ReminderItem> GetCompletedTasks()
        {
            // Return both completed tasks from _tasks and _completedTasks
            var result = new List<ReminderItem>();
            result.AddRange(_tasks.Where(t => t.IsCompleted));
            result.AddRange(_completedTasks);
            return result;
        }

        public List<ReminderItem> GetCompletedReminders()
        {
            return _completedReminders.ToList();
        }

        public TaskManager()
        {
            try
            {
                _dbRepo.InitializeDatabase();
                LoadTasksFromDatabase();
                StartReminderChecker();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database unavailable: {ex.Message}");
                _dbAvailable = false;
            }
        }

        private void StartReminderChecker()
        {
            // Check every 60 seconds for due reminders
            _reminderCheckTimer = new Timer(CheckDueReminders, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        }

        private void CheckDueReminders(object? state)
        {
            try
            {
                var now = DateTime.Now;
                var dueReminders = _reminders.Where(r => r.ReminderDate.HasValue && r.ReminderDate.Value <= now && !r.IsCompleted).ToList();

                foreach (var reminder in dueReminders)
                {
                    // Auto-complete the reminder
                    CompleteReminder(reminder.Description);

                    if (ActivityLog != null)
                    {
                        ActivityLog.Log("Reminder Auto-Completed", $"{reminder.Description} (due date passed)");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reminder checker error: {ex.Message}");
            }
        }

        private void LoadTasksFromDatabase()
        {
            try
            {
                var dbTasks = _dbRepo.GetAllTasks();
                foreach (var dbTask in dbTasks)
                {
                    var item = new ReminderItem(dbTask.Title, dbTask.ReminderDate, dbTask.Category)
                    {
                        IsCompleted = dbTask.IsCompleted,
                        CreatedAt = dbTask.CreatedAt,
                        DbId = dbTask.Id
                    };

                    if (dbTask.IsCompleted)
                    {
                        if (dbTask.Category == "Reminder")
                        {
                            _completedReminders.Add(item);
                        }
                        else
                        {
                            _completedTasks.Add(item);
                        }
                    }
                    else
                    {
                        if (dbTask.Category == "Reminder")
                        {
                            _reminders.Add(item);
                        }
                        else
                        {
                            _tasks.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load tasks from database: {ex.Message}");
            }
        }

        public void AddReminder(string description, DateTime? reminderDate = null)
        {
            var existingReminder = _reminders.FirstOrDefault(r => r.Description.Equals(description, StringComparison.OrdinalIgnoreCase));
            if (existingReminder != null)
            {
                existingReminder.ReminderDate = reminderDate;
                existingReminder.IsCompleted = false;
                existingReminder.CreatedAt = DateTime.Now;
                existingReminder.Category = "Reminder";
                System.Diagnostics.Debug.WriteLine($"Updated reminder '{description}'");
            }
            else
            {
                var reminder = new ReminderItem(description, reminderDate, "Reminder");
                _reminders.Add(reminder);
                System.Diagnostics.Debug.WriteLine($"Added new reminder '{description}'");
            }

            if (_dbAvailable)
            {
                try
                {
                    _dbRepo.AddOrUpdateTask(description, "Reminder", "", reminderDate);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save reminder to database: {ex.Message}");
                }
            }

            if (ActivityLog != null)
            {
                string dateInfo = reminderDate.HasValue ? $" (Due: {reminderDate.Value:dd MMM yyyy})" : "";
                ActivityLog.Log("Reminder Set", $"{description}{dateInfo}");
            }
        }

        public void AddTask(string description, DateTime? dueDate = null)
        {
            var existingTask = _tasks.FirstOrDefault(t => t.Description.Equals(description, StringComparison.OrdinalIgnoreCase));
            if (existingTask != null)
            {
                existingTask.ReminderDate = dueDate;
                existingTask.IsCompleted = false;
                existingTask.CreatedAt = DateTime.Now;
                existingTask.Category = "Task";
                System.Diagnostics.Debug.WriteLine($"Updated task '{description}'");
            }
            else
            {
                var task = new ReminderItem(description, dueDate, "Task");
                _tasks.Add(task);
                System.Diagnostics.Debug.WriteLine($"Added new task '{description}'");
            }

            if (_dbAvailable)
            {
                try
                {
                    _dbRepo.AddOrUpdateTask(description, "Task", "", dueDate);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save task to database: {ex.Message}");
                }
            }

            if (ActivityLog != null)
            {
                string dateInfo = dueDate.HasValue ? $" (Due: {dueDate.Value:dd MMM yyyy})" : "";
                ActivityLog.Log("Task Added", $"{description}{dateInfo}");
            }
        }

        public bool CompleteTask(string description)
        {
            ReminderItem? task = null;

            // First try to find by description
            task = _tasks.FirstOrDefault(t => t.Description.Equals(description, StringComparison.OrdinalIgnoreCase) && !t.IsCompleted);

            // If not found, try to parse as a number
            if (task == null)
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(description, @"\d+");
                if (numberMatch.Success && int.TryParse(numberMatch.Value, out int taskNumber))
                {
                    var activeTasks = GetActiveTasks();
                    if (taskNumber >= 1 && taskNumber <= activeTasks.Count)
                    {
                        task = activeTasks[taskNumber - 1];
                    }
                }
            }

            // If still not found, try word numbers
            if (task == null)
            {
                int numberFromWords = ExtractTaskNumberFromText(description);
                if (numberFromWords > 0)
                {
                    var activeTasks = GetActiveTasks();
                    if (numberFromWords <= activeTasks.Count)
                    {
                        task = activeTasks[numberFromWords - 1];
                    }
                }
            }

            if (task != null)
            {
                task.IsCompleted = true;
                _tasks.Remove(task);
                _completedTasks.Add(task);

                if (_dbAvailable)
                {
                    try
                    {
                        var dbTasks = _dbRepo.GetAllTasks();
                        var dbTask = dbTasks.FirstOrDefault(t => t.Title.Equals(task.Description, StringComparison.OrdinalIgnoreCase) && !t.IsCompleted);
                        if (dbTask != null)
                        {
                            _dbRepo.CompleteTask(dbTask.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to complete task in database: {ex.Message}");
                    }
                }

                if (ActivityLog != null) ActivityLog.Log("Task Completed", task.Description);
                return true;
            }
            return false;
        }
        public bool CompleteReminder(string description)
        {
            ReminderItem? reminder = null;

            // First try to find by description
            reminder = _reminders.FirstOrDefault(r => r.Description.Equals(description, StringComparison.OrdinalIgnoreCase) && !r.IsCompleted);

            // If not found, try to parse as a number
            if (reminder == null)
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(description, @"\d+");
                if (numberMatch.Success && int.TryParse(numberMatch.Value, out int reminderNumber))
                {
                    var activeReminders = GetActiveReminders();
                    if (reminderNumber >= 1 && reminderNumber <= activeReminders.Count)
                    {
                        reminder = activeReminders[reminderNumber - 1];
                    }
                }
            }

            // If still not found, try word numbers
            if (reminder == null)
            {
                int numberFromWords = ExtractTaskNumberFromText(description);
                if (numberFromWords > 0)
                {
                    var activeReminders = GetActiveReminders();
                    if (numberFromWords <= activeReminders.Count)
                    {
                        reminder = activeReminders[numberFromWords - 1];
                    }
                }
            }

            if (reminder != null)
            {
                reminder.IsCompleted = true;
                _reminders.Remove(reminder);
                _completedReminders.Add(reminder);

                if (_dbAvailable)
                {
                    try
                    {
                        var dbTasks = _dbRepo.GetAllTasks();
                        var dbTask = dbTasks.FirstOrDefault(t => t.Title.Equals(reminder.Description, StringComparison.OrdinalIgnoreCase) && !t.IsCompleted);
                        if (dbTask != null)
                        {
                            _dbRepo.CompleteTask(dbTask.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to complete reminder in database: {ex.Message}");
                    }
                }

                if (ActivityLog != null) ActivityLog.Log("Reminder Completed", reminder.Description);
                return true;
            }

            return false;
        }
        public bool DeleteTaskByDescription(string description, RecycleBin? recycleBin = null)
        {
            ReminderItem? task = null;

            // First try to find by description
            task = _tasks.FirstOrDefault(t => t.Description.Equals(description, StringComparison.OrdinalIgnoreCase));

            // If not found, try to parse as a number
            if (task == null)
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(description, @"\d+");
                if (numberMatch.Success && int.TryParse(numberMatch.Value, out int taskNumber))
                {
                    var activeTasks = GetActiveTasks();
                    if (taskNumber >= 1 && taskNumber <= activeTasks.Count)
                    {
                        task = activeTasks[taskNumber - 1];
                    }
                }
            }

            // If still not found, try word numbers (first, second, etc.)
            if (task == null)
            {
                int numberFromWords = ExtractTaskNumberFromText(description);
                if (numberFromWords > 0)
                {
                    var activeTasks = GetActiveTasks();
                    if (numberFromWords <= activeTasks.Count)
                    {
                        task = activeTasks[numberFromWords - 1];
                    }
                }
            }

            // If still not found, try partial match
            if (task == null)
            {
                var allTasks = GetActiveTasks();
                var matchingTasks = allTasks.Where(t => t.Description.Contains(description, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchingTasks.Count == 1)
                {
                    task = matchingTasks.First();
                }
                else if (matchingTasks.Count > 1)
                {
                    // Multiple matches - return false and let caller handle it
                    return false;
                }
            }

            if (task != null)
            {
                if (recycleBin != null)
                {
                    recycleBin.AddTask(task.Description);
                    if (ActivityLog != null) ActivityLog.Log("Task Sent to Recycle Bin", task.Description);
                }

                _tasks.Remove(task);

                if (_dbAvailable)
                {
                    try
                    {
                        var dbTasks = _dbRepo.GetAllTasks();
                        var dbTask = dbTasks.FirstOrDefault(t => t.Title.Equals(task.Description, StringComparison.OrdinalIgnoreCase));
                        if (dbTask != null)
                        {
                            _dbRepo.DeleteTask(dbTask.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete task from database: {ex.Message}");
                    }
                }

                if (ActivityLog != null) ActivityLog.Log("Task Deleted", task.Description);
                return true;
            }
            return false;
        }

        private int ExtractTaskNumberFromText(string text)
        {
            string[] wordNumbers = { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
            string[] ordinals = { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth" };
            string[] ordinalShort = { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th" };

            string lowerText = text.ToLowerInvariant();

            for (int i = 0; i < ordinals.Length; i++)
            {
                if (lowerText.Contains(ordinals[i]))
                    return i + 1;
            }

            for (int i = 0; i < ordinalShort.Length; i++)
            {
                if (lowerText.Contains(ordinalShort[i]))
                    return i + 1;
            }

            for (int i = 0; i < wordNumbers.Length; i++)
            {
                if (lowerText.Contains(wordNumbers[i]))
                    return i + 1;
            }

            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                if (number >= 1 && number <= 20)
                    return number;
            }

            return -1;
        }

        public bool DeleteReminderByDescription(string description, RecycleBin? recycleBin = null)
        {
            ReminderItem? reminder = null;

            // First try to find by description
            reminder = _reminders.FirstOrDefault(r => r.Description.Equals(description, StringComparison.OrdinalIgnoreCase));

            // If not found, try to parse as a number
            if (reminder == null)
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(description, @"\d+");
                if (numberMatch.Success && int.TryParse(numberMatch.Value, out int reminderNumber))
                {
                    var activeReminders = GetActiveReminders();
                    if (reminderNumber >= 1 && reminderNumber <= activeReminders.Count)
                    {
                        reminder = activeReminders[reminderNumber - 1];
                    }
                }
            }

            // If still not found, try word numbers (first, second, etc.)
            if (reminder == null)
            {
                int numberFromWords = ExtractTaskNumberFromText(description);
                if (numberFromWords > 0)
                {
                    var activeReminders = GetActiveReminders();
                    if (numberFromWords <= activeReminders.Count)
                    {
                        reminder = activeReminders[numberFromWords - 1];
                    }
                }
            }

            // If still not found, try partial match
            if (reminder == null)
            {
                var allReminders = GetActiveReminders();
                var matchingReminders = allReminders.Where(r => r.Description.Contains(description, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchingReminders.Count == 1)
                {
                    reminder = matchingReminders.First();
                }
                else if (matchingReminders.Count > 1)
                {
                    return false;
                }
            }

            if (reminder != null)
            {
                if (recycleBin != null)
                {
                    recycleBin.AddReminder(reminder.Description);
                    if (ActivityLog != null) ActivityLog.Log("Reminder Sent to Recycle Bin", reminder.Description);
                }

                _reminders.Remove(reminder);

                if (_dbAvailable)
                {
                    try
                    {
                        var dbTasks = _dbRepo.GetAllTasks();
                        var dbTask = dbTasks.FirstOrDefault(t => t.Title.Equals(reminder.Description, StringComparison.OrdinalIgnoreCase));
                        if (dbTask != null)
                        {
                            _dbRepo.DeleteTask(dbTask.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete reminder from database: {ex.Message}");
                    }
                }

                if (ActivityLog != null) ActivityLog.Log("Reminder Deleted", reminder.Description);
                return true;
            }
            return false;
        }
        public bool DeleteCompletedTaskByDescription(string description)
        {
            ReminderItem? task = null;

            // First try to find by description in _tasks (completed ones)
            task = _tasks.FirstOrDefault(t => t.Description.Equals(description, StringComparison.OrdinalIgnoreCase) && t.IsCompleted);

            // If not found, try in _completedTasks
            if (task == null)
            {
                task = _completedTasks.FirstOrDefault(t => t.Description.Equals(description, StringComparison.OrdinalIgnoreCase));
            }

            // If not found, try to parse as a number
            if (task == null)
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(description, @"\d+");
                if (numberMatch.Success && int.TryParse(numberMatch.Value, out int taskNumber))
                {
                    var completedTasks = GetCompletedTasks();
                    if (taskNumber >= 1 && taskNumber <= completedTasks.Count)
                    {
                        task = completedTasks[taskNumber - 1];
                    }
                }
            }

            // If still not found, try word numbers
            if (task == null)
            {
                int numberFromWords = ExtractTaskNumberFromText(description);
                if (numberFromWords > 0)
                {
                    var completedTasks = GetCompletedTasks();
                    if (numberFromWords <= completedTasks.Count)
                    {
                        task = completedTasks[numberFromWords - 1];
                    }
                }
            }

            if (task != null)
            {
                // Remove from _tasks if it's there
                _tasks.Remove(task);
                // Remove from _completedTasks if it's there
                _completedTasks.Remove(task);

                if (_dbAvailable)
                {
                    try
                    {
                        var dbTasks = _dbRepo.GetAllTasks();
                        var dbTask = dbTasks.FirstOrDefault(t => t.Title.Equals(task.Description, StringComparison.OrdinalIgnoreCase));
                        if (dbTask != null)
                        {
                            _dbRepo.DeleteTask(dbTask.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete completed task from database: {ex.Message}");
                    }
                }
                if (ActivityLog != null) ActivityLog.Log("Completed Task Deleted", task.Description);
                return true;
            }
            return false;
        }
        // ================================================================
        // DELETE ALL TASKS - With Recycle Bin Support
        // ================================================================
        public int DeleteAllTasks(RecycleBin? recycleBin = null)
        {
            var activeTasks = _tasks.Where(t => !t.IsCompleted).ToList();
            int count = activeTasks.Count;

            if (recycleBin != null)
            {
                foreach (var task in activeTasks)
                {
                    recycleBin.AddTask(task.Description);
                }
                if (ActivityLog != null) ActivityLog.Log("All Tasks Sent to Recycle Bin", $"{count} tasks");
            }

            foreach (var task in activeTasks)
            {
                _tasks.Remove(task);
            }
            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks.Where(t => !t.IsCompleted))
                    {
                        _dbRepo.DeleteTask(t.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete all tasks from database: {ex.Message}");
                }
            }
            if (ActivityLog != null) ActivityLog.Log("All Tasks Deleted", $"{count} tasks removed");
            return count;
        }
        // ================================================================
        // DELETE ALL REMINDERS - With Recycle Bin Support
        // ================================================================
        public int DeleteAllReminders(RecycleBin? recycleBin = null)
        {
            var activeReminders = _reminders.Where(r => !r.IsCompleted).ToList();
            int count = activeReminders.Count;

            if (recycleBin != null)
            {
                foreach (var reminder in activeReminders)
                {
                    recycleBin.AddReminder(reminder.Description);
                }
                if (ActivityLog != null) ActivityLog.Log("All Reminders Sent to Recycle Bin", $"{count} reminders");
            }

            foreach (var reminder in activeReminders)
            {
                _reminders.Remove(reminder);
            }

            // Also remove from completed reminders
            _completedReminders.Clear();

            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks)
                    {
                        if (_reminders.Any(r => r.Description.Equals(t.Title, StringComparison.OrdinalIgnoreCase)) ||
                            activeReminders.Any(r => r.Description.Equals(t.Title, StringComparison.OrdinalIgnoreCase)))
                        {
                            _dbRepo.DeleteTask(t.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete all reminders from database: {ex.Message}");
                }
            }
            if (ActivityLog != null) ActivityLog.Log("All Reminders Deleted", $"{count} reminders removed");
            return count;
        }

        // ================================================================
        // DELETE ALL COMPLETED TASKS - With Recycle Bin Support
        // ================================================================
        public int DeleteAllCompleted(RecycleBin? recycleBin = null)
        {
            // Get ALL completed tasks (both from _tasks and _completedTasks)
            var completed = _tasks.Where(t => t.IsCompleted).ToList();
            // Also get from _completedTasks
            completed.AddRange(_completedTasks);

            int count = completed.Count;

            // Add to recycle bin if provided
            if (recycleBin != null)
            {
                foreach (var task in completed)
                {
                    recycleBin.AddTask($"[COMPLETED] {task.Description}");
                }
                if (ActivityLog != null) ActivityLog.Log("All Completed Tasks Sent to Recycle Bin", $"{count} completed tasks");
            }

            // Remove from _tasks
            _tasks.RemoveAll(t => t.IsCompleted);

            // Clear _completedTasks
            _completedTasks.Clear();
            _completedReminders.Clear();

            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks.Where(t => t.IsCompleted))
                    {
                        _dbRepo.DeleteTask(t.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete all completed tasks from database: {ex.Message}");
                }
            }
            if (ActivityLog != null) ActivityLog.Log("All Completed Deleted", $"{count} completed tasks removed");
            return count;
        }

        // ================================================================
        // DELETE EVERYTHING (tasks, reminders, and completed)
        // ================================================================
        public int DeleteAll()
        {
            int total = _tasks.Count + _reminders.Count;
            _tasks.Clear();
            _reminders.Clear();
            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks)
                    {
                        _dbRepo.DeleteTask(t.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete all from database: {ex.Message}");
                }
            }
            if (ActivityLog != null) ActivityLog.Log("All Cleared", $"{total} items removed");
            return total;
        }


        // ================================================================
        // DELETE TASKS AND COMPLETED (both active tasks and completed)
        // ================================================================
        public int DeleteTasksAndCompleted()
        {
            var toRemove = _tasks.ToList();
            int count = toRemove.Count;
            _tasks.Clear();
            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks)
                    {
                        _dbRepo.DeleteTask(t.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete tasks and completed from database: {ex.Message}");
                }
            }
            if (ActivityLog != null) ActivityLog.Log("Tasks & Completed Deleted", $"{count} items removed");
            return count;
        }

        // ================================================================
        // DELETE TASKS AND REMINDERS
        // ================================================================
        public int DeleteTasksAndReminders()
        {
            int count = _tasks.Count + _reminders.Count;
            _tasks.Clear();
            _reminders.Clear();
            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks)
                    {
                        _dbRepo.DeleteTask(t.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete tasks and reminders from database: {ex.Message}");
                }
            }
            if (ActivityLog != null) ActivityLog.Log("Tasks & Reminders Deleted", $"{count} items removed");
            return count;
        }

        // ================================================================
        // DELETE REMINDERS AND COMPLETED
        // ================================================================
        public int DeleteRemindersAndCompleted()
        {
            int count = _reminders.Count + _tasks.Count(t => t.IsCompleted);
            _reminders.Clear();
            _tasks.RemoveAll(t => t.IsCompleted);
            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks)
                    {
                        bool isReminder = _reminders.Any(r => r.Description.Equals(t.Title, StringComparison.OrdinalIgnoreCase));
                        bool isCompleted = t.IsCompleted;

                        if (isReminder || isCompleted)
                        {
                            _dbRepo.DeleteTask(t.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete reminders and completed from database: {ex.Message}");
                }
            }
            if (ActivityLog != null) ActivityLog.Log("Reminders & Completed Deleted", $"{count} items removed");
            return count;
        }

        public bool ReminderExists(string description)
        {
            return _reminders.Any(r => r.Description.Equals(description, StringComparison.OrdinalIgnoreCase) && !r.IsCompleted);
        }

        public bool TaskExists(string description)
        {
            return _tasks.Any(t => t.Description.Equals(description, StringComparison.OrdinalIgnoreCase) && !t.IsCompleted);
        }

        public ReminderItem? GetTask(string description)
        {
            return _tasks.FirstOrDefault(t => t.Description.Equals(description, StringComparison.OrdinalIgnoreCase) && !t.IsCompleted);
        }

        public ReminderItem? GetReminder(string description)
        {
            return _reminders.FirstOrDefault(r => r.Description.Equals(description, StringComparison.OrdinalIgnoreCase) && !r.IsCompleted);
        }

        public string GetSummary()
        {
            var result = new List<string>();

            var activeReminders = GetActiveReminders();
            var activeTasks = GetActiveTasks();

            result.Add("⏰ REMINDERS:");
            if (activeReminders.Count == 0)
                result.Add("  NO REMINDERS SET AS OF YET");
            else
            {
                int counter = 1;
                foreach (var r in activeReminders)
                {
                    if (r.ReminderDate.HasValue)
                        result.Add($"  .[{counter}] {r.Description} (Due: {r.ReminderDate.Value:dd MMMM yyyy})");
                    else
                        result.Add($"  .[{counter}] {r.Description} (No reminder set)");
                    counter++;
                }
            }

            result.Add("");
            result.Add("📌 TASKS:");
            if (activeTasks.Count == 0)
                result.Add("  NO TASKS SET AS OF YET");
            else
            {
                int counter = 1;
                foreach (var t in activeTasks)
                {
                    if (t.ReminderDate.HasValue)
                        result.Add($"  .[{counter}] {t.Description} (Due: {t.ReminderDate.Value:dd MMMM yyyy}) - Pending");
                    else
                        result.Add($"  .[{counter}] {t.Description} (No reminder set) - Pending");
                    counter++;
                }
            }

            // Get completed tasks - from BOTH sources
            var completedTasks = new List<ReminderItem>();
            completedTasks.AddRange(_tasks.Where(t => t.IsCompleted));
            completedTasks.AddRange(_completedTasks);

            result.Add("");
            result.Add("✅ COMPLETED:");
            if (completedTasks.Count == 0)
                result.Add("  NO COMPLETED TASKS SET AS OF YET");
            else
            {
                foreach (var t in completedTasks)
                {
                    result.Add($"  • Task - {t.Description} is completed");
                }
            }

            if (activeReminders.Count == 0 && activeTasks.Count == 0 && completedTasks.Count == 0)
                return "⏰ REMINDERS:\n  NO REMINDERS SET AS OF YET\n\n📌 TASKS:\n  NO TASKS SET AS OF YET\n\n✅ COMPLETED:\n  NO COMPLETED TASKS SET AS OF YET";

            return string.Join("\n", result);
        }
        public string GetRemindersOnly()
        {
            var activeReminders = GetActiveReminders();
            if (activeReminders.Count == 0)
                return "No active reminders.";

            var result = new List<string>();
            foreach (var r in activeReminders)
            {
                if (r.ReminderDate.HasValue)
                    result.Add($"  • {r.Description} (Due: {r.ReminderDate.Value:dd MMMM yyyy})");
                else
                    result.Add($"  • {r.Description} (No reminder set)");
            }
            return string.Join("\n", result);
        }

        public string GetTasksOnly()
        {
            var activeTasks = GetActiveTasks();
            if (activeTasks.Count == 0)
                return "No active tasks.";

            var result = new List<string>();
            foreach (var t in activeTasks)
            {
                if (t.ReminderDate.HasValue)
                    result.Add($"  • {t.Description} (Due: {t.ReminderDate.Value:dd MMMM yyyy})");
                else
                    result.Add($"  • {t.Description} (No reminder set)");
            }
            return string.Join("\n", result);
        }

        public void ClearAll()
        {
            _reminders.Clear();
            _tasks.Clear();
            if (_dbAvailable)
            {
                try
                {
                    var dbTasks = _dbRepo.GetAllTasks();
                    foreach (var t in dbTasks)
                    {
                        _dbRepo.DeleteTask(t.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to clear database: {ex.Message}");
                }
            }
        }
    }

    // ================= MAIN CHATBOT INTERFACE =================
    public partial class CHATBOT_INTERFACE : Window
    {
        private readonly Dictionary<string, string> _accounts = new Dictionary<string, string>();
        private readonly CybersecurityKnowledgeBase _knowledgeBase;
        private readonly KeywordMatcher _keywordMatcher;
        private readonly BotResponseGenerator _responseGenerator;
        private readonly ConversationContext _conversationContext;
        private CybersecurityQuiz _quiz;
        private bool _isQuizMode = false;
        private bool _isLoginMode = true;
        private bool _isPasswordVisible = false;
        private bool _isLoggedIn = true;
        private static string? _loggedInUsername = "Guest";
        public static string? LoggedInUsername { get => _loggedInUsername; set => _loggedInUsername = value; }
        private string _loginMode = "LOGIN";
        private int _animFrame = 0;
        private DispatcherTimer? _logoAnimTimer;
        private int _wavDurationMs = 5000;
        private static readonly Color[] _palette = { Color.FromRgb(0xFF, 0x14, 0x93), Color.FromRgb(0xFF, 0x66, 0xB2), Color.FromRgb(0xFF, 0xFF, 0xFF) };
        private string[] _didYouKnowMessages = { "Cybersecurity is everyone's responsibility!", "80% of cyber attacks could be prevented with basic security measures.", "The average cost of a data breach is over $4 million!", "Human error causes 95% of cybersecurity breaches.", "Using a password manager makes you 80% less likely to be hacked.", "Two-factor authentication blocks 99.9% of automated attacks.", "Cybercrime is expected to cost $10.5 trillion annually by 2025." };
        private int _didYouKnowIndex = 0;
        private DispatcherTimer _didYouKnowTimer;
        private Border? _permanentAsciiArt;
        private Border? _logoutPopup;
        private bool _isLogoutPopupVisible = false;

        // Activity Log
        private ActivityLog _activityLog = new ActivityLog();

        // Quiz UI Elements
        private Border _quizOverlay = null!;
        private Border _quizCard = null!;
        private TextBlock _quizQuestionText = null!;
        private StackPanel _quizOptionsPanel = null!;
        private TextBlock _quizProgressText = null!;
        private TextBlock _quizFeedbackText = null!;
        private Border _feedbackPanel = null!;
        private bool _showingDifficulty = false;
        private bool _showingFeedback = false;
        private DispatcherTimer _quizTimer;
        private DateTime _quizStartTime;

        // Task Manager
        private TaskManager _taskManager = new TaskManager();

        private bool _waitingForReminderTopic = false;
        private bool IsReminderRequest(string input)
        {
            string lower = input.ToLowerInvariant();
            return lower.Contains("remind") ||
                   lower.Contains("reminder") ||
                   lower.Contains("set a reminder") ||
                   lower.Contains("add a reminder");
        }

        private string _pendingTaskDescription = string.Empty;
        private DateTime? _pendingTaskDate = null;
        private bool _isWaitingForTaskConfirmation = false;
        private bool _isTaskFlowActive = false;
        private string _currentTaskDescription = string.Empty;
        private DateTime? _currentTaskDueDate = null;
        private bool _waitingForDateSelection = false;
        private Border? _currentSubmitButton;
        private Border? _taskConfirmationPopup;
        private Border? _taskButtonsPanel;
        private Border? _calendarPanel;
        private DatePicker? _taskDatePicker;

        // Recycle Bin
        private RecycleBin _recycleBin = new RecycleBin();
        private bool _isRecycleBinVisible = false;
        private bool _showFullLog = false;
        private Border? _activityLogContainer = null;

        public CHATBOT_INTERFACE()
        {
            InitializeComponent();
            _knowledgeBase = new CybersecurityKnowledgeBase();
            _keywordMatcher = new KeywordMatcher();
            _responseGenerator = new BotResponseGenerator(_knowledgeBase, _keywordMatcher);
            _conversationContext = new ConversationContext();
            _conversationContext.UserDisplayName = "Guest";
            _conversationContext.Memory.UserName = "Guest";
            _loggedInUsername = "Guest";
            _responseGenerator.ScrollToFirstKeywordConversation = ScrollToFirstKeywordConversation;
            _accounts.Add("Guest", "Aa1!");
            AvatarDefaultIcon.Visibility = Visibility.Visible;
            AvatarInitial.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Visible;
            UpdateUiMode();
            _ = StartNewAnimationSequence();
            UpdateAvatarForGuest();
            _didYouKnowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            _didYouKnowTimer.Tick += (s, e) => UpdateDidYouKnow();
            _didYouKnowTimer.Start();

            // Link ActivityLog to everything
            _responseGenerator.ActivityLog = _activityLog;
            _taskManager.ActivityLog = _activityLog;

            // Initialize Recycle Bin with counter subscription
            _recycleBin = new RecycleBin();
            _recycleBin.ItemsChanged += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateRecycleBinCounter();
                });
            };

            _quiz = new CybersecurityQuiz();
            _quiz.ActivityLog = _activityLog;

            _quizTimer = new DispatcherTimer();
            _quizTimer.Interval = TimeSpan.FromSeconds(1);
            _quizTimer.Tick += QuizTimer_Tick!;

            // Create the quiz overlay and card
            CreateQuizOverlay();

            // Log startup
            _activityLog.Log("Chatbot Started", "Application initialized");

            // Initial update of recycle bin counter
            UpdateRecycleBinCounter();
        }

        private void UpdateRecycleBinCounter()
        {
            Dispatcher.Invoke(() =>
            {
                if (RecycleBinCounter == null) return;

                int count = _recycleBin.Count;
                if (count > 0)
                {
                    RecycleBinCounter.Visibility = Visibility.Visible;
                    RecycleBinCountText.Text = count.ToString();

                    // Pulse animation to draw attention
                    if (RecycleBinCounter.RenderTransform == null || !(RecycleBinCounter.RenderTransform is ScaleTransform))
                    {
                        RecycleBinCounter.RenderTransform = new ScaleTransform(1, 1);
                        RecycleBinCounter.RenderTransformOrigin = new Point(0.5, 0.5);
                    }

                    var scaleTransform = RecycleBinCounter.RenderTransform as ScaleTransform;
                    var pulseAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.3,
                        Duration = TimeSpan.FromMilliseconds(200),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2)
                    };
                    scaleTransform?.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                    scaleTransform?.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
                }
                else
                {
                    RecycleBinCounter.Visibility = Visibility.Visible;
                    RecycleBinCountText.Text = "0";
                }
            });
        }

        private void QuizTimer_Tick(object sender, EventArgs e)
        {
            UpdateTimerDisplay();
        }

        private void CreateQuizOverlay()
        {
            // Create a container that acts like a chat message
            var quizContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 13, 13, 26)),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 10),
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = "QuizContainer"
            };

            var mainStack = new StackPanel();

            // Header with close button
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.Margin = new Thickness(0, 0, 0, 10);

            var titleText = new TextBlock
            {
                Text = "🎮 QUIZ MODE",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            var closeButton = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                CornerRadius = new CornerRadius(12),
                Width = 28,
                Height = 28,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            var closeX = new TextBlock
            {
                Text = "✕",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Child = closeX;
            closeButton.MouseEnter += (s, e) => { closeButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)); closeX.Foreground = Brushes.White; };
            closeButton.MouseLeave += (s, e) => { closeButton.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)); closeX.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)); };
            closeButton.MouseLeftButtonDown += (s, e) => QuitQuiz();
            Grid.SetColumn(closeButton, 1);
            headerGrid.Children.Add(closeButton);
            mainStack.Children.Add(headerGrid);

            // Quiz card
            _quizCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                CornerRadius = new CornerRadius(15),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(25, 20, 25, 20),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Progress
            var progressGrid = new Grid();
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            progressGrid.Margin = new Thickness(0, 0, 0, 8);

            _quizProgressText = new TextBlock
            {
                Text = "",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(_quizProgressText, 0);
            progressGrid.Children.Add(_quizProgressText);
            Grid.SetRow(progressGrid, 0);
            cardGrid.Children.Add(progressGrid);

            // Question
            var questionBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(0.5),
                MinHeight = 50
            };
            _quizQuestionText = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            questionBorder.Child = _quizQuestionText;
            Grid.SetRow(questionBorder, 1);
            cardGrid.Children.Add(questionBorder);

            // Options
            _quizOptionsPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_quizOptionsPanel, 2);
            cardGrid.Children.Add(_quizOptionsPanel);

            // Feedback
            _feedbackPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 5, 0, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(0.5),
                Visibility = Visibility.Collapsed
            };
            _quizFeedbackText = new TextBlock
            {
                FontSize = 13,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            _feedbackPanel.Child = _quizFeedbackText;
            Grid.SetRow(_feedbackPanel, 3);
            cardGrid.Children.Add(_feedbackPanel);

            // Next button
            var navPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(navPanel, 4);
            cardGrid.Children.Add(navPanel);

            _quizCard.Child = cardGrid;
            mainStack.Children.Add(_quizCard);

            quizContainer.Child = mainStack;

            // Store reference
            _quizOverlay = quizContainer;

            // Add to MessagesPanel at the END
            MessagesPanel.Children.Add(_quizOverlay);
        }

        private void UpdateTimerDisplay()
        {
            if (_quizOptionsPanel != null && _quizOptionsPanel.Children.Count > 0)
            {
                var mainContainer = _quizOptionsPanel.Children.OfType<Grid>().FirstOrDefault();
                if (mainContainer != null)
                {
                    var headerGrid = mainContainer.Children.OfType<Grid>().FirstOrDefault();
                    if (headerGrid != null)
                    {
                        var timerText = headerGrid.Children.OfType<TextBlock>().LastOrDefault();
                        if (timerText != null)
                        {
                            var elapsed = DateTime.Now - _quizStartTime;
                            timerText.Text = $"⏱️ {elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                        }
                    }
                }
            }
        }

        private void UpdateAvatarForGuest() { if (!_isLoggedIn || _loggedInUsername == "Guest") { AvatarInitial.Text = "G"; AvatarDefaultIcon.Visibility = Visibility.Collapsed; AvatarInitial.Visibility = Visibility.Visible; ChatUsernameText.Text = "Logged in as: Guest"; } }

        private async Task StartNewAnimationSequence()
        {
            BuildAsciiArt();
            SplashOverlay.Visibility = Visibility.Visible;
            AsciiLayer.Opacity = 0;

            try { string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BOTBUDDY.wav"); if (System.IO.File.Exists(path)) { _wavDurationMs = GetWavDurationMs(path); new SoundPlayer(path).Play(); } } catch { }
            await ShowCuteAiBotWave();
            await SlideAsciiArtToChatArea();
            MainInterface.Visibility = Visibility.Visible;
            _logoAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _logoAnimTimer.Tick += (s, e) => { _animFrame++; AnimateLogoText(); };
            _logoAnimTimer.Start();
            SplashOverlay.Visibility = Visibility.Collapsed;
        }

        private async Task ShowCuteAiBotWave()
        {
            // Fade in ASCII layer
            var asciiOpacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600));
            AsciiLayer.BeginAnimation(OpacityProperty, asciiOpacityAnimation);

            // Wave animation on TxtBot
            var waveAnimation = new DoubleAnimation
            {
                From = -8,
                To = 8,
                Duration = TimeSpan.FromMilliseconds(80),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(4)
            };

            var rotateTransform = new RotateTransform();
            TxtBot.RenderTransform = rotateTransform;
            TxtBot.RenderTransformOrigin = new Point(0.5, 0.5);
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, waveAnimation);

            // Glow pulse effect
            var glowAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(600),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };

            if (TxtBot.Effect is DropShadowEffect shadowEffect)
            {
                shadowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, glowAnimation);
            }

            // Hold for display
            await Task.Delay(3000);

            // Fade out ASCII layer
            var asciiOpacityOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            AsciiLayer.BeginAnimation(OpacityProperty, asciiOpacityOut);

            await Task.Delay(600);

            // Reset transform
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            rotateTransform.Angle = 0;
        }

        private async Task SlideAsciiArtToChatArea()
        {
            _permanentAsciiArt = new Border { Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x0D)), CornerRadius = new CornerRadius(15), BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)), BorderThickness = new Thickness(1.5), Margin = new Thickness(0, 5, 0, 15), Padding = new Thickness(15, 10, 15, 10), HorizontalAlignment = HorizontalAlignment.Center };
            var cyberClone = new TextBlock { Text = TxtCyber.Text ?? "", FontFamily = new FontFamily("Consolas"), FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44)), FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center };
            var botClone = new TextBlock { Text = TxtBot.Text ?? "", FontFamily = new FontFamily("Consolas"), FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0xFF)), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 0, 0), TextAlignment = TextAlignment.Center };
            var taglineClone = new TextBlock { Text = "⚡ YOUR SECURITY, OUR MISSION ⚡", FontFamily = new FontFamily("Consolas"), FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 0), TextAlignment = TextAlignment.Center };
            var asciiStack = new StackPanel();
            asciiStack.Children.Add(cyberClone);
            asciiStack.Children.Add(botClone);
            asciiStack.Children.Add(taglineClone);
            _permanentAsciiArt.Child = asciiStack;
            _permanentAsciiArt.RenderTransform = new TranslateTransform(-900, 0);
            MessagesPanel.Children.Insert(0, _permanentAsciiArt);
            var slideIn = new DoubleAnimation(-900, 0, TimeSpan.FromMilliseconds(1000)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            var translateTransform = _permanentAsciiArt.RenderTransform as TranslateTransform;
            translateTransform?.BeginAnimation(TranslateTransform.XProperty, slideIn);
            await Task.Delay(1100);
            var pulseScale = new DoubleAnimation(1, 1.02, TimeSpan.FromMilliseconds(200)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(2) };
            var scaleTransform = new ScaleTransform(1, 1);
            _permanentAsciiArt.RenderTransform = scaleTransform;
            _permanentAsciiArt.RenderTransformOrigin = new Point(0.5, 0.5);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseScale);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseScale);
        }

        private void UpdateDidYouKnow() { _didYouKnowIndex = (_didYouKnowIndex + 1) % _didYouKnowMessages.Length; DidYouKnowText.Text = _didYouKnowMessages[_didYouKnowIndex]; }

        private void UpdateFavoritesSidebar()
        {
            Dispatcher.Invoke(() =>
            {
                FavoritesPanel.Children.Clear();

                // LAST DISCUSSED Section
                var lastDiscussedLabel = new TextBlock
                {
                    Text = "🕐 LAST DISCUSSED",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                    FontFamily = new FontFamily("Consolas"),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                FavoritesPanel.Children.Add(lastDiscussedLabel);

                if (!string.IsNullOrEmpty(_conversationContext.Memory.LastDiscussedCyberKeyword))
                {
                    var lastBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                        CornerRadius = new CornerRadius(10),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                        BorderThickness = new Thickness(1.5),
                        Margin = new Thickness(0, 0, 0, 15),
                        Padding = new Thickness(12, 10, 12, 10)
                    };
                    var lastStack = new StackPanel();
                    lastStack.Children.Add(new TextBlock
                    {
                        Text = _conversationContext.Memory.LastDiscussedCyberKeyword.ToUpper(),
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        FontFamily = new FontFamily("Consolas")
                    });
                    lastBorder.Child = lastStack;
                    FavoritesPanel.Children.Add(lastBorder);
                }
                else
                {
                    FavoritesPanel.Children.Add(new TextBlock
                    {
                        Text = "No topic discussed yet.\nAsk 'What is phishing?' to start!",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        FontFamily = new FontFamily("Consolas"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 15)
                    });
                }

                // TOPICS COVERED Section
                var coveredLabel = new TextBlock
                {
                    Text = "📚 TOPICS COVERED",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                    FontFamily = new FontFamily("Consolas"),
                    Margin = new Thickness(0, 10, 0, 8)
                };
                FavoritesPanel.Children.Add(coveredLabel);

                var coveredTopics = _conversationContext.Memory.CoveredTopics.ToList();
                if (coveredTopics.Count == 0)
                {
                    FavoritesPanel.Children.Add(new TextBlock
                    {
                        Text = "No topics covered yet.\nAsk 'What is phishing?' to start learning!",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        FontFamily = new FontFamily("Consolas"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 15)
                    });
                }
                else
                {
                    foreach (var topic in coveredTopics)
                    {
                        // Get the usage status for each action
                        bool exampleUsed = _conversationContext.Memory.IsExampleUsed(topic);
                        bool moreUsed = _conversationContext.Memory.IsMoreUsed(topic);

                        // Get tip progress using the new tracking methods
                        int tipCount = _conversationContext.Memory.GetTipCount(topic);
                        int totalTips = _conversationContext.Memory.GetTotalTipCount(topic, _knowledgeBase);
                        bool allTipsUsed = _conversationContext.Memory.AllTipsUsed(topic, _knowledgeBase);

                        var topicBorder = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                            CornerRadius = new CornerRadius(10),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                            BorderThickness = new Thickness(1),
                            Margin = new Thickness(0, 5, 0, 5),
                            Padding = new Thickness(12, 10, 12, 10),
                            Tag = topic
                        };

                        var mainStack = new StackPanel();

                        // Title row with topic and dustbin
                        var titleRow = new Grid();
                        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        titleRow.Margin = new Thickness(0, 0, 0, 5);

                        // Topic title (clickable)
                        var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
                        var titleBorder = new Border
                        {
                            Cursor = Cursors.Hand,
                            Background = Brushes.Transparent,
                            Child = titleStack,
                            Tag = topic
                        };
                        titleBorder.MouseLeftButtonDown += (s, e) =>
                        {
                            string clickedTopic = (titleBorder.Tag as string) ?? topic;
                            ScrollToFirstKeywordConversation(clickedTopic);
                        };

                        titleStack.Children.Add(new TextBlock
                        {
                            Text = "✅ ",
                            FontSize = 12,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        titleStack.Children.Add(new TextBlock
                        {
                            Text = topic.ToUpper(),
                            FontSize = 12,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                            FontFamily = new FontFamily("Consolas"),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        Grid.SetColumn(titleBorder, 0);
                        titleRow.Children.Add(titleBorder);

                        // Dustbin delete button (small black) - DELETES ALL messages and stores only the topic
                        var deleteButton = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
                            CornerRadius = new CornerRadius(8),
                            Width = 24,
                            Height = 24,
                            Cursor = Cursors.Hand,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(8, 0, 0, 0),
                            Tag = topic
                        };
                        var deleteIcon = new TextBlock
                        {
                            Text = "🗑️",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        deleteButton.Child = deleteIcon;

                        deleteButton.MouseEnter += (s, e) =>
                        {
                            deleteButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93));
                            deleteIcon.Foreground = Brushes.White;
                        };
                        deleteButton.MouseLeave += (s, e) =>
                        {
                            deleteButton.Background = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
                            deleteIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
                        };
                        // DELETE BUTTON - Stores the full conversation with all state info
                        deleteButton.MouseLeftButtonDown += (s, e) =>
                        {
                            var border = s as Border;
                            if (border != null && border.Tag is string topicToDelete)
                            {
                                // Find ALL messages (user + bot) for this topic
                                var messagesToRemove = _conversationContext.Messages
                                    .Where(m => m.Message.ToLowerInvariant().Contains(topicToDelete.ToLowerInvariant()))
                                    .ToList();

                                // Store the FULL conversation with all messages AND state flags
                                if (messagesToRemove.Count > 0)
                                {
                                    // Get current state for this topic
                                    string tipCount = _conversationContext.Memory.GetTipCount(topicToDelete).ToString();
                                    string totalTips = _conversationContext.Memory.GetTotalTipCount(topicToDelete, _knowledgeBase).ToString();
                                    string exampleUsed = _conversationContext.Memory.IsExampleUsed(topicToDelete).ToString();
                                    string moreUsed = _conversationContext.Memory.IsMoreUsed(topicToDelete).ToString();
                                    string favorite = _conversationContext.Memory.IsFavoriteTopic(topicToDelete).ToString();

                                    // Format: TOPIC|tipCount|totalTips|exampleUsed|moreUsed|favorite|sender1|message1|||sender2|message2|||...
                                    string messagesPart = string.Join("|||", messagesToRemove.Select(m => $"{m.Sender}|{m.Message}"));
                                    string conversationContent = $"{topicToDelete.ToUpper()}|{tipCount}|{totalTips}|{exampleUsed}|{moreUsed}|{favorite}|{messagesPart}";
                                    _recycleBin.AddConversation(conversationContent);
                                }

                                // Remove ALL messages from conversation context
                                foreach (var msg in messagesToRemove)
                                {
                                    _conversationContext.Messages.Remove(msg);
                                }

                                // Remove ALL messages from UI (user + bot)
                                Dispatcher.Invoke(() =>
                                {
                                    var itemsToRemove = new List<UIElement>();
                                    for (int i = MessagesPanel.Children.Count - 1; i >= 0; i--)
                                    {
                                        var child = MessagesPanel.Children[i];
                                        if (child is Grid grid)
                                        {
                                            bool shouldRemove = false;
                                            foreach (var inner in grid.Children)
                                            {
                                                if (inner is Border bubble && bubble.Child is StackPanel sp)
                                                {
                                                    foreach (var element in sp.Children)
                                                    {
                                                        if (element is TextBlock tb && tb.FontSize == 13)
                                                        {
                                                            if (tb.Text.Contains(topicToDelete, StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                shouldRemove = true;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                if (shouldRemove) break;
                                            }
                                            if (shouldRemove)
                                            {
                                                itemsToRemove.Add(grid);
                                            }
                                        }
                                    }
                                    foreach (var item in itemsToRemove)
                                    {
                                        MessagesPanel.Children.Remove(item);
                                    }
                                });

                                // Remove from covered topics
                                _conversationContext.Memory.CoveredTopics.Remove(topicToDelete);

                                // Reset tip tracking for this topic
                                _conversationContext.Memory.ResetTipTracking(topicToDelete);

                                // Add confirmation message
                                int recycleCount = messagesToRemove.Count;
                                AddBotMessage($"🗑️ {recycleCount} message(s) about '{topicToDelete}' moved to Recycle Bin.");

                                // Update the sidebar
                                UpdateFavoritesSidebar();
                            }
                        };

                        Grid.SetColumn(deleteButton, 1);
                        titleRow.Children.Add(deleteButton);
                        mainStack.Children.Add(titleRow);

                        // Actions buttons row
                        var actionsStack = new StackPanel { Margin = new Thickness(20, 8, 0, 0) };

                        // Tip Button - Shows progress and disables when all tips are used
                        string tipLabel = allTipsUsed ? $"💡 Tip ({tipCount}/{totalTips})" : $"💡 Tip ({tipCount}/{totalTips})";
                        var tipButton = new Button
                        {
                            Content = tipLabel,
                            Height = 28,
                            Margin = new Thickness(0, 2, 0, 2),
                            FontSize = 10,
                            FontFamily = new FontFamily("Consolas"),
                            Foreground = allTipsUsed ? new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)) : Brushes.White,
                            Background = allTipsUsed ? new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) : new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                            Cursor = allTipsUsed ? Cursors.Arrow : Cursors.Hand,
                            IsEnabled = !allTipsUsed,
                            Tag = new Tuple<string, string>(topic, "tip")
                        };
                        tipButton.Click += ActionButton_Click;
                        tipButton.SetValue(Button.TemplateProperty, CreateRoundButtonTemplate());
                        actionsStack.Children.Add(tipButton);

                        // Example Button - Only disabled if Example was explicitly used
                        var exampleButton = new Button
                        {
                            Content = "📖 Example",
                            Height = 28,
                            Margin = new Thickness(0, 2, 0, 2),
                            FontSize = 10,
                            FontFamily = new FontFamily("Consolas"),
                            Foreground = exampleUsed ? new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)) : Brushes.White,
                            Background = exampleUsed ? new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) : new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                            Cursor = exampleUsed ? Cursors.Arrow : Cursors.Hand,
                            IsEnabled = !exampleUsed,
                            Tag = new Tuple<string, string>(topic, "example")
                        };
                        exampleButton.Click += ActionButton_Click;
                        exampleButton.SetValue(Button.TemplateProperty, CreateRoundButtonTemplate());
                        actionsStack.Children.Add(exampleButton);

                        // More Button - Only disabled if More was explicitly used
                        var moreButton = new Button
                        {
                            Content = "🔍 More",
                            Height = 28,
                            Margin = new Thickness(0, 2, 0, 2),
                            FontSize = 10,
                            FontFamily = new FontFamily("Consolas"),
                            Foreground = moreUsed ? new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)) : Brushes.White,
                            Background = moreUsed ? new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) : new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                            Cursor = moreUsed ? Cursors.Arrow : Cursors.Hand,
                            IsEnabled = !moreUsed,
                            Tag = new Tuple<string, string>(topic, "more")
                        };
                        moreButton.Click += ActionButton_Click;
                        moreButton.SetValue(Button.TemplateProperty, CreateRoundButtonTemplate());
                        actionsStack.Children.Add(moreButton);

                        mainStack.Children.Add(actionsStack);
                        topicBorder.Child = mainStack;
                        FavoritesPanel.Children.Add(topicBorder);
                    }
                }

                // FAVORITE TOPICS Section
                var favLabel = new TextBlock
                {
                    Text = "❤️ FAVORITE TOPICS",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                    FontFamily = new FontFamily("Consolas"),
                    Margin = new Thickness(0, 15, 0, 8)
                };
                FavoritesPanel.Children.Add(favLabel);

                if (_conversationContext.Memory.FavoriteTopics.Count == 0)
                {
                    FavoritesPanel.Children.Add(new TextBlock
                    {
                        Text = "No favorites yet.\nTell me 'I like phishing' or 'I'm interested in malware' to add favorites!",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        FontFamily = new FontFamily("Consolas"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    });
                }
                else
                {
                    foreach (var topic in _conversationContext.Memory.FavoriteTopics)
                    {
                        var favBorder = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                            CornerRadius = new CornerRadius(10),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                            BorderThickness = new Thickness(1),
                            Margin = new Thickness(0, 5, 0, 5),
                            Padding = new Thickness(12, 10, 12, 10)
                        };
                        var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
                        titleStack.Children.Add(new TextBlock
                        {
                            Text = "❤️ ",
                            FontSize = 12
                        });
                        titleStack.Children.Add(new TextBlock
                        {
                            Text = topic.ToUpper(),
                            FontSize = 12,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                            FontFamily = new FontFamily("Consolas")
                        });
                        favBorder.Child = titleStack;
                        FavoritesPanel.Children.Add(favBorder);
                    }
                }
            });
        }

        private ControlTemplate CreateRoundButtonTemplate()
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(0));
            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);
            template.VisualTree = border;
            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFF, 0x33, 0x85))));
            template.Triggers.Add(hoverTrigger);
            Trigger disabledTrigger = new Trigger { Property = Button.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33))));
            disabledTrigger.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))));
            template.Triggers.Add(disabledTrigger);
            return template;
        }

        private void ScrollToFirstKeywordConversation(string keyword)
        {
            for (int i = 0; i < _conversationContext.Messages.Count; i++)
            {
                var msg = _conversationContext.Messages[i];
                if (msg.Sender != "BotBuddy" && msg.Message.ToLowerInvariant().Contains(keyword.ToLowerInvariant())) { ScrollToMessage(i); ShowTemporaryMessage($"✨ Clicked on {keyword.ToUpper()} topic! ✨"); return; }
            }
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Tuple<string, string> data)
            {
                string topic = data.Item1;
                string action = data.Item2;
                string question = action switch { "tip" => $"Give me a tip about {topic}", "example" => $"Give me an example of {topic}", "more" => $"Tell me more about {topic}", _ => topic };
                InputBox.Text = question;
                SendMessage();

                // Only disable the button that was clicked
                btn.IsEnabled = false;
                btn.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                btn.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                btn.Cursor = Cursors.Arrow;

                // Only mark the specific action as used
                if (action == "tip") _conversationContext.Memory.MarkTipUsed(topic);
                else if (action == "example") _conversationContext.Memory.MarkExampleUsed(topic);
                else if (action == "more") _conversationContext.Memory.MarkMoreUsed(topic);

                UpdateFavoritesSidebar();
            }
        }

        private void ScrollToMessage(int messageIndex)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (messageIndex >= 0 && messageIndex < MessagesPanel.Children.Count)
                    {
                        var targetMessage = MessagesPanel.Children[messageIndex];
                        if (targetMessage != null)
                        {
                            if (targetMessage is FrameworkElement element) element.BringIntoView();
                            if (targetMessage is Border border)
                            {
                                var originalBg = border.Background;
                                var originalBorderBrush = border.BorderBrush;
                                border.Background = new SolidColorBrush(Color.FromArgb(200, 0xFF, 0xFF, 0x66));
                                border.BorderBrush = new SolidColorBrush(Colors.Gold);
                                border.BorderThickness = new Thickness(2);
                                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2000) };
                                timer.Tick += (s, e) => { border.Background = originalBg; border.BorderBrush = originalBorderBrush; border.BorderThickness = new Thickness(1); timer.Stop(); };
                                timer.Start();
                            }
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Scroll error: {ex.Message}"); }
            });
        }

        private void ShowTemporaryMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var tempMsg = new TextBlock { Text = message, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), FontFamily = new FontFamily("Consolas"), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 5, 0, 5), Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), Padding = new Thickness(10, 5, 10, 5) };
                MessagesPanel.Children.Add(tempMsg);
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s, e) => { MessagesPanel.Children.Remove(tempMsg); timer.Stop(); };
                timer.Start();
            });
        }

        private void BuildAsciiArt() { var asciiArtist = new AsciiArtBuilder(); asciiArtist.BuildCyberArt(TxtCyber, TxtBot, TxtTagline); }

        private int GetWavDurationMs(string path)
        {
            try
            {
                using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                using var br = new System.IO.BinaryReader(fs);
                br.ReadBytes(4); br.ReadBytes(4); br.ReadBytes(4); br.ReadBytes(4);
                int fmtSize = br.ReadInt32();
                br.ReadInt16(); br.ReadInt16();
                int sampleRate = br.ReadInt32();
                int byteRate = br.ReadInt32();
                br.ReadBytes(fmtSize - 12);
                br.ReadBytes(4);
                int dataSize = br.ReadInt32();
                return (int)((double)dataSize / byteRate * 1000);
            }
            catch { return 5000; }
        }

        private void AnimateLogoText()
        {
            string logo = "C  Y  B  E  R   B  O  T   AI";
            LogoText.Inlines.Clear();
            for (int i = 0; i < logo.Length; i++) { Color c = _palette[(_animFrame + i) % _palette.Length]; Run r = new Run(logo[i].ToString()) { Foreground = new SolidColorBrush(c) }; LogoText.Inlines.Add(r); }
            if (_isLoggedIn && !string.IsNullOrEmpty(_loggedInUsername)) { string mode = _loginMode == "REGISTER" ? "WELCOME" : "WELCOME BACK"; string greeting = $"{mode}  {_loggedInUsername.ToUpper()}"; ChatUsernameText.Inlines.Clear(); for (int i = 0; i < greeting.Length; i++) { Color c = _palette[(_animFrame + i * 2) % _palette.Length]; Run r = new Run(greeting[i].ToString()) { Foreground = new SolidColorBrush(c) }; ChatUsernameText.Inlines.Add(r); } }
        }

        private void ShowLoginPanel() { DarkOverlay.Visibility = Visibility.Visible; LoginPanel.Visibility = Visibility.Visible; ChatBlur.Radius = 18; }
        private void HideLoginPanel() { DarkOverlay.Visibility = Visibility.Collapsed; LoginPanel.Visibility = Visibility.Collapsed; ChatBlur.BeginAnimation(System.Windows.Media.Effects.BlurEffect.RadiusProperty, null); ChatBlur.Radius = 0; }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void UpdateUiMode()
        {
            ClearStatus(); ClearValidationErrors();
            if (_isLoginMode) { TxtHeader.Text = "LOGIN PAGE"; BtnSubmit.Content = "LOGIN"; TxtFooterPrompt.Text = "Don't have an account? "; BtnToggleMode.Content = "Register here"; UpdateTabStyles(true); }
            else { TxtHeader.Text = "SIGN UP PAGE"; BtnSubmit.Content = "SIGN UP"; TxtFooterPrompt.Text = "Already have an account? "; BtnToggleMode.Content = "Sign In"; UpdateTabStyles(false); }
        }

        private void UpdateTabStyles(bool isLogin)
        {
            if (isLogin) { BorderLoginTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#25FF1493")); BorderLoginTab.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAFF1493")); TxtSidebarLogin.Foreground = Brushes.White; BorderRegisterTab.Background = Brushes.Transparent; BorderRegisterTab.BorderBrush = Brushes.Transparent; TxtSidebarRegister.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A5375")); }
            else { BorderRegisterTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#25FF1493")); BorderRegisterTab.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAFF1493")); TxtSidebarRegister.Foreground = Brushes.White; BorderLoginTab.Background = Brushes.Transparent; BorderLoginTab.BorderBrush = Brushes.Transparent; TxtSidebarLogin.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A5375")); }
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            ClearStatus();
            string username = TxtUsername.Text.Trim();
            string password = _isPasswordVisible ? TxtPasswordUnmasked.Text : TxtPassword.Password;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) { ShowStatus("Please fill in all fields.", false); return; }
            if (_isLoginMode) HandleLogin(username, password);
            else HandleRegistration(username, password);
        }

        private void HandleLogin(string username, string password)
        {
            if (!_accounts.ContainsKey(username)) { ShowStatus("Account does not exist!", false); return; }
            if (_accounts[username] != password) { ShowStatus("Incorrect password.", false); TxtClearPasswords(); return; }
            _loggedInUsername = username; _loginMode = "LOGIN"; _isLoggedIn = true; ShowChatAfterLogin();
            _activityLog.Log("User Logged In", $"User: {username}");
        }

        private void HandleRegistration(string username, string password) { if (ValidateUsername(username) && ValidatePassword(password)) { _accounts[username] = password; _loginMode = "REGISTER"; _isLoginMode = true; UpdateUiMode(); ShowStatus("Registration successful!", true); TxtClearPasswords(); _activityLog.Log("User Registered", $"User: {username}"); } }

        private void ShowChatAfterLogin()
        {
            HideLoginPanel();
            string initial = _loggedInUsername?.Length > 0 ? _loggedInUsername[0].ToString().ToUpper() : "";
            AvatarInitial.Text = initial;
            AvatarDefaultIcon.Visibility = Visibility.Collapsed;
            AvatarInitial.Visibility = Visibility.Visible;
            _conversationContext.UserDisplayName = _loggedInUsername ?? "User";
            _conversationContext.Memory.UserName = _loggedInUsername ?? string.Empty;
            UpdateFavoritesSidebar();
            InputBox.Focus();
        }

        // ================= FIXED: SHOW LOGOUT POPUP WITH REFRESH =================
        private void ShowLogoutPopup()
        {
            if (_isLogoutPopupVisible && _logoutPopup != null) return;
            HideLogoutPopup();
            var avatarButton = BtnAvatar as FrameworkElement;
            if (avatarButton == null) return;
            var popupPosition = avatarButton.TransformToAncestor(this).Transform(new Point(0, avatarButton.ActualHeight + 5));
            _logoutPopup = new Border { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)), CornerRadius = new CornerRadius(10), BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)), BorderThickness = new Thickness(1.5), Width = 200, Margin = new Thickness(popupPosition.X, popupPosition.Y, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var popupStack = new StackPanel();
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var logoutText = new TextBlock { Text = "🔄 SWITCH ACCOUNT", FontSize = 12, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), Margin = new Thickness(15, 12, 0, 8), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(logoutText, 0);
            var closeButton = new Border { Background = Brushes.Transparent, Padding = new Thickness(12, 8, 15, 8), Margin = new Thickness(0, 5, 5, 5), Cursor = Cursors.Hand, CornerRadius = new CornerRadius(5) };
            var closeX = new TextBlock { Text = "✕", FontSize = 16, FontFamily = new FontFamily("Segoe UI"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            closeButton.Child = closeX;
            closeButton.MouseEnter += (s, e) => { closeButton.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x55)); closeX.Foreground = Brushes.White; };
            closeButton.MouseLeave += (s, e) => { closeButton.Background = Brushes.Transparent; closeX.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)); };
            closeButton.MouseLeftButtonDown += (s, e) => HideLogoutPopup();
            Grid.SetColumn(closeButton, 1);
            headerGrid.Children.Add(logoutText);
            headerGrid.Children.Add(closeButton);
            popupStack.Children.Add(headerGrid);

            // Separator after header
            var separator = new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)), Margin = new Thickness(10, 0, 10, 5), Opacity = 0.5 };
            popupStack.Children.Add(separator);

            // TAKE QUIZ Button
            var quizOption = new Border { Background = Brushes.Transparent, Padding = new Thickness(15, 12, 15, 12), Margin = new Thickness(0, 0, 0, 5), Cursor = Cursors.Hand, CornerRadius = new CornerRadius(8) };
            quizOption.MouseEnter += (s, e) => ((Border)s).Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x55));
            quizOption.MouseLeave += (s, e) => ((Border)s).Background = Brushes.Transparent;
            quizOption.MouseLeftButtonDown += (s, e) => { HideLogoutPopup(); StartQuiz(); };
            var quizStack = new StackPanel { Orientation = Orientation.Horizontal };
            quizStack.Children.Add(new TextBlock { Text = "🎮 ", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            quizStack.Children.Add(new TextBlock { Text = "TAKE QUIZ", FontSize = 13, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            quizOption.Child = quizStack;
            popupStack.Children.Add(quizOption);

            // ================= FIXED: VIEW & MANAGE TASKS Button =================
            var tasksOption = new Border { Background = Brushes.Transparent, Padding = new Thickness(15, 12, 15, 12), Margin = new Thickness(0, 0, 0, 5), Cursor = Cursors.Hand, CornerRadius = new CornerRadius(8) };
            tasksOption.MouseEnter += (s, e) => ((Border)s).Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x55));
            tasksOption.MouseLeave += (s, e) => ((Border)s).Background = Brushes.Transparent;
            tasksOption.MouseLeftButtonDown += (s, e) => { HideLogoutPopup(); RefreshTaskSummary(); };  // FIXED: Use RefreshTaskSummary
            var tasksStack = new StackPanel { Orientation = Orientation.Horizontal };
            tasksStack.Children.Add(new TextBlock { Text = "📋 ", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            tasksStack.Children.Add(new TextBlock { Text = "VIEW & MANAGE TASKS", FontSize = 13, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            tasksOption.Child = tasksStack;
            popupStack.Children.Add(tasksOption);

            // SHOW ACTIVITY LOG Button
            var activityLogOption = new Border { Background = Brushes.Transparent, Padding = new Thickness(15, 12, 15, 12), Margin = new Thickness(0, 0, 0, 5), Cursor = Cursors.Hand, CornerRadius = new CornerRadius(8) };
            activityLogOption.MouseEnter += (s, e) => ((Border)s).Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x55));
            activityLogOption.MouseLeave += (s, e) => ((Border)s).Background = Brushes.Transparent;
            activityLogOption.MouseLeftButtonDown += (s, e) => { HideLogoutPopup(); ShowActivityLog(); };
            var activityLogStack = new StackPanel { Orientation = Orientation.Horizontal };
            activityLogStack.Children.Add(new TextBlock { Text = "📜 ", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            activityLogStack.Children.Add(new TextBlock { Text = "SHOW ACTIVITY LOG", FontSize = 13, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            activityLogOption.Child = activityLogStack;
            popupStack.Children.Add(activityLogOption);

            // LOG OUT Button
            var logoutSeparator = new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)), Margin = new Thickness(10, 5, 10, 5), Opacity = 0.3 };
            popupStack.Children.Add(logoutSeparator);

            var switchAccountOption = new Border { Background = Brushes.Transparent, Padding = new Thickness(15, 12, 15, 12), Margin = new Thickness(0, 0, 0, 5), Cursor = Cursors.Hand, CornerRadius = new CornerRadius(8) };
            switchAccountOption.MouseEnter += (s, e) => ((Border)s).Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x55));
            switchAccountOption.MouseLeave += (s, e) => ((Border)s).Background = Brushes.Transparent;
            switchAccountOption.MouseLeftButtonDown += (s, e) => SwitchAccount();
            var switchAccountStack = new StackPanel { Orientation = Orientation.Horizontal };
            switchAccountStack.Children.Add(new TextBlock { Text = "🔓 ", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            switchAccountStack.Children.Add(new TextBlock { Text = "LOG OUT", FontSize = 13, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)), VerticalAlignment = VerticalAlignment.Center });
            switchAccountOption.Child = switchAccountStack;
            popupStack.Children.Add(switchAccountOption);

            _logoutPopup.Child = popupStack;
            var mainGrid = this.Content as Grid;
            if (mainGrid != null) { mainGrid.Children.Add(_logoutPopup); Panel.SetZIndex(_logoutPopup, 1000); }
            _isLogoutPopupVisible = true;
        }

        // ================= RECYCLE BIN METHODS =================

        private void RecycleBinButton_Click(object sender, MouseButtonEventArgs e)
        {
            ShowRecycleBinInChat();
        }

        private void ShowRecycleBinInChat()
        {
            var items = _recycleBin.GetItems();

            var mainContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0F)),
                CornerRadius = new CornerRadius(15),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = "RecycleBinContainer"
            };

            var mainStack = new StackPanel();
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.Margin = new Thickness(0, 0, 0, 12);

            var headerTitle = new StackPanel { Orientation = Orientation.Horizontal };
            var countBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 0, 0)
            };
            countBadge.Child = new TextBlock
            {
                Text = items.Count.ToString(),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            headerTitle.Children.Add(countBadge);
            Grid.SetColumn(headerTitle, 0);
            headerGrid.Children.Add(headerTitle);

            var closeButton = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                Tag = "CloseRecycleBin"
            };
            var closeX = new TextBlock
            {
                Text = "✕",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2))
            };
            closeButton.Child = closeX;
            closeButton.MouseEnter += (s, e) =>
            {
                closeButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93));
                closeX.Foreground = Brushes.White;
            };
            closeButton.MouseLeave += (s, e) =>
            {
                closeButton.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                closeX.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2));
            };
            closeButton.MouseLeftButtonDown += (s, e) =>
            {
                RemoveRecycleBinFromChat();
            };

            Grid.SetColumn(closeButton, 1);
            headerGrid.Children.Add(closeButton);
            mainStack.Children.Add(headerGrid);

            if (items.Count == 0)
            {
                mainStack.Children.Add(new TextBlock
                {
                    Text = "♻️ Recycle bin is empty\n\nDeleted conversations, tasks, and reminders will appear here.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    FontFamily = new FontFamily("Consolas"),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
            }
            else
            {
                var actionButtons = new WrapPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var emptyBtn = CreateActionButton("🗑️ EMPTY BIN", Color.FromRgb(0xFF, 0x14, 0x93));
                emptyBtn.MouseLeftButtonDown += (s, e) =>
                {
                    int count = _recycleBin.EmptyBin();
                    AddBotMessage($"🗑️ Recycle bin emptied. {count} items permanently deleted.");
                    UpdateFavoritesSidebar();
                    RemoveRecycleBinFromChat();
                    ShowRecycleBinInChat();
                };
                actionButtons.Children.Add(emptyBtn);

                var restoreAllBtn = CreateActionButton("↩️ RESTORE ALL", Color.FromRgb(0x4C, 0xAF, 0x50));
                restoreAllBtn.MouseLeftButtonDown += (s, e) =>
                {
                    if (_recycleBin.Count == 0) return;
                    int count = _recycleBin.RestoreAll(RestoreItem);
                    AddBotMessage($"✅ All {count} items restored from recycle bin.");
                    UpdateFavoritesSidebar();
                    RemoveRecycleBinFromChat();
                    ShowRecycleBinInChat();
                };
                actionButtons.Children.Add(restoreAllBtn);

                var restoreSelectedBtn = CreateActionButton("☑️ RESTORE SELECTED", Color.FromRgb(0xFF, 0x6B, 0x35));
                restoreSelectedBtn.MouseLeftButtonDown += (s, e) =>
                {
                    var selected = _recycleBin.GetSelectedItems();
                    if (selected.Count == 0)
                    {
                        AddBotMessage("⚠️ No items selected. Click on items in the recycle bin to select them.");
                        return;
                    }
                    int count = _recycleBin.RestoreSelected(RestoreItem);
                    AddBotMessage($"✅ {count} selected items restored from recycle bin.");
                    UpdateFavoritesSidebar();
                    RemoveRecycleBinFromChat();
                    ShowRecycleBinInChat();
                };
                actionButtons.Children.Add(restoreSelectedBtn);

                var deleteSelectedBtn = CreateActionButton("🗑️ DELETE SELECTED", Color.FromRgb(0xCC, 0x33, 0x33));
                deleteSelectedBtn.MouseLeftButtonDown += (s, e) =>
                {
                    var selected = _recycleBin.GetSelectedItems();
                    if (selected.Count == 0)
                    {
                        AddBotMessage("⚠️ No items selected. Click on items in the recycle bin to select them.");
                        return;
                    }
                    int count = _recycleBin.DeleteSelected();
                    AddBotMessage($"🗑️ {count} selected items permanently deleted.");
                    UpdateFavoritesSidebar();
                    RemoveRecycleBinFromChat();
                    ShowRecycleBinInChat();
                };
                actionButtons.Children.Add(deleteSelectedBtn);

                mainStack.Children.Add(actionButtons);

                mainStack.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                    Opacity = 0.3,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var itemsScrollViewer = new ScrollViewer
                {
                    MaxHeight = 400,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };
                var itemsPanel = new StackPanel();

                foreach (var item in items)
                {
                    var itemBorder = new Border
                    {
                        Background = item.IsSelected ?
                            new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x4E)) :
                            new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                        CornerRadius = new CornerRadius(8),
                        BorderBrush = new SolidColorBrush(item.IsSelected ?
                            Color.FromRgb(0xFF, 0x66, 0xB2) :
                            Color.FromRgb(0x33, 0x33, 0x44)),
                        BorderThickness = new Thickness(item.IsSelected ? 2 : 1),
                        Margin = new Thickness(0, 3, 0, 3),
                        Padding = new Thickness(10, 8, 10, 8),
                        Cursor = Cursors.Hand,
                        Tag = item.UniqueId
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var checkBox = new Border
                    {
                        Width = 18,
                        Height = 18,
                        CornerRadius = new CornerRadius(4),
                        BorderBrush = new SolidColorBrush(item.IsSelected ?
                            Color.FromRgb(0xFF, 0x66, 0xB2) :
                            Color.FromRgb(0x88, 0x88, 0xAA)),
                        BorderThickness = new Thickness(2),
                        Background = item.IsSelected ?
                            new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)) :
                            Brushes.Transparent,
                        Margin = new Thickness(0, 0, 10, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    if (item.IsSelected)
                    {
                        checkBox.Child = new TextBlock
                        {
                            Text = "✓",
                            FontSize = 12,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                    }
                    Grid.SetColumn(checkBox, 0);
                    grid.Children.Add(checkBox);

                    var contentStack = new StackPanel { Orientation = Orientation.Horizontal };
                    string icon = item.Type switch
                    {
                        "Conversation" => "💬",
                        "Task" => "📋",
                        "Reminder" => "⏰",
                        _ => "📌"
                    };

                    string displayContent = item.Content;
                    if (item.Type == "Conversation")
                    {
                        var parts = item.Content.Split(new[] { '|' }, StringSplitOptions.None);
                        if (parts.Length >= 4)
                        {
                            string topic = parts[0];
                            string messagePart = string.Join("|", parts.Skip(3));
                            var messages = messagePart.Split(new[] { "|||" }, StringSplitOptions.None);
                            displayContent = $"{topic} - {messages.Length} messages";
                        }
                    }

                    contentStack.Children.Add(new TextBlock
                    {
                        Text = $"{icon} ",
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    contentStack.Children.Add(new TextBlock
                    {
                        Text = displayContent,
                        FontSize = 12,
                        Foreground = Brushes.White,
                        FontFamily = new FontFamily("Consolas"),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    Grid.SetColumn(contentStack, 1);
                    grid.Children.Add(contentStack);

                    var timeText = new TextBlock
                    {
                        Text = item.DeletedAt.ToString("HH:mm"),
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        FontFamily = new FontFamily("Consolas"),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    Grid.SetColumn(timeText, 2);
                    grid.Children.Add(timeText);

                    itemBorder.Child = grid;

                    itemBorder.MouseLeftButtonDown += (s, e) =>
                    {
                        var border = s as Border;
                        if (border != null && border.Tag is string id)
                        {
                            _recycleBin.ToggleSelection(id);
                            RemoveRecycleBinFromChat();
                            ShowRecycleBinInChat();
                        }
                    };

                    itemsPanel.Children.Add(itemBorder);
                }

                var actionPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                var selectAllText = new TextBlock
                {
                    Text = "Select All",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 15, 0)
                };
                selectAllText.MouseLeftButtonDown += (s, e) =>
                {
                    _recycleBin.SelectAll();
                    RemoveRecycleBinFromChat();
                    ShowRecycleBinInChat();
                };
                actionPanel.Children.Add(selectAllText);

                var deselectAllText = new TextBlock
                {
                    Text = "Deselect All",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    Cursor = Cursors.Hand
                };
                deselectAllText.MouseLeftButtonDown += (s, e) =>
                {
                    _recycleBin.DeselectAll();
                    RemoveRecycleBinFromChat();
                    ShowRecycleBinInChat();
                };
                actionPanel.Children.Add(deselectAllText);

                itemsPanel.Children.Add(actionPanel);
                itemsScrollViewer.Content = itemsPanel;
                mainStack.Children.Add(itemsScrollViewer);
            }

            mainContainer.Child = mainStack;
            mainContainer.Tag = "RecycleBinContainer";

            Dispatcher.Invoke(() =>
            {
                MessagesPanel.Children.Add(mainContainer);
                ScrollToBottom();
            });
        }

        private Border CreateActionButton(string text, Color bgColor)
        {
            var button = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            button.Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas")
            };

            button.MouseEnter += (s, e) =>
            {
                var b = s as Border;
                if (b != null)
                {
                    var color = (b.Background as SolidColorBrush)?.Color ?? bgColor;
                    b.Background = new SolidColorBrush(Color.FromRgb(
                        (byte)Math.Min(color.R + 30, 255),
                        (byte)Math.Min(color.G + 30, 255),
                        (byte)Math.Min(color.B + 30, 255)
                    ));
                }
            };
            button.MouseLeave += (s, e) =>
            {
                var b = s as Border;
                if (b != null)
                {
                    b.Background = new SolidColorBrush(bgColor);
                }
            };

            return button;
        }

        private void RemoveRecycleBinFromChat()
        {
            Dispatcher.Invoke(() =>
            {
                for (int i = MessagesPanel.Children.Count - 1; i >= 0; i--)
                {
                    if (MessagesPanel.Children[i] is Border border && border.Tag as string == "RecycleBinContainer")
                    {
                        MessagesPanel.Children.RemoveAt(i);
                        break;
                    }
                }
            });
        }

        private void RestoreItem(string type, string content)
        {
            switch (type.ToLowerInvariant())
            {
                case "conversation":
                    var parts = content.Split(new[] { '|' }, StringSplitOptions.None);
                    if (parts.Length >= 7)
                    {
                        string topic = parts[0].Trim();
                        int tipCount = int.TryParse(parts[1], out int t) ? t : 0;
                        int totalTips = int.TryParse(parts[2], out int tt) ? tt : 0;
                        bool exampleUsed = bool.TryParse(parts[3], out bool eu) && eu;
                        bool moreUsed = bool.TryParse(parts[4], out bool mu) && mu;
                        bool favorite = bool.TryParse(parts[5], out bool fav) && fav;

                        string messagePart = string.Join("|", parts.Skip(6));
                        var messages = messagePart.Split(new[] { "|||" }, StringSplitOptions.None);

                        foreach (var msg in messages)
                        {
                            var msgParts = msg.Split(new[] { '|' }, StringSplitOptions.None);
                            if (msgParts.Length == 2)
                            {
                                string sender = msgParts[0];
                                string message = msgParts[1];

                                _conversationContext.Messages.Add((sender, message));

                                if (sender == "BotBuddy")
                                {
                                    AddBotMessage(message);
                                }
                                else
                                {
                                    AddUserMessage(message);
                                }
                            }
                        }

                        for (int i = 0; i < tipCount && i < totalTips; i++)
                        {
                            _conversationContext.Memory.MarkTipUsed(topic, i);
                        }

                        if (tipCount > 0)
                        {
                            _conversationContext.Memory.MarkTipUsed(topic);
                        }

                        if (exampleUsed)
                        {
                            _conversationContext.Memory.MarkExampleUsed(topic);
                        }

                        if (moreUsed)
                        {
                            _conversationContext.Memory.MarkMoreUsed(topic);
                        }

                        if (favorite && !_conversationContext.Memory.FavoriteTopics.Contains(topic))
                        {
                            _conversationContext.Memory.FavoriteTopics.Add(topic);
                        }

                        _conversationContext.Memory.CoveredTopics.Add(topic);
                        _conversationContext.Memory.MarkTopicCovered(topic);

                        AddBotMessage($"🔄 Restored conversation about {topic} ({messages.Length} messages)");
                        UpdateFavoritesSidebar();
                    }
                    break;

                case "task":
                    _taskManager.AddTask(content, null);
                    AddBotMessage($"📋 Restored task: {content}");
                    break;

                case "reminder":
                    _taskManager.AddReminder(content, null);
                    AddBotMessage($"⏰ Restored reminder: {content}");
                    break;
            }
        }

        // ================= ACTIVITY LOG METHODS =================

        private void ShowActivityLog()
        {
            int displayCount = _showFullLog ? _activityLog.EntryCount : 5;
            var entries = _activityLog.GetEntries(displayCount);

            if (entries.Count == 0)
            {
                AddBotMessage("📜 ACTIVITY LOG\n\nNo activity logged yet. Start chatting to see your activity here!");
                return;
            }

            if (_activityLogContainer != null && MessagesPanel.Children.Contains(_activityLogContainer))
            {
                UpdateActivityLogContainer(_activityLogContainer, entries);
                return;
            }

            _activityLogContainer = CreateActivityLogContainer(entries);

            Dispatcher.Invoke(() =>
            {
                MessagesPanel.Children.Add(_activityLogContainer);
                ScrollToBottom();
            });
        }

        private Border CreateActivityLogContainer(List<ActivityLog.ActivityEntry> entries)
        {
            var mainContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0F)),
                CornerRadius = new CornerRadius(15),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = "ActivityLogContainer"
            };

            var mainStack = new StackPanel();
            mainStack.Tag = "ActivityLogStack";

            var headerText = new TextBlock
            {
                Text = "📜 ACTIVITY LOG",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(headerText);

            mainStack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                Opacity = 0.3,
                Margin = new Thickness(0, 0, 0, 10)
            });

            int count = 0;
            foreach (var entry in entries)
            {
                count++;
                string timeStr = entry.Timestamp.ToString("HH:mm:ss");
                string detailsStr = string.IsNullOrEmpty(entry.Details) ? "" : $" ─ {entry.Details}";

                var entryBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 3, 0, 3)
                };
                entryBorder.Child = new TextBlock
                {
                    Text = $"{count}. [{timeStr}] {entry.Action}{detailsStr}",
                    FontSize = 12,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Consolas"),
                    TextWrapping = TextWrapping.Wrap
                };
                mainStack.Children.Add(entryBorder);
            }

            mainStack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                Opacity = 0.3,
                Margin = new Thickness(0, 10, 0, 10)
            });

            var footerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0),
                Tag = "FooterStack"
            };

            AddFooterButtons(footerStack);
            mainStack.Children.Add(footerStack);
            mainContainer.Child = mainStack;

            return mainContainer;
        }

        private void UpdateActivityLogContainer(Border container, List<ActivityLog.ActivityEntry> entries)
        {
            var mainStack = container.Child as StackPanel;
            if (mainStack == null) return;

            var itemsToRemove = new List<UIElement>();

            for (int i = 0; i < mainStack.Children.Count; i++)
            {
                var child = mainStack.Children[i];

                if (i > 0 && child is Border border)
                {
                    if (border.Child is TextBlock)
                    {
                        if (!(border.Child is TextBlock textBlock &&
                              (textBlock.Text == "📜 ACTIVITY LOG" ||
                               textBlock.Text == "📄 SEE LESS (Show 5)" ||
                               textBlock.Text.StartsWith("📄 SEE MORE"))))
                        {
                            itemsToRemove.Add(child);
                        }
                    }
                }
                else if (i > 0 && child is TextBlock textBlock)
                {
                    if (textBlock.Text != "📜 ACTIVITY LOG" &&
                        !textBlock.Text.Contains("SEE MORE") &&
                        !textBlock.Text.Contains("SEE LESS") &&
                        !textBlock.Text.Contains("total entries"))
                    {
                        itemsToRemove.Add(child);
                    }
                }
            }

            foreach (var item in itemsToRemove)
            {
                mainStack.Children.Remove(item);
            }

            int insertIndex = 2;

            int count = 0;
            foreach (var entry in entries)
            {
                count++;
                string timeStr = entry.Timestamp.ToString("HH:mm:ss");
                string detailsStr = string.IsNullOrEmpty(entry.Details) ? "" : $" ─ {entry.Details}";

                var entryBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 3, 0, 3)
                };
                entryBorder.Child = new TextBlock
                {
                    Text = $"{count}. [{timeStr}] {entry.Action}{detailsStr}",
                    FontSize = 12,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Consolas"),
                    TextWrapping = TextWrapping.Wrap
                };

                mainStack.Children.Insert(insertIndex, entryBorder);
                insertIndex++;
            }

            for (int i = 0; i < mainStack.Children.Count; i++)
            {
                var child = mainStack.Children[i];
                if (child is StackPanel footerStack && footerStack.Tag as string == "FooterStack")
                {
                    footerStack.Children.Clear();
                    AddFooterButtons(footerStack);
                    break;
                }
            }
        }

        private void AddFooterButtons(StackPanel footerStack)
        {
            if (_showFullLog)
            {
                var seeLessButton = CreateLogButton("📄 SEE LESS (Show 5)", Color.FromRgb(0xFF, 0x14, 0x93));
                seeLessButton.MouseLeftButtonDown += (s, e) =>
                {
                    _showFullLog = false;
                    ShowActivityLog();
                };
                footerStack.Children.Add(seeLessButton);

                var countText = new TextBlock
                {
                    Text = $"  {_activityLog.EntryCount} total entries  ",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    FontFamily = new FontFamily("Consolas"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                footerStack.Children.Add(countText);
            }
            else
            {
                if (_activityLog.EntryCount > 5)
                {
                    var seeMoreButton = CreateLogButton($"📄 SEE MORE (Show all {_activityLog.EntryCount})", Color.FromRgb(0x4C, 0xAF, 0x50));
                    seeMoreButton.MouseLeftButtonDown += (s, e) =>
                    {
                        _showFullLog = true;
                        ShowActivityLog();
                    };
                    footerStack.Children.Add(seeMoreButton);
                }
                else
                {
                    var countText = new TextBlock
                    {
                        Text = $"  {_activityLog.EntryCount} total entries  ",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        FontFamily = new FontFamily("Consolas"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    footerStack.Children.Add(countText);
                }
            }
        }

        private Border CreateLogButton(string text, Color bgColor)
        {
            var button = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            button.Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas")
            };

            button.MouseEnter += (s, e) =>
            {
                var b = s as Border;
                if (b != null)
                {
                    var color = (b.Background as SolidColorBrush)?.Color ?? bgColor;
                    b.Background = new SolidColorBrush(Color.FromRgb(
                        (byte)Math.Min(color.R + 30, 255),
                        (byte)Math.Min(color.G + 30, 255),
                        (byte)Math.Min(color.B + 30, 255)
                    ));
                }
            };
            button.MouseLeave += (s, e) =>
            {
                var b = s as Border;
                if (b != null)
                {
                    b.Background = new SolidColorBrush(bgColor);
                }
            };

            return button;
        }

        // ================= FIXED: REFRESH TASK SUMMARY =================
        private void RefreshTaskSummary()
        {
            // Find and remove the existing task summary
            Dispatcher.Invoke(() =>
            {
                for (int i = MessagesPanel.Children.Count - 1; i >= 0; i--)
                {
                    if (MessagesPanel.Children[i] is Border border &&
                        border.Tag as string == "TaskSummaryContainer")
                    {
                        MessagesPanel.Children.RemoveAt(i);
                        break;
                    }
                }
            });

            // Show the updated summary
            ShowTaskSummary();
        }

        // ================= FIXED: SHOW TASK SUMMARY WITH TAG =================
        private void ShowTaskSummary()
        {
            string summary = _taskManager.GetSummary();

            var mainContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0F)),
                CornerRadius = new CornerRadius(15),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = "TaskSummaryContainer"  // ADD THIS TAG
            };

            var mainStack = new StackPanel();

            // Header with refresh button
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.Margin = new Thickness(0, 0, 0, 10);

            var titleText = new TextBlock
            {
                Text = "📋 TASK MANAGEMENT",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            var refreshButton = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 6, 15, 6),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            refreshButton.Child = new TextBlock
            {
                Text = "🔄 REFRESH",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2))
            };
            refreshButton.MouseLeftButtonDown += (s, e) => RefreshTaskSummary();
            Grid.SetColumn(refreshButton, 1);
            headerGrid.Children.Add(refreshButton);
            mainStack.Children.Add(headerGrid);

            mainStack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                Opacity = 0.3,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Parse and display the summary with proper formatting
            var lines = summary.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    mainStack.Children.Add(new TextBlock { Height = 8 });
                    continue;
                }

                var textBlock = new TextBlock
                {
                    Text = line,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Consolas"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                // Color coding for different sections
                if (line.StartsWith("⏰ REMINDERS:"))
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2));
                else if (line.StartsWith("📌 TASKS:"))
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2));
                else if (line.StartsWith("✅ COMPLETED:"))
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                else if (line.StartsWith("  •") || line.StartsWith("  ."))
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                else if (line.Contains("NO REMINDERS") || line.Contains("NO TASKS") || line.Contains("NO COMPLETED"))
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

                mainStack.Children.Add(textBlock);
            }

            mainContainer.Child = mainStack;
            Dispatcher.Invoke(() =>
            {
                MessagesPanel.Children.Add(mainContainer);
                ScrollToBottom();
            });
        }

        private void HideLogoutPopup() { if (_logoutPopup != null && this.Content is Grid mainGrid) { mainGrid.Children.Remove(_logoutPopup); _logoutPopup = null; } _isLogoutPopupVisible = false; }
        private void ToggleLogoutPopup() { if (_isLogoutPopupVisible) HideLogoutPopup(); else ShowLogoutPopup(); }
        private void SwitchAccount() { HideLogoutPopup(); ShowLoginPanel(); TxtUsername.Text = ""; TxtClearPasswords(); _isLoginMode = true; UpdateUiMode(); }
        private void BtnAvatar_Click(object sender, RoutedEventArgs e) { ToggleLogoutPopup(); }
        private void BtnToggleMode_Click(object sender, RoutedEventArgs e) { _isLoginMode = !_isLoginMode; UpdateUiMode(); }
        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            if (_isPasswordVisible) { TxtPasswordUnmasked.Text = TxtPassword.Password; TxtPassword.Visibility = Visibility.Collapsed; TxtPasswordUnmasked.Visibility = Visibility.Visible; }
            else { TxtPassword.Password = TxtPasswordUnmasked.Text; TxtPassword.Visibility = Visibility.Visible; TxtPasswordUnmasked.Visibility = Visibility.Collapsed; }
        }

        private bool ValidateUsername(string name)
        {
            if (_isLoginMode) return true;
            ErrorUsername.Text = "";
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2) { ErrorUsername.Text = "Min 2 characters"; return false; }
            if (name.Any(ch => !char.IsLetter(ch))) { ErrorUsername.Text = "Only letters allowed"; return false; }
            if (_accounts.ContainsKey(name)) { ErrorUsername.Text = "Username taken"; return false; }
            return true;
        }

        private bool ValidatePassword(string p)
        {
            if (_isLoginMode) return true;
            ErrorPassword.Text = "";
            if (string.IsNullOrEmpty(p)) return false;
            if (p.Length > 4) { ErrorPassword.Text = "Max 4 characters"; return false; }
            var validator = new PasswordValidator(p);
            string? error = validator.Validate();
            if (error != null) { ErrorPassword.Text = error; return false; }
            return true;
        }

        private void ShowStatus(string msg, bool success) { TxtStatus.Text = msg; StatusBorder.Visibility = Visibility.Visible; }
        private void ClearStatus() => StatusBorder.Visibility = Visibility.Collapsed;
        private void ClearValidationErrors() { ErrorUsername.Text = ""; ErrorPassword.Text = ""; }
        private void TxtClearPasswords() { TxtPassword.Clear(); TxtPasswordUnmasked.Clear(); }
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e) => ValidatePassword(TxtPassword.Password);
        private void TxtPasswordUnmasked_TextChanged(object sender, TextChangedEventArgs e) => ValidatePassword(TxtPasswordUnmasked.Text);
        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e) => ValidateUsername(TxtUsername.Text);
        private void InputBox_TextChanged(object sender, TextChangedEventArgs e) { PlaceholderText.Visibility = InputBox.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible; }
        private void InputBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { e.Handled = true; SendMessage(); } }
        private void BtnSend_Click(object sender, RoutedEventArgs e) => SendMessage();

        // ================= QUIZ METHODS =================

        private bool _answerSubmitted = false;
        private Border _currentFeedbackBorder = null!;
        private TextBlock _currentFeedbackText = null!;
        private Border _currentNextButton = null!;
        private TextBlock _currentTimerText = null!;
        private List<Border> _optionBorders = new List<Border>();

        private void StartQuiz()
        {
            _isQuizMode = true;
            _quizStartTime = DateTime.Now;
            _quizTimer.Start();

            InputBox.IsEnabled = false;
            BtnSend.IsEnabled = false;
            PlaceholderText.Text = "🎮 QUIZ MODE ACTIVE - Answer using the buttons below 🎮";

            Border quizContainer = null;
            foreach (var child in MessagesPanel.Children)
            {
                if (child is Border border && border.Tag as string == "QuizContainer")
                {
                    quizContainer = border;
                    break;
                }
            }

            if (quizContainer != null)
            {
                quizContainer.Visibility = Visibility.Visible;
                _quizOverlay = quizContainer;
                MessagesPanel.Children.Remove(quizContainer);
                MessagesPanel.Children.Add(quizContainer);
            }

            _feedbackPanel.Visibility = Visibility.Collapsed;
            _quizQuestionText.Text = "CHOOSE YOUR QUIZ MODE";
            _quizProgressText.Text = "";
            _quizOptionsPanel.Children.Clear();

            double buttonWidth = 160;

            var topRowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var quickButton = CreateDifficultyButtonContent("⚡ QUICK", "5 questions • 2-3 min", "quick", buttonWidth);
            var balancedButton = CreateDifficultyButtonContent("📚 BALANCED", "15 questions • 5-8 min", "balanced", buttonWidth);

            topRowPanel.Children.Add(quickButton);
            topRowPanel.Children.Add(balancedButton);

            var bottomRowPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var deepButton = CreateDifficultyButtonContent("🎓 DEEP", "30 questions • 10-15 min", "deep", buttonWidth);
            bottomRowPanel.Children.Add(deepButton);

            _quizOptionsPanel.Children.Add(topRowPanel);
            _quizOptionsPanel.Children.Add(bottomRowPanel);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChatScrollViewer.UpdateLayout();
                ChatScrollViewer.ScrollToEnd();
            }), DispatcherPriority.Background);
        }

        private Border CreateDifficultyButtonContent(string title, string description, string difficulty, double width)
        {
            var button = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(8, 6, 8, 6),
                Padding = new Thickness(12, 10, 12, 10),
                Cursor = Cursors.Hand,
                Width = width
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });

            button.Child = stack;

            button.MouseEnter += (s, e) =>
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x2A, 0x4E));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0xCC));
            };
            button.MouseLeave += (s, e) =>
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2));
            };
            button.MouseLeftButtonDown += (s, e) => StartGameWithDifficulty(difficulty);

            return button;
        }

        private void StartGameWithDifficulty(string difficulty)
        {
            _quiz.StartQuiz(difficulty);
            _activityLog.Log("Quiz Started", $"Mode: {difficulty}");
            UpdateQuizUI();
        }

        private void UpdateQuizUI()
        {
            if (!_quiz.IsQuizActive)
            {
                CompleteQuiz();
                return;
            }

            _quizOverlay.Visibility = Visibility.Visible;
            _feedbackPanel.Visibility = Visibility.Collapsed;
            _quizQuestionText.Text = "";
            _optionBorders.Clear();
            _answerSubmitted = false;

            var q = _quiz.CurrentQuestion;
            if (q == null) return;

            int total = _quiz.TotalQuestions;
            int current = _quiz.CurrentQuestionNumber;

            _quizOptionsPanel.Children.Clear();

            var mainContainer = new Grid();
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.Margin = new Thickness(0, 0, 0, 5);

            var counterText = new TextBlock
            {
                Text = $"{current} of {total}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(counterText, 0);
            headerGrid.Children.Add(counterText);

            _currentTimerText = new TextBlock
            {
                Text = GetElapsedTime(),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_currentTimerText, 1);
            headerGrid.Children.Add(_currentTimerText);

            Grid.SetRow(headerGrid, 0);
            mainContainer.Children.Add(headerGrid);

            var progressBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                CornerRadius = new CornerRadius(4),
                Height = 6,
                Margin = new Thickness(0, 0, 0, 12)
            };
            double progress = total > 0 ? (double)(current) / total : 0;
            if (progress > 1) progress = 1;

            var progressFill = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                CornerRadius = new CornerRadius(4),
                Width = progress * 590,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            progressBorder.Child = progressFill;
            Grid.SetRow(progressBorder, 1);
            mainContainer.Children.Add(progressBorder);

            var questionNumText = new TextBlock
            {
                Text = $"Question {current:D2}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(questionNumText, 2);
            mainContainer.Children.Add(questionNumText);

            var questionText = new TextBlock
            {
                Text = q.Question,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(questionText, 3);
            mainContainer.Children.Add(questionText);

            var optionsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < q.Options.Length; i++)
            {
                var optionBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x66)),
                    BorderThickness = new Thickness(1.5),
                    Margin = new Thickness(0, 4, 0, 4),
                    Padding = new Thickness(12, 10, 12, 10),
                    Cursor = Cursors.Hand,
                    Tag = i,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                _optionBorders.Add(optionBorder);

                var optionGrid = new Grid();
                optionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                optionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                optionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var radioCircle = new Border
                {
                    Width = 22,
                    Height = 22,
                    CornerRadius = new CornerRadius(11),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)),
                    BorderThickness = new Thickness(2),
                    Background = Brushes.Transparent,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                var radioInner = new Border
                {
                    Width = 10,
                    Height = 10,
                    CornerRadius = new CornerRadius(5),
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                radioCircle.Child = radioInner;
                Grid.SetColumn(radioCircle, 0);
                optionGrid.Children.Add(radioCircle);

                var optionText = new TextBlock
                {
                    Text = $"{letters[i]}. {q.Options[i]}",
                    FontSize = 15,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Segoe UI"),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(optionText, 1);
                optionGrid.Children.Add(optionText);

                var resultIndicator = new TextBlock
                {
                    Text = "",
                    FontSize = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10, 0, 0, 0),
                    Visibility = Visibility.Collapsed
                };
                Grid.SetColumn(resultIndicator, 2);
                optionGrid.Children.Add(resultIndicator);

                optionBorder.Child = optionGrid;
                optionBorder.Tag = new Tuple<int, Border, Border, TextBlock>(i, radioCircle, radioInner, resultIndicator);

                optionBorder.MouseEnter += (s, e) =>
                {
                    var border = s as Border;
                    if (border != null && !_answerSubmitted)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x2A, 0x4E));
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2));
                    }
                };
                optionBorder.MouseLeave += (s, e) =>
                {
                    var border = s as Border;
                    if (border != null && !_answerSubmitted)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E));
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x66));
                    }
                };
                optionBorder.MouseLeftButtonDown += (s, e) =>
                {
                    if (_answerSubmitted) return;
                    var border = s as Border;
                    if (border != null && border.Tag is Tuple<int, Border, Border, TextBlock> data)
                    {
                        SubmitAnswer(data.Item1);
                    }
                };

                optionsPanel.Children.Add(optionBorder);
            }

            Grid.SetRow(optionsPanel, 4);
            mainContainer.Children.Add(optionsPanel);

            _currentFeedbackBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 5, 0, 10),
                Visibility = Visibility.Collapsed
            };
            _currentFeedbackText = new TextBlock
            {
                FontSize = 14,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap
            };
            _currentFeedbackBorder.Child = _currentFeedbackText;
            Grid.SetRow(_currentFeedbackBorder, 5);
            mainContainer.Children.Add(_currentFeedbackBorder);

            _currentNextButton = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(30, 10, 30, 10),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            var nextTextBlock = new TextBlock
            {
                Text = "Next",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            _currentNextButton.Child = nextTextBlock;
            _currentNextButton.MouseEnter += (s, e) => _currentNextButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x33, 0x85));
            _currentNextButton.MouseLeave += (s, e) => _currentNextButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93));
            _currentNextButton.MouseLeftButtonDown += (s, e) =>
            {
                _answerSubmitted = false;
                UpdateQuizUI();
            };
            Grid.SetRow(_currentNextButton, 6);
            mainContainer.Children.Add(_currentNextButton);

            _quizOptionsPanel.Children.Add(mainContainer);
        }

        private string GetElapsedTime()
        {
            var elapsed = DateTime.Now - _quizStartTime;
            return $"⏱️ {elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        private void SubmitAnswer(int answerIndex)
        {
            if (_answerSubmitted) return;
            _answerSubmitted = true;

            var q = _quiz.CurrentQuestion;
            if (q == null) return;

            var (isCorrect, response) = _quiz.SubmitAnswer(answerIndex);

            int correctIndex = q.CorrectAnswerIndex;

            foreach (var optionBorder in _optionBorders)
            {
                if (optionBorder.Tag is Tuple<int, Border, Border, TextBlock> data)
                {
                    int idx = data.Item1;
                    var radioCircle = data.Item2;
                    var radioInner = data.Item3;
                    var resultIndicator = data.Item4;

                    if (idx == correctIndex)
                    {
                        optionBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                        optionBorder.BorderThickness = new Thickness(2.5);
                        radioCircle.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                        radioInner.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                        resultIndicator.Text = "✅";
                        resultIndicator.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                        resultIndicator.Visibility = Visibility.Visible;
                        optionBorder.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x1A));
                        optionBorder.Opacity = 1.0;
                    }
                    else if (idx == answerIndex && !isCorrect)
                    {
                        optionBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
                        optionBorder.BorderThickness = new Thickness(2.5);
                        radioCircle.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
                        radioInner.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
                        resultIndicator.Text = "❌";
                        resultIndicator.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
                        resultIndicator.Visibility = Visibility.Visible;
                        optionBorder.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x1A, 0x00));
                        optionBorder.Opacity = 1.0;
                    }
                    else
                    {
                        optionBorder.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));
                        optionBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44));
                        optionBorder.Opacity = 0.5;
                        radioCircle.BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x55));
                        radioInner.Background = Brushes.Transparent;
                        resultIndicator.Visibility = Visibility.Collapsed;
                    }
                }
            }

            if (_currentFeedbackBorder != null)
            {
                string selectedLetter = ((char)('A' + answerIndex)).ToString();
                string correctLetter = ((char)('A' + correctIndex)).ToString();

                string explanation = "";

                if (isCorrect)
                {
                    explanation = $"📖 {q.Explanation}";
                }
                else
                {
                    explanation = $"{selectedLetter} is incorrect because:\n\n{q.Explanation}";
                }

                _currentFeedbackText.Text = explanation;
                _currentFeedbackBorder.Visibility = Visibility.Visible;
                _currentFeedbackBorder.BorderBrush = new SolidColorBrush(isCorrect ? Color.FromRgb(0x4C, 0xAF, 0x50) : Color.FromRgb(0xFF, 0x8C, 0x00));
                _currentFeedbackBorder.BorderThickness = new Thickness(1);
            }

            if (_currentNextButton != null)
            {
                if (!_quiz.IsQuizActive)
                {
                    var tb = _currentNextButton.Child as TextBlock;
                    if (tb != null) tb.Text = "📊 VIEW FINAL SCORE";
                    _currentNextButton.MouseLeftButtonDown -= (s, e) => { _answerSubmitted = false; UpdateQuizUI(); };
                    _currentNextButton.MouseLeftButtonDown += (s, e) =>
                    {
                        ClearQuizForFinalScore();
                    };
                }
                else
                {
                    var tb = _currentNextButton.Child as TextBlock;
                    if (tb != null) tb.Text = "Next";
                    _currentNextButton.MouseLeftButtonDown -= (s, e) => { _answerSubmitted = false; UpdateQuizUI(); };
                    _currentNextButton.MouseLeftButtonDown += (s, e) =>
                    {
                        _answerSubmitted = false;
                        UpdateQuizUI();
                    };
                }
                _currentNextButton.Visibility = Visibility.Visible;
            }

            UpdateTimerDisplay();
        }

        private void ClearQuizForFinalScore()
        {
            _quizOverlay.Visibility = Visibility.Visible;

            _quizQuestionText.Text = "";
            _quizProgressText.Text = "";
            _feedbackPanel.Visibility = Visibility.Collapsed;
            _quizOptionsPanel.Children.Clear();
            _quizTimer.Stop();
            _answerSubmitted = false;

            CreateFinalScoreCard();
        }

        private void CreateFinalScoreCard()
        {
            _quizOverlay.Visibility = Visibility.Visible;

            var elapsed = DateTime.Now - _quizStartTime;
            string timeTaken = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

            _quizOptionsPanel.Children.Clear();
            _feedbackPanel.Visibility = Visibility.Collapsed;
            _quizQuestionText.Text = "";
            _quizProgressText.Text = "";

            int total = _quiz.TotalQuestions;
            int score = _quiz.CurrentScore;
            double percentage = total > 0 ? (double)score / total * 100 : 0;

            _activityLog.Log("Quiz Completed", $"{score}/{total} correct ({percentage:F0}%)");

            var cardBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(20, 18, 20, 18),
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 380,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var cardStack = new StackPanel();

            cardStack.Children.Add(new TextBlock
            {
                Text = "🏆",
                FontSize = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            cardStack.Children.Add(new TextBlock
            {
                Text = "QUIZ COMPLETED",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            cardStack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                Margin = new Thickness(0, 0, 0, 12),
                Opacity = 0.3
            });

            var scoreGrid = new Grid();
            scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            scoreGrid.Margin = new Thickness(0, 0, 0, 6);

            var scoreLabel = new TextBlock
            {
                Text = "SCORE:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(scoreLabel, 0);
            scoreGrid.Children.Add(scoreLabel);

            var scoreValue = new TextBlock
            {
                Text = $"{score}/{total}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(scoreValue, 1);
            scoreGrid.Children.Add(scoreValue);
            cardStack.Children.Add(scoreGrid);

            var percentageGrid = new Grid();
            percentageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            percentageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            percentageGrid.Margin = new Thickness(0, 0, 0, 6);

            var percentageLabel = new TextBlock
            {
                Text = "PERCENTAGE:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(percentageLabel, 0);
            percentageGrid.Children.Add(percentageLabel);

            var percentageValue = new TextBlock
            {
                Text = $"{percentage:F0}%",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(GetPercentageColor(percentage)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(percentageValue, 1);
            percentageGrid.Children.Add(percentageValue);
            cardStack.Children.Add(percentageGrid);

            var timeGrid = new Grid();
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            timeGrid.Margin = new Thickness(0, 0, 0, 8);

            var timeLabel = new TextBlock
            {
                Text = "COMPLETION TIME:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(timeLabel, 0);
            timeGrid.Children.Add(timeLabel);

            var timeValue = new TextBlock
            {
                Text = $"{timeTaken}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeValue, 1);
            timeGrid.Children.Add(timeValue);
            cardStack.Children.Add(timeGrid);

            string critique = GetScoreFeedback(score, total);
            var critiqueText = new TextBlock
            {
                Text = critique,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 12),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            cardStack.Children.Add(critiqueText);

            cardStack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                Margin = new Thickness(0, 0, 0, 12),
                Opacity = 0.2
            });

            var btnBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 8, 20, 8),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var btnText = new TextBlock
            {
                Text = "PLAY AGAIN",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            btnBorder.Child = btnText;

            btnBorder.MouseEnter += (s, e) => btnBorder.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x33, 0x85));
            btnBorder.MouseLeave += (s, e) => btnBorder.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93));
            btnBorder.MouseLeftButtonDown += (s, e) =>
            {
                _quiz.Reset();
                StartQuiz();
            };

            cardStack.Children.Add(btnBorder);
            cardBorder.Child = cardStack;

            _quizOptionsPanel.Children.Clear();
            _quizOptionsPanel.Children.Add(cardBorder);

            _quizQuestionText.Text = "";
            _quizProgressText.Text = "";
            _feedbackPanel.Visibility = Visibility.Collapsed;

            _quizOverlay.Visibility = Visibility.Visible;
        }

        private Color GetPercentageColor(double percentage)
        {
            if (percentage >= 90)
                return Color.FromRgb(0xFF, 0xD7, 0x00);
            else if (percentage >= 70)
                return Color.FromRgb(0x4C, 0xAF, 0x50);
            else if (percentage >= 50)
                return Color.FromRgb(0xFF, 0xA5, 0x00);
            else
                return Color.FromRgb(0xFF, 0x44, 0x44);
        }

        private string GetScoreFeedback(int score, int total)
        {
            double percentage = total > 0 ? (double)score / total * 100 : 0;

            if (percentage >= 90)
                return "EXCELLENT! YOU'RE A CYBERSECURITY EXPERT!";
            else if (percentage >= 70)
                return "GOOD JOB! YOU HAVE SOLID CYBERSECURITY KNOWLEDGE!";
            else if (percentage >= 50)
                return "GOOD EFFORT! KEEP LEARNING AND YOU'LL GET THERE!";
            else
                return "KEEP LEARNING! FAMILIARIZE YOURSELF AND TRY AGAIN!";
        }

        private void CompleteQuiz()
        {
            foreach (var child in MessagesPanel.Children)
            {
                if (child is Border border && border.Tag as string == "QuizContainer")
                {
                    border.Visibility = Visibility.Collapsed;
                    _quizOverlay = border;
                    break;
                }
            }

            _isQuizMode = false;
            _quizTimer.Stop();
            _answerSubmitted = false;
            _optionBorders.Clear();

            InputBox.IsEnabled = true;
            BtnSend.IsEnabled = true;
            PlaceholderText.Text = "Ask me about cybersecurity...";
            InputBox.Focus();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChatScrollViewer.UpdateLayout();
                ChatScrollViewer.ScrollToEnd();
            }), DispatcherPriority.Background);
        }

        private void QuitQuiz()
        {
            _quiz.Reset();
            _quizTimer.Stop();
            _answerSubmitted = false;
            _optionBorders.Clear();
            _activityLog.Log("Quiz Quit", $"Questions answered: {_quiz.CurrentQuestionNumber - 1}");

            foreach (var child in MessagesPanel.Children)
            {
                if (child is Border border && border.Tag as string == "QuizContainer")
                {
                    border.Visibility = Visibility.Collapsed;
                    break;
                }
            }

            CompleteQuiz();
        }

        // ================= TASK FLOW WITH CHAT BUTTONS =================

        private bool _waitingForTaskName = false;

        private void ShowTaskConfirmationButtons(string taskDescription, DateTime? dueDate = null)
        {
            _pendingTaskDescription = taskDescription;
            _pendingTaskDate = dueDate;
            _currentTaskDescription = taskDescription;
            _currentTaskDueDate = dueDate;
            _isWaitingForTaskConfirmation = true;
            _isTaskFlowActive = true;
            _waitingForDateSelection = false;

            var mainContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 5, 0, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 450,
                Tag = "TaskButtonsContainer"
            };

            var stackPanel = new StackPanel();

            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Task '{taskDescription}' is added. Would you like to set a reminder for this task?",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var yesButton = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(25, 10, 25, 10),
                Margin = new Thickness(0, 0, 15, 0),
                Cursor = Cursors.Hand,
                Tag = "YesButton"
            };
            var yesText = new TextBlock { Text = "✅ YES", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            yesButton.Child = yesText;
            yesButton.MouseEnter += (s, e) => yesButton.Background = new SolidColorBrush(Color.FromRgb(0x5C, 0xBF, 0x60));
            yesButton.MouseLeave += (s, e) => yesButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            yesButton.MouseLeftButtonDown += (s, e) => HandleYesResponse();

            var noButton = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x66, 0x33, 0x33)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(25, 10, 25, 10),
                Cursor = Cursors.Hand,
                Tag = "NoButton"
            };
            var noText = new TextBlock { Text = "❌ NO", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            noButton.Child = noText;
            noButton.MouseEnter += (s, e) => noButton.Background = new SolidColorBrush(Color.FromRgb(0x77, 0x33, 0x33));
            noButton.MouseLeave += (s, e) => noButton.Background = new SolidColorBrush(Color.FromRgb(0x66, 0x33, 0x33));
            noButton.MouseLeftButtonDown += (s, e) => HandleNoResponse();

            buttonRow.Children.Add(yesButton);
            buttonRow.Children.Add(noButton);
            stackPanel.Children.Add(buttonRow);
            mainContainer.Child = stackPanel;

            _taskButtonsPanel = mainContainer;
            Dispatcher.Invoke(() =>
            {
                MessagesPanel.Children.Add(mainContainer);
                ScrollToBottom();
                InputBox.IsEnabled = false;
                BtnSend.IsEnabled = false;
                PlaceholderText.Text = "📌 Please use the buttons above to respond";
            });
        }

        private void HandleYesResponse()
        {
            if (!_isWaitingForTaskConfirmation || _isTaskFlowActive == false) return;

            if (_taskButtonsPanel != null)
            {
                var yesButton = FindVisualChild<Border>(_taskButtonsPanel, "YesButton");
                if (yesButton != null) { yesButton.IsEnabled = false; yesButton.Cursor = Cursors.Arrow; yesButton.Opacity = 0.5; }
                var noButton = FindVisualChild<Border>(_taskButtonsPanel, "NoButton");
                if (noButton != null) { noButton.IsEnabled = false; noButton.Cursor = Cursors.Arrow; noButton.Opacity = 0.5; }
            }

            AddUserMessage("SET A REMINDER FOR ME");
            ShowDateSelectionCalendar();
            _waitingForDateSelection = true;
            _isTaskFlowActive = true;
        }

        private void HandleNoResponse()
        {
            if (!_isWaitingForTaskConfirmation) return;

            if (_taskButtonsPanel != null)
            {
                var yesButton = FindVisualChild<Border>(_taskButtonsPanel, "YesButton");
                if (yesButton != null) { yesButton.IsEnabled = false; yesButton.Cursor = Cursors.Arrow; yesButton.Opacity = 0.5; }
                var noButton = FindVisualChild<Border>(_taskButtonsPanel, "NoButton");
                if (noButton != null) { noButton.IsEnabled = false; noButton.Cursor = Cursors.Arrow; noButton.Opacity = 0.5; }
            }

            AddUserMessage("DO NOT SET REMINDER");
            _taskManager.AddTask(_pendingTaskDescription, null);

            string taskDesc = _pendingTaskDescription;
            _pendingTaskDescription = string.Empty;
            _pendingTaskDate = null;
            _isWaitingForTaskConfirmation = false;
            _isTaskFlowActive = false;
            _waitingForDateSelection = false;

            AddBotMessage($"✅ Task '{taskDesc}' is added with no scheduled time.");

            Dispatcher.Invoke(() =>
            {
                InputBox.IsEnabled = true;
                BtnSend.IsEnabled = true;
                PlaceholderText.Text = "Ask me about cybersecurity...";
                InputBox.Focus();
            });

            UpdateFavoritesSidebar();
        }

        private void ShowDateSelectionCalendar()
        {
            var calendarContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 5, 0, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 400,
                Tag = "CalendarContainer"
            };

            var stackPanel = new StackPanel();

            stackPanel.Children.Add(new TextBlock
            {
                Text = "📅 Select a date for your reminder:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            _taskDatePicker = new DatePicker
            {
                SelectedDate = null,
                Width = 200,
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _taskDatePicker.BlackoutDates.AddDatesInPast();
            stackPanel.Children.Add(_taskDatePicker);

            var quickRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            quickRow.Children.Add(CreateQuickDateButton("Tomorrow", 1));
            quickRow.Children.Add(CreateQuickDateButton("Next Week", 7));
            quickRow.Children.Add(CreateQuickDateButton("Today", 0));
            stackPanel.Children.Add(quickRow);

            var submitButton = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(30, 10, 30, 10),
                Cursor = Cursors.Arrow,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = "SubmitButton",
                IsEnabled = false,
                Opacity = 0.5
            };
            var submitText = new TextBlock { Text = "✅ SUBMIT DATE", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            submitButton.Child = submitText;

            _currentSubmitButton = submitButton;

            _taskDatePicker.SelectedDateChanged += (s, e) =>
            {
                var selectedDate = _taskDatePicker.SelectedDate;

                submitButton.Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
                submitButton.Cursor = Cursors.Arrow;
                submitButton.IsEnabled = false;
                submitButton.Opacity = 0.5;

                if (selectedDate.HasValue)
                {
                    if (selectedDate.Value.Date >= DateTime.Now.Date)
                    {
                        submitButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93));
                        submitButton.Cursor = Cursors.Hand;
                        submitButton.IsEnabled = true;
                        submitButton.Opacity = 1.0;
                    }
                }
            };

            submitButton.MouseEnter += (s, e) =>
            {
                if (submitButton.IsEnabled)
                    submitButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x33, 0x85));
            };
            submitButton.MouseLeave += (s, e) =>
            {
                if (submitButton.IsEnabled)
                    submitButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93));
                else
                    submitButton.Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            };
            submitButton.MouseLeftButtonDown += (s, e) => HandleDateSubmit();

            stackPanel.Children.Add(submitButton);
            calendarContainer.Child = stackPanel;

            _calendarPanel = calendarContainer;
            Dispatcher.Invoke(() =>
            {
                MessagesPanel.Children.Add(calendarContainer);
                ScrollToBottom();
            });
        }

        private Border CreateQuickDateButton(string label, int daysFromNow)
        {
            var btn = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(4, 0, 4, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            var text = new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btn.Child = text;

            btn.MouseEnter += (s, e) => { btn.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x2A, 0x4E)); btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0xCC)); };
            btn.MouseLeave += (s, e) => { btn.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x3E)); btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)); };
            btn.MouseLeftButtonDown += (s, e) =>
            {
                var date = DateTime.Now.AddDays(daysFromNow);
                if (_taskDatePicker != null)
                {
                    _taskDatePicker.SelectedDate = date;
                }
            };

            return btn;
        }

        private void HandleDateSubmit()
        {
            if (!_waitingForDateSelection || _taskDatePicker == null) return;

            var selectedDate = _taskDatePicker.SelectedDate;
            if (!selectedDate.HasValue)
            {
                AddBotMessage("⚠️ Please select a date before submitting.");
                return;
            }

            if (selectedDate.Value.Date < DateTime.Now.Date)
            {
                AddBotMessage("⚠️ Cannot select a past date. Please choose today or a future date.");
                if (_currentSubmitButton != null)
                {
                    _currentSubmitButton.IsEnabled = false;
                    _currentSubmitButton.Cursor = Cursors.Arrow;
                    _currentSubmitButton.Opacity = 0.5;
                    _currentSubmitButton.Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
                }
                return;
            }

            if (_calendarPanel != null)
            {
                var submitButton = FindVisualChild<Border>(_calendarPanel, "SubmitButton");
                if (submitButton != null) { submitButton.IsEnabled = false; submitButton.Cursor = Cursors.Arrow; submitButton.Opacity = 0.5; }
            }

            string taskDesc = _pendingTaskDescription;

            var existingTask = _taskManager.GetTask(taskDesc);
            if (existingTask != null)
            {
                existingTask.ReminderDate = selectedDate.Value;
                _taskManager.AddTask(taskDesc, selectedDate.Value);
            }

            string datePhrase = GetDatePhrase(selectedDate.Value);

            _pendingTaskDescription = string.Empty;
            _pendingTaskDate = null;
            _isWaitingForTaskConfirmation = false;
            _isTaskFlowActive = false;
            _waitingForDateSelection = false;
            _taskDatePicker = null!;
            _currentSubmitButton = null;

            AddUserMessage($"📅 TASK SCHEDULED FOR {datePhrase.ToUpper()}");

            AddBotMessage($"✅ Task '{taskDesc}' is now scheduled {datePhrase}.");

            Dispatcher.Invoke(() =>
            {
                InputBox.IsEnabled = true;
                BtnSend.IsEnabled = true;
                PlaceholderText.Text = "Ask me about cybersecurity...";
                InputBox.Focus();
            });

            UpdateFavoritesSidebar();
        }

        private T FindVisualChild<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Tag as string == tag) return element;
                var result = FindVisualChild<T>(child, tag);
                if (result != null) return result;
            }
            return null!;
        }

        private string GetDatePhrase(DateTime date, string detectedPhrase = "")
        {
            if (!string.IsNullOrEmpty(detectedPhrase))
            {
                // If it's already a phrase like "in 3 days", return it as-is
                if (detectedPhrase.StartsWith("in ", StringComparison.OrdinalIgnoreCase) ||
                    detectedPhrase.StartsWith("on ", StringComparison.OrdinalIgnoreCase))
                {
                    return detectedPhrase;
                }

                var match = System.Text.RegularExpressions.Regex.Match(detectedPhrase,
                    @"in\s+(\d+)\s+(day|days|week|weeks|month|months|year|years)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return detectedPhrase;
                }

                string[] dayNames = { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
                if (dayNames.Contains(detectedPhrase.ToLowerInvariant()))
                {
                    return $"on {char.ToUpper(detectedPhrase[0]) + detectedPhrase.Substring(1)}";
                }

                if (detectedPhrase.Contains("day") || detectedPhrase.Contains("week") ||
                    detectedPhrase.Contains("month") || detectedPhrase.Contains("year"))
                {
                    return $"in {detectedPhrase}";
                }

                return detectedPhrase;
            }

            if (date.Date == DateTime.Now.Date) return "today";
            if (date.Date == DateTime.Now.Date.AddDays(1)) return "tomorrow";

            int daysUntil = (int)(date.Date - DateTime.Now.Date).TotalDays;
            if (daysUntil > 0 && daysUntil <= 7)
                return $"on {date.ToString("dddd")}";

            return $"on {date:dd MMMM yyyy}";
        }

        private string GetOrdinalSuffix(int day)
        {
            if (day >= 11 && day <= 13) return "th";
            switch (day % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        private string GetOrdinalWord(int number)
        {
            string[] ordinals = { "first", "second", "third", "fourth", "fifth",
                      "sixth", "seventh", "eighth", "ninth", "tenth" };
            if (number >= 1 && number <= 10)
                return ordinals[number - 1];
            return $"{number}th";
        }

        // ================= COMPLETE DATE PHRASE REMOVAL =================
        private string RemoveDatePhrasesFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // First, remove common date-related prepositions and words
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\b(on|in|for|due|at|by|from|of|to|about)\b\s*",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "from now" pattern
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\bfrom\s+now\b",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "time" at the end or after a number
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\s+time\b",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove date patterns: "in 3 days", "in 5 days time", "in 2 weeks", etc.
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\d+\s+(day|days|week|weeks|month|months|year|years)(?:\s+time)?",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "in a day", "in a week", etc.
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"a\s+(day|week|month|year)(?:\s+time)?",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "tomorrow", "today", "next week"
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\b(tomorrow|today|next\s+week)\b",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove day names
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\b(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "the" before numbers (e.g., "on the 8th")
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\bthe\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove ordinal dates (1st, 2nd, 3rd, 4th, etc.)
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\d{1,2}(st|nd|rd|th)",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove month names and abbreviations
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\b(january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\b",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "from now" at the end
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\s+from\s+now\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "on" at the end (leftover)
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\s+on\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "in" at the end
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\s+in\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "for" at the end
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\s+for\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove "due" at the end
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\s+due\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove standalone "s" at the end (from days, weeks, etc.)
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\s+s\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Clean up extra spaces
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = text.Trim();

            // If the text is just filler words, return empty
            string[] fillerWords = {
            "time", "from", "now", "remind", "me", "to", "about", "set",
            "remind me", "remind me to", "to remind me", "remind me about",
            "add task", "add a task", "add task to", "add a task to",
            "new task", "create task", "task", "reminder", "remind",
            "due", "on", "for", "at", "by", "in"
        };

            if (string.IsNullOrWhiteSpace(text)) return "";
            if (fillerWords.Contains(text.ToLowerInvariant())) return "";

            return text.Trim();
        }

        private bool IsCybersecurityRelated(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            string lowerText = text.ToLowerInvariant();

            var allTerms = _knowledgeBase.GetAllTerms();

            foreach (string term in allTerms)
            {
                if (lowerText.Contains(term.ToLowerInvariant()))
                    return true;
            }

            string[] variations = {
            "password", "passwords", "passphrase", "2fa", "mfa", "firewall", "firewalls",
            "antivirus", "vpn", "encryption", "phishing", "malware", "ransomware",
            "spyware", "trojan", "worm", "virus", "botnet", "rootkit", "backup",
            "scam", "privacy", "cyber", "hack", "breach", "exploit", "patch",
            "certificate", "biometric", "authentication", "permission", "access"
        };

            foreach (string term in variations)
            {
                if (lowerText.Contains(term))
                    return true;
            }

            string[] cyberActions = {
            "review", "update", "upgrade", "backup", "install", "change", "enable",
            "set up", "setup", "disable", "configure", "remove", "block", "allow",
            "approve", "scan", "check", "verify", "monitor", "investigate", "reset",
            "create", "delete", "modify", "add", "remove", "secure", "protect",
            "encrypt", "decrypt", "patch", "fix", "restore", "recover", "test","software",
            "security code", "network", "server", "system", "data", "privacy", "authentication",
        };

            string[] cyberObjects = {
            "password", "passphrase", "firewall", "vpn", "antivirus", "backup",
            "encryption", "patch", "2fa", "mfa", "access", "permission",
            "email", "account", "user", "software", "security", "network",
            "server", "system", "data", "privacy", "authentication",
            "certificate", "proxy", "settings", "configuration", "credentials",
            "login", "session", "cookie", "token", "oauth", "quarantine",
            "hash", "salt", "privilege", "permission", "access","cybersecurity", "setting",
            "phone", "smartphone", "mobile", "tablet", "ipad", "iphone", "android",
            "laptop", "notebook", "computer", "pc", "mac", "desktop",
            "device", "devices", "hardware", "router", "modem", "gateway",
            "smart tv", "television", "camera", "webcam", "printer", "scanner",
            "usb", "flash drive", "external drive", "hard drive", "ssd",
            "nas", "storage", "workstation", "terminal"
        };

            bool hasAction = false;
            bool hasObject = false;

            foreach (string action in cyberActions)
            {
                if (lowerText.Contains(action))
                {
                    hasAction = true;
                    break;
                }
            }

            foreach (string obj in cyberObjects)
            {
                if (lowerText.Contains(obj))
                {
                    hasObject = true;
                    break;
                }
            }

            if (hasAction && hasObject)
                return true;

            string[] securityConcerns = {
            "outdated", "vulnerable", "compromised", "infected", "hacked",
            "breached", "exposed", "leaked", "stolen", "lost", "corrupted",
            "damaged", "slow", "old", "unsafe", "insecure"
        };

            foreach (string concern in securityConcerns)
            {
                if (lowerText.Contains(concern) && hasObject)
                    return true;
            }

            return false;
        }

        // ================= COMPLETE TASK NUMBER EXTRACTION =================
        private int ExtractTaskNumber(string text)
        {
            string[] wordNumbers = { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
            string[] ordinals = { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth" };
            string[] ordinalShort = { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th" };

            // Clean the text first - remove "task", "reminder", "completed", etc.
            string cleaned = text.ToLowerInvariant();
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(task|reminder|completed|delete|remove|clear|erase)\b", "").Trim();

            // Check ordinals first (first, second, third...)
            for (int i = 0; i < ordinals.Length; i++)
            {
                if (cleaned.Contains(ordinals[i]) || cleaned.Contains(ordinalShort[i]))
                    return i + 1;
            }

            // Check word numbers (one, two, three...)
            for (int i = 0; i < wordNumbers.Length; i++)
            {
                if (cleaned.Contains(wordNumbers[i]))
                    return i + 1;
            }

            // Check simple numbers
            var match = System.Text.RegularExpressions.Regex.Match(cleaned, @"\b\d+\b");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                if (number >= 1 && number <= 20)
                    return number;
            }

            return -1;
        }

        private string HandleReminderRequest(string input)
        {
            string lowerInput = input.ToLowerInvariant();
            string cleanedInput = input;
            string[] symbolsToRemove = { "_", "(", ")", "{", "}", "[", "]", "/", "\\", "!", "?", "@", "#", "$", "%", "^", "&", "*", "~", "`", "'", "\"", ";", ":", ",", "." };

            foreach (string symbol in symbolsToRemove)
            {
                while (cleanedInput.StartsWith(symbol))
                {
                    cleanedInput = cleanedInput.Substring(symbol.Length).TrimStart();
                }
            }

            string cleanedLower = cleanedInput.ToLowerInvariant();

            // ================================================================
            // CLEAN TEXT HELPER
            // ================================================================
            string CleanText(string text)
            {
                if (string.IsNullOrEmpty(text)) return text;

                bool changed = true;
                int maxIterations = 10;
                int iterations = 0;

                while (changed && iterations < maxIterations)
                {
                    changed = false;
                    iterations++;

                    string[] myPatterns = { "my (", "my [", "my {", "my \"", "my '" };
                    foreach (string pattern in myPatterns)
                    {
                        int index = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            char open = pattern[pattern.Length - 1];
                            char close = open switch
                            {
                                '(' => ')',
                                '[' => ']',
                                '{' => '}',
                                '"' => '"',
                                '\'' => '\'',
                                _ => ' '
                            };

                            int startAfterMy = index + pattern.Length;
                            int closeIndex = text.IndexOf(close, startAfterMy);
                            if (closeIndex > startAfterMy)
                            {
                                string content = text.Substring(startAfterMy, closeIndex - startAfterMy).Trim();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    string before = text.Substring(0, index);
                                    string after = text.Substring(closeIndex + 1);
                                    text = (before + content + after).Trim();
                                    changed = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!changed)
                    {
                        string[] pairs = { "()", "[]", "{}", "\"\"", "''" };
                        foreach (string pair in pairs)
                        {
                            char open = pair[0];
                            char close = pair[1];

                            int openIndex = text.IndexOf(open);
                            if (openIndex >= 0)
                            {
                                bool hasMyBefore = false;
                                if (openIndex >= 3)
                                {
                                    string before = text.Substring(openIndex - 3, 3).ToLowerInvariant();
                                    if (before == "my ")
                                    {
                                        hasMyBefore = true;
                                    }
                                }

                                int closeIndex = text.LastIndexOf(close);
                                if (closeIndex > openIndex)
                                {
                                    string between = text.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();
                                    if (!string.IsNullOrEmpty(between) && !between.Contains(open) && !between.Contains(close))
                                    {
                                        if (hasMyBefore)
                                        {
                                            text = text.Remove(openIndex - 3, 3);
                                            text = text.Remove(closeIndex - 3, 1);
                                            text = text.Remove(openIndex - 3, 1).Trim();
                                        }
                                        else
                                        {
                                            text = text.Remove(closeIndex, 1);
                                            text = text.Remove(openIndex, 1).Trim();
                                        }
                                        changed = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!changed)
                    {
                        string[] separators = { " - ", " : ", " > ", " * ", " • ", " · ", " – ", " — ", " _ ", " | " };
                        foreach (string sep in separators)
                        {
                            if (text.StartsWith(sep))
                            {
                                text = text.Substring(sep.Length).Trim();
                                changed = true;
                                break;
                            }
                            string symbol = sep.Trim();
                            if (text.StartsWith(symbol + " "))
                            {
                                text = text.Substring((symbol + " ").Length).Trim();
                                changed = true;
                                break;
                            }
                        }
                    }

                    if (!changed)
                    {
                        string[] symbolsToRemoveLocal = { "_", "(", ")", "{", "}", "[", "]", "/", "\\", "!", "?", "@", "#", "$", "%", "^", "&", "*", "~", "`", "'", "\"", ";", ":", ",", "." };
                        foreach (string symbol in symbolsToRemoveLocal)
                        {
                            if (text.StartsWith(symbol + " "))
                            {
                                text = text.Substring((symbol + " ").Length).TrimStart();
                                changed = true;
                                break;
                            }
                            else if (text.StartsWith(symbol))
                            {
                                text = text.Substring(symbol.Length).TrimStart();
                                changed = true;
                                break;
                            }
                        }
                    }

                    if (!changed)
                    {
                        string[] symbolsToRemoveLocal = { "_", "(", ")", "{", "}", "[", "]", "/", "\\", "!", "?", "@", "#", "$", "%", "^", "&", "*", "~", "`", "'", "\"", ";", ":", ",", "." };
                        foreach (string symbol in symbolsToRemoveLocal)
                        {
                            if (text.EndsWith(" " + symbol))
                            {
                                text = text.Substring(0, text.Length - (1 + symbol.Length)).TrimEnd();
                                changed = true;
                                break;
                            }
                            else if (text.EndsWith(symbol))
                            {
                                text = text.Substring(0, text.Length - symbol.Length).TrimEnd();
                                changed = true;
                                break;
                            }
                        }
                    }

                    while (text.Contains("  "))
                        text = text.Replace("  ", " ");
                }

                if (text.Length > 1)
                {
                    string[] symbolsToRemoveLocal = { "_", "(", ")", "{", "}", "[", "]", "/", "\\", "!", "?", "@", "#", "$", "%", "^", "&", "*", "~", "`", "'", "\"", ";", ":", ",", "." };
                    foreach (string symbol in symbolsToRemoveLocal)
                    {
                        if (text.StartsWith(symbol))
                            text = text.Substring(symbol.Length).TrimStart();
                        if (text.EndsWith(symbol))
                            text = text.Substring(0, text.Length - symbol.Length).TrimEnd();
                    }
                }

                while (text.Contains("  "))
                    text = text.Replace("  ", " ");

                return text.Trim();
            }

            // ================================================================
            // FIRST: Check for waiting states
            // ================================================================
            if (_waitingForReminderTopic)
            {
                if (!lowerInput.Contains("clear") && !lowerInput.Contains("delete") &&
                    !lowerInput.Contains("remove") && !lowerInput.Contains("erase") &&
                    !lowerInput.Contains("add") && !lowerInput.Contains("remind me"))
                {
                    _waitingForReminderTopic = false;
                    string reminderText = input.Trim();

                    string[] prefixes = { "remind me to", "remind me about", "remind me of", "remind me", "to remind me" };
                    foreach (var prefix in prefixes)
                    {
                        if (reminderText.ToLowerInvariant().StartsWith(prefix))
                        {
                            reminderText = reminderText.Substring(prefix.Length).Trim();
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(reminderText) && reminderText.Length >= 2)
                    {
                        if (!IsCybersecurityRelated(reminderText))
                        {
                            _waitingForReminderTopic = true;
                            return "🔐 As a cybersecurity bot, I can only add cybersecurity-related reminders. Please specify a security task like:\n" +
                                   "• Update my password\n" +
                                   "• Enable two-factor authentication\n" +
                                   "• Backup my files\n" +
                                   "• Install antivirus software\n" +
                                   "• Review my privacy settings";
                        }

                        string detectedPhrase = "";
                        DateTime? reminderDate = ParseReminderDate(reminderText, out detectedPhrase);
                        reminderText = RemoveDatePhrasesFromText(reminderText);

                        if (!string.IsNullOrEmpty(reminderText))
                        {
                            string[] removeWords = { "remind me", "remind", "reminder", "to remind", "about remind" };
                            foreach (var word in removeWords)
                            {
                                if (reminderText.ToLowerInvariant().Contains(word))
                                {
                                    reminderText = reminderText.Replace(word, "", StringComparison.OrdinalIgnoreCase).Trim();
                                }
                            }

                            reminderText = CleanText(reminderText);

                            if (!string.IsNullOrEmpty(reminderText))
                            {
                                _taskManager.AddReminder(reminderText, reminderDate);
                                if (reminderDate.HasValue)
                                {
                                    string datePhrase = GetDatePhrase(reminderDate.Value, detectedPhrase);
                                    return $"✅ Security reminder set for '{reminderText}' on {datePhrase}. (Saved to database)";
                                }
                                else
                                {
                                    return $"✅ Security reminder set for '{reminderText}'. (Saved to database)";
                                }
                            }
                        }
                        return "Please specify what you'd like to be reminded about. For example: 'update password' or 'enable 2FA'.";
                    }
                    return "Please specify a valid reminder. For example: 'update password' or 'enable 2FA'.";
                }
            }

            if (_waitingForTaskName)
            {
                if (!lowerInput.Contains("clear") && !lowerInput.Contains("delete") &&
                    !lowerInput.Contains("remove") && !lowerInput.Contains("erase") &&
                    !lowerInput.Contains("add") && !lowerInput.Contains("remind me"))
                {
                    _waitingForTaskName = false;
                    string taskText = input.Trim();

                    if (!string.IsNullOrEmpty(taskText))
                    {
                        if (!IsCybersecurityRelated(taskText))
                        {
                            _waitingForTaskName = true;
                            return "As a cybersecurity bot, I can only add cybersecurity-related tasks. Please specify a security task like:\n" +
                                   "• Update my password\n" +
                                   "• Enable two-factor authentication\n" +
                                   "• Backup my files\n" +
                                   "• Install antivirus software\n" +
                                   "• Review my privacy settings\n\n" +
                                   "For personal tasks, you might want to use a different app.";
                        }

                        string detectedPhrase = "";
                        DateTime? dueDate = ParseReminderDate(taskText, out detectedPhrase);
                        taskText = RemoveDatePhrasesFromText(taskText);

                        if (!string.IsNullOrEmpty(taskText))
                        {
                            if (dueDate.HasValue)
                            {
                                string datePhrase = GetDatePhrase(dueDate.Value, detectedPhrase);
                                _taskManager.AddTask(taskText, dueDate.Value);
                                _taskManager.AddReminder(taskText, dueDate.Value);
                                return $"✅ Security task '{taskText}' is added and scheduled for {datePhrase}. (Saved to database)";
                            }
                            else
                            {
                                if (_taskManager.TaskExists(taskText))
                                {
                                    var existingTask = _taskManager.GetTask(taskText);
                                    if (existingTask != null && existingTask.ReminderDate.HasValue)
                                    {
                                        string datePhrase = GetDatePhrase(existingTask.ReminderDate.Value, "");
                                        return $"⚠️ Security task '{taskText}' is already set for {datePhrase}.";
                                    }
                                    else
                                    {
                                        _pendingTaskDescription = taskText;
                                        _pendingTaskDate = null;
                                        _isWaitingForTaskConfirmation = true;
                                        Dispatcher.Invoke(() => ShowTaskConfirmationButtons(taskText, null));
                                        return $"⚠️ Security task '{taskText}' already exists.\n\n📌 Would you like to set a reminder for this task?";
                                    }
                                }
                                else
                                {
                                    _pendingTaskDescription = taskText;
                                    _pendingTaskDate = null;
                                    _isWaitingForTaskConfirmation = true;
                                    Dispatcher.Invoke(() => ShowTaskConfirmationButtons(taskText, null));
                                    return string.Empty;
                                }
                            }
                        }
                        else
                        {
                            _waitingForTaskName = true;
                            return "Please specify a valid cybersecurity task. For example: 'update password' or 'back up files'.";
                        }
                    }
                    else
                    {
                        _waitingForTaskName = true;
                        return "Please specify a cybersecurity task. eg 'review privacy setting' or 'enable 2FA'.";
                    }
                }
            }

            if (_isTaskFlowActive)
            {
                return "📌 Please use the buttons above to respond to the task confirmation.";
            }

            // ================================================================
            // CHECK FOR ADD TASK
            // ================================================================
            if (cleanedLower.Contains("add a task") || cleanedLower.Contains("add task") ||
                cleanedLower.Contains("new task") || cleanedLower.Contains("create task") ||
                cleanedLower.Contains("add a new task") || (cleanedLower.Contains("add") && cleanedLower.Contains("task")))
            {
                _waitingForReminderTopic = false;
                _waitingForTaskName = false;

                if (cleanedLower.Contains("set reminder for task") ||
                    cleanedLower.Contains("remind me to add task") ||
                    cleanedLower.Contains("reminder to add task"))
                {
                    return null!;
                }

                string taskText = "";
                DateTime? dueDate = null;
                string detectedPhrase = "";

                try
                {
                    string cleanedForExtraction = input;

                    string[] taskPrefixes = {
                    "add a task to", "add task to", "add a new task", "add a task",
                    "add task", "new task", "create task", "add a new task to",
                    "add task on", "add task for", "add a task on", "add a task for"
                };

                    foreach (string prefix in taskPrefixes)
                    {
                        if (cleanedForExtraction.ToLowerInvariant().Contains(prefix.ToLowerInvariant()))
                        {
                            int index = cleanedForExtraction.ToLowerInvariant().IndexOf(prefix.ToLowerInvariant());
                            cleanedForExtraction = cleanedForExtraction.Substring(index + prefix.Length).Trim();
                            break;
                        }
                    }

                    if (cleanedForExtraction == input)
                    {
                        string[] simplePrefixes = { "add a task", "add task", "new task", "create task" };
                        foreach (string prefix in simplePrefixes)
                        {
                            if (cleanedForExtraction.ToLowerInvariant().Contains(prefix.ToLowerInvariant()))
                            {
                                int index = cleanedForExtraction.ToLowerInvariant().IndexOf(prefix.ToLowerInvariant());
                                cleanedForExtraction = cleanedForExtraction.Substring(index + prefix.Length).Trim();
                                break;
                            }
                        }
                    }

                    taskText = cleanedForExtraction;

                    // Parse the date FIRST before cleaning
                    dueDate = ParseReminderDate(taskText, out detectedPhrase);

                    // THEN clean the text AFTER parsing the date
                    taskText = RemoveDatePhrasesFromText(taskText);

                    // Remove common phrases
                    string[] removePhrases = { "add task", "add a task", "new task", "create task" };
                    foreach (var phrase in removePhrases)
                    {
                        if (taskText.ToLowerInvariant().Contains(phrase))
                        {
                            taskText = taskText.Replace(phrase, "", StringComparison.OrdinalIgnoreCase).Trim();
                        }
                    }

                    // Clean up extra spaces
                    taskText = CleanText(taskText);

                    // Remove any leading prepositions that might remain
                    taskText = System.Text.RegularExpressions.Regex.Replace(taskText, @"^(?:on|for|to|about|due|in)\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    // Final cleanup
                    taskText = taskText.Trim();

                    if (string.IsNullOrEmpty(taskText) || taskText.Length < 2)
                    {
                        _waitingForTaskName = true;
                        return "Which cybersecurity task would you like to add?";
                    }

                    if (!IsCybersecurityRelated(taskText))
                    {
                        _waitingForTaskName = true;
                        return "As a cybersecurity bot, I can only add cybersecurity-related tasks. Please specify a security task like:\n" +
                               "• Update my password\n" +
                               "• Enable two-factor authentication\n" +
                               "• Backup my files\n" +
                               "• Install antivirus software\n" +
                               "• Review my privacy settings\n\n" +
                               "For personal tasks, you might want to use a different app.";
                    }

                    if (dueDate.HasValue)
                    {
                        string datePhrase = GetDatePhrase(dueDate.Value, detectedPhrase);

                        // If task exists, UPDATE it instead of saying "already set"
                        if (_taskManager.TaskExists(taskText))
                        {
                            var existingTask = _taskManager.GetTask(taskText);
                            if (existingTask != null)
                            {
                                // Update the date
                                existingTask.ReminderDate = dueDate;
                                _taskManager.AddTask(taskText, dueDate.Value);
                                // Also update the reminder if it exists
                                var existingReminder = _taskManager.GetReminder(taskText);
                                if (existingReminder != null)
                                {
                                    existingReminder.ReminderDate = dueDate;
                                    _taskManager.AddReminder(taskText, dueDate.Value);
                                }
                                return $"✅ Security task '{taskText}' date has been updated to {datePhrase}. (Saved to database)";
                            }
                        }
                        else
                        {
                            _taskManager.AddTask(taskText, dueDate.Value);
                            _taskManager.AddReminder(taskText, dueDate.Value);
                            return $"✅ Security task '{taskText}' is added and scheduled for {datePhrase}. (Saved to database)";
                        }
                    }
                    else
                    {
                        if (_taskManager.TaskExists(taskText))
                        {
                            var existingTask = _taskManager.GetTask(taskText);
                            if (existingTask != null && existingTask.ReminderDate.HasValue)
                            {
                                string datePhrase = GetDatePhrase(existingTask.ReminderDate.Value, "");
                                return $"⚠️ Security task '{taskText}' is already set for {datePhrase}.";
                            }
                            else
                            {
                                return $"⚠️ Security task '{taskText}' already exists.";
                            }
                        }
                        else
                        {
                            _taskManager.AddTask(taskText, null);
                            _pendingTaskDescription = taskText;
                            _pendingTaskDate = dueDate;
                            _isWaitingForTaskConfirmation = true;
                            Dispatcher.Invoke(() => ShowTaskConfirmationButtons(taskText, dueDate));
                            return string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Add task error: {ex.Message}");
                    _waitingForTaskName = true;
                    return "Please specify a cybersecurity task. For example: 'update password' or 'enable 2FA'.";
                }
            }

            // ================================================================
            // CHECK FOR "SET REMINDER FOR TASK"
            // ================================================================
            if (cleanedLower.Contains("set reminder for task") ||
                cleanedLower.Contains("remind me to add task") ||
                cleanedLower.Contains("reminder to add task") ||
                cleanedLower.Contains("remind me to add a task") ||
                cleanedLower.Contains("reminder to add a task") ||
                cleanedLower.Contains("set a reminder for task") ||
                cleanedLower.Contains("set reminder to add task") ||
                cleanedLower.Contains("remind me about task") ||
                cleanedLower.Contains("reminder about task"))
            {
                string taskText = "";
                string[] triggerPhrases = {
                "set reminder for task", "remind me to add task", "reminder to add task",
                "remind me to add a task", "reminder to add a task", "set a reminder for task",
                "set reminder to add task", "remind me about task", "reminder about task"
            };

                int bestStartIndex = int.MaxValue;
                string bestPhrase = "";

                foreach (string phrase in triggerPhrases)
                {
                    int index = cleanedLower.IndexOf(phrase);
                    if (index >= 0 && index < bestStartIndex)
                    {
                        bestStartIndex = index;
                        bestPhrase = phrase;
                    }
                }

                if (!string.IsNullOrEmpty(bestPhrase))
                {
                    int startIndex = bestStartIndex + bestPhrase.Length;
                    if (startIndex < cleanedInput.Length)
                    {
                        taskText = cleanedInput.Substring(startIndex).Trim();
                    }
                }

                if (string.IsNullOrEmpty(taskText))
                {
                    foreach (string phrase in triggerPhrases)
                    {
                        int index = lowerInput.IndexOf(phrase);
                        if (index >= 0)
                        {
                            int startIndex = index + phrase.Length;
                            if (startIndex < input.Length)
                            {
                                taskText = input.Substring(startIndex).Trim();
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(taskText))
                {
                    taskText = System.Text.RegularExpressions.Regex.Replace(taskText, @"on\s+the\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    taskText = System.Text.RegularExpressions.Regex.Replace(taskText, @"for\s+the\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    string[] removePhrases = { "add task", "add a task", "remind me to add task", "reminder to add task", "set reminder for task" };
                    foreach (var phrase in removePhrases)
                    {
                        if (taskText.ToLowerInvariant().Contains(phrase))
                        {
                            taskText = taskText.Replace(phrase, "", StringComparison.OrdinalIgnoreCase).Trim();
                        }
                    }

                    taskText = CleanText(taskText);
                }

                if (string.IsNullOrEmpty(taskText) || taskText.Length < 2)
                {
                    _waitingForTaskName = true;
                    return "Which cybersecurity task would you like to add?";
                }

                if (!IsCybersecurityRelated(taskText))
                {
                    _waitingForTaskName = true;
                    return "As a cybersecurity bot, I can only add cybersecurity-related tasks. Please specify a security task like:\n" +
                           "• Update my password\n" +
                           "• Enable two-factor authentication\n" +
                           "• Backup my files\n" +
                           "• Install antivirus software\n" +
                           "• Review my privacy settings\n\n" +
                           "For personal tasks, you might want to use a different app.";
                }

                string detectedPhrase = "";
                DateTime? dueDate = ParseReminderDate(taskText, out detectedPhrase);
                taskText = RemoveDatePhrasesFromText(taskText);

                if (!string.IsNullOrEmpty(taskText))
                {
                    taskText = CleanText(taskText);
                }

                if (string.IsNullOrEmpty(taskText))
                {
                    _waitingForTaskName = true;
                    return "Which cybersecurity task would you like to add with a reminder? Please specify a security task.";
                }

                if (dueDate.HasValue)
                {
                    string datePhrase = GetDatePhrase(dueDate.Value, detectedPhrase);

                    // If task exists, UPDATE it instead of saying "already set"
                    if (_taskManager.TaskExists(taskText))
                    {
                        var existingTask = _taskManager.GetTask(taskText);
                        if (existingTask != null)
                        {
                            existingTask.ReminderDate = dueDate;
                            var existingReminder = _taskManager.GetReminder(taskText);
                            if (existingReminder != null)
                            {
                                existingReminder.ReminderDate = dueDate;
                            }
                            else
                            {
                                _taskManager.AddReminder(taskText, dueDate.Value);
                            }
                            _taskManager.AddTask(taskText, dueDate.Value);
                            return $"✅ Security task '{taskText}' date has been updated to {datePhrase}. (Saved to database)";
                        }
                    }
                    else
                    {
                        _taskManager.AddTask(taskText, dueDate.Value);
                        _taskManager.AddReminder(taskText, dueDate.Value);
                        return $"✅ Security task '{taskText}' is added and reminder set for {datePhrase}. (Saved to database)";
                    }
                }
                else
                {
                    if (_taskManager.TaskExists(taskText))
                    {
                        var existingTask = _taskManager.GetTask(taskText);
                        if (existingTask != null)
                        {
                            Dispatcher.Invoke(() => ShowTaskConfirmationButtons(taskText, null));
                            return $"📌 Security task '{taskText}' already exists. Would you like to set a reminder for this task?";
                        }
                    }

                    _pendingTaskDescription = taskText;
                    _pendingTaskDate = null;
                    _isWaitingForTaskConfirmation = true;
                    Dispatcher.Invoke(() => ShowTaskConfirmationButtons(taskText, null));
                    return string.Empty;
                }
            }

            // ================================================================
            // CHECK FOR REMINDER
            // ================================================================
            if (cleanedLower.Contains("remind me on") || cleanedLower.Contains("remind me for") ||
                cleanedLower.Contains("remind me with") || cleanedLower.Contains("remind me about") ||
                cleanedLower.Contains("remind me to") || cleanedLower.Contains("remind me") ||
                cleanedLower.Contains("set reminder for") || cleanedLower.Contains("set reminder on") ||
                cleanedLower.Contains("set reminder with") || cleanedLower.Contains("set a reminder for") ||
                cleanedLower.Contains("set a reminder on") || cleanedLower.Contains("set a reminder with") ||
                cleanedLower.Contains("add a reminder for") || cleanedLower.Contains("add a reminder on") ||
                cleanedLower.Contains("add a reminder with") || cleanedLower.Contains("add reminder") ||
                cleanedLower.Contains("reminder for") || cleanedLower.Contains("reminder on") ||
                cleanedLower.Contains("reminder with") || cleanedLower.Contains("reminder about") ||
                cleanedLower.Contains("reminder"))
            {
                _waitingForReminderTopic = false;
                _waitingForTaskName = false;

                if (cleanedLower.Contains("set reminder for task") ||
                    cleanedLower.Contains("remind me to add task") ||
                    cleanedLower.Contains("reminder to add task") ||
                    cleanedLower.Contains("remind me to add a task") ||
                    cleanedLower.Contains("reminder to add a task") ||
                    cleanedLower.Contains("set a reminder for task") ||
                    cleanedLower.Contains("set reminder to add task") ||
                    cleanedLower.Contains("remind me about task") ||
                    cleanedLower.Contains("reminder about task"))
                {
                    return null!;
                }

                string reminderText = "";
                DateTime? reminderDate = null;
                string detectedPhrase = "";

                string[] reminderKeywords = {
                "remind me on", "remind me for", "remind me with", "remind me about", "remind me to",
                "set reminder for", "set reminder on", "set reminder with",
                "set a reminder for", "set a reminder on", "set a reminder with",
                "add a reminder for", "add a reminder on", "add a reminder with",
                "reminder for", "reminder on", "reminder with", "reminder about",
                "remind me", "add reminder", "set reminder", "reminder"
            };

                int bestStartIndex = int.MaxValue;
                string bestKeyword = "";

                foreach (string keyword in reminderKeywords)
                {
                    int index = cleanedLower.IndexOf(keyword);
                    if (index >= 0 && index < bestStartIndex)
                    {
                        bestStartIndex = index;
                        bestKeyword = keyword;
                    }
                }

                if (!string.IsNullOrEmpty(bestKeyword))
                {
                    int startIndex = bestStartIndex + bestKeyword.Length;
                    if (startIndex < cleanedInput.Length)
                    {
                        reminderText = cleanedInput.Substring(startIndex).Trim();
                    }
                }

                if (string.IsNullOrEmpty(reminderText))
                {
                    foreach (string keyword in reminderKeywords)
                    {
                        int index = lowerInput.IndexOf(keyword);
                        if (index >= 0)
                        {
                            int startIndex = index + keyword.Length;
                            if (startIndex < input.Length)
                            {
                                reminderText = input.Substring(startIndex).Trim();
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(reminderText))
                {
                    reminderText = CleanText(reminderText);
                }

                if (string.IsNullOrEmpty(reminderText) || reminderText.Length < 2)
                {
                    _waitingForReminderTopic = true;
                    return "What would you like to be reminded about?";
                }

                if (!IsCybersecurityRelated(reminderText))
                {
                    return "As a cybersecurity bot, I can only add cybersecurity-related reminders. Please specify a security task like:\n" +
                           "• Update my password\n" +
                           "• Enable two-factor authentication\n" +
                           "• Backup my files\n" +
                           "• Install antivirus software\n" +
                           "• Review my privacy settings\n\n" +
                           "For personal tasks, you might want to use a different app.";
                }

                reminderDate = ParseReminderDate(reminderText, out detectedPhrase);
                reminderText = RemoveDatePhrasesFromText(reminderText);

                if (!string.IsNullOrEmpty(reminderText))
                {
                    reminderText = CleanText(reminderText);
                }

                if (string.IsNullOrEmpty(reminderText))
                {
                    _waitingForReminderTopic = true;
                    return "What would you like to be reminded about?";
                }

                // If reminder exists, UPDATE it instead of saying "already set"
                if (_taskManager.ReminderExists(reminderText))
                {
                    var existingReminder = _taskManager.GetReminder(reminderText);
                    if (existingReminder != null)
                    {
                        if (reminderDate.HasValue)
                        {
                            existingReminder.ReminderDate = reminderDate;
                            _taskManager.AddReminder(reminderText, reminderDate);
                            string datePhrase = GetDatePhrase(reminderDate.Value, detectedPhrase);
                            return $"✅ Security reminder '{reminderText}' date has been updated to {datePhrase}. (Saved to database)";
                        }
                        else
                        {
                            if (existingReminder.ReminderDate.HasValue)
                            {
                                string datePhrase = GetDatePhrase(existingReminder.ReminderDate.Value, "");
                                return $"✅ Security reminder '{reminderText}' is already set for {datePhrase}.";
                            }
                            else
                            {
                                return $"✅ Security reminder '{reminderText}' is already set.";
                            }
                        }
                    }
                }
                else
                {
                    _taskManager.AddReminder(reminderText, reminderDate);
                    if (reminderDate.HasValue)
                    {
                        string datePhrase = GetDatePhrase(reminderDate.Value, detectedPhrase);
                        return $"✅ Security reminder set for '{reminderText}' for {datePhrase}. (Saved to database)";
                    }
                    else
                    {
                        return $"✅ Security reminder set for '{reminderText}'. (Saved to database)";
                    }
                }
            }

            // ================================================================
            // DELETE / REMOVE / CLEAR / ERASE TASK
            // ================================================================
            bool isDeleteTask = lowerInput.StartsWith("delete task") || lowerInput.StartsWith("remove task") ||
                                lowerInput.StartsWith("clear task") || lowerInput.StartsWith("erase task");

            if (isDeleteTask)
            {
                string taskToClear = input.Substring(input.IndexOf(' ') + 1).Trim();
                if (taskToClear.ToLowerInvariant().StartsWith("task "))
                    taskToClear = taskToClear.Substring(5).Trim();

                // Check if user wants to delete ALL tasks
                if (taskToClear.ToLowerInvariant() == "all" || taskToClear.ToLowerInvariant() == "all tasks" ||
                    taskToClear.ToLowerInvariant() == "every" || taskToClear.ToLowerInvariant() == "every task")
                {
                    int count = _taskManager.DeleteAllTasks(_recycleBin);
                    Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                    return $"🗑️ All tasks have been cleared. ({count} tasks moved to Recycle Bin)";
                }

                // Check if user specified a number (1, 2, one, two, first, second, 1st, 2nd)
                int numberFromWords = ExtractTaskNumber(taskToClear);
                if (numberFromWords > 0)
                {
                    var activeTasks = _taskManager.GetActiveTasks();
                    if (activeTasks.Count == 0)
                        return "❌ No active tasks to delete.";

                    if (numberFromWords <= activeTasks.Count)
                    {
                        var task = activeTasks[numberFromWords - 1];
                        if (_taskManager.DeleteTaskByDescription(task.Description, _recycleBin))
                        {
                            Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                            return $"🗑️ Task '{task.Description}' has been deleted. (Moved to Recycle Bin)";
                        }
                    }
                    else
                    {
                        return $"❌ Task #{numberFromWords} not found. You have {activeTasks.Count} active tasks.";
                    }
                }

                // Clean the description
                taskToClear = taskToClear.Trim().TrimEnd('.', '!', '?');

                if (string.IsNullOrEmpty(taskToClear) || taskToClear.Length < 2)
                {
                    return "📌 Please specify which task to delete. Example: 'delete task update password' or 'delete task 5'";
                }

                // Try to delete by exact description
                if (_taskManager.DeleteTaskByDescription(taskToClear, _recycleBin))
                {
                    Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                    return $"🗑️ Task '{taskToClear}' has been deleted. (Moved to Recycle Bin)";
                }

                // Try partial match
                var allTasks = _taskManager.GetActiveTasks();
                var matchingTasks = allTasks.Where(t => t.Description.Contains(taskToClear, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matchingTasks.Count == 0)
                {
                    return $"❌ Task '{taskToClear}' not found. Check your tasks with 'summary' or 'tasks'.";
                }
                else if (matchingTasks.Count == 1)
                {
                    var task = matchingTasks.First();
                    if (_taskManager.DeleteTaskByDescription(task.Description, _recycleBin))
                    {
                        Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                        return $"🗑️ Task '{task.Description}' has been deleted. (Moved to Recycle Bin)";
                    }
                }
                else
                {
                    var result = new List<string>();
                    result.Add($"⚠️ Multiple tasks found matching '{taskToClear}':");
                    int counter = 1;
                    foreach (var t in matchingTasks)
                    {
                        string dateInfo = t.ReminderDate.HasValue ? $" (Due: {t.ReminderDate.Value:dd MMM yyyy})" : "";
                        result.Add($"  [{counter}] {t.Description}{dateInfo}");
                        counter++;
                    }
                    result.Add($"\n💡 Use 'delete task [number]' to delete a specific one.");
                    return string.Join("\n", result);
                }
            }

            // ================================================================
            // DELETE / REMOVE / CLEAR / ERASE REMINDER
            // ================================================================
            bool isDeleteReminder = lowerInput.StartsWith("delete reminder") || lowerInput.StartsWith("remove reminder") ||
                                    lowerInput.StartsWith("clear reminder") || lowerInput.StartsWith("erase reminder");

            if (isDeleteReminder)
            {
                string reminderToClear = input.Substring(input.IndexOf(' ') + 1).Trim();
                if (reminderToClear.ToLowerInvariant().StartsWith("reminder "))
                    reminderToClear = reminderToClear.Substring(9).Trim();

                if (reminderToClear.ToLowerInvariant() == "all" || reminderToClear.ToLowerInvariant() == "all reminders" ||
                    reminderToClear.ToLowerInvariant() == "every" || reminderToClear.ToLowerInvariant() == "every reminder")
                {
                    int count = _taskManager.DeleteAllReminders(_recycleBin);
                    Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                    return $"🗑️ All reminders have been cleared. ({count} reminders moved to Recycle Bin)";
                }

                int numberFromWords = ExtractTaskNumber(reminderToClear);
                if (numberFromWords > 0)
                {
                    var activeReminders = _taskManager.GetActiveReminders();
                    if (activeReminders.Count == 0)
                        return "❌ No active reminders to delete.";

                    if (numberFromWords <= activeReminders.Count)
                    {
                        var reminder = activeReminders[numberFromWords - 1];
                        if (_taskManager.DeleteReminderByDescription(reminder.Description, _recycleBin))
                        {
                            Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                            return $"🗑️ Reminder '{reminder.Description}' has been deleted. (Moved to Recycle Bin)";
                        }
                    }
                    else
                    {
                        return $"❌ Reminder #{numberFromWords} not found. You have {activeReminders.Count} active reminders.";
                    }
                }

                reminderToClear = reminderToClear.Trim().TrimEnd('.', '!', '?');

                if (string.IsNullOrEmpty(reminderToClear) || reminderToClear.Length < 2)
                {
                    return "📌 Please specify which reminder to delete. Example: 'delete reminder update password' or 'delete reminder 5'";
                }

                if (_taskManager.DeleteReminderByDescription(reminderToClear, _recycleBin))
                {
                    Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                    return $"🗑️ Reminder '{reminderToClear}' has been deleted. (Moved to Recycle Bin)";
                }

                var allReminders = _taskManager.GetActiveReminders();
                var matchingReminders = allReminders.Where(r => r.Description.Contains(reminderToClear, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matchingReminders.Count == 0)
                {
                    return $"❌ Reminder '{reminderToClear}' not found. Check your reminders with 'summary' or 'reminders'.";
                }
                else if (matchingReminders.Count == 1)
                {
                    var reminder = matchingReminders.First();
                    if (_taskManager.DeleteReminderByDescription(reminder.Description, _recycleBin))
                    {
                        Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                        return $"🗑️ Reminder '{reminder.Description}' has been deleted. (Moved to Recycle Bin)";
                    }
                }
                else
                {
                    var result = new List<string>();
                    result.Add($"⚠️ Multiple reminders found matching '{reminderToClear}':");
                    int counter = 1;
                    foreach (var r in matchingReminders)
                    {
                        string dateInfo = r.ReminderDate.HasValue ? $" (Due: {r.ReminderDate.Value:dd MMM yyyy})" : "";
                        result.Add($"  [{counter}] {r.Description}{dateInfo}");
                        counter++;
                    }
                    result.Add($"\n💡 Use 'delete reminder [number]' to delete a specific one.");
                    return string.Join("\n", result);
                }
            }

            // ================================================================
            // DELETE / REMOVE / CLEAR / ERASE COMPLETED
            // ================================================================
            bool isDeleteCompleted = lowerInput.StartsWith("delete completed") || lowerInput.StartsWith("remove completed") ||
                                     lowerInput.StartsWith("clear completed") || lowerInput.StartsWith("erase completed");

            if (isDeleteCompleted)
            {
                string completedToClear = input.Substring(input.IndexOf(' ') + 1).Trim();
                if (completedToClear.ToLowerInvariant().StartsWith("completed "))
                    completedToClear = completedToClear.Substring(10).Trim();

                if (completedToClear.ToLowerInvariant() == "all" || completedToClear.ToLowerInvariant() == "all completed" ||
                    completedToClear.ToLowerInvariant() == "every" || completedToClear.ToLowerInvariant() == "every completed")
                {
                    int count = _taskManager.DeleteAllCompleted(_recycleBin);
                    Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                    return $"🗑️ All completed tasks have been cleared. ({count} items moved to Recycle Bin)";
                }

                int numberFromWords = ExtractTaskNumber(completedToClear);
                if (numberFromWords > 0)
                {
                    var completedTasks = _taskManager.GetCompletedTasks();
                    if (completedTasks.Count == 0)
                        return "❌ No completed tasks to delete.";

                    if (numberFromWords <= completedTasks.Count)
                    {
                        var task = completedTasks[numberFromWords - 1];
                        if (_taskManager.DeleteCompletedTaskByDescription(task.Description))
                        {
                            Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                            return $"🗑️ Completed task '{task.Description}' has been deleted. (Removed from database)";
                        }
                    }
                    else
                    {
                        return $"❌ Completed task #{numberFromWords} not found. You have {completedTasks.Count} completed tasks.";
                    }
                }

                completedToClear = completedToClear.Trim().TrimEnd('.', '!', '?');

                if (string.IsNullOrEmpty(completedToClear) || completedToClear.Length < 2)
                {
                    return "📌 Please specify which completed task to delete. Example: 'delete completed update password' or 'delete completed 5'";
                }

                if (_taskManager.DeleteCompletedTaskByDescription(completedToClear))
                {
                    Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                    return $"🗑️ Completed task '{completedToClear}' has been deleted. (Removed from database)";
                }

                var allCompleted = _taskManager.GetCompletedTasks();
                var matchingCompleted = allCompleted.Where(t => t.Description.Contains(completedToClear, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matchingCompleted.Count == 0)
                {
                    return $"❌ Completed task '{completedToClear}' not found. Check your completed tasks with 'summary'.";
                }
                else if (matchingCompleted.Count == 1)
                {
                    var task = matchingCompleted.First();
                    if (_taskManager.DeleteCompletedTaskByDescription(task.Description))
                    {
                        Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                        return $"🗑️ Completed task '{task.Description}' has been deleted. (Removed from database)";
                    }
                }
                else
                {
                    var result = new List<string>();
                    result.Add($"⚠️ Multiple completed tasks found matching '{completedToClear}':");
                    int counter = 1;
                    foreach (var t in matchingCompleted)
                    {
                        result.Add($"  [{counter}] {t.Description}");
                        counter++;
                    }
                    result.Add($"\n💡 Use 'delete completed [number]' to delete a specific one.");
                    return string.Join("\n", result);
                }
            }

            // ================================================================
            // DELETE ALL TASKS (without description)
            // ================================================================
            if (lowerInput.Contains("delete all tasks") || lowerInput.Contains("clear all tasks") ||
                lowerInput.Contains("remove all tasks") || lowerInput.Contains("erase all tasks") ||
                lowerInput.Contains("delete every task") || lowerInput.Contains("clear every task") ||
                lowerInput.Contains("remove every task") || lowerInput.Contains("erase every task") ||
                lowerInput == "delete tasks" || lowerInput == "clear tasks" ||
                lowerInput == "remove tasks" || lowerInput == "erase tasks" ||
                lowerInput == "delete task all" || lowerInput == "clear task all")
            {
                int count = _taskManager.DeleteAllTasks(_recycleBin);
                Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                return $"🗑️ All tasks have been cleared. ({count} tasks moved to Recycle Bin)";
            }

            // ================================================================
            // DELETE ALL REMINDERS (without description)
            // ================================================================
            if (lowerInput.Contains("delete all reminders") || lowerInput.Contains("clear all reminders") ||
                lowerInput.Contains("remove all reminders") || lowerInput.Contains("erase all reminders") ||
                lowerInput.Contains("delete every reminder") || lowerInput.Contains("clear every reminder") ||
                lowerInput.Contains("remove every reminder") || lowerInput.Contains("erase every reminder") ||
                lowerInput == "delete reminders" || lowerInput == "clear reminders" ||
                lowerInput == "remove reminders" || lowerInput == "erase reminders" ||
                lowerInput == "delete reminder all" || lowerInput == "clear reminder all")
            {
                int count = _taskManager.DeleteAllReminders(_recycleBin);
                Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                return $"🗑️ All reminders have been cleared. ({count} reminders moved to Recycle Bin)";
            }

            // ================================================================
            // DELETE ALL COMPLETED (without description)
            // ================================================================
            if (lowerInput.Contains("delete all completed") || lowerInput.Contains("clear all completed") ||
                lowerInput.Contains("remove all completed") || lowerInput.Contains("erase all completed") ||
                lowerInput.Contains("delete every completed") || lowerInput.Contains("clear every completed") ||
                lowerInput.Contains("remove every completed") || lowerInput.Contains("erase every completed") ||
                lowerInput.Contains("delete completed tasks") || lowerInput.Contains("clear completed tasks") ||
                lowerInput.Contains("remove completed tasks") || lowerInput.Contains("erase completed tasks") ||
                lowerInput == "delete completed" || lowerInput == "clear completed" ||
                lowerInput == "remove completed" || lowerInput == "erase completed" ||
                lowerInput == "delete all completed tasks" || lowerInput == "clear all completed tasks")
            {
                int count = _taskManager.DeleteAllCompleted(_recycleBin);
                Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                return $"🗑️ All completed tasks have been cleared. ({count} items moved to Recycle Bin)";
            }

            // ================================================================
            // DELETE ALL / CLEAR EVERYTHING - WITH RECYCLE BIN SUPPORT
            // ================================================================
            if (lowerInput == "delete all" || lowerInput == "clear all" ||
                lowerInput == "remove all" || lowerInput == "erase all" ||
                lowerInput == "delete everything" || lowerInput == "clear everything" ||
                lowerInput == "remove everything" || lowerInput == "erase everything" ||
                lowerInput == "delete anything" || lowerInput == "remove anything")
            {
                // 1. Move ALL tasks to recycle bin
                int taskCount = _taskManager.DeleteAllTasks(_recycleBin);

                // 2. Move ALL reminders to recycle bin (UPDATED to use recycle bin)
                int reminderCount = _taskManager.DeleteAllReminders(_recycleBin);

                // 3. Move ALL completed tasks to recycle bin (UPDATED to use recycle bin)
                int completedCount = _taskManager.DeleteAllCompleted(_recycleBin);

                // 4. Get all recycle bin items count BEFORE emptying
                int binCount = _recycleBin.Count;

                // 5. EMPTY THE RECYCLE BIN completely
                int deletedFromBin = _recycleBin.EmptyBin();

                // 6. Clear conversation messages
                int msgCount = _conversationContext.Messages.Count;
                _conversationContext.Reset();

                // 7. Clear UI messages
                Dispatcher.Invoke(() =>
                {
                    for (int i = MessagesPanel.Children.Count - 1; i >= 0; i--)
                    {
                        if (MessagesPanel.Children[i] is Border border && border.Tag as string != "RecycleBinContainer")
                        {
                            MessagesPanel.Children.RemoveAt(i);
                        }
                    }
                });

                // 8. Update the sidebar and task summary
                Dispatcher.BeginInvoke(new Action(() => RefreshTaskSummary()));
                UpdateFavoritesSidebar();
                UpdateRecycleBinCounter();

                return $"🗑️ ALL CLEARED!\n\n" +
                       $"• {taskCount} tasks deleted\n" +
                       $"• {reminderCount} reminders deleted\n" +
                       $"• {completedCount} completed items deleted\n" +
                       $"• {deletedFromBin} items purged from recycle bin\n" +
                       $"• {msgCount} conversations cleared\n\n" +
                       $"✅ Everything has been permanently deleted!";
            }

            // ================================================================
            // DELETE / REMOVE / CLEAR / ERASE TASK (with no specific description)
            // ================================================================
            if (lowerInput == "delete task" || lowerInput == "remove task" ||
                lowerInput == "clear task" || lowerInput == "erase task" ||
                lowerInput == "delete tasks" || lowerInput == "clear tasks" ||
                lowerInput == "remove tasks" || lowerInput == "erase tasks" ||
                lowerInput == "delete a task" || lowerInput == "clear a task" ||
                lowerInput == "remove a task" || lowerInput == "erase a task")
            {
                _waitingForTaskName = true;
                return "📌 Please specify which task to delete. Example: 'delete task update password' or 'delete task 5'";
            }

            // ================================================================
            // DELETE / REMOVE / CLEAR / ERASE REMINDER (with no specific description)
            // ================================================================
            if (lowerInput == "delete reminder" || lowerInput == "remove reminder" ||
                lowerInput == "clear reminder" || lowerInput == "erase reminder" ||
                lowerInput == "delete reminders" || lowerInput == "clear reminders" ||
                lowerInput == "remove reminders" || lowerInput == "erase reminders" ||
                lowerInput == "delete a reminder" || lowerInput == "clear a reminder" ||
                lowerInput == "remove a reminder" || lowerInput == "erase a reminder")
            {
                return "📌 Please specify which reminder to delete. Example: 'delete reminder update password' or 'delete reminder 5'";
            }

            // ================================================================
            // DELETE / REMOVE / CLEAR / ERASE COMPLETED (with no specific description)
            // ================================================================
            if (lowerInput == "delete completed" || lowerInput == "remove completed" ||
                lowerInput == "clear completed" || lowerInput == "erase completed" ||
                lowerInput == "delete done" || lowerInput == "clear done" ||
                lowerInput == "delete a completed" || lowerInput == "clear a completed")
            {
                return "📌 Please specify which completed task to delete. Example: 'delete completed update password' or 'delete completed 5'";
            }

            // ================================================================
            // COMPLETE TASK / REMINDER
            // ================================================================
            if (lowerInput.Contains("complete task") || lowerInput.Contains("task complete") ||
                lowerInput.Contains("mark task as complete") || lowerInput.Contains("finish task") ||
                lowerInput.Contains("done task") || lowerInput.Contains("complete the") ||
                lowerInput.Contains("complete ") || lowerInput.Contains("done "))
            {
                bool isReminder = lowerInput.Contains("reminder") || lowerInput.Contains("remind");
                string taskToComplete = input;

                int numberFromWords = ExtractTaskNumber(taskToComplete);

                if (numberFromWords > 0)
                {
                    if (isReminder)
                    {
                        var activeReminders = _taskManager.GetActiveReminders();
                        if (activeReminders.Count == 0)
                            return "❌ No active reminders to complete.";

                        if (numberFromWords <= activeReminders.Count)
                        {
                            var reminder = activeReminders[numberFromWords - 1];
                            if (_taskManager.CompleteReminder(reminder.Description))
                            {
                                return $"✅ Reminder '{reminder.Description}' is marked as complete. (Updated in database)";
                            }
                        }
                        else
                        {
                            return $"❌ Reminder #{numberFromWords} not found. You have {activeReminders.Count} active reminders.";
                        }
                    }
                    else
                    {
                        var activeTasks = _taskManager.GetActiveTasks();
                        if (activeTasks.Count == 0)
                            return "❌ No active tasks to complete.";

                        if (numberFromWords <= activeTasks.Count)
                        {
                            var task = activeTasks[numberFromWords - 1];
                            if (_taskManager.CompleteTask(task.Description))
                            {
                                return $"✅ Task '{task.Description}' is marked as complete. (Updated in database)";
                            }
                        }
                        else
                        {
                            return $"❌ Task #{numberFromWords} not found. You have {activeTasks.Count} active tasks.";
                        }
                    }
                }

                string[] removePhrases = { "complete task", "task complete", "mark task as complete", "finish task",
                "done task", "complete the", "complete ", "done " };

                string taskName = input;
                foreach (var phrase in removePhrases)
                {
                    if (lowerInput.Contains(phrase.ToLowerInvariant()))
                    {
                        taskName = input.Replace(phrase, "", StringComparison.OrdinalIgnoreCase).Trim();
                        break;
                    }
                }

                taskName = taskName.Trim().TrimEnd('.', '!', '?');

                if (string.IsNullOrEmpty(taskName))
                    return "Please specify which task to complete. Example: 'Complete task 5' or 'Complete task update password'";

                if (isReminder)
                {
                    if (_taskManager.CompleteReminder(taskName))
                    {
                        return $"✅ Reminder '{taskName}' is marked as complete. (Updated in database)";
                    }
                    return $"❌ Reminder '{taskName}' not found. Check your reminders with 'summary' or 'reminders'.";
                }
                else
                {
                    if (_taskManager.CompleteTask(taskName))
                    {
                        return $"✅ Task '{taskName}' is marked as complete. (Updated in database)";
                    }
                    return $"❌ Task '{taskName}' not found. Check your tasks with 'summary' or 'tasks'.";
                }
            }

            // ================================================================
            // REMINDER FALLBACK
            // ================================================================
            if (cleanedLower == "remind me" || cleanedLower == "remind" || cleanedLower == "reminder")
            {
                _waitingForReminderTopic = true;
                return "What would you like to be reminded about?";
            }

            // ================================================================
            // SUMMARY AND VIEW COMMANDS
            // ================================================================
            if (lowerInput.Contains("summary") || lowerInput.Contains("show summary"))
            {
                return "SUMMARY OF RECENT ACTIONS\n\n" + _taskManager.GetSummary();
            }

            if (lowerInput.Contains("show reminders") || lowerInput.Contains("list reminders") ||
                lowerInput.Contains("my reminders"))
                return "⏰ REMINDERS:\n" + _taskManager.GetRemindersOnly();

            if (lowerInput.Contains("show tasks") || lowerInput.Contains("list tasks") ||
                lowerInput.Contains("my tasks"))
                return "📌 TASKS:\n" + _taskManager.GetTasksOnly();

            return null!;
        }
        private DateTime? ParseReminderDate(string input, out string detectedPhrase)
        {
            detectedPhrase = string.Empty;
            if (string.IsNullOrEmpty(input)) return null;

            string lowerInput = input.ToLowerInvariant();

            // ================================================================
            // FIRST: Check for "in X days" patterns (MOST IMPORTANT)
            // ================================================================

            // Handle "in 3 days", "in 5 days time", "in 2 weeks", "in 1 month", "in 1 year"
            var inTimePattern = System.Text.RegularExpressions.Regex.Match(input,
                @"in\s+(\d+)\s+(day|days|week|weeks|month|months|year|years)(?:\s+time)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (inTimePattern.Success)
            {
                int amount = int.Parse(inTimePattern.Groups[1].Value);
                string unit = inTimePattern.Groups[2].Value.ToLowerInvariant();

                DateTime resultDate = DateTime.Now;
                string unitDisplay = "";

                switch (unit)
                {
                    case "day":
                    case "days":
                        resultDate = DateTime.Now.AddDays(amount);
                        unitDisplay = amount == 1 ? "day" : "days";
                        break;
                    case "week":
                    case "weeks":
                        resultDate = DateTime.Now.AddDays(amount * 7);
                        unitDisplay = amount == 1 ? "week" : "weeks";
                        break;
                    case "month":
                    case "months":
                        resultDate = DateTime.Now.AddMonths(amount);
                        unitDisplay = amount == 1 ? "month" : "months";
                        break;
                    case "year":
                    case "years":
                        resultDate = DateTime.Now.AddYears(amount);
                        unitDisplay = amount == 1 ? "year" : "years";
                        break;
                }
                detectedPhrase = $"in {amount} {unitDisplay}";
                return resultDate;
            }

            // Handle "in a day", "in a week", "in a month", "in a year"
            var inAPattern = System.Text.RegularExpressions.Regex.Match(input,
                @"in\s+a\s+(day|week|month|year)(?:\s+time|'\s*time)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (inAPattern.Success)
            {
                string unit = inAPattern.Groups[1].Value.ToLowerInvariant();
                DateTime resultDate = DateTime.Now;
                string unitDisplay = "";

                switch (unit)
                {
                    case "day":
                        resultDate = DateTime.Now.AddDays(1);
                        unitDisplay = "day";
                        break;
                    case "week":
                        resultDate = DateTime.Now.AddDays(7);
                        unitDisplay = "week";
                        break;
                    case "month":
                        resultDate = DateTime.Now.AddMonths(1);
                        unitDisplay = "month";
                        break;
                    case "year":
                        resultDate = DateTime.Now.AddYears(1);
                        unitDisplay = "year";
                        break;
                }
                detectedPhrase = $"in a {unitDisplay}";
                return resultDate;
            }

            // Handle "a day from now", "a week from now", etc.
            var aFromNowPattern = System.Text.RegularExpressions.Regex.Match(input,
                @"a\s+(day|week|month|year)\s+from\s+now",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (aFromNowPattern.Success)
            {
                string unit = aFromNowPattern.Groups[1].Value.ToLowerInvariant();
                DateTime resultDate = DateTime.Now;
                string unitDisplay = "";

                switch (unit)
                {
                    case "day":
                        resultDate = DateTime.Now.AddDays(1);
                        unitDisplay = "day";
                        break;
                    case "week":
                        resultDate = DateTime.Now.AddDays(7);
                        unitDisplay = "week";
                        break;
                    case "month":
                        resultDate = DateTime.Now.AddMonths(1);
                        unitDisplay = "month";
                        break;
                    case "year":
                        resultDate = DateTime.Now.AddYears(1);
                        unitDisplay = "year";
                        break;
                }
                detectedPhrase = $"in a {unitDisplay}";
                return resultDate;
            }

            // Handle "3 days time", "5 days time" (without "in")
            var timePattern = System.Text.RegularExpressions.Regex.Match(input,
                @"(\d+)\s+(day|days|week|weeks|month|months|year|years)\s+time",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (timePattern.Success)
            {
                int amount = int.Parse(timePattern.Groups[1].Value);
                string unit = timePattern.Groups[2].Value.ToLowerInvariant();

                DateTime resultDate = DateTime.Now;
                string unitDisplay = "";

                switch (unit)
                {
                    case "day":
                    case "days":
                        resultDate = DateTime.Now.AddDays(amount);
                        unitDisplay = amount == 1 ? "day" : "days";
                        break;
                    case "week":
                    case "weeks":
                        resultDate = DateTime.Now.AddDays(amount * 7);
                        unitDisplay = amount == 1 ? "week" : "weeks";
                        break;
                    case "month":
                    case "months":
                        resultDate = DateTime.Now.AddMonths(amount);
                        unitDisplay = amount == 1 ? "month" : "months";
                        break;
                    case "year":
                    case "years":
                        resultDate = DateTime.Now.AddYears(amount);
                        unitDisplay = amount == 1 ? "year" : "years";
                        break;
                }
                detectedPhrase = $"in {amount} {unitDisplay}";
                return resultDate;
            }

            // ================================================================
            // SECOND: Check for "tomorrow", "today", "next week"
            // ================================================================

            if (lowerInput.Contains("tomorrow"))
            {
                detectedPhrase = "tomorrow";
                return DateTime.Now.Date.AddDays(1);
            }

            if (lowerInput.Contains("today"))
            {
                detectedPhrase = "today";
                return DateTime.Now.Date;
            }

            if (lowerInput.Contains("next week"))
            {
                detectedPhrase = "next week";
                return DateTime.Now.Date.AddDays(7);
            }

            // ================================================================
            // THIRD: Check for day names (monday, tuesday, etc.)
            // ================================================================

            string[] dayNames = { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
            DayOfWeek[] dayOfWeeks = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            for (int i = 0; i < dayNames.Length; i++)
            {
                if (lowerInput.Contains(dayNames[i]))
                {
                    detectedPhrase = dayNames[i];
                    int days = ((int)dayOfWeeks[i] - (int)DateTime.Now.DayOfWeek + 7) % 7;
                    return DateTime.Now.Date.AddDays(days == 0 ? 7 : days);
                }
            }

            // Handle "monday's date" pattern
            var dayDatePattern = System.Text.RegularExpressions.Regex.Match(input,
                @"(monday|tuesday|wednesday|thursday|friday|saturday|sunday)['']s\s+date",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (dayDatePattern.Success)
            {
                string dayName = dayDatePattern.Groups[1].Value.ToLowerInvariant();
                detectedPhrase = dayName;
                DayOfWeek targetDay;
                switch (dayName)
                {
                    case "monday": targetDay = DayOfWeek.Monday; break;
                    case "tuesday": targetDay = DayOfWeek.Tuesday; break;
                    case "wednesday": targetDay = DayOfWeek.Wednesday; break;
                    case "thursday": targetDay = DayOfWeek.Thursday; break;
                    case "friday": targetDay = DayOfWeek.Friday; break;
                    case "saturday": targetDay = DayOfWeek.Saturday; break;
                    case "sunday": targetDay = DayOfWeek.Sunday; break;
                    default: return null;
                }
                int days = ((int)targetDay - (int)DateTime.Now.DayOfWeek + 7) % 7;
                return DateTime.Now.Date.AddDays(days == 0 ? 7 : days);
            }

            // ================================================================
            // FOURTH: Check for "on the 9th", "for the 5th" with "the"
            // ================================================================

            var ordinalDatePattern = System.Text.RegularExpressions.Regex.Match(input,
                @"(?:on|for|due)\s+the\s+(\d{1,2})(?:st|nd|rd|th)?(?:\s+of\s+)?(\w+)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (ordinalDatePattern.Success)
            {
                int day = int.Parse(ordinalDatePattern.Groups[1].Value);
                string month = ordinalDatePattern.Groups[2].Value;

                if (!string.IsNullOrEmpty(month))
                {
                    var monthNumber = GetMonthNumber(month);
                    if (monthNumber.HasValue)
                    {
                        int year = DateTime.Now.Year;
                        try
                        {
                            var date = new DateTime(year, monthNumber.Value, day);
                            if (date < DateTime.Now.Date) date = date.AddYears(1);
                            detectedPhrase = $"{day}{GetOrdinalSuffix(day)} {date.ToString("MMMM")}";
                            return date;
                        }
                        catch
                        {
                            int nextMonth = monthNumber.Value + 1;
                            int nextYear = year;
                            if (nextMonth > 12) { nextMonth = 1; nextYear += 1; }
                            try
                            {
                                var date = new DateTime(nextYear, nextMonth, Math.Min(day, 28));
                                detectedPhrase = $"{Math.Min(day, 28)}{GetOrdinalSuffix(Math.Min(day, 28))} {date.ToString("MMMM")}";
                                return date;
                            }
                            catch { return null; }
                        }
                    }
                }

                var now = DateTime.Now;
                try
                {
                    var targetDate = new DateTime(now.Year, now.Month, day);
                    if (targetDate >= now.Date)
                    {
                        detectedPhrase = $"{day}{GetOrdinalSuffix(day)}";
                        return targetDate;
                    }
                }
                catch { }

                int nextMonthNum = now.Month + 1;
                int nextYearNum = now.Year;
                if (nextMonthNum > 12) { nextMonthNum = 1; nextYearNum += 1; }

                try
                {
                    var targetDate = new DateTime(nextYearNum, nextMonthNum, day);
                    detectedPhrase = $"{day}{GetOrdinalSuffix(day)}";
                    return targetDate;
                }
                catch
                {
                    try
                    {
                        int monthAfter = nextMonthNum + 1;
                        int yearAfter = nextYearNum;
                        if (monthAfter > 12) { monthAfter = 1; yearAfter += 1; }
                        var targetDate = new DateTime(yearAfter, monthAfter, Math.Min(day, 28));
                        detectedPhrase = $"{Math.Min(day, 28)}{GetOrdinalSuffix(Math.Min(day, 28))}";
                        return targetDate;
                    }
                    catch { return null; }
                }
            }

            // ================================================================
            // FIFTH: Check for "for [date]" pattern
            // ================================================================

            var forPattern = System.Text.RegularExpressions.Regex.Match(input,
                @"for\s+(.+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (forPattern.Success)
            {
                string datePart = forPattern.Groups[1].Value.Trim();
                var tempDate = ParseDateOnly(datePart, ref detectedPhrase);
                if (tempDate.HasValue)
                    return tempDate;
            }

            // ================================================================
            // SIXTH: Check for "on [date]" pattern
            // ================================================================

            var onPattern = System.Text.RegularExpressions.Regex.Match(input,
                @"on\s+(.+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (onPattern.Success)
            {
                string datePart = onPattern.Groups[1].Value.Trim();
                var tempDate = ParseDateOnly(datePart, ref detectedPhrase);
                if (tempDate.HasValue)
                    return tempDate;
            }

            // ================================================================
            // SEVENTH: Check for "due [date]" pattern
            // ================================================================

            var duePattern = System.Text.RegularExpressions.Regex.Match(input,
                @"due\s+(?:on|for)?\s+(.+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (duePattern.Success)
            {
                string datePart = duePattern.Groups[1].Value.Trim();
                var tempDate = ParseDateOnly(datePart, ref detectedPhrase);
                if (tempDate.HasValue)
                    return tempDate;
            }

            // ================================================================
            // EIGHTH: Handle "8 aug" / "5 july"
            // ================================================================

            var simpleDatePattern = System.Text.RegularExpressions.Regex.Match(input,
                @"(\d{1,2})(?:st|nd|rd|th)?\s+(january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (simpleDatePattern.Success)
            {
                int day = int.Parse(simpleDatePattern.Groups[1].Value);
                string month = simpleDatePattern.Groups[2].Value.ToLowerInvariant();
                var monthNumber = GetMonthNumber(month);
                if (monthNumber.HasValue)
                {
                    int year = DateTime.Now.Year;
                    try
                    {
                        var date = new DateTime(year, monthNumber.Value, day);
                        if (date < DateTime.Now.Date) date = date.AddYears(1);
                        detectedPhrase = $"{day}{GetOrdinalSuffix(day)} {date.ToString("MMMM")}";
                        return date;
                    }
                    catch
                    {
                        try
                        {
                            int nextMonth = monthNumber.Value + 1;
                            int nextYear = year;
                            if (nextMonth > 12) { nextMonth = 1; nextYear += 1; }
                            var date = new DateTime(nextYear, nextMonth, Math.Min(day, 28));
                            detectedPhrase = $"{Math.Min(day, 28)}{GetOrdinalSuffix(Math.Min(day, 28))} {date.ToString("MMMM")}";
                            return date;
                        }
                        catch { return null; }
                    }
                }
            }

            // ================================================================
            // NINTH: Handle "5th of august"
            // ================================================================

            var dateWithOrdinalPattern = System.Text.RegularExpressions.Regex.Match(input,
                @"(\d{1,2})(?:st|nd|rd|th)?\s+of\s+(january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (dateWithOrdinalPattern.Success)
            {
                int day = int.Parse(dateWithOrdinalPattern.Groups[1].Value);
                string month = dateWithOrdinalPattern.Groups[2].Value.ToLowerInvariant();
                var monthNumber = GetMonthNumber(month);
                if (monthNumber.HasValue)
                {
                    int year = DateTime.Now.Year;
                    try
                    {
                        var date = new DateTime(year, monthNumber.Value, day);
                        if (date < DateTime.Now.Date) date = date.AddYears(1);
                        detectedPhrase = $"{day}{GetOrdinalSuffix(day)} {date.ToString("MMMM")}";
                        return date;
                    }
                    catch
                    {
                        try
                        {
                            int nextMonth = monthNumber.Value + 1;
                            int nextYear = year;
                            if (nextMonth > 12) { nextMonth = 1; nextYear += 1; }
                            var date = new DateTime(nextYear, nextMonth, Math.Min(day, 28));
                            detectedPhrase = $"{Math.Min(day, 28)}{GetOrdinalSuffix(Math.Min(day, 28))} {date.ToString("MMMM")}";
                            return date;
                        }
                        catch { return null; }
                    }
                }
            }

            return null;
        }

   
      private DateTime? ParseDateOnly(string datePart, ref string detectedPhrase)
        {
            // Try to parse as "8 aug" or "5 july"
            var simpleDatePattern = System.Text.RegularExpressions.Regex.Match(datePart,
                @"(\d{1,2})(?:st|nd|rd|th)?\s+(january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (simpleDatePattern.Success)
            {
                int day = int.Parse(simpleDatePattern.Groups[1].Value);
                string month = simpleDatePattern.Groups[2].Value.ToLowerInvariant();
                var monthNumber = GetMonthNumber(month);
                if (monthNumber.HasValue)
                {
                    int year = DateTime.Now.Year;
                    try
                    {
                        var date = new DateTime(year, monthNumber.Value, day);
                        if (date < DateTime.Now.Date) date = date.AddYears(1);
                        detectedPhrase = $"{day}{GetOrdinalSuffix(day)} {date.ToString("MMMM")}";
                        return date;
                    }
                    catch { }
                }
            }

            // Try to parse as "the 9th" or "5th"
            var ordinalPattern = System.Text.RegularExpressions.Regex.Match(datePart,
                @"(?:the\s+)?(\d{1,2})(?:st|nd|rd|th)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (ordinalPattern.Success)
            {
                int day = int.Parse(ordinalPattern.Groups[1].Value);
                var now = DateTime.Now;
                try
                {
                    var targetDate = new DateTime(now.Year, now.Month, day);
                    if (targetDate >= now.Date)
                    {
                        detectedPhrase = $"{day}{GetOrdinalSuffix(day)}";
                        return targetDate;
                    }
                }
                catch { }

                int nextMonth = now.Month + 1;
                int nextYear = now.Year;
                if (nextMonth > 12) { nextMonth = 1; nextYear += 1; }
                try
                {
                    var targetDate = new DateTime(nextYear, nextMonth, day);
                    detectedPhrase = $"{day}{GetOrdinalSuffix(day)}";
                    return targetDate;
                }
                catch { }
            }

            return null;
        }
        private int? GetMonthNumber(string month)
        {
            if (string.IsNullOrEmpty(month)) return null;

            var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {"january", 1}, {"jan", 1},
            {"february", 2}, {"feb", 2},
            {"march", 3}, {"mar", 3},
            {"april", 4}, {"apr", 4},
            {"may", 5},
            {"june", 6}, {"jun", 6},
            {"july", 7}, {"jul", 7},
            {"august", 8}, {"aug", 8},
            {"september", 9}, {"sep", 9},
            {"october", 10}, {"oct", 10},
            {"november", 11}, {"nov", 11},
            {"december", 12}, {"dec", 12}
        };

            return monthMap.TryGetValue(month, out int value) ? value : (int?)null;
        }

        // ================= SEND MESSAGE =================

        private async void SendMessage()
        {
            if (!_isLoggedIn) return;

            if (_isTaskFlowActive || _isQuizMode)
            {
                InputBox.Clear();
                return;
            }

            string text = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            InputBox.Clear();
            PlaceholderText.Visibility = Visibility.Visible;
            _conversationContext.Messages.Add((_loggedInUsername ?? "Guest", text));
            AddUserMessage(text);

            ShowTyping(true);
            BtnSend.IsEnabled = false;
            InputBox.IsEnabled = false;

            string lowerText = text.ToLowerInvariant().Trim();

            // Check for quiz-related text commands
            if (lowerText == "game" ||
                lowerText == "quiz" ||
                lowerText == "quiz game" ||
                lowerText == "questions" ||
                lowerText == "test me" ||
                lowerText == "test" ||
                lowerText == "take quiz" ||
                lowerText == "start quiz" ||
                lowerText == "play quiz" ||
                lowerText == "i want to take a quiz" ||
                lowerText == "let's do a quiz" ||
                lowerText == "quiz me" ||
                lowerText.Contains("start the quiz") ||
                lowerText.Contains("take the quiz") ||
                lowerText.Contains("play the quiz"))
            {
                await Task.Delay(300);
                ShowTyping(false);
                BtnSend.IsEnabled = true;
                InputBox.IsEnabled = true;
                InputBox.Focus();

                _activityLog.Log("Quiz Started", "Via text command");
                StartQuiz();
                UpdateFavoritesSidebar();
                return;
            }

            // Check for activity log commands
            if (lowerText.Contains("show activity log") ||
                lowerText.Contains("what have you done for me") ||
                lowerText.Contains("show log") ||
                lowerText.Contains("activity log") ||
                lowerText.Contains("show full log") ||
                lowerText.Contains("full activity log"))
            {
                bool showFull = lowerText.Contains("full") || lowerText.Contains("all");
                string logResponse = showFull ? _activityLog.GetFullLog() : _activityLog.GetSummary(10);
                await Task.Delay(300);
                ShowTyping(false);
                AddBotMessage($"📜 ACTIVITY LOG\n\n{logResponse}");
                BtnSend.IsEnabled = true;
                InputBox.IsEnabled = true;
                InputBox.Focus();
                UpdateFavoritesSidebar();
                return;
            }

            // Check for clear log command
            if (lowerText.Contains("clear log") || lowerText.Contains("clear activity log"))
            {
                _activityLog.Clear();
                await Task.Delay(300);
                ShowTyping(false);
                AddBotMessage("🧹 Activity log has been cleared.");
                BtnSend.IsEnabled = true;
                InputBox.IsEnabled = true;
                InputBox.Focus();
                UpdateFavoritesSidebar();
                return;
            }

            // Task / Reminder handling
            string taskResponse = HandleReminderRequest(text);

            if (taskResponse != null)
            {
                await Task.Delay(500);
                ShowTyping(false);
                BtnSend.IsEnabled = true;
                InputBox.IsEnabled = true;
                InputBox.Focus();

                if (!string.IsNullOrEmpty(taskResponse))
                {
                    AddBotMessage(taskResponse);
                }

                UpdateFavoritesSidebar();
                return;
            }

            // Recycle bin select commands
            if (lowerText.StartsWith("select "))
            {
                var idPart = lowerText.Substring(8).Trim();
                var item = _recycleBin.GetItems().FirstOrDefault(i => i.UniqueId.StartsWith(idPart, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    _recycleBin.ToggleSelection(item.UniqueId);
                    string status = item.IsSelected ? "selected" : "deselected";
                    AddBotMessage($"✅ Toggled selection for: {item.Content} ({status})");
                    ShowRecycleBinInChat();
                }
                else
                {
                    AddBotMessage($"❌ Item with ID '{idPart}' not found.");
                }
                UpdateFavoritesSidebar();
                return;
            }

            if (lowerText == "select all")
            {
                if (_recycleBin.Count == 0)
                {
                    AddBotMessage("♻️ Recycle bin is empty.");
                    return;
                }
                _recycleBin.SelectAll();
                AddBotMessage($"✅ Selected all {_recycleBin.Count} items.");
                ShowRecycleBinInChat();
                UpdateFavoritesSidebar();
                return;
            }

            if (lowerText == "deselect all")
            {
                if (_recycleBin.Count == 0)
                {
                    AddBotMessage("♻️ Recycle bin is empty.");
                    return;
                }
                _recycleBin.DeselectAll();
                AddBotMessage($"✅ Deselected all items.");
                ShowRecycleBinInChat();
                UpdateFavoritesSidebar();
                return;
            }

            if (lowerText == "restore selected")
            {
                var selected = _recycleBin.GetSelectedItems();
                if (selected.Count == 0)
                {
                    AddBotMessage("⚠️ No items selected. Use `/select [id]` or `/selectall` first.");
                    return;
                }
                int count = _recycleBin.RestoreSelected(RestoreItem);
                AddBotMessage($"✅ {count} selected items restored from recycle bin.");
                UpdateFavoritesSidebar();
                ShowRecycleBinInChat();
                return;
            }

            if (lowerText == "restore all")
            {
                if (_recycleBin.Count == 0)
                {
                    AddBotMessage("♻️ Recycle bin is empty. Nothing to restore.");
                    return;
                }
                int count = _recycleBin.RestoreAll(RestoreItem);
                AddBotMessage($"✅ All {count} items restored from recycle bin.");
                UpdateFavoritesSidebar();
                ShowRecycleBinInChat();
                return;
            }

            if (lowerText == "empty bin")
            {
                if (_recycleBin.Count == 0)
                {
                    AddBotMessage("♻️ Recycle bin is already empty.");
                    return;
                }
                var result = MessageBox.Show(
                    $"Are you sure you want to permanently delete all {_recycleBin.Count} items in the recycle bin?",
                    "Empty Recycle Bin",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    int count = _recycleBin.EmptyBin();
                    AddBotMessage($"🗑️ Recycle bin emptied. {count} items permanently deleted.");
                    UpdateFavoritesSidebar();
                    ShowRecycleBinInChat();
                }
                return;
            }

            // Recycle bin text commands
            if (lowerText.Contains("recycle bin") ||
                lowerText.Contains("show bin") ||
                lowerText.Contains("view bin") ||
                lowerText.Contains("open bin") ||
                lowerText == "bin" ||
                lowerText == "trash")
            {
                await Task.Delay(300);
                ShowTyping(false);
                BtnSend.IsEnabled = true;
                InputBox.IsEnabled = true;
                InputBox.Focus();

                ShowRecycleBinInChat();
                UpdateFavoritesSidebar();
                return;
            }

            // Activity log see more/less
            if (lowerText == "see more")
            {
                _showFullLog = true;
                ShowActivityLog();
                return;
            }

            if (lowerText == "see less")
            {
                _showFullLog = false;
                ShowActivityLog();
                return;
            }

            // Regular bot response
            string reply = await Task.Run(() => { string r = _responseGenerator.GenerateReply(text, _conversationContext); Thread.Sleep(500); return r; });
            ShowTyping(false);
            BtnSend.IsEnabled = true;
            InputBox.IsEnabled = true;
            InputBox.Focus();
            _conversationContext.Messages.Add(("BotBuddy", reply));
            AddBotMessage(reply);
            UpdateFavoritesSidebar();
        }

        private void AddUserMessage(string text) { Dispatcher.Invoke(() => { string displayName = _loggedInUsername ?? "Guest"; var bubble = BuildBubble(text, displayName, true); MessagesPanel.Children.Add(bubble); ScrollToBottom(); }); }
        private void AddBotMessage(string text) { Dispatcher.Invoke(() => { var bubble = BuildBubble(text, "BotBuddy", false); MessagesPanel.Children.Add(bubble); ScrollToBottom(); }); }

        private UIElement BuildBubble(string text, string sender, bool isUser)
        {
            var outer = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            var bubble = new Border
            {
                CornerRadius = new CornerRadius(isUser ? 16 : 16, isUser ? 4 : 16, 16, 16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 580,
                Background = isUser ? new SolidColorBrush(Color.FromRgb(0x4A, 0x00, 0x28)) : new SolidColorBrush(Color.FromRgb(0x1A, 0x10, 0x2A)),
                BorderBrush = isUser ? new SolidColorBrush(Color.FromRgb(0xFF, 0x14, 0x93)) : new SolidColorBrush(Color.FromRgb(0x60, 0x20, 0x80)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = isUser ? new Thickness(80, 0, 0, 0) : new Thickness(0, 0, 80, 0)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = sender, FontSize = 11, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Foreground = isUser ? new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0xB2)) : new SolidColorBrush(Color.FromRgb(0xCC, 0x44, 0xFF)), Margin = new Thickness(0, 0, 0, 4) });
            var messageText = new TextBlock { Text = text, FontSize = 13, FontFamily = new FontFamily("Consolas"), Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, LineHeight = 20 };
            sp.Children.Add(messageText);
            bubble.Child = sp;
            outer.Children.Add(bubble);
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            bubble.BeginAnimation(OpacityProperty, anim);
            return outer;
        }

        private void ShowTyping(bool show)
        {
            Dispatcher.Invoke(() =>
            {
                TypingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                if (show)
                {
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
                    int dots = 1;
                    timer.Tick += (s, e) =>
                    {
                        if (TypingIndicator.Visibility != Visibility.Visible) { timer.Stop(); return; }
                        dots = dots % 3 + 1;
                        TypingIndicator.Text = new string('*', dots);
                    };
                    timer.Start();
                }
            });
        }

        private void ScrollToBottom() { ChatScrollViewer.UpdateLayout(); ChatScrollViewer.ScrollToEnd(); }
    }
    public class AsciiArtBuilder
    {
        public void BuildCyberArt(TextBlock cyberBlock, TextBlock botBlock, TextBlock taglineBlock)
        {
            var cyberLines = GetCyberLines();
            var botLines = GetBotLines();

            // VIBRANT colors for solid black background
            Color[] redColors = {
            Color.FromRgb(0xFF, 0x22, 0x22),  // Bright red
            Color.FromRgb(0xFF, 0x44, 0x44),  // Lighter red
            Color.FromRgb(0xDD, 0x00, 0x00)   // Deep red
        };

            Color[] magentaColors = {
            Color.FromRgb(0xFF, 0x44, 0xFF),  // Bright magenta
            Color.FromRgb(0xFF, 0x66, 0xFF),  // Light magenta
            Color.FromRgb(0xCC, 0x00, 0xCC)   // Deep magenta
        };

            cyberBlock.Inlines.Clear();
            botBlock.Inlines.Clear();

            for (int i = 0; i < cyberLines.Length; i++)
            {
                var cyberRun = new Run(cyberLines[i] + "\n")
                {
                    Foreground = new SolidColorBrush(redColors[i % redColors.Length]),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    // Ensure text is solid
                    Background = null
                };
                cyberBlock.Inlines.Add(cyberRun);

                var botRun = new Run(botLines[i] + "\n")
                {
                    Foreground = new SolidColorBrush(magentaColors[i % magentaColors.Length]),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    // Ensure text is solid
                    Background = null
                };
                botBlock.Inlines.Add(botRun);
            }

            BuildTagline(taglineBlock);
        }

        private void BuildTagline(TextBlock taglineBlock)
        {
            string tagline = "Secure Today, Safe tomorrow";
           
            // VIBRANT shimmer colors
            Color[] shimmerColors = {
            Color.FromRgb(0xFF, 0x44, 0xFF),  // Bright magenta
            Color.FromRgb(0xFF, 0x33, 0x33),  // Bright red
            Color.FromRgb(0xFF, 0x88, 0xFF),  // Light magenta
            Color.FromRgb(0xFF, 0x66, 0x66)   // Light red
        };

            taglineBlock.Inlines.Clear();
            taglineBlock.Opacity = 1.0;  

            for (int i = 0; i < tagline.Length; i++)
            {
                var run = new Run(tagline[i].ToString())
                {
                    Foreground = new SolidColorBrush(shimmerColors[i % shimmerColors.Length]),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Background = null  
                };
                taglineBlock.Inlines.Add(run);
            }
        }

        private string[] GetCyberLines() => new string[]
        {
        " ██████╗██╗   ██╗██████╗ ███████╗██████╗ ███████╗███████╗ ██████╗██╗   ██╗██████╗ ██╗████████╗██╗   ██╗",
        "██╔════╝╚██╗ ██╔╝██╔══██╗██╔════╝██╔══██╗██╔════╝██╔════╝██╔════╝██║   ██║██╔══██╗██║╚══██╔══╝╚██╗ ██╔╝",
        "██║      ╚████╔╝ ██████╔╝█████╗  ██████╔╝███████╗█████╗  ██║     ██║   ██║██████╔╝██║   ██║    ╚████╔╝ ",
        "██║       ╚██╔╝  ██╔══██╗██╔══╝  ██╔══██╗╚════██║██╔══╝  ██║     ██║   ██║██╔══██╗██║   ██║     ╚██╔╝  ",
        "╚██████╗   ██║   ██████╔╝███████╗██║  ██║███████║███████╗╚██████╗╚██████╔╝██║  ██║██║   ██║      ██║   ",
        " ╚═════╝   ╚═╝   ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝ ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝      ╚═╝  "
        };

        private string[] GetBotLines() => new string[]
        {
        "██████╗  ██████╗ ████████╗██████╗ ██╗   ██╗██████╗ ██████╗ ██╗   ██╗",
        "██╔══██╗██╔═══██╗╚══██╔══╝██╔══██╗██║   ██║██╔══██╗██╔══██╗╚██╗ ██╔╝",
        "██████╔╝██║   ██║   ██║   ██████╔╝██║   ██║██║  ██║██║  ██║ ╚████╔╝ ",
        "██╔══██╗██║   ██║   ██║   ██╔══██╗██║   ██║██║  ██║██║  ██║  ╚██╔╝  ",
        "██████╔╝╚██████╔╝   ██║   ██████╔╝╚██████╔╝██████╔╝██████╔╝   ██║   ",
        "╚═════╝  ╚═════╝    ╚═╝   ╚═════╝  ╚═════╝ ╚═════╝ ╚═════╝    ╚═╝   "
        };
    }

    // ================= PASSWORD VALIDATOR CLASS =================
    public class PasswordValidator
    {
        private readonly string _password;
        public PasswordValidator(string password) { _password = password; }
        public string? Validate()
        {
            if (string.IsNullOrEmpty(_password)) return null;
            if (_password.Length > 4) return "Max 4 characters";
            var requirements = new Dictionary<string, int> { { "lowercase", _password.Count(char.IsLower) }, { "uppercase", _password.Count(char.IsUpper) }, { "number", _password.Count(char.IsDigit) }, { "symbol", _password.Count(ch => !char.IsLetterOrDigit(ch)) } };
            foreach (var req in requirements) if (req.Value > 1) return $"Only 1 {req.Key}";
            var missing = new List<string>();
            if (requirements["lowercase"] == 0) missing.Add("1 LOWER");
            if (requirements["uppercase"] == 0) missing.Add("1 UPPER");
            if (requirements["number"] == 0) missing.Add("1 NUMBER");
            if (requirements["symbol"] == 0) missing.Add("1 SYMBOL");
            if (missing.Count > 0) return "Missing: " + string.Join(", ", missing);
            return (string?)null;
        }
    }
}