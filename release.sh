from=polly/build
from2=libs/bootstrap/x64
to=release

cp -pv $from/*.dll $to
cp -pv $from/*.exe $to
cp -pv $from/*.config $to
cp -pv readme.md $to
cp -pv $from2/Release/injected.dll $to
cp -pv $from2/ReleaseNoDevFee/injectedNoDevFee.dll $to
