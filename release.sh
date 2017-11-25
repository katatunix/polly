rm -dfr release

mkdir -p release/polly
cp -Rpv polly/build/* release/polly

rm release/polly/*.xml
rm release/polly/*.pdb
rm release/polly/System.ValueTuple.*

cp -pv readme.md release/polly
