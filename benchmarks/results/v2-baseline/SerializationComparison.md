```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]     : .NET 10.0.7 (10.0.726.21808), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.7 (10.0.726.21808), X64 RyuJIT AVX2


```
| Method                          | Job        | Toolchain              | Mean        | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------- |----------------------- |------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| SimpleModel_Stj_Reflection      | DefaultJob | Default                |    139.2 ns |   2.25 ns |   2.10 ns |   1.00 |    0.02 | 0.0155 |     296 B |        1.00 |
| SimpleModel_Stj_SourceGen       | DefaultJob | Default                |    136.3 ns |   1.31 ns |   1.16 ns |   0.98 |    0.02 | 0.0155 |     296 B |        1.00 |
| SimpleModel_PopcornDefault      | DefaultJob | Default                |    208.2 ns |   2.70 ns |   2.52 ns |   1.50 |    0.03 | 0.0279 |     528 B |        1.78 |
| SimpleModel_PopcornAll          | DefaultJob | Default                |    297.6 ns |   4.80 ns |   4.49 ns |   2.14 |    0.04 | 0.0377 |     712 B |        2.41 |
| SimpleModel_PopcornCustom       | DefaultJob | Default                |    276.9 ns |   2.59 ns |   2.30 ns |   1.99 |    0.03 | 0.0300 |     568 B |        1.92 |
| SimpleModelList_Stj_Reflection  | DefaultJob | Default                | 11,758.5 ns | 215.88 ns | 201.93 ns |  84.50 |    1.87 | 1.4954 |   28504 B |       96.30 |
| SimpleModelList_Stj_SourceGen   | DefaultJob | Default                | 12,835.0 ns | 107.34 ns |  95.15 ns |  92.24 |    1.49 | 1.4954 |   28504 B |       96.30 |
| SimpleModelList_PopcornDefault  | DefaultJob | Default                | 12,637.5 ns | 187.55 ns | 175.44 ns |  90.82 |    1.80 | 1.5564 |   29408 B |       99.35 |
| SimpleModelList_PopcornAll      | DefaultJob | Default                | 21,148.7 ns | 198.06 ns | 175.57 ns | 151.99 |    2.52 | 2.5024 |   47616 B |      160.86 |
| SimpleModelList_PopcornCustom   | DefaultJob | Default                | 19,477.1 ns | 186.02 ns | 164.90 ns | 139.97 |    2.33 | 1.7090 |   32440 B |      109.59 |
| ComplexModel_Stj_Reflection     | DefaultJob | Default                |  1,268.2 ns |  19.82 ns |  18.54 ns |   9.11 |    0.18 | 0.1602 |    3032 B |       10.24 |
| ComplexModel_Stj_SourceGen      | DefaultJob | Default                |  1,253.1 ns |  23.01 ns |  21.52 ns |   9.01 |    0.20 | 0.1602 |    3032 B |       10.24 |
| ComplexModel_PopcornDefault     | DefaultJob | Default                |    206.5 ns |   1.78 ns |   1.58 ns |   1.48 |    0.02 | 0.0241 |     456 B |        1.54 |
| ComplexModel_PopcornAll         | DefaultJob | Default                |  1,585.8 ns |  13.71 ns |  12.15 ns |  11.40 |    0.19 | 0.1717 |    3240 B |       10.95 |
| ComplexModel_PopcornCustom      | DefaultJob | Default                |  1,781.7 ns |  12.23 ns |  10.21 ns |  12.80 |    0.20 | 0.1640 |    3088 B |       10.43 |
| ComplexModelList_Stj_Reflection | DefaultJob | Default                | 26,327.4 ns | 393.58 ns | 368.15 ns | 189.20 |    3.76 | 2.8687 |   54768 B |      185.03 |
| ComplexModelList_Stj_SourceGen  | DefaultJob | Default                | 25,764.8 ns | 327.23 ns | 306.09 ns | 185.16 |    3.43 | 2.8992 |   54776 B |      185.05 |
| ComplexModelList_PopcornDefault | DefaultJob | Default                |  3,039.2 ns |  21.57 ns |  18.01 ns |  21.84 |    0.34 | 0.2861 |    5400 B |       18.24 |
| ComplexModelList_PopcornAll     | DefaultJob | Default                | 25,419.0 ns | 312.53 ns | 292.34 ns | 182.67 |    3.34 | 2.4414 |   46208 B |      156.11 |
| ComplexModelList_PopcornCustom  | DefaultJob | Default                | 28,493.1 ns | 226.62 ns | 189.24 ns | 204.77 |    3.25 | 2.1362 |   40480 B |      136.76 |
|                                 |            |                        |             |           |           |        |         |        |           |             |
| SimpleModel_Stj_Reflection      | Job-YCYDSY | InProcessEmitToolchain |    140.2 ns |   1.24 ns |   1.16 ns |   1.00 |    0.01 | 0.0155 |     296 B |        1.00 |
| SimpleModel_Stj_SourceGen       | Job-YCYDSY | InProcessEmitToolchain |    148.5 ns |   1.48 ns |   1.39 ns |   1.06 |    0.01 | 0.0169 |     320 B |        1.08 |
| SimpleModel_PopcornDefault      | Job-YCYDSY | InProcessEmitToolchain |    201.1 ns |   1.67 ns |   1.56 ns |   1.43 |    0.02 | 0.0250 |     472 B |        1.59 |
| SimpleModel_PopcornAll          | Job-YCYDSY | InProcessEmitToolchain |    315.5 ns |   3.95 ns |   3.70 ns |   2.25 |    0.03 | 0.0377 |     712 B |        2.41 |
| SimpleModel_PopcornCustom       | Job-YCYDSY | InProcessEmitToolchain |    307.0 ns |   1.69 ns |   1.41 ns |   2.19 |    0.02 | 0.0300 |     568 B |        1.92 |
| SimpleModelList_Stj_Reflection  | Job-YCYDSY | InProcessEmitToolchain | 12,638.9 ns | 114.10 ns | 106.73 ns |  90.16 |    1.03 | 1.3733 |   26112 B |       88.22 |
| SimpleModelList_Stj_SourceGen   | Job-YCYDSY | InProcessEmitToolchain | 12,064.5 ns | 121.49 ns | 113.64 ns |  86.06 |    1.04 | 1.5106 |   28520 B |       96.35 |
| SimpleModelList_PopcornDefault  | Job-YCYDSY | InProcessEmitToolchain | 13,028.0 ns | 150.65 ns | 140.92 ns |  92.93 |    1.22 | 1.5106 |   28584 B |       96.57 |
| SimpleModelList_PopcornAll      | Job-YCYDSY | InProcessEmitToolchain | 21,736.5 ns | 190.84 ns | 178.51 ns | 155.05 |    1.75 | 2.5024 |   47336 B |      159.92 |
| SimpleModelList_PopcornCustom   | Job-YCYDSY | InProcessEmitToolchain | 21,166.8 ns | 141.32 ns | 132.19 ns | 150.99 |    1.51 | 1.7090 |   32424 B |      109.54 |
| ComplexModel_Stj_Reflection     | Job-YCYDSY | InProcessEmitToolchain |    629.2 ns |   6.34 ns |   5.93 ns |   4.49 |    0.05 | 0.0858 |    1616 B |        5.46 |
| ComplexModel_Stj_SourceGen      | Job-YCYDSY | InProcessEmitToolchain |    757.3 ns |   6.74 ns |   6.30 ns |   5.40 |    0.06 | 0.0858 |    1616 B |        5.46 |
| ComplexModel_PopcornDefault     | Job-YCYDSY | InProcessEmitToolchain |    213.7 ns |   0.89 ns |   0.83 ns |   1.52 |    0.01 | 0.0241 |     456 B |        1.54 |
| ComplexModel_PopcornAll         | Job-YCYDSY | InProcessEmitToolchain |  1,482.7 ns |  14.81 ns |  13.85 ns |  10.58 |    0.13 | 0.1469 |    2776 B |        9.38 |
| ComplexModel_PopcornCustom      | Job-YCYDSY | InProcessEmitToolchain |    633.5 ns |   4.20 ns |   3.93 ns |   4.52 |    0.05 | 0.0486 |     920 B |        3.11 |
| ComplexModelList_Stj_Reflection | Job-YCYDSY | InProcessEmitToolchain | 33,353.8 ns | 316.17 ns | 280.28 ns | 237.92 |    2.71 | 3.3569 |   64008 B |      216.24 |
| ComplexModelList_Stj_SourceGen  | Job-YCYDSY | InProcessEmitToolchain | 36,772.1 ns | 464.57 ns | 434.56 ns | 262.30 |    3.66 | 3.7231 |   70336 B |      237.62 |
| ComplexModelList_PopcornDefault | Job-YCYDSY | InProcessEmitToolchain |  3,152.2 ns |  31.49 ns |  29.46 ns |  22.48 |    0.27 | 0.2861 |    5392 B |       18.22 |
| ComplexModelList_PopcornAll     | Job-YCYDSY | InProcessEmitToolchain | 28,527.2 ns | 162.86 ns | 152.34 ns | 203.49 |    1.94 | 2.6245 |   49536 B |      167.35 |
| ComplexModelList_PopcornCustom  | Job-YCYDSY | InProcessEmitToolchain | 32,693.1 ns | 375.55 ns | 351.29 ns | 233.21 |    3.06 | 2.3193 |   44168 B |      149.22 |
