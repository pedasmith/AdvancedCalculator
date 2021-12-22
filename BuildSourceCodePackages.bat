set SRC1=AdvancedCalculatorUniversal
set SRC2=BluetoothCalculatorUniversal
set SRC3=BCBasic
set SRC4=Fonts
set MINISRC=..\Mini
set DEST=SourceCode
set FINALDEST=BluetoothCalculatorUniversal\Assets\SourceCode
set CODE1DIR=BluetoothCalculatorUniversal\Bluetooth
set CODE2DIR=BluetoothCalculatorUniversal\Bluetooth\Devices
set ZIPNAME=SourceCode.zip

copy %CODE1DIR%\Bluetooth.cs %FINALDEST%
copy %CODE1DIR%\BluetoothDevice.cs %FINALDEST%
copy %CODE1DIR%\BluetoothUtilities.cs %FINALDEST%
copy %CODE1DIR%\ObjectList.cs %FINALDEST%
copy %CODE2DIR%\DOTTI.cs %FINALDEST%
copy %CODE2DIR%\Hexiwear.cs %FINALDEST%
copy %CODE2DIR%\MagicLight.cs %FINALDEST%
copy %CODE2DIR%\NOTTI.cs %FINALDEST%
copy %CODE2DIR%\SensorTag2541.cs %FINALDEST%
ECHO This is a dummy SourceCode.zip file >%FINALDEST%\%ZIPNAME%

rmdir /s /q %DEST%
xcopy %SRC1% %DEST%\AdvancedCalculator\%SRC1% /Exclude:BuildSourceCodePackagesExclude.txt /S /I /Y
xcopy %SRC2% %DEST%\AdvancedCalculator\%SRC2% /Exclude:BuildSourceCodePackagesExclude.txt /S /I /Y
xcopy %SRC3% %DEST%\AdvancedCalculator\%SRC3% /Exclude:BuildSourceCodePackagesExclude.txt /S /I /Y
xcopy %SRC4% %DEST%\AdvancedCalculator\%SRC4% /Exclude:BuildSourceCodePackagesExclude.txt /S /I /Y
xcopy %MINISRC%\Feedback %DEST%\Mini\Feedback /Exclude:BuildSourceCodePackagesExclude.txt /S /I /Y
xcopy %MINISRC%\Utilities %DEST%\Mini\Utilities /Exclude:BuildSourceCodePackagesExclude.txt /S /I /Y
xcopy AdvancedCalculatorUniversal.sln %DEST%\AdvancedCalculator\ /Exclude:BuildSourceCodePackagesExclude.txt /Y

pushd %DEST%
7Z a -r ..\%ZIPNAME%
popd
copy %ZIPNAME% %FINALDEST%\%ZIPNAME%

:DONE
pause