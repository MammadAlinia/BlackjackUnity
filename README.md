# Blackjack Unity client

This is an offline UI shell. It does not connect to a server or perform authentication, room management, chat, or blackjack gameplay. The mock services return success for local navigation, return an empty room list, and do not persist or transmit data.

## Run

1. Open the project in Unity **6000.6.0f1** and open `Assets/Scenes/SampleScene.unity`.
2. If needed, run **Blackjack → Configure current scene** to wire the UI and preserved card graphics into the scene.
3. Enter Play Mode. Sign in or create an account with any values, browse the empty room list, and use the table screen as an inert UI preview.

## Structure

- `Services`: Unity-local results and concrete offline mock services.
- `UI`: composed UXML/USS screens and local navigation.
- `Scripts`: Reflex composition, application lifetime, and preserved card graphics. The table presenter intentionally creates no cards.
- `Editor`: scene setup and Unity Test Runner hooks.
- `Tests`: offline service, UI lifecycle, and preserved card-asset checks.

## Verify

Run `Blackjack.EditTests` and `Blackjack.PlayTests` from Unity's Test Runner. **Blackjack → Build Windows IL2CPP** builds the current scene without a backend fixture.
