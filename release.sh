rm -dfr polly/build/tmp
cp -Rpv polly/build .
rm build/*.xml
rm build/*.pdb
rm build/System.ValueTuple.*
cp -pv readme.md build
