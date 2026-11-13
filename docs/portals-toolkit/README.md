# Reusable Swift procedural graphics toolkit — Phase 1

**Decision: technically viable as a selective toolkit; do not port the Portals engine.** Start with a terrain surface component, then add terrain synthesis, placement and atmospheric depth as separately validated capabilities.

Research only: no production Swift, package manifest, game changes or executable demonstration was created. The Swift declarations in the proposal are design sketches.

## Assessment

| Document | Contents |
|---|---|
| [Portals architecture](portals-architecture.md) | Source evidence, representative worlds, rendering lifecycle, limitations and performance |
| [Capability catalogue](capability-catalogue.md) | Evidence-backed candidates, A–D portability, dependencies, complexity and priorities |
| [Swift architecture](swift-architecture.md) | SPM targets, dependency diagram, typed API sketches and adapter boundaries |
| [Licensing assessment](licensing-assessment.md) | Noncommercial intended use, attribution, ShareAlike, upstream exceptions and open questions |
| [Proof of concept](proof-of-concept.md) | Narrow terrain surface experiment, tests, visual comparison and proposed acceptance gates |

## Executive decision

- Strongest candidates: seeded noise and height functions; shared geological strata; slope/altitude surface masks; baked terrain light; terrain-aware placement; sky/fog/light consistency.
- Pure CPU algorithms can become Swift value-oriented modules. GLSL materials require native shader work. Three.js scene traversal, shader-chunk injection, renderer warm-up and portal management require redesign or omission.
- Use one SPM package with independently importable core, terrain, surface and placement targets, plus optional Metal integration. Add vegetation and atmosphere only when justified. Avoid an engine-sized abstraction.
- Intended use is **noncommercial**, as clarified by Zuhayr. Portals' CC BY-NC-SA 4.0 permits noncommercial adaptations subject to its conditions; translation does not remove attribution or ShareAlike. Public distribution and third-party provenance still need care.
- First experiment: a static, world-space rock/snow terrain surface with slope and altitude masks, macro variation and filtered detail, on two fixed terrain fixtures. Do not begin with adaptive meshing, volumetric clouds or the whole world builder.

## Reproducibility and scope

Repository: [emollick/ai-portals](https://github.com/emollick/ai-portals). Analysed commit: **`210133f4e718acb7b461f2a40c90661a40a2397f`**, dated 2026-11-10, release message “Portals: the site's code at release s8f (471 worlds)”. Assessment date: 2026-11-13. Source links throughout are commit-pinned.

Clone and check the snapshot with:

```sh
git clone https://github.com/emollick/ai-portals.git ai-portals
cd ai-portals
git checkout --detach 210133f4e718acb7b461f2a40c90661a40a2397f
git rev-parse HEAD
```

The analysis checkout was outside this repository at `/tmp/portals-source-analysis`; it is temporary and not a delivery dependency. No Portals source or assets are vendored here. These documents are housed under Lifecycle's documentation solely because that was the supplied workspace; this is a standalone graphics initiative, not a Lifecycle feature or StarFlight refactoring.

Evidence labels: **Confirmed** = inspected implementation; **Inference** = engineering interpretation; **Proposal** = future design or acceptance target. No browser/GPU benchmarks, native prototype, or StarFlight renderer inspection were performed. Integration choices remain contingent on StarFlight's actual renderer and supported device floor.
