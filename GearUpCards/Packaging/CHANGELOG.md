## Patch Notes
<details>
<summary>Public Beta 5-0 [V0.5.0]</summary>

- Added **[Laser Sight]** : Uncommon
  - visuals for bullet trajectory, stacking increase range
  - -20 deg spread
  - +0.1s attack time

- Added **[Hyper Regeneration]** : Uncommon
  - +25 HP/s
  - +0.5% HP/s
  - -15% HP Cap

- Added **[Medic!!!]** : Uncommon
  - flat +250 HP
  - -10% Heal Effects
  - appear more for player with HP below 100, less for anyone above 500, none beyond 1000

- Added **[Desolation]** : Uncommon
- *"Blocking strips away nearby enemy block's defense and slow its recharge!"*
  - Gear-based block card that get better with more copies of itself
  - starts with 3s duration, 6s Cooldown
  - +1s Block Cooldown

- Added **[Protection Glyph]** - Common
  - +0.15s min block cooldown that cannot be removed in anyway (for now)
  - block I-Frame last longer (+35%)
  - +0.1s Gun Attack time (can mess up high RPM build quick)
  - reduce incoming spell's effects and damages

- Reworked **[Magick Fragment]**
  - now give -30% & -0.2s Block Cooldown
  - no longer reduce HP but...
  - block I-Frame last shorter (-35%)
  - block echoes occur quicker

- Added **[Arcane Conversion]** - Common (because only effective with multiple copies)
  - 1st copy -- 75% bullet damage turned to negHeal, 50% lifesteal benefit (75% effective LS)
  - 1st copy -- make spell deal full 'magic' damage
  - 2nd copy -- 100% damage turned to negHeal, 50% lifesteal benefit
  - 3rd copy and onward -- +35% all damage dealt, +10% all damage taken
  - [Protection Glyph] reduce damage taken from arcane bullets by 10% per stack (diminishing)

- Scaled up **[Hollow Life]**
  - +200% > +300% HP
  - -25% > -30% HP Cap
  - -15% > -25% Heal Effects

- Scaled Down **[Potency Glyph]**
  - +65% > +50% Damage
  - -15% > -10% HP

- Rescaled **[Glyph CAD Module]**
  - offer up to one Uncommon Glyph card
  - more Glyphs weight, no longer boosting spell draws
  - only boost stats bonus by ~50%

- Shift **[Replication Glyph]** to **Rare**
  - by default it won't be offer with [Glyph CAD Module]

- Reworked **[Medical Parts]**
  - flat 7.5 HP/s
  - +10% Heal Effects, additively
  - +50% > +25% HP

- Scaled down **[Flak Cannon]** again
  - has a hard limit on Attack Speed and cannot Burst-Fire
  - overall damage reduced
  - reduce projectiles spawned
  - shell will bounce off player once and scatter right after with a brief delay

- Reescale **[Tiberium Bullet]** burst HP drain
  - shift its effect to be stronger long-term
  - make it viable option for bullet-spam builds

- Fixed **[Mystic Missile]**
  - reduce explosion force in general
  - clear up VFX persisting issue
  - replace mystic missile blasts with animated images and reduce bullet particles, for now

- **[Arc of Bullets]** now has minimum spread and no longer be focused down to pin-point

- Fixed the underlying issue with **[Medical Parts]**, **[Glyph CAD Module]** and **[Bullets.rar]** stats modifiers

- Changes on how booster pack extra draw works:
  - **[Supply Drop]** to drop in after everyone else's card picks
  - **[Vintage Gears]** and **[Supply Drop]** giving only **TWO** cards instead of three

- Blacklist troublesome card combos, including
  - BSC's [Pong] with unique gun mods
  - RSClass' [Mirror Sage] with unique gun mods
  - PCE's [Piercing Bullets] with [Arcane Conversion]
  - Root's [Anonymity] with [Arcane Conversion]

Minors:
- add and update card arts!
- add status bar for heal buff/debuff and block break
- resize and adjust Arcane Sun's burn VFX
- lots of card's description changes to make it brief (and lil' bigger)

</details>

<details>
<summary>Public Beta 4-2 [V0.4.2]</summary>

- make cards and spells that deal direct HP subtraction and life drains to utilize **Heal(negative)** function for proper interaction with damage-taken multipliers and to support displaying with \[DamageTracker]

- nerfed **[Replication Glyph]**
  - +3 > +2 Gun Projectiles
  - -25% > -35% ATK SPD
  - 0.0s > +0.25s Reload Time
  
- put a limit behind **[Supply Drop!]** to only have 1 ongoing 'delivery' at a time
  - also have unlisted effect to temporarly blacklist CardManipulation and BoosterPack cards until the drop.

</details>

<details>
<summary>Public Beta 4-0 [V0.4.0]</summary>

