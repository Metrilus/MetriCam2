@ECHO OFF
REM The build server runs Doxygen with the workspace root as working directory.
ECHO Patching doxygen config (paths) for build server
SET DOXY_FILE=doc\Doxyfile
ATTRIB -R %DOXY_FILE%
@powershell -Command "(get-content %DOXY_FILE%) |foreach-object {$_ -replace \"^STRIP_FROM_PATH\s*=.*$\", \"STRIP_FROM_PATH = .\"} |foreach-object {$_ -replace \"^INPUT\s*=.*$\", \"INPUT = .\"} |foreach-object {$_ -replace \"^OUTPUT_DIRECTORY\s*=.*$\", \"OUTPUT_DIRECTORY = doc\"} |set-content %DOXY_FILE%"
