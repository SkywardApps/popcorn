```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]     : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2


```
| Method                          | Job        | Toolchain              | Mean         | Error       | StdDev      | Ratio  | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------- |----------------------- |-------------:|------------:|------------:|-------:|--------:|--------:|-------:|----------:|------------:|
| SimpleModel_Stj_Reflection      | DefaultJob | Default                |     164.9 ns |     1.15 ns |     1.08 ns |   1.00 |    0.01 |  0.0155 |      - |     296 B |        1.00 |
| SimpleModel_Stj_SourceGen       | DefaultJob | Default                |     156.7 ns |     1.23 ns |     1.09 ns |   0.95 |    0.01 |  0.0155 |      - |     296 B |        1.00 |
| SimpleModel_PopcornDefault      | DefaultJob | Default                |     204.0 ns |     1.13 ns |     0.95 ns |   1.24 |    0.01 |  0.0219 |      - |     416 B |        1.41 |
| SimpleModel_PopcornAll          | DefaultJob | Default                |     292.0 ns |     1.77 ns |     1.57 ns |   1.77 |    0.01 |  0.0315 |      - |     600 B |        2.03 |
| SimpleModel_PopcornCustom       | DefaultJob | Default                |     280.5 ns |     1.57 ns |     1.47 ns |   1.70 |    0.01 |  0.0238 |      - |     456 B |        1.54 |
| SimpleModel_LegacyDefault       | DefaultJob | Default                |     845.0 ns |     3.63 ns |     3.40 ns |   5.12 |    0.04 |  0.0906 |      - |    1720 B |        5.81 |
| SimpleModel_LegacyAll           | DefaultJob | Default                |   1,418.7 ns |    13.31 ns |    11.80 ns |   8.60 |    0.09 |  0.1450 |      - |    2840 B |        9.59 |
| SimpleModel_LegacyCustom        | DefaultJob | Default                |     731.5 ns |     5.70 ns |     5.33 ns |   4.44 |    0.04 |  0.0896 |      - |    1696 B |        5.73 |
| SimpleModelList_Stj_Reflection  | DefaultJob | Default                |  15,487.9 ns |    96.00 ns |    80.17 ns |  93.92 |    0.76 |  1.4954 |      - |   28512 B |       96.32 |
| SimpleModelList_Stj_SourceGen   | DefaultJob | Default                |  14,822.7 ns |   118.03 ns |   110.40 ns |  89.89 |    0.86 |  1.4954 |      - |   28504 B |       96.30 |
| SimpleModelList_PopcornDefault  | DefaultJob | Default                |  12,491.4 ns |   100.60 ns |    89.17 ns |  75.75 |    0.71 |  1.5411 |      - |   29296 B |       98.97 |
| SimpleModelList_PopcornAll      | DefaultJob | Default                |  21,775.3 ns |   179.27 ns |   158.92 ns | 132.05 |    1.25 |  2.5024 |      - |   47504 B |      160.49 |
| SimpleModelList_PopcornCustom   | DefaultJob | Default                |  19,491.8 ns |   161.61 ns |   151.17 ns | 118.21 |    1.16 |  1.7090 |      - |   32328 B |      109.22 |
| SimpleModelList_LegacyDefault   | DefaultJob | Default                |  69,620.3 ns |   676.77 ns |   599.94 ns | 422.20 |    4.41 |  8.0566 | 0.6104 |  152219 B |      514.25 |
| SimpleModelList_LegacyAll       | DefaultJob | Default                | 125,098.7 ns | 2,457.17 ns | 2,629.15 ns | 758.65 |   16.25 | 13.6719 | 0.9766 |  264110 B |      892.26 |
| SimpleModelList_LegacyCustom    | DefaultJob | Default                |  66,440.9 ns |   685.00 ns |   640.75 ns | 402.92 |    4.54 |  7.9346 | 0.7324 |  149754 B |      505.93 |
| ComplexModel_Stj_Reflection     | DefaultJob | Default                |   1,478.6 ns |    12.23 ns |    11.44 ns |   8.97 |    0.09 |  0.1602 |      - |    3024 B |       10.22 |
| ComplexModel_Stj_SourceGen      | DefaultJob | Default                |   1,504.9 ns |    17.12 ns |    16.01 ns |   9.13 |    0.11 |  0.1602 |      - |    3032 B |       10.24 |
| ComplexModel_PopcornDefault     | DefaultJob | Default                |     225.6 ns |     1.14 ns |     1.07 ns |   1.37 |    0.01 |  0.0274 |      - |     520 B |        1.76 |
| ComplexModel_PopcornAll         | DefaultJob | Default                |   1,795.2 ns |    12.77 ns |    11.94 ns |  10.89 |    0.10 |  0.1850 |      - |    3496 B |       11.81 |
| ComplexModel_PopcornCustom      | DefaultJob | Default                |   2,059.0 ns |    15.51 ns |    14.51 ns |  12.49 |    0.12 |  0.1755 |      - |    3344 B |       11.30 |
| ComplexModel_LegacyDefault      | DefaultJob | Default                |     717.3 ns |     5.15 ns |     4.81 ns |   4.35 |    0.04 |  0.0868 |      - |    1648 B |        5.57 |
| ComplexModel_LegacyAll          | DefaultJob | Default                |   6,988.7 ns |    94.94 ns |    88.81 ns |  42.38 |    0.59 |  0.7324 |      - |   14106 B |       47.66 |
| ComplexModel_LegacyCustom       | DefaultJob | Default                |   4,122.7 ns |    81.89 ns |    80.43 ns |  25.00 |    0.50 |  0.4578 |      - |    8945 B |       30.22 |
| ComplexModelList_Stj_Reflection | DefaultJob | Default                |  31,952.3 ns |   368.66 ns |   326.81 ns | 193.77 |    2.27 |  2.8687 |      - |   54776 B |      185.05 |
| ComplexModelList_Stj_SourceGen  | DefaultJob | Default                |  32,486.4 ns |   193.39 ns |   171.44 ns | 197.01 |    1.60 |  2.8687 |      - |   54776 B |      185.05 |
| ComplexModelList_PopcornDefault | DefaultJob | Default                |   3,150.4 ns |    24.96 ns |    23.35 ns |  19.11 |    0.18 |  0.2899 |      - |    5456 B |       18.43 |
| ComplexModelList_PopcornAll     | DefaultJob | Default                |  27,824.4 ns |   201.12 ns |   188.13 ns | 168.74 |    1.54 |  2.6855 |      - |   51000 B |      172.30 |
| ComplexModelList_PopcornCustom  | DefaultJob | Default                |  31,692.6 ns |   236.23 ns |   220.97 ns | 192.20 |    1.78 |  2.3804 |      - |   44880 B |      151.62 |
| ComplexModelList_LegacyDefault  | DefaultJob | Default                |  17,116.2 ns |   206.02 ns |   182.63 ns | 103.80 |    1.26 |  1.9531 |      - |   36835 B |      124.44 |
| ComplexModelList_LegacyAll      | DefaultJob | Default                | 118,331.5 ns | 1,613.76 ns | 1,430.56 ns | 717.61 |    9.53 | 12.6953 | 1.4648 |  241453 B |      815.72 |
| ComplexModelList_LegacyCustom   | DefaultJob | Default                |  63,468.5 ns |   900.11 ns |   751.64 ns | 384.90 |    5.02 |  7.3242 | 0.4883 |  142069 B |      479.96 |
|                                 |            |                        |              |             |             |        |         |         |        |           |             |
| SimpleModel_Stj_Reflection      | Job-ZVQXMR | InProcessEmitToolchain |     156.9 ns |     1.33 ns |     1.24 ns |   1.00 |    0.01 |  0.0155 |      - |     296 B |        1.00 |
| SimpleModel_Stj_SourceGen       | Job-ZVQXMR | InProcessEmitToolchain |     169.4 ns |     0.95 ns |     0.89 ns |   1.08 |    0.01 |  0.0169 |      - |     320 B |        1.08 |
| SimpleModel_PopcornDefault      | Job-ZVQXMR | InProcessEmitToolchain |     199.4 ns |     1.42 ns |     1.33 ns |   1.27 |    0.01 |  0.0191 |      - |     360 B |        1.22 |
| SimpleModel_PopcornAll          | Job-ZVQXMR | InProcessEmitToolchain |     319.8 ns |     2.88 ns |     2.69 ns |   2.04 |    0.02 |  0.0315 |      - |     600 B |        2.03 |
| SimpleModel_PopcornCustom       | Job-ZVQXMR | InProcessEmitToolchain |     331.5 ns |     1.44 ns |     1.34 ns |   2.11 |    0.02 |  0.0238 |      - |     456 B |        1.54 |
| SimpleModel_LegacyDefault       | Job-ZVQXMR | InProcessEmitToolchain |     933.7 ns |     5.67 ns |     5.30 ns |   5.95 |    0.06 |  0.0925 |      - |    1744 B |        5.89 |
| SimpleModel_LegacyAll           | Job-ZVQXMR | InProcessEmitToolchain |   1,630.8 ns |    11.06 ns |    10.35 ns |  10.39 |    0.10 |  0.1507 |      - |    2840 B |        9.59 |
| SimpleModel_LegacyCustom        | Job-ZVQXMR | InProcessEmitToolchain |     778.2 ns |     4.38 ns |     3.88 ns |   4.96 |    0.04 |  0.0896 |      - |    1696 B |        5.73 |
| SimpleModelList_Stj_Reflection  | Job-ZVQXMR | InProcessEmitToolchain |  14,958.9 ns |    73.31 ns |    64.98 ns |  95.35 |    0.83 |  1.4954 |      - |   28224 B |       95.35 |
| SimpleModelList_Stj_SourceGen   | Job-ZVQXMR | InProcessEmitToolchain |  14,405.6 ns |   113.30 ns |   105.98 ns |  91.82 |    0.96 |  1.4496 |      - |   27560 B |       93.11 |
| SimpleModelList_PopcornDefault  | Job-ZVQXMR | InProcessEmitToolchain |  12,710.0 ns |    60.11 ns |    50.19 ns |  81.01 |    0.69 |  1.5411 |      - |   29080 B |       98.24 |
| SimpleModelList_PopcornAll      | Job-ZVQXMR | InProcessEmitToolchain |  21,863.3 ns |   123.78 ns |   115.78 ns | 139.35 |    1.28 |  2.5024 |      - |   47216 B |      159.51 |
| SimpleModelList_PopcornCustom   | Job-ZVQXMR | InProcessEmitToolchain |  21,003.5 ns |   118.97 ns |   105.46 ns | 133.87 |    1.21 |  1.7090 |      - |   32288 B |      109.08 |
| SimpleModelList_LegacyDefault   | Job-ZVQXMR | InProcessEmitToolchain |  70,099.0 ns |   410.03 ns |   342.40 ns | 446.80 |    4.01 |  7.9346 | 0.6104 |  151466 B |      511.71 |
| SimpleModelList_LegacyAll       | Job-ZVQXMR | InProcessEmitToolchain | 127,953.8 ns |   945.01 ns |   837.73 ns | 815.56 |    8.09 | 13.9160 | 1.4648 |  262500 B |      886.82 |
| SimpleModelList_LegacyCustom    | Job-ZVQXMR | InProcessEmitToolchain |  69,904.7 ns |   579.10 ns |   513.35 ns | 445.56 |    4.64 |  7.9346 | 0.6104 |  149774 B |      505.99 |
| ComplexModel_Stj_Reflection     | Job-ZVQXMR | InProcessEmitToolchain |   2,330.7 ns |    20.87 ns |    19.53 ns |  14.86 |    0.17 |  0.2174 |      - |    4160 B |       14.05 |
| ComplexModel_Stj_SourceGen      | Job-ZVQXMR | InProcessEmitToolchain |   1,430.6 ns |     3.63 ns |     2.83 ns |   9.12 |    0.07 |  0.1354 |      - |    2584 B |        8.73 |
| ComplexModel_PopcornDefault     | Job-ZVQXMR | InProcessEmitToolchain |     249.8 ns |     1.25 ns |     1.05 ns |   1.59 |    0.01 |  0.0272 |      - |     520 B |        1.76 |
| ComplexModel_PopcornAll         | Job-ZVQXMR | InProcessEmitToolchain |   1,380.8 ns |     9.22 ns |     8.62 ns |   8.80 |    0.09 |  0.1240 |      - |    2360 B |        7.97 |
| ComplexModel_PopcornCustom      | Job-ZVQXMR | InProcessEmitToolchain |   1,967.4 ns |    12.79 ns |    11.34 ns |  12.54 |    0.12 |  0.1411 |      - |    2680 B |        9.05 |
| ComplexModel_LegacyDefault      | Job-ZVQXMR | InProcessEmitToolchain |     776.9 ns |     5.08 ns |     4.75 ns |   4.95 |    0.05 |  0.0868 |      - |    1640 B |        5.54 |
| ComplexModel_LegacyAll          | Job-ZVQXMR | InProcessEmitToolchain |   6,542.7 ns |    37.65 ns |    33.38 ns |  41.70 |    0.38 |  0.6714 |      - |   12762 B |       43.11 |
| ComplexModel_LegacyCustom       | Job-ZVQXMR | InProcessEmitToolchain |   3,847.4 ns |    41.81 ns |    39.11 ns |  24.52 |    0.31 |  0.4120 |      - |    7761 B |       26.22 |
| ComplexModelList_Stj_Reflection | Job-ZVQXMR | InProcessEmitToolchain |  46,652.2 ns |   268.07 ns |   250.75 ns | 297.35 |    2.75 |  3.8452 |      - |   72480 B |      244.86 |
| ComplexModelList_Stj_SourceGen  | Job-ZVQXMR | InProcessEmitToolchain |  35,107.4 ns |   284.74 ns |   252.41 ns | 223.77 |    2.31 |  2.9907 |      - |   57504 B |      194.27 |
| ComplexModelList_PopcornDefault | Job-ZVQXMR | InProcessEmitToolchain |   3,249.7 ns |    25.23 ns |    23.60 ns |  20.71 |    0.22 |  0.2899 |      - |    5464 B |       18.46 |
| ComplexModelList_PopcornAll     | Job-ZVQXMR | InProcessEmitToolchain |  30,306.3 ns |   297.05 ns |   263.33 ns | 193.17 |    2.19 |  2.7466 |      - |   51856 B |      175.19 |
| ComplexModelList_PopcornCustom  | Job-ZVQXMR | InProcessEmitToolchain |  33,674.1 ns |   312.56 ns |   292.37 ns | 214.63 |    2.44 |  2.3804 |      - |   45904 B |      155.08 |
| ComplexModelList_LegacyDefault  | Job-ZVQXMR | InProcessEmitToolchain |  17,647.6 ns |   166.80 ns |   147.87 ns | 112.48 |    1.25 |  1.9531 | 0.0305 |   36851 B |      124.50 |
| ComplexModelList_LegacyAll      | Job-ZVQXMR | InProcessEmitToolchain | 127,242.1 ns | 1,685.76 ns | 1,576.86 ns | 811.02 |   11.54 | 13.1836 | 1.4648 |  248967 B |      841.10 |
| ComplexModelList_LegacyCustom   | Job-ZVQXMR | InProcessEmitToolchain |  61,188.8 ns |   651.82 ns |   609.71 ns | 390.01 |    4.80 |  6.9580 | 0.5493 |  131309 B |      443.61 |
