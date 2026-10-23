# Carbon Cycle page — Tab 1 step 1: chemistry calculation engine

- **Date:** 2026-10-23
- **Status:** Proposed (ideal and real fuels added 2026-10-23)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Technical plan for [Tab 1 §8](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) step 1: the calculation engine and its unit tests (stoichiometry, product masses, heat released or absorbed, carbon split, calcination). A new UI-free model project and a new unit test project.
- **Builds on:** [Tab 1: Fossil fuels and limestone](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md); [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md) (§3.1 constants vs assumptions, §3.4 engine design)
- **Branch context:** `issues/carbon-cycle`

The engine is written in C#, but this plan is deliberately language-neutral: it describes concepts, data and behaviour, not types or syntax. Section references without "Tab 1" or "overview" are to this document.

---

## 1. Scope

**In:**
- A chemistry catalog: elements, chemical species and reactions, holding only what the Tab 1 plan and its [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md) use (§4.5).
- Fuels as mixtures of components: **ideal fuels** (one pure molecule) and **real fuels** (the mixture actually sold and burned) (§5). Rock products for calcination.
- The stage 1 calculation: fuel combustion with a carbon split, SO₂ and NOx, calcination and the fuel that heats it, heat released or absorbed (§6).
- The three stage 1 input modes: fuel quantity, energy, rock or product quantity (§6.4).
- Unit tests (§8).

**Out:**
- Sourcing and verifying the values. That is Tab 1 step 2. This step uses the provisional values already in the Tab 1 plan, marked "to verify", and the tests use tolerances that survive the switch to sourced values.
- The parameter system (overview §5 step 1). The engine takes its assumptions as plain inputs and gives every constant a stable id and a source, so the parameter system can expose them later without changing the engine (§4.6).
- `EmissionResult` (overview §5 step 3). Mapping the Tab 1 result onto it is Tab 1 step 8.
- Presets, population scaling and the emission profile. These are inputs to the engine (a preset is a parameter set; population scaling multiplies a quantity), so they belong to the calculator UI step.
- Stage 2 combustors and upstream. A combustor takes a real fuel and sets how completely it burns, its NOx and its SO₂ removal ([combustor sub-plan §3](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md)). It never changes the fuel's composition.

## 2. Design decisions

The starting sketch was element → compound → reaction: an element with atomic number, mass and symbol; a compound made of two or more elements whose mass is the sum of its elements; a reaction with reactants, products and an energy. The three-layer shape is right and is kept. These changes are needed for it to cover the plans:

1. **Species, not compounds.** Several things in the plans are made of one element: O₂, N₂, C (coal as carbon, and soot), S, and the atoms O, H and N in the Zeldovich and ozone steps. "Two or more elements" would exclude O₂, the reactant in every combustion. The middle layer is a *chemical species*: one or more atoms of one or more elements.
2. **Formulas carry counts.** CO₂ is one C and *two* O. A species' molar mass is the sum of count × atomic weight, not the sum of its distinct elements.
3. **Species carry a phase.** Water as vapour or liquid changes methane's heat from −802 to −890 kJ/mol (Tab 1 §2.2, gross vs net). Soot is treated as solid carbon (graphite). A species is identified by formula *and* phase.
4. **Reactions carry coefficients.** `CH₄ + 2 O₂` is a list of (coefficient, species) pairs, not a list of species. Coefficients can be fractions (`½ O₂`, `12.5 O₂`).
5. **Reaction energy is derived, not stored.** Each species holds its standard enthalpy of formation. A reaction's enthalpy is products minus reactants (Hess's law). This reproduces every number in the Tab 1 §2.2 table from about twenty species values, guarantees that related reactions agree with each other (the CO steps sum to the CO₂ value), and gives the generated reactions of §6 an energy for free. Species that are only shown, such as radicals, have no enthalpy, and a reaction containing one has no energy.
6. **One sign convention.** Enthalpy change, negative when heat is released, as in the Tab 1 plan's tables. The UI turns it into "released" or "absorbed".
7. **Ideal fuels and real fuels.** A fuel is a mixture of components, each with a mass fraction (§5).
   - An **ideal fuel** is one pure molecule: coal as carbon, natural gas as methane, petrol as octane. Everything about it comes from the catalog: a clean balanced equation, and heat from enthalpies of formation. It is what the Level 1 chemistry shows (Tab 1 §2).
   - A **real fuel** is what is actually sold and burned: petrol is alkanes around octane, plus aromatics and a trace of sulfur; coal is carbon, hydrogen, oxygen, nitrogen, sulfur, moisture and ash. It is what the fuel table, the calculator and every combustor use. Its heat comes from its net calorific value, an editable assumption (IPCC first), because coal has no enthalpy of formation and a blend's sourced value is more reliable than one added up from guessed parts.
   - The switch from ideal to real belongs to the fuel, not the combustor. Composition is a property of the fuel; completeness, NOx and scrubbing are properties of the combustor.
