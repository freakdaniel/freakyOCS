namespace OcsNet.Core.Datasets;

public sealed record MacDevice(
    string Name,
    string Cpu,
    string CpuGeneration,
    string? DiscreteGpu,
    string InitialSupport,
    string? LastSupportedVersion = null
)
{
    public string EffectiveLastSupported =>
        LastSupportedVersion ?? OsData.GetLatestDarwinVersion();
}

public static class MacModelData
{
    public static readonly MacDevice[] MacDevices =
    [
        // iMac Models
        new("iMac11,1",      "i7-860",       "Lynnfield",        "ATI Radeon HD 4850",         "10.2.0", "17.99.99"),
        new("iMac11,2",      "i5-680",       "Clarkdale",        "ATI Radeon HD 4670",         "10.3.0", "17.99.99"),
        new("iMac11,3",      "i7-870",       "Clarkdale",        "ATI Radeon HD 5670",         "10.3.0", "17.99.99"),
        new("iMac12,1",      "i5-2400S",     "Sandy Bridge",     "AMD Radeon HD 6750M",        "10.6.0", "17.99.99"),
        new("iMac12,2",      "i7-2600",      "Sandy Bridge",     "AMD Radeon HD 6770M",        "10.6.0", "17.99.99"),
        new("iMac13,1",      "i7-3770S",     "Ivy Bridge",       "NVIDIA GeForce GT 640M",     "12.2.0", "19.99.99"),
        new("iMac13,2",      "i5-3470S",     "Ivy Bridge",       "NVIDIA GeForce GTX 660M",    "12.2.0", "19.99.99"),
        new("iMac13,3",      "i5-3470S",     "Ivy Bridge",       null,                         "12.2.0", "19.99.99"),
        new("iMac14,1",      "i5-4570R",     "Haswell",          null,                         "12.4.0", "19.99.99"),
        new("iMac14,2",      "i7-4771",      "Haswell",          "NVIDIA GeForce GT 750M",     "12.4.0", "19.99.99"),
        new("iMac14,3",      "i5-4570S",     "Haswell",          "NVIDIA GeForce GT 755M",     "12.4.0", "19.99.99"),
        new("iMac14,4",      "i5-4260U",     "Haswell",          null,                         "13.2.0", "20.99.99"),
        new("iMac15,1",      "i7-4790K",     "Haswell",          "AMD Radeon R9 M290X",        "14.0.0", "20.99.99"),
        new("iMac16,1",      "i5-5250U",     "Broadwell",        null,                         "15.0.0", "21.99.99"),
        new("iMac16,2",      "i5-5675R",     "Broadwell",        null,                         "15.0.0", "21.99.99"),
        new("iMac17,1",      "i5-6500",      "Skylake",          "AMD Radeon R9 M380",         "15.0.0", "21.99.99"),
        new("iMac18,1",      "i5-7360U",     "Kaby Lake",        null,                         "16.5.0", "22.99.99"),
        new("iMac18,2",      "i5-7400",      "Kaby Lake",        "AMD Radeon Pro 555",         "16.5.0", "22.99.99"),
        new("iMac18,3",      "i5-7600K",     "Kaby Lake",        "AMD Radeon Pro 570",         "16.5.0", "22.99.99"),
        new("iMac19,1",      "i9-9900K",     "Coffee Lake",      "AMD Radeon Pro 570X",        "18.5.0", "24.99.99"),
        new("iMac19,2",      "i5-8500",      "Coffee Lake",      "AMD Radeon Pro 555X",        "18.5.0", "24.99.99"),
        new("iMac20,1",      "i5-10500",     "Comet Lake",       "AMD Radeon Pro 5300",        "19.6.0"),
        new("iMac20,2",      "i9-10910",     "Comet Lake",       "AMD Radeon Pro 5300",        "19.6.0"),
        // MacBook Models
        new("MacBook8,1",    "M-5Y51",       "Broadwell",        null,                         "14.1.0", "20.99.99"),
        new("MacBook9,1",    "m3-6Y30",      "Skylake",          null,                         "15.4.0", "21.99.99"),
        new("MacBook10,1",   "m3-7Y32",      "Kaby Lake",        null,                         "16.6.0", "22.99.99"),
        // MacBookAir Models
        new("MacBookAir4,1", "i5-2467M",     "Sandy Bridge",     null,                         "11.0.0", "17.99.99"),
        new("MacBookAir4,2", "i5-2557M",     "Sandy Bridge",     null,                         "11.0.0", "17.99.99"),
        new("MacBookAir5,1", "i5-3317U",     "Ivy Bridge",       null,                         "11.4.0", "19.99.99"),
        new("MacBookAir5,2", "i5-3317U",     "Ivy Bridge",       null,                         "12.2.0", "19.99.99"),
        new("MacBookAir6,1", "i5-4250U",     "Haswell",          null,                         "12.4.0", "20.99.99"),
        new("MacBookAir6,2", "i5-4250U",     "Haswell",          null,                         "12.4.0", "20.99.99"),
        new("MacBookAir7,1", "i5-5250U",     "Broadwell",        null,                         "14.1.0", "21.99.99"),
        new("MacBookAir7,2", "i5-5250U",     "Broadwell",        null,                         "14.1.0", "21.99.99"),
        new("MacBookAir8,1", "i5-8210Y",     "Amber Lake",       null,                         "18.2.0", "24.99.99"),
        new("MacBookAir8,2", "i5-8210Y",     "Amber Lake",       null,                         "18.6.0", "24.99.99"),
        new("MacBookAir9,1", "i3-1000NG4",   "Ice Lake",         null,                         "19.4.0", "24.99.99"),
        // MacBookPro Models
        new("MacBookPro6,1", "i7-640M",      "Arrandale",        "NVIDIA GeForce GT 330M",     "10.3.0", "17.99.99"),
        new("MacBookPro6,2", "i7-640M",      "Arrandale",        "NVIDIA GeForce GT 330M",     "10.3.0", "17.99.99"),
        new("MacBookPro8,1", "i5-2415M",     "Sandy Bridge",     null,                         "10.6.0", "17.99.99"),
        new("MacBookPro8,2", "i7-2675QM",    "Sandy Bridge",     "AMD Radeon HD 6490M",        "10.6.0", "17.99.99"),
        new("MacBookPro8,3", "i7-2820QM",    "Sandy Bridge",     "AMD Radeon HD 6750M",        "10.6.0", "17.99.99"),
        new("MacBookPro9,1", "i7-3615QM",    "Ivy Bridge",       "NVIDIA GeForce GT 650M",     "11.3.0", "19.99.99"),
        new("MacBookPro9,2", "i5-3210M",     "Ivy Bridge",       null,                         "11.3.0", "19.99.99"),
        new("MacBookPro10,1","i7-3615QM",    "Ivy Bridge",       "NVIDIA GeForce GT 650M",     "11.4.0", "19.99.99"),
        new("MacBookPro10,2","i5-3210M",     "Ivy Bridge",       null,                         "12.2.0", "19.99.99"),
        new("MacBookPro11,1","i5-4258U",     "Haswell",          null,                         "13.0.0", "20.99.99"),
        new("MacBookPro11,2","i7-4770HQ",    "Haswell",          null,                         "13.0.0", "20.99.99"),
        new("MacBookPro11,3","i7-4850HQ",    "Haswell",          "NVIDIA GeForce GT 750M",     "13.0.0", "20.99.99"),
        new("MacBookPro11,4","i7-4770HQ",    "Haswell",          null,                         "14.3.0", "21.99.99"),
        new("MacBookPro11,5","i7-4870HQ",    "Haswell",          "AMD Radeon R9 M370X",        "14.3.0", "21.99.99"),
        new("MacBookPro12,1","i5-5257U",     "Broadwell",        null,                         "14.1.0", "21.99.99"),
        new("MacBookPro13,1","i5-6360U",     "Skylake",          null,                         "16.0.0", "21.99.99"),
        new("MacBookPro13,2","i7-6567U",     "Skylake",          null,                         "16.1.0", "21.99.99"),
        new("MacBookPro13,3","i7-6700HQ",    "Skylake",          "AMD Radeon Pro 450",         "16.1.0", "21.99.99"),
        new("MacBookPro14,1","i5-7360U",     "Kaby Lake",        null,                         "16.6.0", "22.99.99"),
        new("MacBookPro14,2","i5-7267U",     "Kaby Lake",        null,                         "16.6.0", "22.99.99"),
        new("MacBookPro14,3","i7-7700HQ",    "Kaby Lake",        "AMD Radeon Pro 555",         "16.6.0", "22.99.99"),
        new("MacBookPro15,1","i7-8750H",     "Coffee Lake",      "AMD Radeon Pro 555X",        "17.99.99","24.99.99"),
        new("MacBookPro15,2","i7-8559U",     "Coffee Lake",      null,                         "17.99.99","24.99.99"),
        new("MacBookPro15,3","i7-8850H",     "Coffee Lake",      "AMD Radeon Pro Vega 16",     "18.2.0", "24.99.99"),
        new("MacBookPro15,4","i5-8257U",     "Coffee Lake",      null,                         "18.6.0", "24.99.99"),
        new("MacBookPro16,1","i7-9750H",     "Coffee Lake",      "AMD Radeon Pro 5300",        "19.0.0"),
        new("MacBookPro16,2","i5-1038NG7",   "Ice Lake",         null,                         "19.4.0"),
        new("MacBookPro16,3","i5-8257U",     "Coffee Lake",      null,                         "19.4.0", "24.99.99"),
        new("MacBookPro16,4","i7-9750H",     "Coffee Lake",      "AMD Radeon Pro 5600M",       "19.0.0"),
        // Macmini Models
        new("Macmini5,1",    "i5-2415M",     "Sandy Bridge",     null,                         "11.0.0", "17.99.99"),
        new("Macmini5,2",    "i5-2520M",     "Sandy Bridge",     null,                         "11.0.0", "17.99.99"),
        new("Macmini5,3",    "i7-2635QM",    "Sandy Bridge",     null,                         "11.0.0", "17.99.99"),
        new("Macmini6,1",    "i5-3210M",     "Ivy Bridge",       null,                         "10.8.1", "19.99.99"),
        new("Macmini6,2",    "i7-3615QM",    "Ivy Bridge",       null,                         "10.8.1", "19.99.99"),
        new("Macmini7,1",    "i5-4260U",     "Haswell",          null,                         "14.0.0", "21.99.99"),
        new("Macmini8,1",    "i7-8700B",     "Coffee Lake",      null,                         "18.0.0", "24.99.99"),
        // iMacPro Models
        new("iMacPro1,1",    "W-2140B",      "Skylake-W",        "AMD Radeon RX Vega 56",      "17.3.0", "24.99.99"),
        // MacPro Models
        new("MacPro5,1",     "X5675 x2",     "Nehalem/Westmere", "ATI Radeon HD 5770",         "10.4.0", "18.99.99"),
        new("MacPro6,1",     "E5-1620 v2",   "Ivy Bridge EP",    "AMD FirePro D300",           "13.0.0", "21.99.99"),
        new("MacPro7,1",     "W-3245M",      "Cascade Lake-W",   "AMD Radeon Pro 580X",        "19.0.0"),
    ];

    public static MacDevice? GetByName(string name) =>
        MacDevices.FirstOrDefault(d => d.Name == name);
}
