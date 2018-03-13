from=polly/build
from2=libs/bootstrap/x64
to=release

cp -pv $from/FSharp.Core.dll $to
cp -pv $from/FSharp.Data.dll $to
cp -pv $from/NghiaBui.Common.dll $to
cp -pv $from/polly.exe $to
cp -pv $from/polly.exe.config $to
cp -pv readme.md $to
cp -pv $from2/Release/injected.dll $to
cp -pv $from2/ReleaseNoDevFee/injectedNoDevFee.dll $to
