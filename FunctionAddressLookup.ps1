function LookupFunc {

	Param ($moduleName, $functionName)

    $systemdll = ([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GlobalAssemblyCache -And $_.Location.Split('\\')[-1].Equals('System.dll') })
    $unsafeObj = $systemdll.GetType('Microsoft.Win32.UnsafeNativeMethods')
    $GetModuleHandle = $unsafeObj.GetMethod('GetModuleHandle')
    
    $tmp=@()
    $unsafeObj.GetMethods() | ForEach-Object {If($_.Name -eq "GetProcAddress") {$tmp+=$_}}
    $GetProcAddress = $tmp[0]
    $moduleName
    $functionName

    $assembly = $GetModuleHandle.Invoke($null, $moduleName)
    
    $GetProcAddress.Invoke($null, @($assembly, $functionName))
}

LookupFunc "user32.dll" "MessageBoxA"