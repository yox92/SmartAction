# SmartAction – Adaptive Healing/Food/Drink & Surgery QoL mod for SPT‑AKI

## Description
SmartAction is a mod for EFT SPT. It enhances the medical gameplay loop by making the healing, surgery, and food drink smarter and faster. It allows you to adjust the speed of medical items, surgeries, food, and water consumption based on your current movement state (Idle / Walk / Sprint).

---

## Key Features

| Feature                     | Description                                                                                                                                                                                                  | File(s)                                        |
|-----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------|
| **Adaptive healing speed**  | Dynamically speeds up or slows down animation and the *tick rate* of consummable/healing effects based on your current **movement state** (Idle / Walk / Sprint). Animation speed is synced to avoid desync. | `SpeedHealing.cs`, `PatchModulateHealSpeed.cs` |
| **Auto‑finish healed limb** | The mod automatically detects when the targeted body part is fully healed and forces the effect to **advance to `Residue`**, preventing wasted ticks.                                                        | `PatchDoMedEffect.cs`                          |
| **Quick‑cancel surgery**    | Double‑right‑click with a CMS or Surv12 kit to instantly **cancel the animation**, fast‑forward the internal state, *and drop the item* so you can keep moving.                                              | `DropSurgery.cs`                               |
| **Move Sprint**             | Can sprint when use consommable / healing. Can walk when surgery                                                                                                                                             | `**Transpiler**.cs`                            |

---

## ⚙️ Configuration

| Setting       | Default | Description                                                                  |
| ------------- |---------|------------------------------------------------------------------------------|
| `IdleSpeed`   | **15**  | x1.5 No move   when healing food/drink speed multiplier when standing still. |
| `WalkSpeed`   | **10**  | when walking (10 = vanilla)                                                  |
| `SprintSpeed` | **9**   | x0.9 when sprinting.                                                         |

Edit them in‑game (F12 ➜ SmartAction) or tweak the `.cfg` file while the game is closed.

> **Tip:** Extreme values may break animation sync – keep them reasonable.

---
## 🎮 In‑game usage

* **Adaptive healing** is automatic – just use any healing item and observe how the progress accelerates / decelerates as you move or stop.
* **Cancel surgery**:
    1. Start a CMS or Surv12 action.
    2. Within **0.2 s** double‑right‑click.
    3. The animation stops, and the kit is dropped in front of you.

A detailed log is printed to the BepInEx console/log file under the `SmartAction` tag.

---
