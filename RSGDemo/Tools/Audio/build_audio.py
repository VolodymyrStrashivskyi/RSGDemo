# Prepares the game's audio from the downloaded CC0 files (and a few synthesized loops) for Unity.
# Usage: python -P build_audio.py <download_dir> <out_dir>  (download_dir holds the CC0 files listed in Assets/_Project/Art/Audio/CREDITS.md, archives extracted into x/)
import os
import subprocess
import sys
import tempfile

import numpy as np
from scipy import signal
from scipy.io import wavfile

SRC, OUT = sys.argv[1], sys.argv[2]
SR = 44100
RNG = np.random.default_rng(7)
os.makedirs(OUT, exist_ok=True)


def load(path, mono=True):
    tmp = os.path.join(tempfile.gettempdir(), "aud_load.wav")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", path, "-ar", str(SR), "-ac", "1" if mono else "2",
                    "-c:a", "pcm_f32le", tmp], check=True)
    sr, data = wavfile.read(tmp)
    return data.astype(np.float32)


def save(name, data, quality=6):
    data = np.asarray(data, dtype=np.float32)
    peak = np.max(np.abs(data)) + 1e-9
    data = data / peak * 0.89
    tmp = os.path.join(tempfile.gettempdir(), "aud_save.wav")
    wavfile.write(tmp, SR, data)
    path = os.path.join(OUT, name + ".ogg")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp, "-c:a", "libvorbis", "-q:a", str(quality), path],
                   check=True)
    print(f"{name}: {len(data) / SR:.2f}s")


def seamless(data, fade_seconds):
    """Make a loop seamless: crossfade the tail into the head (equal power)."""
    n = int(fade_seconds * SR)
    head, body, tail = data[:n], data[n:-n] if n else data, data[-n:]
    t = np.linspace(0, np.pi / 2, n, dtype=np.float32)
    if data.ndim == 2:
        t = t[:, None]
    mixed = tail * np.cos(t) + head * np.sin(t)
    return np.concatenate([mixed, data[n:-n]])


def trim(data, start, end):
    return data[int(start * SR):int(end * SR)]


def fade(data, fade_in=0.005, fade_out=0.05):
    data = data.copy()
    a, b = int(fade_in * SR), int(fade_out * SR)
    if a:
        data[:a] *= np.linspace(0, 1, a, dtype=np.float32)[:, None] if data.ndim == 2 else np.linspace(0, 1, a)
    if b:
        data[-b:] *= np.linspace(1, 0, b, dtype=np.float32)[:, None] if data.ndim == 2 else np.linspace(1, 0, b)
    return data


def band(data, low, high, order=4):
    sos = signal.butter(order, [low, high], btype="bandpass", fs=SR, output="sos")
    return signal.sosfilt(sos, data, axis=0).astype(np.float32)


def lowpass(data, cutoff, order=4):
    sos = signal.butter(order, cutoff, btype="lowpass", fs=SR, output="sos")
    return signal.sosfilt(sos, data, axis=0).astype(np.float32)


def highpass(data, cutoff, order=4):
    sos = signal.butter(order, cutoff, btype="highpass", fs=SR, output="sos")
    return signal.sosfilt(sos, data, axis=0).astype(np.float32)


def resample_speed(data, factor):
    """Play faster (>1) or slower (<1): pitch and tempo together."""
    n = int(len(data) / factor)
    return signal.resample(data, n, axis=0).astype(np.float32)


def smooth_noise(seconds, rate_hz):
    """Slowly wandering 0..1 control signal."""
    n = int(seconds * SR)
    points = int(seconds * rate_hz) + 2
    knots = RNG.random(points)
    x = np.linspace(0, points - 1, n)
    return np.interp(x, np.arange(points), knots).astype(np.float32)


# ---------------------------------------------------------------- fire
fire = load(os.path.join(SRC, "fire.wav"))
save("Fire_Loop", seamless(fire, 1.0))
roar = resample_speed(fire, 0.72)
roar = lowpass(roar, 2400) * 0.7 + lowpass(highpass(RNG.standard_normal(len(roar)).astype(np.float32), 40), 300) * 0.05
save("Fire_RoarLoop", seamless(roar, 1.2))
crackle = load(os.path.join(SRC, "fire-1.wav"))
save("Fire_Ignite", fade(np.concatenate([crackle[:int(1.4 * SR)]]), 0.002, 0.4))

# whoosh for ignition / reignition: rising filtered noise + crackle
n = int(1.2 * SR)
noise = RNG.standard_normal(n).astype(np.float32)
env = np.concatenate([np.linspace(0, 1, int(0.25 * SR)) ** 2, np.exp(-np.linspace(0, 4, n - int(0.25 * SR)))])
whoosh = band(noise, 150, 2500) * env.astype(np.float32)
save("Fire_Whoosh", fade(whoosh + 0.6 * np.pad(crackle[:n], (0, max(0, n - len(crackle))))[:n] * env, 0.01, 0.2))

