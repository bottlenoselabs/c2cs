tmp_file=$(mktemp)
cat > $tmp_file <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="github" value="https://nuget.pkg.github.com/lithiumtoast/index.json" />
    </packageSources>
    <packageSourceCredentials>
        <github>
            <add key="Username" value="lithiumtoast-mu" />
            <add key="ClearTextPassword" value="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" />
        </github>
    </packageSourceCredentials>
</configuration>
EOF
dotnet tool install C2CS --global --version *-* --configfile $tmp_file 
rm -rf $tmp_dir