
$endfile = "Best Calculator and BC BASIC.docx"
$endzip = "myresult.zip"
$startappx = "start.appxmanifest"
$startfile = "startdocument.zip"
$tempdir = "mydoc"


if (test-path $tempdir) { remove-item $tempdir -recurse }
if (test-path $startappx) { remove-item $startappx }
if (test-path $startfile) { remove-item $startfile }
