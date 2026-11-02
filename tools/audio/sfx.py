"""Sound effect recipes for Target Toss. Each function returns a rendered Track."""

from synth import Track, note_freq, saw, square, triangle


def pulse(duty):
    return lambda p: square(p, duty)


def pop():
    """A good target pops: a bubbly blip. The game raises its pitch with the combo."""
    t = Track(0.18)
    t.add_tone(0, 0.05, 520, triangle, 0.45, freq_end=1250, attack=0.001, release=0.06)
    t.add_tone(0, 0.03, note_freq("E6"), pulse(0.25), 0.1, release=0.04)
    t.add_noise(0, 0.03, 0.08, smoothing=0.3, decay_power=2.0, seed=1)
    return t


def bonus():
    """A bonus star: a bright sparkling chime."""
    t = Track(0.6)
    for i, name in enumerate(["E6", "G#6", "B6", "E7"]):
        t.add_tone(i * 0.045, 0.06, note_freq(name), triangle, 0.22, release=0.25)
    t.add_tone(0, 0.08, note_freq("B5"), pulse(0.125), 0.08, release=0.15)
    return t


def bad():
    """A bad target is clicked: a sour buzz."""
    t = Track(0.4)
    t.add_tone(0, 0.22, 180, saw, 0.3, freq_end=120, release=0.08)
    t.add_tone(0, 0.22, 190, pulse(0.5), 0.18, freq_end=125, release=0.08)
    return t


def bomb():
    """A bomb goes off: a long crunchy blast under a falling rumble."""
    t = Track(1.4)
    t.add_noise(0, 1.1, 0.7, smoothing=0.75, decay_power=1.8, seed=4)
    t.add_noise(0, 0.25, 0.35, smoothing=0.2, decay_power=2.5, seed=5)
    t.add_tone(0, 0.6, 120, triangle, 0.7, freq_end=35, release=0.3)
    return t


def whiff():
    """A click that hits nothing: a soft air swish."""
    t = Track(0.14)
    t.add_noise(0, 0.1, 0.18, smoothing=0.55, decay_power=1.3, seed=6)
    return t


def fell():
    """A good target drops out unpopped: a dull low thud."""
    t = Track(0.3)
    t.add_tone(0, 0.12, 160, triangle, 0.5, freq_end=70, release=0.08)
    t.add_noise(0, 0.06, 0.1, smoothing=0.8, decay_power=2.0, seed=7)
    return t


def slow_time():
    """Slow time starts: a long downward 'wooom'."""
    t = Track(0.9)
    t.add_tone(0, 0.7, 880, triangle, 0.32, freq_end=180, release=0.15, vibrato=0.02)
    t.add_tone(0, 0.7, 440, pulse(0.125), 0.08, freq_end=90, release=0.15)
    return t


def magnet():
    """The magnet switches on: a wobbling rise."""
    t = Track(0.7)
    t.add_tone(0, 0.5, 220, pulse(0.25), 0.18, freq_end=880, release=0.12, vibrato=0.06)
    t.add_tone(0.05, 0.45, 330, triangle, 0.2, freq_end=1320, release=0.12)
    return t


def power_end():
    """A power-up runs out."""
    t = Track(0.3)
    t.add_tone(0, 0.08, note_freq("E5"), triangle, 0.28, release=0.04)
    t.add_tone(0.1, 0.1, note_freq("A4"), triangle, 0.28, release=0.08)
    return t


def multiplier():
    """The multiplier goes up: three quick rising notes."""
    t = Track(0.4)
    for i, name in enumerate(["C6", "E6", "G6"]):
        t.add_tone(i * 0.06, 0.06, note_freq(name), pulse(0.25), 0.2, release=0.08)
    return t


def combo_break():
    """A combo ends: two falling notes."""
    t = Track(0.35)
    t.add_tone(0, 0.08, note_freq("G5"), pulse(0.5), 0.16, release=0.04)
    t.add_tone(0.1, 0.12, note_freq("D5"), pulse(0.5), 0.16, release=0.1)
    return t