# collapse when a spot burns out: low thud + crackle tail
impact_dir = os.path.join(SRC, "x", "impact", "Audio")
thud = load(os.path.join(impact_dir, "impactWood_heavy_002.ogg"))
thud = lowpass(resample_speed(thud, 0.7), 1800)
tail = crackle[:int(1.5 * SR)] * np.linspace(1, 0, int(1.5 * SR), dtype=np.float32) * 0.6
collapse = np.zeros(int(1.8 * SR), dtype=np.float32)
collapse[:len(thud)] += thud[:len(collapse)]
collapse[int(0.1 * SR):int(0.1 * SR) + len(tail)] += tail
save("Fire_Collapse", fade(collapse))

# steam hiss when water hits a hot spot
n = int(2.0 * SR)
hiss = highpass(RNG.standard_normal(n).astype(np.float32), 2500) * (0.7 + 0.3 * smooth_noise(2.0, 12))
env = np.concatenate([np.linspace(0, 1, int(0.05 * SR)), np.exp(-np.linspace(0, 3, n - int(0.05 * SR)))])
save("Fire_SteamHiss", fade(hiss * env.astype(np.float32)))

# ---------------------------------------------------------------- water
flow = load(os.path.join(SRC, "waterflow.mp3"))
seconds = 6.0
n = int(seconds * SR)
white = RNG.standard_normal(n).astype(np.float32)
spray = band(white, 900, 9000) * (0.75 + 0.25 * smooth_noise(seconds, 9))
body = band(np.tile(flow, 2)[:n], 200, 6000)
save("Water_SprayLoop", seamless(spray * 0.55 + body * 0.6, 0.6))

splat = band(RNG.standard_normal(n).astype(np.float32), 300, 5000) * (0.6 + 0.4 * smooth_noise(seconds, 14))
save("Water_ImpactLoop", seamless(splat * 0.5 + lowpass(np.tile(flow, 2)[:n], 3000) * 0.8, 0.6))

gush = lowpass(resample_speed(np.tile(flow, 3), 0.8), 2500)[:n]
gush = gush + band(RNG.standard_normal(n).astype(np.float32), 150, 1200) * 0.25
save("Water_HydrantLoop", seamless(gush, 0.6))

splash_dir = os.path.join(SRC, "x", "splash", "ezwa-water_splash")
for i in (1, 3, 5):
    save(f"Water_Splash_{i:02d}", fade(load(os.path.join(splash_dir, f"water_splash-{i:02d}.flac"))))

# ---------------------------------------------------------------- machines
seconds = 4.0
t = np.arange(int(seconds * SR)) / SR
base = 50.0
hum = sum(np.sin(2 * np.pi * base * k * t + RNG.random() * 6) / k ** 1.1 for k in range(1, 12)).astype(np.float32)
hum = lowpass(hum, 900) + lowpass(RNG.standard_normal(len(t)).astype(np.float32), 400) * 0.15
hum *= (1 + 0.06 * np.sin(2 * np.pi * 12.5 * t)).astype(np.float32)
save("Truck_PumpLoop", seamless(hum, 0.5))

# ---------------------------------------------------------------- music (stereo, kept as authored)
for src_name, dst in (("loopcity_0.ogg", "Music_Calm"),
                      ("action%20game%20theme.wav", "Music_Tension"),
                      ("determined_pursuit_loop.wav", "Music_Intense")):
    save(dst, load(os.path.join(SRC, src_name), mono=False), quality=5)

# ---------------------------------------------------------------- Kenney one-shots (mono, normalised)
ui_dir = os.path.join(SRC, "x", "ui", "Audio")
jingle_dir = os.path.join(SRC, "x", "jingles", "OGG")
one_shots = {
    "Hose_ReelBrake": os.path.join(impact_dir, "impactMetal_heavy_001.ogg"),
    "Hose_CouplingConnect": os.path.join(impact_dir, "impactMetal_medium_002.ogg"),
    "Hydrant_Valve": os.path.join(impact_dir, "impactMetal_light_003.ogg"),
    "Item_PickUp": os.path.join(impact_dir, "impactPlate_light_001.ogg"),
    "Item_Drop": os.path.join(impact_dir, "impactPlate_medium_002.ogg"),
    "Door_Move": os.path.join(impact_dir, "impactWood_medium_001.ogg"),
    "UI_DispatchAlert": os.path.join(ui_dir, "question_001.ogg"),
    "UI_FireOut": os.path.join(jingle_dir, "jingles_SAX", "jingles_SAX07.ogg"),
}
for name, path in one_shots.items():
    save(name, fade(load(path), 0.0, 0.03))
for surface in ("concrete", "grass", "wood"):
    for i in range(5):
        step = load(os.path.join(impact_dir, f"footstep_{surface}_00{i}.ogg"))
        save(f"Footstep_{surface.capitalize()}_{i}", fade(lowpass(step, 2600), 0.004, 0.03))
