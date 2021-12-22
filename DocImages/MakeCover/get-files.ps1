
$startappx = "start.appxmanifest"
$startdoc = "startdocument.zip"
$startfile = "startdocument.zip"
$tempdir = "mydoc"

if (test-path $startdoc) { remove-item $startdoc }
copy-item "..\..\Best Calculator and BC BASIC.docx" $startdoc

if (test-path $startappx) { remove-item $startappx }
copy-item "..\..\BluetoothCalculatorUniversal\Package.appxmanifest" $startappx

if (test-path $tempdir) { remove-item $tempdir -recurse }
expand-archive $tempdir -literalpath $startfile

