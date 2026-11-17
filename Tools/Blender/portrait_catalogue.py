"""Character identities shared by the Blender portrait generator."""
SKIN_LIGHT = (0.72, 0.47, 0.36)
SKIN_PINK = (0.78, 0.48, 0.38)
SKIN_RUDDY = (0.74, 0.4, 0.31)
SKIN_OLIVE = (0.6, 0.4, 0.27)
SKIN_DARK = (0.13, 0.066, 0.038)

HAIR_DARK_BLOND = (0.24, 0.16, 0.08)
HAIR_BROWN = (0.12, 0.06, 0.03)
HAIR_BLACK = (0.01, 0.0085, 0.008)
HAIR_GREY = (0.42, 0.4, 0.38)
HAIR_WHITE = (0.7, 0.68, 0.64)
HAIR_RED = (0.5, 0.11, 0.03)
HAIR_AUBURN = (0.32, 0.07, 0.04)
HAIR_BLONDE = (0.62, 0.45, 0.2)

PEOPLE = {
    # Kuba: 24, five years of philosophy, three days in Dublin. Tired, wary, a hoodie and a rucksack strap.
    "kuba": dict(skin=SKIN_LIGHT, hair=HAIR_DARK_BLOND, haircut="messy", iris=(0.12, 0.2, 0.28), lids=0.42,
                 brows=(0.05, 0.12), mouth=-0.002, nose=1.0, jaw=0.95, build=0.9, clothes="hoodie",
                 cloth=(0.035, 0.065, 0.04), yaw=-18, tilt=4, stubble=True),
    # Declan: the head chef, fifties, from the Liberties via Boston. Big, bald under the skull cap, grey beard.
    "declan": dict(skin=SKIN_RUDDY, hair=HAIR_GREY, haircut="none", iris=(0.14, 0.24, 0.32), lids=0.3,
                   brows=(-0.12, 0.0), mouth=0.003, nose=1.35, jaw=1.12, build=1.15, clothes="chef",
                   cloth=(0.86, 0.85, 0.82), hat="skullcap", beard="full", yaw=-22, tilt=-2),
    # Marek: thirties, from Radom, knows everyone and everything; buzz cut, tracksuit top.
    "marek": dict(skin=SKIN_PINK, hair=HAIR_BROWN, haircut="buzz", iris=(0.2, 0.14, 0.07), lids=0.28,
                  brows=(0.0, 0.0), mouth=0.006, nose=1.15, jaw=1.1, build=1.1, clothes="tracksuit",
                  cloth=(0.012, 0.018, 0.07), yaw=-26, tilt=-3),
    # Agnieszka: a teacher of English back home, studying for the exams here; glasses, ponytail, cardigan.
    "agnieszka": dict(skin=SKIN_LIGHT, hair=HAIR_BLONDE, haircut="ponytail", iris=(0.18, 0.26, 0.2), lids=0.2,
                      brows=(0.06, 0.04), mouth=0.004, nose=0.85, jaw=0.86, build=0.82, clothes="cardigan",
                      cloth=(0.34, 0.22, 0.12), glasses=True, yaw=-16, tilt=5, female=True),
    # Darius: the Lithuanian who never takes his beanie off; beard, grey hoodie.
    "darius": dict(skin=SKIN_PINK, hair=HAIR_BROWN, haircut="none", iris=(0.2, 0.28, 0.34), lids=0.36,
                   brows=(0.0, 0.0), mouth=0.0, nose=1.1, jaw=1.05, build=1.05, clothes="hoodie",
                   cloth=(0.1, 0.1, 0.105), hat="beanie", beard="full", yaw=-20, tilt=0),
    # Gerry of "Emerald Recruitment": slicked hair, a shiny suit and a mobile phone at his ear.
    "agent": dict(skin=SKIN_PINK, hair=HAIR_BLACK, haircut="slick", iris=(0.1, 0.07, 0.04), lids=0.18,
                  brows=(-0.06, 0.1), mouth=0.008, nose=1.05, jaw=1.0, build=0.98, clothes="suit",
                  cloth=(0.03, 0.035, 0.05), tie=(0.4, 0.015, 0.02), phone="mobile", yaw=-24, tilt=-6),
    # Mrs Byrne: the landlady, seventy, "I will, yeah"; a grey perm, glasses on a chain, a lilac cardigan, pearls.
    "byrne": dict(skin=SKIN_PINK, hair=HAIR_WHITE, haircut="perm", iris=(0.2, 0.3, 0.38), lids=0.38,
                  brows=(-0.08, 0.06), mouth=-0.004, nose=1.0, jaw=0.95, build=0.95, clothes="cardigan",
                  cloth=(0.2, 0.11, 0.24), glasses=True, pearls=True, yaw=-18, tilt=-4, female=True),
    # The clerk at the PPS office: a neat bob, a blouse and a lanyard with a badge.
    "clerk": dict(skin=SKIN_LIGHT, hair=HAIR_BROWN, haircut="bob", iris=(0.16, 0.12, 0.06), lids=0.32,
                  brows=(-0.02, 0.0), mouth=-0.002, nose=0.9, jaw=0.9, build=0.88, clothes="blouse",
                  cloth=(0.55, 0.66, 0.78), lanyard=True, yaw=-12, tilt=2, female=True),
    # The man at the bank: side parting, glasses, a grey suit and a navy tie.
    "banker": dict(skin=SKIN_LIGHT, hair=HAIR_BROWN, haircut="parting", iris=(0.14, 0.1, 0.05), lids=0.3,
                   brows=(0.0, 0.0), mouth=0.002, nose=1.0, jaw=1.0, build=1.0, clothes="suit",
                   cloth=(0.1, 0.105, 0.115), tie=(0.02, 0.04, 0.16), glasses=True, yaw=-14, tilt=0),
    # Mum, on the phone from Poland: short auburn waves, a maroon sweater and the landline receiver.
    "mum": dict(skin=SKIN_LIGHT, hair=HAIR_AUBURN, haircut="waves", iris=(0.12, 0.2, 0.3), lids=0.22,
                brows=(0.1, 0.06), mouth=0.006, nose=0.95, jaw=0.92, build=0.95, clothes="sweater",
                cloth=(0.17, 0.018, 0.035), phone="receiver", yaw=-18, tilt=6, female=True),
    # Siobhán: the waitress, early twenties, red curls everywhere, black shirt, a pencil behind her ear.
    "siobhan": dict(skin=SKIN_PINK, hair=HAIR_RED, haircut="curls", iris=(0.12, 0.3, 0.18), lids=0.16,
                    brows=(0.04, 0.1), mouth=0.009, nose=0.82, jaw=0.85, build=0.82, clothes="shirt",
                    cloth=(0.03, 0.03, 0.035), pencil=True, freckles=True, yaw=-20, tilt=8, female=True),
    # Tomek: thirty, a plasterer from Kielce on a Dublin site; hard hat, hi-vis vest, stubble.
    "tomek": dict(skin=SKIN_RUDDY, hair=HAIR_BROWN, haircut="buzz", iris=(0.18, 0.13, 0.06), lids=0.3,
                  brows=(0.06, 0.0), mouth=-0.003, nose=1.15, jaw=1.08, build=1.08, clothes="hivis",
                  cloth=(0.09, 0.09, 0.1), hat="hardhat", stubble=True, yaw=-24, tilt=-2),
    # Gavin: the sous-chef, a Dubliner in his thirties; a black quiff, an earring, chef's whites, a scowl.
    "gavin": dict(skin=SKIN_PINK, hair=HAIR_BLACK, haircut="quiff", iris=(0.2, 0.26, 0.2), lids=0.34,
                  brows=(-0.16, 0.05), mouth=-0.004, nose=1.05, jaw=1.02, build=1.0, clothes="chef",
                  cloth=(0.86, 0.85, 0.82), earring=True, yaw=-26, tilt=-5),
    # Tunde: from Lagos, twenty, the new kitchen porter; close-cropped hair, a white T-shirt, a ready smile.
    "tunde": dict(skin=SKIN_DARK, hair=HAIR_BLACK, haircut="buzz", iris=(0.06, 0.03, 0.015), lids=0.2,
                  brows=(0.06, 0.06), mouth=0.009, nose=1.12, jaw=0.98, build=0.92, clothes="sweater",
                  cloth=(0.78, 0.78, 0.75), apron=(0.03, 0.03, 0.035), coily=True, yaw=-18, tilt=4),
    # Bartek: twenty, from Białystok, cheated by an agency like Kuba was; wide eyes, worried brows, an old hoodie.
    "bartek": dict(skin=SKIN_LIGHT, hair=(0.09, 0.06, 0.035), haircut="buzz", iris=(0.16, 0.24, 0.3), lids=0.1,
                   brows=(0.15, 0.06), mouth=-0.004, nose=0.92, jaw=0.84, build=0.84, clothes="hoodie",
                   cloth=(0.2, 0.04, 0.04), yaw=-14, tilt=7),
    # Dad: fifty-eight, a toolmaker from Kielce who never phones; grey, a moustache, a cardigan, the landline.
    "dad": dict(skin=SKIN_PINK, hair=HAIR_GREY, haircut="parting", iris=(0.14, 0.2, 0.26), lids=0.36,
                brows=(-0.05, 0.02), mouth=-0.002, nose=1.2, jaw=1.05, build=1.02, clothes="cardigan",
                cloth=(0.17, 0.11, 0.06), beard="moustache", phone="receiver", yaw=-18, tilt=-3),
    # Thiago: twenty-one, from Goiânia, studying English in Dublin; short black waves, a green T-shirt, a hopeful smile.
    "thiago": dict(skin=(0.3, 0.17, 0.1), hair=HAIR_BLACK, haircut="waves", iris=(0.08, 0.04, 0.02), lids=0.18,
                   brows=(0.08, 0.06), mouth=0.009, nose=1.02, jaw=0.92, build=0.9, clothes="sweater",
                   cloth=(0.015, 0.13, 0.04), yaw=-18, tilt=5),
    # Ołeh: twenty-two, from Lviv, studied building, washes dishes in Kraków; fair crew cut, a tracksuit, serious.
    "oleh": dict(skin=SKIN_LIGHT, hair=HAIR_BLONDE, haircut="buzz", iris=(0.16, 0.26, 0.34), lids=0.26,
                 brows=(0.02, 0.0), mouth=0.0, nose=1.05, jaw=1.02, build=0.95, clothes="tracksuit",
                 cloth=(0.03, 0.04, 0.06), yaw=-20, tilt=0),
    # Liam: twenty-three, from Cork, fled the bust to a mining camp; sunburnt and freckled, red hair, a Cork-red shirt.
    "liam": dict(skin=(0.86, 0.42, 0.32), hair=(0.36, 0.1, 0.035), haircut="messy", iris=(0.14, 0.26, 0.2), lids=0.2,
                 brows=(0.06, 0.08), mouth=0.006, nose=0.95, jaw=0.95, build=0.92, clothes="sweater",
                 cloth=(0.22, 0.01, 0.012), freckles=True, yaw=-22, tilt=3),
}

