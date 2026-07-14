from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "custom-character" / "source" / "rimuru-original.png"
SPRITES = ROOT / "custom-character" / "sprites"
CHARSEL = ROOT / "custom-character" / "charsel.png"
DEMON_SKIN = ROOT / "custom-character" / "skins" / "demon_lord"
DEMON_SOURCE = DEMON_SKIN / "source" / "RimuruLordDemonio.png"
SLIME_SKIN = ROOT / "custom-character" / "skins" / "slime"
SLIME_SOURCE = SLIME_SKIN / "source" / "RimuruSlimeReference.png"
HUMANOID_SKIN = ROOT / "custom-character" / "skins" / "humanoid"
RANGA_ROOT = ROOT / "assets" / "summons" / "ranga"
RANGA_SOURCE = RANGA_ROOT / "source" / "RangaReference.png"
RANGA_CHARGE = RANGA_ROOT / "ranga_charge.png"
WEAPON_ROOT = ROOT / "assets" / "weapons"
WEAPON_SOURCE = WEAPON_ROOT / "source" / "rimuru-weapon-sheet-source.png"


def remove_connected_background(image: Image.Image, threshold: int = 8) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    queue: deque[tuple[int, int]] = deque()
    visited = bytearray(width * height)

    def is_background(x: int, y: int) -> bool:
        r, g, b, _ = pixels[x, y]
        return r <= threshold and g <= threshold and b <= threshold

    def enqueue(x: int, y: int) -> None:
        index = y * width + x
        if not visited[index] and is_background(x, y):
            visited[index] = 1
            queue.append((x, y))

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(height):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        x, y = queue.popleft()
        pixels[x, y] = (0, 0, 0, 0)
        if x > 0:
            enqueue(x - 1, y)
        if x + 1 < width:
            enqueue(x + 1, y)
        if y > 0:
            enqueue(x, y - 1)
        if y + 1 < height:
            enqueue(x, y + 1)

    return rgba


def remove_connected_checkerboard(image: Image.Image) -> Image.Image:
    """Remove a light neutral checkerboard while preserving enclosed white details."""
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    queue: deque[tuple[int, int]] = deque()
    visited = bytearray(width * height)

    def is_background(x: int, y: int) -> bool:
        r, g, b, _ = pixels[x, y]
        return min(r, g, b) >= 220 and max(r, g, b) - min(r, g, b) <= 18

    def enqueue(x: int, y: int) -> None:
        index = y * width + x
        if not visited[index] and is_background(x, y):
            visited[index] = 1
            queue.append((x, y))

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(height):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        x, y = queue.popleft()
        pixels[x, y] = (0, 0, 0, 0)
        if x > 0:
            enqueue(x - 1, y)
        if x + 1 < width:
            enqueue(x + 1, y)
        if y > 0:
            enqueue(x, y - 1)
        if y + 1 < height:
            enqueue(x, y + 1)

    return rgba


def remove_connected_dark_background(image: Image.Image) -> Image.Image:
    """Remove a dark connected backdrop without deleting brighter internal details."""
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    queue: deque[tuple[int, int]] = deque()
    visited = bytearray(width * height)

    def is_background(x: int, y: int) -> bool:
        r, g, b, _ = pixels[x, y]
        return r <= 32 and g <= 48 and b <= 72

    def enqueue(x: int, y: int) -> None:
        index = y * width + x
        if not visited[index] and is_background(x, y):
            visited[index] = 1
            queue.append((x, y))

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(height):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        x, y = queue.popleft()
        pixels[x, y] = (0, 0, 0, 0)
        if x > 0:
            enqueue(x - 1, y)
        if x + 1 < width:
            enqueue(x + 1, y)
        if y > 0:
            enqueue(x, y - 1)
        if y + 1 < height:
            enqueue(x, y + 1)

    return rgba


def remove_chroma_green(image: Image.Image) -> Image.Image:
    """Remove the generated flat green background without touching cyan weapon pixels."""
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, _ = pixels[x, y]
            green_dominance = g - max(r, b)
            if g >= 135 and green_dominance >= 38:
                alpha = max(0, 255 - (green_dominance - 38) * 5)
                pixels[x, y] = (r, min(g, max(r, b)), b, alpha)
    return rgba


