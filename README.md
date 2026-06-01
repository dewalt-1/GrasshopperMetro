# Metro suite — Max/MSP-style components for Grasshopper

A small collection of time-based Grasshopper components ported from Max/MSP idioms. Currently includes:

- **Metro** — interval clock with counter, elapsed time, and a per-tick Bang
- **Tempo** — like Metro, but specified in BPM instead of milliseconds
- **Delay** — delays an incoming Bang by N milliseconds
- **Cycle** — sine wave LFO driven by a Count input
- **Phasor** — sawtooth ramp LFO driven by a Count input
- **Noise** — random value generator driven by a Count input

This is a portfolio / experimental project, not a production plugin.

## Metro

### Inputs

| Pin | Type | Default | Description |
|---|---|---|---|
| Run (R) | bool | false | Start/stop the timer |
| Interval (I) | int (ms) | 500 | Time between ticks; clamped to ≥ 1 ms |
| Reset (X) | bool | false | When true, zero the counter and elapsed time |
| Max Count (M) | int | 0 | Auto-stop after N ticks; 0 means run forever |
| Bang Every (BE) | int | 1 | Fire Bang only every Nth tick; 1 = every tick |

### Outputs

| Pin | Type | Description |
|---|---|---|
| Count (C) | int | Tick counter — 0 on start/reset, increments by 1 each tick |
| Elapsed (E) | double (s) | Wall-clock seconds since the current run began |
| Bang (B) | bool | True only on solves triggered by a timer tick |

## Tempo

Same behavior as Metro, but you specify the rate in beats per minute.

### Inputs

| Pin | Type | Default | Description |
|---|---|---|---|
| Run (R) | bool | false | Start/stop the timer |
| Reset (X) | bool | false | When true, restart the beat counter on this solve |
| BPM (BPM) | double | 120 | Beats per minute; clamped to ≥ 1 |
| Bang Every (BE) | list of int | empty | Rhythm pattern that repeats forever. List = beat positions to fire on; cycle length = max(list). `[3]` → beats 3, 6, 9, … `[2, 4]` → beats 2, 4, 6, 8, … (backbeat). `[1, 3]` → beats 1, 3, 4, 6, 7, 9, … (positions 1 & 3 of a 3-beat cycle). Empty / disconnected = every beat |

### Outputs

| Pin | Type | Description |
|---|---|---|
| Count (C) | int | Beat counter — 0 on start/reset, increments by 1 per beat |
| Bang (B) | bool | True only on beats matching the rhythm pattern in Bang Every |

## Delay

Delays an incoming Bang by N milliseconds. Wire any pulsing Bang output into Delay's Bang input — each rising edge schedules a delayed re-emission.

### Inputs

| Pin | Type | Default | Description |
|---|---|---|---|
| Run (R) | bool | false | Enable — when false, incoming bangs are ignored |
| Reset (X) | bool | false | Cancel any pending fire and zero Count |
| Bang (B) | bool | false | Trigger input. Each rising edge (false→true) while Run is true schedules a delayed fire |
| Delay (D) | int (ms) | 1000 | Wait time before re-emitting Bang; clamped to ≥ 1 ms |

### Outputs

| Pin | Type | Description |
|---|---|---|
| Count (C) | int | Number of times Delay has fired since start/reset |
| Bang (B) | bool | True on the single solve where the delayed bang fires |

**Chaining note.** Delay detects rising edges, so the upstream Bang source must actually pulse (true → false → true). To chain from Metro or Tempo, set their `Bang Every` input to ≥ 2 — at the default of 1, those Bang outputs stay true between solves and there's no edge to detect after the first one.

## Cycle

Sine wave LFO. Wire a Metro or Tempo Count into its Count input to drive it at a controlled rate, or leave Count unwired for an internal ~30 Hz fallback.

### Inputs

