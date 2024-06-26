function LookupFunc {

	Param ($moduleName, $functionName)

	$assem = ([AppDomain]::CurrentDomain.GetAssemblies() | 
    Where-Object { $_.GlobalAssemblyCache -And $_.Location.Split('\\')[-1].
      Equals('System.dll') }).GetType('Microsoft.Win32.UnsafeNativeMethods')
    $tmp=@()
    $assem.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
	return $tmp[0].Invoke($null, @($assem.GetMethod('GetModuleHandle').Invoke($null, @($moduleName)), $functionName))
	
}

$WinExec = LookupFunc kernel32.dll WinExec

$Assembly = New-Object System.Reflection.AssemblyName('WinExecDelegate')
$Domain = [AppDomain]::CurrentDomain
$AssemblyBuilder = $Domain.DefineDynamicAssembly($Assembly, [System.Reflection.Emit.AssemblyBuilderAccess]::Run)
$ModuleBuilder = $AssemblyBuilder.DefineDynamicModule('InMemoryModule', $false)
$TypeBuilder = $ModuleBuilder.DefineType('WinExecType', 'Class, Public, Sealed, AnsiClass, AutoClass', [System.MulticastDelegate])

$ConstructorBuilder = $TypeBuilder.DefineConstructor(
  'RTSpecialName, HideBySig, Public', 
    [System.Reflection.CallingConventions]::Standard, 
      @([String], [int]))
$ConstructorBuilder.SetImplementationFlags('Runtime, Managed')

$MethodBuilder = $TypeBuilder.DefineMethod('Invoke', 
  'Public, HideBySig, NewSlot, Virtual', 
    [int], 
      @([String], [int]))
$MethodBuilder.SetImplementationFlags('Runtime, Managed')

$WinExecSig = $TypeBuilder.CreateType()

$WinExec = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($WinExec, $WinExecSig)
$WinExec.Invoke("notepad.exe", 1)