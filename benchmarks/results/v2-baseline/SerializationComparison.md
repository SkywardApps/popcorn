```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]     : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2


```
| Method                          | Job        | Toolchain              | Mean         | Error       | StdDev      | Median       | Ratio  | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------- |----------------------- |-------------:|------------:|------------:|-------------:|-------:|--------:|--------:|-------:|----------:|------------:|
| SimpleModel_Stj_Reflection      | DefaultJob | Default                |     172.9 ns |     3.11 ns |     4.26 ns |     171.7 ns |   1.00 |    0.03 |  0.0155 |      - |     296 B |        1.00 |
| SimpleModel_Stj_SourceGen       | DefaultJob | Default                |     169.9 ns |     2.85 ns |     2.22 ns |     169.7 ns |   0.98 |    0.03 |  0.0155 |      - |     296 B |        1.00 |
| SimpleModel_PopcornDefault      | DefaultJob | Default                |     262.8 ns |     4.86 ns |     4.55 ns |     262.0 ns |   1.52 |    0.04 |  0.0310 |      - |     592 B |        2.00 |
| SimpleModel_PopcornAll          | DefaultJob | Default                |     351.3 ns |     5.14 ns |     4.55 ns |     351.2 ns |   2.03 |    0.05 |  0.0410 |      - |     776 B |        2.62 |
| SimpleModel_PopcornCustom       | DefaultJob | Default                |     335.1 ns |     6.27 ns |     5.24 ns |     335.9 ns |   1.94 |    0.05 |  0.0334 |      - |     632 B |        2.14 |
| SimpleModel_LegacyDefault       | DefaultJob | Default                |     885.0 ns |    14.90 ns |    25.30 ns |     882.0 ns |   5.12 |    0.19 |  0.0896 |      - |    1720 B |        5.81 |
| SimpleModel_LegacyAll           | DefaultJob | Default                |   1,493.0 ns |    29.49 ns |    41.34 ns |   1,487.3 ns |   8.64 |    0.31 |  0.1450 |      - |    2840 B |        9.59 |
| SimpleModel_LegacyCustom        | DefaultJob | Default                |     746.9 ns |    12.03 ns |    11.25 ns |     744.8 ns |   4.32 |    0.12 |  0.0896 |      - |    1696 B |        5.73 |
| SimpleModelList_Stj_Reflection  | DefaultJob | Default                |  14,950.5 ns |   201.60 ns |   178.71 ns |  14,942.7 ns |  86.53 |    2.28 |  1.4954 |      - |   28504 B |       96.30 |
| SimpleModelList_Stj_SourceGen   | DefaultJob | Default                |  15,165.2 ns |   297.09 ns |   305.09 ns |  15,168.5 ns |  87.77 |    2.70 |  1.5106 |      - |   28512 B |       96.32 |
| SimpleModelList_PopcornDefault  | DefaultJob | Default                |  16,415.1 ns |   268.74 ns |   238.23 ns |  16,416.6 ns |  95.00 |    2.62 |  1.8921 |      - |   35808 B |      120.97 |
| SimpleModelList_PopcornAll      | DefaultJob | Default                |  26,861.9 ns |   373.43 ns |   430.04 ns |  26,877.9 ns | 155.46 |    4.42 |  2.8687 |      - |   54024 B |      182.51 |
| SimpleModelList_PopcornCustom   | DefaultJob | Default                |  23,775.9 ns |   463.99 ns |   434.01 ns |  23,694.3 ns | 137.60 |    4.07 |  2.0447 |      - |   38840 B |      131.22 |
| SimpleModelList_LegacyDefault   | DefaultJob | Default                |  70,735.0 ns |   714.35 ns |   633.25 ns |  70,673.5 ns | 409.38 |   10.34 |  8.0566 | 0.6104 |  152210 B |      514.22 |
| SimpleModelList_LegacyAll       | DefaultJob | Default                | 126,543.3 ns | 2,082.77 ns | 1,948.22 ns | 126,486.3 ns | 732.36 |   20.52 | 13.6719 | 1.4648 |  264103 B |      892.24 |
| SimpleModelList_LegacyCustom    | DefaultJob | Default                |  74,536.5 ns | 1,443.60 ns | 3,287.80 ns |  74,213.0 ns | 431.38 |   21.48 |  7.9346 | 0.7324 |  149754 B |      505.93 |
| ComplexModel_Stj_Reflection     | DefaultJob | Default                |   1,649.7 ns |    24.51 ns |    19.13 ns |   1,643.6 ns |   9.55 |    0.25 |  0.1602 |      - |    3032 B |       10.24 |
| ComplexModel_Stj_SourceGen      | DefaultJob | Default                |   1,687.8 ns |    33.63 ns |    41.30 ns |   1,681.9 ns |   9.77 |    0.33 |  0.1602 |      - |    3032 B |       10.24 |
| ComplexModel_PopcornDefault     | DefaultJob | Default                |     252.3 ns |     2.84 ns |     2.51 ns |     253.0 ns |   1.46 |    0.04 |  0.0272 |      - |     520 B |        1.76 |
| ComplexModel_PopcornAll         | DefaultJob | Default                |   2,629.2 ns |   200.74 ns |   591.87 ns |   2,281.1 ns |  15.22 |    3.43 |  0.2060 |      - |    3880 B |       13.11 |
| ComplexModel_PopcornCustom      | DefaultJob | Default                |   2,325.1 ns |    36.13 ns |    33.80 ns |   2,325.6 ns |  13.46 |    0.37 |  0.1945 |      - |    3720 B |       12.57 |
| ComplexModel_LegacyDefault      | DefaultJob | Default                |     779.6 ns |    15.15 ns |    14.88 ns |     780.8 ns |   4.51 |    0.14 |  0.0839 |      - |    1648 B |        5.57 |
| ComplexModel_LegacyAll          | DefaultJob | Default                |   7,704.3 ns |   147.39 ns |   201.75 ns |   7,662.4 ns |  44.59 |    1.56 |  0.7324 |      - |   14098 B |       47.63 |
| ComplexModel_LegacyCustom       | DefaultJob | Default                |   5,008.5 ns |    97.11 ns |   136.14 ns |   5,002.8 ns |  28.99 |    1.04 |  0.4578 |      - |    8945 B |       30.22 |
| ComplexModelList_Stj_Reflection | DefaultJob | Default                |  37,822.3 ns |   753.66 ns |   897.18 ns |  37,545.5 ns | 218.89 |    7.26 |  2.8687 |      - |   54776 B |      185.05 |
| ComplexModelList_Stj_SourceGen  | DefaultJob | Default                |  35,227.0 ns |   693.73 ns |   614.98 ns |  35,113.4 ns | 203.88 |    5.94 |  2.8687 |      - |   54776 B |      185.05 |
| ComplexModelList_PopcornDefault | DefaultJob | Default                |   3,884.5 ns |    76.95 ns |    82.33 ns |   3,869.9 ns |  22.48 |    0.71 |  0.3662 |      - |    6992 B |       23.62 |
| ComplexModelList_PopcornAll     | DefaultJob | Default                |  36,837.1 ns |   717.23 ns |   907.07 ns |  37,018.0 ns | 213.19 |    7.21 |  2.9907 |      - |   56640 B |      191.35 |
| ComplexModelList_PopcornCustom  | DefaultJob | Default                |  35,384.1 ns |   506.98 ns |   497.93 ns |  35,382.0 ns | 204.78 |    5.61 |  2.6245 |      - |   50512 B |      170.65 |
| ComplexModelList_LegacyDefault  | DefaultJob | Default                |  18,279.6 ns |   187.39 ns |   156.48 ns |  18,255.7 ns | 105.79 |    2.66 |  1.9531 |      - |   36835 B |      124.44 |
| ComplexModelList_LegacyAll      | DefaultJob | Default                | 128,479.8 ns | 2,564.35 ns | 2,850.27 ns | 127,615.3 ns | 743.57 |   23.87 | 12.6953 | 1.4648 |  241461 B |      815.75 |
| ComplexModelList_LegacyCustom   | DefaultJob | Default                |  69,551.9 ns | 1,352.87 ns | 1,610.50 ns |  69,172.9 ns | 402.53 |   13.20 |  7.3242 | 0.4883 |  142069 B |      479.96 |
|                                 |            |                        |              |             |             |              |        |         |         |        |           |             |
| SimpleModel_Stj_Reflection      | Job-HLUJQF | InProcessEmitToolchain |     171.2 ns |     2.81 ns |     2.63 ns |     169.9 ns |   1.00 |    0.02 |  0.0155 |      - |     296 B |        1.00 |
| SimpleModel_Stj_SourceGen       | Job-HLUJQF | InProcessEmitToolchain |     192.8 ns |     3.89 ns |     5.45 ns |     193.9 ns |   1.13 |    0.04 |  0.0169 |      - |     320 B |        1.08 |
| SimpleModel_PopcornDefault      | Job-HLUJQF | InProcessEmitToolchain |     266.8 ns |     4.28 ns |     4.00 ns |     265.4 ns |   1.56 |    0.03 |  0.0281 |      - |     536 B |        1.81 |
| SimpleModel_PopcornAll          | Job-HLUJQF | InProcessEmitToolchain |     402.4 ns |     7.04 ns |     6.24 ns |     401.1 ns |   2.35 |    0.05 |  0.0410 |      - |     776 B |        2.62 |
| SimpleModel_PopcornCustom       | Job-HLUJQF | InProcessEmitToolchain |     376.5 ns |     1.95 ns |     1.53 ns |     376.4 ns |   2.20 |    0.03 |  0.0334 |      - |     632 B |        2.14 |
| SimpleModel_LegacyDefault       | Job-HLUJQF | InProcessEmitToolchain |   1,069.7 ns |    13.07 ns |    12.22 ns |   1,064.8 ns |   6.25 |    0.12 |  0.0916 |      - |    1744 B |        5.89 |
| SimpleModel_LegacyAll           | Job-HLUJQF | InProcessEmitToolchain |   1,748.9 ns |    20.07 ns |    16.76 ns |   1,750.6 ns |  10.22 |    0.18 |  0.1507 |      - |    2840 B |        9.59 |
| SimpleModel_LegacyCustom        | Job-HLUJQF | InProcessEmitToolchain |     862.4 ns |    13.37 ns |    11.85 ns |     859.3 ns |   5.04 |    0.10 |  0.0896 |      - |    1696 B |        5.73 |
| SimpleModelList_Stj_Reflection  | Job-HLUJQF | InProcessEmitToolchain |  16,559.5 ns |   278.13 ns |   246.56 ns |  16,522.7 ns |  96.77 |    1.99 |  1.4954 |      - |   28232 B |       95.38 |
| SimpleModelList_Stj_SourceGen   | Job-HLUJQF | InProcessEmitToolchain |  16,311.3 ns |   303.21 ns |   283.63 ns |  16,315.3 ns |  95.32 |    2.13 |  1.4343 |      - |   27568 B |       93.14 |
| SimpleModelList_PopcornDefault  | Job-HLUJQF | InProcessEmitToolchain |  18,515.0 ns |   370.05 ns |   739.04 ns |  18,544.0 ns | 108.20 |    4.56 |  1.8616 |      - |   35592 B |      120.24 |
| SimpleModelList_PopcornAll      | Job-HLUJQF | InProcessEmitToolchain |  28,359.3 ns |   439.04 ns |   389.20 ns |  28,312.9 ns | 165.72 |    3.29 |  2.8381 |      - |   53720 B |      181.49 |
| SimpleModelList_PopcornCustom   | Job-HLUJQF | InProcessEmitToolchain |  27,058.8 ns |   433.74 ns |   405.72 ns |  27,025.3 ns | 158.12 |    3.27 |  2.0447 |      - |   38800 B |      131.08 |
| SimpleModelList_LegacyDefault   | Job-HLUJQF | InProcessEmitToolchain |  80,122.3 ns |   846.31 ns |   706.71 ns |  80,154.9 ns | 468.21 |    7.97 |  7.9346 | 0.6104 |  151459 B |      511.69 |
| SimpleModelList_LegacyAll       | Job-HLUJQF | InProcessEmitToolchain | 145,817.4 ns | 2,706.45 ns | 2,531.62 ns | 145,666.7 ns | 852.11 |   19.06 | 13.9160 | 1.4648 |  262500 B |      886.82 |
| SimpleModelList_LegacyCustom    | Job-HLUJQF | InProcessEmitToolchain |  79,530.3 ns | 1,291.81 ns | 1,208.36 ns |  79,406.4 ns | 464.75 |    9.68 |  7.9346 | 0.6104 |  149774 B |      505.99 |
| ComplexModel_Stj_Reflection     | Job-HLUJQF | InProcessEmitToolchain |   2,660.6 ns |    47.22 ns |    44.17 ns |   2,656.9 ns |  15.55 |    0.34 |  0.2174 |      - |    4160 B |       14.05 |
| ComplexModel_Stj_SourceGen      | Job-HLUJQF | InProcessEmitToolchain |   1,632.2 ns |    30.19 ns |    26.77 ns |   1,635.0 ns |   9.54 |    0.21 |  0.1354 |      - |    2576 B |        8.70 |
| ComplexModel_PopcornDefault     | Job-HLUJQF | InProcessEmitToolchain |     266.2 ns |     4.47 ns |     3.96 ns |     266.1 ns |   1.56 |    0.03 |  0.0272 |      - |     520 B |        1.76 |
| ComplexModel_PopcornAll         | Job-HLUJQF | InProcessEmitToolchain |   1,520.0 ns |    29.78 ns |    34.30 ns |   1,518.4 ns |   8.88 |    0.24 |  0.1316 |      - |    2496 B |        8.43 |
| ComplexModel_PopcornCustom      | Job-HLUJQF | InProcessEmitToolchain |   2,246.8 ns |    44.03 ns |    74.76 ns |   2,245.2 ns |  13.13 |    0.47 |  0.1602 |      - |    3064 B |       10.35 |
| ComplexModel_LegacyDefault      | Job-HLUJQF | InProcessEmitToolchain |     875.6 ns |    14.39 ns |    12.76 ns |     873.8 ns |   5.12 |    0.10 |  0.0868 |      - |    1648 B |        5.57 |
| ComplexModel_LegacyAll          | Job-HLUJQF | InProcessEmitToolchain |   7,093.4 ns |   124.51 ns |   116.47 ns |   7,079.5 ns |  41.45 |    0.90 |  0.6714 |      - |   12762 B |       43.11 |
| ComplexModel_LegacyCustom       | Job-HLUJQF | InProcessEmitToolchain |   4,100.5 ns |    54.70 ns |    74.88 ns |   4,069.6 ns |  23.96 |    0.56 |  0.4120 |      - |    7761 B |       26.22 |
| ComplexModelList_Stj_Reflection | Job-HLUJQF | InProcessEmitToolchain |  49,269.0 ns |   753.99 ns |   705.28 ns |  49,201.9 ns | 287.91 |    5.83 |  3.8452 |      - |   72472 B |      244.84 |
| ComplexModelList_Stj_SourceGen  | Job-HLUJQF | InProcessEmitToolchain |  38,160.1 ns |   746.90 ns |   767.01 ns |  38,248.2 ns | 222.99 |    5.46 |  2.9907 |      - |   57512 B |      194.30 |
| ComplexModelList_PopcornDefault | Job-HLUJQF | InProcessEmitToolchain |   4,030.8 ns |    49.07 ns |    40.97 ns |   4,037.6 ns |  23.55 |    0.42 |  0.3662 |      - |    7000 B |       23.65 |
| ComplexModelList_PopcornAll     | Job-HLUJQF | InProcessEmitToolchain |  35,767.2 ns |   416.33 ns |   389.43 ns |  35,867.7 ns | 209.01 |    3.79 |  2.9907 |      - |   57240 B |      193.38 |
| ComplexModelList_PopcornCustom  | Job-HLUJQF | InProcessEmitToolchain |  37,762.0 ns |   330.19 ns |   308.86 ns |  37,841.8 ns | 220.67 |    3.69 |  2.6855 |      - |   51664 B |      174.54 |
| ComplexModelList_LegacyDefault  | Job-HLUJQF | InProcessEmitToolchain |  19,288.3 ns |   297.48 ns |   278.26 ns |  19,374.5 ns | 112.71 |    2.29 |  1.9531 | 0.0305 |   36842 B |      124.47 |
| ComplexModelList_LegacyAll      | Job-HLUJQF | InProcessEmitToolchain | 144,544.4 ns | 1,784.16 ns | 1,668.90 ns | 145,140.9 ns | 844.67 |   15.63 | 13.1836 | 1.4648 |  248953 B |      841.06 |
| ComplexModelList_LegacyCustom   | Job-HLUJQF | InProcessEmitToolchain |  68,215.6 ns | 1,004.58 ns |   939.69 ns |  68,358.2 ns | 398.63 |    7.93 |  6.9580 | 0.4883 |  131310 B |      443.61 |