def retain_largest_component(image: Image.Image) -> Image.Image:
    """Discard detached decorations such as the small slime in the reference panel."""
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    pixels = alpha.load()
    width, height = rgba.size
    visited = bytearray(width * height)
    largest: list[tuple[int, int]] = []

    for start_y in range(height):
        for start_x in range(width):
            start_index = start_y * width + start_x
            if visited[start_index] or pixels[start_x, start_y] == 0:
                continue
            queue: deque[tuple[int, int]] = deque([(start_x, start_y)])
            visited[start_index] = 1
            component: list[tuple[int, int]] = []
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for neighbor_x, neighbor_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if not (0 <= neighbor_x < width and 0 <= neighbor_y < height):
                        continue
                    index = neighbor_y * width + neighbor_x
                    if not visited[index] and pixels[neighbor_x, neighbor_y] != 0:
                        visited[index] = 1
                        queue.append((neighbor_x, neighbor_y))
            if len(component) > len(largest):
                largest = component

    if not largest:
        raise ValueError("The source image has no visible component")

    keep = set(largest)
    rgba_pixels = rgba.load()
    for y in range(height):
        for x in range(width):
            if (x, y) not in keep:
                r, g, b, _ = rgba_pixels[x, y]
                rgba_pixels[x, y] = (r, g, b, 0)
    return rgba


def extract_primary_slime(image: Image.Image) -> Image.Image:
    """Extract the large cyan slime and discard the panel border and small companion slime."""
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    visited = bytearray(width * height)
    largest: list[tuple[int, int]] = []

    def is_anchor(x: int, y: int) -> bool:
        r, g, b, a = pixels[x, y]
        return a > 0 and b >= 150 and g >= 110 and b >= r + 30

    for start_y in range(height):
        for start_x in range(width):
            start_index = start_y * width + start_x
            if visited[start_index] or not is_anchor(start_x, start_y):
                continue
            queue: deque[tuple[int, int]] = deque([(start_x, start_y)])
            visited[start_index] = 1
            component: list[tuple[int, int]] = []
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for neighbor_x, neighbor_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if not (0 <= neighbor_x < width and 0 <= neighbor_y < height):
                        continue
                    index = neighbor_y * width + neighbor_x
                    if not visited[index] and is_anchor(neighbor_x, neighbor_y):
                        visited[index] = 1
                        queue.append((neighbor_x, neighbor_y))
            if len(component) > len(largest):
                largest = component

    if not largest:
        raise ValueError("No cyan slime was found in the reference panel")

    min_x = max(0, min(x for x, _ in largest) - 8)
    max_x = min(width, max(x for x, _ in largest) + 9)
    min_y = max(0, min(y for _, y in largest) - 8)
    max_y = min(height, max(y for _, y in largest) + 9)
    subject = rgba.crop((min_x, min_y, max_x, max_y))
    subject_pixels = subject.load()
    for y in range(subject.height):
        for x in range(subject.width):
            r, g, b, a = subject_pixels[x, y]
            if a and abs(r - 24) <= 10 and abs(g - 32) <= 10 and abs(b - 49) <= 12:
                subject_pixels[x, y] = (r, g, b, 0)
    return subject


def fit_subject(subject: Image.Image, canvas_size: int, target_height: int) -> Image.Image:
    alpha_box = subject.getchannel("A").getbbox()
    if alpha_box is None:
        raise ValueError("The source image has no visible pixels after background removal")

    cropped = subject.crop(alpha_box)
    target_width = max(1, round(cropped.width * target_height / cropped.height))
    if target_width > canvas_size - 2:
        target_width = canvas_size - 2
        target_height = max(1, round(cropped.height * target_width / cropped.width))

    return cropped.resize((target_width, target_height), Image.Resampling.NEAREST)


