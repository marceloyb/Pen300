function LookupFunc {
	Param ($moduleName, $functionName)
	$assem = ([AppDomain]::CurrentDomain.GetAssemblies() | 
    Where-Object { $_.GlobalAssemblyCache -And $_.Location.Split('\\')[-1].
      Equals('System.dll') }).GetType('Microsoft.Win32.UnsafeNativeMethods')
    $tmp=@()
    $assem.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
	return $tmp[0].Invoke($null, @($assem.GetMethod('GetModuleHandle').Invoke($null, @($moduleName)), $functionName))	
}
$MessageBoxA = LookupFunc user32.dll MessageBoxA
$Assembly = New-Object System.Reflection.AssemblyName('MessageBoxDelegate')
$Domain = [AppDomain]::CurrentDomain
$AssemblyBuilder = $Domain.DefineDynamicAssembly($Assembly, [System.Reflection.Emit.AssemblyBuilderAccess]::Run)
$ModuleBuilder = $AssemblyBuilder.DefineDynamicModule('InMemoryModule', $false)
$TypeBuilder = $ModuleBuilder.DefineType('MessageBoxType', 'Class, Public, Sealed, AnsiClass, AutoClass', [System.MulticastDelegate])
$ConstructorBuilder = $TypeBuilder.DefineConstructor(
  'RTSpecialName, HideBySig, Public', 
    [System.Reflection.CallingConventions]::Standard, 
      @([IntPtr], [String], [String], [int]))
$ConstructorBuilder.SetImplementationFlags('Runtime, Managed')
$MethodBuilder = $TypeBuilder.DefineMethod('Invoke', 
  'Public, HideBySig, NewSlot, Virtual', 
    [int], 
      @([IntPtr], [String], [String], [int]))
$MethodBuilder.SetImplementationFlags('Runtime, Managed')
$MessageBoxSig = $TypeBuilder.CreateType()
$MessageBox = [System.Runtime.InteropServices.Marshal]::GetDelegateForFunctionPointer($MessageBoxA, $MessageBoxSig)
$MessageBox.Invoke([IntPtr]::Zero,"Hello World","This is My MessageBox",0)