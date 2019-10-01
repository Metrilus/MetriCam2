@echo off

echo Updating Version: %2 / %3
@powershell -Command "(get-content \"%~f1\")" ^
 " |foreach-object { $_ -replace \"^^(\s*^<FileVersion^>)[^^^<]*\", \"`${1}%2\" }" ^
 " |foreach-object { $_ -replace \"^^(\s*^<Version^>)[^^^<]*\", \"`${1}%3\" }" ^
 " |set-content \"%~f1\""
