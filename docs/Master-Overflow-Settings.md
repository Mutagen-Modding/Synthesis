# Master Overflow Settings

Bethesda plugin files can only reference a limited number of master files (~255).  As load orders grow, a Synthesis patch that touches records from many different mods can run up against that ceiling.

Master Overflow settings let Synthesis handle that situation by **splitting your patch across several plugin files**, so that each individual file stays under the limit.  This is an opt-in feature: it is off until you turn it on.

When a patch would exceed the master limit, Synthesis writes the patch out as a numbered set of files instead of a single one:

```
Synthesis.esp
Synthesis_2.esp
Synthesis_3.esp
```

- The first file keeps the plain name.  Numbering on the additional files **starts at `_2`**
- Each file carries a subset of the records, chosen so that no single file exceeds the master limit
- Together they represent one logical patch

!!! info "It is still one patch"
    A split set is not "several patches".  Synthesis, and any patcher running after it, reads the whole set back as a single unified mod.  You do not need to configure anything extra, and patcher authors do not need to write special code.

## Split Files if Max Masters Exceeded

This is the main setting toggle.  When enabled, Synthesis splits patch output as described above rather than failing with a `TooManyMastersException`.

**Default:** OFF

## Update Load Order After Run

This setting automates the addition/removal of new split mod files to the load order as they appear or disappear.

**Default:** ON

## Requirements

### Version Requirement

!!! warning "All patchers in the group must support this feature"
    Split output can only be read back by patchers targeting **Synthesis v0.36.0 or later**.  A patcher on an older version that receives a split set as its input will not understand it, and will typically fail or misbehave

If you have patchers that must stay on older Synthesis versions:

- **Place them earlier in the group**, before the master limit is reached
- Patchers on modern versions can then come afterwards and handle any split files that appear

See [Versioning](Versioning.md#recommended-setup) for guidance on keeping patchers up to date.

### Adjacency Requirement

**All files in a split set must sit next to each other, in order, in your load order.**

Correct:

```
ModA.esp
Synthesis.esp
Synthesis_2.esp
Synthesis_3.esp
ModB.esp
```

Not allowed:

```
ModA.esp
Synthesis.esp
ModB.esp          <-- breaks up the split set
Synthesis_2.esp
```

If the set is broken up, Synthesis will refuse to run and report a non-adjacent split mods error naming the files involved.  The fix is to move the numbered files back together, immediately following their base file.
