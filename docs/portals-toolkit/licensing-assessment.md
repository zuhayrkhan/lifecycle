# Licensing assessment

Snapshot: `210133f4e718acb7b461f2a40c90661a40a2397f`; reviewed 2026-11-13. Intended use, clarified by Zuhayr: **noncommercial**. That removes commercial reuse as a current project requirement; it does not erase attribution, adaptation or distribution conditions. This is a source-based engineering assessment, not a legal opinion or a complete audit of every world.

## Confirmed repository terms

[LICENSE](https://github.com/emollick/ai-portals/blob/210133f4e718acb7b461f2a40c90661a40a2397f/LICENSE) contains **Creative Commons Attribution–NonCommercial–ShareAlike 4.0 International**, consistent with [package.json](https://github.com/emollick/ai-portals/blob/210133f4e718acb7b461f2a40c90661a40a2397f/package.json). The permissions and obligations come from the licence text, not the repository's availability on GitHub.

Relevant local licence sections:

- Section 1(a) includes translation and modification in the definition of adapted material where copyright permission is needed. A JavaScript-to-Swift or GLSL-to-MSL translation does not wash away provenance.
- Section 2(a)(1) grants reproduction and production/sharing of adaptations for noncommercial purposes. Noncommercial is defined in section 1(k) by purpose, not merely whether a fee is charged.
- Section 3(a), when sharing, requires retention of supplied attribution and notices, identification of modifications and a licence reference/link.
- Section 3(b), when sharing adaptations, requires an adapter's licence with the same licence elements under the specified version/compatibility rules, and forbids restrictive additional terms or effective technological measures on the adapted material.
- Section 2(b) does not grant patent or trademark rights. Section 5 gives no warranty of rights clearance or fitness.

The [official CC summary](https://creativecommons.org/licenses/by-nc-sa/4.0/) confirms the attribution, noncommercial, ShareAlike and no-additional-restrictions conditions; the [legal code](https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode.en) controls. Creative Commons [advises against CC licences for software](https://creativecommons.org/faq/#can-i-apply-a-creative-commons-license-to-software), including because of software-specific issues such as source distribution and patents. That advice does not invalidate the author's chosen licence.

## Consequences by activity

| Activity | Assessment for this initiative | Practical action |
|---|---|---|
| Direct Swift translation of Portals algorithms | Treat as adapted Portals material unless an independently verified exception applies | Track original path/commit, author credit, retained notices and precise changes; distribute under appropriate BY-NC-SA terms |
| GLSL copied or adapted into MSL | Same copyright question as other source; shader code is not exempt | Keep shader provenance alongside resources; review separately credited upstream functions |
| Modify a shader or reorganize modules | Renaming, optimization or splitting targets does not make the inherited expression independently authored | Record changes; do not mark an entire adapted module as MIT merely because some functions were MIT upstream |
| Independently implement known math | Ideas/mathematical algorithms can often be implemented independently, but expression and legal boundaries are jurisdiction-dependent | Work from verified publications/permissive upstreams, write new code and tests, record provenance; avoid assuming inspected source can be rephrased into a licence-free implementation |
| Private noncommercial experiment | Consistent with the licence's noncommercial grant, subject to applicable terms | Keep provenance now so later sharing is possible; do not need commercial permission merely to complete this research |
| Publish reusable Swift source/binaries | Sharing triggers attribution and, for adaptations, ShareAlike requirements | Include licence texts, notices, source links, modification log and module-level origin records. CC BY-NC-SA is not a conventional source-offer licence; do not invent a GPL-like source-disclosure rule |
| Use in a noncommercial StarFlight build | Current intended direction is compatible in principle | Keep library and game notices accurate; inspect actual packaging/distribution terms before public release |
| Later commercial game or commercial toolkit reuse | Not covered for adapted Portals material by the noncommercial grant | Obtain suitable rights from relevant holders or replace affected implementations with independently sourced alternatives; an MIT noise helper does not clear the rest |

ShareAlike is not a basis for declaring that every independent game file must automatically use CC BY-NC-SA. The boundary between an adaptation and a collection/aggregation, including linked libraries, needs review for the actual composition. Conversely, a separate SPM target does not guarantee separation in copyright terms. App-store/DRM and recipient terms also require review before distribution; do not assert either blanket incompatibility or blanket permission without the actual delivery arrangement.

## Third-party inventory and provenance confidence

[THIRD-PARTY-NOTICES.txt](https://github.com/emollick/ai-portals/blob/210133f4e718acb7b461f2a40c90661a40a2397f/THIRD-PARTY-NOTICES.txt) explicitly identifies the following. “Confirmed notice” means the repository makes the claim; it is not a complete upstream code-identity audit.

| Component | Evidence and current confidence | Obligations / unresolved work |
|---|---|---|
| Three.js release 170 and addons | Vendored source and import map; MIT notice in Portals. [Official r170 licence](https://raw.githubusercontent.com/mrdoob/three.js/r170/LICENSE) also inspected | Preserve copyright and MIT text if any source/substantial portions are reused. Calling Apple APIs instead need not carry Three's implementation. Notice passes through Ericson triangle-distance and Stephen Hill tone-fit credits for specified worlds; retain where relevant |
| David Hoskins “Hash without Sine” | Notices and `GLSL_HASH` header in [fx.js](https://github.com/emollick/ai-portals/blob/210133f4e718acb7b461f2a40c90661a40a2397f/src/kit/fx.js#L14-L27) identify MIT and author | Keep credit/MIT notice for translated functions. Upstream Shadertoy terms/code identity were not independently verified in this assessment |
| Ashima Arts / Stefan Gustavson webgl-noise | Notices and `GLSL_SIMPLEX` header in [fx.js](https://github.com/emollick/ai-portals/blob/210133f4e718acb7b461f2a40c90661a40a2397f/src/kit/fx.js#L41-L48). [Upstream licence](https://raw.githubusercontent.com/ashima/webgl-noise/master/LICENSE) inspected and consistent with MIT terms | Retain both supplied copyright lines and permission text. For an upstream-based implementation, pin an exact upstream revision rather than depending on moving `master` |
| Inigo Quilez smooth min/max, distance and texture noise helpers | Notices; `heights.js` smooth-min header and `sky.js` `vn3` header identify source/MIT | Verify original function/version if extracting separately. Attempt to open the smooth-min article returned HTTP 403; its current terms were not independently confirmed here. Attribution in Portals is evidence, not a warranty |
| mulberry32 RNG form | Notices name Tommy Ettinger and explicitly say no licence statement was found; core and sky contain that form | Do not silently assert MIT or public domain. For POC select a documented generator with verified licence or retain this as a specific unresolved provenance item |
| Sine/fract hashes and common constants | Notices explicitly cannot establish author/licence for all variants | Commonness is not clearance. Avoid copying uncertain forms; prefer documented alternatives or independent arithmetic mapping |
| esbuild 0.28.2 | Development dependency; [package-lock.json](https://github.com/emollick/ai-portals/blob/210133f4e718acb7b461f2a40c90661a40a2397f/package-lock.json) records MIT for resolved packages | Build tooling is not needed by the proposed Swift runtime. If redistribution ever includes it, verify package notices and binaries separately |
| Other worlds/assets | Representative code inspection only; full world and art inventory not audited | Do not import thumbnails, branding, generated assets or unrelated world code under an assumption of universal clearance |

The vendored Three MIT licence and webgl-noise licence allow broad use with retained notices. Their inclusion does **not** turn Portals' original material into permissively licensed code. A separately identified original upstream function may be obtained under its upstream terms; Portals-specific surrounding work and modifications must be evaluated separately.

## Recommended project handling

For this noncommercial initiative, adaptation under BY-NC-SA is a reasonable path, with a small per-component provenance ledger: source URL and commit, symbol, original holder/notice, inherited licence, modifications, intended distribution and any unresolved origin. Keep clearly identified MIT pieces and original new code distinguishable from adapted Portals modules; do not impose an accidental blanket licence on unrelated Lifecycle code.

Before any later public toolkit/game distribution, resolve the chosen RNG provenance, exact shader upstream terms where used, adaptation boundaries, credits and distribution restrictions. If eventual permissive reuse matters, independently source those components from appropriately licensed upstreams from the beginning. This is an architectural option, not a new blocker for the user's stated noncommercial scope.

A suitable future attribution entry would identify “Portals — Ethan Mollick / emollick/ai-portals”, the exact source commit and files, CC BY-NC-SA 4.0, and that the relevant code was adapted into Swift/MSL, followed by actual third-party notices. Verify the supplied author designations and modification list when producing the real component; this report does not create an attribution notice for nonexistent translated code.