def make_demon_lord_palette(source: Image.Image) -> Image.Image:
    """Shift the blue coat to a dark violet palette while retaining cyan hair and skin."""
    variant = source.copy().convert("RGBA")
    pixels = variant.load()
    for y in range(variant.height):
        for x in range(variant.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            is_cyan_highlight = g >= 145 and b >= 175 and b >= r + 35
            is_blue_cloth = b >= 95 and b >= r + 18 and b >= g + 5
            if is_blue_cloth and not is_cyan_highlight:
                luminance = max(r, g, b)
                pixels[x, y] = (
                    min(82, 22 + luminance // 4),
                    min(46, 10 + luminance // 8),
                    min(112, 48 + luminance // 2),
                    a,
                )
            elif r < 70 and g < 80 and b >= 85:
                pixels[x, y] = (38, 16, 62, a)
    return variant


def render_frame(subject: Image.Image, body_y: int, left_step: int, right_step: int) -> Image.Image:
    canvas = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    x = (32 - subject.width) // 2
    split_y = max(1, round(subject.height * 0.78))

    upper = subject.crop((0, 0, subject.width, split_y))
    lower = subject.crop((0, split_y, subject.width, subject.height))
    middle = lower.width // 2
    left = lower.crop((0, 0, middle, lower.height))
    right = lower.crop((middle, 0, lower.width, lower.height))

    canvas.alpha_composite(upper, (x, body_y))
    canvas.alpha_composite(left, (x + left_step, body_y + split_y))
    canvas.alpha_composite(right, (x + middle + right_step, body_y + split_y))
    return canvas


def write_animation_set(source: Image.Image, output_root: Path, prefix: str) -> None:
    sprites = output_root / "sprites"
    sprites.mkdir(parents=True, exist_ok=True)
    walk_subject = fit_subject(source, canvas_size=32, target_height=29)
    frames = (
        render_frame(walk_subject, body_y=2, left_step=-1, right_step=0),
        render_frame(walk_subject, body_y=1, left_step=0, right_step=1),
        render_frame(walk_subject, body_y=2, left_step=0, right_step=-1),
        render_frame(walk_subject, body_y=1, left_step=1, right_step=0),
    )
    for index, frame in enumerate(frames, start=1):
        frame.save(sprites / f"{prefix}_{index:02}.png", optimize=True)

    selection_subject = fit_subject(source, canvas_size=48, target_height=44)
    charsel = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    charsel.alpha_composite(
        selection_subject,
        ((48 - selection_subject.width) // 2, 48 - selection_subject.height - 1),
    )
    charsel.save(output_root / "charsel.png", optimize=True)


def write_slime_animation_set(source: Image.Image, output_root: Path) -> None:
    sprites = output_root / "sprites"
    sprites.mkdir(parents=True, exist_ok=True)
    subject = fit_subject(source, canvas_size=32, target_height=23)
    poses = ((0, 0, 8), (2, -2, 10), (-1, 1, 7), (1, -1, 9))
    for index, (width_delta, height_delta, y) in enumerate(poses, start=1):
        pose = subject.resize(
            (max(1, subject.width + width_delta), max(1, subject.height + height_delta)),
            Image.Resampling.NEAREST,
        )
        frame = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        frame.alpha_composite(pose, ((32 - pose.width) // 2, y))
        frame.save(sprites / f"rimuru_{index:02}.png", optimize=True)

    selection_subject = fit_subject(source, canvas_size=48, target_height=38)
    charsel = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    charsel.alpha_composite(
        selection_subject,
        ((48 - selection_subject.width) // 2, 48 - selection_subject.height - 3),
    )
    charsel.save(output_root / "charsel.png", optimize=True)


def write_ranga_animation_set(source: Image.Image) -> None:
    RANGA_ROOT.mkdir(parents=True, exist_ok=True)
    subject = fit_subject(source, canvas_size=32, target_height=17)
    poses = ((-1, 11), (0, 10), (1, 11), (0, 10))
    for index, (x_offset, y) in enumerate(poses, start=1):
        frame = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        frame.alpha_composite(subject, ((32 - subject.width) // 2 + x_offset, y))
        frame.save(RANGA_ROOT / f"ranga_{index:02}.png", optimize=True)

    if RANGA_CHARGE.exists():
        charge = fit_subject(Image.open(RANGA_CHARGE), canvas_size=32, target_height=17)
        frame = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        frame.alpha_composite(charge, ((32 - charge.width) // 2, 10))
        frame.save(RANGA_ROOT / "ranga_charge_01.png", optimize=True)


def write_weapon_icons(source: Image.Image) -> None:
    WEAPON_ROOT.mkdir(parents=True, exist_ok=True)
    keyed = remove_chroma_green(source)
    names = ("predator-core", "rimuru-katana-v2", "beelzebuth-blade")
    segment_width = keyed.width // len(names)

    for index, name in enumerate(names):
        left = index * segment_width
        right = keyed.width if index == len(names) - 1 else (index + 1) * segment_width
        segment = keyed.crop((left, 0, right, keyed.height))
        alpha_box = segment.getchannel("A").getbbox()
        if alpha_box is None:
            raise ValueError(f"Generated weapon segment {name} is empty")
        subject = segment.crop(alpha_box)
        fitted = fit_subject(subject, canvas_size=48, target_height=42)
        icon = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
        icon.alpha_composite(fitted, ((48 - fitted.width) // 2, (48 - fitted.height) // 2))
        icon.save(WEAPON_ROOT / f"{name}.png", optimize=True)


def build() -> None:
    source = remove_connected_background(Image.open(SOURCE))
    write_animation_set(source, HUMANOID_SKIN, "rimuru")
    if SLIME_SOURCE.exists():
        slime_source = extract_primary_slime(Image.open(SLIME_SOURCE))
        write_slime_animation_set(slime_source, ROOT / "custom-character")
    else:
        write_animation_set(source, ROOT / "custom-character", "rimuru")
    if DEMON_SOURCE.exists():
        demon_source = remove_connected_checkerboard(Image.open(DEMON_SOURCE))
    else:
        demon_source = make_demon_lord_palette(source)
    write_animation_set(demon_source, DEMON_SKIN, "rimuru")
    if RANGA_SOURCE.exists():
        ranga_source = retain_largest_component(remove_connected_dark_background(Image.open(RANGA_SOURCE)))
        write_ranga_animation_set(ranga_source)
    if WEAPON_SOURCE.exists():
        write_weapon_icons(Image.open(WEAPON_SOURCE))


if __name__ == "__main__":
    build()