8. **Incomplete combustion works on both.** The carbon split applies to an ideal fuel (the Tab 1 §3 methane series is pure methane) and to a real one. Sulfur needs a real fuel, since no ideal fuel contains any; `S + O₂ → SO₂` can still be shown on its own.
9. **Some reactions are generated.** The equation for "diesel with 2% of its carbon going to CO" is not in any table. The engine builds it from the fuel's composition and the carbon split, then balances and displays it like a catalog reaction.
10. **Not every output is stoichiometric.** NOx comes mostly from nitrogen in the air, in amounts set by flame temperature, so stage 1 treats it as an emission factor (g per kg of fuel), not a reaction product.
11. **Reactions run in any direction of known quantity.** "1 t of clinker" fixes the CaO, from which the CaCO₃ and CO₂ follow; "1 GJ of heat" fixes the fuel. A reaction is scaled from a known amount of *any* participant, or from its heat.
12. **Mechanisms are display data with one check.** The ozone and sulfate chains include things that are not species, the third body `M` and sunlight. They are stored as conditions on a step, not as species. A mechanism is its ordered steps plus its net reaction, and the engine checks that the steps sum to the net.

## 3. Projects

- **Model project** (suggested name `ClimateExplorer.CarbonCycle`): the engine. No UI, no data access, no file input or output, no dependencies beyond the standard library. It must run inside the Blazor WebAssembly client, since the page recalculates as the reader edits. Later tabs (4–6) can add their models to it.
- **Unit test project** (suggested name `ClimateExplorer.CarbonCycle.UnitTests`): references only the model project. Test names follow `MethodName_StateUnderTest_ExpectedBehavior`.
- Both are added to the solution. The web client references the model project when the UI steps begin, not in this step.

The catalog (§4.5) and the default fuels (§5.3) are defined in code rather than in a data file. They are small, and code keeps them type-checked and free of loading at start-up.

## 4. Chemistry catalog

Everything in the catalog is a Constant (overview §3.1): fixed, read-only and shown with its source.

### 4.1 Element

```
Element
  Symbol                // "C"; the identity
  Name                  // "Carbon", for tooltips and accessible names
  AtomicNumber          // 6; ordering and identity check
  StandardAtomicWeight  // g/mol, e.g. 12.011
  Source                // e.g. IUPAC abridged standard atomic weights
```

Nothing else is needed. Isotopes, charge and electron structure play no part in the plans.

### 4.2 Chemical species

```
Species
  Id                    // stable key, e.g. "CO2(g)", "H2O(l)", "C(s,graphite)"
  Name                  // "Carbon dioxide", "Water vapour", "Soot (as graphite)"
  Formula               // ordered (element, count) pairs, as authored
  Phase                 // gas | liquid | solid | aqueous
  EnthalpyOfFormation   // kJ/mol at 25 °C, standard state; optional
  Source
  MolarMass             // derived: Σ count × atomic weight
```

- The formula is written as a string (`"CaCO3"`, `"Fe2O3"`, `"C8H18"`) and parsed into element counts. The authored order is kept for display, since chemists write CaCO₃ and not CCaO₃. No parentheses are needed for anything in §4.5.
- Display uses subscripts (CO₂) and, at Level 2, the phase (H₂O(g)).
- Counts are whole numbers. The per-carbon formula of a real fuel (§5.2) is generated for display and is not a catalog species.

### 4.3 Reaction

```
Reaction
  Id                    // e.g. "combustion.methane.complete"
  Name
  Reactants             // (coefficient, species) pairs
  Products              // (coefficient, species) pairs
  Conditions            // optional text: "heat", "sunlight", "+M", "in cloud", "flame temperature"
  EnthalpyChange        // derived, kJ per mole of reaction as written; absent if any species lacks a value
```