def life_lost():
    """A life is lost: a wobbly drop."""
    t = Track(0.55)
    t.add_tone(0, 0.35, 660, square, 0.18, freq_end=220, release=0.12, vibrato=0.04)
    return t


def tick():
    """The last seconds of a timed round."""
    t = Track(0.08)
    t.add_tone(0, 0.02, 1500, pulse(0.5), 0.16, release=0.03)
    return t


def time_up():
    """The clock runs out: a two-tone horn."""
    t = Track(0.9)
    t.add_tone(0, 0.25, note_freq("C5"), saw, 0.2, release=0.05)
    t.add_tone(0, 0.25, note_freq("E5"), pulse(0.5), 0.12, release=0.05)
    t.add_tone(0.3, 0.45, note_freq("G4"), saw, 0.22, release=0.15)
    t.add_tone(0.3, 0.45, note_freq("C5"), pulse(0.5), 0.12, release=0.15)
    return t


def new_record():
    """A new high score: a bright fanfare."""
    t = Track(1.6)
    for name, at, length in [("C5", 0.0, 0.1), ("E5", 0.12, 0.1), ("G5", 0.24, 0.1),
                             ("C6", 0.36, 0.2), ("G5", 0.6, 0.1), ("C6", 0.72, 0.6)]:
        t.add_tone(at, length, note_freq(name), pulse(0.25), 0.22, release=0.15)
    t.add_tone(0.72, 0.6, note_freq("C3"), triangle, 0.45, release=0.3)
    return t


def round_over():
    """The round ends without a record: a short falling line."""
    t = Track(1.2)
    for name, at, length in [("G5", 0.0, 0.15), ("E5", 0.18, 0.15), ("C5", 0.36, 0.5)]:
        t.add_tone(at, length, note_freq(name), triangle, 0.35, release=0.2)
    t.add_tone(0.36, 0.5, note_freq("C3"), triangle, 0.35, release=0.25)
    return t


def go():
    """The round starts."""
    t = Track(0.45)
    t.add_tone(0, 0.08, note_freq("G5"), pulse(0.25), 0.2, release=0.03)
    t.add_tone(0.1, 0.2, note_freq("C6"), pulse(0.25), 0.22, release=0.12)
    return t


def move():
    """Menu cursor."""
    t = Track(0.06)
    t.add_tone(0, 0.02, 1100, pulse(0.5), 0.14, release=0.02)
    return t


def select():
    """Menu confirm."""
    t = Track(0.16)
    t.add_tone(0, 0.04, note_freq("E6"), pulse(0.25), 0.18, release=0.02)
    t.add_tone(0.05, 0.06, note_freq("B6"), pulse(0.25), 0.18, release=0.05)
    return t


def pause():
    """The game pauses or resumes."""
    t = Track(0.25)
    t.add_tone(0, 0.08, note_freq("E5"), triangle, 0.3, release=0.05)
    t.add_tone(0.09, 0.08, note_freq("B4"), triangle, 0.3, release=0.08)
    return t


EFFECTS = {
    "pop.ogg": pop,
    "bonus.ogg": bonus,
    "bad.ogg": bad,
    "bomb.ogg": bomb,
    "whiff.ogg": whiff,
    "fell.ogg": fell,
    "slow_time.ogg": slow_time,
    "magnet.ogg": magnet,
    "power_end.ogg": power_end,
    "multiplier.ogg": multiplier,
    "combo_break.ogg": combo_break,
    "life_lost.ogg": life_lost,
    "tick.ogg": tick,
    "time_up.ogg": time_up,
    "new_record.ogg": new_record,
    "round_over.ogg": round_over,
    "go.ogg": go,
    "move.ogg": move,
    "select.ogg": select,
    "pause.ogg": pause,
}
