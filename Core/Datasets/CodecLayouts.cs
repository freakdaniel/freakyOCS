namespace OcsNet.Core.Datasets;

public sealed record CodecLayout(int Id, string Comment);

public static class CodecLayouts
{
    public static readonly Dictionary<string, CodecLayout[]> Data = new()
    {
        ["10EC-0295"] =
        [
            new(1,  "Damon - Realtek ALC 295 for HP Envy x360 15-bp107tx"),
            new(3,  "Mirone - Realtek ALC295/ALC3254"),
            new(11, "Realtek ALC295, ZenBook UX581"),
            new(13, "DalianSky - Realtek ALC295/ALC3254 Dell7570"),
            new(14, "InsanelyDeepak - Realtek ALC295 v2 Asus UX430UA"),
            new(15, "InsanelyDeepak - Realtek ALC295/ALC3254 "),
            new(21, "Andres - ALC295 Acer Nitro 5 Spin (NP515-51)"),
            new(22, "Realtek ALC295 by aleix"),
            new(23, "Lancet-X—Realtek ALC295/ALC3254 for HP OMEN 15-AX000"),
            new(24, "zty199 - ALC295 for HP Pavilion / OMEN-2"),
            new(28, "vusun123 - ALC 295 for Skylake HP Pavilion"),
            new(33, "Lorys89 - Realtek ALC295/ALC3254 for Dell Latitude 7210 2-in-1"),
            new(69, "Baio77 - ALC295 Lenovo_X1_Tablet_3°Gen"),
            new(75, "Lorys89 - Realtek ALC295/ALC3254 for Dell Inspiron 7590"),
            new(77, "Unbelievable9 - Realtek ALC295/ALC3254 for Dell Latitude 5290"),
        ],
        ["10EC-0298"] =
        [
            new(3,  "Mirone - Realtek ALC298 SP4 - ComboJack"),
            new(11, "Rockjesus.cn - Realtek ALC298 for Alienware 17 R4 2.1ch"),
            new(13, "InsanelyDeepak - Realtek ALC298"),
            new(15, "Piscean - Realtek ALC298 for Dell Precision 5540"),
            new(16, "Ping - Realtek ALC298 for Dell Precision 5520"),
            new(21, "Lenovo 720S-15IKB ALC298 by Andres ZeroCross"),
            new(22, "Razer Blade 14 2017 by Andres ZeroCross"),
            new(25, "hoaug - ALC295 - Razer Blade 15 2018 Advanced"),
            new(28, "vusun123 - Realtek ALC298 for Dell XPS 9x50"),
            new(29, "vusun123 - Realtek ALC298 for Lenovo X270"),
            new(30, "Constanta - Realtek ALC298 for Xiaomi Mi Notebook Air 13.3 Fingerprint 2018"),
            new(32, "smallssnow xps 9570 - Realtek ALC298"),
            new(33, "RockJesus.cn - Realtek ALC298 for surface laptop 1gen"),
            new(47, "Daliansky - Realtek ALC298 ThinkPad T470p"),
            new(66, "lgs3137 - Realtek ALC298 MECHREVO S1"),
            new(69, "mbarbierato - Realtek ALC298 for Microsoft Surface GO 2"),
            new(72, "Custom - Realtek ALC298 for Dell XPS 9560 by KNNSpeed"),
            new(94, "Custom - Realtek ALC298 for Lenovo Yoga C940 by idalin"),
            new(99, "Daliansky - Realtek ALC298 XiaoMi Pro"),
        ],
        ["10EC-1168"] =
        [
            new(1,  "Toleda -  Realtek ALC S1220A"),
            new(2,  "Toleda -  Realtek ALC S1220A"),
            new(3,  "Toleda -  Realtek ALC S1220A"),
            new(5,  "Mirone - Realtek ALC S1220A"),
            new(7,  "Mirone - Realtek ALC S1220A"),
            new(8,  "Realtek ALC S1220P_MSI_Z490i_UNIFY_ by_vio"),
            new(11, "Realtek ALC S1220A Kushamot for Asus Z270G mb (based on Mirone's layout 7)"),
            new(13, "Realtek ALC S1220A for Asus ProArt Z690-Creator WiFi (CaseySJ)"),
            new(15, "Realtek ALC S1220A for Asus ROG Strix X570-F Gaming (based on Mirone's layout 7)"),
            new(20, "Realtek ALC S1220A RodionS, Nacho 2.0 outputs(green), 2 inputs (blue)+front panel (mic fr.panel), mic (pink), headphones(lime), SPDIF/Optical "),
            new(21, "Realtek ALC S1220A RodionS, Nacho 5.1 outputs(green, black, orange), 2 inputs (blue)+front panel (mic fr.panel), mic (pink), headphones(lime), SPDIF/Optical "),
            new(99, "Realtek ALC S1220A Hoangtu92, 7.1 outputs (MSI X470 Gaming Pro Carbon)"),
        ],
        ["10EC-0256"] =
        [
            new(5,   "Realtek ALC256"),
            new(11,  "Rockjesus - Realtek ALC256 (3246) - dell 7559"),
            new(12,  "HafidzRadhival - DELL Vostro 5468 ALC256 (3246)"),
            new(13,  "Insanelydeepak - Realtek ALC256 (3246) for Dell Series"),
            new(14,  "Insanelydeepak - Realtek ALC256 (3246) for Dell Series with subwoofer"),
            new(16,  "VicQ - Realtek ALC256 (3246) for Dell 7000 Series with 2.1Ch"),
            new(17,  "hjmmc - Realtek ALC256 (3246) for Magicbook 2018 with 4CH"),
            new(19,  "Wanwu - Realtek ALC256 (3246) for MateBook X Pro 2019"),
            new(20,  "Andres ZeroCross for Asus AIO PC V222UAK-WA541T"),
            new(21,  "Andres ZeroCross for Dell 5570"),
            new(22,  "Andres ZeroCross for Asus VivoBook Pro 17  N705UDR"),
            new(23,  "Andres ZeroCross for Razer Blade 15 RZ09-02705E75"),
            new(24,  "Andres ZeroCross - Intel NUC NUC10i5FNH"),
            new(28,  "vusun123 - ALC256 for Asus X555UJ"),
            new(33,  "insanelyme - ALC256 for Huawei Matebook D15 2018 (MRC-W10)"),
            new(38,  "lshbluesky - Realtek ALC256 for Samsung Galaxy Book NT750XDA-KF59U"),
            new(56,  "DalianSky - Realtek ALC256 (3246) for Dell 7000 Series"),
            new(57,  "Kk Realtek ALC256 (3246) for magicbook"),
            new(66,  "lgs3137 - Realtek ALC256 for ASUS Y5000U X507UBR"),
            new(67,  "Realtek ALC256 for Dell OptiPlex 7080"),
            new(68,  "Littlesum - Realtek ALC256 (3246)  for Intel NUC9 "),
            new(69,  "agasecond - Realtek ALC256 (3246) for Xiaomi Pro Enhanced 2019"),
            new(70,  "b0ltun/agasecond - Realtek ALC256 (3246) for Hasee KingBook X57S1"),
            new(76,  "Durian - Realtek ALC256 (3246) for MateBook X Pro 2019（4CH）"),
            new(77,  "Asus x430_s4300FN by fangf2018"),
            new(88,  "Asus x430_s4300FN by fangf2018 (mic in and line in  mic in separated)"),
            new(95,  "Floron - Realtek ALC256 (3246) for Honor MagicBook Pro HBB-WAH9"),
            new(97,  "DalianSky - Realtek ALC256 (3246) for MateBook X Pro 2019"),
            new(99,  "Hoping - Realtek ALC256 (3246) for XiaoMiPro 2020"),
        ],
        ["10EC-0282"] =
        [
            new(3,   "Mirone - Realtek ALC282_v1"),
            new(4,   "Mirone - Realtek ALC282_v2"),
            new(13,  "InsanelyDeepak - Realtek ALC282"),
            new(21,  "ALC282 for TinyMonster ECO by DalianSky"),
            new(22,  "Custom ALC282 lenovo y430p by loverto"),
            new(27,  "Skvo ALC282 Acer Aspire on IvyBridge by Andrey1970"),
            new(28,  "Custom ALC282 Acer Aspire E1-572G"),
            new(29,  "Custom ALC282 Dell Inspirion 3521 by Generation88"),
            new(30,  "Custom ALC282 Soarsea S210H by Jokerman1991"),
            new(41,  "Custom ALC282 Lenovo Y410P by yunsur"),
            new(43,  "Custom ALC282 Lenovo Y430P by yunsur"),
            new(51,  "Custom ALC282 Lenovo Y510P by yunsur"),
            new(69,  "Custom ALC282 Lenovo-IdeaPad-Z510 by hoseinrez"),
            new(76,  "Custom ALC282 Hasee K580C by YM2008"),
            new(86,  "Custom ALC282 for Asus x200la"),
            new(127, "No input boost ALC282 Acer Aspire on IvyBridge by Andrey1970"),
        ],
        ["10EC-0269"] =
        [
            new(1,   "Mirone Laptop patch ALC269 Asus N53J"),
            new(2,   "Mirone - Realtek ALC269-VB v1"),
            new(3,   "ALC269"),
            new(4,   "Mirone - Realtek ALC269-VB v2"),
            new(5,   "Mirone - Realtek ALC269-VB v3"),
            new(6,   "Mirone - Realtek ALC269-VC v1"),
            new(7,   "Mirone - Realtek ALC269-VC v2"),
            new(8,   "Mirone - Realtek ALC269VC-v3"),
            new(9,   "Mirone - Realtek ALC269VB v4"),
            new(10,  "Toleda ALC269 patch for Brix"),
            new(11,  "Mosser - ALC269VB Dell Precision Workstation T1600"),
            new(12,  "Asus Vivobook S200CE - Realtek ALC269VB"),
            new(13,  "InsanelyDeepak - Realtek ALC269VC for Samsung NP350V5C-S08IT"),
            new(14,  "Custom ALC269VC for Samsung NT550P7C-S65 with subwoofer 2.1ch by Rockjesus"),
            new(15,  "MacPeet - ALC269VB for Dell Optiplex 790"),
            new(16,  "MacPeet - ALC269VB for Dell Optiplex 790 Version2"),
            new(17,  "MacPeet - Latte Panda"),
            new(18,  "Hypereitan - ALC269VC for Thinkpad X230 i7"),
            new(19,  "Asus Vivobook S300CA - Realtek ALC269VB"),
            new(20,  "ALC269"),
            new(21,  "Goldfish64 - ALC269VB for Dell Optiplex 7010"),
            new(22,  "ALC269"),
            new(23,  "Custom ALC269VD for ThinkPad T430"),
            new(24,  "ALC269"),
            new(25,  "ALC269"),
            new(26,  "Andres ZeroCross - ALC269 for Infinix X1 XL11"),
            new(27,  "ALC269"),
            new(28,  "ALC269VC"),
            new(29,  "ALC269VC for Lenovo V580, ar4er"),
            new(30,  "ALC269VC for Hasee Z6SL7R3 by HF"),
            new(31,  "Custom ALC271x Acer Aspire s3-951"),
            new(32,  "Custom ALC269 Samsung np880z5e-x01ru by Constanta"),
            new(33,  "Custom ALC269VC for Samsung NP530U3C-A0F by BblDE3HAP"),
            new(34,  "Custom ALC269-VC Samsung np540U4E by majonez"),
            new(35,  "Mirone - Realtek ALC269VC - Samsung NP350V5C-S0URU"),
            new(36,  "Realtek ALC269 - Samsung R780"),
            new(37,  "ALC269"),
            new(38,  "jayveeballesteros - ALC269 for Fujitsu Esprimo D552"),
            new(39,  "5T33Z0 - Lenoco T530 with Dock 4337/4338"),
            new(40,  "vusun123 - Realtek ALC269VC for Lenovo W530"),
            new(44,  "ALC269VC"),
            new(45,  "maiconjs (Wolfield) - Asus A45A 269VB1"),
            new(47,  "ALC269VC for Hasee K790s"),
            new(55,  "ALC269VC for Thinkpad X230 with Dock4338"),
            new(58,  "HASEE Z6-i78154S2 ALC269 by lianny  "),
            new(66,  "ALC269VC for Clevo N155RD by DalianSky"),
            new(69,  "Vorshim92 - Realtek ALC269 - GF63 Thin 9SEXR"),
            new(76,  "Custom ALC269VB for ENZ C16B by jimmy19990"),
            new(77,  "ALC269"),
            new(88,  "ALC269 for MECHREVO X8Ti Plus by DalianSky"),
            new(89,  "ALC269"),
            new(91,  "ALC269"),
            new(93,  "ALC269"),
            new(99,  "ALC269-VB v4 Mod by Andrey1970 (No input boost - no noise in Siri)"),
            new(100, "ALC269"),
            new(111, "ALC269"),
            new(127, "ALC269"),
            new(128, "ALC269"),
            new(138, "aa820t - Realtek ALC269VC for Lenovo G480"),
            new(188, "ALC269"),
        ],
        ["111D-7695"] =
        [
            new(11, "Toshiba Satellite Pro C50"),
            new(12, "Custom IDT92HD95 by RehabMan"),
            new(14, "Custom IDT92HD95 - LenovoG710 by Svilen Ivanov layout14"),
        ],
        ["14F1-5098"] =
        [
            new(20, "phucnguyen2411 - CX20632 HP Elitedesk 800 G5 mini"),
            new(21, "Andres ZeroCross - Axioo MyPC One Pro H5"),
            new(23, "frankiezdh - Conexant CX20632 for HP ProDesk 480 G4"),
            new(28, "CX20632 by Daniel"),
        ],
        ["1102-0011"] =
        [
            new(0,  "Creative CA0132, default"),
            new(1,  "Creative CA0132: Alienware 15 R2"),
            new(2,  "Creative CA0132: Alienware 17, Desktop 2xIn 3xOut"),
            new(3,  "Creative CA0132, 2.0 + rear line-out"),
            new(4,  "Creative CA0132: R3Di default"),
            new(5,  "Creative CA0132, 2.0 front HP + Mic "),
            new(6,  "Creative CA0132, 5.1 with front HP"),
            new(7,  "Creative CA0132: ZxRi"),
            new(9,  "Creative CA0132 by Andres ZeroCross"),
            new(10, "Creative CA0132 by Andres ZeroCross"),
            new(11, "Custom Creative CA0132 5.1 channel"),
            new(12, "Custom Creative CA0132"),
            new(99, "Creative CA0132 5.1 channel for Alienware-M17X-R4 by DalianSky"),
        ],
        ["1013-4210"] = [new(13, "InsanelyDeepak - Cirrus Logic CS4210")],
        ["1013-4213"] = [new(28, "InsanelyDeepak - Cirrus Logic -CS4213")],
        ["11D4-1884"] = [new(11, "Goldfish64 - AD1884 - Panasonic Toughbook CF-30")],
        ["11D4-1984"] = [new(11, "MacPeet - AD1984 - for_IBM_Lenovo_ThinkPad_T61_T61p")],
        ["11D4-194A"] =
        [
            new(11, "MacPeet - AD1984A"),
            new(13, "MacPeet - AD1984A - Version2"),
            new(44, "AD1984A - giesteira"),
        ],
        ["11D4-1988"] = [new(12, "AD1988A by chrome")],
        ["11D4-198B"] =
        [
            new(5,  "Mirone - ADI-1988B"),
            new(7,  "Mirone - ADI-1988B"),
            new(12, "0x11d4198b"),
        ],
        ["11D4-989B"] =
        [
            new(5, "Mirone - ADI-2000B"),
            new(7, "Mirone - ADI-2000B"),
        ],
        ["10EC-0215"] = [new(18, "ALC215 for HP 830 G6 for 965987400abc")],
        ["10EC-0221"] =
        [
            new(11, "Goldfish64 - ALC221 for HP Compaq Pro 4300/Pro 6300/Elite 8300 (All Form Factors)"),
            new(15, "MacPeet - ALC221 for HP ELITE DESK 800 G1"),
            new(88, "ALC221 for HP ProDesk 400 G2 Desktop Mini PC by dragonbbc"),
        ],
        ["10EC-0222"] =
        [
            new(11, "ALC222 aka ALC3205-CG for HP EliteDesk 800 G6 Mini"),
            new(12, "ALC222 for Lenovo Tianyi 510s-07IMB Desktop PC by hgs v1"),
        ],
        ["10EC-0225"] =
        [
            new(28, "ALC225/ALC3253 on dell 7579 by ChalesYu"),
            new(30, "Custom ALC225/ALC3253 for Dell Inspiron 17-7779 by Constanta"),
            new(33, "Custom ALC225/ALC3253 by ChalesYu"),
            new(90, "Custom ALC225/ALC3253 for Dell Inspiron 15-5379 by fast900"),
        ],
        ["10EC-0230"] =
        [
            new(13, "Andres Laptop Patch ALC230 Lenovo 310-14ISK"),
            new(20, "Realtek ALC230 for Lenovo Ideapad 320 by maiconjs"),
        ],
        ["10EC-0233"] =
        [
            new(3,  "Mirone - Realtek ALC233"),
            new(4,  "Custom Realtek ALC233 (3236)"),
            new(5,  "Mirone - Realtek ALC233/ALC3236"),
            new(11, "Custom IDT 92HD81B1X5 by Andres ZeroCross"),
            new(13, "InsanelyDeepak - Realtek ALC233 for Asus X550LC"),
            new(21, "Andres ZeroCross - Realtek ALC233 for Asus A451LB-WX076D"),
            new(27, "Custom for Realtek ALC233 for SONY VAIO Fit 14E(SVF14316SCW) by SquallATF"),
            new(28, "Custom for Realtek ALC3236 for Asus TP500LN by Mohamed Khairy"),
            new(29, "Custom by Mirone - Realtek ALC233 (ALC3236) for Asus X550LDV"),
            new(32, "MacPeet - ALC233 (ALC3236) for ASUS VIVOBOOK S301LA "),
            new(33, "MacPeet - ALC233 (ALC3236) for ASUS VIVOBOOK S451LA "),
        ],
        ["10EC-0236"] =
        [
            new(3,  "Mirone - Realtek ALC236"),
            new(11, "Jake Lo - Realtek ALC236"),
            new(12, "ALC236 for Lenovo Xiaoxin Air 14IKBR by AlexanderLake"),
            new(13, "Custom - Realtek ALC236 for Lenovi Air 13 Pro by rexx0520"),
            new(14, "erinviegas - ALC236 for Lenovo Ideapad 330S"),
            new(15, "MacPeet - ALC236 for Lenovo Ideapad 500-15ISK"),
            new(16, "RodionS - ALC236 for Lenovo Ideapad 320s 14ikb"),
            new(17, "ALC236 for Lenovo IdeaPad 330S-14IKB by Ab2774"),
            new(18, "ALC236 for Lenovo LEGION Y7000/Y530 by xiaoM"),
            new(19, "wolf606 - ALC236 for Lenovo Ideapad 500-14ISK"),
            new(23, "JudySL - ALC236 for Lenovo Air 13 IML(S530-13IML)"),
            new(36, "volcbs - ALC236 for Lenovo Ideapad 510s 14isk (modified from MacPeet's)"),
            new(54, "ALC236 for DELL-5488 by Daggeryu"),
            new(55, "ALC236 for HP-240G8 by 8DireZ3"),
            new(68, "ALC236 for Dell Vostro 5401 for Lorys89"),
            new(69, "ALC236 for Dell ICL for Lorys89 by Vorshim"),
            new(99, "ALC236 for Lenovo Air 13 IWL by DalianSky"),
        ],
        ["10EC-0235"] =
        [
            new(3,  "Mirone - Realtek ALC235"),
            new(8,  "Realtek ALC235 Intel NUC 8"),
            new(11, "Realtek ALC235 for Ienovo by soto2080"),
            new(12, "ALC235 for Lenovo Rescuer 15ISK by Z39"),
            new(13, "Deskmini H470 ALC235 by dumk1217"),
            new(14, "the-braveknight - Realtek ALC235 for Lenovo Legion Y520"),
            new(15, "qiuchenly - Realtek ALC235 for ASUS FX53VD"),
            new(16, "MacPeet - Realtek ALC235 for ASUS GL553VD"),
            new(17, "Realtek ALC235 for Lenovo ThinkCentre Tiny M720q by marian"),
            new(18, "ALC235 for Asrock_bb_310 by_vio"),
            new(21, "ALC235 for Lenovo C530 Desktop PC by Andres ZeroCross"),
            new(22, "ALC235 for Asus ROG GL553VD-FY380 by Andres ZeroCross"),
            new(24, "ALC235 for Asus TUF FX705GM by TheRealGudv1n"),
            new(28, "vusun123 - Realtek ALC235 for Lenovo Legion Y520"),
            new(29, "hla63 - Realtek ALC235 for Msi Modern 15 A10M"),
            new(33, "Custom by fuzzyrock for ALC235 Lenovo A340-22IWL"),
            new(35, "Realtek ALC235 for Lenovo Qitian M420 by Cryse Hillmes"),
            new(36, "ALC235 for Lenovo Tianyi 510 pro-18ICB Desktop PC by hgs v1"),
            new(37, "Realtek ALC235 for Lenovo Ideacentre Mini 5"),
            new(72, "Realtek ALC235 for Lenovo M920x by meloay"),
            new(88, "NUC8I5BEH JUST MIC"),
            new(99, "ALC235 for Lenovo TianYi 510s Mini by DalianSky"),
        ],
        ["10EC-0245"] =
        [
            new(11, "Realtek ALC245 for Ienovo by soto2080"),
            new(12, "Realtek ALC245 for Ienovo by soto2080"),
            new(13, "lunjielee - Realtek ALC245 for HP Omen 2020"),
        ],
        ["10EC-0255"] =
        [
            new(3,   "Mirone - Realtek ALC255"),
            new(11,  "Realtek ALC255(3234) for Dell Optiplex series by Heporis"),
            new(12,  "ALC255, Dell Optiplex 7040 MT"),
            new(13,  "InsanelyDeepak - Realtek ALC255_v1"),
            new(15,  "Realtek ALC255 Gigabyte Brix BRI5(H) by Najdanovic Ivan"),
            new(17,  "InsanelyDeepak - Realtek ALC255_v2"),
            new(18,  "DuNe - Realtek ALC255 for Aorus X5V7"),
            new(20,  "Realtek ALC255 for Dell 7447 by was3912734"),
            new(21,  "ALC255 for Asus X441UA-WX096D by Andres ZeroCross"),
            new(22,  "Realtek ALC255(3234) for Asus N752VX by Feartech"),
            new(23,  "Realtek ALC255 for Acer Aspire A515-54G"),
            new(27,  "ALC255 for Asus X556UA m-dudarev"),
            new(28,  "Realtek ALC255 for Lenovo B470 - vusun123"),
            new(29,  "dhinakg - Realtek ALC255 for Acer Predator G3-571"),
            new(30,  "HongyuS - Realtek ALC255 for XiaoMiAir 13.3"),
            new(31,  "cowpod - Realtek ALC255 for UX360CA"),
            new(37,  "Imoize - Realtek ALC255 for Acer Nitro 5 AN515-52-73Y8"),
            new(66,  "ALC255 for Dell Optiplex7060/7070MT(Separate LineOut)"),
            new(69,  "juniorcaesar - Acer Aspire A315-56-327T ALC255"),
            new(71,  "DoctorStrange96 - Realtek ALC255 for Acer Aspire A51x"),
            new(80,  "Realtek ALC255 for Acer Aspire 7 A715-42G AMD by Long"),
            new(82,  "Realtek ALC255 for minisforum U820 by DalianSky"),
            new(86,  "Armênio - Realtek ALC255/ALC3234 - Dell 7348"),
            new(96,  "Bhavin dell 5559 alc255"),
            new(99,  "DalianSky - Realtek ALC255 (3246) for XiaoMi Air"),
            new(100, "DalianSky - Realtek ALC255 (3246) for alienware alpha r2"),
            new(255, "Realtek ALC255(3234) for Dell Inspiron 5548 by CynCYX"),
        ],
        ["10EC-0257"] =
        [
            new(11,  "MacPeet - Realtek ALC257 for Lenovo T480"),
            new(18,  "Realtek ALC257 for Lenovo Legion Y540 and Y7000-2019"),
            new(86,  "Armênio - Realtek ALC257 - Lenovo T480"),
            new(96,  "antoniomcr96 - Realtek ALC257 for Lenovo Thinkpad L390"),
            new(97,  "savvamitrofanov - Realtek ALC257 for Lenovo Thinkpad T490"),
            new(99,  "Realtek ALC257 for Lenovo XiaoXin Pro 2019(81XB/81XD) by DalianSky"),
            new(100, "Realtek ALC257 for Lenovo XiaoXin Pro 2019(81XB/81XD) by DalianSky"),
            new(101, "Hoping - Realtek ALC257 for Lenovo XiaoXin Air14ALC"),
        ],
        ["10EC-0260"] =
        [
            new(11, "MacPeet ALC260 for Fujitsu Celsius M 450"),
            new(12, "Custom ALC260"),
        ],
        ["10EC-0262"] =
        [
            new(7,  "DalianSky - ALC262 for MS-7480N1"),
            new(11, "MacPeet - ALC262"),
            new(12, "Goldfish64 - ALC262 for HP Compaq dc7700 SFF"),
            new(13, "MacPeet - ALC262 for Fujitsu Celsius H270"),
            new(14, "Goldfish64 - ALC262 for Dell Studio One 19 1909"),
            new(28, "MacPeet - ALC262 for HP Z800-Z600 series"),
            new(66, "ALC262 for MS-7847"),
        ],
        ["10EC-0268"] =
        [
            new(3,  "Mirone - Realtek ALC268"),
            new(11, "Goldfish64 - ALC268 for Dell Inspiron Mini 9"),
        ],
        ["10EC-0270"] =
        [
            new(3,  "Mirone - Realtek ALC270 v1"),
            new(4,  "Mirone - Realtek ALC270 v2"),
            new(21, "ALC270"),
            new(27, "ALC270"),
            new(28, "ALC270"),
        ],
        ["10EC-0272"] =
        [
            new(3,  "Mirone - Realtek ALC272"),
            new(11, "ALC 272 - Lenovo B470 - Sam Chen"),
            new(12, "Realtek ALC 272 for Lenovo Y470 by amu_1680c"),
            new(18, "Sniki - Realtek ALC 272 for Lenovo B570 and B570e"),
            new(21, "Andres ZeroCross - Lenovo All In One PC C440"),
        ],
        ["10EC-0274"] =
        [
            new(11, "Realtek ALC274 for Optiplex 7470 AIO"),
            new(21, "Andres ZeroCross - Realtek ALC274 for Dell Inspiron 27-7777 AIO Series"),
            new(28, "Andres ZeroCross - Realtek ALC274 for Dell Inspiron 27-7777 AIO Series"),
            new(35, "jackjack1-su Realtek ALC274 for Microsoft Surface Pro 7"),
            new(39, "Harahi - Realtek ALC274 for Mechrevo UmiPro3 (Tongfang GM5MG0Y)"),
        ],
        ["10EC-0275"] =
        [
            new(3,  "Mirone - Realtek ALC275"),
            new(13, "InsanelyDeepak - Realtek ALC275"),
            new(15, "Piscean - ALC275 for Sony Vaio SVD11225PXB"),
            new(28, "Custom ALC275 for Sony Vaio - vusun123"),
        ],
        ["10EC-0280"] =
        [
            new(3,  "Mirone - Realtek ALC280"),
            new(4,  "Mirone - Realtek ALC280 - ComboJack"),
            new(11, "Alienware alpha - Realtek ALC280"),
            new(13, "MacPeet - Realtek ALC280 - Dell T20 - Version1 - ManualMode"),
            new(15, "MacPeet - Realtek ALC280 - Dell T20 - Version2 - SwitchMode"),
            new(16, "cowpod - Realtek ALC280 - Optiplex 9020SFF"),
            new(17, "Realtek ALC280 - Optiplex 9020SFF - ManualMode"),
            new(18, "james090500 - Dell OptiPlex 9020 AIO"),
            new(21, "Dell Precision T7610 Workstation ALC280 by Andres ZeroCross"),
        ],
        ["10EC-0283"] =
        [
            new(1,  "Toleda NUC/BRIX patch ALC283"),
            new(3,  "Mirone - Realtek ALC283"),
            new(11, "Custom by Slbomber ALC283 (V3-371)"),
            new(12, "ThinkCentre M73(10AX) ALC283 by dumk1217"),
            new(13, "ALC283 for AlldoCube/Cube Mix Plus by Aldo97"),
            new(15, "MacPeet - alc283 for LENOVO IDEAPAD 14"),
            new(44, "Realtek ALC283 for ThinkCentre M93z 10AF ALC283 by giesteira "),
            new(45, "Realtek ALC283 for NUC7 by mikes "),
            new(66, "ASRock DeskMini 110(H110M-STX) ALC283 by licheedev"),
            new(73, "UHDbits - Realtek ALC283/ALC3239 for the Lenovo ThinkCentre M73 Tiny"),
            new(88, "Realtek ALC283 for DELL R14 3437 by xiaoleGun(zoran)"),
        ],
        ["10EC-0284"] = [new(3, "Mirone - Realtek ALC284")],
        ["10EC-0285"] =
        [
            new(11, "Rover Realtek ALC285 for X1C6th"),
            new(21, "Andres - Realtek ALC285 for  Lenovo X1 Carbon 6th "),
            new(31, "Flymin - Realtek ALC285 for  Thinkpad X1E"),
            new(33, "PIut02 - Realtek ALC285 for ROG-Zephyrus-G14"),
            new(52, "Z  Realtek ALC285 for thinkpad p52"),
            new(61, "Realtek ALC285 for Yoga C740 by fewtarius"),
            new(66, "Realtek ALC285 for Lenovo Legion S740 15-IRH (Y9000X 2020) by R-a-s-c-a-l"),
            new(71, "jpuxdev - Realtek ALC285 for Spectre x360 13-ap0xxx"),
            new(88, "Realtek ALC285 for Yoga S740 14IIL by frozenzero123"),
        ],
        ["10EC-0286"] =
        [
            new(3,  "Mirone - Realtek ALC286"),
            new(11, "Lenovo YOGA3 pro ALC286 - gdllzkusi"),
            new(69, "HP-Pavilion-Wave-600-A058cn"),
        ],
        ["10EC-0287"] =
        [
            new(11, "Realtek ALC287"),
            new(13, "ALC287 for Legion 5 Pro(R9000p)"),
            new(21, "ALC287 for Lenovo Yoga Slim 7-14IIL05 by Andres ZeroCross"),
        ],
        ["10EC-0288"] =
        [
            new(3,  "Mirone - Realtek ALC288"),
            new(13, "InsanelyDeepak - Realtek ALC288 for Dell XPS 9343"),
            new(23, "yyfn - Realtek ALC288 for Dell XPS 9343"),
        ],
        ["10EC-0289"] =
        [
            new(11, "leeoem - Realtek ALC289 for alienware m17r2"),
            new(12, "ALC289 for Dell XPS 13 9300"),
            new(13, "ALC289 for Dell XPS 15 9500 4 Speakers"),
            new(15, "MacPeet - ALC289 for Dell 7730 Precision CM240 "),
            new(23, "Realtek ALC289 for Acer PT515-51 By Bugprogrammer and Rover"),
            new(33, "PIut02 - Realtek ALC289 for ROG-Zephyrus-G14"),
            new(68, "ALC289 for Dell XPS 7390 ICL 2in1 By Lorys89"),
            new(69, "ALC289 for Dell XPS 2in1 7390 Vorshim"),
            new(87, "naufalkharits - Realtek ALC289 for Alienware m15"),
            new(93, "sweet3c - ALC289 for XPS 9500 4k "),
            new(99, "Realtek ALC289 for Dell XPS 13 9300 by DalianSky"),
        ],
        ["10EC-0290"] =
        [
            new(3,  "Mirone - Realtek ALC290"),
            new(4,  "macpeetALC ALC290 aka ALC3241"),
            new(10, "ALC3241 - HP Envy 15t-k200 Beats Audio 2.1"),
            new(15, "MacPeet - ALC290 for HP m6 n015dx"),
            new(28, "vusun123 - ALC 290 for Dell Vostro 5480"),
        ],
        ["10EC-0292"] =
        [
            new(12, "Custom ALC292"),
            new(15, "MacPeet - alc292 for LENOVO THINKPAD T450_T450s_X240 - ManualMode"),
            new(18, "vanquybn - ALC 292 for Dell M4800"),
            new(28, "vusun123 - ALC 292 for Lenovo T440"),
            new(32, "ALC292 for Lenovo T450s By Echo"),
            new(55, "baesar0 -ALC 292 for e6540 with dock"),
            new(59, "ALC 292 for Dell M4800 with Dock"),
        ],
        ["10EC-0293"] =
        [
            new(11, "ALC293 Dell E7450 by Andres ZeroCross"),
            new(28, "tluck - ALC 293 for Lenovo T460/T560 - extra LineOut on Dock"),
            new(29, "tluck - ALC 293 for Lenovo T460/T560"),
            new(30, "ALC 293 for Hasee ZX8-CT5DA/Clevo N9x0TD_TF by RushiaBoingBoing"),
            new(31, "ALC 293 for Hasee Z7-CT7NA by lgh07711"),
        ],
        ["10EC-0294"] =
        [
            new(11, "Rover - Realtek ALC294 for Asus FL8000U"),
            new(12, "MacPeet - Realtek ALC294 for Lenovo M710Q"),
            new(13, "InsanelyDeepak - Realtek ALC294"),
            new(15, "Realtek ALC294, ZenBook UX434"),
            new(21, "Andres ZeroCross - ALC294 ASUS ZenBook Flip 14 UX461UA"),
            new(22, "cowpod - Realtek ALC294 for ASUS ROG GL504GW"),
            new(28, "Ayat Kyo - Realtek ALC294 for Asus ROG G531GD"),
            new(44, "narcyzzo - Realtek ALC294 for ASUS UX534FAC"),
            new(66, "KKKIIINNN - ALC294 ASUS X542UQR"),
            new(99, "hoping - Realtek ALC294 for ASUS ROG GU502LV"),
        ],
        ["10EC-0299"] =
        [
            new(21, "Andres - ALC299 Acer Helios 500"),
            new(22, "Andres - ALC299 Dell XPS13"),
        ],
        ["10EC-0623"] =
        [
            new(13, "Pinokyo-H - Lenovo ThinkCentre SFF M720e"),
            new(21, "Andres ZeroCross - ALC623 Lenovo M70T"),
        ],
        ["10EC-0662"] =
        [
            new(5,  "Mirone - Realtek ALC662"),
            new(7,  "Mirone - Realtek ALC662"),
            new(11, "Custom ALC662 by Irving23 for Lenovo ThinkCentre M8400t-N000"),
            new(12, "Custom ALC662 by stich86 for Lenovo ThinkCentre M800"),
            new(13, "Custom ALC662 by Vandroiy for Asus X66Ic"),
            new(15, "MacPeet - ALC662 for Acer Aspire A7600U All in One"),
            new(16, "phucnguyen.2411 - ALC662v3 for Lenovo ThinkCentre M92P SFF"),
            new(17, "Custom ALC662 by aloha_cn for HP Compaq Elite 8000 SFF"),
            new(18, "Custom ALC662 by ryahpalma for MP67-DI/Esprimo Q900"),
            new(19, "Custom ALC662 for MSI X79A-GD65"),
            new(66, "ALC662v3 for Lenovo M415-D339 by Eric"),
        ],
        ["10EC-0663"] =
        [
            new(3,  "Mirone - Realtek ALC663"),
            new(4,  "Mirone - Realtek ALC663_V2"),
            new(15, "MacPeet - ALC663 for Fujitsu Celsius r670"),
            new(28, "ALC663"),
            new(99, "ALC663"),
        ],
        ["10EC-0665"] =
        [
            new(12, "InsanelyDeepak - Realtek ALC665"),
            new(13, "InsanelyDeepak - Realtek ALC665"),
        ],
        ["10EC-0668"] =
        [
            new(3,  "ALC668 Mirone Laptop Patch"),
            new(20, "Custom ALC668 by lazzy for laptop ASUS G551JM"),
            new(27, "ALC668 syscl Laptop Patch (DELL Precision M3800)"),
            new(28, "ALC668 Mirone Laptop Patch (Asus N750Jk)"),
            new(29, "ALC668 Custom (Asus N750JV)"),
        ],
        ["10EC-0670"] = [new(12, "Custom ALC670 by Alex Auditore")],
        ["10EC-0671"] =
        [
            new(12, "MacPeet - ALC671 for Fujitsu-Siemens D3433-S (Q170 chip)"),
            new(15, "MacPeet - ALC671 for Fujitsu  Esprimo C720"),
            new(16, "Sisumara - ALC671 for Fujitsu Q558"),
            new(88, " alc671 for HP 280 Pro G4  by Lcp"),
        ],
        ["10EC-0700"] =
        [
            new(11, "osy86 - Realtek ALC700"),
            new(22, "Baio77 - Realtek ALC700"),
        ],
        ["10EC-0882"] =
        [
            new(5, "Mirone - Realtek ALC882"),
            new(7, "Mirone - Realtek ALC882"),
        ],
        ["10EC-0883"] =
        [
            new(7,  "ALC883"),
            new(20, "ALC883"),
        ],
        ["10EC-0885"] =
        [
            new(1,  "toleda ALC885"),
            new(12, "ALC885"),
            new(15, "ALC885"),
        ],
        ["10EC-0887"] =
        [
            new(1,  "Toleda ALC887"),
            new(2,  "Toleda ALC887"),
            new(3,  "Toleda ALC887"),
            new(5,  "Mirone - Realtek ALC887-VD"),
            new(7,  "Mirone - Realtek ALC887-VD"),
            new(11, "InsanelyDeepak - Realtek ALC887-VD"),
            new(12, "VictorXu - ALC887-VD for ASUS H81M-D"),
            new(13, "InsanelyDeepak - Realtek ALC887-VD"),
            new(17, "InsanelyDeepak - Realtek ALC887-VD"),
            new(18, "InsanelyDeepak - Realtek ALC887-VD"),
            new(20, "Realtek ALC887-VD AD0 for Asus Z97M-PLUS/BR by maiconjs"),
            new(33, "Custom by klblk ALC887 for GA-Q87TN"),
            new(40, "Realtek ALC887-VD for Asus B85-ME by maiconjs"),
            new(50, "0th3r ALC887 for PRIME B250-PLUS"),
            new(52, "ALC887 for Asus PRIME Z270-P (full Rear and Front, non auto-switch) by ctich"),
            new(53, "ALC887 for Asus PRIME Z270-P (Rear LineOut1, Mic - LineOut2, LineIn - LineOut3 - 5.1 and Front, non auto-switch) by ctich"),
            new(87, "Realtek ALC887-VD GA-Z97 HD3 ver2.1 by varrtix"),
            new(99, "Custom Realtek ALC887-VD by Constanta"),
        ],
        ["10EC-0888"] =
        [
            new(1,  "toleda ALC888"),
            new(2,  "toleda ALC888"),
            new(3,  "toleda ALC888"),
            new(4,  "Mirone - Realtek ALC888 for Laptop"),
            new(5,  "Mirone - Realtek ALC888 3 ports (Pink, Green, Blue)"),
            new(7,  "Mirone - Realtek ALC888 5/6 ports (Gray, Black, Orange, Pink, Green, Blue)"),
            new(11, "ALC888S-VD Version1 for MedionP9614 by MacPeet"),
            new(27, "ALC888 for Acer Aspire 7738G by MacPeet"),
            new(28, "ALC888S-VD Version2 for MedionE7216 by MacPeet"),
            new(29, "ALC888S-VD Version3 for MedionP8610 by MacPeet"),
        ],
        ["10EC-0889"] =
        [
            new(1,  "ALC889, Toleda"),
            new(2,  "ALC889, Toleda"),
            new(3,  "ALC889, Toleda"),
            new(11, "MacPeet ALC889 Medion P4020 D"),
            new(12, "alc889, Custom by Sergey_Galan"),
        ],
        ["10EC-0867"] =
        [
            new(11, "MacPeet - ALC891 for HP Pavilion Power 580-030ng"),
            new(13, "InsanelyDeepak - Realtek ALC891"),
        ],
        ["10EC-0892"] =
        [
            new(1,   "ALC892, Toleda"),
            new(2,   "ALC892, Toleda"),
            new(3,   "ALC892, Toleda"),
            new(4,   "Mirone - Realtek ALC892 for Laptop"),
            new(5,   "ALC892, Mirone"),
            new(7,   "ALC892, Mirone"),
            new(11,  "ALC892 for MSI GF72-8RE"),
            new(12,  "MSI GP70/CR70 by Slava77"),
            new(15,  "MacPeet - alc892 for MSi Z97S SLI Krait Edition"),
            new(16,  "MacPeet - alc892 for MSI GL73-8RD"),
            new(17,  "MacPeet - alc892 for MSI B150M MORTAR - SwitchMode"),
            new(18,  "MacPeet - alc892 for MSI B150M MORTAR - ManualMode"),
            new(20,  "Custom ALC892 for GIGABYTE Z390M GAMING - Manual - by Bokey"),
            new(21,  "Custom ALC892 for GIGABYTE B365M AORUS ELITE"),
            new(22,  "ASRock Z390m-ITX/ac by imEgo"),
            new(23,  "ALC892 for ASRock B365 Pro4 By TheHackGuy"),
            new(28,  "ALC892 for Clevo P751DMG by Cryse Hillmes"),
            new(31,  "ALC892 for Clevo P65xSE/SA by Derek Zhu"),
            new(32,  "Custom ALC892 for G4/G5mod by ATL"),
            new(90,  "Custom ALC892 for GIGABYTE B360 M AORUS PRO"),
            new(92,  "Custom ALC892 for GA-Z87-HD3 by BIM167"),
            new(97,  "Custom ALC892 for HASEE K770e i7 D1 by gitawake"),
            new(98,  "ALC892 with working SPDIF"),
            new(99,  "Custom ALC892 DNS P150EM by Constanta"),
            new(100, "GeorgeWan - ALC892 for MSI-Z370-A PRO"),
        ],
        ["10EC-0897"] =
        [
            new(11, "Custom ALC897 by Sergey_Galan  for GIGABYTE Z590M"),
            new(12, "Custom ALC897 by Sergey_Galan  for GIGABYTE Z590 Gaming X"),
            new(13, "GeorgeWan - ALC897 for MSI-Z590-A-PRO"),
            new(21, "OPS Computer ALC897 by Andres ZeroCross"),
            new(22, "Asus VivoBook 15 OLED M513UA by Andres ZeroCross"),
            new(23, "ALC897 for Chuwi-CoreBookX14 by weachy"),
            new(66, "Asus_PRIME_B460M-K_ALC897"),
            new(69, "ALC297 for MSI Z490-A Pro by MathCampbell"),
            new(77, "ONDA H510D4 IPC ALC897"),
            new(98, "liangyi - ALC897 for MSI PRO B760M-P DDR4"),
            new(99, "Custom ALC897 by Marcos_Vinicios  for HUANANZHI QD4"),
        ],
        ["10EC-0899"] =
        [
            new(1,   "ALC898, Toleda"),
            new(2,   "ALC898, Toleda"),
            new(3,   "ALC898, Toleda"),
            new(5,   "Mirone - Realtek ALC898"),
            new(7,   "Mirone - Realtek ALC898"),
            new(11,  "Custom ALC898 by Irving23 for MSI GT72S 6QF-065CN"),
            new(13,  "InsanelyDeepak - Realtek ALC898 for MSI GS40"),
            new(28,  "ALC898, Toleda"),
            new(65,  "Realtek ALC898 for CLEVO P65xRS(-G) by datasone"),
            new(66,  "Realtek ALC898 for Clevo P750DM2-G"),
            new(98,  "Realtek ALC898 for MSI GE62 7RE Apache Pro by spectra"),
            new(99,  "Realtek ALC898 for MSI GP62-6QG Leopard Pro"),
            new(101, "ALC898, 4 Line Out by Andrey1970"),
        ],
        ["10EC-0900"] =
        [
            new(1,  "toleda - ALC1150 "),
            new(2,  "toleda - ALC1150 "),
            new(3,  "toleda - ALC1150 "),
            new(5,  "Mirone - Realtek ALC1150"),
            new(7,  "Mirone - Realtek ALC1150"),
            new(11, "Mirone - Realtek ALC1150 (mic boost)"),
            new(99, "ALC1150 for Gigabyte GA-Z97X-UD5H by DalianSky"),
        ],
        ["10EC-1220"] =
        [
            new(1,   "Toleda -  Realtek ALC1220"),
            new(2,   "Toleda -  Realtek ALC1220"),
            new(3,   "Toleda -  Realtek ALC1220"),
            new(5,   "Mirone - Realtek ALC1220"),
            new(7,   "Mirone - Realtek ALC1220"),
            new(11,  "Custom Realtek ALC1220 by truesoldier"),
            new(13,  "MacPeet - ALC1220 for Clevo P950HR"),
            new(15,  "fleaplus - ALC1220 for MSI WT75"),
            new(16,  "MacPeet - ALC1220 for Gigabyte Z390"),
            new(17,  "NIBLIZE - ALC1220 for Gigabyte Z490 Vision G manual SP/HP"),
            new(18,  "hgsshaanxi- ALC1220 for Gigabyte Z490 Aorus Master"),
            new(20,  "CaseySJ - ALC1220 for Gigabyte B550 Vision D"),
            new(21,  "ALC1220 for MSI GE63 Raider RGB 8RF"),
            new(25,  "Realtek ALC1220 for MSI GE73 Raider RGB 8RF by Ardhi96"),
            new(27,  "lostwolf - ALC1220 for Gigabyte Z370-HD3P"),
            new(28,  "MacPeet- ALC1220 for Z390 Aorus Ultra - Output SP/HP Manualmode "),
            new(29,  "MacPeet- ALC1220 for Z390 Aorus Ultra - Output SP/HP SwitchMode"),
            new(30,  "MacPeet- ALC1220 for Z370 AORUS Gaming 7 - Output SP/HP SwitchMode"),
            new(34,  "Custom ALC1220 for MSI P65 Creator by CleverCoder"),
            new(35,  "Custom ALC1220 for MSI GP75 9SD by Win7GM"),
            new(69,  "Lorys89 ALC1220 for AMD B450/B550 - SwitchMode"),
            new(98,  "Custom ALC1220 for Mi Gaming Notebook Creator by Xsixu"),
            new(99,  "MiBook 2019 by Dynamix1997"),
            new(100, "Hasee_G8-CU7PK"),
        ],
        ["10EC-0B00"] =
        [
            new(1,  "toleda -  Realtek ALCS1200A"),
            new(2,  "toleda -  Realtek ALCS1200A"),
            new(3,  "toleda -  Realtek ALCS1200A"),
            new(7,  "ALCS1200A for B550M Gaming Carbon WIFI by Kila2"),
            new(11, "owen0o0 -  Realtek ALCS1200A"),
            new(12, "mobilestebu - Realtek ALCS1200A for ASUS TUF-Z390M-Gaming (based on owen0o0 layout 11)"),
            new(23, "VictorXu -  Realtek ALCS1200A for MSI B460I GAMING EDGE WIFI"),
            new(49, "VictorXu -  Realtek ALCS1200A for Asrock Z490M-ITX"),
            new(50, "VictorXu -  Realtek ALCS1200A for Gigabyte B460M Aorus Pro"),
            new(51, "GeorgeWan - ALCS1200A for ASROCK-Z490-Steel-Legend"),
            new(52, "GeorgeWan - ALCS1200A for MSI-Mortar-B460M"),
            new(69, "Lorys89 and Vorshim92 - ALCS1200A for ASROCK Z490M ITX AC"),
        ],
        ["14F1-1F72"] =
        [
            new(3,  "Mirone - Conexant CX8050"),
            new(13, "Conexant CX8050 for ASUS S410U/X411U by cowpod"),
        ],
        ["14F1-1F86"] =
        [
            new(15, "MacPeet - Conexant CX8070 (CX11880) for Lenovo ThinkPad E590"),
            new(21, "Andres ZeroCross - Conexant CX8070 for Lenovo ThinkPad E14"),
        ],
        ["14F1-1FD6"] =
        [
            new(21, "Asus VivoBook Pro 15 CX8150 by Andres ZeroCross"),
            new(22, "ASUS VivoBook S405UA-EB906T - CX8150 by Andres ZeroCross"),
        ],
        ["14F1-2008"] =
        [
            new(3,  "Mirone - Conexant CX8200"),
            new(15, "MacPeet - Conexant CX8200 for HP ZbooK 15UG4"),
            new(21, "Andres ZeroCross - HP Spectre 13-V130NG"),
            new(23, "frankiezdh - Conexant CX8200 for HP Probook 440 G5"),
            new(80, "Conexant CX8200 for LG Gram Z990/Z90N"),
        ],
        ["14F1-20D0"] =
        [
            new(12, "Conexant CX8400"),
            new(13, "Conexant CX11970 (CX8400) for Acer Swift 3 SF313 (Ice Lake) by b0ltun"),
            new(14, "Conexant CX8400 for Zbook G5 - theroadw"),
        ],
        ["14F1-5051"] = [new(11, "Conexant CX20561")],
        ["14F1-5067"] = [new(3, "Mirone - Conexant CX20583")],
        ["14F1-5069"] =
        [
            new(3,  "Mirone - Conexant CX20585"),
            new(13, "Constanta custom for Toshiba L755-16R - Conexant CX20585"),
        ],
        ["14F1-506C"] = [new(3, "Mirone - Conexant CX20588")],
        ["14F1-506E"] =
        [
            new(3,  "Mirone - Conexant CX20590"),
            new(12, "CX20590 Custom for Lenovo Yoga 13 by usr-sse2"),
            new(13, "CX20590 for Lenovo T420 by tluck (Additional ports for use with a Docking Station)"),
            new(14, "CX20590 for Lenovo T420 by tluck (Standard Laptop)"),
            new(28, "Custom for Dell Vostro 3x60 by vusun123"),
        ],
        ["14F1-50A1"] =
        [
            new(11, "CX20641 - MacPeet - Dell OptiPlex 3010 - ManualMode"),
            new(13, "CX20641 - MacPeet - Dell OptiPlex 3010 - SwitchMode"),
        ],
        ["14F1-50A2"] =
        [
            new(11, "CX20642 - MacPeet - Fujitsu ESPRIMO E910 E90+ Desktop - ManualMode"),
            new(13, "CX20642 - MacPeet - Fujitsu ESPRIMO E910 E90+ Desktop - SwitchMode"),
        ],
        ["14F1-50F2"] = [new(3, "Mirone - Conexant CX20722")],
        ["14F1-50F4"] =
        [
            new(3,  "Mirone - Conexant CX20724"),
            new(13, "InsanelyDeepak - Conexant CX20724"),
        ],
        ["14F1-510F"] =
        [
            new(3,  "Mirone - Conexant CX20752"),
            new(21, "Andres ZeroCross - Asus A455LF - WX039D"),
            new(28, "Conexant - CX20751/2 by RehabMan"),
        ],
        ["14F1-5111"] =
        [
            new(3,  "Mirone - Conexant CX20753/4"),
            new(14, "InsanelyDeepak - Conexant CX20753/4"),
            new(15, "MacPeet - CX20753/4 for Lenovo Thinkpad E580"),
            new(21, "Andres ZeroCross - LG gram 15ZD960-GX5BK"),
        ],
        ["14F1-5113"] = [new(3, "Mirone - Conexant CX20755")],
        ["14F1-5114"] =
        [
            new(3,  "Mirone - Conexant CX20756"),
            new(13, "InsanelyDeepak - Conexant CX20756"),
        ],
        ["14F1-5115"] =
        [
            new(3,  "Mirone - Conexant CX20757"),
            new(28, "Custom CX20757 Lenovo G510 by Z39"),
        ],
        ["111D-76D1"] =
        [
            new(12, "Custom IDT 92HD87B1/3 by RehabMan"),
            new(13, "InsanelyDeepak - IDT92HD87B1/3"),
        ],
        ["111D-76D9"] = [new(13, "Custom IDT92HD87B2/4 by RehabMan")],
        ["111D-76F3"] = [new(3, "Mirone - IDT 92HD66C3/65")],
        ["111D-76B2"] = [new(3, "Mirone - IDT 92HD71B7X")],
        ["111D-7675"] =
        [
            new(19, "Dell Studio 1535 - IDT 92HD73C1X5 by chunnann"),
            new(21, "Andres ZeroCross - IDT 92HD73C1X5 for Alienware M17X R2"),
        ],
        ["111D-7676"] = [new(15, "MacPeet - IDT92HD73E1X5 for HP Envy h8 1425eg")],
        ["111D-76D5"] =
        [
            new(3,  "Mirone - IDT 92HD81B1C5"),
            new(11, "Goldfish64 - IDT 92HD81B1C5 for Dell Latitude E6410"),
        ],
        ["111D-7605"] =
        [
            new(3,  "Mirone - IDT 92HD81B1X5"),
            new(3,  "Mirone - IDT 92HD87B1"),
            new(12, "RehabMan - IDT 92HD81B1X5"),
            new(20, "Custom IDT 92HD81B1X5 by Sergey_Galan for HP ProBook 4520s"),
            new(21, "Custom IDT 92HD81B1X5 by Sergey_Galan for HP DV6-6169er"),
            new(28, "Custom IDT 92HD81B1X5 by Gujiangjiang for HP Pavilion g4 1000 series"),
            new(76, "IDT 92HD81B1X5 by SkyrilHD for HP Elitebook 8x70 series"),
        ],
        ["111D-7608"] = [new(3, "Mirone - IDT 92HD75B2X5")],
        ["111D-7603"] =
        [
            new(3,  "Mirone - IDT 92HD75B3X5"),
            new(11, "Mirone - IDT 92HD75B3X5"),
        ],
        ["111D-76E7"] =
        [
            new(3,  "Mirone - IDT 92HD90BXX"),
            new(12, "vusun123 - IDT 92HD90BXX"),
        ],
        ["111D-76E0"] =
        [
            new(3,  "Mirone - IDT 92HD91BXX "),
            new(12, "RehabMan - IDT 92HD91BXX for HP Envy"),
            new(13, "MacPeet - IDT92HD91BXX for HP Envy 6 1171-SG"),
            new(33, "jl4c - IDT 92HD91BXX for HP Envy"),
            new(84, "macish - IDT 92HD91BXX for HP Elitebook G1"),
        ],
        ["111D-76DF"] = [new(12, "Custom - IDT 92HD93BXX Dell Latitude E6430")],
        ["111D-76E5"] = [new(3, "Mirone - IDT 92HD99BXX ")],
        ["8384-7690"] = [new(11, "Goldfish64 - STAC9200 for Dell Precision 390, Latitude D520")],
        ["8384-76A0"] = [new(11, "Goldfish64 - STAC9205 for Dell Inspiron 1520, Latitude D630")],
        ["8384-7662"] = [new(12, "STAC9872AK for Sony VGN-FZ11MR by ctich")],
        ["1106-4760"] = [new(21, "VIA VT1705 ECS H81H3-M4 (1.0A) by Andres ZeroCross")],
        ["1106-8446"] =
        [
            new(3,  "Mirone - VIA VT1802"),
            new(33, "ChalesYu - VIA VT1802"),
            new(65, "VIA VT1802 for hasee k650d"),
        ],
        ["1106-0441"] =
        [
            new(5,  "Mirone - VIA VT2021"),
            new(7,  "Mirone - VIA VT2021"),
            new(9,  "SonicBSV - VIA VT2020/2021"),
            new(13, "Enrico - GA-Z77X-D3Hrev1.0 - VIA VT2020/2021"),
        ],
    };

    public static CodecLayout[]? GetLayouts(string codecId) =>
        Data.TryGetValue(codecId, out var layouts) ? layouts : null;
}
