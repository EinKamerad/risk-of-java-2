using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RiskOfCSharp2.Net
{
    public class SteamLobbyEntry
    {
        public ulong LobbyId;
        public string HostName = "Steam Игрок";
        public string GameType = "coop";
        public int MemberCount = 1;
        public int MaxMembers = 8;
        public bool IsFriendLobby = false;

        public bool IsFriend => IsFriendLobby;
        public string Mode => GameType;
    }

    /// <summary>
    /// Интеграция мультиплеера через Steamworks Flat C API на базе Spacewar (AppID 480):
    /// - Проверка наличия клиента Steam и установленного Spacewar (AppID 480) во всех библиотеках Steam;
    /// - Автоматический запуск скачивания/установки Spacewar через steam://install/480, если он не установлен;
    /// - Создание и поиск Steam-лобби, приглашения через Shift+Tab Overlay, вход по приглашению друга или ID лобби;
    /// - P2P-обмен сообщениями через Valve Steam Networking Relay (ISteamNetworkingMessages) без проброса портов и IP.
    /// </summary>
    public sealed class SteamSpacewarService
    {
        public const uint SpacewarAppId = 480;
        public const string LobbyAppTag = "RiskOfCSharp2_v1";

        private const int k_ELobbyTypePublic = 2;
        private const int k_ELobbyComparisonEqual = 0;
        private const int k_EFriendFlagImmediate = 0x04;

        private const int k_nSteamNetworkingSend_UnreliableNoNagle = 1;
        private const int k_nSteamNetworkingSend_ReliableNoNagle = 9;

        private const int Callback_GameLobbyJoinRequested = 333;
        private const int Callback_GameRichPresenceJoinRequested = 337;
        private const int Callback_LobbyEnter = 504;
        private const int Callback_LobbyChatUpdate = 506;
        private const int Callback_LobbyMatchList = 510;
        private const int Callback_LobbyCreated = 513;
        private const int Callback_SteamAPICallCompleted = 703;
        private const int Callback_NetMessagesSessionRequest = 1251;
        private const int Callback_NetMessagesSessionFailed = 1252;

        public static SteamSpacewarService Instance { get; } = new();

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct CallbackMsg_t
        {
            public int HSteamUser;
            public int Callback;
            public IntPtr ParamPtr;
            public int ParamSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct SteamAPICallCompleted_t
        {
            public ulong AsyncCall;
            public int Callback;
            public uint ParamSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct FriendGameInfo_t
        {
            public ulong GameId;
            public uint GameIp;
            public ushort GamePort;
            public ushort QueryPort;
            public ulong SteamIdLobby;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct SteamNetworkingIdentityBuf
        {
            public fixed byte Data[160];
        }

        // Делегаты плоского C API (cdecl)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FnInitFlat(IntPtr errMsgOut1024);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnInitLegacy();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnShutdown();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnIsSteamRunning();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FnGetHSteamPipe();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnManualDispatchInit();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnManualDispatchRunFrame(int pipe);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnManualDispatchGetNextCallback(int pipe, out CallbackMsg_t msg);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnManualDispatchFreeLastCallback(int pipe);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnManualDispatchGetAPICallResult(int pipe, ulong call, IntPtr buf, int bufSize, int expectedCallback, out byte failed);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr FnGetInterface();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnUserGetSteamID(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnUserBLoggedOn(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnAppsBIsAppInstalled(IntPtr self, uint appId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr FnFriendsGetPersonaName(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr FnFriendsGetFriendPersonaName(IntPtr self, ulong steamIdFriend);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnFriendsSetRichPresence(IntPtr self, IntPtr keyUtf8, IntPtr valUtf8);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnFriendsClearRichPresence(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnFriendsActivateInviteDialog(IntPtr self, ulong steamIdLobby);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FnFriendsGetFriendCount(IntPtr self, int friendFlags);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnFriendsGetFriendByIndex(IntPtr self, int index, int friendFlags);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnFriendsGetFriendGamePlayed(IntPtr self, ulong steamIdFriend, out FriendGameInfo_t gameInfo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnMatchCreateLobby(IntPtr self, int lobbyType, int maxMembers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnMatchJoinLobby(IntPtr self, ulong steamIdLobby);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnMatchLeaveLobby(IntPtr self, ulong steamIdLobby);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnMatchSetLobbyData(IntPtr self, ulong steamIdLobby, IntPtr keyUtf8, IntPtr valUtf8);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr FnMatchGetLobbyData(IntPtr self, ulong steamIdLobby, IntPtr keyUtf8);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnMatchSetLobbyJoinable(IntPtr self, ulong steamIdLobby, byte joinable);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnMatchGetLobbyOwner(IntPtr self, ulong steamIdLobby);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FnMatchGetNumLobbyMembers(IntPtr self, ulong steamIdLobby);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FnMatchGetLobbyMemberLimit(IntPtr self, ulong steamIdLobby);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnMatchGetLobbyMemberByIndex(IntPtr self, ulong steamIdLobby, int memberIndex);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnMatchAddStringFilter(IntPtr self, IntPtr keyUtf8, IntPtr valUtf8, int comparisonType);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnMatchAddResultCountFilter(IntPtr self, int maxResults);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnMatchRequestLobbyList(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnMatchGetLobbyByIndex(IntPtr self, int iLobby);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnMatchRequestLobbyData(IntPtr self, ulong steamIdLobby);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnNetUtilsInitRelay(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnNetUtilsSetGlobalConfigInt32(IntPtr self, int configValue, int val);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnIdentityClear(ref SteamNetworkingIdentityBuf self);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnIdentitySetSteamID64(ref SteamNetworkingIdentityBuf self, ulong steamId);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate ulong FnIdentityGetSteamID64(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FnNetMsgSendMessageToUser(IntPtr self, ref SteamNetworkingIdentityBuf identityRemote, IntPtr pubData, uint cubData, int sendFlags, int remoteChannel);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FnNetMsgReceiveMessagesOnChannel(IntPtr self, int localChannel, IntPtr ppOutMessages, int maxMessages);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnNetMsgAcceptSessionWithUser(IntPtr self, ref SteamNetworkingIdentityBuf identityRemote);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte FnNetMsgCloseSessionWithUser(IntPtr self, ref SteamNetworkingIdentityBuf identityRemote);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FnNetMessageRelease(IntPtr msgPtr);

        private IntPtr _dllHandle = IntPtr.Zero;
        private IntPtr _userPtr;
        private IntPtr _appsPtr;
        private IntPtr _friendsPtr;
        private IntPtr _matchPtr;
        private IntPtr _netMsgPtr;
        private IntPtr _netUtilsPtr;

        private FnInitFlat? _initFlat;
        private FnInitLegacy? _initLegacy;
        private FnShutdown? _shutdown;
        private FnIsSteamRunning? _isSteamRunning;
        private FnGetHSteamPipe? _getHSteamPipe;
        private FnManualDispatchInit? _mdInit;
        private FnManualDispatchRunFrame? _mdRunFrame;
        private FnManualDispatchGetNextCallback? _mdGetNext;
        private FnManualDispatchFreeLastCallback? _mdFreeLast;
        private FnManualDispatchGetAPICallResult? _mdGetCallResult;

        private FnUserGetSteamID? _userGetSteamId;
        private FnUserBLoggedOn? _userBLoggedOn;
        private FnAppsBIsAppInstalled? _appsIsInstalled;

        private FnFriendsGetPersonaName? _friendsGetPersonaName;
        private FnFriendsGetFriendPersonaName? _friendsGetFriendPersonaName;
        private FnFriendsSetRichPresence? _friendsSetRichPresence;
        private FnFriendsClearRichPresence? _friendsClearRichPresence;
        private FnFriendsActivateInviteDialog? _friendsActivateInviteDialog;
        private FnFriendsGetFriendCount? _friendsGetCount;
        private FnFriendsGetFriendByIndex? _friendsGetByIndex;
        private FnFriendsGetFriendGamePlayed? _friendsGetGamePlayed;

        private FnMatchCreateLobby? _matchCreateLobby;
        private FnMatchJoinLobby? _matchJoinLobby;
        private FnMatchLeaveLobby? _matchLeaveLobby;
        private FnMatchSetLobbyData? _matchSetLobbyData;
        private FnMatchGetLobbyData? _matchGetLobbyData;
        private FnMatchSetLobbyJoinable? _matchSetLobbyJoinable;
        private FnMatchGetLobbyOwner? _matchGetLobbyOwner;
        private FnMatchGetNumLobbyMembers? _matchGetNumMembers;
        private FnMatchGetLobbyMemberLimit? _matchGetMemberLimit;
        private FnMatchGetLobbyMemberByIndex? _matchGetMemberByIndex;
        private FnMatchAddStringFilter? _matchAddStringFilter;
        private FnMatchAddResultCountFilter? _matchAddCountFilter;
        private FnMatchRequestLobbyList? _matchRequestLobbyList;
        private FnMatchGetLobbyByIndex? _matchGetLobbyByIndex;
        private FnMatchRequestLobbyData? _matchRequestLobbyData;

        private FnNetUtilsInitRelay? _netUtilsInitRelay;
        private FnNetUtilsSetGlobalConfigInt32? _netUtilsSetConfigInt32;
        private FnIdentityClear? _identityClear;
        private FnIdentitySetSteamID64? _identitySetSteamId64;
        private FnIdentityGetSteamID64? _identityGetSteamId64;
        private FnNetMsgSendMessageToUser? _netSend;
        private FnNetMsgReceiveMessagesOnChannel? _netRecv;
        private FnNetMsgAcceptSessionWithUser? _netAccept;
        private FnNetMsgCloseSessionWithUser? _netClose;
        private FnNetMessageRelease? _netMsgRelease;

        public bool IsInitialized { get; private set; }
        public bool IsAvailable => IsInitialized;
        public bool SpacewarInstalledOnDisk { get; private set; }
        public bool SpacewarDownloadingOnDisk { get; private set; }
        public bool WaitingForSpacewarInstall { get; private set; }
        public bool IsSearchingLobbies { get; private set; }
        public ulong MySteamId { get; private set; }
        public string MyPersonaName { get; private set; } = "";
        public string LocalPersonaName => MyPersonaName;
        public ulong CurrentLobbyId { get; private set; }
        public ulong HostSteamId { get; private set; }
        public bool IsLobbyHost { get; private set; }
        public bool SessionActive { get; private set; }
        public bool HandshakeCompleted { get; private set; }
        public string StatusText { get; private set; } = "Steam Spacewar не инициализирован";

        private string _pendingGameType = "coop";
        private ulong _pendingLaunchJoinLobbyId = 0;
        private bool _autoInstallPromptedThisSession = false;
        private double _nextInstallPollTime = 0;
        private double _nextKeepAliveTime = 0;
        private double _nextHelloRetryTime = 0;
        private int _nextPeerIndex = 2;
        private ushort _nextChunkMsgId = 1;

        private readonly Dictionary<ulong, string> _steamToPid = new();
        private readonly Dictionary<string, ulong> _pidToSteam = new();
        private readonly Dictionary<ulong, double> _peerLastSeen = new();
        private readonly Dictionary<(ulong Peer, ushort MsgId), byte[][]> _chunkBuffers = new();
        private readonly Dictionary<(ulong Peer, ushort MsgId), int> _chunkReceivedCounts = new();

        public readonly List<SteamLobbyEntry> DiscoveredLobbies = new();
        public readonly ConcurrentQueue<JsonObject> Inbox = new();
        public ulong PendingAutoTransitionLobbyId = 0;

        private SteamSpacewarService() { }

        public void ParseCommandLineConnectArgs(string[]? args)
        {
            string[] argv = args ?? Environment.GetCommandLineArgs();
            for (int i = 0; i < argv.Length - 1; i++)
            {
                if (string.Equals(argv[i], "+connect_lobby", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(argv[i], "-steamjoin", StringComparison.OrdinalIgnoreCase))
                {
                    if (ulong.TryParse(argv[i + 1].Trim(), out ulong lobbyId) && lobbyId > 0)
                    {
                        _pendingLaunchJoinLobbyId = lobbyId;
                    }
                }
            }
        }

        public static string? FindSteamInstallPath()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var hkcu = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                    string? p1 = hkcu?.GetValue("SteamPath") as string;
                    if (!string.IsNullOrWhiteSpace(p1))
                    {
                        string norm = p1.Replace('/', Path.DirectorySeparatorChar);
                        if (Directory.Exists(norm)) return norm;
                    }
                }
                catch { }

                try
                {
                    using var hklm = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                    string? p2 = hklm?.GetValue("InstallPath") as string;
                    if (!string.IsNullOrWhiteSpace(p2) && Directory.Exists(p2))
                        return p2;
                }
                catch { }

                string[] winFallbacks =
                {
                    @"C:\Program Files (x86)\Steam",
                    @"C:\Program Files\Steam",
                    @"D:\Steam",
                    @"D:\steam",
                    @"E:\Steam"
                };
                foreach (string fb in winFallbacks)
                {
                    if (Directory.Exists(fb)) return fb;
                }
            }
            else
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] unixPaths =
                {
                    Path.Combine(home, ".steam", "steam"),
                    Path.Combine(home, ".local", "share", "Steam"),
                    Path.Combine(home, "Library", "Application Support", "Steam")
                };
                foreach (string up in unixPaths)
                {
                    if (Directory.Exists(up)) return up;
                }
            }
            return null;
        }

        public static List<string> GetSteamLibraryFolders()
        {
            var libs = new List<string>();
            string? root = FindSteamInstallPath();
            if (!string.IsNullOrEmpty(root))
            {
                libs.Add(root);
                string vdfPath = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    try
                    {
                        string content = File.ReadAllText(vdfPath);
                        foreach (Match m in Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\""))
                        {
                            string rawPath = m.Groups[1].Value.Replace("\\\\", "\\");
                            if (Directory.Exists(rawPath) && !libs.Any(x => string.Equals(x, rawPath, StringComparison.OrdinalIgnoreCase)))
                                libs.Add(rawPath);
                        }
                    }
                    catch { }
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                    string candidate = Path.Combine(drive.RootDirectory.FullName, "SteamLibrary");
                    if (Directory.Exists(candidate) && !libs.Any(x => string.Equals(x, candidate, StringComparison.OrdinalIgnoreCase)))
                        libs.Add(candidate);
                }
            }
            return libs;
        }

        public void RefreshSpacewarDiskState()
        {
            bool installed = false;
            bool downloading = false;

            if (IsInitialized && _appsPtr != IntPtr.Zero && _appsIsInstalled != null)
            {
                try
                {
                    if (_appsIsInstalled(_appsPtr, SpacewarAppId) != 0)
                        installed = true;
                }
                catch { }
            }

            foreach (string lib in GetSteamLibraryFolders())
            {
                string manifest = Path.Combine(lib, "steamapps", "appmanifest_480.acf");
                if (File.Exists(manifest))
                {
                    try
                    {
                        string txt = File.ReadAllText(manifest);
                        var m = Regex.Match(txt, "\"StateFlags\"\\s+\"(\\d+)\"");
                        if (m.Success)
                        {
                            int flags = int.Parse(m.Groups[1].Value);
                            if ((flags & 4) != 0)
                                installed = true;
                            else
                                downloading = true;
                        }
                        else
                        {
                            installed = true;
                        }
                    }
                    catch
                    {
                        installed = true;
                    }
                }

                string spacewarDir = Path.Combine(lib, "steamapps", "common", "Spacewar");
                if (Directory.Exists(spacewarDir))
                {
                    try
                    {
                        if (Directory.EnumerateFiles(spacewarDir).Any())
                            installed = true;
                    }
                    catch { }
                }
            }

            SpacewarInstalledOnDisk = installed;
            SpacewarDownloadingOnDisk = !installed && downloading;
        }

        public static bool IsSteamProcessRunning()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return Process.GetProcessesByName("steam").Length > 0;
                return Process.GetProcessesByName("steam").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool TriggerSpacewarInstall()
        {
            RefreshSpacewarDiskState();
            if (SpacewarInstalledOnDisk)
            {
                WaitingForSpacewarInstall = false;
                StatusText = "Spacewar (AppID 480) уже установлен на ПК.";
                return true;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string? steamRoot = FindSteamInstallPath();
                    string? steamExe = steamRoot != null ? Path.Combine(steamRoot, "steam.exe") : null;
                    if (steamExe != null && File.Exists(steamExe))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = steamExe,
                            Arguments = "steam://install/480",
                            UseShellExecute = false
                        });
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "steam://install/480",
                            UseShellExecute = true
                        });
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = "steam://install/480",
                        UseShellExecute = false
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "steam://install/480",
                        UseShellExecute = true
                    });
                }

                WaitingForSpacewarInstall = true;
                StatusText = "Открыто окно установки Spacewar (AppID 480) в Steam — подтвердите скачивание (~6 МБ)!";
                return true;
            }
            catch (Exception ex)
            {
                StatusText = $"Не удалось открыть steam://install/480: {ex.Message}";
                return false;
            }
        }

        private void EnsureSteamAppIdFiles()
        {
            Environment.SetEnvironmentVariable("SteamAppId", "480");
            Environment.SetEnvironmentVariable("SteamGameId", "480");
            string[] dirs = { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
            foreach (string d in dirs.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    string p = Path.Combine(d, "steam_appid.txt");
                    if (!File.Exists(p) || File.ReadAllText(p).Trim() != "480")
                        File.WriteAllText(p, "480");
                }
                catch { }
            }
        }

        private T? BindOptional<T>(params string[] exportNames) where T : Delegate
        {
            if (_dllHandle == IntPtr.Zero) return null;
            foreach (string name in exportNames)
            {
                if (NativeLibrary.TryGetExport(_dllHandle, name, out IntPtr addr) && addr != IntPtr.Zero)
                    return Marshal.GetDelegateForFunctionPointer<T>(addr);
            }
            return null;
        }

        private bool LoadNativeLibraryAndDelegates()
        {
            if (_dllHandle != IntPtr.Zero) return true;

            string libName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "steam_api64.dll"
                : (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libsteam_api.dylib" : "libsteam_api.so");

            string[] searchPaths =
            {
                Path.Combine(AppContext.BaseDirectory, libName),
                Path.Combine(AppContext.BaseDirectory, "Native", libName),
                Path.Combine(Directory.GetCurrentDirectory(), libName),
                Path.Combine(Directory.GetCurrentDirectory(), "Native", libName)
            };

            foreach (string candidate in searchPaths)
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out _dllHandle))
                    break;
            }

            if (_dllHandle == IntPtr.Zero)
                NativeLibrary.TryLoad(libName, out _dllHandle);

            if (_dllHandle == IntPtr.Zero)
            {
                StatusText = $"Не найден {libName} в папке игры.";
                return false;
            }

            _initFlat = BindOptional<FnInitFlat>("SteamAPI_InitFlat");
            _initLegacy = BindOptional<FnInitLegacy>("SteamAPI_Init");
            _shutdown = BindOptional<FnShutdown>("SteamAPI_Shutdown");
            _isSteamRunning = BindOptional<FnIsSteamRunning>("SteamAPI_IsSteamRunning");
            _getHSteamPipe = BindOptional<FnGetHSteamPipe>("SteamAPI_GetHSteamPipe");

            _mdInit = BindOptional<FnManualDispatchInit>("SteamAPI_ManualDispatch_Init");
            _mdRunFrame = BindOptional<FnManualDispatchRunFrame>("SteamAPI_ManualDispatch_RunFrame");
            _mdGetNext = BindOptional<FnManualDispatchGetNextCallback>("SteamAPI_ManualDispatch_GetNextCallback");
            _mdFreeLast = BindOptional<FnManualDispatchFreeLastCallback>("SteamAPI_ManualDispatch_FreeLastCallback");
            _mdGetCallResult = BindOptional<FnManualDispatchGetAPICallResult>("SteamAPI_ManualDispatch_GetAPICallResult");

            _userGetSteamId = BindOptional<FnUserGetSteamID>("SteamAPI_ISteamUser_GetSteamID");
            _userBLoggedOn = BindOptional<FnUserBLoggedOn>("SteamAPI_ISteamUser_BLoggedOn");
            _appsIsInstalled = BindOptional<FnAppsBIsAppInstalled>("SteamAPI_ISteamApps_BIsAppInstalled");

            _friendsGetPersonaName = BindOptional<FnFriendsGetPersonaName>("SteamAPI_ISteamFriends_GetPersonaName");
            _friendsGetFriendPersonaName = BindOptional<FnFriendsGetFriendPersonaName>("SteamAPI_ISteamFriends_GetFriendPersonaName");
            _friendsSetRichPresence = BindOptional<FnFriendsSetRichPresence>("SteamAPI_ISteamFriends_SetRichPresence");
            _friendsClearRichPresence = BindOptional<FnFriendsClearRichPresence>("SteamAPI_ISteamFriends_ClearRichPresence");
            _friendsActivateInviteDialog = BindOptional<FnFriendsActivateInviteDialog>("SteamAPI_ISteamFriends_ActivateGameOverlayInviteDialog");
            _friendsGetCount = BindOptional<FnFriendsGetFriendCount>("SteamAPI_ISteamFriends_GetFriendCount");
            _friendsGetByIndex = BindOptional<FnFriendsGetFriendByIndex>("SteamAPI_ISteamFriends_GetFriendByIndex");
            _friendsGetGamePlayed = BindOptional<FnFriendsGetFriendGamePlayed>("SteamAPI_ISteamFriends_GetFriendGamePlayed");

            _matchCreateLobby = BindOptional<FnMatchCreateLobby>("SteamAPI_ISteamMatchmaking_CreateLobby");
            _matchJoinLobby = BindOptional<FnMatchJoinLobby>("SteamAPI_ISteamMatchmaking_JoinLobby");
            _matchLeaveLobby = BindOptional<FnMatchLeaveLobby>("SteamAPI_ISteamMatchmaking_LeaveLobby");
            _matchSetLobbyData = BindOptional<FnMatchSetLobbyData>("SteamAPI_ISteamMatchmaking_SetLobbyData");
            _matchGetLobbyData = BindOptional<FnMatchGetLobbyData>("SteamAPI_ISteamMatchmaking_GetLobbyData");
            _matchSetLobbyJoinable = BindOptional<FnMatchSetLobbyJoinable>("SteamAPI_ISteamMatchmaking_SetLobbyJoinable");
            _matchGetLobbyOwner = BindOptional<FnMatchGetLobbyOwner>("SteamAPI_ISteamMatchmaking_GetLobbyOwner");
            _matchGetNumMembers = BindOptional<FnMatchGetNumLobbyMembers>("SteamAPI_ISteamMatchmaking_GetNumLobbyMembers");
            _matchGetMemberLimit = BindOptional<FnMatchGetLobbyMemberLimit>("SteamAPI_ISteamMatchmaking_GetLobbyMemberLimit");
            _matchGetMemberByIndex = BindOptional<FnMatchGetLobbyMemberByIndex>("SteamAPI_ISteamMatchmaking_GetLobbyMemberByIndex");
            _matchAddStringFilter = BindOptional<FnMatchAddStringFilter>("SteamAPI_ISteamMatchmaking_AddRequestLobbyListStringFilter");
            _matchAddCountFilter = BindOptional<FnMatchAddResultCountFilter>("SteamAPI_ISteamMatchmaking_AddRequestLobbyListResultCountFilter");
            _matchRequestLobbyList = BindOptional<FnMatchRequestLobbyList>("SteamAPI_ISteamMatchmaking_RequestLobbyList");
            _matchGetLobbyByIndex = BindOptional<FnMatchGetLobbyByIndex>("SteamAPI_ISteamMatchmaking_GetLobbyByIndex");
            _matchRequestLobbyData = BindOptional<FnMatchRequestLobbyData>("SteamAPI_ISteamMatchmaking_RequestLobbyData");

            _netUtilsInitRelay = BindOptional<FnNetUtilsInitRelay>("SteamAPI_ISteamNetworkingUtils_InitRelayNetworkAccess");
            _netUtilsSetConfigInt32 = BindOptional<FnNetUtilsSetGlobalConfigInt32>("SteamAPI_ISteamNetworkingUtils_SetGlobalConfigValueInt32");
            _identityClear = BindOptional<FnIdentityClear>("SteamAPI_SteamNetworkingIdentity_Clear");
            _identitySetSteamId64 = BindOptional<FnIdentitySetSteamID64>("SteamAPI_SteamNetworkingIdentity_SetSteamID64");
            _identityGetSteamId64 = BindOptional<FnIdentityGetSteamID64>("SteamAPI_SteamNetworkingIdentity_GetSteamID64");

            _netSend = BindOptional<FnNetMsgSendMessageToUser>("SteamAPI_ISteamNetworkingMessages_SendMessageToUser");
            _netRecv = BindOptional<FnNetMsgReceiveMessagesOnChannel>("SteamAPI_ISteamNetworkingMessages_ReceiveMessagesOnChannel");
            _netAccept = BindOptional<FnNetMsgAcceptSessionWithUser>("SteamAPI_ISteamNetworkingMessages_AcceptSessionWithUser");
            _netClose = BindOptional<FnNetMsgCloseSessionWithUser>("SteamAPI_ISteamNetworkingMessages_CloseSessionWithUser");
            _netMsgRelease = BindOptional<FnNetMessageRelease>("SteamAPI_SteamNetworkingMessage_t_Release");

            return _mdInit != null && _mdRunFrame != null && _mdGetNext != null && _mdFreeLast != null;
        }

        public bool EnsureInitialized(bool autoInstallIfMissing = true)
        {
            RefreshSpacewarDiskState();

            if (!SpacewarInstalledOnDisk && autoInstallIfMissing && !_autoInstallPromptedThisSession)
            {
                _autoInstallPromptedThisSession = true;
                TriggerSpacewarInstall();
                if (!SpacewarInstalledOnDisk)
                    return false;
            }

            if (IsInitialized)
                return true;

            if (!SpacewarInstalledOnDisk)
            {
                StatusText = SpacewarDownloadingOnDisk
                    ? "Spacewar (AppID 480) скачивается в Steam... Подождите пару секунд."
                    : "Spacewar (AppID 480) не установлен. Нажмите «Скачать Spacewar через Steam».";
                return false;
            }

            EnsureSteamAppIdFiles();
            if (!LoadNativeLibraryAndDelegates())
                return false;

            if (_isSteamRunning != null && _isSteamRunning() == 0 && !IsSteamProcessRunning())
            {
                StatusText = "Клиент Steam не запущен! Запустите Steam и войдите в аккаунт.";
                return false;
            }

            bool initOk = false;
            string errDetail = "";
            if (_initFlat != null)
            {
                IntPtr errBuf = Marshal.AllocHGlobal(1024);
                try
                {
                    Marshal.WriteByte(errBuf, 0, 0);
                    int rc = _initFlat(errBuf);
                    initOk = rc == 0;
                    if (!initOk)
                        errDetail = Marshal.PtrToStringUTF8(errBuf) ?? $"код {rc}";
                }
                finally
                {
                    Marshal.FreeHGlobal(errBuf);
                }
            }
            else if (_initLegacy != null)
            {
                initOk = _initLegacy() != 0;
            }

            if (!initOk)
            {
                StatusText = string.IsNullOrWhiteSpace(errDetail)
                    ? "Не удалось подключиться к Steam API (проверьте, запущен ли Steam)."
                    : $"Steam API: {errDetail}";
                return false;
            }

            var getUser = BindOptional<FnGetInterface>("SteamAPI_SteamUser_v023", "SteamAPI_SteamUser_v022", "SteamAPI_SteamUser_v021");
            var getApps = BindOptional<FnGetInterface>("SteamAPI_SteamApps_v009", "SteamAPI_SteamApps_v008");
            var getFriends = BindOptional<FnGetInterface>("SteamAPI_SteamFriends_v018", "SteamAPI_SteamFriends_v017");
            var getMatch = BindOptional<FnGetInterface>("SteamAPI_SteamMatchmaking_v009");
            var getNetMsg = BindOptional<FnGetInterface>("SteamAPI_SteamNetworkingMessages_SteamAPI_v002");
            var getNetUtils = BindOptional<FnGetInterface>("SteamAPI_SteamNetworkingUtils_SteamAPI_v004", "SteamAPI_SteamNetworkingUtils_SteamAPI_v003");

            _userPtr = getUser != null ? getUser() : IntPtr.Zero;
            _appsPtr = getApps != null ? getApps() : IntPtr.Zero;
            _friendsPtr = getFriends != null ? getFriends() : IntPtr.Zero;
            _matchPtr = getMatch != null ? getMatch() : IntPtr.Zero;
            _netMsgPtr = getNetMsg != null ? getNetMsg() : IntPtr.Zero;
            _netUtilsPtr = getNetUtils != null ? getNetUtils() : IntPtr.Zero;

            if (_userPtr == IntPtr.Zero || _matchPtr == IntPtr.Zero || _netMsgPtr == IntPtr.Zero)
            {
                StatusText = "Не удалось получить интерфейсы Steam Matchmaking / NetworkingMessages.";
                return false;
            }

            _mdInit?.Invoke();

            if (_netUtilsPtr != IntPtr.Zero)
            {
                _netUtilsInitRelay?.Invoke(_netUtilsPtr);
                // Увеличиваем буферы релея (SendBufferSize=13, RecvBufferSize=14, RecvMaxMessageSize=15 в ESteamNetworkingConfigValue)
                _netUtilsSetConfigInt32?.Invoke(_netUtilsPtr, 13, 8 * 1024 * 1024);
                _netUtilsSetConfigInt32?.Invoke(_netUtilsPtr, 14, 8 * 1024 * 1024);
            }

            MySteamId = _userGetSteamId != null ? _userGetSteamId(_userPtr) : 0;
            MyPersonaName = ReadUtf8Ptr(_friendsGetPersonaName != null && _friendsPtr != IntPtr.Zero ? _friendsGetPersonaName(_friendsPtr) : IntPtr.Zero);
            if (string.IsNullOrWhiteSpace(MyPersonaName))
                MyPersonaName = "SteamPlayer";

            IsInitialized = true;
            WaitingForSpacewarInstall = false;
            RefreshSpacewarDiskState();
            StatusText = $"Steam Spacewar готов ({MyPersonaName})";

            if (_pendingLaunchJoinLobbyId != 0)
            {
                ulong target = _pendingLaunchJoinLobbyId;
                _pendingLaunchJoinLobbyId = 0;
                JoinSteamLobby(target, _pendingGameType);
            }

            return true;
        }

        public (bool Ok, string Info) HostSteamLobby(string gameType, int maxMembers = 8)
        {
            _pendingGameType = gameType;
            if (!EnsureInitialized(autoInstallIfMissing: true))
                return (false, StatusText);

            LeaveLobbyAndClearPeers();

            if (_matchCreateLobby == null)
                return (false, "CreateLobby недоступен в steam_api64.dll");

            IsLobbyHost = true;
            SessionActive = true;
            HandshakeCompleted = true;
            StatusText = "Создание лобби Steam (Spacewar)...";
            _matchCreateLobby(_matchPtr, k_ELobbyTypePublic, maxMembers);
            return (true, "STEAM-ЛОББИ...");
        }

        public (bool Ok, string Info) JoinSteamLobby(ulong lobbyId, string expectedGameType)
        {
            _pendingGameType = expectedGameType;
            if (lobbyId == 0)
                return (false, "Некорректный ID Steam-лобби");

            if (!EnsureInitialized(autoInstallIfMissing: true))
                return (false, StatusText);

            LeaveLobbyAndClearPeers();

            if (_matchJoinLobby == null)
                return (false, "JoinLobby недоступен в steam_api64.dll");

            IsLobbyHost = false;
            SessionActive = true;
            HandshakeCompleted = false;
            CurrentLobbyId = lobbyId;
            StatusText = $"Подключение к Steam-лобби {lobbyId}...";
            _matchJoinLobby(_matchPtr, lobbyId);
            return (true, $"STEAM-{lobbyId}");
        }

        public void RefreshLobbies(string gameType)
        {
            _pendingGameType = gameType;
            if (!EnsureInitialized(autoInstallIfMissing: true))
                return;

            DiscoveredLobbies.Clear();

            // 1. Сначала мгновенно ищем друзей, играющих в Spacewar (480) с открытым лобби
            if (_friendsPtr != IntPtr.Zero && _friendsGetCount != null && _friendsGetByIndex != null && _friendsGetGamePlayed != null)
            {
                int count = _friendsGetCount(_friendsPtr, k_EFriendFlagImmediate);
                for (int i = 0; i < count; i++)
                {
                    ulong fId = _friendsGetByIndex(_friendsPtr, i, k_EFriendFlagImmediate);
                    if (fId == 0) continue;
                    if (_friendsGetGamePlayed(_friendsPtr, fId, out var gInfo) != 0)
                    {
                        uint appId = (uint)(gInfo.GameId & 0xFFFFFFUL);
                        if (appId == SpacewarAppId && gInfo.SteamIdLobby != 0)
                        {
                            _matchRequestLobbyData?.Invoke(_matchPtr, gInfo.SteamIdLobby);
                            string fName = GetFriendName(fId);
                            string mode = GetLobbyDataString(gInfo.SteamIdLobby, "roc2_mode");
                            if (string.IsNullOrEmpty(mode)) mode = gameType;
                            if (DiscoveredLobbies.All(x => x.LobbyId != gInfo.SteamIdLobby))
                            {
                                DiscoveredLobbies.Add(new SteamLobbyEntry
                                {
                                    LobbyId = gInfo.SteamIdLobby,
                                    HostName = string.IsNullOrWhiteSpace(fName) ? $"Друг {fId}" : fName,
                                    GameType = mode,
                                    MemberCount = Math.Max(1, _matchGetNumMembers != null ? _matchGetNumMembers(_matchPtr, gInfo.SteamIdLobby) : 1),
                                    MaxMembers = Math.Max(2, _matchGetMemberLimit != null ? _matchGetMemberLimit(_matchPtr, gInfo.SteamIdLobby) : 8),
                                    IsFriendLobby = true
                                });
                            }
                        }
                    }
                }
            }

            // 2. Запрашиваем глобальный список лобби с фильтром roc2_app = RiskOfCSharp2_v1
            if (_matchAddStringFilter != null && _matchRequestLobbyList != null)
            {
                WithUtf8(LobbyAppTag, appValPtr =>
                {
                    WithUtf8("roc2_app", keyPtr =>
                    {
                        _matchAddStringFilter(_matchPtr, keyPtr, appValPtr, k_ELobbyComparisonEqual);
                    });
                });
                _matchAddCountFilter?.Invoke(_matchPtr, 32);
                _matchRequestLobbyList(_matchPtr);
                IsSearchingLobbies = true;
                StatusText = "Поиск лобби Risk of C# 2 в Steam (Spacewar)...";
            }
        }

        public void OpenInviteOverlay()
        {
            if (!IsInitialized || CurrentLobbyId == 0 || _friendsPtr == IntPtr.Zero) return;
            _friendsActivateInviteDialog?.Invoke(_friendsPtr, CurrentLobbyId);
        }

        public void Update()
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            if (!IsInitialized)
            {
                if (WaitingForSpacewarInstall && now >= _nextInstallPollTime)
                {
                    _nextInstallPollTime = now + 1.6;
                    RefreshSpacewarDiskState();
                    if (SpacewarInstalledOnDisk)
                    {
                        EnsureInitialized(autoInstallIfMissing: false);
                    }
                    else if (SpacewarDownloadingOnDisk)
                    {
                        StatusText = "Spacewar (AppID 480) скачивается в клиенте Steam... Почти готово!";
                    }
                }
                return;
            }

            PumpCallbacks();

            if (!SessionActive)
                return;

            ReceiveChannelMessages(0, now);
            ReceiveChannelMessages(1, now);

            // Если клиент вошёл в лобби и ждёт welcome от хоста — повторяем hello каждые 0.85с
            if (!IsLobbyHost && HostSteamId != 0 && !HandshakeCompleted && now >= _nextHelloRetryTime)
            {
                _nextHelloRetryTime = now + 0.85;
                SendControlPacket(HostSteamId, "steam_hello");
            }

            // Keep-alive раз в 1.5с + таймаут 22с
            if (now >= _nextKeepAliveTime)
            {
                _nextKeepAliveTime = now + 1.5;
                if (IsLobbyHost)
                {
                    foreach (ulong peer in _steamToPid.Keys.ToArray())
                    {
                        SendControlPacket(peer, "ping");
                        if (_peerLastSeen.TryGetValue(peer, out double last) && now - last > 22.0)
                            HandlePeerDisconnected(peer);
                    }
                }
                else if (HostSteamId != 0)
                {
                    SendControlPacket(HostSteamId, HandshakeCompleted ? "ping" : "steam_hello");
                    if (HandshakeCompleted && _peerLastSeen.TryGetValue(HostSteamId, out double lastHost) && now - lastHost > 22.0)
                    {
                        StatusText = "Связь с хостом Steam потеряна (таймаут).";
                        Inbox.Enqueue(new JsonObject { ["type"] = "disconnected" });
                        LeaveLobbyAndClearPeers();
                    }
                }
            }
        }

        private void PumpCallbacks()
        {
            if (_getHSteamPipe == null || _mdRunFrame == null || _mdGetNext == null || _mdFreeLast == null)
                return;

            int pipe = _getHSteamPipe();
            if (pipe == 0) return;

            _mdRunFrame(pipe);
            while (_mdGetNext(pipe, out CallbackMsg_t msg) != 0)
            {
                try
                {
                    if (msg.Callback == Callback_SteamAPICallCompleted && msg.ParamPtr != IntPtr.Zero && _mdGetCallResult != null)
                    {
                        var completed = Marshal.PtrToStructure<SteamAPICallCompleted_t>(msg.ParamPtr);
                        int sz = (int)Math.Max(16u, completed.ParamSize);
                        IntPtr resBuf = Marshal.AllocHGlobal(sz);
                        try
                        {
                            if (_mdGetCallResult(pipe, completed.AsyncCall, resBuf, sz, completed.Callback, out byte failed) != 0 && failed == 0)
                            {
                                DispatchCallback(completed.Callback, resBuf, (int)completed.ParamSize);
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(resBuf);
                        }
                    }
                    else if (msg.ParamPtr != IntPtr.Zero)
                    {
                        DispatchCallback(msg.Callback, msg.ParamPtr, msg.ParamSize);
                    }
                }
                finally
                {
                    _mdFreeLast(pipe);
                }
            }
        }

        private void DispatchCallback(int callbackId, IntPtr paramPtr, int paramSize)
        {
            switch (callbackId)
            {
                case Callback_LobbyCreated:
                {
                    int result = Marshal.ReadInt32(paramPtr, 0);
                    ulong lobbyId = paramSize == 12
                        ? (ulong)Marshal.ReadInt64(paramPtr, 4)
                        : (ulong)Marshal.ReadInt64(paramPtr, 8);

                    if (result != 1 || lobbyId == 0)
                    {
                        StatusText = $"Ошибка создания Steam-лобби (код {result}).";
                        SessionActive = false;
                        return;
                    }

                    // LobbyCreated приходит дважды в реальном Steam (call result 703 + broadcast 513) — обрабатываем идемпотентно
                    if (CurrentLobbyId == lobbyId)
                        return;

                    CurrentLobbyId = lobbyId;
                    HostSteamId = MySteamId;
                    IsLobbyHost = true;
                    SessionActive = true;
                    HandshakeCompleted = true;

                    SetLobbyDataString(lobbyId, "roc2_app", LobbyAppTag);
                    SetLobbyDataString(lobbyId, "roc2_mode", _pendingGameType);
                    SetLobbyDataString(lobbyId, "roc2_host", MyPersonaName);
                    _matchSetLobbyJoinable?.Invoke(_matchPtr, lobbyId, 1);
                    SetRichPresenceString("connect", $"+connect_lobby {lobbyId}");
                    SetRichPresenceString("status", $"Risk of C# 2 [{_pendingGameType.ToUpper()}]");

                    NetworkManager.Instance.OnSteamLobbyCreated(lobbyId, _pendingGameType);
                    StatusText = $"Лобби Steam создано! ID: {lobbyId}";
                    break;
                }

                case Callback_LobbyEnter:
                {
                    ulong lobbyId = (ulong)Marshal.ReadInt64(paramPtr, 0);
                    uint response = (uint)Marshal.ReadInt32(paramPtr, 16);
                    if (response != 1 || lobbyId == 0)
                    {
                        StatusText = $"Не удалось войти в Steam-лобби (код {response}).";
                        SessionActive = false;
                        return;
                    }

                    ulong owner = _matchGetLobbyOwner != null ? _matchGetLobbyOwner(_matchPtr, lobbyId) : 0;
                    if (owner == MySteamId && IsLobbyHost)
                        return;

                    string appTag = GetLobbyDataString(lobbyId, "roc2_app");
                    if (!string.IsNullOrEmpty(appTag) && !string.Equals(appTag, LobbyAppTag, StringComparison.Ordinal))
                    {
                        StatusText = "Это лобби другой игры на Spacewar (не Risk of C# 2).";
                        LeaveLobbyAndClearPeers();
                        return;
                    }

                    string lobbyMode = GetLobbyDataString(lobbyId, "roc2_mode");
                    if (lobbyMode is "coop" or "pvp")
                        _pendingGameType = lobbyMode;

                    CurrentLobbyId = lobbyId;
                    HostSteamId = owner;
                    IsLobbyHost = false;
                    SessionActive = true;
                    HandshakeCompleted = false;
                    _peerLastSeen[owner] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

                    AcceptPeer(owner);
                    SetRichPresenceString("connect", $"+connect_lobby {lobbyId}");
                    SendControlPacket(owner, "steam_hello");
                    _nextHelloRetryTime = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0) + 0.6;

                    NetworkManager.Instance.OnSteamClientEnteredLobby(lobbyId, _pendingGameType);
                    StatusText = $"Вход в лобби {lobbyId} выполнен, ожидание ответа хоста...";
                    break;
                }

                case Callback_LobbyMatchList:
                {
                    IsSearchingLobbies = false;
                    uint count = (uint)Marshal.ReadInt32(paramPtr, 0);
                    if (_matchGetLobbyByIndex != null)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            ulong lId = _matchGetLobbyByIndex(_matchPtr, i);
                            if (lId == 0) continue;
                            string appTag = GetLobbyDataString(lId, "roc2_app");
                            if (!string.Equals(appTag, LobbyAppTag, StringComparison.Ordinal))
                                continue;

                            string hostName = GetLobbyDataString(lId, "roc2_host");
                            string mode = GetLobbyDataString(lId, "roc2_mode");
                            if (string.IsNullOrWhiteSpace(mode)) mode = "coop";
                            int members = _matchGetNumMembers != null ? _matchGetNumMembers(_matchPtr, lId) : 1;
                            int limit = _matchGetMemberLimit != null ? _matchGetMemberLimit(_matchPtr, lId) : 8;

                            var existing = DiscoveredLobbies.FirstOrDefault(x => x.LobbyId == lId);
                            if (existing != null)
                            {
                                if (!string.IsNullOrWhiteSpace(hostName)) existing.HostName = hostName;
                                existing.GameType = mode;
                                existing.MemberCount = Math.Max(1, members);
                                existing.MaxMembers = Math.Max(2, limit);
                            }
                            else
                            {
                                DiscoveredLobbies.Add(new SteamLobbyEntry
                                {
                                    LobbyId = lId,
                                    HostName = string.IsNullOrWhiteSpace(hostName) ? $"Лобби {lId}" : hostName,
                                    GameType = mode,
                                    MemberCount = Math.Max(1, members),
                                    MaxMembers = Math.Max(2, limit),
                                    IsFriendLobby = false
                                });
                            }
                        }
                    }
                    StatusText = DiscoveredLobbies.Count > 0
                        ? $"Найдено лобби Steam: {DiscoveredLobbies.Count}"
                        : "Открытых лобби Risk of C# 2 в Steam пока не найдено.";
                    break;
                }

                case Callback_LobbyChatUpdate:
                {
                    ulong lobbyId = (ulong)Marshal.ReadInt64(paramPtr, 0);
                    ulong userChanged = (ulong)Marshal.ReadInt64(paramPtr, 8);
                    uint flags = (uint)Marshal.ReadInt32(paramPtr, 24);
                    if (lobbyId != CurrentLobbyId || CurrentLobbyId == 0)
                        break;

                    // 0x02 Left, 0x04 Disconnected, 0x08 Kicked, 0x10 Banned
                    if ((flags & 0x1EU) != 0)
                    {
                        if (IsLobbyHost)
                        {
                            HandlePeerDisconnected(userChanged);
                        }
                        else if (userChanged == HostSteamId)
                        {
                            StatusText = "Хост закрыл Steam-лобби.";
                            Inbox.Enqueue(new JsonObject { ["type"] = "disconnected" });
                            LeaveLobbyAndClearPeers();
                        }
                    }
                    break;
                }

                case Callback_GameLobbyJoinRequested:
                {
                    ulong lobbyId = (ulong)Marshal.ReadInt64(paramPtr, 0);
                    if (lobbyId != 0)
                    {
                        PendingAutoTransitionLobbyId = lobbyId;
                        NetworkManager.Instance.JoinSteamLobbyFromInvite(lobbyId);
                    }
                    break;
                }

                case Callback_GameRichPresenceJoinRequested:
                {
                    string connectStr = ReadUtf8Ptr(paramPtr + 8, 256);
                    var m = Regex.Match(connectStr, @"(\d{10,22})");
                    if (m.Success && ulong.TryParse(m.Groups[1].Value, out ulong lobbyId) && lobbyId != 0)
                    {
                        PendingAutoTransitionLobbyId = lobbyId;
                        NetworkManager.Instance.JoinSteamLobbyFromInvite(lobbyId);
                    }
                    break;
                }

                case Callback_NetMessagesSessionRequest:
                {
                    ulong remoteId = _identityGetSteamId64 != null ? _identityGetSteamId64(paramPtr) : 0;
                    if (remoteId != 0 && SessionActive && (IsLobbyHost || remoteId == HostSteamId))
                    {
                        AcceptPeer(remoteId);
                    }
                    break;
                }

                case Callback_NetMessagesSessionFailed:
                {
                    ulong remoteId = _identityGetSteamId64 != null ? _identityGetSteamId64(paramPtr) : 0;
                    if (remoteId != 0)
                    {
                        if (IsLobbyHost)
                            HandlePeerDisconnected(remoteId);
                        else if (remoteId == HostSteamId && HandshakeCompleted)
                        {
                            Inbox.Enqueue(new JsonObject { ["type"] = "disconnected" });
                            LeaveLobbyAndClearPeers();
                        }
                    }
                    break;
                }
            }
        }

        private void ReceiveChannelMessages(int channel, double now)
        {
            if (_netRecv == null || _netMsgRelease == null || _netMsgPtr == IntPtr.Zero)
                return;

            const int batchMax = 64;
            IntPtr msgArray = Marshal.AllocHGlobal(IntPtr.Size * batchMax);
            try
            {
                while (true)
                {
                    int count = _netRecv(_netMsgPtr, channel, msgArray, batchMax);
                    if (count <= 0) break;

                    for (int i = 0; i < count; i++)
                    {
                        IntPtr msgPtr = Marshal.ReadIntPtr(msgArray, i * IntPtr.Size);
                        if (msgPtr == IntPtr.Zero) continue;
                        try
                        {
                            IntPtr dataPtr = Marshal.ReadIntPtr(msgPtr, 0);
                            int cbSize = Marshal.ReadInt32(msgPtr, IntPtr.Size == 8 ? 8 : 4);
                            ulong senderSteamId = ExtractSenderSteamId(msgPtr);

                            if (senderSteamId != 0)
                                _peerLastSeen[senderSteamId] = now;

                            if (dataPtr != IntPtr.Zero && cbSize > 0 && cbSize <= 16 * 1024 * 1024)
                            {
                                byte[] raw = new byte[cbSize];
                                Marshal.Copy(dataPtr, raw, 0, cbSize);
                                if (channel == 1)
                                    HandleControlPayload(senderSteamId, raw, now);
                                else
                                    HandleDataPayload(senderSteamId, raw);
                            }
                        }
                        finally
                        {
                            _netMsgRelease(msgPtr);
                        }
                    }

                    if (count < batchMax) break;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(msgArray);
            }
        }

        private ulong ExtractSenderSteamId(IntPtr msgPtr)
        {
            if (_identityGetSteamId64 == null) return 0;
            // На 64-бит и 32-бит MSVC m_identityPeer находится по смещению 16; делаем резервную проверку 24/12
            ulong id16 = _identityGetSteamId64(msgPtr + 16);
            if (id16 >= 76561197960265728UL) return id16;
            ulong id24 = _identityGetSteamId64(msgPtr + 24);
            if (id24 >= 76561197960265728UL) return id24;
            return id16 != 0 ? id16 : id24;
        }

        private void HandleControlPayload(ulong senderSteamId, byte[] raw, double now)
        {
            if (senderSteamId == 0 || senderSteamId == MySteamId) return;
            string cmd = Encoding.UTF8.GetString(raw).Trim();
            if (cmd == "steam_hello" && IsLobbyHost)
            {
                AcceptPeer(senderSteamId);
                RegisterOrResendWelcome(senderSteamId, now);
            }
            else if (cmd == "goodbye")
            {
                if (IsLobbyHost)
                    HandlePeerDisconnected(senderSteamId);
                else if (senderSteamId == HostSteamId)
                {
                    Inbox.Enqueue(new JsonObject { ["type"] = "disconnected" });
                    LeaveLobbyAndClearPeers();
                }
            }
            else if (cmd == "ping" && IsLobbyHost && !_steamToPid.ContainsKey(senderSteamId))
            {
                AcceptPeer(senderSteamId);
                RegisterOrResendWelcome(senderSteamId, now);
            }
        }

        private void RegisterOrResendWelcome(ulong clientSteamId, double now)
        {
            bool isNew = false;
            if (!_steamToPid.TryGetValue(clientSteamId, out string? pid))
            {
                pid = $"p{_nextPeerIndex++}";
                _steamToPid[clientSteamId] = pid;
                _pidToSteam[pid] = clientSteamId;
                _peerLastSeen[clientSteamId] = now;
                isNew = true;
            }

            string roomLabel = NetworkManager.Instance.RoomCode;
            var welcome = new JsonObject
            {
                ["type"] = "welcome",
                ["player_id"] = pid,
                ["room_code"] = roomLabel,
                ["game_type"] = NetworkManager.Instance.GameType
            };
            SendJsonToSteamPeer(clientSteamId, welcome, reliable: true);

            if (isNew)
            {
                Inbox.Enqueue(new JsonObject
                {
                    ["type"] = "client_joined",
                    ["player_id"] = pid
                });
            }
        }

        private void HandleDataPayload(ulong senderSteamId, byte[] raw)
        {
            if (raw.Length < 2 || senderSteamId == 0 || senderSteamId == MySteamId) return;

            byte frameType = raw[0];
            byte[]? fullJsonBytes = null;

            if (frameType == 0)
            {
                fullJsonBytes = new byte[raw.Length - 1];
                Buffer.BlockCopy(raw, 1, fullJsonBytes, 0, fullJsonBytes.Length);
            }
            else if (frameType == 1 && raw.Length > 5)
            {
                ushort msgId = (ushort)(raw[1] | (raw[2] << 8));
                int idx = raw[3];
                int total = raw[4];
                if (total > 0 && idx < total)
                {
                    var key = (senderSteamId, msgId);
                    if (!_chunkBuffers.TryGetValue(key, out var parts) || parts.Length != total)
                    {
                        parts = new byte[total][];
                        _chunkBuffers[key] = parts;
                        _chunkReceivedCounts[key] = 0;
                    }
                    if (parts[idx] == null)
                    {
                        byte[] slice = new byte[raw.Length - 5];
                        Buffer.BlockCopy(raw, 5, slice, 0, slice.Length);
                        parts[idx] = slice;
                        _chunkReceivedCounts[key]++;
                    }
                    if (_chunkReceivedCounts[key] == total)
                    {
                        int sum = 0;
                        for (int k = 0; k < total; k++) sum += parts[k].Length;
                        fullJsonBytes = new byte[sum];
                        int pos = 0;
                        for (int k = 0; k < total; k++)
                        {
                            Buffer.BlockCopy(parts[k], 0, fullJsonBytes, pos, parts[k].Length);
                            pos += parts[k].Length;
                        }
                        _chunkBuffers.Remove(key);
                        _chunkReceivedCounts.Remove(key);
                    }
                }
            }

            if (fullJsonBytes == null) return;

            try
            {
                string jsonStr = Encoding.UTF8.GetString(fullJsonBytes);
                var msg = JsonNode.Parse(jsonStr)?.AsObject();
                if (msg == null) return;

                if (IsLobbyHost)
                {
                    if (!_steamToPid.TryGetValue(senderSteamId, out string? senderPid))
                    {
                        RegisterOrResendWelcome(senderSteamId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
                        senderPid = _steamToPid[senderSteamId];
                    }
                    msg["_sender"] = senderPid;
                    BroadcastFromHost(msg, excludePid: senderPid);
                    Inbox.Enqueue(msg);
                }
                else if (senderSteamId == HostSteamId)
                {
                    string? mType = msg["type"]?.GetValue<string>();
                    if (mType == "welcome")
                    {
                        HandshakeCompleted = true;
                        StatusText = $"Подключено к Steam-лобби {CurrentLobbyId}!";
                    }
                    Inbox.Enqueue(msg);
                }
            }
            catch { }
        }

        private void HandlePeerDisconnected(ulong peerSteamId)
        {
            if (!_steamToPid.TryGetValue(peerSteamId, out string? pid))
                return;

            _steamToPid.Remove(peerSteamId);
            _pidToSteam.Remove(pid);
            _peerLastSeen.Remove(peerSteamId);
            ClosePeer(peerSteamId);

            Inbox.Enqueue(new JsonObject { ["type"] = "player_left", ["player_id"] = pid });
            BroadcastFromHost(new JsonObject { ["type"] = "player_left", ["player_id"] = pid }, excludePid: pid);
        }

        public void Broadcast(JsonObject data, string? excludePid = null)
        {
            if (!SessionActive || !IsInitialized) return;
            string? msgType = data["type"]?.GetValue<string>();
            bool reliable = msgType != "player_state";

            if (IsLobbyHost)
            {
                BroadcastFromHost(data, excludePid);
            }
            else if (HostSteamId != 0)
            {
                SendJsonToSteamPeer(HostSteamId, data, reliable || !HandshakeCompleted);
            }
        }

        private void BroadcastFromHost(JsonObject data, string? excludePid)
        {
            string? msgType = data["type"]?.GetValue<string>();
            bool reliable = msgType != "player_state";
            foreach (var (pid, steamId) in _pidToSteam.ToArray())
            {
                if (pid == excludePid) continue;
                SendJsonToSteamPeer(steamId, data, reliable);
            }
        }

        private void SendControlPacket(ulong targetSteamId, string text)
        {
            if (_netSend == null || _netMsgPtr == IntPtr.Zero || targetSteamId == 0) return;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            SendRawOnChannel(targetSteamId, bytes, k_nSteamNetworkingSend_ReliableNoNagle, 1);
        }

        private void SendJsonToSteamPeer(ulong targetSteamId, JsonObject data, bool reliable)
        {
            if (_netSend == null || _netMsgPtr == IntPtr.Zero || targetSteamId == 0) return;
            byte[] payload = Encoding.UTF8.GetBytes(data.ToJsonString());
            const int maxSingle = 56000;
            int flags = reliable ? k_nSteamNetworkingSend_ReliableNoNagle : k_nSteamNetworkingSend_UnreliableNoNagle;

            if (payload.Length <= maxSingle)
            {
                byte[] framed = new byte[payload.Length + 1];
                framed[0] = 0;
                Buffer.BlockCopy(payload, 0, framed, 1, payload.Length);
                SendRawOnChannel(targetSteamId, framed, flags, 0);
            }
            else
            {
                ushort msgId = _nextChunkMsgId++;
                int totalChunks = (payload.Length + maxSingle - 1) / maxSingle;
                for (int c = 0; c < totalChunks; c++)
                {
                    int offset = c * maxSingle;
                    int len = Math.Min(maxSingle, payload.Length - offset);
                    byte[] chunk = new byte[len + 5];
                    chunk[0] = 1;
                    chunk[1] = (byte)(msgId & 0xFF);
                    chunk[2] = (byte)((msgId >> 8) & 0xFF);
                    chunk[3] = (byte)c;
                    chunk[4] = (byte)totalChunks;
                    Buffer.BlockCopy(payload, offset, chunk, 5, len);
                    SendRawOnChannel(targetSteamId, chunk, k_nSteamNetworkingSend_ReliableNoNagle, 0);
                }
            }
        }

        private void SendRawOnChannel(ulong targetSteamId, byte[] bytes, int sendFlags, int channel)
        {
            if (_netSend == null || _identitySetSteamId64 == null) return;
            var ident = CreateIdentity(targetSteamId);
            unsafe
            {
                fixed (byte* p = bytes)
                {
                    _netSend(_netMsgPtr, ref ident, (IntPtr)p, (uint)bytes.Length, sendFlags, channel);
                }
            }
        }

        private SteamNetworkingIdentityBuf CreateIdentity(ulong steamId)
        {
            var buf = new SteamNetworkingIdentityBuf();
            _identityClear?.Invoke(ref buf);
            _identitySetSteamId64?.Invoke(ref buf, steamId);
            return buf;
        }

        private void AcceptPeer(ulong steamId)
        {
            if (_netAccept == null || _netMsgPtr == IntPtr.Zero || steamId == 0) return;
            var ident = CreateIdentity(steamId);
            _netAccept(_netMsgPtr, ref ident);
        }

        private void ClosePeer(ulong steamId)
        {
            if (_netClose == null || _netMsgPtr == IntPtr.Zero || steamId == 0) return;
            var ident = CreateIdentity(steamId);
            _netClose(_netMsgPtr, ref ident);
        }

        public void LeaveLobbyAndClearPeers()
        {
            if (IsInitialized && SessionActive)
            {
                if (IsLobbyHost)
                {
                    foreach (ulong peer in _steamToPid.Keys.ToArray())
                    {
                        SendControlPacket(peer, "goodbye");
                        ClosePeer(peer);
                    }
                }
                else if (HostSteamId != 0)
                {
                    SendControlPacket(HostSteamId, "goodbye");
                    ClosePeer(HostSteamId);
                }

                if (CurrentLobbyId != 0 && _matchLeaveLobby != null)
                {
                    _matchLeaveLobby(_matchPtr, CurrentLobbyId);
                }
                _friendsClearRichPresence?.Invoke(_friendsPtr);
            }

            SessionActive = false;
            IsLobbyHost = false;
            HandshakeCompleted = false;
            CurrentLobbyId = 0;
            HostSteamId = 0;
            _steamToPid.Clear();
            _pidToSteam.Clear();
            _peerLastSeen.Clear();
            _chunkBuffers.Clear();
            _chunkReceivedCounts.Clear();
            while (Inbox.TryDequeue(out _)) { }
        }

        public void Shutdown()
        {
            LeaveLobbyAndClearPeers();
            if (IsInitialized)
            {
                try { _shutdown?.Invoke(); } catch { }
                IsInitialized = false;
            }
        }

        private string GetFriendName(ulong friendSteamId)
        {
            if (_friendsPtr == IntPtr.Zero || _friendsGetFriendPersonaName == null) return "";
            return ReadUtf8Ptr(_friendsGetFriendPersonaName(_friendsPtr, friendSteamId));
        }

        private void SetLobbyDataString(ulong lobbyId, string key, string val)
        {
            if (_matchSetLobbyData == null || _matchPtr == IntPtr.Zero) return;
            WithUtf8(key, kPtr => WithUtf8(val, vPtr => _matchSetLobbyData(_matchPtr, lobbyId, kPtr, vPtr)));
        }

        private string GetLobbyDataString(ulong lobbyId, string key)
        {
            if (_matchGetLobbyData == null || _matchPtr == IntPtr.Zero) return "";
            string res = "";
            WithUtf8(key, kPtr =>
            {
                IntPtr ptr = _matchGetLobbyData(_matchPtr, lobbyId, kPtr);
                res = ReadUtf8Ptr(ptr);
            });
            return res;
        }

        private void SetRichPresenceString(string key, string val)
        {
            if (_friendsSetRichPresence == null || _friendsPtr == IntPtr.Zero) return;
            WithUtf8(key, kPtr => WithUtf8(val, vPtr => _friendsSetRichPresence(_friendsPtr, kPtr, vPtr)));
        }

        private static void WithUtf8(string text, Action<IntPtr> action)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes((text ?? "") + "\0");
            unsafe
            {
                fixed (byte* p = utf8)
                {
                    action((IntPtr)p);
                }
            }
        }

        private static string ReadUtf8Ptr(IntPtr ptr, int maxBytes = 512)
        {
            if (ptr == IntPtr.Zero) return "";
            int len = 0;
            while (len < maxBytes && Marshal.ReadByte(ptr, len) != 0)
                len++;
            if (len == 0) return "";
            byte[] buf = new byte[len];
            Marshal.Copy(ptr, buf, 0, len);
            return Encoding.UTF8.GetString(buf);
        }
    }
}
