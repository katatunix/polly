rm -dfr build

mkdir -p build/polly
cp -Rpv polly/build/* build/polly

rm build/polly/*.xml
rm build/polly/*.pdb
rm build/polly/System.ValueTuple.*

cp -pv readme.md build/polly