# Art direction: silhouette and facial proportions vary without changing speaker identities.
FACE_SHAPES = {
    'kuba': ((1.10,1.08,1.10), .94, 1.03),
    'declan': ((1.23,1.16,1.00), 1.13, 1.08),
    'marek': ((1.20,1.12,1.00), 1.06, 1.03),
    'agnieszka': ((1.05,1.07,1.08), .96, .91),
    'darius': ((1.14,1.11,1.10), 1.02, 1.02),
    'agent': ((1.12,1.10,1.06), .97, 1.02),
    'byrne': ((1.14,1.10,1.00), 1.16, .92),
    'clerk': ((1.11,1.08,1.04), 1.00, .94),
    'banker': ((1.08,1.10,1.12), .96, 1.02),
    'mum': ((1.14,1.10,1.01), 1.12, .97),
    'siobhan': ((1.08,1.08,1.01), 1.04, .94),
    'tomek': ((1.19,1.12,1.03), 1.03, 1.04),
    'gavin': ((1.10,1.10,1.08), .94, 1.03),
    'tunde': ((1.12,1.11,1.04), 1.04, 1.06),
    'bartek': ((1.06,1.07,1.00), 1.07, .93),
    'dad': ((1.16,1.13,1.06), 1.04, 1.05),
    'thiago': ((1.12,1.09,1.02), 1.07, 1.06),
    'oleh': ((1.09,1.10,1.07), .96, .96),
    'liam': ((1.14,1.10,1.00), 1.06, 1.04),
}



