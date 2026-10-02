# Blackjack Unity client

The game uses the existing `CardGameServer` backend and `CardGame.Client` library. Unity renders server state; cards, scores, turns, room membership, reconnect leases and command receipts belong to the server.

## Run

1. Configure and run the server using `../CardGameServer/README.md`. The default HTTPS address is `https://localhost:7093`.
2. Open this project in Unity **6000.6.0f1** and open `Assets/Scenes/SampleScene.unity`.
3. Script import wires UI Toolkit, Reflex and the existing card assets into the current scene. Repeat through **Blackjack → Configure current scene** if needed.
4. Enter Play Mode. Register or sign in using email/password or Google. Find/create a room, ready up, and use Hit/Stand when offered by the server.

The login screen accepts another server address and remembers the last successful address. Startup restores the saved refresh token; rotation updates storage. Transient failures preserve credentials. Sign-out deletes them. PlayerPrefs implements the requested storage boundary and stores credentials in its normal local format; passwords are never stored.

Google opens the system browser. The existing OAuth callback validates the account, then sends a short-lived, single-use code to a temporary loopback listener. Unity exchanges it using a PKCE verifier for Identity tokens. The Google client secret remains server-side. Google's authorized redirect remains the server's `/signin-google` URL. Account conflicts and two-factor restrictions retain existing server behavior.

## Update the shared client

```powershell
./Tools/Import-CardGameClient.ps1 -EditorPath 'E:/Programs/Unity/Hub/Editor/6000.6.0f1/Editor'
```

The script builds source in the sibling server repository and imports resolved runtime DLLs. It preserves existing metadata and removes obsolete imports using its inventory. Unity's built-in framework/BCL extensions are excluded. Shared libraries retain the .NET Standard 2.0 target and original dependencies for the Framework console client; the .NET Standard 2.1 target uses SignalR 8 for Unity's BCL compatibility. JSON metadata is generated for IL2CPP.

## Structure

- `Services`: result contracts, SDK adapter, authentication, asynchronous storage/persistence, rooms, game sessions, chat and main-thread dispatch.
- `UI`: composed UXML/USS, navigation and disposable controllers using service interfaces.
- `Scripts`: Reflex composition, application lifetime and card/table presentation. The table never evaluates blackjack rules.
- `Editor`: idempotent scene setup and verification hooks for an open Editor.
- `Tests`: result/storage/card mapping/lifecycle tests and real two-client integration.
- `Verification`: integration scenarios, included only in Editor/development builds.

## Verify

```powershell
dotnet test ../CardGameServer/CardGameServer.sln
dotnet build ../CardGameServer/CardGameServer.sln
```

Run `Blackjack.EditTests` in Unity's Test Runner. For live Play Mode tests, first start the isolated fixture:

```powershell
dotnet run --project Tools/SmokeServer/SmokeServer.csproj
```

It hosts the existing server with an in-memory database and writes its random loopback address to `Temp/Blackjack-smoke-server.txt`. Run `Blackjack.PlayTests` to exercise authentication, room creation/join, forced reconnect, connection takeover/rejoin, chat, two rounds, closure, restoration and logout. Stop the fixture with Ctrl+C afterward.

**Blackjack → Build Windows IL2CPP** creates `Builds/Windows/Blackjack.exe`. Run the same integration scenario in the development player:

```powershell
$server = Get-Content Temp/Blackjack-smoke-server.txt
./Builds/Windows/Blackjack.exe -batchmode -nographics -blackjackSmoke $server
Get-Content Builds/Windows/smoke-result.txt
```

This explicit argument runs verification and exits. A normal launch shows the UI. Real Google consent and browser callbacks also require a manual check with your configured OAuth client.

For rendered player screenshots, run `./Builds/Windows/Blackjack.exe -blackjackPreview $server`. This development-only check signs in with a temporary fixture account, captures Home/Rooms/Gameplay into `Builds/Windows/Temp`, closes its room and exits. Keep its graphics window visible; omit `-batchmode` and `-nographics`. Hidden windows can suppress rendering and produce black captures. Direct3D 11 (`-force-d3d11`) can be used if Direct3D 12 screenshot capture is unavailable.

To script an open Editor, write an action (`configure`, `open-scene`, `edit-tests`, `play-tests`, `build`, `play`, `stop`, `capture`, `ui-preview`) to `Temp/Blackjack-command.json`, e.g. `{"action":"edit-tests"}`. Read `Temp/Blackjack-status.json` and `Temp/Blackjack-test-results.xml`. Wait for compilation/import to finish before issuing commands. `ui-preview` requires Play Mode and the isolated fixture; it captures Home, Rooms and live Gameplay, then closes its test room and signs out.

### Acceptance checks (2026-10-02)

- Server solution: clean build; 75 tests passed.
- Unity Editor: 14 Edit Mode tests and the live two-client Play Mode scenario passed.
- Windows x64 IL2CPP development player: build succeeded; the same live scenario passed, including duplex streams, reconnect, takeover, explicit rejoin, chat, consecutive rounds, persisted restoration and logout.
- Google grant exchange: automated callback/account checks, proof rejection, expiry and replay tests passed. Real Google browser consent still requires manual verification with configured credentials.
- Visual checks: the Editor login layout was inspected. Full Home/Rooms/Gameplay visual acceptance requires a visible game window; hidden-player captures were black and were not counted as visual validation.

Run the player with normal Windows user access. PlayerPrefs requires write access to the user's registry; a restricted command sandbox can correctly produce a `Storage` result while authentication remains usable. Editor and player validation are separate checks.
