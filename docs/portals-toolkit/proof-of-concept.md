# Recommended first proof of concept

## Decision and scope

**Proposal: a reusable world-space terrain surface component**, evaluated on fixed canyon and mountain meshes. No implementation is included in Phase 1.

Adapt a deliberately small subset of [src/kit/terrain/material.js](https://github.com/emollick/ai-portals/blob/210133f4e718acb7b461f2a40c90661a40a2397f/src/kit/terrain/material.js#L80-L208): slope-based rock exposure, altitude/slope snow, macro albedo variation, and one filtered gradient-detail layer. Support a small authored strata profile for the canyon; the procedural geological-table generator is a follow-up, not a hidden prerequisite. This validates the most important Swift-to-shader contract without requiring a new terrain mesher or renderer engine.

The reusable output is material data and surface evaluation: linear albedo, roughness and perturbed normal. It does not own spacecraft motion, collisions, camera paths, biome gameplay, scene loading, directional lights or post effects.

## Why start here

| Alternative | Why the surface experiment is preferable first |
|---|---|
| Noise library alone | Easy to test, but a new noise field alone does not establish a meaningful visual improvement |
| Adaptive terrain mesher | Adds topology, cracks, queries and route/free-flight differences before testing the surface look; geometry can stay fixed initially |
| Full mountain/canyon generator | Changes silhouette and shading simultaneously, making visual attribution harder; useful next after the surface contract works |
| Cloud fields or full atmosphere | Transparency, sorting, sky integration and overdraw make costs harder to bound; gradient/haze can follow separately |
| Full vegetation kit | Requires atlas coverage, wind and shadow agreement, placement and culling together |
| Complete Portals world | Carries authored route assumptions, special effects, lifecycle and broader provenance; weak reusable boundary |

Licensing remains manageable for the clarified **noncommercial** experiment: record the adapted shader functions, retain Portals attribution/CC terms and avoid unclear RNG/hash fragments. Use a newly authored fixed macro/detail fixture or a generator from a verified source. No copied Portals textures or world meshes are required. See [licensing](licensing-assessment.md); this does not claim the adapted material becomes MIT.

## Minimum implementation proposed for Phase 2

1. Define validated surface recipe and CPU reference mask evaluation in Core/Surface. Use explicit linear colour and world metres.
2. Build one native shader integration with matching parameters and one low/full detail variant. A simple periodic detail fixture avoids building the entire cellular texture kit.
3. Add a standalone minimal viewer with two original fixed heightfield fixtures: one with broad mountains, steep rock faces and a snow line; one with stepped orange canyon walls, a floor and a few neutral boulders. Reuse the same mesh in baseline and enhanced views.
4. Render an untextured lit baseline beside/toggled against the new surface, sharing camera, mesh, light, exposure and tone mapping. Provide flat diagnostic views for slope/snow masks, albedo and normals.
5. Record captures and performance results on one selected supported iPhone and one Mac, using a recorded camera sequence and fixed resolution. Select and record actual models, OS, build mode and thermal conditions before judging results.

Start with the StarFlight renderer capability inspection in the next phase. If integration support is uncertain, use a tiny direct-Metal viewer to validate the surface, then budget the specific game adapter. No claim of drop-in compatibility is made. Phase 1 did not inspect or modify StarFlight.

## Proposed acceptance criteria

These are **targets**, not measured results or guarantees. The target-device floor is an open input to Phase 2; do not silently redefine “supported hardware” to make the experiment pass. Measurements use release builds, fixed scene/resolution and no unrelated effect changes.

| Area | Proposed measurable gate |
|---|---|
| CPU mask correctness | Ground/rock/snow weights finite, in [0,1], sum within 1e-5 of one; test flat/vertical/intermediate normals, both sides of thresholds, snow disabled and low/high altitude |
| GPU reference agreement | Diagnostic float readback at controlled points differs from CPU mask weights by at most 1e-3, away from raster interpolation edges; same inputs and float precision documented |
| World-space stability | Two adjacent mesh chunks with identical world-position samples yield mask differences ≤1e-3; transformed instances agree with an equivalent baked world transform. Camera motion must not move rock/snow masks |
| Filtering and detail | At near/mid/far fixed captures, fine detail fades rather than forming persistent repeating grids. A 10-second motion capture at fixed exposure must pass review for shimmer; store baseline and enhanced clips for comparison |
| Visual contribution | Review six fixed views (near/mid/far for each fixture). Reviewer identifies rock/snow transition and macro variation in mountains, bands/detail on canyon walls, and confirms no loss of silhouette readability. Record pass/fail per view; subjective quality is not replaced by an arbitrary pixel metric |
| Predictable resources | Additional draws for the opaque terrain surface: zero; new data textures ≤4 MiB including mip levels; no per-frame texture or topology generation after warm-up; log resources to verify |
| Runtime overhead | At a fixed 1920×1080 offscreen render size on both named devices, aim for ≤1 ms p95 GPU increase over the same baseline, and ≤0.2 ms p95 CPU submission increase. If missed, simplify the shader and report results rather than claim success |
| Frame consistency | After pipeline/resource warm-up, run 60 seconds per fixture. Target total p95 frame time ≤16.7 ms and p99 ≤20 ms in this deliberately small demo; record worst frame and stall count. These are demo gates, not a promise for the full game |
| Preparation and lifetime | Prepare shader variants/uploads before the measurement interval; report cold preparation time separately. After 20 fixture swaps, live owned texture/buffer counts return to the initial steady-state count once GPU work completes |
| Integration boundary | Consume existing mesh positions/normals without changing terrain generation; disabling the component restores the original material path; no gameplay dependency in the package |
| Provenance | Every adapted function/resource has a pinned origin and licence entry; intended noncommercial scope and distribution terms documented |

The 1 ms GPU allowance is a budget hypothesis. Use GPU timing for shader cost and end-to-end frame timing for smoothness; CPU wall time around an asynchronous draw is insufficient. If the selected low-end device cannot meet the tiny demo's baseline, report that baseline explicitly and reconsider scope/resolution before drawing conclusions about the material.

## Tests and demonstration artifacts for Phase 2

- Unit/property tests: threshold validation, zero/invalid normal handling, negative world heights, disabled snow, large coordinates within declared level bounds, weights and explicit linear-colour conversions. Do not reproduce every implementation line in a test.
- Numerical GPU tests: flat/sloped test patches, scaled/rotated transforms and matching CPU weights. Compare masks rather than fully lit pixels, which depend on renderer lighting and tone mapping.
- Texture tests: periodic wrapping and finite gradients where procedural detail is used; verify non-sRGB sampling for data channels, mip availability and resource-byte accounting.
- Visual checks: fixed seeds/fixtures and camera transforms with image references on the pinned renderer/device. Tolerate documented device differences; include motion clips because screenshots cannot prove temporal stability.
- Performance capture: baseline versus enhanced, cold versus warm, CPU/GPU distributions, memory and draw counts, and a sustained 10-minute flight-style run for thermal degradation after the short acceptance capture.

Deliver a small viewer, versioned recipe fixtures, source/provenance notes and a results sheet with device/build settings, raw timings and screenshots. Do not hide failed gates with automatic resolution changes during comparison.

## Incremental StarFlight integration after acceptance

In a separately authorized implementation phase, attach the chosen adapter to one existing constrained level's terrain material behind a feature flag. Supply world transforms and normals; select rock/snow settings in game-owned configuration. Keep geometry, controls and gameplay unchanged for the first comparison. Run the same recorded flight with baseline and enhanced materials on the supported hardware floor and check actual frame headroom before enabling it more broadly.

If successful, add coherent strata generation/terracing and terrain light bakes as separate experiments. Atmosphere should then share sun/horizon colours with host lighting. If shader integration or cost fails, retain the useful renderer-neutral surface data and reference tests, document the failure, and choose a simpler material path; do not expand into a replacement engine to rescue the POC.
