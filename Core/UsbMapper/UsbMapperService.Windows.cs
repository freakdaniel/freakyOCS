#pragma warning disable CA1416

using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace OcsNet.Core.UsbMapper;

public sealed partial class UsbMapperService
{
    // Windows USB port enumeration using SetupAPI + DeviceIoControl (Hub IOCTLs).
    // Enumerates ALL ports (occupied AND empty) without requiring device plugging.

    #region P/Invoke declarations

    private static readonly Guid GuidUsbHubInterface = new("F18A0E88-C30C-11D0-8815-00A0C906BED8");

    private const uint DIGCF_PRESENT           = 0x02;
    private const uint DIGCF_DEVICEINTERFACE   = 0x10;
    private const uint GENERIC_WRITE           = 0x40000000;
    private const uint FILE_SHARE_READ_WRITE   = 0x03;  // FILE_SHARE_READ | FILE_SHARE_WRITE
    private const uint OPEN_EXISTING           = 3;
    private static readonly IntPtr InvalidHandle = new(-1);

    // Hub IOCTLs (from Windows DDK usbioctl.h)
    // CTL_CODE(FILE_DEVICE_USB=0x22, function, METHOD_BUFFERED=0, FILE_ANY_ACCESS=0)
    // Note: 0x220448 (fn=0x112) does NOT work on modern AMD/Intel via device interface path.
    // Confirmed working via diagnostic scan on Windows 11:
    private const uint IOCTL_USB_GET_NODE_INFORMATION               = 0x00220408; // fn=0x102 — confirmed working
    private const uint IOCTL_USB_GET_HUB_INFORMATION_EX             = 0x00220454; // fn=0x115 — returns USB_HUB_INFORMATION_EX (77 bytes), HighestPortNumber at offset 4
    private const uint IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX = 0x0022044C; // fn=0x113 — connection info per port

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public  int     CbSize;
        public  Guid    InterfaceClassGuid;
        public  uint    Flags;
        private UIntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public  int     CbSize;
        public  Guid    ClassGuid;
        public  uint    DevInst;
        private UIntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct USB_HUB_DESCRIPTOR
    {
        public byte   bDescriptorLength;
        public byte   bDescriptorType;
        public byte   bNumberOfPorts;
        public ushort wHubCharacteristics;
        public byte   bPowerOnToPowerGood;
        public byte   bHubControlCurrent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] bRemoveAndPowerMask;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct USB_HUB_INFORMATION
    {
        public USB_HUB_DESCRIPTOR HubDescriptor;
        public byte               IsBusPowered;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct USB_NODE_INFORMATION
    {
        public int                 NodeType;
        public USB_HUB_INFORMATION HubInformation;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct USB_DEVICE_DESCRIPTOR
    {
        public byte   bLength;
        public byte   bDescriptorType;
        public ushort bcdUSB;
        public byte   bDeviceClass;
        public byte   bDeviceSubClass;
        public byte   bDeviceProtocol;
        public byte   bMaxPacketSize0;
        public ushort idVendor;
        public ushort idProduct;
        public ushort bcdDevice;
        public byte   iManufacturer;
        public byte   iProduct;
        public byte   iSerialNumber;
        public byte   bNumConfigurations;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct USB_NODE_CONNECTION_INFORMATION_EX
    {
        public uint                  ConnectionIndex;
        public USB_DEVICE_DESCRIPTOR DeviceDescriptor;
        public byte                  CurrentConfigurationValue;
        public byte                  Speed;             // 0=Low 1=Full 2=High 3=Super 4=SuperPlus
        public byte                  DeviceIsHub;
        public ushort                DeviceAddress;
        public uint                  NumberOfOpenPipes;
        public uint                  ConnectionStatus;  // 0=NoDevice 1=DeviceConnected ...
    }

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid ClassGuid, string? Enumerator, IntPtr hwndParent, uint Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid,
        uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    // Raw IntPtr version avoids struct-marshaling issues with inline-string fields
    [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetailW")]
    private static extern bool SetupDiGetDeviceInterfaceDetailRaw(
        IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
        IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize,
        out uint RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SetupDiGetDeviceInstanceId(
        IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
        StringBuilder DeviceInstanceId, uint DeviceInstanceIdSize, out uint RequiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize,
        IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    // CfgMgr32 — device tree traversal (parent-child relationships)
    private const uint CR_SUCCESS = 0;

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Locate_DevNodeW(
        out uint pdnDevInst, string pDeviceID, uint ulFlags);

    [DllImport("cfgmgr32.dll")]
    private static extern uint CM_Get_Parent(
        out uint pdnDevInst, uint dnDevInst, uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Get_Device_IDW(
        uint dnDevInst, StringBuilder Buffer, uint BufferLen, uint ulFlags);

    #endregion

    // ── Entry point ───────────────────────────────────────────────────────────

    private async Task<List<UsbController>> GetWindowsControllersAsync(CancellationToken ct)
    {
        var controllers = new List<UsbController>();
        try
        {
            var deviceNames = await BuildWmiDeviceNameCacheAsync(ct);
            await Task.Run(() => EnumerateViaHubIoctl(controllers, deviceNames), ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "USB enumeration failed on Windows");
        }
        return controllers;
    }

    // ── Core enumeration ──────────────────────────────────────────────────────

    private void EnumerateViaHubIoctl(List<UsbController> result, Dictionary<string, string> deviceNames)
    {
        var hubs = CollectUsbHubInterfaces();
        _logger?.LogDebug("USB: found {Total} hub interfaces total", hubs.Count);

        var rootHubs = hubs.Where(h => h.InstanceId.Contains("ROOT_HUB", StringComparison.OrdinalIgnoreCase)).ToList();
        _logger?.LogDebug("USB: {Count} root hubs found", rootHubs.Count);

        if (rootHubs.Count == 0)
        {
            _logger?.LogWarning("USB: no root hubs found via SetupDi, falling back to WMI");
            FallbackWmiEnumeration(result);
            return;
        }

        // Group USB2 + USB3 root hubs that share the same physical XHCI controller
        // via the common "4&XXXXXXXX&Y" parent prefix in their instance IDs
        foreach (var group in rootHubs.GroupBy(h => ExtractParentPrefix(h.InstanceId)))
        {
            var ctrl = BuildControllerFromGroup(group.ToList(), deviceNames);
            if (ctrl is not null)
            {
                _logger?.LogDebug("USB controller '{Name}' — {Ports} ports found", ctrl.Name, ctrl.Ports.Count);
                result.Add(ctrl);
            }
        }

        // If every controller came back empty (IOCTL failed for all), use WMI fallback
        if (result.Count == 0)
        {
            _logger?.LogWarning("USB: IOCTL enumeration yielded no ports, falling back to WMI");
            FallbackWmiEnumeration(result);
        }
    }

    // ── SetupDi hub enumeration ───────────────────────────────────────────────

    private List<(string DevicePath, string InstanceId)> CollectUsbHubInterfaces()
    {
        var list    = new List<(string, string)>();
        var guid    = GuidUsbHubInterface;
        var devInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

        if (devInfo == InvalidHandle)
        {
            _logger?.LogWarning("USB: SetupDiGetClassDevs failed (error {Err})", Marshal.GetLastWin32Error());
            return list;
        }

        try
        {
            uint idx = 0;
            while (true)
            {
                var ifaceData = new SP_DEVICE_INTERFACE_DATA { CbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
                if (!SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, ref guid, idx++, ref ifaceData))
                    break;

                var devInfoData = new SP_DEVINFO_DATA { CbSize = Marshal.SizeOf<SP_DEVINFO_DATA>() };

                // Step 1: probe required buffer size (call will fail but fills RequiredSize)
                SetupDiGetDeviceInterfaceDetailRaw(devInfo, ref ifaceData, IntPtr.Zero, 0, out uint required, ref devInfoData);
                if (required < 8) required = 1024; // safety fallback

                // Step 2: allocate raw buffer and make the real call (avoids string-marshaling issues)
                IntPtr buf = Marshal.AllocHGlobal((int)required);
                try
                {
                    // cbSize: 8 on 64-bit, 6 on 32-bit for SP_DEVICE_INTERFACE_DETAIL_DATA_W
                    Marshal.WriteInt32(buf, Environment.Is64BitProcess ? 8 : 6);

                    if (!SetupDiGetDeviceInterfaceDetailRaw(devInfo, ref ifaceData, buf, required, out _, ref devInfoData))
                    {
                        _logger?.LogDebug("USB: detail failed for interface {Idx}, error {Err}", idx - 1, Marshal.GetLastWin32Error());
                        continue;
                    }

                    // DevicePath is a WCHAR[] starting at offset 4 (right after the DWORD cbSize)
                    var path = Marshal.PtrToStringUni(buf + 4);
                    if (string.IsNullOrEmpty(path)) continue;

                    var sb = new StringBuilder(512);
                    if (!SetupDiGetDeviceInstanceId(devInfo, ref devInfoData, sb, 512, out _)) continue;

                    _logger?.LogDebug("USB hub: '{InstanceId}' @ '{Path}'", sb, path);
                    list.Add((path, sb.ToString()));
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(devInfo); }

        return list;
    }

    // ── Controller grouping & building ────────────────────────────────────────

    private static string ExtractParentPrefix(string instanceId)
    {
        // "USB\ROOT_HUB30\4&16FD3950&0&0000" → "4&16FD3950&0"
        var parts    = instanceId.Split('\\');
        if (parts.Length < 3) return instanceId;
        var ampParts = parts[2].Split('&');
        return ampParts.Length >= 3 ? $"{ampParts[0]}&{ampParts[1]}&{ampParts[2]}" : parts[2];
    }

    private UsbController? BuildControllerFromGroup(
        List<(string DevicePath, string InstanceId)> hubs,
        Dictionary<string, string> deviceNames)
    {
        var prefix   = ExtractParentPrefix(hubs[0].InstanceId);
        var ctrlInfo = ReadControllerInfoFromRegistry(prefix);
        bool hasUsb3 = hubs.Any(h => h.InstanceId.Contains("ROOT_HUB30", StringComparison.OrdinalIgnoreCase));

        var controller = new UsbController
        {
            Name           = ctrlInfo?.Name ?? (hasUsb3 ? "USB 3.0 xHCI Host Controller" : "USB 2.0 EHCI Host Controller"),
            ControllerType = hasUsb3 ? UsbControllerType.XHCI : UsbControllerType.EHCI,
            Identifiers    = new UsbControllerIdentifiers
            {
                InstanceId = ctrlInfo?.InstanceId,
                PciId      = ctrlInfo?.PciId,
                Bdf        = ctrlInfo?.Bdf,
            }
        };

        // USB2 hub first (HS ports), USB3 hub second (SS ports)
        var sortedHubs = hubs.OrderBy(h => h.InstanceId.Contains("ROOT_HUB30", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

        // Build registry device map once, keyed by "{rootHubInstanceId}|{portNum}"
        var deviceMap = BuildPortDeviceMapFromRegistry();

        int hsIdx = 1, ssIdx = 1;
        foreach (var hub in sortedHubs)
        {
            bool isUsb3 = hub.InstanceId.Contains("ROOT_HUB30", StringComparison.OrdinalIgnoreCase);
            var  ports  = EnumerateHubPorts(hub.DevicePath, isUsb3, deviceNames);

            _logger?.LogDebug("USB: hub '{Id}' → {Count} ports", hub.InstanceId, ports.Count);

            // Enrich with connected-device info from registry/CM BEFORE port.Index is overwritten
            EnrichPortsFromRegistry(ports, hub.InstanceId, deviceMap);

            foreach (var port in ports)
            {
                if (!isUsb3) { port.Name = $"HS{hsIdx:D2}"; port.Index = hsIdx++; }
                else         { port.Name = $"SS{ssIdx:D2}"; port.Index = ssIdx++; }
                controller.Ports.Add(port);
            }
        }

        // Return controller even if 0 ports — frontend can still show the controller card
        return controller;
    }

    // ── Hub IOCTL port enumeration ────────────────────────────────────────────

    private List<UsbPort> EnumerateHubPorts(
        string hubDevicePath, bool isUsb3, Dictionary<string, string> deviceNames)
    {
        var ports  = new List<UsbPort>();

        // Use original device interface path (with #{GUID} suffix) — CreateFile works on it.
        // Use IOCTL_USB_GET_HUB_INFORMATION_EX (0x220454) which is confirmed working via
        // diagnostic scan. The older IOCTL_USB_GET_NODE_INFORMATION (fn=0x112) never works
        // on modern AMD/Intel XHCI via the device interface path.
        _logger?.LogDebug("USB: opening hub '{Path}'", hubDevicePath);

        var handle = CreateFile(hubDevicePath, GENERIC_WRITE, FILE_SHARE_READ_WRITE,
            IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

        if (handle == InvalidHandle)
        {
            _logger?.LogWarning("USB: CreateFile failed for '{Path}' (error {Err})", hubDevicePath, Marshal.GetLastWin32Error());
            return ports;
        }

        try
        {
            // USB_HUB_INFORMATION_EX layout (77 bytes):
            //   offset 0: HubType     (UINT32)
            //   offset 4: HighestPortNumber (UINT16) ← port count
            //   offset 6: hub descriptor union (USB_30_HUB_DESCRIPTOR or USB_HUB_DESCRIPTOR)
            const int bufSize = 512;
            IntPtr    buf     = Marshal.AllocHGlobal(bufSize);
            try
            {
                if (!DeviceIoControl(handle, IOCTL_USB_GET_HUB_INFORMATION_EX,
                        IntPtr.Zero, 0, buf, bufSize, out uint bytesRet, IntPtr.Zero))
                {
                    _logger?.LogWarning("USB: IOCTL_USB_GET_HUB_INFORMATION_EX failed (error {Err})", Marshal.GetLastWin32Error());
                    return ports;
                }

                int portCount = Marshal.ReadInt16(buf, 4);   // HighestPortNumber
                _logger?.LogDebug("USB: hub has {Count} ports (HubType={Type})",
                    portCount, Marshal.ReadInt32(buf, 0));

                for (uint p = 1; p <= portCount; p++)
                    ports.Add(QuerySinglePort(handle, p, isUsb3, deviceNames));
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        finally { CloseHandle(handle); }

        return ports;
    }

    private UsbPort QuerySinglePort(
        IntPtr hub, uint portNo, bool isUsb3, Dictionary<string, string> deviceNames)
    {
        // Extra space beyond the fixed struct for the variable-length PipeList
        int    connSize = Marshal.SizeOf<USB_NODE_CONNECTION_INFORMATION_EX>() + 30 * 12;
        IntPtr buf      = Marshal.AllocHGlobal(connSize);
        try
        {
            Marshal.WriteInt32(buf, (int)portNo);   // ConnectionIndex is first field

            // Diagnostic confirmed: IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX (0x22044C)
            // returns ok=True but br=0 for empty ports on this path.
            // If the IOCTL fails OR returns 0 bytes, treat port as empty.
            bool ok = DeviceIoControl(hub, IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX,
                buf, (uint)connSize, buf, (uint)connSize, out uint bytesRet, IntPtr.Zero);
            if (!ok || bytesRet == 0)
                return MakeEmptyPort(portNo, isUsb3);

            var info = Marshal.PtrToStructure<USB_NODE_CONNECTION_INFORMATION_EX>(buf);

            var speed = info.Speed switch
            {
                0 => UsbDeviceSpeed.LowSpeed,
                1 => UsbDeviceSpeed.FullSpeed,
                2 => UsbDeviceSpeed.HighSpeed,
                3 => UsbDeviceSpeed.SuperSpeed,
                4 => UsbDeviceSpeed.SuperSpeedPlus,
                _ => isUsb3 ? UsbDeviceSpeed.SuperSpeed : UsbDeviceSpeed.HighSpeed
            };

            if (info.ConnectionStatus != 1)   // 0=NoDevice, 2+=error states
                return MakeEmptyPort(portNo, isUsb3);

            var port = new UsbPort { Index = (int)portNo, SpeedClass = speed };

            if (info.DeviceDescriptor.bLength > 0)
            {
                var vid  = info.DeviceDescriptor.idVendor;
                var pid  = info.DeviceDescriptor.idProduct;
                var name = ResolveDeviceName(vid, pid, deviceNames);

                port.Devices.Add(new UsbDevice
                {
                    Name       = name,
                    Speed      = speed,
                    InstanceId = $"VID_{vid:X4}&PID_{pid:X4}"
                });

                port.GuessedType = InferConnectorType(name, speed);
                port.Selected    = true;    // auto-select occupied ports
            }

            port.GuessedType ??= isUsb3 ? UsbConnectorType.USB3TypeA : UsbConnectorType.USB2TypeA;
            return port;
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    private static UsbPort MakeEmptyPort(uint portNo, bool isUsb3) =>
        new()
        {
            Index       = (int)portNo,
            SpeedClass  = isUsb3 ? UsbDeviceSpeed.SuperSpeed : UsbDeviceSpeed.HighSpeed,
            GuessedType = isUsb3 ? UsbConnectorType.USB3TypeA : UsbConnectorType.USB2TypeA,
            Selected    = false
        };

    // ── Connector-type inference from device name ─────────────────────────────

    private static UsbConnectorType InferConnectorType(string deviceName, UsbDeviceSpeed speed)
    {
        var lower      = deviceName.ToLowerInvariant();
        bool isInternal = lower.Contains("bluetooth") || lower.Contains(" bt ")    ||
                          lower.Contains("camera")    || lower.Contains("webcam")  ||
                          lower.Contains("fingerprint")|| lower.Contains("ir sensor") ||
                          lower.Contains("card reader")|| lower.Contains("nfc")    ||
                          lower.Contains("biometric");

        if (isInternal) return UsbConnectorType.Internal;

        return speed switch
        {
            UsbDeviceSpeed.SuperSpeed or UsbDeviceSpeed.SuperSpeedPlus => UsbConnectorType.USB3TypeA,
            _ => UsbConnectorType.USB2TypeA
        };
    }

    // ── Device-name resolution ────────────────────────────────────────────────

    private static string ResolveDeviceName(ushort vid, ushort pid, Dictionary<string, string> cache)
    {
        var prefix = $"USB\\VID_{vid:X4}&PID_{pid:X4}";
        foreach (var kv in cache)
            if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        return $"USB Device (VID:{vid:X4} PID:{pid:X4})";
    }

    // ── USB device-to-port map from registry + CM API ─────────────────────────
    // Key: "{rootHubInstanceId}|{portNumber}"
    // Walks up the CM device tree (max 6 levels) to handle devices behind
    // intermediate hubs (e.g. front-panel USB 2.0 headers connected via a hub chip).

    private static Dictionary<string, (string Name, UsbDeviceSpeed Speed, string InstanceId)>
        BuildPortDeviceMapFromRegistry()
    {
        var map = new Dictionary<string, (string, UsbDeviceSpeed, string)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var usbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
            if (usbKey is null) return map;

            foreach (var vidPidName in usbKey.GetSubKeyNames())
            {
                // Skip root hub keys — they're never a "device" we want to show
                if (vidPidName.Contains("ROOT_HUB", StringComparison.OrdinalIgnoreCase)) continue;

                using var vidPidKey = usbKey.OpenSubKey(vidPidName);
                if (vidPidKey is null) continue;

                foreach (var instName in vidPidKey.GetSubKeyNames())
                {
                    using var instKey = vidPidKey.OpenSubKey(instName);
                    if (instKey is null) continue;

                    var rawName = instKey.GetValue("FriendlyName")?.ToString()
                               ?? instKey.GetValue("DeviceDesc")?.ToString() ?? "";
                    var devName = StripInfPrefix(rawName);
                    if (string.IsNullOrEmpty(devName)) continue;

                    string instanceId = $"USB\\{vidPidName}\\{instName}";

                    // Walk the CM tree to find the root hub and the port on it
                    if (!TryGetRootHubPort(instanceId,
                            out var rootHubId, out var rootPort, out var rootHubChildId))
                        continue;
                    if (rootPort <= 0) continue;

                    // Infer connection speed from the direct-child-of-root-hub node
                    var inferredSpeed = InferSpeedFromEntry(rootHubChildId, devName);

                    var key = $"{rootHubId}|{rootPort}";

                    // Prefer actual peripheral devices over generic hub nodes at the same port
                    bool isGenericHub = devName.StartsWith("Generic", StringComparison.OrdinalIgnoreCase)
                                     && devName.Contains("Hub", StringComparison.OrdinalIgnoreCase);
                    if (!map.ContainsKey(key) || !isGenericHub)
                        map[key] = (devName, inferredSpeed, instanceId);
                }
            }
        }
        catch { /* registry is best-effort */ }
        return map;
    }

    // Walk the CM device tree from instanceId upward until we find a ROOT_HUB parent.
    // Returns the root hub instance ID, the port number on it, and the ID of the
    // node that is the DIRECT child of the root hub (used for speed inference).
    private static bool TryGetRootHubPort(
        string instanceId, out string rootHubId, out int rootHubPort, out string rootHubChildId)
    {
        rootHubId = rootHubChildId = "";
        rootHubPort = -1;

        if (CM_Locate_DevNodeW(out uint devNode, instanceId, 0) != CR_SUCCESS) return false;

        uint currentNode = devNode;
        for (int depth = 0; depth < 6; depth++)
        {
            if (CM_Get_Parent(out uint parentNode, currentNode, 0) != CR_SUCCESS) return false;

            var parentSb = new StringBuilder(512);
            if (CM_Get_Device_IDW(parentNode, parentSb, 512, 0) != CR_SUCCESS) return false;
            string parentId = parentSb.ToString();

            if (parentId.Contains("ROOT_HUB", StringComparison.OrdinalIgnoreCase))
            {
                var curSb = new StringBuilder(512);
                CM_Get_Device_IDW(currentNode, curSb, 512, 0);
                rootHubChildId = curSb.ToString();
                rootHubId      = parentId;
                rootHubPort    = GetPortFromRegistry(rootHubChildId);
                return rootHubPort > 0;
            }
            currentNode = parentNode;
        }
        return false;
    }

    // Read the port number a device occupies on its immediate parent hub
    // from its LocationInformation registry value ("Port_#XXXX.Hub_#YYYY").
    private static int GetPortFromRegistry(string instanceId)
    {
        var parts = instanceId.Split('\\', 3);
        if (parts.Length < 3) return -1;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Enum\{parts[0]}\{parts[1]}\{parts[2]}");
            var locInfo = key?.GetValue("LocationInformation")?.ToString() ?? "";
            var m = Regex.Match(locInfo, @"Port_#(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }
        catch { return -1; }
    }

    // Infer USB speed from a device's CompatibleIDs registry entry.
    // Hub protocol codes: 01/02 = USB 2.0 (HighSpeed), 03 = USB 3 (SuperSpeed).
    // Falls back to name-based heuristic if CompatibleIDs aren't present.
    private static UsbDeviceSpeed InferSpeedFromEntry(string instanceId, string devName)
    {
        var parts = instanceId.Split('\\', 3);
        if (parts.Length >= 3)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Enum\{parts[0]}\{parts[1]}\{parts[2]}");
                var compat = (key?.GetValue("CompatibleIDs") as string[]) ?? [];
                foreach (var id in compat)
                {
                    if (id.Contains("Prot_01", StringComparison.OrdinalIgnoreCase) ||
                        id.Contains("Prot_02", StringComparison.OrdinalIgnoreCase))
                        return UsbDeviceSpeed.HighSpeed;
                    if (id.Contains("Prot_03", StringComparison.OrdinalIgnoreCase))
                        return UsbDeviceSpeed.SuperSpeed;
                }
            }
            catch { /* best-effort */ }
        }
        // Name-based fallback
        if (devName.Contains("SuperSpeed", StringComparison.OrdinalIgnoreCase))
            return UsbDeviceSpeed.SuperSpeed;
        if (devName.Contains("USB Hub", StringComparison.OrdinalIgnoreCase) ||
            devName.Contains("Generic Hub", StringComparison.OrdinalIgnoreCase))
            return UsbDeviceSpeed.HighSpeed;
        return UsbDeviceSpeed.Unknown;
    }

    // Strip "@driver.inf,%key%;" INF-style prefix from registry device names.
    private static string StripInfPrefix(string name)
    {
        if (name.StartsWith('@'))
        {
            var idx = name.IndexOf(';');
            if (idx >= 0) return name[(idx + 1)..].Trim();
        }
        return name;
    }

    // Applies the port-device map to a list of ports from ONE hub (before index renaming).
    // hubInstanceId must match the root hub's instance ID exactly as returned by SetupDi.
    private static void EnrichPortsFromRegistry(
        List<UsbPort> ports, string hubInstanceId,
        Dictionary<string, (string Name, UsbDeviceSpeed Speed, string InstanceId)> deviceMap)
    {
        foreach (var port in ports)
        {
            var key = $"{hubInstanceId}|{port.Index}";
            if (!deviceMap.TryGetValue(key, out var dev)) continue;
            if (string.IsNullOrEmpty(dev.Name)) continue;

            // Override the default (hub-derived) speed class with the inferred one
            if (dev.Speed != UsbDeviceSpeed.Unknown)
            {
                port.SpeedClass  = dev.Speed;
                port.GuessedType = dev.Speed >= UsbDeviceSpeed.SuperSpeed
                    ? UsbConnectorType.USB3TypeA
                    : UsbConnectorType.USB2TypeA;
            }

            port.Selected = true;
            port.Devices.Add(new UsbDevice
            {
                Name       = dev.Name,
                Speed      = port.SpeedClass,
                InstanceId = dev.InstanceId
            });
            port.GuessedType ??= InferConnectorType(dev.Name, port.SpeedClass);
        }
    }

    private async Task<Dictionary<string, string>> BuildWmiDeviceNameCacheAsync(CancellationToken ct)
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT PNPDeviceID, Name, Description FROM Win32_PnPEntity " +
                    "WHERE ClassGuid = '{36FC9E60-C465-11CF-8056-444553540000}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var id   = obj["PNPDeviceID"]?.ToString() ?? "";
                    var name = obj["Name"]?.ToString() ?? obj["Description"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                        cache[id] = name;
                }
            }
            catch { /* WMI is best-effort */ }
        }, ct);
        return cache;
    }

    // ── Controller metadata from PCI registry ─────────────────────────────────

    private sealed record ControllerRegistryInfo(
        string? Name, string? InstanceId, string[]? PciId, int[]? Bdf);

    private ControllerRegistryInfo? ReadControllerInfoFromRegistry(string parentPrefix)
    {
        try
        {
            using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
            if (pciKey is null) return null;

            foreach (var devKeyName in pciKey.GetSubKeyNames())
            {
                using var devKey = pciKey.OpenSubKey(devKeyName);
                if (devKey is null) continue;

                foreach (var instKeyName in devKey.GetSubKeyNames())
                {
                    if (!instKeyName.Contains(parentPrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    using var instKey = devKey.OpenSubKey(instKeyName);
                    if (instKey is null) continue;

                    var cls = instKey.GetValue("Class")?.ToString() ?? "";
                    if (!cls.Equals("USB", StringComparison.OrdinalIgnoreCase)) continue;

                    var name   = instKey.GetValue("FriendlyName")?.ToString()
                              ?? instKey.GetValue("DeviceDesc")?.ToString();
                    var locStr = instKey.GetValue("LocationInformation")?.ToString() ?? "";
                    var hwId   = (instKey.GetValue("HardwareID") as string[])?.FirstOrDefault() ?? "";

                    return new ControllerRegistryInfo(
                        name,
                        $"PCI\\{devKeyName}\\{instKeyName}",
                        ParsePciIdFromHwId(hwId),
                        ParseBdfFromLocation(locStr));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Registry controller lookup failed: {Error}", ex.Message);
        }
        return null;
    }

    private static string[]? ParsePciIdFromHwId(string hwId)
    {
        var m = Regex.Match(hwId,
            @"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})(?:&SUBSYS_([0-9A-F]{4})([0-9A-F]{4}))?",
            RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        return m.Groups[3].Success
            ? [m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value]
            : [m.Groups[1].Value, m.Groups[2].Value];
    }

    private static int[]? ParseBdfFromLocation(string location)
    {
        var m = Regex.Match(location,
            @"bus\s*(\d+)[,\s]+device\s*(\d+)[,\s]+function\s*(\d+)", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        return [int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value)];
    }

    // ── WMI fallback (when SetupDi finds no root hubs or IOCTL fails for all) ──

    private void FallbackWmiEnumeration(List<UsbController> result)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "USB Controller";
                var ctrl = new UsbController
                {
                    Name        = name,
                    Identifiers = new UsbControllerIdentifiers { InstanceId = obj["PNPDeviceID"]?.ToString() }
                };
                var u = name.ToUpperInvariant();
                if      (u.Contains("XHCI") || u.Contains("USB 3")) ctrl.ControllerType = UsbControllerType.XHCI;
                else if (u.Contains("EHCI"))                         ctrl.ControllerType = UsbControllerType.EHCI;
                result.Add(ctrl);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("WMI fallback also failed: {Error}", ex.Message);
        }
    }

    // ── Historical data merge ─────────────────────────────────────────────────

    private static void MergeControllers(List<UsbController> historical, List<UsbController> current)
    {
        foreach (var ctrl in current)
        {
            var existing = historical.FirstOrDefault(c => IsSameController(c, ctrl));
            if (existing is null) { historical.Add(ctrl); continue; }

            foreach (var port in ctrl.Ports)
            {
                var ep = existing.Ports.FirstOrDefault(p => p.Index == port.Index && p.Name == port.Name);
                if (ep is null)
                {
                    existing.Ports.Add(port);
                }
                else
                {
                    foreach (var dev in port.Devices)
                        if (!ep.Devices.Any(d => d.InstanceId == dev.InstanceId || d.Name == dev.Name))
                            ep.Devices.Add(dev);

                    ep.ConnectorType ??= port.ConnectorType;
                    ep.GuessedType   ??= port.GuessedType;
                }
            }

            existing.Ports = [.. existing.Ports.OrderBy(p => p.Name)];
        }
    }

    private static bool IsSameController(UsbController a, UsbController b)
    {
        var ia = a.Identifiers;
        var ib = b.Identifiers;

        if (ia.PciId is { Length: >= 2 } && ib.PciId is { Length: >= 2 })
            if (ia.PciId[0] == ib.PciId[0] && ia.PciId[1] == ib.PciId[1]) return true;

        if (ia.Bdf is not null && ib.Bdf is not null && ia.Bdf.SequenceEqual(ib.Bdf)) return true;
        if (!string.IsNullOrEmpty(ia.AcpiPath) && ia.AcpiPath == ib.AcpiPath)         return true;

        return a.Name == b.Name;
    }
}
