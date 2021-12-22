#
# OLD FILE DO NOT USE
#
$tempdir = "mydoc"
$startfile = "startdocument.zip"
$endzip = "myresult.zip"
$endfile = "Best Calculator and BC BASIC.docx"

if (test-path $tempdir) { remove-item $tempdir -recurse }
if (test-path $endzip) { remove-item $endzip }
if (test-path $endfile) { remove-item $endfile }

expand-archive $tempdir -literalpath $startfile
copy "image1.png" "$tempdir\word\media"

cd $tempdir 
compress-archive "*" -destinationpath "..\$endzip"
cd ".."
rename-item $endzip $endfile
remove-item $tempdir -recurse
remove-item $startfile
