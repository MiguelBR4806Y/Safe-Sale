using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace SS.Services;

internal static class MacDockIcon
{
    private const string ObjCLibrary = "/usr/lib/libobjc.dylib";
    private static readonly Uri IconUri = new("avares://SafeSale/Assets/SS-ico.png");

    [DllImport(ObjCLibrary, EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjCGetClass(string name);

    [DllImport(ObjCLibrary, EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSendPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSendPtrArg(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSendBytes(IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void MsgSendVoidArg(IntPtr receiver, IntPtr selector, IntPtr arg);

    public static void Apply()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        try
        {
            NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");

            using var stream = AssetLoader.Open(IconUri);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                var nsData = MsgSendBytes(
                    ObjCGetClass("NSData"),
                    SelRegisterName("dataWithBytes:length:"),
                    handle.AddrOfPinnedObject(),
                    (nuint)bytes.Length);

                if (nsData == IntPtr.Zero)
                    return;

                var allocatedImage = MsgSendPtr(ObjCGetClass("NSImage"), SelRegisterName("alloc"));
                var nsImage = MsgSendPtrArg(allocatedImage, SelRegisterName("initWithData:"), nsData);
                if (nsImage == IntPtr.Zero)
                    return;

                var nsApplication = MsgSendPtr(
                    ObjCGetClass("NSApplication"),
                    SelRegisterName("sharedApplication"));

                if (nsApplication != IntPtr.Zero)
                {
                    MsgSendVoidArg(
                        nsApplication,
                        SelRegisterName("setApplicationIconImage:"),
                        nsImage);
                }

                MsgSendPtr(nsImage, SelRegisterName("release"));
            }
            finally
            {
                handle.Free();
            }
        }
        catch
        {
        }
    }
}
