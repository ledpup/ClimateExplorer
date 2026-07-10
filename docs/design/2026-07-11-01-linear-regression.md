Create a C# linear regression utility with clearly isolated calculation sections.

## Context

I want a general-purpose linear regression implementation.

There are sample input and expected output files in:

`docs\design\linear-regression`

Use these files to create unit tests for the implementation.

Find **additional public sample datasets** suitable for simple linear regression tests. The results will be already calculated independantly. For each dataset:

- Add the input data to the test fixtures.
- Add expected output values.
- Include source/reference links in test comments or test documentation.
- Use small enough datasets that the test data remains readable.
- Prefer authoritative or educational references where the expected regression result is published or easily verifiable.

## Goal

Create a reusable C# implementation for simple linear regression where the logical result sections are clearly isolated.

The implementation should preferably fit as multiple methods in one class in the `stats` folder.

Suggested location:

`ClimateExplorer.Core/stats/LinearRegressionCalculator.cs`

## Required design

Implement the regression in logical blocks. The public API should make these sections clear:

### 1. Input summary

The input summary should include:

- Count
- Minimum X
- Maximum X
- Mean X
- Mean Y
- Any useful validation/sanity information

### 2. Best-fit line

The best-fit line should include:

- Slope
- Intercept
- Ability to calculate predicted Y for a supplied X

Equation form:

`Y = Intercept + Slope * X`

### 3. Fit / explanatory power

The fit section should include:

- R²
- Residual standard error
- Residual sum of squares
- Total sum of squares

### 4. Significance / uncertainty

The significance section should include:

- Slope standard error
- t statistic or F statistic
- p-value for whether slope is significantly different from zero
- Degrees of freedom
- Slope confidence interval, probably 95% by default
- Boolean significance flag based on a supplied alpha value, default `0.05`

### 5. Prediction / projection

Prediction should be logically separate from the main regression calculation.

Provide a method that, given an X value, returns:

- Predicted Y
- Confidence interval for the fitted mean
- Prediction interval for an individual observation

Use a default 95% interval, but allow the confidence level or alpha to be supplied.

## API shape

Prefer a clean object model, something like:

- `DataPoint`
  - `double X`
  - `double Y`

- `LinearRegressionResult`
  - `RegressionInputSummary Input`
  - `RegressionLine Line`
  - `RegressionFit Fit`
  - `RegressionSignificance Significance`

- `RegressionPrediction`
  - `double X`
  - `double PredictedY`
  - `ConfidenceInterval MeanConfidenceInterval`
  - `ConfidenceInterval ObservationPredictionInterval`

- `ConfidenceInterval`
  - `double Lower`
  - `double Upper`

Exact names can be improved if the project already has conventions.

## Implementation requirements

- Use ordinary least squares simple linear regression.
- The implementation must handle unevenly spaced X values.
- Missing values should not be represented inside the calculator. The caller should pass only valid `(X, Y)` points.
- Avoid duplicate looping where reasonable, but prioritise readability.
- Keep the methods small and named after the logical calculation section.
- Add XML comments where they clarify statistical meaning.

## Important distinction

Keep these concepts separate:

- Confidence interval for the fitted mean
- Prediction interval for an individual observation

Do not conflate them.

## Unit tests

Create comprehensive unit tests.

The tests should verify, within tolerances:

- Count
- Mean X
- Mean Y
- Slope
- Intercept
- R²
- Residual standard error
- Slope standard error
- p-value
- Slope confidence interval
- Prediction result for at least one X value if expected values are available

Also add tests for:

- Perfect positive line
- Perfect negative line
- No significant trend / near-flat data
- Unevenly spaced X values
- Invalid inputs
- All X values the same

## Additional reference datasets

Find at least two more public/reference regression examples and add tests for them.

For each one, document:

- Source URL
- Input data used
- Expected slope/intercept/R²/p-value must be available from the reference examples
- Any rounding/tolerance decisions

Do not use huge datasets.