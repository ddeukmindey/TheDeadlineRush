"""Generate M13a pixel placeholders: 16x16 tiles, 16x24 characters, icon E, UI frames, WAVs."""
from __future__ import annotations

import math
import os
import struct
import wave
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]  # Assets/_Project
ART = ROOT / "Art"
AUDIO = ROOT / "Audio"

# Palette — night campus, never magenta / default-gray
INK = (18, 14, 28, 255)
INK_SOFT = (28, 22, 42, 255)
WALL = (46, 40, 64, 255)
WALL_HI = (62, 54, 86, 255)
ACCENT = (232, 196, 120, 255)
ACCENT_DIM = (168, 132, 72, 255)
CREAM = (240, 228, 200, 255)
SHADOW = (10, 8, 16, 255)
PLAYER_SKIN = (220, 176, 132, 255)
PLAYER_HAIR = (32, 26, 38, 255)
PLAYER_HOODIE = (52, 86, 140, 255)
PLAYER_PANTS = (36, 40, 62, 255)
NPC_SHIRT = (148, 72, 78, 255)
NPC_HAIR = (72, 48, 36, 255)
UI_FILL = (24, 20, 36, 230)
UI_BORDER = (214, 186, 118, 255)
UI_INNER = (48, 40, 64, 255)
TRANSPARENT = (0, 0, 0, 0)

FLOOR = {
    "dorm": ((58, 48, 72, 255), (70, 58, 86, 255)),
    "hallway": ((48, 46, 58, 255), (58, 56, 70, 255)),
    "library": ((64, 46, 38, 255), (78, 56, 44, 255)),
    "lab": ((38, 54, 60, 255), (46, 66, 72, 255)),
    "canteen": ((70, 54, 40, 255), (84, 64, 46, 255)),
    "secret": ((36, 26, 44, 255), (48, 32, 58, 255)),
}


def new_img(w: int, h: int, fill=TRANSPARENT) -> Image.Image:
    return Image.new("RGBA", (w, h), fill)


def px(im: Image.Image, x: int, y: int, c) -> None:
    if 0 <= x < im.width and 0 <= y < im.height:
        im.putpixel((x, y), c)


def rect(im, x0, y0, x1, y1, c):
    for y in range(y0, y1):
        for x in range(x0, x1):
            px(im, x, y, c)


def hline(im, x0, x1, y, c):
    for x in range(x0, x1):
        px(im, x, y, c)


def vline(im, x, y0, y1, c):
    for y in range(y0, y1):
        px(im, x, y, c)


def save(im: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path, "PNG")
    print("wrote", path.relative_to(ROOT.parent.parent) if False else path)