Operations:
- **Is balanced:** each element's atom count is equal on both sides, within a small tolerance. Every catalog reaction is checked by a test, and generated reactions are checked on creation.
- **Scale:** given a known moles, mass or heat for one participant (or for the reaction's heat), return the moles and mass of every participant and the heat. This is the core of every calculation in §6.
- **Normalise:** rescale so a chosen participant has coefficient 1 ("per mole of fuel").
- **Display coefficients:** the smallest whole-number form where one exists (`2 C₈H₁₈ + 25 O₂ → 16 CO₂ + 18 H₂O`), otherwise rounded decimals.
- **Add:** sum two reactions, cancelling species that appear on both sides. Used by the mechanism check and to show "fuel burn + calcination" as one line.

### 4.4 Mechanism

```
Mechanism
  Id, Name
  Steps                 // ordered reactions
  Net                   // reaction
```

The test checks that the steps, added with the right multiples, give the net reaction (§8). Mechanisms are for the Level 2 reaction display; the stage 1 numbers don't use them.

### 4.5 Catalog contents

Only what the plans use. Enthalpies are the provisional values that reproduce the Tab 1 plan's tables; Tab 1 step 2 verifies them (e.g. against NIST-JANAF / CODATA).

**Elements:** H, C, N, O, Mg, S, Ca, Fe.

**Species:**

| Species | Phase | ΔHf° (kJ/mol, to verify) | Used for |
|---|---|---|---|
| C (graphite) | s | 0 | Ideal coal; soot |
| H₂, O₂, N₂ | g | 0 | Reactants, products |
| S (rhombic) | s | 0 | `S + O₂ → SO₂` |
| Fe | s | 0 | Iron-ore reduction |
| CO₂ | g | −393.5 | |
| CO | g | −110.5 | |
| H₂O | g | −241.8 | Net heating value basis |
| H₂O | l | −285.8 | Gross heating value comparison; moisture in real fuels |
| CH₄ | g | −74.9 | Ideal natural gas; real natural gas component |
| C₂H₆ | g | −84.0 | Real natural gas component |
| C₃H₈, C₄H₁₀ | g | −104.7, −125.6 | Ideal LPG (propane); real LPG components |
| C₈H₁₈ (octane) | l | −250.1 | Ideal petrol; alkane component of real petrol |
| C₇H₈ (toluene) | l | +12.0 | Aromatic component of real petrol and jet fuel |
| C₁₂H₂₆ (dodecane) | l | −350.9 | Ideal jet fuel; alkane component of real jet fuel |
| C₁₆H₃₄ (cetane) | l | −456.1 | Ideal diesel; alkane component of real diesel |
| C₁₁H₁₀ (1-methylnaphthalene) | l | to source | Aromatic component of real diesel |
| C₂H₅OH (ethanol) | l | −277.6 | Fast-cycle component of E10 petrol |
| C₁₉H₃₆O₂ (methyl oleate) | l | to source | Fast-cycle component of B7 diesel (biodiesel) |
| CaCO₃ (calcite) | s | −1207.6 | |
| CaO | s | −634.9 | |
| MgCO₃, MgO | s | to source, −601.6 | Dolomitic lime |
| Fe₂O₃ | s | −824.2 | |
| NO, NO₂ | g | +90.3, +33.2 | |
| SO₂, SO₃ | g | −296.8, −395.7 | |
| O₃ | g | +142.7 | |
| CaSO₄ | s | −1434.5 | Flue-gas desulfurisation (combustor sub-plan) |
| H₂SO₄, H₂O₂ | aq | — | Display only |
| OH, H, O, N, HO₂, HOSO₂ | g | — | Mechanism steps; display only |

The component molecules of real fuels (toluene, methylnaphthalene, methyl oleate) are stand-ins for a class (aromatics, biodiesel), in the same way octane stands for petrol's alkanes. Their enthalpies are only needed for the derived heating-value check (§6.2), so a missing value disables that check rather than the fuel.

**Reactions** (calculation = used by §6; display = shown only):

| Reaction | ΔH (kJ, derived) | Use |
|---|---|---|
| `C + O₂ → CO₂` | −393.5 | Ideal coal; heat lost to soot (§6.2) |
| `C + ½ O₂ → CO` | −110.5 | Calculation |
| `CO + ½ O₂ → CO₂` | −283.0 | Heat lost to CO (§6.2) |
| `CH₄ + 2 O₂ → CO₂ + 2 H₂O(g)` | −802 | Ideal natural gas |
| `CH₄ + 2 O₂ → CO₂ + 2 H₂O(l)` | −890 | Gross vs net comparison |
| `CH₄ + 1.5 O₂ → CO + 2 H₂O` | −519 | Incomplete-combustion series |
| `CH₄ + O₂ → C + 2 H₂O` | −408 | Incomplete-combustion series |
| `C₈H₁₈ + 12.5 O₂ → 8 CO₂ + 9 H₂O` | −5,074 | Ideal petrol |
| `CxHyOz + (x + y/4 − z/2) O₂ → x CO₂ + (y/2) H₂O` | generated | Each molecular component of a real fuel (§5.2) |
| `S + O₂ → SO₂` | −296.8 | Calculation |
| `CaCO₃ → CaO + CO₂` | +178 | Calculation |
| `MgCO₃ → MgO + CO₂` | to source | Calculation (dolomitic lime) |
| `N₂ + O₂ → 2 NO` | +180.6 | Display (NOx is a factor, §2 item 10) |
| `2 NO + O₂ → 2 NO₂` | −114 | Display |
| `CH₄ + 2 H₂O → CO₂ + 4 H₂` | +165 | Display (steam methane reforming) |
| `Fe₂O₃ + 3 CO → 2 Fe + 3 CO₂` | derived | Display (iron-ore reduction) |
| `CaO + CO₂ → CaCO₃` | −178 | Display (recarbonation) |
| `CaCO₃ + SO₂ + ½ O₂ → CaSO₄ + CO₂` | derived | Display (combustor sub-plan) |
| `SO₃ + H₂O → H₂SO₄` and the other sulfate steps | — | Display |

**Mechanisms:** Zeldovich NO formation (Tab 1 §3), CO → ozone with net `CO + 2 O₂ → CO₂ + O₃` (Tab 1 §3), SO₂ → sulfate gas-phase route (Tab 1 §4).

When Tabs 2 and 3 are planned in detail, they add their own entries (N₂O, NH₃, the halogenated gases with F and Cl).

### 4.6 Ids and the parameter system

Every element, species, reaction and fuel has a stable id. Its fixed values map onto parameter ids of the overview §3.2 form, such as `element.C.atomicWeight` and `species.CO2(g).enthalpyOfFormation`, and a real fuel's assumptions map onto ids such as `fuel.petrol.component.aromatics` and `fuel.petrol.ncv`. When the parameter system exists, it lists the constants as locked and the assumptions as editable. Nothing in the engine changes.

## 5. Fuels and rock products

### 5.1 Fuel

```
Fuel
  Id                    // e.g. "fuel.petrol.ideal", "fuel.petrol"
  Name
  Kind                  // Ideal | Real
  IdealCounterpart      // real fuels only: the ideal fuel shown beside it (petrol → octane, coal → carbon)
  Components            // (component, mass fraction) pairs, fractions summing to 1
  NetCalorificValue     // MJ/kg; ideal: derived (Constant); real: Assumption
  Density               // kg/L, or kg/m³ for gas (Assumption; ideal fuels use the pure substance)
  NoxFactor             // g NOx (as NO₂) per kg fuel (Assumption)

Component
  Kind                  // Molecule (a catalog species) | Element (C, H, O, N, S) | Moisture | Ash
  Origin                // Fossil (default) | FastCycle (ethanol, biodiesel)
```

- **An ideal fuel** has one molecular component at 100%. Its heating value is derived from enthalpies of formation, with water as vapour, and is a locked Constant. It has no sulfur, moisture or ash.
- **A real liquid or gas fuel** is a few molecular components, each standing for a class: real petrol is alkanes (as octane), aromatics (as toluene), a trace of sulfur (as an element), and optionally ethanol.
- **A real solid or residual fuel** (coal ranks, coke, petroleum coke, heavy fuel oil) has no molecules to list. It is written as element components plus moisture and ash, which is the standard "ultimate analysis" of coal, as received. Hydrogen here is an element, not H₂.
- **Derived properties**, shown in the fuel table: carbon mass fraction, atomic H:C ratio, sulfur mass fraction, fast-cycle share of carbon, and the per-carbon formula `CH_y O_z S_w` (e.g. real petrol ≈ `CH₁.₉`). These replace the separately editable carbon fraction and H:C of the earlier draft, so they can never disagree with each other.
- **Editing:** the reader edits component fractions (e.g. aromatics share, sulfur ppm, ethanol share). The others rescale so the fractions still sum to 1, with the largest component taking up the difference.
- **Unburnt fuel is reported by component:** methane as CH₄, other molecular components as NMVOC, element-written fuels as NMVOC (§9).
- **Validation:** fractions in [0, 1] summing to 1; positive heating value and density; a real fuel has a heating value.

### 5.2 Generated per-carbon formula and reactions

For the reaction display, a real fuel is shown two ways at Level 2:
- **Per component:** each molecular component's balanced combustion reaction (`CxHyOz` general rule), weighted by its mass fraction.
- **Per carbon:** one reaction for `CH_y O_z S_w`, normalised per mole of fuel carbon, with the carbon split applied. Its coefficients are generally non-integers and display as rounded decimals.

The calculation itself (§6.1) works on element totals, which gives the same products as burning each component separately. A test checks this.

### 5.3 Default fuels

One ideal fuel per representative molecule (carbon, methane, propane, octane, dodecane, cetane), and one real fuel per row of the Tab 1 §5 fuel table. Real-fuel component fractions are provisional; Tab 1 step 2 sources them so that each real fuel's derived carbon content matches the IPCC carbon content × heating value (Tab 1 §5).

### 5.4 Rock products for calcination

```
CalcinationProduct
  Id                     // "product.clinker", "product.lime.highCalcium", "product.lime.dolomitic"
  CaOMassFraction        // e.g. clinker ~0.65 (editable)
  MgOMassFraction        // dolomitic lime only; 0 otherwise (editable)
CementClinkerRatio       // ~0.65–0.75 (editable)
```

Limestone itself is taken as pure CaCO₃, as in Tab 1 §2.1.

## 6. Calculation

All quantities are SI internally: kg, mol, J (overview §3.4 and Tab 1 §6). Values are 64-bit floating point. Unit conversion happens only at the UI.

### 6.1 Combustion with a carbon split

Inputs: a fuel (ideal or real), a fuel mass, and a carbon split (fractions of the fuel's carbon leaving as CO₂, CO, soot and unburnt fuel, summing to 1; default all CO₂).

- **Unburnt fuel** is a slice of the whole fuel, with its own composition, so its hydrogen and sulfur stay unburnt too. Its mass is the unburnt fraction × fuel mass. This matches the Tab 1 §3 methane series, where "unburnt slip" releases nothing.
- **The burnt remainder** is reduced to element totals (moles of C, H, O, N, S) from its components. Carbon goes to CO₂, CO or soot by the split, all hydrogen to H₂O (vapour), all sulfur to SO₂, and fuel nitrogen to N₂.
- **Oxygen from the air** = 1 O₂ per C to CO₂, ½ per C to CO, none for soot, ¼ per H, 1 per S, minus ½ per O atom already in the fuel. Fuel oxygen (coal, ethanol, biodiesel) therefore lowers the oxygen demand.
- **Moisture** leaves as water vapour, alongside the water formed from hydrogen. **Ash** passes through unchanged. Together these close the mass balance.
- **NOx** mass = factor × fuel mass, reported as NO₂, independent of the split. It takes no oxygen from the mass balance (it is small, and most of its N comes from the air).
- **Carbon origin:** every carbon result is split into fossil and fast-cycle by the components it came from. All of it is reported; whether it counts is the overview §2.3 rule, applied in Tab 1 step 8.

### 6.2 Heat

- **Ideal fuel, complete combustion:** from enthalpies of formation, through the catalog reaction. The derived heating values match the Tab 1 §2.2 table (C 32.8, CH₄ 50.0, octane 44.4 MJ/kg).
- **Real fuel, complete combustion:** burnt fuel mass × net calorific value (the Assumption).
- **Incomplete combustion, either kind:** subtract the heat not released by carbon that stopped at CO or soot, using catalog reactions: 283.0 kJ per mole of carbon left as CO (`CO + ½ O₂ → CO₂`) and 393.5 kJ per mole left as soot (`C + O₂ → CO₂`). For ideal methane this reproduces the Tab 1 §3 series exactly: −802, −519, −408 and 0 kJ.
- **Derived check for real fuels:** where every component is a molecule with an enthalpy of formation, the engine also derives the blend's heating value from its parts. The page shows it at Level 2 beside the sourced value, as a check on the composition. It is never used for the result.
- **Calcination:** heat absorbed comes from scaling `CaCO₃ → CaO + CO₂` (and `MgCO₃ → MgO + CO₂`).

### 6.3 Calcination and its heating fuel

- **Rock CO₂:** scale the calcination reaction from the known quantity: CaCO₃ mass (limestone input) or CaO mass (product input, = product mass × CaO fraction; cement first × clinker ratio). Dolomitic lime adds the MgCO₃ reaction from its MgO fraction.
- **Heating fuel:** stage 1 supplies exactly the reaction's own heat with no losses (Tab 1 §2.1). Fuel mass = heat absorbed ÷ the fuel's heat released per kg at its carbon split. That fuel then goes through §6.1 as a second stream.
- The result keeps the two streams separate: combustion and calcination (rock).

### 6.4 Input modes

1. **Fuel quantity:** a mass. Volume inputs are converted with the fuel's density before the engine, at the input layer.
2. **Energy:** heat wanted and a fuel. Fuel mass = heat ÷ heat released per kg at the chosen split, so a smoky flame needs more fuel for the same heat.
3. **Rock or product quantity:** limestone, clinker, cement or lime mass, plus the heating fuel (§6.3).

Any mode accepts an ideal or a real fuel, so the page can put the two side by side ("1 kg of coal as pure carbon, and as real bituminous coal").

### 6.5 Result

```
Tab1ChemistryResult
  Streams                   // combustion | calcination (rock); each with:
    Reactants               // mass and moles per species, including O₂ taken from the air
    Products                // mass and moles per species: CO₂, H₂O, CO, soot, unburnt fuel (as CH₄ or NMVOC),
                            //   SO₂, NOx (as NO₂), N₂ from fuel nitrogen, ash
    EnthalpyChange          // J; negative = released
    CarbonByFate            // kg C as CO₂, CO, soot, unburnt; each split fossil | fast-cycle
    Reactions               // the balanced reactions for display (§5.2)
  Totals                    // the same fields summed across streams
```

Mapping to `EmissionResult` (carbon accounting rules of overview §2.3: CO and unburnt fossil carbon counted, soot carbon not, fast-cycle CO₂ not) is Tab 1 step 8.

## 7. Validation and errors

- Reject, with a message naming the field: negative quantities, fractions outside [0, 1], component fractions or a carbon split not summing to 1 (within tolerance), non-positive heating value or density.
- Catalog errors (an unbalanced reaction, an unknown element symbol in a formula) are programming errors and fail the unit tests, not the page.
- Tolerances: balance and fraction sums use a relative tolerance of about 10⁻⁹; known-answer tests use the tolerance of the source value (§8).

## 8. Tests

Known answers are from the Tab 1 plan; tolerances allow for step 2's sourced values.

**Catalog**
- Molar masses: CO₂ 44.01, H₂O 18.02, CH₄ 16.04, C₈H₁₈ 114.23, CaCO₃ 100.09, SO₂ 64.06 g/mol.
- CO₂ ÷ C mass ratio = 3.664; SO₂ ÷ S ≈ 2.0; CO ÷ C = 28/12.
- Formula parsing: `Fe2O3` → Fe 2, O 3; an unknown symbol fails.
- Every catalog reaction is balanced in atoms and in mass.
- Derived enthalpies match the Tab 1 tables within 1%: C −393.5, CH₄ −802 (gross −890), octane −5,074, CaCO₃ +178, NO +180.6, S −296.8, and the methane series −519 and −408.
- `C + ½ O₂ → CO` plus `CO + ½ O₂ → CO₂` equals `C + O₂ → CO₂`, energy included.
- Each mechanism's steps sum to its net reaction.
- Display coefficients: octane shows as `2 C₈H₁₈ + 25 O₂ → 16 CO₂ + 18 H₂O`.

**Fuels**
- Ideal fuels' derived heating values: C 32.8, CH₄ 50.0, octane 44.4 MJ/kg.
- Ideal fuels' CO₂ per MJ: methane ~55, octane ~69, carbon ~112 g.
- Real petrol's derived H:C falls below octane's 2.25, in the Tab 1 §5 real-blend range.
- Component fractions not summing to 1 are rejected; editing one component rescales the rest to 1.
- Burning each component separately gives the same products as burning the element totals.
- E10: the ethanol's carbon is reported as fast-cycle, the rest as fossil.
- The derived heating value is present for an all-molecule fuel and absent for an element-written fuel (coal).

**Combustion**
- Carbon conservation: carbon in = carbon out across CO₂, CO, soot and unburnt, for many splits and for ideal and real fuels.
- Mass conservation: fuel + O₂ = all products, including moisture and ash.
- Fuel oxygen lowers oxygen demand: a coal with oxygen needs less O₂ than the same coal with that oxygen replaced by ash.
- 1 kg ideal coal → 3.664 kg CO₂; 1 L real petrol (0.74 kg) → ~2.3 kg CO₂; ideal methane → 2.75 kg CO₂ and 2.25 kg H₂O per kg.
- Carbon to CO releases ~28% of the heat of carbon to CO₂.
- Sulfur: 1 kg S → ~2 kg SO₂; an ideal fuel gives no SO₂.
- Linearity: 1 kg and 10¹² kg give results in exact proportion.

**Calcination and modes**
- 1 kg CaCO₃ → 0.44 kg CO₂; 1 t clinker at 65% CaO → ~0.51 t CO₂; 1 t pure CaO lime → 0.785 t CO₂.
- Heat absorbed ~4.0 MJ per kg of rock CO₂; heating it with ideal coal adds ~0.45 kg of fuel CO₂.
- Energy mode round trip: the fuel mass found for a given heat, burned in fuel-quantity mode, gives back that heat.
- Each validation rule rejects its bad input.

The overview §3.4 known-answer tests against the IPCC 95% ranges need the sourced real-fuel compositions, so they are added in step 2.

## 9. Risks and open questions

- **Two heating values for some real fuels.** The sourced value and the one derived from components can differ by a few per cent, because the stand-in molecules are approximate. The result always uses the sourced value; the page shows the gap at Level 2 and says why.
- **Choosing stand-in molecules.** Toluene for petrol aromatics and methylnaphthalene for diesel aromatics are reasonable but arbitrary. They are Level 2 detail; step 2 should check that the chosen fractions reproduce the sourced carbon content.
- **Unburnt solid fuel.** Unburnt coal is mostly carbon left in the ash, not a gas. Reporting it as NMVOC is wrong for coal; the alternative is a "carbon in ash" product that is not an air emission. Decide before the combustor sub-plan adds low-completeness rows such as brick kilns.
- **Soot as graphite.** Real soot is not pure graphite, but it is the standard simplification and gives the plan's −408 kJ.
- **NOx formation heat** (+180.6 kJ per 2 mol NO) is ignored in the heat total; it is far below the rounding of any fuel's calorific value.
- **Enthalpy sources differ in the last digit** (e.g. CH₄ −74.6 or −74.9). Pick one source for all species in step 2, so derived reactions stay consistent with each other.
- **Missing enthalpies:** MgCO₃, methylnaphthalene and methyl oleate vary between tables or are not yet sourced.

## 10. Work breakdown

1. Create the model and unit test projects and add them to the solution (§3).
2. Element, species and formula parsing; molar mass (§4.1, §4.2).
3. Reaction: balance check, scaling from any participant or heat, normalising, display coefficients, adding; mechanism and its net check (§4.3, §4.4).
4. Catalog contents (§4.5).
5. Fuel: components, ideal and real kinds, derived properties, editing and rescaling, default fuels (§5.1–§5.3).
6. Combustion: carbon split, element totals, products, NOx and SO₂, carbon origin, heat (§6.1, §6.2).
7. Calcination, products and heating fuel (§5.4, §6.3).
8. Input modes, validation and the result (§6.4, §6.5, §7).
9. Tests for each of the above (§8).
