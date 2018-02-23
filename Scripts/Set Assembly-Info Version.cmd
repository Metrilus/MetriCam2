@powershell -Command "(get-content \"%~f1\") | foreach-object { $_ -replace \"(Assembly\w*Version)\(`\"(\d+\.\d+\.\d+\.\d+)`\"\)\", \"`$1(`\"%2`\")\" } | set-content \"%~f1\""