| Pin | Type | Default | Description |
|---|---|---|---|
| Run (R) | bool | false | Enable — when false, the output freezes at its last value |
| Reset (X) | bool | false | When self-scheduling, zeros the internal counter (so phase restarts at 0) |
| Count (C) | int | unwired | Optional upstream clock. If wired, drives the component. If unwired, ~30 Hz fallback |
| Frequency (F) | int | 30 | Counts per cycle. Higher = slower wave. Clamped to ≥ 1 |

### Outputs

| Pin | Type | Description |
|---|---|---|
| Value (V) | double | Sine wave value in [-1, 1] |

## Phasor

Sawtooth ramp LFO. Same interface as Cycle, different waveform.

### Inputs

| Pin | Type | Default | Description |
|---|---|---|---|
| Run (R) | bool | false | Enable — when false, the output freezes at its last value |
| Reset (X) | bool | false | When self-scheduling, zeros the internal counter (so the ramp restarts at 0) |
| Count (C) | int | unwired | Optional upstream clock |
| Frequency (F) | int | 30 | Counts per cycle. Higher = slower ramp. Clamped to ≥ 1 |

### Outputs

| Pin | Type | Description |
|---|---|---|
| Value (V) | double | Sawtooth ramp in [0, 1) — wraps to 0 at each integer phase |

## Noise

Uniform random value in [0, 1]. Generates a fresh value each time Count changes (or each internal tick when self-scheduling).

### Inputs

| Pin | Type | Default | Description |
|---|---|---|---|
| Run (R) | bool | false | Enable — when false, the output freezes at its last value |
| Reset (X) | bool | false | Zeros the internal counter when self-scheduling |
| Count (C) | int | unwired | Optional upstream clock. If wired, each new value of Count produces a new random. If unwired, ~30 Hz fallback |

### Outputs

| Pin | Type | Description |
|---|---|---|
| Value (V) | double | Uniform random in [0, 1] |

## Build

Requires the .NET 7 SDK and a copy of Rhino 8 to test against. If you don't have .NET 7 installed, the easiest path on macOS is:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 7.0 --install-dir "$HOME/.dotnet"
```

Then build:

```bash
"$HOME/.dotnet/dotnet" restore
"$HOME/.dotnet/dotnet" build -c Release
```

Output: `bin/Release/net7.0/Metro.gha`

The project ships a `global.json` that pins SDK 7.0.410, so any other dotnet installs on your machine won't be picked up.

## Install (macOS)

If you don't want to build from source, grab the prebuilt `dist/Metro.gha` from this repo (Code → Download ZIP, or `git clone`).

Drop `Metro.gha` into Grasshopper's user library folder. On Rhino 8 macOS this folder is suffixed with the Grasshopper plugin GUID, which is the same on every install:

```bash
# If you downloaded the prebuilt release:
cp dist/Metro.gha \
   "$HOME/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries/"

# Or if you built from source:
cp bin/Release/net7.0/Metro.gha \
   "$HOME/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries/"
```

Then restart Rhino. The components appear under **Params → Util** as "Metro", "Tempo", "Delay", "Cycle", "Phasor", and "Noise".

If Grasshopper refuses to load the plugin, clear the macOS quarantine attribute:

```bash
xattr -d com.apple.quarantine \
   "$HOME/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries/Metro.gha"
```

## Usage notes

- The `Bang` outputs are the closest GH equivalent of a Max/MSP bang: `true` only on solves that were triggered by the internal timer callback, `false` on solves caused by user input (e.g., dragging a slider). Gate downstream "do something per tick" logic on `Bang`.
- Changing a rate input (Metro's Interval, Tempo's BPM, Delay's Delay) while running takes effect on the *next* tick — the in-flight scheduled callback uses the previous value. This is intentional; without it, dragging the slider would queue up runaway ticks. For Delay specifically, the in-flight wait keeps the original Delay value; the next incoming bang edge will use the new value.
- All components from the suite tick independently. You can run multiple of any type on the same canvas without interference.
- The signal-generator components (Cycle, Phasor, Noise) take an optional Count input. Wire Metro's or Tempo's Count output into it to drive them at a controlled rate, or leave it unwired for an internal ~30 Hz fallback.
