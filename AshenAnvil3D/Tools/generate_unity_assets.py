#!/usr/bin/env python3
"""Generates the serialized Unity data for Ashen Anvil 3D.

* .meta files with stable GUIDs for every C# script (so assets can reference them)
* ScriptableObject assets: skills, weapons, equipment, consumables, heroes, monsters,
  stages, quests and the GameDatabase (Resources/GameDatabase.asset)
* Low-poly OBJ models (see models.py)

Run from anywhere:  python3 Tools/generate_unity_assets.py
Re-running is safe: files are rewritten with identical GUIDs.
"""
import hashlib
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(HERE)
ROOT = os.path.join(PROJECT, "Assets", "AshenAnvil")
sys.path.insert(0, HERE)
import models  # noqa: E402


def guid(key):
    return hashlib.md5(("ashen-anvil-3d:" + key).encode()).hexdigest()


def rel(path):
    return os.path.relpath(path, PROJECT).replace(os.sep, "/")


# ---------------------------------------------------------------------------- script metas
SCRIPT_GUIDS = {}


def write_script_metas():
    for base, _, files in os.walk(os.path.join(PROJECT, "Assets")):
        for f in files:
            if not f.endswith(".cs"):
                continue
            path = os.path.join(base, f)
            g = guid(rel(path))
            SCRIPT_GUIDS[f[:-3]] = g
            with open(path + ".meta", "w") as m:
                m.write(f"""fileFormatVersion: 2
guid: {g}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")


# ---------------------------------------------------------------------------- yaml
class Ref:
    def __init__(self, asset):
        self.asset = asset


class Col:
    def __init__(self, r, g, b, a=1.0):
        self.v = (r, g, b, a)


def scalar(v):
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, int):
        return str(v)
    if isinstance(v, float):
        s = ("%.4f" % v).rstrip("0").rstrip(".")
        return s if s not in ("", "-0") else "0"
    if isinstance(v, str):
        return '"' + v.replace("\\", "\\\\").replace('"', '\\"') + '"'
    if isinstance(v, Col):
        r, g, b, a = (scalar(float(x)) for x in v.v)
        return f"{{r: {r}, g: {g}, b: {b}, a: {a}}}"
    if isinstance(v, Ref):
        if v.asset is None:
            return "{fileID: 0}"
        return f"{{fileID: 11400000, guid: {v.asset.guid}, type: 2}}"
    if v is None:
        return "{fileID: 0}"
    raise TypeError(v)


def emit(lines, key, value, indent):
    pad = "  " * indent
    if isinstance(value, dict):
        lines.append(f"{pad}{key}:")
        for k, v in value.items():
            emit(lines, k, v, indent + 1)
    elif isinstance(value, list):
        if not value:
            lines.append(f"{pad}{key}: []")
            return
        lines.append(f"{pad}{key}:")
        for item in value:
            if isinstance(item, dict):
                first = True
                for k, v in item.items():
                    sub = []
                    emit(sub, k, v, 0)
                    for i, line in enumerate(sub):
                        prefix = pad + ("- " if first and i == 0 else "  ")
                        lines.append(prefix + ("  " * 0) + line if i == 0 else pad + "  " + line)
                    first = False
            else:
                lines.append(f"{pad}- {scalar(item)}")
    else:
        lines.append(f"{pad}{key}: {scalar(value)}")


class Asset:
    all = []

    def __init__(self, folder, name, script, fields):
        self.path = os.path.join(ROOT, folder, name + ".asset")
        self.name = name
        self.script = script
        self.fields = fields
        self.guid = guid(rel(self.path))
        Asset.all.append(self)

    def write(self):
        os.makedirs(os.path.dirname(self.path), exist_ok=True)
        lines = [
            "%YAML 1.1",
            "%TAG !u! tag:unity3d.com,2011:",
            "--- !u!114 &11400000",
            "MonoBehaviour:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            "  m_GameObject: {fileID: 0}",
            "  m_Enabled: 1",
            "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUIDS[self.script]}, type: 3}}",
            f"  m_Name: {self.name}",
            "  m_EditorClassIdentifier: ",
        ]
        for k, v in self.fields.items():
            emit(lines, k, v, 1)
        with open(self.path, "w") as f:
            f.write("\n".join(lines) + "\n")
        with open(self.path + ".meta", "w") as f:
            f.write(f"""fileFormatVersion: 2
guid: {self.guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")


# ---------------------------------------------------------------------------- enums
RARITY = {"Common": 0, "Rare": 1, "Epic": 2, "Legendary": 3, "Mythic": 4}
HERO_CLASS = {"Warrior": 0, "Assassin": 1, "Mage": 2, "Archer": 3, "Support": 4}
SLOT = {"Weapon": 0, "Helmet": 1, "Armor": 2, "Boots": 3, "Ring": 4, "Amulet": 5}
WEAPON = {"Sword": 0, "Axe": 1, "Dagger": 2, "Staff": 3, "Bow": 4, "Hammer": 5}
STAT = {"MaxHealth": 0, "Attack": 1, "Defense": 2, "MoveSpeed": 3, "CritChance": 4, "CritDamage": 5, "AttackSpeed": 6, "Lifesteal": 7}
SKILL = {"GroundSlam": 0, "Whirlwind": 1, "Fireball": 2, "ArrowVolley": 3, "ShadowStrike": 4, "HealingNova": 5}
BEHAVIOUR = {"Melee": 0, "Ranged": 1, "Charger": 2, "Boss": 3}
EFFECT = {"Heal": 0, "AttackBuff": 1, "SpeedBuff": 2}
HEAD = {"Hair": 0, "Hood": 1, "Helmet": 2, "Horns": 3, "Crown": 4, "WizardHat": 5, "Skull": 6}
GOAL = {"DefeatMonsters": 0, "DefeatBosses": 1, "ClearStage": 2, "CollectGold": 3, "UpgradeItems": 4, "ReachHeroLevel": 5}
RARITY_COLOR = [Col(.66, .71, .76), Col(.25, .64, 1), Col(.72, .4, 1), Col(1, .71, .15), Col(1, .24, .37)]


def stats(hp=0., atk=0., df=0., ms=0., crit=0., cd=0., aspd=0., ls=0.):
    return {"maxHealth": float(hp), "attack": float(atk), "defense": float(df), "moveSpeed": float(ms),
            "critChance": float(crit), "critDamage": float(cd), "attackSpeed": float(aspd), "lifesteal": float(ls)}


def mods(*pairs):
    out = []
    for p in pairs:
        stat, value = p[0], p[1]
        pct = p[2] if len(p) > 2 else False
        out.append({"stat": STAT[stat], "value": float(value), "percent": pct})
    return out


def look(primary, secondary, skin, hair, eye=(0.08, 0.08, 0.08), head="Hair", scale=1.0, bulk=1.0, cape=False):
    return {"primaryColor": Col(*primary), "secondaryColor": Col(*secondary), "skinColor": Col(*skin),
            "hairColor": Col(*hair), "eyeColor": Col(*eye), "headStyle": HEAD[head],
            "scale": float(scale), "bulk": float(bulk), "cape": cape}


# ---------------------------------------------------------------------------- items
def item_fields(id_, name, desc, rarity, tint, sell, stack=1):
    return {"id": id_, "displayName": name, "description": desc, "rarity": RARITY[rarity], "icon": None,
            "tint": Col(*tint), "sellPrice": sell, "maxStack": stack}


def weapon(id_, name, desc, rarity, wtype, atk, aspd=1.0, bonus=(), tint=(0.78, 0.8, 0.85), sell=None):
    f = item_fields(id_, name, desc, rarity, tint, sell or int(15 * (1 + RARITY[rarity]) ** 2))
    f.update({"weaponType": WEAPON[wtype], "baseAttack": float(atk), "attackSpeed": float(aspd),
              "bonusStats": mods(*bonus), "modelPrefab": None})
    return Asset("Data/Weapons", "Weapon_" + id_, "WeaponData", f)


def equip(id_, name, desc, rarity, slot, stat_mods, tint=(0.6, 0.6, 0.6)):
    f = item_fields(id_, name, desc, rarity, tint, int(12 * (1 + RARITY[rarity]) ** 2))
    f.update({"slot": SLOT[slot], "stats": mods(*stat_mods)})
    return Asset("Data/Equipment", "Equip_" + id_, "EquipmentData", f)


def consumable(id_, name, desc, rarity, effect, amount, duration, tint):
    f = item_fields(id_, name, desc, rarity, tint, 4 + 8 * RARITY[rarity], 99)
    f.update({"effect": EFFECT[effect], "amount": float(amount), "duration": float(duration)})
    return Asset("Data/Consumables", "Potion_" + id_, "ConsumableData", f)


W = {}
W["ironjaw_blade"] = weapon("ironjaw_blade", "Ironjaw Blade", "A notched sentry's sword that held Hollow Pass for three nights.", "Common", "Sword", 12)
W["fenwood_bow"] = weapon("fenwood_bow", "Fenwood Bow", "Strung with marsh-reed sinew. Quiet as fog.", "Common", "Bow", 12, 1.05, tint=(0.55, 0.75, 0.4))
W["kindling_staff"] = weapon("kindling_staff", "Kindling Staff", "Its ember orb never quite goes out.", "Rare", "Staff", 16, tint=(1.0, 0.5, 0.15))
W["alley_fang"] = weapon("alley_fang", "Alley Fang", "Thin, quick and entirely without honour.", "Rare", "Dagger", 15, 1.2, [("CritChance", 0.05)], tint=(0.7, 0.6, 0.95))
W["menders_maul"] = weapon("menders_maul", "Mender's Maul", "Sets bones. Also breaks them.", "Epic", "Hammer", 20, 0.95, [("Lifesteal", 0.03)], tint=(0.95, 0.85, 0.55))
W["bloodfang_cleaver"] = weapon("bloodfang_cleaver", "Bloodfang Cleaver", "A steppe-forged axe that drinks deep.", "Epic", "Axe", 24, 1.0, [("Lifesteal", 0.04)], tint=(0.85, 0.3, 0.25))
W["starfall_scepter"] = weapon("starfall_scepter", "Starfall Scepter", "A shard of the shattered sky, bound in silver.", "Legendary", "Staff", 32, 1.0, [("CritChance", 0.06)], tint=(0.55, 0.65, 1.0))
W["ashen_crown_blade"] = weapon("ashen_crown_blade", "Ashen Crown Blade", "The king's blade, still warm from the pyre.", "Mythic", "Sword", 44, 1.0, [("CritDamage", 0.3)], tint=(1.0, 0.55, 0.2))

W["rusty_shortsword"] = weapon("rusty_shortsword", "Rusty Shortsword", "Better than bare hands. Barely.", "Common", "Sword", 9, tint=(0.6, 0.45, 0.35))
W["goblin_hatchet"] = weapon("goblin_hatchet", "Goblin Hatchet", "Crude, chipped and surprisingly sharp.", "Common", "Axe", 11, 1.05, tint=(0.55, 0.55, 0.5))
W["tempered_longsword"] = weapon("tempered_longsword", "Tempered Longsword", "Quenched thrice in the Anvil's cistern.", "Rare", "Sword", 22, 1.0, [("CritChance", 0.05)])
W["bonecleaver"] = weapon("bonecleaver", "Bonecleaver", "Carved from the jaw of something enormous.", "Rare", "Axe", 26, 0.95, [("Lifesteal", 0.04)], tint=(0.9, 0.87, 0.75))
W["viper_dagger"] = weapon("viper_dagger", "Viper Dagger", "Green-black steel that never stops dripping.", "Rare", "Dagger", 19, 1.25, [("CritChance", 0.1)], tint=(0.4, 0.85, 0.45))
W["cinder_rod"] = weapon("cinder_rod", "Cinder Rod", "A rod of compressed ash, hot to the touch.", "Rare", "Staff", 25, 1.0, [("CritDamage", 0.15)], tint=(1.0, 0.35, 0.1))
W["ember_edge"] = weapon("ember_edge", "Ember Edge", "A sword that glows brighter with every kill.", "Epic", "Sword", 40, 1.05, [("CritDamage", 0.3)], tint=(1.0, 0.45, 0.15))
W["wyrmbone_bow"] = weapon("wyrmbone_bow", "Wyrmbone Bow", "Its string hums a low draconic note.", "Epic", "Bow", 38, 1.1, [("CritChance", 0.1)], tint=(0.95, 0.9, 0.7))
W["pyre_kings_maul"] = weapon("pyre_kings_maul", "Pyre King's Maul", "The tyrant's hammer. Each blow rekindles your strength.", "Legendary", "Hammer", 70, 0.95,
                              [("MaxHealth", 0.1, True), ("Lifesteal", 0.05)], tint=(1.0, 0.6, 0.2))

E = {}
E["leather_cap"] = equip("leather_cap", "Leather Cap", "Boiled leather, stitched by a nervous apprentice.", "Common", "Helmet", [("MaxHealth", 60), ("Defense", 4)])
E["iron_helm"] = equip("iron_helm", "Iron Helm", "Dented, dependable, slightly too big.", "Rare", "Helmet", [("MaxHealth", 140), ("Defense", 10)])
E["hood_of_whispers"] = equip("hood_of_whispers", "Hood of Whispers", "Wearers hear their enemies' weak points.", "Epic", "Helmet", [("CritChance", 0.08), ("CritDamage", 0.2)])
E["padded_jerkin"] = equip("padded_jerkin", "Padded Jerkin", "Layers of quilted wool and good intentions.", "Common", "Armor", [("MaxHealth", 120), ("Defense", 8)])
E["scale_hauberk"] = equip("scale_hauberk", "Scale Hauberk", "Overlapping iron scales that ring like rain.", "Rare", "Armor", [("MaxHealth", 260), ("Defense", 18)])
E["emberforged_plate"] = equip("emberforged_plate", "Emberforged Plate", "Plate hammered on the Ashen Anvil itself.", "Legendary", "Armor", [("MaxHealth", 600), ("Defense", 40), ("MaxHealth", 0.08, True)])
E["worn_boots"] = equip("worn_boots", "Worn Boots", "Someone else broke these in for you.", "Common", "Boots", [("MoveSpeed", 0.4), ("Defense", 3)])
E["swiftstride_boots"] = equip("swiftstride_boots", "Swiftstride Boots", "Light as ash on the wind.", "Epic", "Boots", [("MoveSpeed", 1.0), ("AttackSpeed", 0.1)])
E["copper_ring"] = equip("copper_ring", "Copper Ring", "Turns your finger green. Worth it.", "Common", "Ring", [("Attack", 6)])
E["red_steppe_ring"] = equip("red_steppe_ring", "Ring of the Red Steppe", "Wolfsblood garnet set in black iron.", "Epic", "Ring", [("Attack", 20), ("Lifesteal", 0.05)])
E["ashen_amulet"] = equip("ashen_amulet", "Ashen Amulet", "A pinch of pyre-ash sealed in glass.", "Rare", "Amulet", [("Attack", 10), ("MaxHealth", 0.08, True)])
E["heart_of_the_anvil"] = equip("heart_of_the_anvil", "Heart of the Anvil", "The forge's ember heart. It beats in time with yours.", "Mythic", "Amulet",
                                [("Attack", 0.15, True), ("MaxHealth", 0.15, True), ("CritChance", 0.1)])

P = {}
P["minor_heal"] = consumable("minor_heal", "Minor Healing Draught", "Restores a portion of health. Tastes of mint and copper.", "Common", "Heal", 0.35, 0, (0.9, 0.2, 0.25))
P["greater_heal"] = consumable("greater_heal", "Greater Healing Draught", "Restores most of your health.", "Rare", "Heal", 0.7, 0, (1.0, 0.35, 0.5))
P["elixir_fury"] = consumable("elixir_fury", "Elixir of Fury", "Your blows land like a forge hammer for a while.", "Epic", "AttackBuff", 0.4, 20, (1.0, 0.45, 0.1))
P["windstep_tonic"] = consumable("windstep_tonic", "Windstep Tonic", "Feet light as cinders on the breeze.", "Rare", "SpeedBuff", 0.35, 20, (0.4, 0.8, 1.0))


# ---------------------------------------------------------------------------- skills
def skill(id_, name, desc, stype, cd, mult, radius=4.0, rng=10.0, count=1, speed=18.0, heal=0.0, stun=0.0, kb=6.0, color=(1, .55, .15)):
    return Asset("Data/Skills", "Skill_" + id_, "SkillData", {
        "id": id_, "displayName": name, "description": desc, "skillType": SKILL[stype], "cooldown": float(cd),
        "damageMultiplier": float(mult), "radius": float(radius), "range": float(rng), "count": count,
        "projectileSpeed": float(speed), "healPercent": float(heal), "stunDuration": float(stun), "knockback": float(kb),
        "vfxColor": Col(*color, 0.7)})


S = {}
S["shield_quake"] = skill("shield_quake", "Shield Quake", "Slams the ground for 220% ATK around the front, stunning foes for 1.2s.", "GroundSlam", 7, 2.2, 4, stun=1.2, kb=8, color=(1, .7, .3))
S["marsh_volley"] = skill("marsh_volley", "Marsh Volley", "Looses 5 piercing arrows in a fan, each dealing 110% ATK.", "ArrowVolley", 6, 1.1, rng=18, count=5, speed=30, color=(.6, 1, .4))
S["kindle_burst"] = skill("kindle_burst", "Kindle Burst", "Hurls a fireball that explodes for 250% ATK in an area.", "Fireball", 6, 2.5, 3, rng=16, speed=16, color=(1, .45, .1))
S["dusk_step"] = skill("dusk_step", "Dusk Step", "Blinks behind the nearest enemy and strikes for a guaranteed 300% ATK critical.", "ShadowStrike", 7, 3.0, 2, rng=12, stun=0.6, kb=4, color=(.6, .3, 1))
S["field_mending"] = skill("field_mending", "Field Mending", "Heals 30% max HP and blasts nearby enemies for 100% ATK.", "HealingNova", 10, 1.0, 4.5, heal=0.3, kb=7, color=(.4, 1, .5))
S["red_whirl"] = skill("red_whirl", "Red Steppe Whirl", "Spins 6 times, each hit dealing 90% ATK to everything nearby.", "Whirlwind", 8, 0.9, 3.2, count=6, kb=3, color=(1, .25, .2))
S["shattered_sky"] = skill("shattered_sky", "Shattered Sky", "Calls 3 falling-star bolts that explode for 220% ATK and daze.", "Fireball", 7, 2.2, 3.5, rng=18, count=3, speed=15, stun=0.5, color=(.55, .6, 1))
S["cinder_cataclysm"] = skill("cinder_cataclysm", "Cinder Cataclysm", "The Ashen King's fury: 340% ATK in a huge ring, stunning for 1.5s.", "GroundSlam", 8, 3.4, 6, stun=1.5, kb=12, color=(1, .35, .1))


# ---------------------------------------------------------------------------- heroes
CLASS_BASE = {
    "Warrior": (stats(950, 55, 40, 5.6, .05, .5, 1.0), stats(75, 5, 3)),
    "Archer": (stats(700, 60, 22, 6.0, .12, .6, 1.05), stats(55, 6, 2)),
    "Mage": (stats(650, 68, 18, 5.6, .08, .5, .95), stats(50, 7, 1.5)),
    "Assassin": (stats(720, 64, 24, 6.6, .2, .7, 1.25), stats(55, 6, 2)),
    "Support": (stats(850, 48, 34, 5.6, .05, .5, 1.0, .05), stats(70, 4, 3)),
}
RARITY_MULT = [1.0, 1.1, 1.2, 1.35, 1.5]


def hero(id_, name, title, cls, rarity, cost, weapon_asset, skill_asset, lore, lk):
    base, growth = CLASS_BASE[cls]
    mult = RARITY_MULT[RARITY[rarity]]
    b = dict(base)
    for k in ("maxHealth", "attack", "defense"):
        b[k] = round(b[k] * mult, 1)
    return Asset("Data/Heroes", "Hero_" + id_, "HeroData", {
        "id": id_, "displayName": name, "title": title, "heroClass": HERO_CLASS[cls], "rarity": RARITY[rarity],
        "lore": lore, "unlockCost": cost, "baseStats": b, "growthPerLevel": growth,
        "startingWeapon": Ref(weapon_asset), "skill": Ref(skill_asset), "look": lk, "prefab": None})


H = [
    hero("torvald", "Torvald Ironjaw", "Shieldbreaker of Hollow Pass", "Warrior", "Common", 0, W["ironjaw_blade"], S["shield_quake"],
         "A mountain sentry who held Hollow Pass alone for three nights. His jaw is iron; his patience is not.",
         look((.43, .48, .55), (.26, .22, .2), (.88, .69, .55), (.55, .35, .17), head="Helmet", bulk=1.25)),
    hero("nessa", "Nessa Fen", "Marsh Warden", "Archer", "Common", 600, W["fenwood_bow"], S["marsh_volley"],
         "Nessa can track a heron through fog by the ripples it leaves. Poachers learn this too late.",
         look((.31, .42, .23), (.22, .2, .15), (.85, .64, .49), (.35, .22, .12), head="Hood", bulk=0.9)),
    hero("wick", "Wick the Kindler", "Apprentice Pyromancer", "Mage", "Rare", 1200, W["kindling_staff"], S["kindle_burst"],
         "Wick has burned down two academies and one very surprised dragon. He insists only one was on purpose.",
         look((.7, .2, .15), (.35, .15, .1), (.93, .76, .6), (.95, .55, .2), eye=(1, .55, .15), head="WizardHat", bulk=0.85)),
    hero("morrow", "Morrow Dusk", "Alley Knife", "Assassin", "Rare", 1500, W["alley_fang"], S["dusk_step"],
         "Born in the gutters below the Anvil, Morrow learned that the quickest blade settles every debt.",
         look((.2, .17, .25), (.12, .1, .14), (.8, .62, .5), (.1, .1, .1), eye=(.7, .45, 1), head="Hood", bulk=0.85, cape=True)),
    hero("hilde", "Hilde the Mender", "Field Surgeon", "Support", "Epic", 2500, W["menders_maul"], S["field_mending"],
         "Hilde has stitched more soldiers than any general has commanded. She swings the maul for the ones she couldn't.",
         look((.85, .82, .72), (.55, .2, .2), (.93, .75, .62), (.95, .82, .45), head="Hair", bulk=1.05)),
    hero("kael", "Kael Bloodfang", "Wolf of the Red Steppe", "Warrior", "Epic", 3500, W["bloodfang_cleaver"], S["red_whirl"],
         "Raised by the steppe wolves, Kael fights like a storm of teeth and never takes a step back.",
         look((.55, .18, .15), (.22, .15, .12), (.78, .56, .42), (.18, .12, .1), head="Horns", bulk=1.35, cape=True)),
    hero("nyx", "Nyx Starfall", "Oracle of the Shattered Sky", "Mage", "Legendary", 6000, W["starfall_scepter"], S["shattered_sky"],
         "Nyx saw the sky break before it happened. She has been pulling down its pieces ever since.",
         look((.2, .24, .5), (.75, .78, .95), (.82, .75, .9), (.85, .88, 1), eye=(.6, .7, 1), head="Crown", bulk=0.85, cape=True)),
    hero("oberon", "Oberon", "The Ashen King", "Warrior", "Mythic", 12000, W["ashen_crown_blade"], S["cinder_cataclysm"],
         "The king who burned his own crown to stop the Pyre Tide. The ashes chose him anyway.",
         look((.18, .16, .17), (.85, .55, .2), (.75, .6, .5), (.8, .8, .8), eye=(1, .5, .15), head="Crown", scale=1.08, bulk=1.4, cape=True)),
]


# ---------------------------------------------------------------------------- monsters
def monster(id_, name, behaviour, st, aggro, arange, cd, xp, gmin, gmax, loot, lk, proj_speed=12, proj_color=(1, .4, .1), summon=None):
    return Asset("Data/Monsters", "Monster_" + id_, "MonsterData", {
        "id": id_, "displayName": name, "behaviour": BEHAVIOUR[behaviour], "baseStats": st,
        "aggroRange": float(aggro), "attackRange": float(arange), "attackCooldown": float(cd),
        "projectileSpeed": float(proj_speed), "projectileColor": Col(*proj_color), "summon": Ref(summon),
        "xpReward": xp, "goldMin": gmin, "goldMax": gmax,
        "loot": [{"item": Ref(i), "chance": float(c)} for i, c in loot],
        "look": lk, "prefab": None})


GOBLIN_SKIN, BONE, EMBER_EYE = (.45, .62, .3), (.86, .83, .72), (1, .45, .1)
M = {}
M["ash_goblin"] = monster("ash_goblin", "Ash Goblin", "Melee", stats(120, 18, 5, 3.8, .05, .5, 1), 20, 1.8, 1.5, 12, 2, 5,
                          [(W["rusty_shortsword"], .04), (E["leather_cap"], .04), (W["goblin_hatchet"], .03)],
                          look((.42, .3, .2), (.3, .22, .15), GOBLIN_SKIN, (.2, .15, .1), eye=(1, .85, .2), head="Horns", scale=.75, bulk=.95))
M["cinder_imp"] = monster("cinder_imp", "Cinder Imp", "Ranged", stats(90, 16, 3, 3.5, .05, .5, 1), 22, 11, 2.2, 14, 2, 6,
                          [(W["cinder_rod"], .015), (E["copper_ring"], .04), (P["windstep_tonic"], .03)],
                          look((.55, .15, .1), (.25, .08, .06), (.85, .3, .18), (.2, .05, .05), eye=(1, .8, .2), head="Horns", scale=.7, bulk=.8),
                          proj_speed=12, proj_color=(1, .45, .1))
M["bone_warrior"] = monster("bone_warrior", "Bone Warrior", "Melee", stats(220, 28, 15, 3.3, .05, .5, 1), 20, 2, 1.6, 20, 3, 8,
                            [(W["tempered_longsword"], .03), (E["iron_helm"], .03), (E["padded_jerkin"], .04), (E["copper_ring"], .03)],
                            look((.3, .3, .33), (.2, .2, .22), BONE, BONE, eye=(1, .2, .15), head="Skull", bulk=.9))
M["bone_archer"] = monster("bone_archer", "Bone Archer", "Ranged", stats(160, 24, 8, 3.2, .1, .5, 1), 24, 13, 2.0, 20, 3, 8,
                           [(W["fenwood_bow"], .03), (E["worn_boots"], .04), (P["minor_heal"], .05)],
                           look((.35, .32, .25), (.22, .2, .16), BONE, BONE, eye=(.4, 1, .5), head="Skull", bulk=.85),
                           proj_speed=16, proj_color=(.6, 1, .6))
M["ironhide_orc"] = monster("ironhide_orc", "Ironhide Orc", "Charger", stats(420, 38, 22, 3.6, .05, .5, 1), 20, 2.2, 1.8, 35, 6, 14,
                            [(W["bonecleaver"], .04), (E["scale_hauberk"], .03), (E["worn_boots"], .04), (E["red_steppe_ring"], .01)],
                            look((.35, .3, .28), (.22, .18, .15), (.4, .5, .3), (.15, .12, .1), eye=(1, .3, .1), head="Helmet", scale=1.35, bulk=1.5))
M["shade_stalker"] = monster("shade_stalker", "Shade Stalker", "Charger", stats(300, 45, 12, 4.6, .15, .6, 1), 22, 2, 1.4, 40, 6, 14,
                             [(W["viper_dagger"], .04), (E["hood_of_whispers"], .03), (P["elixir_fury"], .05)],
                             look((.15, .1, .2), (.08, .06, .1), (.3, .25, .4), (.1, .08, .12), eye=(.75, .4, 1), head="Hood", scale=1.05, bulk=.85, cape=True))
M["ember_golem"] = monster("ember_golem", "Ember Golem", "Melee", stats(900, 55, 40, 2.6, .05, .5, 1), 18, 2.6, 2.2, 60, 10, 22,
                           [(W["ember_edge"], .04), (E["emberforged_plate"], .01), (W["cinder_rod"], .03), (E["swiftstride_boots"], .02)],
                           look((.25, .22, .21), (.9, .35, .1), (.3, .27, .25), (.9, .35, .1), eye=EMBER_EYE, head="Helmet", scale=1.7, bulk=1.7))

M["goblin_warchief"] = monster("goblin_warchief", "Grubnak the Warchief", "Boss", stats(1500, 32, 15, 3.4, .1, .5, 1), 60, 2.6, 1.7, 150, 50, 90,
                               [(E["copper_ring"], .5), (E["padded_jerkin"], .4), (P["greater_heal"], .5)],
                               look((.5, .25, .15), (.3, .2, .12), GOBLIN_SKIN, (.2, .12, .08), eye=(1, .8, .1), head="Horns", scale=1.8, bulk=1.4, cape=True),
                               summon=M["ash_goblin"])
M["gravelord_mordrek"] = monster("gravelord_mordrek", "Gravelord Mordrek", "Boss", stats(2600, 45, 25, 3.2, .1, .5, 1), 60, 2.8, 1.8, 250, 80, 140,
                                 [(W["wyrmbone_bow"], .25), (E["ashen_amulet"], .3), (P["greater_heal"], .5)],
                                 look((.2, .2, .24), (.55, .5, .3), BONE, BONE, eye=(.4, 1, .6), head="Crown", scale=2.0, bulk=1.2, cape=True),
                                 summon=M["bone_warrior"])
M["orc_warlord"] = monster("orc_warlord", "Throk Ironhide", "Boss", stats(4200, 60, 35, 3.3, .1, .5, 1), 60, 3.0, 1.9, 380, 120, 200,
                           [(W["bonecleaver"], .3), (E["scale_hauberk"], .3), (E["red_steppe_ring"], .2)],
                           look((.4, .2, .15), (.25, .2, .18), (.38, .48, .28), (.1, .08, .08), eye=(1, .25, .1), head="Helmet", scale=2.1, bulk=1.7, cape=True),
                           summon=M["ironhide_orc"])
M["infernal_colossus"] = monster("infernal_colossus", "Infernal Colossus", "Boss", stats(6500, 75, 45, 2.9, .1, .5, 1), 60, 3.2, 2.0, 520, 160, 280,
                                 [(W["ember_edge"], .4), (E["emberforged_plate"], .2), (E["swiftstride_boots"], .3), (P["elixir_fury"], .5)],
                                 look((.3, .12, .08), (1, .4, .1), (.45, .2, .12), (1, .4, .1), eye=(1, .8, .2), head="Horns", scale=2.4, bulk=1.6),
                                 summon=M["cinder_imp"])
M["pyre_king"] = monster("pyre_king", "Vargoth, the Pyre King", "Boss", stats(9000, 90, 55, 3.4, .15, .6, 1), 60, 3.2, 1.7, 800, 250, 400,
                         [(W["pyre_kings_maul"], .5), (E["heart_of_the_anvil"], .25), (E["emberforged_plate"], .3)],
                         look((.15, .12, .14), (1, .6, .2), (.55, .4, .35), (.25, .2, .2), eye=(1, .5, .1), head="Crown", scale=2.5, bulk=1.4, cape=True),
                         summon=M["shade_stalker"])


# ---------------------------------------------------------------------------- stages
def wave(*entries):
    return {"entries": [{"monster": Ref(M[m]), "count": c} for m, c in entries]}


def stage(n, name, desc, rec, diff, waves, boss, gold, xp, reward, ground, fog, density, ambient, sun, sun_int):
    return Asset("Data/Stages", f"Stage_{n:02d}", "StageData", {
        "stageNumber": n, "displayName": name, "description": desc, "recommendedPower": rec, "difficulty": float(diff),
        "waves": waves, "boss": Ref(M[boss]), "goldReward": gold, "xpReward": xp, "firstClearReward": Ref(reward),
        "groundColor": Col(*ground), "fogColor": Col(*fog), "fogDensity": float(density), "ambientColor": Col(*ambient),
        "sunColor": Col(*sun), "sunIntensity": float(sun_int)})


ST = [
    stage(1, "Ember Outskirts", "Goblin raiders loot the farms below the Anvil. Drive them back to the hills.", 280, 1.0,
          [wave(("ash_goblin", 4)), wave(("ash_goblin", 4), ("cinder_imp", 2)), wave(("ash_goblin", 5), ("cinder_imp", 3))],
          "goblin_warchief", 200, 150, E["iron_helm"], (.36, .31, .24), (.42, .32, .26), .012, (.48, .42, .38), (1, .85, .7), 1.25),
    stage(2, "The Bonefield", "An old battlefield where the dead have started to stand back up.", 420, 1.3,
          [wave(("bone_warrior", 3), ("ash_goblin", 3)), wave(("bone_archer", 3), ("bone_warrior", 3)),
           wave(("bone_warrior", 4), ("bone_archer", 3), ("cinder_imp", 2))],
          "gravelord_mordrek", 350, 260, W["tempered_longsword"], (.42, .4, .36), (.33, .35, .4), .016, (.4, .42, .48), (.85, .9, 1), 1.0),
    stage(3, "Ironwood Warcamp", "The Ironhide clan has fortified the forest. Break their warcamp.", 600, 1.7,
          [wave(("ironhide_orc", 2), ("ash_goblin", 5)), wave(("ironhide_orc", 3), ("bone_archer", 3)),
           wave(("ironhide_orc", 4), ("cinder_imp", 4))],
          "orc_warlord", 550, 400, E["swiftstride_boots"], (.26, .3, .2), (.25, .3, .24), .018, (.36, .42, .34), (1, .95, .8), 1.1),
    stage(4, "Cinder Depths", "Below the forge, the earth runs red. Something enormous stirs in the heat.", 850, 2.2,
          [wave(("cinder_imp", 6), ("ember_golem", 1)), wave(("shade_stalker", 3), ("cinder_imp", 4)),
           wave(("ember_golem", 2), ("shade_stalker", 3))],
          "infernal_colossus", 800, 600, E["red_steppe_ring"], (.25, .17, .15), (.4, .18, .1), .02, (.55, .3, .25), (1, .6, .35), 1.2),
    stage(5, "Throne of Ash", "The Pyre King waits upon a throne of cinders. End his reign.", 1150, 2.8,
          [wave(("shade_stalker", 4), ("ember_golem", 2)), wave(("ironhide_orc", 3), ("bone_archer", 4), ("cinder_imp", 3)),
           wave(("ember_golem", 3), ("shade_stalker", 4))],
          "pyre_king", 1200, 900, E["heart_of_the_anvil"], (.2, .19, .2), (.18, .14, .18), .022, (.4, .32, .38), (1, .55, .35), 1.0),
]


# ---------------------------------------------------------------------------- quests
def quest(id_, title, desc, goal, target, gold, reward=None):
    return Asset("Data/Quests", "Quest_" + id_, "QuestData", {
        "id": id_, "title": title, "description": desc, "goal": GOAL[goal], "target": target,
        "goldReward": gold, "rewardItem": Ref(reward)})


Q = [
    quest("first_blood", "First Blood", "Defeat 25 monsters.", "DefeatMonsters", 25, 150, P["minor_heal"]),
    quest("boss_hunter", "Boss Hunter", "Defeat 3 bosses.", "DefeatBosses", 3, 800, P["greater_heal"]),
    quest("warpath", "On the Warpath", "Clear stage 3 of the campaign.", "ClearStage", 3, 1500, E["ashen_amulet"]),
    quest("veteran", "Veteran", "Raise a hero to level 10.", "ReachHeroLevel", 10, 1000, E["hood_of_whispers"]),
    quest("forgemaster", "Forgemaster", "Upgrade equipment at the forge 10 times.", "UpgradeItems", 10, 600, P["windstep_tonic"]),
    quest("hoarder", "Dragon's Hoard", "Collect 5,000 gold.", "CollectGold", 5000, 1000),
    quest("monster_slayer", "Monster Slayer", "Defeat 250 monsters.", "DefeatMonsters", 250, 1200, P["elixir_fury"]),
    quest("ashen_legend", "Ashen Legend", "Clear the Throne of Ash.", "ClearStage", 5, 5000, W["pyre_kings_maul"]),
]


# ---------------------------------------------------------------------------- database
def build_database():
    items = list(W.values()) + list(E.values()) + list(P.values())
    return Asset("Resources", "GameDatabase", "GameDatabase", {
        "heroes": [Ref(h) for h in H],
        "items": [Ref(i) for i in items],
        "skills": [Ref(s) for s in S.values()],
        "monsters": [Ref(m) for m in M.values()],
        "stages": [Ref(s) for s in ST],
        "quests": [Ref(q) for q in Q],
        "starterItems": [Ref(P["minor_heal"]), Ref(E["worn_boots"])],
        "starterGold": 300,
        "defaultPotion": Ref(P["minor_heal"]),
        "characterAnimator": None,
        "litMaterial": None,
        "vfxMaterial": None,
        "coinMesh": None,
        "lootMesh": None,
        "potionMesh": None,
    })


def write_models():
    out = os.path.join(ROOT, "Models")
    os.makedirs(out, exist_ok=True)
    total = 0
    for name, fn in models.MODELS.items():
        total += fn().write(os.path.join(out, name + ".obj"), name)
    return len(models.MODELS), total


def copy_icon():
    src = os.path.join(os.path.dirname(PROJECT), "icons", "icon-512.png")
    dst_dir = os.path.join(ROOT, "Art")
    if os.path.exists(src):
        os.makedirs(dst_dir, exist_ok=True)
        shutil.copyfile(src, os.path.join(dst_dir, "AppIcon.png"))


def main():
    write_script_metas()
    build_database()
    for a in Asset.all:
        a.write()
    n_models, n_verts = write_models()
    copy_icon()
    print(f"scripts: {len(SCRIPT_GUIDS)}  assets: {len(Asset.all)}  models: {n_models} ({n_verts} verts)")


if __name__ == "__main__":
    main()