def make_floor(a, b) -> Image.Image:
    im = new_img(16, 16)
    for y in range(16):
        for x in range(16):
            c = a if (x // 4 + y // 4) % 2 == 0 else b
            px(im, x, y, c)
    hline(im, 0, 16, 0, SHADOW)
    hline(im, 0, 16, 15, INK_SOFT)
    return im


def make_wall() -> Image.Image:
    im = new_img(16, 16, WALL)
    hline(im, 0, 16, 0, WALL_HI)
    hline(im, 0, 16, 1, ACCENT_DIM)
    for y in range(3, 16, 4):
        hline(im, 0, 16, y, INK_SOFT)
    vline(im, 7, 0, 16, INK)
    vline(im, 8, 0, 16, WALL_HI)
    return im


def make_carpet() -> Image.Image:
    im = new_img(16, 16, (86, 36, 48, 255))
    for y in range(16):
        for x in range(16):
            if (x + y) % 6 == 0:
                px(im, x, y, (110, 48, 58, 255))
    rect(im, 0, 0, 16, 1, INK)
    rect(im, 0, 15, 16, 16, INK)
    return im


def draw_letter_e(im, ox, oy, c):
    # 5x7 E
    for y in range(7):
        px(im, ox, oy + y, c)
    hline(im, ox, ox + 4, oy, c)
    hline(im, ox, ox + 3, oy + 3, c)
    hline(im, ox, ox + 4, oy + 6, c)


def make_icon_e() -> Image.Image:
    im = new_img(16, 16)
    rect(im, 1, 1, 15, 15, INK)
    rect(im, 2, 2, 14, 14, (48, 40, 70, 255))
    hline(im, 1, 15, 1, ACCENT)
    vline(im, 1, 1, 15, ACCENT)
    hline(im, 1, 15, 14, ACCENT_DIM)
    vline(im, 14, 1, 15, ACCENT_DIM)
    draw_letter_e(im, 6, 4, CREAM)
    return im


def make_frame_9slice() -> Image.Image:
    # 48x48, 8px border for 9-slice
    im = new_img(48, 48)
    rect(im, 0, 0, 48, 48, UI_BORDER)
    rect(im, 2, 2, 46, 46, INK)
    rect(im, 4, 4, 44, 44, UI_INNER)
    rect(im, 8, 8, 40, 40, UI_FILL)
    # corner studs
    for cx, cy in ((3, 3), (43, 3), (3, 43), (43, 43)):
        px(im, cx, cy, ACCENT)
    return im


def make_button() -> Image.Image:
    im = new_img(48, 16)
    rect(im, 0, 0, 48, 16, UI_BORDER)
    rect(im, 1, 1, 47, 15, (58, 48, 78, 255))
    hline(im, 1, 47, 1, ACCENT)
    hline(im, 1, 47, 14, ACCENT_DIM)
    return im


def make_panel_header() -> Image.Image:
    im = new_img(64, 12, INK)
    hline(im, 0, 64, 0, ACCENT)
    hline(im, 0, 64, 11, ACCENT_DIM)
    return im


def draw_human(im, ox, oy, facing: str, hoodie, hair, frame: int):
    """16x24 character occupying full canvas; ox,oy usually 0,0."""
    # shadow
    rect(im, ox + 4, oy + 22, ox + 12, oy + 24, (0, 0, 0, 90))

    leg_off = 0 if frame == 0 else (1 if facing in ("down", "up") else 0)
    arm_off = 0 if frame == 0 else 1

    # legs
    if facing in ("down", "up"):
        rect(im, ox + 5, oy + 16, ox + 8, oy + 22 - leg_off, PLAYER_PANTS)
        rect(im, ox + 8, oy + 16, ox + 11, oy + 22 + (leg_off - 1 if frame else 0), PLAYER_PANTS)
        px(im, ox + 5, oy + 21, SHADOW)
        px(im, ox + 10, oy + 21, SHADOW)
    elif facing == "right":
        rect(im, ox + 6, oy + 16, ox + 9, oy + 22, PLAYER_PANTS)
        rect(im, ox + 8 + frame, oy + 17, ox + 11 + frame, oy + 22, PLAYER_PANTS)
    else:
        rect(im, ox + 7, oy + 16, ox + 10, oy + 22, PLAYER_PANTS)
        rect(im, ox + 5 - frame, oy + 17, ox + 8 - frame, oy + 22, PLAYER_PANTS)

    # torso
    rect(im, ox + 4, oy + 9, ox + 12, oy + 17, hoodie)
    hline(im, ox + 5, ox + 11, oy + 10, (hoodie[0] + 20, hoodie[1] + 16, min(255, hoodie[2] + 20), 255))

    # arms
    if facing == "down":
        rect(im, ox + 3, oy + 10 + arm_off, ox + 5, oy + 16 + arm_off, hoodie)
        rect(im, ox + 11, oy + 10 - arm_off, ox + 13, oy + 16 - arm_off, hoodie)
        px(im, ox + 3, oy + 15 + arm_off, PLAYER_SKIN)
        px(im, ox + 12, oy + 15 - arm_off, PLAYER_SKIN)
    elif facing == "up":
        rect(im, ox + 3, oy + 10, ox + 5, oy + 16, hoodie)
        rect(im, ox + 11, oy + 10, ox + 13, oy + 16, hoodie)
    elif facing == "right":
        rect(im, ox + 11, oy + 10, ox + 14 + frame, oy + 16, hoodie)
        px(im, ox + 13 + frame, oy + 15, PLAYER_SKIN)
    else:
        rect(im, ox + 2 - frame, oy + 10, ox + 5, oy + 16, hoodie)
        px(im, ox + 2 - frame, oy + 15, PLAYER_SKIN)

    # head
    hx, hy = ox + 5, oy + 2
    rect(im, hx, hy, hx + 6, hy + 7, PLAYER_SKIN)
    if facing == "up":
        rect(im, hx, hy, hx + 6, hy + 5, hair)
    else:
        rect(im, hx, hy, hx + 6, hy + 3, hair)
        if facing == "down":
            px(im, hx + 1, hy + 4, INK)  # eyes
            px(im, hx + 4, hy + 4, INK)
            hline(im, hx + 2, hx + 4, hy + 6, (180, 90, 90, 255))
        elif facing == "right":
            px(im, hx + 4, hy + 4, INK)
            rect(im, hx, hy, hx + 2, hy + 6, hair)
        else:
            px(im, hx + 1, hy + 4, INK)
            rect(im, hx + 4, hy, hx + 6, hy + 6, hair)


def make_char(facing, hoodie, hair, frame=0) -> Image.Image:
    im = new_img(16, 24)
    draw_human(im, 0, 0, facing, hoodie, hair, frame)
    return im


def make_char_sheet(hoodie, hair, name: str) -> Image.Image:
    # 4 dirs x 2 frames, row-major: down, left, right, up
    sheet = new_img(64, 48)
    dirs = ["down", "left", "right", "up"]
    for i, d in enumerate(dirs):
        for f in range(2):
            ch = make_char(d, hoodie, hair, f)
            sheet.paste(ch, (i * 16, f * 24), ch)
    return sheet


def make_prop_bed() -> Image.Image:
    im = new_img(32, 24, TRANSPARENT)
    rect(im, 2, 8, 30, 22, (70, 58, 90, 255))
    rect(im, 3, 6, 29, 14, (120, 80, 90, 255))
    rect(im, 3, 4, 12, 12, CREAM)
    rect(im, 2, 20, 30, 23, INK)
    return im


def make_prop_computer() -> Image.Image:
    im = new_img(16, 16)
    rect(im, 2, 2, 14, 11, INK)
    rect(im, 3, 3, 13, 10, (40, 90, 80, 255))
    px(im, 6, 5, (120, 220, 160, 255))
    px(im, 7, 6, (120, 220, 160, 255))
    rect(im, 5, 11, 11, 13, WALL)
    rect(im, 3, 13, 13, 15, INK_SOFT)
    return im


def make_prop_phone() -> Image.Image:
    im = new_img(16, 16)
    rect(im, 5, 2, 11, 14, INK)
    rect(im, 6, 3, 10, 11, (48, 90, 110, 255))
    hline(im, 6, 10, 12, ACCENT)
    return im


def make_prop_door() -> Image.Image:
    im = new_img(16, 24, (72, 48, 36, 255))
    rect(im, 1, 1, 15, 23, (92, 62, 44, 255))
    vline(im, 8, 1, 23, (60, 38, 28, 255))
    px(im, 12, 12, ACCENT)
    px(im, 11, 12, ACCENT_DIM)
    return im


def make_prop_bookshelf() -> Image.Image:
    im = new_img(16, 24, (62, 42, 30, 255))
    for y in (2, 8, 14, 20):
        hline(im, 1, 15, y, INK)
        for x, c in ((2, (80, 40, 40, 255)), (5, (40, 60, 90, 255)), (8, (90, 70, 40, 255)), (11, (50, 70, 50, 255))):
            rect(im, x, y + 1, x + 3, y + 6, c)
    return im


def make_prop_counter() -> Image.Image:
    im = new_img(32, 16, (90, 70, 48, 255))
    hline(im, 0, 32, 0, CREAM)
    hline(im, 0, 32, 1, ACCENT_DIM)
    rect(im, 0, 10, 32, 16, INK_SOFT)
    return im


def make_prop_loop_record() -> Image.Image:
    im = new_img(16, 16)
    rect(im, 3, 2, 13, 14, INK)
    rect(im, 4, 3, 12, 13, (90, 78, 48, 255))
    for y in range(5, 12, 2):
        hline(im, 5, 11, y, INK_SOFT)
    px(im, 8, 4, (180, 40, 40, 255))
    return im


# ── Audio ──────────────────────────────────────────────────────────────────
def write_wav(path: Path, samples, rate=22050):
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        frames = b"".join(struct.pack("<h", max(-32767, min(32767, int(s * 32767)))) for s in samples)
        w.writeframes(frames)
    print("wrote", path)


def env(i, n, attack=0.01, release=0.05, rate=22050):
    a = int(attack * rate)
    r = int(release * rate)
    if i < a:
        return i / max(1, a)
    if i > n - r:
        return max(0.0, (n - i) / max(1, r))
    return 1.0


def tone(freq, dur, rate=22050, vol=0.25, wave_kind="sine"):
    n = int(dur * rate)
    out = []
    for i in range(n):
        t = i / rate
        if wave_kind == "sine":
            s = math.sin(2 * math.pi * freq * t)
        elif wave_kind == "square":
            s = 1.0 if math.sin(2 * math.pi * freq * t) >= 0 else -1.0
        else:
            s = math.sin(2 * math.pi * freq * t) * 0.7 + 0.3 * math.sin(4 * math.pi * freq * t)
        out.append(s * vol * env(i, n, 0.02, 0.08, rate))
    return out


def noise(dur, rate=22050, vol=0.12, brown=False):
    n = int(dur * rate)
    out = []
    acc = 0.0
    seed = 12345
    for i in range(n):
        seed = (1103515245 * seed + 12345) & 0x7FFFFFFF
        w = (seed / 0x7FFFFFFF) * 2 - 1
        if brown:
            acc = acc * 0.98 + w * 0.02
            s = acc * 8
        else:
            s = w
        out.append(max(-1, min(1, s)) * vol * env(i, n, 0.05, 0.1, rate))
    return out


def mix(*tracks):
    n = max(len(t) for t in tracks)
    out = [0.0] * n
    for t in tracks:
        for i, s in enumerate(t):
            out[i] += s
    m = max(1e-6, max(abs(x) for x in out))
    if m > 1:
        out = [x / m * 0.95 for x in out]
    return out


def loopable_pad(base_freq, dur=6.0, rate=22050, warmth=0.2):
    n = int(dur * rate)
    out = []
    for i in range(n):
        t = i / rate
        fade = 0.5 - 0.5 * math.cos(2 * math.pi * i / n)  # seamless-ish
        s = (
            math.sin(2 * math.pi * base_freq * t)
            + 0.35 * math.sin(2 * math.pi * base_freq * 1.5 * t)
            + 0.2 * math.sin(2 * math.pi * (base_freq * 0.5) * t)
            + warmth * math.sin(2 * math.pi * 0.15 * t)
        )
        out.append(s * 0.12 * fade)
    return out


def main():
    tiles = ART / "Placeholders" / "Tiles"
    chars = ART / "Placeholders" / "Characters"
    ui = ART / "Placeholders" / "UI"
    props = ART / "Placeholders" / "Props"

    for area, cols in FLOOR.items():
        save(make_floor(*cols), tiles / f"tile_floor_{area}.png")
    save(make_wall(), tiles / "tile_wall.png")
    save(make_carpet(), tiles / "tile_carpet.png")

    save(make_char_sheet(PLAYER_HOODIE, PLAYER_HAIR, "player"), chars / "player_walk_sheet.png")
    save(make_char_sheet(NPC_SHIRT, NPC_HAIR, "npc"), chars / "npc_walk_sheet.png")
    for d in ("down", "left", "right", "up"):
        save(make_char(d, PLAYER_HOODIE, PLAYER_HAIR, 0), chars / f"player_idle_{d}.png")
    save(make_char("down", NPC_SHIRT, NPC_HAIR, 0), chars / "npc_idle_down.png")

    save(make_icon_e(), ui / "icon_interact_e.png")
    save(make_frame_9slice(), ui / "frame_panel_9slice.png")
    save(make_button(), ui / "frame_button.png")
    save(make_panel_header(), ui / "frame_header.png")

    save(make_prop_bed(), props / "prop_bed.png")
    save(make_prop_computer(), props / "prop_computer.png")
    save(make_prop_phone(), props / "prop_phone.png")
    save(make_prop_door(), props / "prop_door.png")
    save(make_prop_bookshelf(), props / "prop_bookshelf.png")
    save(make_prop_counter(), props / "prop_counter.png")
    save(make_prop_loop_record(), props / "prop_loop_record.png")

    music = AUDIO / "Music"
    amb = AUDIO / "Ambience"
    sfx = AUDIO / "SFX"
    write_wav(music / "bgm_phase1.wav", loopable_pad(110, 8, warmth=0.35))  # warm dusk
    write_wav(music / "bgm_phase2.wav", loopable_pad(98, 8, warmth=0.15))
    write_wav(music / "bgm_phase3.wav", mix(loopable_pad(82, 8, warmth=0.05), noise(8, vol=0.04, brown=True)))
    write_wav(music / "bgm_phase4.wav", mix(loopable_pad(55, 8, warmth=0.0), noise(8, vol=0.06, brown=True)))
    write_wav(amb / "amb_dorm.wav", noise(8, vol=0.08, brown=True))
    write_wav(amb / "amb_whisper.wav", mix(noise(4, vol=0.05), tone(180, 4, vol=0.04, wave_kind="sine")))
    write_wav(amb / "amb_heartbeat.wav", mix(tone(55, 0.18, vol=0.35), [0] * 4000, tone(48, 0.22, vol=0.28)))
    write_wav(sfx / "sfx_knock.wav", mix(tone(180, 0.08, vol=0.4, wave_kind="square"), [0] * 1200, tone(160, 0.08, vol=0.35, wave_kind="square"), [0] * 1200, tone(150, 0.1, vol=0.3, wave_kind="square")))
    write_wav(sfx / "sfx_phone.wav", mix(tone(880, 0.12, vol=0.22), [0] * 800, tone(880, 0.12, vol=0.22)))
    write_wav(sfx / "sfx_computer.wav", mix(tone(520, 0.05, vol=0.18, wave_kind="square"), tone(740, 0.08, vol=0.16, wave_kind="square")))
    write_wav(sfx / "sfx_ui_click.wav", noise(0.04, vol=0.2, brown=False))
    write_wav(sfx / "sfx_glitch.wav", mix(noise(0.35, vol=0.18), tone(90, 0.35, vol=0.1, wave_kind="square")))

    readme = ART / "Placeholders" / "README_PLACEHOLDERS.txt"
    readme.write_text(
        """THE DEADLINE RUSH — Placeholder Art (Agent D / M13a)
====================================================
PPU = 16 for ALL sprites below. Filter Mode = Point, Compression = None, Mip Maps OFF.

Tiles (16x16) .......... Art/Placeholders/Tiles/
  tile_floor_dorm / hallway / library / lab / canteen / secret
  tile_wall.png
  tile_carpet.png

Characters (16x24) ..... Art/Placeholders/Characters/
  player_idle_{down,left,right,up}.png
  npc_idle_down.png
  player_walk_sheet.png  — 64x48, grid 16x24, 4 columns (D,L,R,U) x 2 rows (idle, step)
  npc_walk_sheet.png     — same layout

UI ..................... Art/Placeholders/UI/
  icon_interact_e.png    — 16x16, gán vào Interactable prompt "E"
  frame_panel_9slice.png — 48x48, border 8px (Slice)
  frame_button.png       — 48x16
  frame_header.png       — 64x12

Props .................. Art/Placeholders/Props/
  prop_bed, prop_computer, prop_phone, prop_door, prop_bookshelf,
  prop_counter, prop_loop_record

Thay pixel art final: giữ NGUYÊN tên file + PPU để Prefab/Scene không gãy (M13b).
""",
        encoding="utf-8",
    )
    print("done")


if __name__ == "__main__":
    main()