### Implemented [Booster Pack] mechanic:

- added **\[Vintage Gears]** redraw and pick 3 vanilla cards of uncommon or lower rarity.
- added **\[Veteran's Friend]** redraw and pick 1 guaranteed rare vanilla card.
- added **\[Supply Drop]** do nothing now, next round's card pick you get to pick +3 more cards of Uncommon or lower rarity.
- updated **\[Glyph CAD Module]** to give 1 Glyph card of your choice.
- added **\[Pure Canvas]** -- a \[Shuffle] that excludes Glyph, Spell and Magick cards from appearing.

### Additions:

- added **[Replication Glyph]** -- more gun projectiles! MOAR SPELL PROJECTILE!!
- added **[Mystic Missile]** -- a passive spell card that enchants gun-fired bullets with explosive arcane energy, scales with glyphs and additional copies.

### Other changes:

#### Buffed [Aracane Sun]

- stronger start + ramp up rate
- update beam and sun visual codes
- fixed delayed activation at battle start

#### Rebalance [Flak Cannon]

- severely limiting primary gun firerate, bursts and #projectiles gains
- now only some shrapnels carry effects while the rest is only carrying basic bullet stats

#### Buffed [Anti-Bullet Magick]

- affecting larger area and lasting longer at base level

#### Scaled up [Tiberium Bullets]

- inflicts more life drains proportional to gun's damage
- also incurs more self life drains on pick up

#### Misc.

- Reworked how **\[Arc of Bullets]** and **\[Parallel Bullets]** spread the bullets and fixed the issue with burst-fire guns

</details>

<details>
<summary>Public Beta 3-0 [V0.3.1]</summary>

- added \[Bullets.rar] trim down bullet spams (projectile counts) in exchange for more damage per bullets
- added \[Guardians Charm] boosts block card draw chance and 'block cards draw more block cards' (no longer the card pack's default mechanic)
- added \[Lifeforce Duorblity] orb spell of mobile Heal & DMG zone
- added \[Lifeforce Blast!] orb spell with explosive Heal/Damage and then boost/hinder healings afterward
- added \[Arcane Sun] spell that passively deal ramping DPS + "Damage Amp" debuff
- added \[Portal Magick] Unique Magick, "Now you're thinking with Portals"

- buffed \[Orb-literation] increased base radius and amount of Max HP culls on impact
- Reworked \[Parallel Bullets] make it so it properly arranges in parallel and scales with gun spread & proj. counts

- Spells passively boost glyphs' draw chances
- Spells compatible with controller
- Improved/fixed issues with card draw rarity/weight adjustment
- Block-based ability cooldowns start at 0.5s at battle start; should be available when grace period is over.

</details>

<details>
<summary>Public Beta 2-0 [v0.2.0]</summary>

- added \[Orb-Literation] and dependencies: map destruction, Max HP culls on impact
- added \[Tiberium Bullet] and dependencies: caustic HP removal bullet modifier
- added \[Arc of Bullets] evenly spread bullets in arc
- added \[Parallel Bullets] neatly arranged and focused bullets

- added \[Divination Glyph] velocity/trajectory. speed to both Spell and Bullets
- added \[Influence Glyph] spell range/AoE upgrade
- added \[Geometric Glyph] bounces to both Spell and Bullets
- added \[Potency Glyph] spell power, add raw damage to Bullets

- reworked \[Size Normalizer] to utilze patch instead of MonoBehavior; works instantly and reliably

- rebalanced all of the initial release cards
- all block ability cooldowns start at 2.0s at battle starts

Under the hood:
- Implemented Hollow Life mechanic to handle temp HP caps incurred by **\[Orb-Literation]** and possibly future cards

- **\[Chompy Bullet]** and any future bullet modifiers only add one instance of the effect to each bullet, they will calculate the effect on the fly

- Disabled redundancy system that iterate and resolve unique and/or mutually exclusive cards (it will be other mods' faults that violates the backbones of the system)
</details>

<details>
<summary>Public Beta 1-2 [v0.1.13]</summary>

- Under the hood reworks of healing and damage multipliers.

- **\[Tactical Scanner]** now properly modify **healings** and **damages** taken and ignore all **direct health changes**.

- **\[Hollow Life]** changes:
  - rarity changed to **Rare**
  - Max HP gains changed from **x2.5** to **3x**
  - now giving **-15% healing effects**; reducing healing and regeneration

- Reduced the delay caused by card conflict resolver at the start of each round.
</details>

#### Public Beta 1-1 \[v0.1.9]
- Patched the logic behind the monobehavior that manages and prevents card conflicts, to execute from the main mod class instead of from each players!
- **\[Shield Battery]** sneak peak

#### Public Beta 1-0 \[v0.1.0]
- It all begins. Starting out with 5 wacky cards and 3 minor all-around passive cards
