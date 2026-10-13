# Frends.Hl7v2.CreateFromXml

Task to create Hl7v2 message from Xml

[![CreateFromXml_build](https://github.com/FrendsPlatform/Frends.Hl7v2/actions/workflows/CreateFromXml_test_on_main.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.Hl7v2/actions/workflows/CreateFromXml_test_on_main.yml)
![Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.Hl7v2/Frends.Hl7v2.CreateFromXml|main)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

## Installing

You can install the Task via Frends UI Task View.

## Building

### Clone a copy of the repository

`git clone https://github.com/FrendsPlatform/Frends.Hl7v2.git`

### Build the project

`dotnet build`

### Run tests

Run the tests

`dotnet test`

### Run large-message benchmarks

The explicit `LargeMessageBenchmarks` fixture is excluded from normal test runs.
Run it from this solution directory in Release mode:

```powershell
$env:HL7_BENCHMARK_STAGE = "01 - Baseline"
$env:HL7_BENCHMARK_REVISION = git rev-parse --short HEAD
dotnet test .\Frends.Hl7v2.CreateFromXml.Tests\Frends.Hl7v2.CreateFromXml.Tests.csproj -c Release --filter "FullyQualifiedName~LargeMessageBenchmarks"
```

Each run appends a timestamped table to `TestResultFile.md` in this directory
(ignored by Git). Change the stage label when comparing implementations.
`HL7_BENCHMARK_RESULTS` can override the output path;
`HL7_BENCHMARK_ITERATIONS` controls the sample count (default: 3).

The synthetic workloads distinguish byte size from sibling count: 1,000–8,000
observation groups under one parent, observations spread across orders, 1/10 MiB
single-field values, and 1,000–8,000 repetitions of one field. The tests verify
the entire output against stock NHapi and independently generated expected ER7.
They report median task time/managed allocations and separate diagnostic DOM,
stock XML parse, pipe encode and newline-conversion timings. Input generation
and assertions are not timed. Allocations are not peak memory measurements.
There are no machine-dependent timing assertions; use the same machine,
configuration and sample count for comparisons. Cold-start costs are excluded.
The stock-parser comparisons can take several minutes on slower machines.

The final implementation also includes an explicit 64,000-sibling stress
benchmark (over 10 MiB under one order). It checks the complete expected output
without running the known-slow stock parser at that size. Its supplemental
report is not a baseline speedup comparison.

### Create a NuGet package

`dotnet pack --configuration Release`

### StyleCop.Analyzers Version
This project uses StyleCop.Analyzers 1.2.0-beta.556, as recommended by the author, to get the latest fixes and improvements not available in the last stable release.
